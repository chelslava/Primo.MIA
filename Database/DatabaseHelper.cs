using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;

namespace Primo.MIA
{
    /// <summary>
    /// Фасад для обратной совместимости. Делегирует вызовы к специализированным хелперам.
    /// </summary>
    public static class DatabaseHelper
    {
        public const string DefaultProviderInvariantName = "System.Data.SqlClient";

        // --- Connection ---

        public static DbProviderFactory GetFactory(string providerInvariantName)
            => DatabaseConnectionHelper.GetFactory(providerInvariantName);

        public static DbConnection OpenConnection(string providerInvariantName, string connectionString)
            => DatabaseConnectionHelper.OpenConnection(providerInvariantName, connectionString);

        // --- Command ---

        public static DbCommand CreateCommand(
            DbConnection connection,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters = null,
            DbTransaction transaction = null)
            => DatabaseCommandHelper.CreateCommand(connection, commandText, commandType, commandTimeoutSeconds, parameters, transaction);

        public static void AddParameters(DbCommand command, Dictionary<string, string> parameters)
            => DatabaseCommandHelper.AddParameters(command, parameters);

        public static string NormalizeParameterName(string parameterName)
            => DatabaseCommandHelper.NormalizeParameterName(parameterName);

        public static string NormalizeOutputParameterName(string providerInvariantName, string parameterName)
            => DatabaseCommandHelper.NormalizeOutputParameterName(providerInvariantName, parameterName);

        // --- Execute ---

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

        public static PagedQueryResult ExecutePagedQuery(
            string providerInvariantName,
            string connectionString,
            string sourceQuery,
            string orderByExpression,
            int pageNumber,
            int pageSize,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters = null)
        {
            using (var connection = OpenConnection(providerInvariantName, connectionString))
            {
                return ExecutePagedQueryCore(
                    connection,
                    null,
                    providerInvariantName,
                    sourceQuery,
                    orderByExpression,
                    pageNumber,
                    pageSize,
                    commandTimeoutSeconds,
                    parameters);
            }
        }

