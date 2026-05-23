using System.Collections.Generic;

namespace Primo.MIA
{
    public class DatabaseCommandResult
    {
        public int RowsAffected { get; set; }
        public object ScalarValue { get; set; }
        public bool Success { get; set; }
        public int BatchCount { get; set; }
        public string IdentityValue { get; set; }
    }

    public class DatabaseCommandLogic
    {
        public DatabaseCommandResult ExecuteNonQuery(
            string provider,
            string connectionString,
            DatabaseCommandType commandType,
            string commandText,
            Dictionary<string, string> parameters,
            int timeoutSeconds,
            string transactionId,
            bool splitByGoBatches = false,
            bool returnIdentity = false)
        {
            var transactionHandle = DatabaseTransactionManager.Get(transactionId);
            var result = transactionHandle != null
                ? DatabaseHelper.ExecuteNonQuery(transactionHandle, commandText, commandType, timeoutSeconds, splitByGoBatches, returnIdentity, parameters)
                : DatabaseHelper.ExecuteNonQuery(provider, connectionString, commandText, commandType, timeoutSeconds, splitByGoBatches, returnIdentity, parameters);

            return new DatabaseCommandResult
            {
                RowsAffected = result.AffectedRows,
                BatchCount = result.BatchCount,
                IdentityValue = result.IdentityValue,
                ScalarValue = result.IdentityValue,
                Success = true
            };
        }

        public DatabaseCommandResult ExecuteScalar(
            string provider,
            string connectionString,
            DatabaseCommandType commandType,
            string commandText,
            Dictionary<string, string> parameters,
            int timeoutSeconds,
            string transactionId)
        {
            var transactionHandle = DatabaseTransactionManager.Get(transactionId);
            var rawValue = transactionHandle != null
                ? DatabaseHelper.ExecuteScalar(transactionHandle, commandText, commandType, timeoutSeconds, parameters)
                : DatabaseHelper.ExecuteScalar(provider, connectionString, commandText, commandType, timeoutSeconds, parameters);

            return new DatabaseCommandResult
            {
                RowsAffected = 0,
                ScalarValue = rawValue,
                Success = true
            };
        }
    }
}
