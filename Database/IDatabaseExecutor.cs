// =============================================================================
// IDatabaseExecutor.cs — шов для тестовости Database-логики (issue #10).
//
// DatabaseQueryLogic/DatabaseCommandLogic/DatabaseBulkLogic/
// DatabaseStoredProcLogic — тонкие обёртки, которые раньше звали static
// DatabaseHelper напрямую: unit-тест без БД был невозможен. Теперь Logic
// принимает IDatabaseExecutor в конструкторе (default — DatabaseExecutor,
// делегирующий в DatabaseHelper), а тесты подставляют мок.
// Public API DatabaseHelper не менялся (переходный период по issue #10).
// =============================================================================

using System.Collections.Generic;
using System.Data;

namespace Primo.MIA
{
    /// <summary>
    /// Операции выполнения команд БД. Реализация по умолчанию делегирует
    /// в DatabaseHelper; тесты подменяют моком.
    /// </summary>
    public interface IDatabaseExecutor
    {
        DataTable ExecuteQuery(
            string providerInvariantName,
            string connectionString,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters);

        DataTable ExecuteQuery(
            DatabaseTransactionHandle transactionHandle,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters);

        NonQueryExecutionResult ExecuteNonQuery(
            string providerInvariantName,
            string connectionString,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            bool splitByGoBatches,
            bool returnIdentity,
            Dictionary<string, string> parameters);

        NonQueryExecutionResult ExecuteNonQuery(
            DatabaseTransactionHandle transactionHandle,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            bool splitByGoBatches,
            bool returnIdentity,
            Dictionary<string, string> parameters);

        object ExecuteScalar(
            string providerInvariantName,
            string connectionString,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters);

        object ExecuteScalar(
            DatabaseTransactionHandle transactionHandle,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters);

        UpsertResult ExecuteUpsert(
            string providerInvariantName,
            string connectionString,
            DataTable dataTable,
            string destinationTableName,
            List<string> keyColumns,
            List<string> updateColumns,
            Dictionary<string, string> columnMappings,
            int commandTimeoutSeconds,
            int batchSize);

        UpsertResult ExecuteUpsert(
            DatabaseTransactionHandle transactionHandle,
            DataTable dataTable,
            string destinationTableName,
            List<string> keyColumns,
            List<string> updateColumns,
            Dictionary<string, string> columnMappings,
            int commandTimeoutSeconds,
            int batchSize);

        BulkInsertResult ExecuteBulkInsert(
            string providerInvariantName,
            string connectionString,
            DataTable dataTable,
            string destinationTableName,
            int batchSize,
            int commandTimeoutSeconds,
            bool useTableLock,
            bool keepIdentity,
            DatabaseBulkPreloadMode preloadMode,
            Dictionary<string, string> columnMappings);

        BulkInsertResult ExecuteBulkInsert(
            DatabaseTransactionHandle transactionHandle,
            DataTable dataTable,
            string destinationTableName,
            int batchSize,
            int commandTimeoutSeconds,
            bool useTableLock,
            bool keepIdentity,
            DatabaseBulkPreloadMode preloadMode,
            Dictionary<string, string> columnMappings);

        StoredProcedureExecutionResult ExecuteStoredProcedure(
            string providerInvariantName,
            string connectionString,
            string procedureName,
            int commandTimeoutSeconds,
            Dictionary<string, string> inputParameters,
            Dictionary<string, string> inputOutputParameters,
            List<string> outputParameterNames,
            int outputParameterSize,
            bool includeReturnValue);

        StoredProcedureExecutionResult ExecuteStoredProcedure(
            DatabaseTransactionHandle transactionHandle,
            string procedureName,
            int commandTimeoutSeconds,
            Dictionary<string, string> inputParameters,
            Dictionary<string, string> inputOutputParameters,
            List<string> outputParameterNames,
            int outputParameterSize,
            bool includeReturnValue);
    }

    /// <summary>Реализация по умолчанию: прямое делегирование в DatabaseHelper.</summary>
    public class DatabaseExecutor : IDatabaseExecutor
    {
        public DataTable ExecuteQuery(
            string providerInvariantName,
            string connectionString,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters)
            => DatabaseHelper.ExecuteQuery(providerInvariantName, connectionString,
                commandText, commandType, commandTimeoutSeconds, parameters);

        public DataTable ExecuteQuery(
            DatabaseTransactionHandle transactionHandle,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters)
            => DatabaseHelper.ExecuteQuery(transactionHandle,
                commandText, commandType, commandTimeoutSeconds, parameters);

