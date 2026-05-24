using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Primo.MIA
{
    internal static class DatabaseUpsertHelper
    {
        internal static UpsertResult ExecuteUpsertCore(
            DbConnection connection,
            DbTransaction transaction,
            string providerInvariantName,
            DataTable dataTable,
            string destinationTableName,
            List<string> keyColumns,
            List<string> updateColumns,
            Dictionary<string, string> columnMappings,
            int commandTimeoutSeconds)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable), "Исходная таблица данных не указана.");
            if (string.IsNullOrWhiteSpace(destinationTableName))
                throw new ArgumentException("Имя таблицы-приёмника не может быть пустым.", nameof(destinationTableName));
            if (keyColumns == null || keyColumns.Count == 0)
                throw new ArgumentException("Список ключевых колонок не может быть пустым.", nameof(keyColumns));

            var allMappings = DatabaseBulkInsertHelper.BuildMappings(dataTable, columnMappings);
            var keyMappings = ResolveMappings(allMappings, keyColumns);
            var updateMappings = updateColumns != null && updateColumns.Count > 0
                ? ResolveMappings(allMappings, updateColumns)
                : System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(allMappings, m =>
                    System.Linq.Enumerable.All(keyMappings, k =>
                        !string.Equals(k.SourceColumn, m.SourceColumn, StringComparison.OrdinalIgnoreCase))));

            using (var updateCommand = CreateUpsertUpdateCommand(connection, transaction, providerInvariantName,
                       destinationTableName, keyMappings, updateMappings, commandTimeoutSeconds))
            using (var insertCommand = CreateUpsertInsertCommand(connection, transaction, providerInvariantName,
                       destinationTableName, allMappings, commandTimeoutSeconds))
            using (var existsCommand = updateMappings.Count == 0
                       ? CreateUpsertExistsCommand(connection, transaction, providerInvariantName,
                           destinationTableName, keyMappings, commandTimeoutSeconds)
                       : null)
            {
                var insertedCount = 0;
                var updatedCount = 0;

                foreach (DataRow row in dataTable.Rows)
                {
                    var wasUpdated = false;

                    if (updateMappings.Count > 0)
                    {
                        AssignUpsertUpdateParameterValues(updateCommand, row, updateMappings, keyMappings);
                        wasUpdated = updateCommand.ExecuteNonQuery() > 0;
                    }
                    else
                    {
                        AssignUpsertExistsParameterValues(existsCommand, row, keyMappings);
                        wasUpdated = (DatabaseHelper.ConvertScalarToInt32(existsCommand.ExecuteScalar()) ?? 0) > 0;
                    }

                    if (wasUpdated)
                    {
                        updatedCount++;
                        continue;
                    }

                    DatabaseBulkInsertHelper.AssignInsertParameterValues(insertCommand, row, allMappings);
                    insertCommand.ExecuteNonQuery();
                    insertedCount++;
                }

                return new UpsertResult { InsertedCount = insertedCount, UpdatedCount = updatedCount };
            }
        }

        internal static List<BulkInsertColumnMapping> ResolveMappings(
            List<BulkInsertColumnMapping> mappings,
            List<string> names)
        {
            return System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(
                System.Linq.Enumerable.Where(names, name => !string.IsNullOrWhiteSpace(name)),
                name =>
                {
                    var mapping = System.Linq.Enumerable.FirstOrDefault(mappings, m =>
                        string.Equals(m.SourceColumn, name, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(m.DestinationColumn, name, StringComparison.OrdinalIgnoreCase));

                    if (mapping == null)
                        throw new InvalidOperationException(
                            $"Колонка '{name}' не найдена в DataTable или маппинге.");

                    return mapping;
                }));
        }

        private static DbCommand CreateUpsertUpdateCommand(
            DbConnection connection,
            DbTransaction transaction,
            string providerInvariantName,
            string destinationTableName,
            List<BulkInsertColumnMapping> keyMappings,
            List<BulkInsertColumnMapping> updateMappings,
            int commandTimeoutSeconds)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = commandTimeoutSeconds > 0 ? commandTimeoutSeconds : 60;
            command.CommandText = BuildUpdateCommandText(providerInvariantName, destinationTableName, keyMappings, updateMappings);
            CreateUpsertUpdateParameters(command, providerInvariantName, updateMappings, keyMappings);
            DatabaseCommandHelper.TryPrepareCommand(command);
            return command;
        }

        private static DbCommand CreateUpsertInsertCommand(
            DbConnection connection,
            DbTransaction transaction,
            string providerInvariantName,
            string destinationTableName,
            List<BulkInsertColumnMapping> mappings,
            int commandTimeoutSeconds)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = commandTimeoutSeconds > 0 ? commandTimeoutSeconds : 60;
            command.CommandText = DatabaseBulkInsertHelper.BuildInsertCommandText(providerInvariantName, destinationTableName, mappings);
            DatabaseBulkInsertHelper.CreateInsertParameters(command, providerInvariantName, mappings);
            DatabaseCommandHelper.TryPrepareCommand(command);
            return command;
        }

        private static DbCommand CreateUpsertExistsCommand(
            DbConnection connection,
            DbTransaction transaction,
            string providerInvariantName,
            string destinationTableName,
            List<BulkInsertColumnMapping> keyMappings,
            int commandTimeoutSeconds)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = commandTimeoutSeconds > 0 ? commandTimeoutSeconds : 60;
            command.CommandText = BuildExistsCommandText(providerInvariantName, destinationTableName, keyMappings);
            CreateUpsertExistsParameters(command, providerInvariantName, keyMappings);
            DatabaseCommandHelper.TryPrepareCommand(command);
            return command;
        }

        private static string BuildUpdateCommandText(
            string providerInvariantName,
            string destinationTableName,
            List<BulkInsertColumnMapping> keyMappings,
            List<BulkInsertColumnMapping> updateMappings)
        {
            if (updateMappings == null || updateMappings.Count == 0)
                throw new ArgumentException("Список колонок обновления не может быть пустым.", nameof(updateMappings));

            var safeTableName = DatabaseHelper.QuoteQualifiedIdentifier(providerInvariantName, destinationTableName);
            var setClause = string.Join(", ", System.Linq.Enumerable.Select(updateMappings, (m, index) =>
                DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn) + " = " +
                DatabaseCommandHelper.GetParameterPlaceholder(providerInvariantName, index)));
            var whereClause = string.Join(" AND ", System.Linq.Enumerable.Select(keyMappings, (m, index) =>
                DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn) + " = " +
                DatabaseCommandHelper.GetParameterPlaceholder(providerInvariantName, updateMappings.Count + index)));
            return $"UPDATE {safeTableName} SET {setClause} WHERE {whereClause}";
        }

        private static string BuildExistsCommandText(
            string providerInvariantName,
            string destinationTableName,
            List<BulkInsertColumnMapping> keyMappings)
        {
            var safeTableName = DatabaseHelper.QuoteQualifiedIdentifier(providerInvariantName, destinationTableName);
            var whereClause = string.Join(" AND ", System.Linq.Enumerable.Select(keyMappings, (m, index) =>
                DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn) + " = " +
                DatabaseCommandHelper.GetParameterPlaceholder(providerInvariantName, index)));
            return $"SELECT COUNT(1) FROM {safeTableName} WHERE {whereClause}";
        }

        private static void CreateUpsertUpdateParameters(
            DbCommand command,
            string providerInvariantName,
            List<BulkInsertColumnMapping> updateMappings,
            List<BulkInsertColumnMapping> keyMappings)
        {
            command.Parameters.Clear();
            for (int i = 0; i < updateMappings.Count + keyMappings.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = DatabaseCommandHelper.GetParameterName(providerInvariantName, i);
                command.Parameters.Add(parameter);
            }
        }

        private static void CreateUpsertExistsParameters(
            DbCommand command,
            string providerInvariantName,
            List<BulkInsertColumnMapping> keyMappings)
        {
            command.Parameters.Clear();
            for (int i = 0; i < keyMappings.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = DatabaseCommandHelper.GetParameterName(providerInvariantName, i);
                command.Parameters.Add(parameter);
            }
        }

        private static void AssignUpsertUpdateParameterValues(
            DbCommand command,
            DataRow row,
            List<BulkInsertColumnMapping> updateMappings,
            List<BulkInsertColumnMapping> keyMappings)
        {
            var index = 0;
            foreach (var updateMapping in updateMappings)
                command.Parameters[index++].Value = row[updateMapping.SourceColumn] ?? DBNull.Value;
            foreach (var keyMapping in keyMappings)
                command.Parameters[index++].Value = row[keyMapping.SourceColumn] ?? DBNull.Value;
        }

        private static void AssignUpsertExistsParameterValues(
            DbCommand command,
            DataRow row,
            List<BulkInsertColumnMapping> keyMappings)
        {
            for (int i = 0; i < keyMappings.Count; i++)
                command.Parameters[i].Value = row[keyMappings[i].SourceColumn] ?? DBNull.Value;
        }
    }
}
