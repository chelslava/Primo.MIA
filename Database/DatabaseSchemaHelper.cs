using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Primo.MIA
{
    public static class DatabaseSchemaHelper
    {
        public static DataTable GetSchema(
            string providerInvariantName,
            string connectionString,
            string collectionName)
        {
            using (var connection = DatabaseConnectionHelper.OpenConnection(providerInvariantName, connectionString))
            {
                return connection.GetSchema(collectionName);
            }
        }

        public static DataTable GetTablesSchema(
            string providerInvariantName,
            string connectionString,
            string schemaName = null,
            bool includeViews = false)
        {
            var schema = GetSchema(providerInvariantName, connectionString, "Tables");
            var filtered = schema.Clone();

            foreach (DataRow row in schema.Rows)
            {
                var tableSchema = GetSchemaValue(row, "TABLE_SCHEMA");
                var tableName = GetSchemaValue(row, "TABLE_NAME");
                var tableType = GetSchemaValue(row, "TABLE_TYPE");

                if (string.IsNullOrWhiteSpace(tableName))
                    continue;
                if (!string.IsNullOrWhiteSpace(schemaName) &&
                    !string.Equals(tableSchema, schemaName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!includeViews &&
                    !string.IsNullOrWhiteSpace(tableType) &&
                    tableType.IndexOf("VIEW", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                filtered.ImportRow(row);
            }

            return filtered;
        }

        public static bool TableExists(
            string providerInvariantName,
            string connectionString,
            string tableName,
            string schemaName = null,
            bool includeViews = false)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Имя таблицы не может быть пустым.", nameof(tableName));

            var schema = GetTablesSchema(providerInvariantName, connectionString, schemaName, includeViews);
            return schema.Rows.Cast<DataRow>()
                .Any(row => string.Equals(GetSchemaValue(row, "TABLE_NAME"), tableName, StringComparison.OrdinalIgnoreCase));
        }

        public static List<string> GetTableNames(
            string providerInvariantName,
            string connectionString,
            string schemaName = null,
            bool includeViews = false)
        {
            var schema = GetTablesSchema(providerInvariantName, connectionString, schemaName, includeViews);
            return schema.Rows.Cast<DataRow>()
                .Select(row =>
                {
                    var tableSchema = GetSchemaValue(row, "TABLE_SCHEMA");
                    var tableName = GetSchemaValue(row, "TABLE_NAME");
                    return string.IsNullOrWhiteSpace(tableSchema) ? tableName : tableSchema + "." + tableName;
                })
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static DataTable GetColumnsSchema(
            string providerInvariantName,
            string connectionString,
            string tableName,
            string schemaName = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Имя таблицы не может быть пустым.", nameof(tableName));

            var schema = GetSchema(providerInvariantName, connectionString, "Columns");
            var filtered = schema.Clone();

            foreach (DataRow row in schema.Rows)
            {
                var rowSchema = GetSchemaValue(row, "TABLE_SCHEMA");
                var rowTable = GetSchemaValue(row, "TABLE_NAME");

                if (!string.Equals(rowTable, tableName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrWhiteSpace(schemaName) &&
                    !string.Equals(rowSchema, schemaName, StringComparison.OrdinalIgnoreCase))
                    continue;

                filtered.ImportRow(row);
            }

            return filtered;
        }

        public static List<string> GetColumnNames(
            string providerInvariantName,
            string connectionString,
            string tableName,
            string schemaName = null)
        {
            var schema = GetColumnsSchema(providerInvariantName, connectionString, tableName, schemaName);
            return schema.Rows.Cast<DataRow>()
                .Select(row => GetSchemaValue(row, "COLUMN_NAME"))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string GetSchemaValue(DataRow row, string columnName)
        {
            if (row == null || row.Table == null)
                return null;

            if (row.Table.Columns.Contains(columnName))
                return row[columnName]?.ToString();

            var matchingColumn = row.Table.Columns
                .Cast<DataColumn>()
                .FirstOrDefault(col => string.Equals(col.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));

            return matchingColumn != null ? row[matchingColumn]?.ToString() : null;
        }
    }
}
