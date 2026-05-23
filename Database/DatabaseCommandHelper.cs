using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    public static class DatabaseCommandHelper
    {
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
            Guard.NotNullOrWhiteSpace(commandText, nameof(commandText));

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
            Guard.NotNullOrWhiteSpace(parameterName, nameof(parameterName));

            var trimmed = parameterName.Trim();
            if (trimmed.StartsWith("@", StringComparison.Ordinal) ||
                trimmed.StartsWith(":", StringComparison.Ordinal) ||
                trimmed.StartsWith("?", StringComparison.Ordinal))
            {
                return trimmed;
            }

            return "@" + trimmed;
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

        public static string GetParameterPrefix(string providerInvariantName)
        {
            if (string.IsNullOrWhiteSpace(providerInvariantName))
                return "@";

            if (providerInvariantName.IndexOf("Oracle", StringComparison.OrdinalIgnoreCase) >= 0)
                return ":";

            return "@";
        }

        public static bool IsPositionalProvider(string providerInvariantName)
        {
            if (string.IsNullOrWhiteSpace(providerInvariantName))
                return false;

            return providerInvariantName.IndexOf(typeof(OdbcFactory).Namespace, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   providerInvariantName.IndexOf(typeof(OleDbFactory).Namespace, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string GetParameterPlaceholder(string providerInvariantName, int index)
        {
            if (IsPositionalProvider(providerInvariantName))
                return "?";

            return GetParameterPrefix(providerInvariantName) + "p" + index;
        }

        public static string GetParameterName(string providerInvariantName, int index)
        {
            if (IsPositionalProvider(providerInvariantName))
                return "p" + index;

            return GetParameterPrefix(providerInvariantName) + "p" + index;
        }

        public static void TryPrepareCommand(DbCommand command)
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

        public static string BuildPagedQuery(
            string providerInvariantName,
            string sourceQuery,
            string orderByExpression,
            int pageNumber,
            int pageSize)
        {
            if (string.IsNullOrWhiteSpace(sourceQuery))
                throw new ArgumentException("Исходный SQL-запрос не может быть пустым.", nameof(sourceQuery));
            if (string.IsNullOrWhiteSpace(orderByExpression))
                throw new ArgumentException("Order by выражение не может быть пустым.", nameof(orderByExpression));

            var safePageNumber = pageNumber > 0 ? pageNumber : 1;
            var safePageSize = pageSize > 0 ? pageSize : 100;
            var offset = (safePageNumber - 1) * safePageSize;
            var normalizedProvider = string.IsNullOrWhiteSpace(providerInvariantName)
                ? DatabaseHelper.DefaultProviderInvariantName
                : providerInvariantName.Trim();
            var alias = GetDerivedTableAlias(normalizedProvider);

            if (normalizedProvider.IndexOf("SqlClient", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"SELECT * FROM ({sourceQuery}) {alias} ORDER BY {orderByExpression} OFFSET {offset} ROWS FETCH NEXT {safePageSize} ROWS ONLY";
            }

            if (normalizedProvider.IndexOf("Oracle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"SELECT * FROM ({sourceQuery}) {alias} ORDER BY {orderByExpression} OFFSET {offset} ROWS FETCH NEXT {safePageSize} ROWS ONLY";
            }

            return $"SELECT * FROM ({sourceQuery}) {alias} ORDER BY {orderByExpression} LIMIT {safePageSize} OFFSET {offset}";
        }

        public static string BuildCountQuery(string providerInvariantName, string sourceQuery)
        {
            if (string.IsNullOrWhiteSpace(sourceQuery))
                throw new ArgumentException("Исходный SQL-запрос не может быть пустым.", nameof(sourceQuery));

            return $"SELECT COUNT(1) FROM ({sourceQuery}) {GetDerivedTableAlias(providerInvariantName)}";
        }

        public static string GetIdentityQuery(string providerInvariantName)
        {
            if (string.IsNullOrWhiteSpace(providerInvariantName))
                return "SELECT SCOPE_IDENTITY()";

            if (providerInvariantName.IndexOf("Npgsql", StringComparison.OrdinalIgnoreCase) >= 0)
                return "SELECT LASTVAL()";

            if (providerInvariantName.IndexOf("MySql", StringComparison.OrdinalIgnoreCase) >= 0)
                return "SELECT LAST_INSERT_ID()";

            if (providerInvariantName.IndexOf("SQLite", StringComparison.OrdinalIgnoreCase) >= 0)
                return "SELECT last_insert_rowid()";

            if (providerInvariantName.IndexOf("SqlClient", StringComparison.OrdinalIgnoreCase) >= 0)
                return "SELECT SCOPE_IDENTITY()";

            return null;
        }

        public static string GetDerivedTableAlias(string providerInvariantName)
        {
            if (!string.IsNullOrWhiteSpace(providerInvariantName) &&
                providerInvariantName.IndexOf("Oracle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "src";
            }

            return "AS src";
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
    }
}
