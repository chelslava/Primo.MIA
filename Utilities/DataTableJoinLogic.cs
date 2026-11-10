// =============================================================================
// DataTableJoinLogic.cs — чистая логика Join / Pivot / Unpivot (issue #36).
//
// Join: SQL-подобные соединения двух DataTable по одному или нескольким
// ключевым колонкам (inner/left/right/full), NULL-ключи не совпадают
// (семантика SQL), конфликты имён колонок решаются суффиксом.
// Pivot: длинная форма → широкая (строки × значения pivot-колонки → агрегат).
// Unpivot: широкая форма → длинная.
// Всё на чистом System.Data — без внешних зависимостей.
// =============================================================================

using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Логика соединения и переструктурирования DataTable.
    /// </summary>
    public class DataTableJoinLogic
    {
        /// <summary>Суффикс по умолчанию для конфликтов имён колонок.</summary>
        public const string DefaultRightSuffix = "_right";

        // ── Join ───────────────────────────────────────────────────────

        /// <summary>
        /// Соединяет две таблицы по ключевым колонкам.
        /// </summary>
        /// <param name="left">Левая таблица.</param>
        /// <param name="right">Правая таблица.</param>
        /// <param name="leftKeys">Имена ключевых колонок левой таблицы.</param>
        /// <param name="rightKeys">Имена ключевых колонок правой таблицы (по порядку соответствуют leftKeys).</param>
        /// <param name="kind">Тип соединения.</param>
        /// <param name="rightSuffix">Суффикс для конфликтов имён не-ключевых колонок справа.</param>
        /// <param name="caseSensitive">Сравнивать ли строковые ключи с учётом регистра.</param>
        /// <returns>Соединённая таблица.</returns>
        public DataTable Join(
            DataTable left,
            DataTable right,
            IReadOnlyList<string> leftKeys,
            IReadOnlyList<string> rightKeys,
            DataTableJoinKind kind,
            string rightSuffix = DefaultRightSuffix,
            bool caseSensitive = false)
        {
            Guard.NotNull(left, nameof(left));
            Guard.NotNull(right, nameof(right));
            Guard.NotNull(leftKeys, nameof(leftKeys));
            Guard.NotNull(rightKeys, nameof(rightKeys));

            if (leftKeys.Count == 0)
                throw new InvalidOperationException("Не заданы ключевые колонки для соединения");
            if (leftKeys.Count != rightKeys.Count)
                throw new InvalidOperationException(
                    $"Число ключевых колонок слева ({leftKeys.Count}) и справа ({rightKeys.Count}) должно совпадать");

            foreach (var key in leftKeys) ValidateColumn(left, key, "левой");
            foreach (var key in rightKeys) ValidateColumn(right, key, "правой");

            var comparer = caseSensitive
                ? StringComparer.Ordinal
                : StringComparer.OrdinalIgnoreCase;

            // Индекс правой таблицы: ключ → список строк (DBNull-ключи не совпадают)
            var rightIndex = BuildRightIndex(right, rightKeys, comparer);

            var (result, mapping) = BuildResultTable(left, right, leftKeys, rightKeys, rightSuffix, kind);

            foreach (DataRow leftRow in left.Rows)
            {
                var key = MakeCompositeKey(leftRow, leftKeys);
                var hasNullKey = ContainsNullKey(leftRow, leftKeys);

                List<DataRow> matches = null;
                if (!hasNullKey && rightIndex.TryGetValue(key, out var candidates))
                    matches = candidates;

                if (matches != null && matches.Count > 0)
                {
                    foreach (var rightRow in matches)
                        CopyJoinedRow(result, mapping, leftRow, rightRow);
                }
                else if (kind == DataTableJoinKind.Left || kind == DataTableJoinKind.Full)
                {
                    CopyJoinedRow(result, mapping, leftRow, null);
                }
            }

            if (kind == DataTableJoinKind.Right || kind == DataTableJoinKind.Full)
            {
                // Правые строки без пары: те, чей ключ не встречался слева
                var matchedRight = new HashSet<DataRow>();
                foreach (DataRow leftRow in left.Rows)
                {
                    if (ContainsNullKey(leftRow, leftKeys))
                        continue;
                    var key = MakeCompositeKey(leftRow, leftKeys);
                    if (rightIndex.TryGetValue(key, out var candidates))
                        foreach (var r in candidates) matchedRight.Add(r);
                }

                foreach (DataRow rightRow in right.Rows)
                {
                    if (ContainsNullKey(rightRow, rightKeys))
                    {
                        // NULL-ключи правой таблицы попадают в Right/Full без пары
                        CopyJoinedRow(result, mapping, null, rightRow);
                        continue;
                    }

                    var key = MakeCompositeKey(rightRow, rightKeys);
                    if (!matchedRight.Contains(rightRow))
                        CopyJoinedRow(result, mapping, null, rightRow);
                }
            }

            return result;
        }

        /// <summary>
        /// Индекс правой таблицы по составному ключу. DBNull-ключи исключаются
        /// (NULL не совпадает ни с чем — семантика SQL).
        /// </summary>
        private Dictionary<string, List<DataRow>> BuildRightIndex(
            DataTable right, IReadOnlyList<string> rightKeys, StringComparer comparer)
        {
            var index = new Dictionary<string, List<DataRow>>(comparer);
            foreach (DataRow row in right.Rows)
            {
                if (ContainsNullKey(row, rightKeys))
                    continue;
                var key = MakeCompositeKey(row, rightKeys);
                if (!index.TryGetValue(key, out var list))
                    index[key] = list = new List<DataRow>();
                list.Add(row);
            }
            return index;
        }

        private static bool ContainsNullKey(DataRow row, IReadOnlyList<string> keys)
        {
            foreach (var k in keys)
            {
                if (row[k] == null || row[k] == DBNull.Value)
                    return true;
            }
            return false;
        }

        private static string MakeCompositeKey(DataRow row, IReadOnlyList<string> keys)
        {
            // \u0001 между значениями — устойчиво к значениям, содержащим '|'
            return string.Join("\u0001", keys.Select(k => Convert.ToString(row[k], CultureInfo.InvariantCulture)));
        }

        private static void ValidateColumn(DataTable table, string columnName, string side)
        {
            Guard.NotNullOrWhiteSpace(columnName, nameof(columnName));
            if (!table.Columns.Contains(columnName))
                throw new InvalidOperationException(
                    $"Колонка '{columnName}' не найдена в {side} таблице");
        }

        /// <summary>
        /// Строит таблицу результата с явным маппингом «колонка результата →
        /// (сторона, имя колонки источника)»: все колонки левой + не-ключевые
        /// правые (с суффиксом при конфликте имён); ключевые правые исключаются
        /// (значения уже дублируются левой стороной).
        /// </summary>
        private (DataTable Table, List<(string Column, bool FromLeft, string SourceName)> Mapping) BuildResultTable(
            DataTable left, DataTable right,
            IReadOnlyList<string> leftKeys, IReadOnlyList<string> rightKeys,
            string rightSuffix, DataTableJoinKind kind)
        {
            var result = new DataTable($"{left.TableName}_{right.TableName}".Trim('_'));
            var suffix = string.IsNullOrEmpty(rightSuffix) ? DefaultRightSuffix : rightSuffix;
            var mapping = new List<(string Column, bool FromLeft, string SourceName)>();

            foreach (DataColumn c in left.Columns)
            {
                result.Columns.Add(c.ColumnName, c.DataType);
                mapping.Add((c.ColumnName, true, c.ColumnName));
            }

            var rightKeySet = new HashSet<string>(rightKeys, StringComparer.OrdinalIgnoreCase);
            var leftNames = new HashSet<string>(
                left.Columns.Cast<DataColumn>().Select(c => c.ColumnName),
                StringComparer.OrdinalIgnoreCase);

            foreach (DataColumn c in right.Columns)
            {
                // ключ справа дублирует левый — исключаем (Full сохраняет все)
                if (kind != DataTableJoinKind.Full && rightKeySet.Contains(c.ColumnName))
                    continue;

                var name = leftNames.Contains(c.ColumnName) ? c.ColumnName + suffix : c.ColumnName;
                result.Columns.Add(name, c.DataType);
                mapping.Add((name, false, c.ColumnName));
            }

            return (result, mapping);
        }

        private void CopyJoinedRow(
            DataTable result,
            List<(string Column, bool FromLeft, string SourceName)> mapping,
            DataRow leftRow, DataRow rightRow)
        {
            var row = result.NewRow();
            foreach (var m in mapping)
            {
                object value = null;
                var found = false;

                if (m.FromLeft)
                {
                    if (leftRow != null)
                    {
                        value = leftRow[m.SourceName];
                        found = true;
                    }
                }
                else if (rightRow != null)
                {
                    value = rightRow[m.SourceName];
                    found = true;
                }

                row[m.Column] = found ? value : DBNull.Value;
            }
            result.Rows.Add(row);
        }

        // ── Pivot ──────────────────────────────────────────────────────

        /// <summary>
        /// Сводная таблица: уникальные значения rowKeyColumn → строки,
        /// уникальные значения pivotColumn → колонки, ячейки — агрегат
        /// по valueColumn для пары (строка, колонка).
        /// </summary>
        public DataTable Pivot(
            DataTable source,
            string rowKeyColumn,
            string pivotColumn,
            string valueColumn,
            DataTableAggregation aggregation)
        {
            Guard.NotNull(source, nameof(source));
            ValidateColumn(source, rowKeyColumn, "исходной");
            ValidateColumn(source, pivotColumn, "исходной");
            ValidateColumn(source, valueColumn, "исходной");

            var rowKeys = new List<string>();
            var seenRows = new HashSet<string>(StringComparer.Ordinal);
            var pivots = new List<string>();
            var seenPivots = new HashSet<string>(StringComparer.Ordinal);

            foreach (DataRow row in source.Rows)
            {
                var rowKey = Convert.ToString(row[rowKeyColumn], CultureInfo.InvariantCulture);
                var pivot = Convert.ToString(row[pivotColumn], CultureInfo.InvariantCulture);

                if (seenRows.Add(rowKey)) rowKeys.Add(rowKey);
                if (seenPivots.Add(pivot)) pivots.Add(pivot);
            }

            var result = new DataTable("Pivot");
            result.Columns.Add(rowKeyColumn, typeof(string));
            foreach (var p in pivots)
                result.Columns.Add(p, typeof(object));

            // группировка значений
            var cells = new Dictionary<string, List<object>>(StringComparer.Ordinal);
            foreach (DataRow row in source.Rows)
            {
                var rowKey = Convert.ToString(row[rowKeyColumn], CultureInfo.InvariantCulture);
                var pivot = Convert.ToString(row[pivotColumn], CultureInfo.InvariantCulture);
                var value = row[valueColumn];

                if (!(value == null || value == DBNull.Value))
                {
                    var cellKey = rowKey + "\u0001" + pivot;
                    if (!cells.TryGetValue(cellKey, out var list))
                        cells[cellKey] = list = new List<object>();
                    list.Add(value);
                }
            }

            foreach (var rowKey in rowKeys)
            {
                var row = result.NewRow();
                row[rowKeyColumn] = rowKey;
                foreach (var p in pivots)
                {
                    var cellKey = rowKey + "\u0001" + p;
                    row[p] = cells.TryGetValue(cellKey, out var list)
                        ? Aggregate(list, aggregation)
                        : DBNull.Value;
                }
                result.Rows.Add(row);
            }

            return result;
        }

        private static object Aggregate(List<object> values, DataTableAggregation aggregation)
        {
            switch (aggregation)
            {
                case DataTableAggregation.Count:
                    return values.Count;

                case DataTableAggregation.First:
                    return values[0];

                case DataTableAggregation.Sum:
                    return SumNumeric(values);

                case DataTableAggregation.Min:
                case DataTableAggregation.Max:
                    return MinMax(values, aggregation == DataTableAggregation.Min);

                default:
                    return values[0];
            }
        }

        private static object SumNumeric(List<object> values)
        {
            decimal sum = 0m;
            var allIntegers = true;
            foreach (var v in values)
            {
                if (!TryToDecimal(v, out var d))
                    return null; // нечисловое значение — суммирование невозможно
                sum += d;
                if (d != decimal.Truncate(d)) allIntegers = false;
            }
            return allIntegers ? (object)Convert.ToInt64(sum) : (object)sum;
        }

        private static object MinMax(List<object> values, bool min)
        {
            // Числа и даты; смешанные типы — null
            if (values.All(v => v is DateTime))
            {
                var dates = values.Cast<DateTime>().ToList();
                return min ? dates.Min() : (object)dates.Max();
            }
            if (values.All(v => TryToDecimal(v, out _)))
            {
                decimal best = min ? decimal.MaxValue : decimal.MinValue;
                foreach (var v in values)
                {
                    TryToDecimal(v, out var d);
                    best = min ? Math.Min(best, d) : Math.Max(best, d);
                }
                return best;
            }
            if (values.All(v => v is string))
            {
                var strings = values.Cast<string>().ToList();
                return min ? strings.Min() : (object)strings.Max();
            }
            return null;
        }

        private static bool TryToDecimal(object value, out decimal d)
        {
            d = 0m;
            try
            {
                if (value is int i) { d = i; return true; }
                if (value is long l) { d = l; return true; }
                if (value is double db) { d = (decimal)db; return true; }
                if (value is decimal m) { d = m; return true; }
                if (value is float f) { d = (decimal)f; return true; }
                return decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out d);
            }
            catch
            {
                return false;
            }
        }

        // ── Unpivot ────────────────────────────────────────────────────

        /// <summary>
        /// Разбор сводной: широкая форма → длинная. KeyColumns копируются в
        /// каждую строку; для каждого valueColumn добавляется строка
        /// (имя колонки → varColumnName, значение → valueColumnName).
        /// </summary>
        public DataTable Unpivot(
            DataTable source,
            IReadOnlyList<string> keyColumns,
            IReadOnlyList<string> valueColumns,
            string variableColumnName,
            string valueColumnName)
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(keyColumns, nameof(keyColumns));
            Guard.NotNull(valueColumns, nameof(valueColumns));
            Guard.NotNullOrWhiteSpace(variableColumnName, nameof(variableColumnName));
            Guard.NotNullOrWhiteSpace(valueColumnName, nameof(valueColumnName));

            if (keyColumns.Count == 0)
                throw new InvalidOperationException("Не заданы ключевые колонки для Unpivot");
            if (valueColumns.Count == 0)
                throw new InvalidOperationException("Не заданы разворачиваемые колонки для Unpivot");

            foreach (var k in keyColumns) ValidateColumn(source, k, "исходной");
            foreach (var v in valueColumns) ValidateColumn(source, v, "исходной");

            var result = new DataTable("Unpivot");
            foreach (var k in keyColumns)
                result.Columns.Add(k, source.Columns[k].DataType);
            result.Columns.Add(variableColumnName, typeof(string));
            result.Columns.Add(valueColumnName, typeof(object));

            foreach (DataRow row in source.Rows)
            {
                foreach (var v in valueColumns)
                {
                    var newRow = result.NewRow();
                    foreach (var k in keyColumns)
                        newRow[k] = row[k];
                    newRow[variableColumnName] = v;
                    newRow[valueColumnName] = row[v];
                    result.Rows.Add(newRow);
                }
            }

            return result;
        }
    }
}
