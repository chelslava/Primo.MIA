using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Primo.MIA
{
    public class DatabaseQueryResult
    {
        public DataTable Table { get; set; }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public List<string> ColumnNames { get; set; }
        public bool HasRows { get; set; }
    }

    public class DatabaseQueryLogic
    {
        public DatabaseQueryResult Execute(
            string provider,
            string connectionString,
            DatabaseCommandType commandType,
            string commandText,
            Dictionary<string, string> parameters,
            int timeoutSeconds,
            string transactionId)
        {
            var transactionHandle = DatabaseTransactionManager.Get(transactionId);
            var table = transactionHandle != null
                ? DatabaseHelper.ExecuteQuery(transactionHandle, commandText, commandType, timeoutSeconds, parameters)
                : DatabaseHelper.ExecuteQuery(provider, connectionString, commandText, commandType, timeoutSeconds, parameters);

            var columnNames = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            return new DatabaseQueryResult
            {
                Table = table,
                RowCount = table.Rows.Count,
                ColumnCount = table.Columns.Count,
                ColumnNames = columnNames,
                HasRows = table.Rows.Count > 0
            };
        }
    }
}
