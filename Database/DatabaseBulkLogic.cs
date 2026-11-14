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
        private readonly IDatabaseExecutor _executor;

        public DatabaseBulkLogic() : this(new DatabaseExecutor())
        {
        }

        /// <summary>Тестовый шов (issue #10): подмена исполнителя БД моком.</summary>
        public DatabaseBulkLogic(IDatabaseExecutor executor)
        {
            _executor = executor ?? new DatabaseExecutor();
        }

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
                ? _executor.ExecuteBulkInsert(transactionHandle, dataTable, destinationTable, batchSize, timeoutSeconds, useTableLock, keepIdentity, preloadMode, columnMappings)
                : _executor.ExecuteBulkInsert(provider, connectionString, dataTable, destinationTable, batchSize, timeoutSeconds, useTableLock, keepIdentity, preloadMode, columnMappings);

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
            int timeoutSeconds,
            int batchSize = 0)
        {
            var transactionHandle = DatabaseTransactionManager.Get(transactionId);
            var result = transactionHandle != null
                ? _executor.ExecuteUpsert(transactionHandle, dataTable, destinationTable, keyColumns, updateColumns, columnMappings, timeoutSeconds, batchSize)
                : _executor.ExecuteUpsert(provider, connectionString, dataTable, destinationTable, keyColumns, updateColumns, columnMappings, timeoutSeconds, batchSize);

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
