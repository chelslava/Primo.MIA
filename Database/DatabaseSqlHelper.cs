// =============================================================================
// DatabaseSqlHelper.cs — SQL-идентификаторы и генерация команд DDL/предзагрузки.
//
// Вынесено из DatabaseHelper (issue #38, разбиение монолита): экранирование
// идентификаторов по провайдеру ([...] / "..." / `...`), защита от SQL-инъекций
// через уже закавыченные идентификаторы, разбор qualified-имён, команды
// предзагрузки таблицы (DELETE/TRUNCATE), маппинг SqlBulkCopy.
// Public API сохранено: DatabaseHelper делегирует в эти методы.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Primo.MIA
{
    internal static class DatabaseSqlHelper
    {
        public static string QuoteIdentifier(string providerInvariantName, string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("Идентификатор не может быть пустым.", nameof(identifier));

            var trimmed = identifier.Trim();
            if (IsAlreadyQuoted(trimmed))
            {
                // Уже закавыченный идентификатор возвращается как есть —
                // проверяем содержимое на разделители SQL-операторов (инъекция)
                ValidateQuotedIdentifier(trimmed, nameof(identifier));
                return trimmed;
            }

            var commandBuilder = TryCreateCommandBuilder(providerInvariantName);
            if (commandBuilder != null)
            {
                try
                {
                    return commandBuilder.QuoteIdentifier(trimmed);
                }
                catch
                {
                    // Билдер недоступен/не знает провайдера — ручное экранирование ниже
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

            return BuildPreloadCommandText(DatabaseHelper.DefaultProviderInvariantName, destinationTableName, preloadMode);
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

        /// <summary>
        /// Защита от SQL-инъекций через уже закавыченные идентификаторы:
        /// такой идентификатор возвращается в SQL как есть, без повторного
        /// экранирования, поэтому проверяем отсутствие разделителей операторов.
        /// </summary>
        private static void ValidateQuotedIdentifier(string quotedIdentifier, string paramName)
        {
            // Снимаем внешние кавычки и разворачиваем легитимное удвоение
            string inner = quotedIdentifier.Substring(1, quotedIdentifier.Length - 2)
                .Replace("\"\"", "\"")
                .Replace("]]", "]")
                .Replace("``", "`");

            if (inner.IndexOf(';') >= 0 ||
                inner.IndexOf("--", StringComparison.Ordinal) >= 0 ||
                inner.IndexOf("/*", StringComparison.Ordinal) >= 0)
            {
                throw new ArgumentException(
                    $"Идентификатор содержит недопустимую последовательность SQL (';', '--', '/*'): {quotedIdentifier}",
                    paramName);
            }
        }

        private static DbCommandBuilder TryCreateCommandBuilder(string providerInvariantName)
        {
            try
            {
                return DatabaseConnectionHelper.GetFactory(providerInvariantName).CreateCommandBuilder();
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
}
