// =============================================================================
// DatabaseUpsertBatchHelper.cs — батч-UPSERT по провайдерам (issue #22).
//
// Построчный путь (DatabaseUpsertHelper.ExecuteUpsertCore) даёт до 2
// round-trip'ов на строку; батч-путь отправляет порцию строк одним
// запросом:
//   SqlClient   → MERGE ... USING (VALUES ...) ... OUTPUT $action (точный
//                 подсчёт вставленных/обновлённых)
//   Npgsql      → INSERT ... ON CONFLICT (key) DO UPDATE ... RETURNING
//                 (xmax = 0) (точный подсчёт)
//   MySql.Data  → INSERT ... ON DUPLICATE KEY UPDATE (разбиение
//                 вставленных/обновлённых — эвристика по affected rows;
//                 batchSize=1 даёт точный подсчёт: 1=insert, 2=update)
// Прочие провайдеры — fallback на построчный путь.
// Транзакционность не меняется: команды выполняются в транзакции,
// переданной вызывающим (активность/DatabaseHelper).
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Primo.MIA
{
    internal static class DatabaseUpsertBatchHelper
    {
        /// <summary>Порция строк по умолчанию.</summary>
        internal const int DefaultBatchSize = 500;

        /// <summary>Лимит параметров SQL Server (2100) с запасом.</summary>
        private const int SqlClientMaxParameters = 2100;

        internal static bool IsBatchSupported(string providerInvariantName)
        {
            var provider = (providerInvariantName ?? string.Empty).Trim();
            return IsSqlClient(provider)
                   || string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase)
                   || provider.IndexOf("MySql", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Приводит размер порции: 0/отрицательный — DefaultBatchSize;
        /// для SqlClient дополнительно ограничен лимитом 2100 параметров
        /// (порция × колонок на строку ≤ 2100).
        /// </summary>
        internal static int ResolveBatchSize(int requested, string providerInvariantName, int columnsPerRow)
        {
            var size = requested > 0 ? requested : DefaultBatchSize;

            var provider = (providerInvariantName ?? string.Empty).Trim();
            if (IsSqlClient(provider)
                && columnsPerRow > 0)
            {
                var max = SqlClientMaxParameters / columnsPerRow;
                if (max < 1)
                    max = 1;
                if (size > max)
                    size = max;
            }

            return size;
        }

        /// <summary>
        /// Батч-UPSERT: таблица делится на порции, каждая порция — один
        /// ExecuteReader (SqlClient/Npgsql отдают подсчёт через reader,
        /// MySql — через affected rows в ExecuteNonQuery).
        /// </summary>
        internal static UpsertResult ExecuteUpsertBatchCore(
            DbConnection connection,
            DbTransaction transaction,
            string providerInvariantName,
            DataTable dataTable,
            string destinationTableName,
            List<BulkInsertColumnMapping> allMappings,
            List<BulkInsertColumnMapping> keyMappings,
            List<BulkInsertColumnMapping> updateMappings,
            int commandTimeoutSeconds,
            int batchSize)
        {
            var columnsPerRow = allMappings.Count;
            var size = ResolveBatchSize(batchSize, providerInvariantName, columnsPerRow);

            var inserted = 0;
            var updated = 0;
            var rowIndex = 0;

            while (rowIndex < dataTable.Rows.Count)
            {
                var take = Math.Min(size, dataTable.Rows.Count - rowIndex);

                using (var command = BuildBatchCommand(
                           connection, transaction, providerInvariantName,
                           destinationTableName, allMappings, keyMappings, updateMappings,
                           commandTimeoutSeconds, rowIndex, take))
                {
                    AssignBatchParameterValues(command, dataTable, rowIndex, take, allMappings);
                    if (IsMySql(providerInvariantName))
                    {
                        // 1 = вставка, 2 = обновление (с изменением), 0 = обновление без изменений
                        var affected = command.ExecuteNonQuery();
                        var updatedEstimate = Math.Max(0, affected - take);
                        var insertedEstimate = Math.Max(0, take - updatedEstimate);
                        inserted += insertedEstimate;
                        updated += updatedEstimate;
                    }
                    else
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (IsSqlClient(providerInvariantName))
                                {
                                    // OUTPUT $action: "INSERT" / "UPDATE"
                                    var action = Convert.ToString(reader.GetValue(0), System.Globalization.CultureInfo.InvariantCulture);
                                    if (string.Equals(action, "INSERT", StringComparison.OrdinalIgnoreCase))
                                        inserted++;
                                    else
                                        updated++;
                                }
                                else
                                {
                                    // Npgsql RETURNING (xmax = 0): true = вставка
                                    inserted += Convert.ToBoolean(reader.GetValue(0), System.Globalization.CultureInfo.InvariantCulture) ? 1 : 0;
                                    updated++;
                                }
                            }
                        }
                    }
                }

                rowIndex += take;
            }

            return new UpsertResult { InsertedCount = inserted, UpdatedCount = updated };
        }

        /// <summary>Строит команду батча для порции строк [rowIndex, rowIndex+take).</summary>
        internal static DbCommand BuildBatchCommand(
            DbConnection connection,
            DbTransaction transaction,
            string providerInvariantName,
            string destinationTableName,
            List<BulkInsertColumnMapping> allMappings,
            List<BulkInsertColumnMapping> keyMappings,
            List<BulkInsertColumnMapping> updateMappings,
            int commandTimeoutSeconds,
            int rowIndex,
            int take)
        {
            var columnsPerRow = allMappings.Count;
            var totalParameters = columnsPerRow * take;

            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = commandTimeoutSeconds > 0 ? commandTimeoutSeconds : 60;

            var text = new StringBuilder();
            var destination = DatabaseHelper.QuoteQualifiedIdentifier(providerInvariantName, destinationTableName);
            var insertColumns = string.Join(", ", System.Linq.Enumerable.Select(allMappings, m =>
                DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn)));

            // VALUES (...), (...) — параметры p0..p(total-1)
            var valuesClauses = new List<string>(take);
            for (var r = 0; r < take; r++)
            {
                var placeholders = new List<string>(columnsPerRow);
                for (var c = 0; c < columnsPerRow; c++)
                    placeholders.Add(DatabaseCommandHelper.GetParameterPlaceholder(
                        providerInvariantName, r * columnsPerRow + c));
                valuesClauses.Add("(" + string.Join(", ", placeholders) + ")");
            }

            text.Append("INSERT INTO ").Append(destination)
                .Append(" (").Append(insertColumns).Append(") VALUES ")
                .Append(string.Join(", ", valuesClauses));

            if (IsSqlClient(providerInvariantName))
            {
                // MERGE не нужен поверх INSERT: SqlClient-путь строится
                // отдельно ниже (INSERT...VALUES в MERGE USING)
                text.Clear();
                text.Append(BuildSqlClientMergeText(
                    providerInvariantName, destinationTableName, allMappings,
                    keyMappings, updateMappings, take));
            }
            else if (IsMySql(providerInvariantName))
            {
                text.Append(" ON DUPLICATE KEY UPDATE ")
                    .AppendJoinUpdateColumnsMySql(providerInvariantName, updateMappings, allMappings);
            }
            else // Npgsql
            {
                var conflictKeys = string.Join(", ", System.Linq.Enumerable.Select(keyMappings, m =>
                    DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn)));
                if (updateMappings.Count > 0)
                {
                    text.Append(" ON CONFLICT (").Append(conflictKeys).Append(") DO UPDATE SET ")
                        .AppendJoinUpdateColumnsNpgsql(providerInvariantName, updateMappings)
                        .Append(" RETURNING (xmax = 0)");
                }
                else
                {
                    text.Append(" ON CONFLICT (").Append(conflictKeys).Append(") DO NOTHING")
                        .Append(" RETURNING 1");
                }
            }

            command.CommandText = text.ToString();
            CreateBatchParameters(command, providerInvariantName, allMappings, totalParameters);
            DatabaseCommandHelper.TryPrepareCommand(command);
            return command;
        }

        /// <summary>
        /// SqlClient: MERGE с OUTPUT $action (точный подсчёт вставленных/обновлённых).
        /// При пустом списке колонок обновления — no-op UPDATE по ключу.
        /// </summary>
        private static string BuildSqlClientMergeText(
            string providerInvariantName,
            string destinationTableName,
            List<BulkInsertColumnMapping> allMappings,
            List<BulkInsertColumnMapping> keyMappings,
            List<BulkInsertColumnMapping> updateMappings,
            int take)
        {
            var destination = DatabaseHelper.QuoteQualifiedIdentifier(providerInvariantName, destinationTableName);
            var columnsPerRow = allMappings.Count;

            var sourceColumns = string.Join(", ", System.Linq.Enumerable.Select(allMappings, m =>
                DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn)));

            var valuesClauses = new List<string>(take);
            for (var r = 0; r < take; r++)
            {
                var placeholders = new List<string>(columnsPerRow);
                for (var c = 0; c < columnsPerRow; c++)
                    placeholders.Add(DatabaseCommandHelper.GetParameterPlaceholder(
                        providerInvariantName, r * columnsPerRow + c));
                valuesClauses.Add("(" + string.Join(", ", placeholders) + ")");
            }

            var onClause = string.Join(" AND ", System.Linq.Enumerable.Select(keyMappings, (m, i) =>
                $"tgt.{DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn)} = src.{DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn)}"));

            string whenMatched;
            if (updateMappings.Count > 0)
            {
                whenMatched = "WHEN MATCHED THEN UPDATE SET " +
                              string.Join(", ", System.Linq.Enumerable.Select(updateMappings, m =>
                                  $"tgt.{DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn)} = src.{DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn)}"));
            }
            else
            {
                // No-op обновление по ключу — affected-строки всё равно считаются OUTPUT'ом
                whenMatched = "WHEN MATCHED THEN UPDATE SET " +
                              string.Join(", ", System.Linq.Enumerable.Select(keyMappings, m =>
                                  $"tgt.{DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn)} = src.{DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn)}"));
            }

            var insertColumns = string.Join(", ", System.Linq.Enumerable.Select(allMappings, m =>
                DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn)));
            var sourceValues = string.Join(", ", System.Linq.Enumerable.Select(allMappings, m =>
                "src." + DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn)));

            return $"MERGE INTO {destination} WITH (HOLDLOCK) AS tgt " +
                   $"USING (VALUES {string.Join(", ", valuesClauses)}) AS src ({sourceColumns}) " +
                   $"ON {onClause} " +
                   $"{whenMatched} " +
                   $"WHEN NOT MATCHED THEN INSERT ({insertColumns}) VALUES ({sourceValues}) " +
                   "OUTPUT $action;";
        }

        private static void CreateBatchParameters(
            DbCommand command,
            string providerInvariantName,
            List<BulkInsertColumnMapping> allMappings,
            int totalParameters)
        {
            command.Parameters.Clear();
            for (var i = 0; i < totalParameters; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = DatabaseCommandHelper.GetParameterName(providerInvariantName, i);
                command.Parameters.Add(parameter);
            }
        }

        /// <summary>Значения параметров порции: строка за строкой, колонка за колонкой.</summary>
        internal static void AssignBatchParameterValues(
            DbCommand command,
            DataTable dataTable,
            int rowIndex,
            int take,
            List<BulkInsertColumnMapping> allMappings)
        {
            var index = 0;
            for (var r = 0; r < take; r++)
            {
                var row = dataTable.Rows[rowIndex + r];
                foreach (var mapping in allMappings)
                    command.Parameters[index++].Value = row[mapping.SourceColumn] ?? DBNull.Value;
            }
        }

        private static bool IsSqlClient(string providerInvariantName)
        {
            // MySql.Data.MySqlClient тоже содержит подстроку "SqlClient" —
            // исключаем: SqlClient = System.Data.SqlClient / Microsoft.Data.SqlClient
            var provider = (providerInvariantName ?? string.Empty).Trim();
            return provider.EndsWith("SqlClient", StringComparison.OrdinalIgnoreCase)
                   && !provider.EndsWith("MySqlClient", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMySql(string providerInvariantName)
        {
            return (providerInvariantName ?? string.Empty).IndexOf("MySql", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    /// <summary>Хелперы построения UPDATE-части (локально для читаемости).</summary>
    internal static class UpsertBatchTextExtensions
    {
        /// <summary>MySql: col = VALUES(col).</summary>
        public static StringBuilder AppendJoinUpdateColumnsMySql(
            this StringBuilder sb,
            string providerInvariantName,
            List<BulkInsertColumnMapping> updateMappings,
            List<BulkInsertColumnMapping> allMappings)
        {
            if (updateMappings == null || updateMappings.Count == 0)
            {
                // Обновлять нечего — no-op по первому ключу (MySql не допускает
                // пустого SET); значение переписывается тем же
                var first = allMappings[0];
                sb.Append(DatabaseHelper.QuoteIdentifier(providerInvariantName, first.DestinationColumn))
                  .Append(" = VALUES(")
                  .Append(DatabaseHelper.QuoteIdentifier(providerInvariantName, first.DestinationColumn))
                  .Append(")");
                return sb;
            }

            var clauses = new List<string>(updateMappings.Count);
            foreach (var m in updateMappings)
            {
                var column = DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn);
                clauses.Add($"{column} = VALUES({column})");
            }
            sb.Append(string.Join(", ", clauses));
            return sb;
        }

        /// <summary>Npgsql: col = EXCLUDED.col.</summary>
        public static StringBuilder AppendJoinUpdateColumnsNpgsql(
            this StringBuilder sb,
            string providerInvariantName,
            List<BulkInsertColumnMapping> updateMappings)
        {
            var clauses = new List<string>(updateMappings.Count);
            foreach (var m in updateMappings)
            {
                var column = DatabaseHelper.QuoteIdentifier(providerInvariantName, m.DestinationColumn);
                clauses.Add($"{column} = EXCLUDED.{column}");
            }
            sb.Append(string.Join(", ", clauses));
            return sb;
        }
    }
}
