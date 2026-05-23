using System.Collections.Generic;
using System.Data;

namespace Primo.MIA
{
    public class DatabaseBulkResult
    {
        public int RowsAffected { get; set; }
        public bool Success { get; set; }
        public int InsertedCount { get; set; }
        public int UpdatedCount { get; set; }
        public string WriteMode { get; set; }
    }

    public class DatabaseBulkLogic
    {
        public DatabaseBulkResult BulkInsert(
            string provider,
            string connectionString,
            string transactionId,
            DataTable dataTable,
            string destinationTable,
            Dictionary<string, string> columnMappings,
            int batchSize,
            int timeoutSeconds,
            bool useTableLock,
            bool keepIdentity,
            DatabaseBulkPreloadMode preloadMode)
        {
            var transactionHandle = DatabaseTransactionManager.Get(transactionId);
            var result = transactionHandle != null
                ? DatabaseHelper.ExecuteBulkInsert(transactionHandle, dataTable, destinationTable, batchSize, timeoutSeconds, useTableLock, keepIdentity, preloadMode, columnMappings)
                : DatabaseHelper.ExecuteBulkInsert(provider, connectionString, dataTable, destinationTable, batchSize, timeoutSeconds, useTableLock, keepIdentity, preloadMode, columnMappings);

            return new DatabaseBulkResult
            {
                RowsAffected = result.RowsWritten,
                WriteMode = result.Mode,
                Success = true
            };
        }

        public DatabaseBulkResult Upsert(
            string provider,
            string connectionString,
            string transactionId,
            DataTable dataTable,
            string destinationTable,
            List<string> keyColumns,
            List<string> updateColumns,
            Dictionary<string, string> columnMappings,
            int timeoutSeconds)
        {
            var transactionHandle = DatabaseTransactionManager.Get(transactionId);
            var result = transactionHandle != null
                ? DatabaseHelper.ExecuteUpsert(transactionHandle, dataTable, destinationTable, keyColumns, updateColumns, columnMappings, timeoutSeconds)
                : DatabaseHelper.ExecuteUpsert(provider, connectionString, dataTable, destinationTable, keyColumns, updateColumns, columnMappings, timeoutSeconds);

            return new DatabaseBulkResult
            {
                RowsAffected = result.InsertedCount + result.UpdatedCount,
                InsertedCount = result.InsertedCount,
                UpdatedCount = result.UpdatedCount,
                Success = true
            };
        }
    }
}
