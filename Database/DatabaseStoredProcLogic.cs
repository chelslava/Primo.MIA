using System.Collections.Generic;
using System.Data;

namespace Primo.MIA
{
    public class DatabaseStoredProcResult
    {
        public DataTable ResultTable { get; set; }
        public int RowCount { get; set; }
        public Dictionary<string, object> OutputParams { get; set; }
    }

    public class DatabaseStoredProcLogic
    {
        public DatabaseStoredProcResult Execute(
            string provider,
            string connectionString,
            string transactionId,
            string procedureName,
            int timeoutSeconds,
            Dictionary<string, string> inputParameters,
            Dictionary<string, string> inputOutputParameters,
            List<string> outputParameterNames,
            int outputParameterSize,
            bool includeReturnValue)
        {
            var transactionHandle = DatabaseTransactionManager.Get(transactionId);
            var result = transactionHandle != null
                ? DatabaseHelper.ExecuteStoredProcedure(transactionHandle, procedureName, timeoutSeconds, inputParameters, inputOutputParameters, outputParameterNames, outputParameterSize, includeReturnValue)
                : DatabaseHelper.ExecuteStoredProcedure(provider, connectionString, procedureName, timeoutSeconds, inputParameters, inputOutputParameters, outputParameterNames, outputParameterSize, includeReturnValue);

            var outputParams = new Dictionary<string, object>();
            if (result.OutputParameters != null)
            {
                foreach (var kvp in result.OutputParameters)
                    outputParams[kvp.Key] = kvp.Value;
            }
            if (!string.IsNullOrEmpty(result.ReturnValue))
                outputParams["__ReturnValue"] = result.ReturnValue;

            return new DatabaseStoredProcResult
            {
                ResultTable = null,
                RowCount = result.AffectedRows,
                OutputParams = outputParams
            };
        }
    }
}
