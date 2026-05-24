using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

namespace Primo.MIA
{
    internal static class DatabaseBulkInsertHelper
    {
        internal static void EnsureColumnExists(DataTable dataTable, string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Имя колонки не может быть пустым.", nameof(columnName));
            if (!dataTable.Columns.Contains(columnName))
                throw new InvalidOperationException($"Колонка '{columnName}' отсутствует в DataTable.");
        }

        internal static List<BulkInsertColumnMapping> BuildMappings(
            DataTable dataTable,
            Dictionary<string, string> columnMappings)
        {
            if (columnMappings != null && columnMappings.Count > 0)
            {
                return System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(columnMappings, pair =>
                {
                    EnsureColumnExists(dataTable, pair.Key);
                    return new BulkInsertColumnMapping { SourceColumn = pair.Key, DestinationColumn = pair.Value };
                }));
            }

            return System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(
                System.Linq.Enumerable.Cast<DataColumn>(dataTable.Columns),
                column => new BulkInsertColumnMapping
                {
                    SourceColumn = column.ColumnName,
                    DestinationColumn = column.ColumnName
                }));
        }

        internal static string BuildInsertCommandText(
            string providerInvariantName,
            string destinationTableName,
            List<BulkInsertColumnMapping> mappings)
        {
            var safeTableName = DatabaseHelper.QuoteQualifiedIdentifier(providerInvariantName, destinationTableName);
            var columnList = string.Join(", ", System.Linq.Enumerable.Select(
                mappings, m => DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn)));
            var valuesList = string.Join(", ", System.Linq.Enumerable.Select(
                mappings, (m, index) => DatabaseCommandHelper.GetParameterPlaceholder(providerInvariantName, index)));
            return $"INSERT INTO {safeTableName} ({columnList}) VALUES ({valuesList})";
        }

        internal static void CreateInsertParameters(
            DbCommand command,
            string providerInvariantName,
            List<BulkInsertColumnMapping> mappings)
        {
            command.Parameters.Clear();
            for (int i = 0; i < mappings.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = DatabaseCommandHelper.GetParameterName(providerInvariantName, i);
                command.Parameters.Add(parameter);
            }
        }

        internal static void AssignInsertParameterValues(
            DbCommand command,
            DataRow row,
            List<BulkInsertColumnMapping> mappings)
        {
            for (int i = 0; i < mappings.Count; i++)
            {
                var value = row[mappings[i].SourceColumn];
                command.Parameters[i].Value = value ?? DBNull.Value;
            }
        }

        internal static void ExecutePreloadCommand(
            DbConnection connection,
            DbTransaction transaction,
            string destinationTableName,
            DatabaseBulkPreloadMode preloadMode)
        {
            var preloadCommandText = DatabaseHelper.BuildPreloadCommandText(
                DatabaseConnectionHelper.GetProviderInvariantName(connection),
                destinationTableName,
                preloadMode);

            if (string.IsNullOrWhiteSpace(preloadCommandText))
                return;

            using (var preloadCommand = connection.CreateCommand())
            {
                preloadCommand.Transaction = transaction;
                preloadCommand.CommandType = CommandType.Text;
                preloadCommand.CommandText = preloadCommandText;
                preloadCommand.ExecuteNonQuery();
            }
        }

        internal static string BuildBulkInsertModeName(string baseMode, DatabaseBulkPreloadMode preloadMode)
        {
            return preloadMode == DatabaseBulkPreloadMode.None
                ? baseMode
                : baseMode + "+" + preloadMode;
        }

        internal static BulkInsertResult ExecuteSqlServerBulkInsert(
            string connectionString,
            DataTable dataTable,
            string destinationTableName,
            int batchSize,
            int bulkCopyTimeoutSeconds,
            bool useTableLock,
            bool keepIdentity,
            DatabaseBulkPreloadMode preloadMode,
            Dictionary<string, string> columnMappings)
        {
            var options = SqlBulkCopyOptions.CheckConstraints;
            if (useTableLock) options |= SqlBulkCopyOptions.TableLock;
            if (keepIdentity) options |= SqlBulkCopyOptions.KeepIdentity;

            using (var connection = new SqlConnection(connectionString))
            using (var bulkCopy = new SqlBulkCopy(connection, options, null))
            {
                connection.Open();
                ExecutePreloadCommand(connection, null, destinationTableName, preloadMode);

                bulkCopy.DestinationTableName = destinationTableName;
                bulkCopy.BatchSize = batchSize > 0 ? batchSize : 1000;
                bulkCopy.BulkCopyTimeout = bulkCopyTimeoutSeconds > 0 ? bulkCopyTimeoutSeconds : 60;

                DatabaseHelper.ApplyBulkCopyMappings(bulkCopy, dataTable, columnMappings);
                bulkCopy.WriteToServer(dataTable);
            }

            return new BulkInsertResult
            {
                RowsWritten = dataTable.Rows.Count,
                Mode = BuildBulkInsertModeName("SqlBulkCopy", preloadMode)
            };
        }

        internal static BulkInsertResult ExecuteSqlServerBulkInsert(
            DatabaseTransactionHandle transactionHandle,
            DataTable dataTable,
            string destinationTableName,
            int batchSize,
            int bulkCopyTimeoutSeconds,
            bool useTableLock,
            bool keepIdentity,
            DatabaseBulkPreloadMode preloadMode,
            Dictionary<string, string> columnMappings)
        {
            DatabaseTransactionHelper.EnsureTransactionHandle(transactionHandle);

            var sqlConnection = transactionHandle.Connection as SqlConnection;
            var sqlTransaction = transactionHandle.Transaction as SqlTransaction;
            if (sqlConnection == null || sqlTransaction == null)
                throw new InvalidOperationException(
                    "Для SqlBulkCopy внутри транзакции требуется SqlConnection/SqlTransaction.");

            var options = SqlBulkCopyOptions.CheckConstraints;
            if (useTableLock) options |= SqlBulkCopyOptions.TableLock;
            if (keepIdentity) options |= SqlBulkCopyOptions.KeepIdentity;

            using (var bulkCopy = new SqlBulkCopy(sqlConnection, options, sqlTransaction))
            {
                ExecutePreloadCommand(sqlConnection, sqlTransaction, destinationTableName, preloadMode);
                bulkCopy.DestinationTableName = destinationTableName;
                bulkCopy.BatchSize = batchSize > 0 ? batchSize : 1000;
                bulkCopy.BulkCopyTimeout = bulkCopyTimeoutSeconds > 0 ? bulkCopyTimeoutSeconds : 60;
                DatabaseHelper.ApplyBulkCopyMappings(bulkCopy, dataTable, columnMappings);
                bulkCopy.WriteToServer(dataTable);
            }

            return new BulkInsertResult
            {
                RowsWritten = dataTable.Rows.Count,
                Mode = BuildBulkInsertModeName("SqlBulkCopy", preloadMode)
            };
        }

        internal static BulkInsertResult ExecuteBatchedInsert(
            string providerInvariantName,
            string connectionString,
            DataTable dataTable,
            string destinationTableName,
            int batchSize,
            int commandTimeoutSeconds,
            DatabaseBulkPreloadMode preloadMode,
            Dictionary<string, string> columnMappings)
        {
            var mappings = BuildMappings(dataTable, columnMappings);

            using (var connection = DatabaseHelper.OpenConnection(providerInvariantName, connectionString))
            using (var transaction = connection.BeginTransaction())
            using (var command = connection.CreateCommand())
            {
                ExecutePreloadCommand(connection, transaction, destinationTableName, preloadMode);
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                command.CommandTimeout = commandTimeoutSeconds > 0 ? commandTimeoutSeconds : 60;
                command.CommandText = BuildInsertCommandText(providerInvariantName, destinationTableName, mappings);

                CreateInsertParameters(command, providerInvariantName, mappings);
                DatabaseCommandHelper.TryPrepareCommand(command);

                var rowsWritten = 0;
                foreach (DataRow row in dataTable.Rows)
                {
                    AssignInsertParameterValues(command, row, mappings);
                    command.ExecuteNonQuery();
                    rowsWritten++;
                }

                transaction.Commit();
                return new BulkInsertResult
                {
                    RowsWritten = rowsWritten,
                    Mode = BuildBulkInsertModeName("BatchedInsert", preloadMode)
                };
            }
        }

        internal static BulkInsertResult ExecuteBatchedInsert(
            DatabaseTransactionHandle transactionHandle,
            DataTable dataTable,
            string destinationTableName,
            int batchSize,
            int commandTimeoutSeconds,
            DatabaseBulkPreloadMode preloadMode,
            Dictionary<string, string> columnMappings)
        {
            DatabaseTransactionHelper.EnsureTransactionHandle(transactionHandle);

            var mappings = BuildMappings(dataTable, columnMappings);

            using (var command = transactionHandle.Connection.CreateCommand())
            {
                ExecutePreloadCommand(
                    transactionHandle.Connection, transactionHandle.Transaction,
                    destinationTableName, preloadMode);

                command.Transaction = transactionHandle.Transaction;
                command.CommandType = CommandType.Text;
                command.CommandTimeout = commandTimeoutSeconds > 0 ? commandTimeoutSeconds : 60;
                command.CommandText = BuildInsertCommandText(
                    transactionHandle.ProviderInvariantName, destinationTableName, mappings);

                CreateInsertParameters(command, transactionHandle.ProviderInvariantName, mappings);
                DatabaseCommandHelper.TryPrepareCommand(command);

                var rowsWritten = 0;
                foreach (DataRow row in dataTable.Rows)
                {
                    AssignInsertParameterValues(command, row, mappings);
                    command.ExecuteNonQuery();
                    rowsWritten++;
                }

                return new BulkInsertResult
                {
                    RowsWritten = rowsWritten,
                    Mode = BuildBulkInsertModeName("BatchedInsert", preloadMode)
                };
            }
        }

        internal static BulkInsertResult ExecutePostgreSqlBulkInsert(
            string connectionString,
            DataTable dataTable,
            string destinationTableName,
            DatabaseBulkPreloadMode preloadMode,
            Dictionary<string, string> columnMappings)
        {
            var mappings = BuildMappings(dataTable, columnMappings);
            var npgsqlTypes = BuildNpgsqlTypes(dataTable, mappings);
            var copyCommand = BuildPostgresCopyCommand(destinationTableName, mappings);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                ExecutePreloadCommand(connection, null, destinationTableName, preloadMode);

                using (var importer = connection.BeginBinaryImport(copyCommand))
                {
                    foreach (DataRow row in dataTable.Rows)
                    {
                        importer.StartRow();
                        for (int i = 0; i < mappings.Count; i++)
                        {
                            var value = row[mappings[i].SourceColumn];
                            if (value == null || value == DBNull.Value)
                                importer.WriteNull();
                            else
                                WriteNpgsqlValue(importer, value, npgsqlTypes[i]);
                        }
                    }
                    importer.Complete();
                }
            }

            return new BulkInsertResult
            {
                RowsWritten = dataTable.Rows.Count,
                Mode = BuildBulkInsertModeName("NpgsqlBinaryImporter", preloadMode)
            };
        }

        internal static BulkInsertResult ExecutePostgreSqlBulkInsert(
            DatabaseTransactionHandle transactionHandle,
            DataTable dataTable,
            string destinationTableName,
            DatabaseBulkPreloadMode preloadMode,
            Dictionary<string, string> columnMappings)
        {
            DatabaseTransactionHelper.EnsureTransactionHandle(transactionHandle);

            var npgsqlConnection = transactionHandle.Connection as NpgsqlConnection;
            if (npgsqlConnection == null)
                throw new InvalidOperationException(
                    "Для NpgsqlBinaryImporter внутри транзакции требуется NpgsqlConnection.");

            var mappings = BuildMappings(dataTable, columnMappings);
            var npgsqlTypes = BuildNpgsqlTypes(dataTable, mappings);
            var copyCommand = BuildPostgresCopyCommand(destinationTableName, mappings);

            ExecutePreloadCommand(npgsqlConnection, transactionHandle.Transaction, destinationTableName, preloadMode);

            using (var importer = npgsqlConnection.BeginBinaryImport(copyCommand))
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    importer.StartRow();
                    for (int i = 0; i < mappings.Count; i++)
                    {
                        var value = row[mappings[i].SourceColumn];
                        if (value == null || value == DBNull.Value)
                            importer.WriteNull();
                        else
                            WriteNpgsqlValue(importer, value, npgsqlTypes[i]);
                    }
                }
                importer.Complete();
            }

            return new BulkInsertResult
            {
                RowsWritten = dataTable.Rows.Count,
                Mode = BuildBulkInsertModeName("NpgsqlBinaryImporter", preloadMode)
            };
        }

        private static string BuildPostgresCopyCommand(string tableName, List<BulkInsertColumnMapping> mappings)
        {
            var safeTable = QuotePostgresQualifiedIdentifier(tableName);
            var columns = string.Join(", ", mappings.Select(m => QuotePostgresQualifiedIdentifier(m.DestinationColumn)));
            return $"COPY {safeTable} ({columns}) FROM STDIN (FORMAT BINARY)";
        }

        private static string QuotePostgresQualifiedIdentifier(string identifier)
        {
            return string.Join(".", identifier.Split('.').Select(p => "\"" + p.Replace("\"", "\"\"") + "\""));
        }

        private static NpgsqlDbType[] BuildNpgsqlTypes(DataTable dataTable, List<BulkInsertColumnMapping> mappings)
        {
            return mappings.Select(m => MapToNpgsqlDbType(dataTable.Columns[m.SourceColumn].DataType)).ToArray();
        }

        private static NpgsqlDbType MapToNpgsqlDbType(Type type)
        {
            if (type == typeof(int)     || type == typeof(int?))     return NpgsqlDbType.Integer;
            if (type == typeof(long)    || type == typeof(long?))    return NpgsqlDbType.Bigint;
            if (type == typeof(short)   || type == typeof(short?))   return NpgsqlDbType.Smallint;
            if (type == typeof(double)  || type == typeof(double?))  return NpgsqlDbType.Double;
            if (type == typeof(float)   || type == typeof(float?))   return NpgsqlDbType.Real;
            if (type == typeof(decimal) || type == typeof(decimal?)) return NpgsqlDbType.Numeric;
            if (type == typeof(bool)    || type == typeof(bool?))    return NpgsqlDbType.Boolean;
            if (type == typeof(DateTime)|| type == typeof(DateTime?))return NpgsqlDbType.Timestamp;
            if (type == typeof(Guid)    || type == typeof(Guid?))    return NpgsqlDbType.Uuid;
            if (type == typeof(byte[]))                               return NpgsqlDbType.Bytea;
            return NpgsqlDbType.Text;
        }

        private static void WriteNpgsqlValue(NpgsqlBinaryImporter importer, object value, NpgsqlDbType type)
        {
            switch (value)
            {
                case int     v: importer.Write(v, type); break;
                case long    v: importer.Write(v, type); break;
                case short   v: importer.Write(v, type); break;
                case double  v: importer.Write(v, type); break;
                case float   v: importer.Write(v, type); break;
                case decimal v: importer.Write(v, type); break;
                case bool    v: importer.Write(v, type); break;
                case DateTime v: importer.Write(v, type); break;
                case Guid    v: importer.Write(v, type); break;
                case byte[]  v: importer.Write(v, type); break;
                default: importer.Write(Convert.ToString(value), NpgsqlDbType.Text); break;
            }
        }
    }
}