        public NonQueryExecutionResult ExecuteNonQuery(
            string providerInvariantName,
            string connectionString,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            bool splitByGoBatches,
            bool returnIdentity,
            Dictionary<string, string> parameters)
            => DatabaseHelper.ExecuteNonQuery(providerInvariantName, connectionString,
                commandText, commandType, commandTimeoutSeconds, splitByGoBatches, returnIdentity, parameters);

        public NonQueryExecutionResult ExecuteNonQuery(
            DatabaseTransactionHandle transactionHandle,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            bool splitByGoBatches,
            bool returnIdentity,
            Dictionary<string, string> parameters)
            => DatabaseHelper.ExecuteNonQuery(transactionHandle,
                commandText, commandType, commandTimeoutSeconds, splitByGoBatches, returnIdentity, parameters);

        public object ExecuteScalar(
            string providerInvariantName,
            string connectionString,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters)
            => DatabaseHelper.ExecuteScalar(providerInvariantName, connectionString,
                commandText, commandType, commandTimeoutSeconds, parameters);

        public object ExecuteScalar(
            DatabaseTransactionHandle transactionHandle,
            string commandText,
            DatabaseCommandType commandType,
            int commandTimeoutSeconds,
            Dictionary<string, string> parameters)
            => DatabaseHelper.ExecuteScalar(transactionHandle,
                commandText, commandType, commandTimeoutSeconds, parameters);

        public UpsertResult ExecuteUpsert(
            string providerInvariantName,
            string connectionString,
            DataTable dataTable,
            string destinationTableName,
            List<string> keyColumns,
            List<string> updateColumns,
            Dictionary<string, string> columnMappings,
            int commandTimeoutSeconds,
            int batchSize)
            => DatabaseHelper.ExecuteUpsert(providerInvariantName, connectionString,
                dataTable, destinationTableName, keyColumns, updateColumns,
                columnMappings, commandTimeoutSeconds, batchSize);

        public UpsertResult ExecuteUpsert(
            DatabaseTransactionHandle transactionHandle,
            DataTable dataTable,
            string destinationTableName,
            List<string> keyColumns,
            List<string> updateColumns,
            Dictionary<string, string> columnMappings,
            int commandTimeoutSeconds,
            int batchSize)
            => DatabaseHelper.ExecuteUpsert(transactionHandle,
                dataTable, destinationTableName, keyColumns, updateColumns,
                columnMappings, commandTimeoutSeconds, batchSize);

        public BulkInsertResult ExecuteBulkInsert(
            string providerInvariantName,
            string connectionString,
            DataTable dataTable,
            string destinationTableName,
            int batchSize,
            int commandTimeoutSeconds,
            bool useTableLock,
            bool keepIdentity,
            DatabaseBulkPreloadMode preloadMode,
            Dictionary<string, string> columnMappings)
            => DatabaseHelper.ExecuteBulkInsert(providerInvariantName, connectionString,
                dataTable, destinationTableName, batchSize, commandTimeoutSeconds,
                useTableLock, keepIdentity, preloadMode, columnMappings);

        public BulkInsertResult ExecuteBulkInsert(
            DatabaseTransactionHandle transactionHandle,
            DataTable dataTable,
            string destinationTableName,
            int batchSize,
            int commandTimeoutSeconds,
            bool useTableLock,
            bool keepIdentity,
            DatabaseBulkPreloadMode preloadMode,
            Dictionary<string, string> columnMappings)
            => DatabaseHelper.ExecuteBulkInsert(transactionHandle,
                dataTable, destinationTableName, batchSize, commandTimeoutSeconds,
                useTableLock, keepIdentity, preloadMode, columnMappings);

        public StoredProcedureExecutionResult ExecuteStoredProcedure(
            string providerInvariantName,
            string connectionString,
            string procedureName,
            int commandTimeoutSeconds,
            Dictionary<string, string> inputParameters,
            Dictionary<string, string> inputOutputParameters,
            List<string> outputParameterNames,
            int outputParameterSize,
            bool includeReturnValue)
            => DatabaseHelper.ExecuteStoredProcedure(providerInvariantName, connectionString,
                procedureName, commandTimeoutSeconds, inputParameters, inputOutputParameters,
                outputParameterNames, outputParameterSize, includeReturnValue);

        public StoredProcedureExecutionResult ExecuteStoredProcedure(
            DatabaseTransactionHandle transactionHandle,
            string procedureName,
            int commandTimeoutSeconds,
            Dictionary<string, string> inputParameters,
            Dictionary<string, string> inputOutputParameters,
            List<string> outputParameterNames,
            int outputParameterSize,
            bool includeReturnValue)
            => DatabaseHelper.ExecuteStoredProcedure(transactionHandle,
                procedureName, commandTimeoutSeconds, inputParameters, inputOutputParameters,
                outputParameterNames, outputParameterSize, includeReturnValue);
    }
}