        public static PagedQueryResult ExecutePagedQuery(
            DatabaseTransactionHandle transactionHandle,
            string sourceQuery,
            string orderByExpression,
            int pageNumber,
            int pageSize,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters = null)
        {
            EnsureTransactionHandle(transactionHandle);
            return ExecutePagedQueryCore(
                transactionHandle.Connection,
                transactionHandle.Transaction,
                transactionHandle.ProviderInvariantName,
                sourceQuery,
                orderByExpression,
                pageNumber,
                pageSize,
                commandTimeoutSeconds,
                parameters);
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
            bool returnIdentity,
            Dictionary<string, string> parameters = null)
        {
            if (splitByGoBatches && commandType == DatabaseCommandType.Text)
            {
                return ExecuteNonQueryBatches(providerInvariantName, connectionString, commandText, commandTimeoutSeconds, parameters);
            }

            if (returnIdentity)
            {
                using (var connection = OpenConnection(providerInvariantName, connectionString))
                using (var transaction = connection.BeginTransaction())
                {
                    var affectedRows = ExecuteNonQueryInternal(connection, transaction, commandText, commandType, commandTimeoutSeconds, parameters);
                    var identityValue = ExecuteIdentityQuery(connection, transaction, providerInvariantName);
                    transaction.Commit();

                    return new NonQueryExecutionResult
                    {
                        AffectedRows = affectedRows,
                        BatchCount = 1,
                        IdentityValue = ConvertScalarToString(identityValue)
                    };
                }
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
            bool returnIdentity,
            Dictionary<string, string> parameters = null)
        {
            if (splitByGoBatches && commandType == DatabaseCommandType.Text)
            {
                return ExecuteNonQueryBatches(transactionHandle, commandText, commandTimeoutSeconds, parameters);
            }

            if (returnIdentity)
            {
                EnsureTransactionHandle(transactionHandle);

                var affectedRows = ExecuteNonQueryInternal(
                    transactionHandle.Connection,
                    transactionHandle.Transaction,
                    commandText,
                    commandType,
                    commandTimeoutSeconds,
                    parameters);
                var identityValue = ExecuteIdentityQuery(
                    transactionHandle.Connection,
                    transactionHandle.Transaction,
                    transactionHandle.ProviderInvariantName);

                return new NonQueryExecutionResult
                {
                    AffectedRows = affectedRows,
                    BatchCount = 1,
                    IdentityValue = ConvertScalarToString(identityValue)
                };
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
            Dictionary<string, string> inputOutputParameters = null,
            List<string> outputParameterNames = null,
            int outputParameterSize = 4000,
            bool includeReturnValue = false)
        {
            using (var connection = OpenConnection(providerInvariantName, connectionString))
            using (var command = CreateStoredProcedureCommand(
                connection,
                providerInvariantName,
                procedureName,
                commandTimeoutSeconds,
                inputParameters,
                inputOutputParameters,
                outputParameterNames,
                outputParameterSize,
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
            Dictionary<string, string> inputOutputParameters = null,
            List<string> outputParameterNames = null,
            int outputParameterSize = 4000,
            bool includeReturnValue = false)
        {
            EnsureTransactionHandle(transactionHandle);

            using (var command = CreateStoredProcedureCommand(
                transactionHandle.Connection,
                transactionHandle.ProviderInvariantName,
                procedureName,
                commandTimeoutSeconds,
                inputParameters,
                inputOutputParameters,
                outputParameterNames,
                outputParameterSize,
                includeReturnValue,
                transactionHandle.Transaction))
            {
                return ExecuteStoredProcedureCommand(command, transactionHandle.ProviderInvariantName);
            }
        }

        // --- Scalar converters ---

        public static string ConvertScalarToString(object value)
            => DatabaseCommandHelper.ConvertScalarToString(value);

        public static int? ConvertScalarToInt32(object value)
            => DatabaseCommandHelper.ConvertScalarToInt32(value);

        public static decimal? ConvertScalarToDecimal(object value)
            => DatabaseCommandHelper.ConvertScalarToDecimal(value);

        public static bool? ConvertScalarToBoolean(object value)
            => DatabaseCommandHelper.ConvertScalarToBoolean(value);

        public static DateTime? ConvertScalarToDateTime(object value)
            => DatabaseCommandHelper.ConvertScalarToDateTime(value);

        // --- Upsert ---

        public static UpsertResult ExecuteUpsert(
            string providerInvariantName,
            string connectionString,
            DataTable dataTable,
            string destinationTableName,
            List<string> keyColumns,
            List<string> updateColumns = null,
            Dictionary<string, string> columnMappings = null,
            int commandTimeoutSeconds = 60)
        {
            using (var connection = OpenConnection(providerInvariantName, connectionString))
            using (var transaction = connection.BeginTransaction())
            {
                var result = DatabaseUpsertHelper.ExecuteUpsertCore(
                    connection,
                    transaction,
                    providerInvariantName,
                    dataTable,
                    destinationTableName,
                    keyColumns,
                    updateColumns,
                    columnMappings,
                    commandTimeoutSeconds);
                transaction.Commit();
                return result;
            }
        }

        public static UpsertResult ExecuteUpsert(
            DatabaseTransactionHandle transactionHandle,
            DataTable dataTable,
            string destinationTableName,
            List<string> keyColumns,
            List<string> updateColumns = null,
            Dictionary<string, string> columnMappings = null,
            int commandTimeoutSeconds = 60)
        {
            EnsureTransactionHandle(transactionHandle);
            return DatabaseUpsertHelper.ExecuteUpsertCore(
                transactionHandle.Connection,
                transactionHandle.Transaction,
                transactionHandle.ProviderInvariantName,
                dataTable,
                destinationTableName,
                keyColumns,
                updateColumns,
                columnMappings,
                commandTimeoutSeconds);
        }

        // --- Bulk Insert ---

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
                return DatabaseBulkInsertHelper.ExecuteSqlServerBulkInsert(
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

            if (provider.IndexOf("Npgsql", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return DatabaseBulkInsertHelper.ExecutePostgreSqlBulkInsert(
                    connectionString,
                    dataTable,
                    destinationTableName,
                    preloadMode,
                    columnMappings);
            }

            return DatabaseBulkInsertHelper.ExecuteBatchedInsert(
                provider,
                connectionString,
                dataTable,
                destinationTableName,
                batchSize,
                bulkCopyTimeoutSeconds,
                preloadMode,
                columnMappings);
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
                return DatabaseBulkInsertHelper.ExecuteSqlServerBulkInsert(transactionHandle, dataTable, destinationTableName, batchSize, bulkCopyTimeoutSeconds, useTableLock, keepIdentity, preloadMode, columnMappings);
            }

            if (transactionHandle.ProviderInvariantName.IndexOf("Npgsql", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return DatabaseBulkInsertHelper.ExecutePostgreSqlBulkInsert(transactionHandle, dataTable, destinationTableName, preloadMode, columnMappings);
            }

            return DatabaseBulkInsertHelper.ExecuteBatchedInsert(transactionHandle, dataTable, destinationTableName, batchSize, bulkCopyTimeoutSeconds, preloadMode, columnMappings);
        }

        // --- Schema ---

        public static DataTable GetSchema(string providerInvariantName, string connectionString, string collectionName)
            => DatabaseSchemaHelper.GetSchema(providerInvariantName, connectionString, collectionName);

        public static DataTable GetTablesSchema(
            string providerInvariantName,
            string connectionString,
            string schemaName = null,
            bool includeViews = false)
            => DatabaseSchemaHelper.GetTablesSchema(providerInvariantName, connectionString, schemaName, includeViews);

        public static bool TableExists(
            string providerInvariantName,
            string connectionString,
            string tableName,
            string schemaName = null,
            bool includeViews = false)
            => DatabaseSchemaHelper.TableExists(providerInvariantName, connectionString, tableName, schemaName, includeViews);

        public static List<string> GetTableNames(
            string providerInvariantName,
            string connectionString,
            string schemaName = null,
            bool includeViews = false)
            => DatabaseSchemaHelper.GetTableNames(providerInvariantName, connectionString, schemaName, includeViews);

        public static DataTable GetColumnsSchema(
            string providerInvariantName,
            string connectionString,
            string tableName,
            string schemaName = null)
            => DatabaseSchemaHelper.GetColumnsSchema(providerInvariantName, connectionString, tableName, schemaName);

        public static List<string> GetColumnNames(
            string providerInvariantName,
            string connectionString,
            string tableName,
            string schemaName = null)
            => DatabaseSchemaHelper.GetColumnNames(providerInvariantName, connectionString, tableName, schemaName);

        // --- Query builders ---

        public static string BuildPagedQuery(
            string providerInvariantName,
            string sourceQuery,
            string orderByExpression,
            int pageNumber,
            int pageSize)
            => DatabaseCommandHelper.BuildPagedQuery(providerInvariantName, sourceQuery, orderByExpression, pageNumber, pageSize);

        public static string BuildCountQuery(string providerInvariantName, string sourceQuery)
            => DatabaseCommandHelper.BuildCountQuery(providerInvariantName, sourceQuery);

        public static string GetIdentityQuery(string providerInvariantName)
            => DatabaseCommandHelper.GetIdentityQuery(providerInvariantName);

        public static List<string> SplitSqlBatches(string commandText)
            => DatabaseCommandHelper.SplitSqlBatches(commandText);

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
            return string.Join(".", System.Linq.Enumerable.Select(parts, part => QuoteIdentifier(providerInvariantName, part)));
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
                    DatabaseBulkInsertHelper.EnsureColumnExists(dataTable, pair.Key);
                    bulkCopy.ColumnMappings.Add(pair.Key, pair.Value);
                }

                return;
            }

            foreach (DataColumn column in dataTable.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }
        }

        // --- Private helpers ---

        private static void EnsureTransactionHandle(DatabaseTransactionHandle transactionHandle)
            => DatabaseTransactionHelper.EnsureTransactionHandle(transactionHandle);





        private static DbCommand CreateStoredProcedureCommand(
            DbConnection connection,
            string providerInvariantName,
            string procedureName,
            int commandTimeoutSeconds,
            Dictionary<string, string> inputParameters,
            Dictionary<string, string> inputOutputParameters,
            List<string> outputParameterNames,
            int outputParameterSize,
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
            AddOutputParameters(command, providerInvariantName, inputOutputParameters, outputParameterNames, outputParameterSize, includeReturnValue);
            return command;
        }

        private static void AddOutputParameters(
            DbCommand command,
            string providerInvariantName,
            Dictionary<string, string> inputOutputParameters,
            List<string> outputParameterNames,
            int outputParameterSize,
            bool includeReturnValue)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var parameterSize = outputParameterSize > 0 ? outputParameterSize : 4000;

            if (inputOutputParameters != null)
            {
                foreach (var inputOutputParameter in inputOutputParameters)
                {
                    if (string.IsNullOrWhiteSpace(inputOutputParameter.Key))
                        continue;

                    var parameter = command.CreateParameter();
                    parameter.ParameterName = NormalizeOutputParameterName(providerInvariantName, inputOutputParameter.Key);
                    parameter.Direction = ParameterDirection.InputOutput;
                    parameter.Size = parameterSize;
                    parameter.Value = string.IsNullOrEmpty(inputOutputParameter.Value)
                        ? (object)DBNull.Value
                        : inputOutputParameter.Value;
                    command.Parameters.Add(parameter);
                }
            }

            if (outputParameterNames != null)
            {
                foreach (var outputParameterName in System.Linq.Enumerable.Distinct(
                    System.Linq.Enumerable.Where(outputParameterNames, name => !string.IsNullOrWhiteSpace(name)),
                    StringComparer.OrdinalIgnoreCase))
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = NormalizeOutputParameterName(providerInvariantName, outputParameterName);
                    parameter.Direction = ParameterDirection.Output;
                    parameter.Size = parameterSize;
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

        private static int ExecuteNonQueryInternal(
            DbConnection connection,
            DbTransaction transaction,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters)
        {
            using (var command = CreateCommand(connection, commandText, commandType, commandTimeoutSeconds, parameters, transaction))
            {
                return command.ExecuteNonQuery();
            }
        }

        private static object ExecuteIdentityQuery(
            DbConnection connection,
            DbTransaction transaction,
            string providerInvariantName)
        {
            var identityCommandText = GetIdentityQuery(providerInvariantName);
            if (string.IsNullOrWhiteSpace(identityCommandText))
                return null;

            using (var command = CreateCommand(connection, identityCommandText, DatabaseCommandType.Text, 30, null, transaction))
            {
                return command.ExecuteScalar();
            }
        }

        private static PagedQueryResult ExecutePagedQueryCore(
            DbConnection connection,
            DbTransaction transaction,
            string providerInvariantName,
            string sourceQuery,
            string orderByExpression,
            int pageNumber,
            int pageSize,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters)
        {
            var safePageNumber = pageNumber > 0 ? pageNumber : 1;
            var safePageSize = pageSize > 0 ? pageSize : 100;
            var countQuery = BuildCountQuery(providerInvariantName, sourceQuery);
            var pageQuery = BuildPagedQuery(providerInvariantName, sourceQuery, orderByExpression, safePageNumber, safePageSize);

            int totalRows;
            using (var countCommand = CreateCommand(connection, countQuery, DatabaseCommandType.Text, commandTimeoutSeconds, parameters, transaction))
            {
                totalRows = ConvertScalarToInt32(countCommand.ExecuteScalar()) ?? 0;
            }

            using (var pageCommand = CreateCommand(connection, pageQuery, DatabaseCommandType.Text, commandTimeoutSeconds, parameters, transaction))
            using (var adapter = GetFactory(providerInvariantName).CreateDataAdapter())
            {
                if (adapter == null)
                    throw new InvalidOperationException("Провайдер не смог создать DataAdapter.");

                adapter.SelectCommand = pageCommand;
                var table = new DataTable();
                adapter.Fill(table);

                var totalPages = safePageSize > 0
                    ? (int)Math.Ceiling(totalRows / (double)safePageSize)
                    : 0;

                return new PagedQueryResult
                {
                    ResultTable = table,
                    TotalRows = totalRows,
                    PageNumber = safePageNumber,
                    PageSize = safePageSize,
                    TotalPages = totalPages,
                    HasNextPage = safePageNumber < totalPages,
                    HasPreviousPage = safePageNumber > 1
                };
            }
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

        public string IdentityValue { get; set; }
    }

    public class StoredProcedureExecutionResult
    {
        public int AffectedRows { get; set; }

        public Dictionary<string, string> OutputParameters { get; set; }

        public string ReturnValue { get; set; }
    }

    public class PagedQueryResult
    {
        public DataTable ResultTable { get; set; }

        public int TotalRows { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }

        public bool HasNextPage { get; set; }

        public bool HasPreviousPage { get; set; }
    }

    public class UpsertResult
    {
        public int InsertedCount { get; set; }

        public int UpdatedCount { get; set; }
    }

    internal class BulkInsertColumnMapping
    {
        public string SourceColumn { get; set; }

        public string DestinationColumn { get; set; }
    }

    internal class BulkInsertMappingComparer : IEqualityComparer<BulkInsertColumnMapping>
    {
        public bool Equals(BulkInsertColumnMapping x, BulkInsertColumnMapping y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x == null || y == null)
                return false;

            return string.Equals(x.SourceColumn, y.SourceColumn, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.DestinationColumn, y.DestinationColumn, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(BulkInsertColumnMapping obj)
        {
            if (obj == null)
                return 0;

            var source = obj.SourceColumn ?? string.Empty;
            var destination = obj.DestinationColumn ?? string.Empty;
            return StringComparer.OrdinalIgnoreCase.GetHashCode(source + "|" + destination);
        }
    }
}
