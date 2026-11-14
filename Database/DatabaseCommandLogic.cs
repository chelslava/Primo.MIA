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
        private readonly IDatabaseExecutor _executor;

        public DatabaseCommandLogic() : this(new DatabaseExecutor())
        {
        }

        /// <summary>Тестовый шов (issue #10): подмена исполнителя БД моком.</summary>
        public DatabaseCommandLogic(IDatabaseExecutor executor)
        {
            _executor = executor ?? new DatabaseExecutor();
        }

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
                ? _executor.ExecuteNonQuery(transactionHandle, commandText, commandType, timeoutSeconds, splitByGoBatches, returnIdentity, parameters)
                : _executor.ExecuteNonQuery(provider, connectionString, commandText, commandType, timeoutSeconds, splitByGoBatches, returnIdentity, parameters);

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
                ? _executor.ExecuteScalar(transactionHandle, commandText, commandType, timeoutSeconds, parameters)
                : _executor.ExecuteScalar(provider, connectionString, commandText, commandType, timeoutSeconds, parameters);

            return new DatabaseCommandResult
            {
                RowsAffected = 0,
                ScalarValue = rawValue,
                Success = true
            };
        }
    }
}
