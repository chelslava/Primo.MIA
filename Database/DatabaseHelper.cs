using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    /// <summary>
    /// Вспомогательные методы для ADO.NET активностей.
    /// Работают через DbProviderFactory и подходят для любых
    /// зарегистрированных провайдеров с invariant name.
    /// </summary>
    public static class DatabaseHelper
    {
        public const string DefaultProviderInvariantName = "System.Data.SqlClient";

        public static DbProviderFactory GetFactory(string providerInvariantName)
        {
            var provider = string.IsNullOrWhiteSpace(providerInvariantName)
                ? DefaultProviderInvariantName
                : providerInvariantName.Trim();

            try
            {
                return DbProviderFactories.GetFactory(provider);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Не удалось получить DbProviderFactory для '{provider}'. " +
                    "Проверьте invariant name и установлен ли ADO.NET провайдер.",
                    ex);
            }
        }

        public static DbConnection OpenConnection(string providerInvariantName, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Строка подключения не может быть пустой.", nameof(connectionString));

            var factory = GetFactory(providerInvariantName);
            var connection = factory.CreateConnection();
            if (connection == null)
                throw new InvalidOperationException("Провайдер не смог создать объект подключения.");

            connection.ConnectionString = connectionString;
            connection.Open();
            return connection;
        }

        public static DbCommand CreateCommand(
            DbConnection connection,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters = null,
            DbTransaction transaction = null)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(commandText))
                throw new ArgumentException("Текст команды не может быть пустым.", nameof(commandText));

            var command = connection.CreateCommand();
            command.CommandText = commandText;
            command.CommandType = commandType == DatabaseCommandType.StoredProcedure
                ? CommandType.StoredProcedure
                : CommandType.Text;
            command.CommandTimeout = commandTimeoutSeconds > 0 ? commandTimeoutSeconds : 30;
            command.Transaction = transaction;

            AddParameters(command, parameters);
            return command;
        }

        public static void AddParameters(DbCommand command, Dictionary<string, string> parameters)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (parameters == null || parameters.Count == 0)
                return;

            foreach (var pair in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = NormalizeParameterName(pair.Key);
                parameter.Value = string.IsNullOrEmpty(pair.Value)
                    ? (object)DBNull.Value
                    : pair.Value;
                command.Parameters.Add(parameter);
            }
        }

        public static string NormalizeParameterName(string parameterName)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
                throw new ArgumentException("Имя параметра не может быть пустым.", nameof(parameterName));

            var trimmed = parameterName.Trim();
            if (trimmed.StartsWith("@", StringComparison.Ordinal) ||
                trimmed.StartsWith(":", StringComparison.Ordinal) ||
                trimmed.StartsWith("?", StringComparison.Ordinal))
            {
                return trimmed;
            }

            return "@" + trimmed;
        }

        public static DataTable ExecuteQuery(
            string providerInvariantName,
            string connectionString,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters = null)
        {
            using (var connection = OpenConnection(providerInvariantName, connectionString))
            using (var command = CreateCommand(connection, commandText, commandType, commandTimeoutSeconds, parameters))
            using (var adapter = GetFactory(providerInvariantName).CreateDataAdapter())
            {
                if (adapter == null)
                    throw new InvalidOperationException("Провайдер не смог создать DataAdapter.");

                adapter.SelectCommand = command;
                var table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }

        public static DataTable ExecuteQuery(
            DatabaseTransactionHandle transactionHandle,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters = null)
        {
            EnsureTransactionHandle(transactionHandle);

            using (var command = CreateCommand(
                transactionHandle.Connection,
                commandText,
                commandType,
                commandTimeoutSeconds,
                parameters,
                transactionHandle.Transaction))
            using (var adapter = GetFactory(transactionHandle.ProviderInvariantName).CreateDataAdapter())
            {
                if (adapter == null)
                    throw new InvalidOperationException("Провайдер не смог создать DataAdapter.");

                adapter.SelectCommand = command;
                var table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }

        public static object ExecuteScalar(
            string providerInvariantName,
            string connectionString,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters = null)
        {
            using (var connection = OpenConnection(providerInvariantName, connectionString))
            using (var command = CreateCommand(connection, commandText, commandType, commandTimeoutSeconds, parameters))
            {
                return command.ExecuteScalar();
            }
        }

        public static object ExecuteScalar(
            DatabaseTransactionHandle transactionHandle,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters = null)
        {
            EnsureTransactionHandle(transactionHandle);

            using (var command = CreateCommand(
                transactionHandle.Connection,
                commandText,
                commandType,
                commandTimeoutSeconds,
                parameters,
                transactionHandle.Transaction))
            {
                return command.ExecuteScalar();
            }
        }

        public static int ExecuteNonQuery(
            string providerInvariantName,
            string connectionString,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters = null)
        {
            using (var connection = OpenConnection(providerInvariantName, connectionString))
            using (var command = CreateCommand(connection, commandText, commandType, commandTimeoutSeconds, parameters))
            {
                return command.ExecuteNonQuery();
            }
        }

        public static NonQueryExecutionResult ExecuteNonQuery(
            string providerInvariantName,
            string connectionString,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            bool splitByGoBatches,
            Dictionary<string, string> parameters = null)
        {
            if (splitByGoBatches && commandType == DatabaseCommandType.Text)
            {
                return ExecuteNonQueryBatches(providerInvariantName, connectionString, commandText, commandTimeoutSeconds, parameters);
            }

            return new NonQueryExecutionResult
            {
                AffectedRows = ExecuteNonQuery(providerInvariantName, connectionString, commandText, commandType, commandTimeoutSeconds, parameters),
                BatchCount = 1
            };
        }

        public static int ExecuteNonQuery(
            DatabaseTransactionHandle transactionHandle,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters = null)
        {
            EnsureTransactionHandle(transactionHandle);

            using (var command = CreateCommand(
                transactionHandle.Connection,
                commandText,
                commandType,
                commandTimeoutSeconds,
                parameters,
                transactionHandle.Transaction))
            {
                return command.ExecuteNonQuery();
            }
        }

        public static NonQueryExecutionResult ExecuteNonQuery(
            DatabaseTransactionHandle transactionHandle,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            bool splitByGoBatches,
            Dictionary<string, string> parameters = null)
        {
            if (splitByGoBatches && commandType == DatabaseCommandType.Text)
            {
                return ExecuteNonQueryBatches(transactionHandle, commandText, commandTimeoutSeconds, parameters);
            }

            return new NonQueryExecutionResult
            {
                AffectedRows = ExecuteNonQuery(transactionHandle, commandText, commandType, commandTimeoutSeconds, parameters),
                BatchCount = 1
            };
        }

        public static StoredProcedureExecutionResult ExecuteStoredProcedure(
            string providerInvariantName,
            string connectionString,
            string procedureName,
            int commandTimeoutSeconds,
            Dictionary<string, string> inputParameters = null,
            List<string> outputParameterNames = null,
            bool includeReturnValue = false)
        {
            using (var connection = OpenConnection(providerInvariantName, connectionString))
            using (var command = CreateStoredProcedureCommand(
                connection,
                providerInvariantName,
                procedureName,
                commandTimeoutSeconds,
                inputParameters,
                outputParameterNames,
                includeReturnValue))
            {
                return ExecuteStoredProcedureCommand(command, providerInvariantName);
            }
        }

        public static StoredProcedureExecutionResult ExecuteStoredProcedure(
            DatabaseTransactionHandle transactionHandle,
            string procedureName,
            int commandTimeoutSeconds,
            Dictionary<string, string> inputParameters = null,
            List<string> outputParameterNames = null,
            bool includeReturnValue = false)
        {
            EnsureTransactionHandle(transactionHandle);

            using (var command = CreateStoredProcedureCommand(
                transactionHandle.Connection,
                transactionHandle.ProviderInvariantName,
                procedureName,
                commandTimeoutSeconds,
                inputParameters,
                outputParameterNames,
                includeReturnValue,
                transactionHandle.Transaction))
            {
                return ExecuteStoredProcedureCommand(command, transactionHandle.ProviderInvariantName);
            }
        }

        public static string ConvertScalarToString(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            if (value is DateTime dt)
                return dt.ToString("O", CultureInfo.InvariantCulture);

            if (value is DateTimeOffset dto)
                return dto.ToString("O", CultureInfo.InvariantCulture);

            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);

            return value.ToString();
        }

        public static int? ConvertScalarToInt32(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            if (value is int intValue)
                return intValue;

            if (value is IConvertible)
            {
                try
                {
                    return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                }
                catch
                {
                }
            }

            int parsed;
            return int.TryParse(ConvertScalarToString(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : (int?)null;
        }

        public static decimal? ConvertScalarToDecimal(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            if (value is decimal decimalValue)
                return decimalValue;

            if (value is IConvertible)
            {
                try
                {
                    return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                }
                catch
                {
                }
            }

            decimal parsed;
            return decimal.TryParse(ConvertScalarToString(value), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : (decimal?)null;
        }

        public static bool? ConvertScalarToBoolean(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            if (value is bool boolValue)
                return boolValue;

            if (value is IConvertible)
            {
                try
                {
                    return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                }
                catch
                {
                }
            }

            var text = ConvertScalarToString(value);
            if (string.IsNullOrWhiteSpace(text))
                return null;

            bool parsedBool;
            if (bool.TryParse(text, out parsedBool))
                return parsedBool;

            if (string.Equals(text, "1", StringComparison.Ordinal))
                return true;
            if (string.Equals(text, "0", StringComparison.Ordinal))
                return false;

            return null;
        }

        public static DateTime? ConvertScalarToDateTime(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            if (value is DateTime dateTimeValue)
                return dateTimeValue;

            if (value is DateTimeOffset dateTimeOffsetValue)
                return dateTimeOffsetValue.UtcDateTime;

            if (value is IConvertible)
            {
                try
                {
                    return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                }
                catch
                {
                }
            }

            DateTime parsed;
            return DateTime.TryParse(
                ConvertScalarToString(value),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                out parsed)
                ? parsed
                : (DateTime?)null;
        }

        public static DataTable GetSchema(
            string providerInvariantName,
            string connectionString,
            string collectionName)
        {
            using (var connection = OpenConnection(providerInvariantName, connectionString))
            {
                return connection.GetSchema(collectionName);
            }
        }

        public static DataTable GetTablesSchema(
            string providerInvariantName,
            string connectionString,
            string schemaName = null,
            bool includeViews = false)
        {
            var schema = GetSchema(providerInvariantName, connectionString, "Tables");
            var filtered = schema.Clone();

            foreach (DataRow row in schema.Rows)
            {
                var tableSchema = GetSchemaValue(row, "TABLE_SCHEMA");
                var tableName = GetSchemaValue(row, "TABLE_NAME");
                var tableType = GetSchemaValue(row, "TABLE_TYPE");

                if (string.IsNullOrWhiteSpace(tableName))
                    continue;
                if (!string.IsNullOrWhiteSpace(schemaName) &&
                    !string.Equals(tableSchema, schemaName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!includeViews &&
                    !string.IsNullOrWhiteSpace(tableType) &&
                    tableType.IndexOf("VIEW", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                filtered.ImportRow(row);
            }

            return filtered;
        }

        public static bool TableExists(
            string providerInvariantName,
            string connectionString,
            string tableName,
            string schemaName = null,
            bool includeViews = false)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Имя таблицы не может быть пустым.", nameof(tableName));

            var schema = GetTablesSchema(providerInvariantName, connectionString, schemaName, includeViews);
            return schema.Rows.Cast<DataRow>()
                .Any(row => string.Equals(GetSchemaValue(row, "TABLE_NAME"), tableName, StringComparison.OrdinalIgnoreCase));
        }

        public static List<string> GetTableNames(
            string providerInvariantName,
            string connectionString,
            string schemaName = null,
            bool includeViews = false)
        {
            var schema = GetTablesSchema(providerInvariantName, connectionString, schemaName, includeViews);
            return schema.Rows.Cast<DataRow>()
                .Select(row =>
                {
                    var tableSchema = GetSchemaValue(row, "TABLE_SCHEMA");
                    var tableName = GetSchemaValue(row, "TABLE_NAME");
                    return string.IsNullOrWhiteSpace(tableSchema) ? tableName : tableSchema + "." + tableName;
                })
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static DataTable GetColumnsSchema(
            string providerInvariantName,
            string connectionString,
            string tableName,
            string schemaName = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Имя таблицы не может быть пустым.", nameof(tableName));

            var schema = GetSchema(providerInvariantName, connectionString, "Columns");
            var filtered = schema.Clone();

            foreach (DataRow row in schema.Rows)
            {
                var rowSchema = GetSchemaValue(row, "TABLE_SCHEMA");
                var rowTable = GetSchemaValue(row, "TABLE_NAME");
                if (!string.Equals(rowTable, tableName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrWhiteSpace(schemaName) &&
                    !string.Equals(rowSchema, schemaName, StringComparison.OrdinalIgnoreCase))
                    continue;

                filtered.ImportRow(row);
            }

            return filtered;
        }

        public static List<string> GetColumnNames(
            string providerInvariantName,
            string connectionString,
            string tableName,
            string schemaName = null)
        {
            var schema = GetColumnsSchema(providerInvariantName, connectionString, tableName, schemaName);
            return schema.Rows.Cast<DataRow>()
                .Select(row => GetSchemaValue(row, "COLUMN_NAME"))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Универсальная массовая запись DataTable.
        /// Для SQL Server использует SqlBulkCopy, для остальных провайдеров —
        /// batched insert через DbProviderFactory и транзакцию.
        /// </summary>
        public static BulkInsertResult ExecuteBulkInsert(
            string providerInvariantName,
            string connectionString,
            DataTable dataTable,
            string destinationTableName,
            int batchSize,
            int bulkCopyTimeoutSeconds,
            bool useTableLock,
            bool keepIdentity,
            DatabaseBulkPreloadMode preloadMode,
            Dictionary<string, string> columnMappings = null)
        {
            var provider = string.IsNullOrWhiteSpace(providerInvariantName)
                ? DefaultProviderInvariantName
                : providerInvariantName.Trim();

            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable), "Исходная таблица данных не указана.");
            if (string.IsNullOrWhiteSpace(destinationTableName))
                throw new ArgumentException("Имя таблицы-приёмника не может быть пустым.", nameof(destinationTableName));

            if (dataTable.Columns.Count == 0)
                return new BulkInsertResult { RowsWritten = dataTable.Rows.Count, Mode = "NoColumns" };

            if (string.Equals(provider, DefaultProviderInvariantName, StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteSqlServerBulkInsert(
                    connectionString,
                    dataTable,
                    destinationTableName,
                    batchSize,
                    bulkCopyTimeoutSeconds,
                    useTableLock,
                    keepIdentity,
                    preloadMode,
                    columnMappings);
            }

            return ExecuteBatchedInsert(
                provider,
                connectionString,
                dataTable,
                destinationTableName,
                batchSize,
                bulkCopyTimeoutSeconds,
                preloadMode,
                columnMappings);
        }

        private static BulkInsertResult ExecuteSqlServerBulkInsert(
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
            if (useTableLock)
                options |= SqlBulkCopyOptions.TableLock;
            if (keepIdentity)
                options |= SqlBulkCopyOptions.KeepIdentity;

            using (var connection = new SqlConnection(connectionString))
            using (var bulkCopy = new SqlBulkCopy(connection, options, null))
            {
                connection.Open();
                ExecutePreloadCommand(connection, null, destinationTableName, preloadMode);

                bulkCopy.DestinationTableName = destinationTableName;
                bulkCopy.BatchSize = batchSize > 0 ? batchSize : 1000;
                bulkCopy.BulkCopyTimeout = bulkCopyTimeoutSeconds > 0 ? bulkCopyTimeoutSeconds : 60;

                ApplyBulkCopyMappings(bulkCopy, dataTable, columnMappings);
                bulkCopy.WriteToServer(dataTable);
            }

            return new BulkInsertResult
            {
                RowsWritten = dataTable.Rows.Count,
                Mode = BuildBulkInsertModeName("SqlBulkCopy", preloadMode)
            };
        }

        public static BulkInsertResult ExecuteBulkInsert(
            DatabaseTransactionHandle transactionHandle,
            DataTable dataTable,
            string destinationTableName,
            int batchSize,
            int bulkCopyTimeoutSeconds,
            bool useTableLock,
            bool keepIdentity,
            DatabaseBulkPreloadMode preloadMode,
            Dictionary<string, string> columnMappings = null)
        {
            EnsureTransactionHandle(transactionHandle);

            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable), "Исходная таблица данных не указана.");
            if (string.IsNullOrWhiteSpace(destinationTableName))
                throw new ArgumentException("Имя таблицы-приёмника не может быть пустым.", nameof(destinationTableName));
            if (dataTable.Columns.Count == 0)
                return new BulkInsertResult { RowsWritten = dataTable.Rows.Count, Mode = "NoColumns" };

            if (string.Equals(transactionHandle.ProviderInvariantName, DefaultProviderInvariantName, StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteSqlServerBulkInsert(transactionHandle, dataTable, destinationTableName, batchSize, bulkCopyTimeoutSeconds, useTableLock, keepIdentity, preloadMode, columnMappings);
            }

            return ExecuteBatchedInsert(transactionHandle, dataTable, destinationTableName, batchSize, bulkCopyTimeoutSeconds, preloadMode, columnMappings);
        }

        private static BulkInsertResult ExecuteSqlServerBulkInsert(
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
            EnsureTransactionHandle(transactionHandle);

            var sqlConnection = transactionHandle.Connection as SqlConnection;
            var sqlTransaction = transactionHandle.Transaction as SqlTransaction;
            if (sqlConnection == null || sqlTransaction == null)
                throw new InvalidOperationException("Для SqlBulkCopy внутри транзакции требуется SqlConnection/SqlTransaction.");

            var options = SqlBulkCopyOptions.CheckConstraints;
            if (useTableLock)
                options |= SqlBulkCopyOptions.TableLock;
            if (keepIdentity)
                options |= SqlBulkCopyOptions.KeepIdentity;

            using (var bulkCopy = new SqlBulkCopy(sqlConnection, options, sqlTransaction))
            {
                ExecutePreloadCommand(sqlConnection, sqlTransaction, destinationTableName, preloadMode);
                bulkCopy.DestinationTableName = destinationTableName;
                bulkCopy.BatchSize = batchSize > 0 ? batchSize : 1000;
                bulkCopy.BulkCopyTimeout = bulkCopyTimeoutSeconds > 0 ? bulkCopyTimeoutSeconds : 60;
                ApplyBulkCopyMappings(bulkCopy, dataTable, columnMappings);
                bulkCopy.WriteToServer(dataTable);
            }

            return new BulkInsertResult
            {
                RowsWritten = dataTable.Rows.Count,
                Mode = BuildBulkInsertModeName("SqlBulkCopy", preloadMode)
            };
        }

        private static BulkInsertResult ExecuteBatchedInsert(
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
            var effectiveBatchSize = batchSize > 0 ? batchSize : 1000;

            using (var connection = OpenConnection(providerInvariantName, connectionString))
            using (var transaction = connection.BeginTransaction())
            using (var command = connection.CreateCommand())
            {
                ExecutePreloadCommand(connection, transaction, destinationTableName, preloadMode);
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                command.CommandTimeout = commandTimeoutSeconds > 0 ? commandTimeoutSeconds : 60;
                command.CommandText = BuildInsertCommandText(providerInvariantName, destinationTableName, mappings);

                CreateInsertParameters(command, providerInvariantName, mappings);
                TryPrepareCommand(command);

                var rowsWritten = 0;

                foreach (DataRow row in dataTable.Rows)
                {
                    AssignInsertParameterValues(command, row, mappings);
                    command.ExecuteNonQuery();
                    rowsWritten++;

                    if (rowsWritten % effectiveBatchSize == 0)
                    {
                        // Граница пачки оставлена как явный маркер: при необходимости
                        // сюда можно добавить логирование/чекпоинты без перестройки API.
                    }
                }

                transaction.Commit();

                return new BulkInsertResult
                {
                    RowsWritten = rowsWritten,
                    Mode = BuildBulkInsertModeName("BatchedInsert", preloadMode)
                };
            }
        }

        private static BulkInsertResult ExecuteBatchedInsert(
            DatabaseTransactionHandle transactionHandle,
            DataTable dataTable,
            string destinationTableName,
            int batchSize,
            int commandTimeoutSeconds,
            DatabaseBulkPreloadMode preloadMode,
            Dictionary<string, string> columnMappings)
        {
            EnsureTransactionHandle(transactionHandle);

            var mappings = BuildMappings(dataTable, columnMappings);
            var effectiveBatchSize = batchSize > 0 ? batchSize : 1000;

            using (var command = transactionHandle.Connection.CreateCommand())
            {
                ExecutePreloadCommand(transactionHandle.Connection, transactionHandle.Transaction, destinationTableName, preloadMode);
                command.Transaction = transactionHandle.Transaction;
                command.CommandType = CommandType.Text;
                command.CommandTimeout = commandTimeoutSeconds > 0 ? commandTimeoutSeconds : 60;
                command.CommandText = BuildInsertCommandText(transactionHandle.ProviderInvariantName, destinationTableName, mappings);

                CreateInsertParameters(command, transactionHandle.ProviderInvariantName, mappings);
                TryPrepareCommand(command);

                var rowsWritten = 0;
                foreach (DataRow row in dataTable.Rows)
                {
                    AssignInsertParameterValues(command, row, mappings);
                    command.ExecuteNonQuery();
                    rowsWritten++;

                    if (rowsWritten % effectiveBatchSize == 0)
                    {
                    }
                }

                return new BulkInsertResult
                {
                    RowsWritten = rowsWritten,
                    Mode = BuildBulkInsertModeName("BatchedInsert", preloadMode)
                };
            }
        }

        public static string BuildPreloadCommandText(string destinationTableName, DatabaseBulkPreloadMode preloadMode)
        {
            if (string.IsNullOrWhiteSpace(destinationTableName))
                throw new ArgumentException("Имя таблицы-приёмника не может быть пустым.", nameof(destinationTableName));

            return BuildPreloadCommandText(DefaultProviderInvariantName, destinationTableName, preloadMode);
        }

        public static string BuildPreloadCommandText(
            string providerInvariantName,
            string destinationTableName,
            DatabaseBulkPreloadMode preloadMode)
        {
            if (string.IsNullOrWhiteSpace(destinationTableName))
                throw new ArgumentException("Имя таблицы-приёмника не может быть пустым.", nameof(destinationTableName));

            var safeTableName = QuoteQualifiedIdentifier(providerInvariantName, destinationTableName);

            switch (preloadMode)
            {
                case DatabaseBulkPreloadMode.DeleteAll:
                    return $"DELETE FROM {safeTableName}";
                case DatabaseBulkPreloadMode.Truncate:
                    return $"TRUNCATE TABLE {safeTableName}";
                default:
                    return null;
            }
        }

        public static List<string> SplitSqlBatches(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                return new List<string>();

            var batches = Regex.Split(
                    commandText,
                    @"^\s*GO(?:\s+\d+)?\s*(?:--.*)?$",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline)
                .Select(batch => batch?.Trim())
                .Where(batch => !string.IsNullOrWhiteSpace(batch))
                .ToList();

            return batches;
        }

        /// <summary>
        /// Настраивает маппинг колонок для bulk copy.
        /// Если словарь не указан, колонки маппятся по одинаковым именам.
        /// </summary>
        public static void ApplyBulkCopyMappings(
            SqlBulkCopy bulkCopy,
            DataTable dataTable,
            Dictionary<string, string> columnMappings = null)
        {
            if (bulkCopy == null)
                throw new ArgumentNullException(nameof(bulkCopy));
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            bulkCopy.ColumnMappings.Clear();

            if (columnMappings != null && columnMappings.Count > 0)
            {
                foreach (var pair in columnMappings)
                {
                    EnsureColumnExists(dataTable, pair.Key);
                    bulkCopy.ColumnMappings.Add(pair.Key, pair.Value);
                }

                return;
            }

            foreach (DataColumn column in dataTable.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }
        }

        private static void EnsureColumnExists(DataTable dataTable, string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Имя колонки не может быть пустым.", nameof(columnName));

            if (!dataTable.Columns.Contains(columnName))
                throw new InvalidOperationException($"Колонка '{columnName}' отсутствует в DataTable.");
        }

        private static List<BulkInsertColumnMapping> BuildMappings(
            DataTable dataTable,
            Dictionary<string, string> columnMappings)
        {
            if (columnMappings != null && columnMappings.Count > 0)
            {
                return columnMappings
                    .Select(pair =>
                    {
                        EnsureColumnExists(dataTable, pair.Key);
                        return new BulkInsertColumnMapping
                        {
                            SourceColumn = pair.Key,
                            DestinationColumn = pair.Value
                        };
                    })
                    .ToList();
            }

            return dataTable.Columns
                .Cast<DataColumn>()
                .Select(column => new BulkInsertColumnMapping
                {
                    SourceColumn = column.ColumnName,
                    DestinationColumn = column.ColumnName
                })
                .ToList();
        }

        private static string BuildInsertCommandText(
            string providerInvariantName,
            string destinationTableName,
            List<BulkInsertColumnMapping> mappings)
        {
            var safeTableName = QuoteQualifiedIdentifier(providerInvariantName, destinationTableName);
            var columnList = string.Join(", ", mappings.Select(m => QuoteIdentifier(providerInvariantName, m.DestinationColumn)));
            var valuesList = string.Join(", ", mappings.Select((m, index) => GetParameterPlaceholder(providerInvariantName, index)));
            return $"INSERT INTO {safeTableName} ({columnList}) VALUES ({valuesList})";
        }

        private static void CreateInsertParameters(
            DbCommand command,
            string providerInvariantName,
            List<BulkInsertColumnMapping> mappings)
        {
            command.Parameters.Clear();

            for (int i = 0; i < mappings.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = GetParameterName(providerInvariantName, i);
                command.Parameters.Add(parameter);
            }
        }

        private static void AssignInsertParameterValues(
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

        private static string GetParameterPlaceholder(string providerInvariantName, int index)
        {
            if (IsPositionalProvider(providerInvariantName))
                return "?";

            return GetParameterPrefix(providerInvariantName) + "p" + index;
        }

        private static string GetParameterName(string providerInvariantName, int index)
        {
            if (IsPositionalProvider(providerInvariantName))
                return "p" + index;

            return GetParameterPrefix(providerInvariantName) + "p" + index;
        }

        private static string GetParameterPrefix(string providerInvariantName)
        {
            if (string.IsNullOrWhiteSpace(providerInvariantName))
                return "@";

            if (providerInvariantName.IndexOf("Oracle", StringComparison.OrdinalIgnoreCase) >= 0)
                return ":";

            return "@";
        }

        private static bool IsPositionalProvider(string providerInvariantName)
        {
            if (string.IsNullOrWhiteSpace(providerInvariantName))
                return false;

            return providerInvariantName.IndexOf(typeof(OdbcFactory).Namespace, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   providerInvariantName.IndexOf(typeof(OleDbFactory).Namespace, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void TryPrepareCommand(DbCommand command)
        {
            try
            {
                command.Prepare();
            }
            catch
            {
                // Не все провайдеры поддерживают Prepare — это допустимо.
            }
        }

        private static void ExecutePreloadCommand(
            DbConnection connection,
            DbTransaction transaction,
            string destinationTableName,
            DatabaseBulkPreloadMode preloadMode)
        {
            var preloadCommandText = BuildPreloadCommandText(GetProviderInvariantName(connection), destinationTableName, preloadMode);
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

        private static string BuildBulkInsertModeName(string baseMode, DatabaseBulkPreloadMode preloadMode)
        {
            return preloadMode == DatabaseBulkPreloadMode.None
                ? baseMode
                : baseMode + "+" + preloadMode;
        }

        private static string GetProviderInvariantName(DbConnection connection)
        {
            if (connection is SqlConnection)
                return DefaultProviderInvariantName;

            return connection != null ? connection.GetType().Namespace : DefaultProviderInvariantName;
        }

        public static string QuoteIdentifier(string providerInvariantName, string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("Идентификатор не может быть пустым.", nameof(identifier));

            var trimmed = identifier.Trim();
            if (IsAlreadyQuoted(trimmed))
                return trimmed;

            var commandBuilder = TryCreateCommandBuilder(providerInvariantName);
            if (commandBuilder != null)
            {
                try
                {
                    return commandBuilder.QuoteIdentifier(trimmed);
                }
                catch
                {
                }
            }

            string prefix;
            string suffix;
            GetIdentifierQuotes(providerInvariantName, out prefix, out suffix);
            return prefix + trimmed.Replace(suffix, suffix + suffix) + suffix;
        }

        public static string QuoteQualifiedIdentifier(string providerInvariantName, string qualifiedIdentifier)
        {
            if (string.IsNullOrWhiteSpace(qualifiedIdentifier))
                throw new ArgumentException("Идентификатор не может быть пустым.", nameof(qualifiedIdentifier));

            var parts = SplitQualifiedIdentifier(qualifiedIdentifier);
            return string.Join(".", parts.Select(part => QuoteIdentifier(providerInvariantName, part)));
        }

        public static string NormalizeOutputParameterName(string providerInvariantName, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
                throw new ArgumentException("Имя параметра не может быть пустым.", nameof(parameterName));

            var trimmed = parameterName.Trim();
            if (trimmed.StartsWith("@", StringComparison.Ordinal) ||
                trimmed.StartsWith(":", StringComparison.Ordinal) ||
                trimmed.StartsWith("?", StringComparison.Ordinal))
            {
                return trimmed;
            }

            return GetParameterPrefix(providerInvariantName) + trimmed;
        }

        private static List<string> SplitQualifiedIdentifier(string qualifiedIdentifier)
        {
            var parts = new List<string>();
            var current = string.Empty;
            var squareDepth = 0;
            var doubleQuoteDepth = 0;
            var backtickDepth = 0;

            foreach (var ch in qualifiedIdentifier.Trim())
            {
                if (ch == '[')
                    squareDepth++;
                else if (ch == ']' && squareDepth > 0)
                    squareDepth--;
                else if (ch == '"')
                    doubleQuoteDepth = doubleQuoteDepth == 0 ? 1 : 0;
                else if (ch == '`')
                    backtickDepth = backtickDepth == 0 ? 1 : 0;

                if (ch == '.' && squareDepth == 0 && doubleQuoteDepth == 0 && backtickDepth == 0)
                {
                    if (!string.IsNullOrWhiteSpace(current))
                        parts.Add(current.Trim());
                    current = string.Empty;
                    continue;
                }

                current += ch;
            }

            if (!string.IsNullOrWhiteSpace(current))
                parts.Add(current.Trim());

            return parts;
        }

        private static bool IsAlreadyQuoted(string identifier)
        {
            return (identifier.StartsWith("[", StringComparison.Ordinal) && identifier.EndsWith("]", StringComparison.Ordinal)) ||
                   (identifier.StartsWith("\"", StringComparison.Ordinal) && identifier.EndsWith("\"", StringComparison.Ordinal)) ||
                   (identifier.StartsWith("`", StringComparison.Ordinal) && identifier.EndsWith("`", StringComparison.Ordinal));
        }

        private static DbCommandBuilder TryCreateCommandBuilder(string providerInvariantName)
        {
            try
            {
                return GetFactory(providerInvariantName).CreateCommandBuilder();
            }
            catch
            {
                return null;
            }
        }

        private static void GetIdentifierQuotes(string providerInvariantName, out string prefix, out string suffix)
        {
            prefix = "[";
            suffix = "]";

            if (string.IsNullOrWhiteSpace(providerInvariantName))
                return;

            if (providerInvariantName.IndexOf("MySql", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                prefix = "`";
                suffix = "`";
                return;
            }

            if (providerInvariantName.IndexOf("Oracle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                providerInvariantName.IndexOf("Npgsql", StringComparison.OrdinalIgnoreCase) >= 0 ||
                providerInvariantName.IndexOf("SQLite", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                prefix = "\"";
                suffix = "\"";
            }
        }

        private static DbCommand CreateStoredProcedureCommand(
            DbConnection connection,
            string providerInvariantName,
            string procedureName,
            int commandTimeoutSeconds,
            Dictionary<string, string> inputParameters,
            List<string> outputParameterNames,
            bool includeReturnValue,
            DbTransaction transaction = null)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
                throw new ArgumentException("Имя stored procedure не может быть пустым.", nameof(procedureName));

            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = commandTimeoutSeconds > 0 ? commandTimeoutSeconds : 30;

            AddParameters(command, inputParameters);
            AddOutputParameters(command, providerInvariantName, outputParameterNames, includeReturnValue);
            return command;
        }

        private static void AddOutputParameters(
            DbCommand command,
            string providerInvariantName,
            List<string> outputParameterNames,
            bool includeReturnValue)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (outputParameterNames != null)
            {
                foreach (var outputParameterName in outputParameterNames
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = NormalizeOutputParameterName(providerInvariantName, outputParameterName);
                    parameter.Direction = ParameterDirection.Output;
                    parameter.Size = 4000;
                    command.Parameters.Add(parameter);
                }
            }

            if (includeReturnValue)
            {
                var returnParameter = command.CreateParameter();
                returnParameter.ParameterName = NormalizeOutputParameterName(providerInvariantName, "RETURN_VALUE");
                returnParameter.Direction = ParameterDirection.ReturnValue;
                command.Parameters.Add(returnParameter);
            }
        }

        private static StoredProcedureExecutionResult ExecuteStoredProcedureCommand(
            DbCommand command,
            string providerInvariantName)
        {
            var affectedRows = command.ExecuteNonQuery();
            var outputValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string returnValue = null;

            foreach (DbParameter parameter in command.Parameters)
            {
                if (parameter.Direction == ParameterDirection.Output || parameter.Direction == ParameterDirection.InputOutput)
                {
                    outputValues[parameter.ParameterName] = ConvertScalarToString(parameter.Value);
                }
                else if (parameter.Direction == ParameterDirection.ReturnValue)
                {
                    returnValue = ConvertScalarToString(parameter.Value);
                }
            }

            return new StoredProcedureExecutionResult
            {
                AffectedRows = affectedRows,
                OutputParameters = outputValues,
                ReturnValue = returnValue
            };
        }

        private static void EnsureTransactionHandle(DatabaseTransactionHandle transactionHandle)
        {
            if (transactionHandle == null)
                throw new InvalidOperationException("Транзакция БД не найдена.");
            if (transactionHandle.Connection == null || transactionHandle.Transaction == null)
                throw new InvalidOperationException("Транзакция БД не инициализирована корректно.");
        }

        private static NonQueryExecutionResult ExecuteNonQueryBatches(
            string providerInvariantName,
            string connectionString,
            string commandText,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters)
        {
            var batches = SplitSqlBatches(commandText);
            using (var connection = OpenConnection(providerInvariantName, connectionString))
            using (var transaction = connection.BeginTransaction())
            {
                var result = ExecuteNonQueryBatchesCore(connection, transaction, batches, commandTimeoutSeconds, parameters);
                transaction.Commit();
                return result;
            }
        }

        private static NonQueryExecutionResult ExecuteNonQueryBatches(
            DatabaseTransactionHandle transactionHandle,
            string commandText,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters)
        {
            EnsureTransactionHandle(transactionHandle);
            var batches = SplitSqlBatches(commandText);
            return ExecuteNonQueryBatchesCore(
                transactionHandle.Connection,
                transactionHandle.Transaction,
                batches,
                commandTimeoutSeconds,
                parameters);
        }

        private static NonQueryExecutionResult ExecuteNonQueryBatchesCore(
            DbConnection connection,
            DbTransaction transaction,
            List<string> batches,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters)
        {
            if (batches == null || batches.Count == 0)
            {
                return new NonQueryExecutionResult
                {
                    AffectedRows = 0,
                    BatchCount = 0
                };
            }

            var totalAffectedRows = 0;

            foreach (var batch in batches)
            {
                using (var command = CreateCommand(connection, batch, DatabaseCommandType.Text, commandTimeoutSeconds, parameters, transaction))
                {
                    totalAffectedRows += command.ExecuteNonQuery();
                }
            }

            return new NonQueryExecutionResult
            {
                AffectedRows = totalAffectedRows,
                BatchCount = batches.Count
            };
        }

        private static string GetSchemaValue(DataRow row, string columnName)
        {
            if (row == null || row.Table == null)
                return null;

            if (row.Table.Columns.Contains(columnName))
                return row[columnName]?.ToString();

            var matchingColumn = row.Table.Columns
                .Cast<DataColumn>()
                .FirstOrDefault(col => string.Equals(col.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));

            return matchingColumn != null ? row[matchingColumn]?.ToString() : null;
        }
    }

    /// <summary>Результат массовой записи в БД.</summary>
    public class BulkInsertResult
    {
        public int RowsWritten { get; set; }

        public string Mode { get; set; }
    }

    public class NonQueryExecutionResult
    {
        public int AffectedRows { get; set; }

        public int BatchCount { get; set; }
    }

    public class StoredProcedureExecutionResult
    {
        public int AffectedRows { get; set; }

        public Dictionary<string, string> OutputParameters { get; set; }

        public string ReturnValue { get; set; }
    }

    internal class BulkInsertColumnMapping
    {
        public string SourceColumn { get; set; }

        public string DestinationColumn { get; set; }
    }
}
