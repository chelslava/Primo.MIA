// =============================================================================
// DataTableFilterLogic.cs — чистая логика активности «DataTable: Фильтрация».
//
// Фильтрует DataTable LINQ-выражением (Rows.Cast<DataRow>().Where) — исходный
// порядок строк сохраняется, исходная таблица не изменяется.
//
// Два режима задания условий:
//   - SQL-подобное выражение → компилирует DataTableSqlWhereParser;
//   - список условий конструктора → общий движок сравнения значений.
//
// Результат — две таблицы (клоны схемы исходной):
//   Filtered — строки, прошедшие условие; Rejected — не прошедшие.
//
// Ошибки: ArgumentNullException (null-таблица), UtilityException
// (синтаксис выражения, неизвестная колонка, пустой список условий).
// =============================================================================

using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    /// <summary>
    /// Логика фильтрации DataTable: SQL-подобные выражения и конструктор условий.
    /// </summary>
    public class DataTableFilterLogic
    {
        // ── ФИЛЬТРАЦИЯ ─────────────────────────────────────────────────────────

        /// <summary>
        /// Отфильтровать таблицу SQL-подобным выражением.
        /// Порядок строк и схема сохраняются, исходная таблица не изменяется.
        /// </summary>
        /// <param name="source">Исходная таблица.</param>
        /// <param name="sqlExpression">Выражение, например: Age &gt;= 18 AND City = 'Москва'</param>
        /// <param name="caseSensitive">Учитывать регистр при сравнении строк.</param>
        public DataTableFilterResult Filter(DataTable source, string sqlExpression, bool caseSensitive = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var predicate = DataTableSqlWhereParser.Compile(source, sqlExpression, caseSensitive);
            return Apply(source, predicate);
        }

        /// <summary>
        /// Отфильтровать таблицу списком условий конструктора.
        /// Условия объединяются слева направо: (c1 ⋈ c2) ⋈ c3...
        /// Связка (Join) первого условия игнорируется.
        /// </summary>
        /// <param name="source">Исходная таблица.</param>
        /// <param name="conditions">Условия (минимум одно).</param>
        /// <param name="caseSensitive">Учитывать регистр при сравнении строк.</param>
        public DataTableFilterResult Filter(DataTable source, IReadOnlyList<DataTableFilterCondition> conditions, bool caseSensitive = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (conditions == null || conditions.Count == 0)
                throw new UtilityException(ActivityStrings.Error_ConditionsRequired);

            // Колонки резолвим один раз — неизвестная колонка даёт ошибку сразу
            var compiled = conditions
                .Select(c => (Join: c.Join, Column: ResolveColumn(source, c.Column), Condition: c))
                .ToList();

            var predicate = BuildConditionPredicate(compiled, caseSensitive);
            return Apply(source, predicate);
        }

        private DataTableFilterResult Apply(DataTable source, Func<DataRow, bool> predicate)
        {
            // Cast<DataRow>() + Where — LINQ поверх source.Rows:
            // перебор идёт в порядке Rows, поэтому порядок строк сохраняется
            var rows = source.Rows.Cast<DataRow>();

            var filtered = source.Clone();
            foreach (var row in rows.Where(predicate))
                filtered.ImportRow(row);

            var rejected = source.Clone();
            foreach (var row in rows.Where(row => !predicate(row)))
                rejected.ImportRow(row);

            return new DataTableFilterResult
            {
                Filtered = filtered,
                Rejected = rejected,
                FilteredCount = filtered.Rows.Count,
                RejectedCount = rejected.Rows.Count
            };
        }

        // ── DSL КОНСТРУКТОРА УСЛОВИЙ ───────────────────────────────────────────

        /// <summary>
        /// Сериализовать условия в DSL-строку для хранения в свойстве активности.
        /// Формат строки: «Join|Column|Operator|Value», по строке на условие;
        /// '|' и '\' внутри колонки/значения экранируются обратным слэшем.
        /// </summary>
        public static string ToConditionsDsl(IEnumerable<DataTableFilterCondition> conditions)
        {
            if (conditions == null) return string.Empty;

            var sb = new StringBuilder();
            foreach (var c in conditions)
            {
                sb.Append(c.Join.ToString())
                  .Append('|')
                  .Append(DslEscape(c.Column ?? string.Empty))
                  .Append('|')
                  .Append(c.Operator.ToString())
                  .Append('|')
                  .Append(DslEscape(c.Value ?? string.Empty))
                  .AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Разобрать DSL-строку конструктора условий. Пустая строка → пустой список.
        /// </summary>
        /// <exception cref="UtilityException">Некорректный формат строки условия.</exception>
        public static List<DataTableFilterCondition> ParseConditionsDsl(string dsl)
        {
            var result = new List<DataTableFilterCondition>();
            if (string.IsNullOrWhiteSpace(dsl)) return result;

            string[] lines = dsl.Replace("\r\n", "\n").Split('\n');
            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = DslSplitUnescaped(line);
                if (parts.Count < 4)
                    throw new UtilityException(
                        $"{ActivityStrings.Error_SqlParse}: некорректная строка условия «{line}». " +
                        "Ожидается формат: Join|Column|Operator|Value");

                if (!Enum.TryParse<DataTableConditionJoin>(parts[0].Trim(), true, out var join))
                    join = DataTableConditionJoin.And;
                if (!Enum.TryParse<DataTableConditionOperator>(parts[2].Trim(), true, out var op))
                    throw new UtilityException(
                        $"{ActivityStrings.Error_SqlParse}: неизвестный оператор «{parts[2].Trim()}».");

                result.Add(new DataTableFilterCondition
                {
                    Join = join,
                    Column = parts[1].Trim(),
                    Operator = op,
                    Value = parts[3]
                });
            }

            return result;
        }

        private static string DslEscape(string text) =>
            text.Replace("\\", "\\\\").Replace("|", "\\|");

        /// <summary>Разделить строку по неэкранированным '|' с восстановлением экранирования.</summary>
        private static List<string> DslSplitUnescaped(string line)
        {
            var parts = new List<string>();
            var sb = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\\' && i + 1 < line.Length && (line[i + 1] == '|' || line[i + 1] == '\\'))
                {
                    sb.Append(line[i + 1]);
                    i++;
                }
                else if (c == '|')
                {
                    parts.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            parts.Add(sb.ToString());
            return parts;
        }

        // ── ДВИЖОК СРАВНЕНИЯ ЗНАЧЕНИЙ ──────────────────────────────────────────

        /// <summary>Ячейка пустая (null или DBNull).</summary>
        internal static bool IsNullOrDbNull(object value) => value == null || value == DBNull.Value;

        /// <summary>Значение ячейки как строка; null/DBNull → null.</summary>
        internal static string CellAsString(DataRow row, DataColumn column)
        {
            object value = row[column];
            return IsNullOrDbNull(value) ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Сравнить ячейку с литералом оператором cmp (=, &lt;&gt;, &gt;, &lt;, &gt;=, &lt;=).
        /// Пустая ячейка или несовместимый литерал → false.
        /// </summary>
        internal static bool Compare(DataRow row, DataColumn column, string cmp, object literal, bool caseSensitive)
        {
            object cell = row[column];
            if (IsNullOrDbNull(cell)) return false;

            object right = CoerceValue(literal, column.DataType);
            if (right == null) return false;

            int? cmpResult = CompareValues(cell, right, caseSensitive);
            if (cmpResult == null) return false;

            switch (cmp)
            {
                case "=": return cmpResult == 0;
                case "<>": return cmpResult != 0;
                case ">": return cmpResult > 0;
                case "<": return cmpResult < 0;
                case ">=": return cmpResult >= 0;
                case "<=": return cmpResult <= 0;
                default: return false;
            }
        }

        /// <summary>Ячейка равна любому из вариантов (для IN).</summary>
        internal static bool CompareWithAny(DataRow row, DataColumn column, IReadOnlyList<object> variants, bool caseSensitive)
        {
            foreach (var variant in variants)
            {
                if (Compare(row, column, "=", variant, caseSensitive))
                    return true;
            }
            return false;
        }

        /// <summary>Проверить строку шаблоном LIKE (% — любое число символов, _ — один).</summary>
        internal static bool MatchLike(string text, string pattern, bool caseSensitive)
        {
            var regex = LikeToRegex(pattern, caseSensitive);
            return regex.IsMatch(text);
        }

        /// <summary>Вычислить одно условие конструктора по строке.</summary>
        internal static bool EvaluateCondition(DataRow row, DataColumn column, DataTableFilterCondition condition, bool caseSensitive)
        {
            object cell = row[column];
            bool empty = IsNullOrDbNull(cell);

            switch (condition.Operator)
            {
                case DataTableConditionOperator.IsNull:
                    return empty;
                case DataTableConditionOperator.IsNotNull:
                    return !empty;
            }

            if (empty) return false;

            switch (condition.Operator)
            {
                case DataTableConditionOperator.Contains:
                    return CellAsString(row, column).IndexOf(NullToEmpty(condition.Value), GetStringComparison(caseSensitive)) >= 0;
                case DataTableConditionOperator.StartsWith:
                    return CellAsString(row, column).StartsWith(NullToEmpty(condition.Value), GetStringComparison(caseSensitive));
                case DataTableConditionOperator.EndsWith:
                    return CellAsString(row, column).EndsWith(NullToEmpty(condition.Value), GetStringComparison(caseSensitive));
                case DataTableConditionOperator.Like:
                    return MatchLike(CellAsString(row, column), NullToEmpty(condition.Value), caseSensitive);
                case DataTableConditionOperator.In:
                    return ParseInVariants(condition.Value)
                        .Any(v => Compare(row, column, "=", v, caseSensitive));
                default:
                    return Compare(row, column, OperatorToCmp(condition.Operator), condition.Value, caseSensitive);
            }
        }

        // ── СЛУЖЕБНЫЕ МЕТОДЫ ───────────────────────────────────────────────────

        private static Func<DataRow, bool> BuildConditionPredicate(
            List<(DataTableConditionJoin Join, DataColumn Column, DataTableFilterCondition Condition)> compiled,
            bool caseSensitive)
        {
            // Компилируем каждое условие в отдельный предикат
            var items = compiled
                .Select(x => (x.Join, Func: (Func<DataRow, bool>)(row => EvaluateCondition(row, x.Column, x.Condition, caseSensitive))))
                .ToList();

            // Слева-направо: (c1 ⋈ c2) ⋈ c3...
            return row =>
            {
                bool result = true;
                for (int i = 0; i < items.Count; i++)
                {
                    bool value = items[i].Func(row);
                    if (i == 0)
                    {
                        result = value;
                    }
                    else if (items[i].Join == DataTableConditionJoin.And)
                    {
                        result &= value;
                    }
                    else
                    {
                        result |= value;
                    }
                }
                return result;
            };
        }

        private static DataColumn ResolveColumn(DataTable table, string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new UtilityException(
                    $"{ActivityStrings.Error_SqlParse}: имя колонки условия не указано.");

            var column = table.Columns
                .Cast<DataColumn>()
                .FirstOrDefault(c => string.Equals(c.ColumnName, columnName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (column == null)
                throw new UtilityException(
                    $"{ActivityStrings.Error_UnknownColumn}: '{columnName.Trim()}'. Таблица содержит: " +
                    string.Join(", ", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName)) + ".");

            return column;
        }

        private static string OperatorToCmp(DataTableConditionOperator op)
        {
            switch (op)
            {
                case DataTableConditionOperator.NotEqual: return "<>";
                case DataTableConditionOperator.GreaterThan: return ">";
                case DataTableConditionOperator.GreaterOrEqual: return ">=";
                case DataTableConditionOperator.LessThan: return "<";
                case DataTableConditionOperator.LessOrEqual: return "<=";
                default: return "=";
            }
        }

        /// <summary>Строка условия IN → список литералов («1, 2» / «'a', "b"»).</summary>
        private static List<object> ParseInVariants(string value)
        {
            var variants = new List<object>();
            if (string.IsNullOrWhiteSpace(value)) return variants;

            foreach (string raw in value.Split(','))
            {
                string item = raw.Trim();
                if (item.Length >= 2 && ((item[0] == '\'' && item[item.Length - 1] == '\'') ||
                                         (item[0] == '"' && item[item.Length - 1] == '"')))
                {
                    item = item.Substring(1, item.Length - 2);
                }
                variants.Add(item);
            }
            return variants;
        }

        private static StringComparison GetStringComparison(bool caseSensitive) =>
            ComparisonHelper.GetStringComparison(caseSensitive);

        private static string NullToEmpty(string value) => value ?? string.Empty;

        /// <summary>Привести литерал к типу колонки; несовместимо → null.</summary>
        private static object CoerceValue(object value, Type targetType)
        {
            try
            {
                targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                if (value is string s)
                {
                    if (targetType == typeof(string)) return s;

                    if (targetType == typeof(DateTime))
                        return ParseDateTime(s);

                    if (targetType == typeof(bool))
                        return bool.TryParse(s, out bool b) ? b : (object)null;

                    if (IsNumericType(targetType))
                        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal d)
                            ? Convert.ChangeType(d, targetType, CultureInfo.InvariantCulture)
                            : null;

                    return s;
                }

                if (targetType == typeof(string))
                    return Convert.ToString(value, CultureInfo.InvariantCulture);

                if (IsNumericType(targetType) && value is decimal d2)
                    return Convert.ChangeType(d2, targetType, CultureInfo.InvariantCulture);

                if (targetType.IsInstanceOfType(value))
                    return value;

                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
            {
                return null;
            }
        }

        /// <summary>Строка → DateTime: текущая культура, затем инвариантная.</summary>
        private static object ParseDateTime(string s)
        {
            if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var dt))
                return dt;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dt))
                return dt;
            return null;
        }

        private static bool IsNumericType(Type type)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;
            return t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) ||
                   t == typeof(sbyte) || t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort) ||
                   t == typeof(decimal) || t == typeof(double) || t == typeof(float);
        }

        /// <summary>
        /// Сравнить приведённые значения: числа — численно, DateTime — по дате,
        /// bool — как 0/1, остальное — как строки. Несравнимо → null.
        /// </summary>
        private static int? CompareValues(object left, object right, bool caseSensitive)
        {
            var comparison = GetStringComparison(caseSensitive);

            // Числа (ячейки разных числовых типов приводим к decimal; double — к double)
            if (IsNumericType(left.GetType()) && IsNumericType(right.GetType()))
            {
                if (left is double || right is double || left is float || right is float)
                {
                    double dl = Convert.ToDouble(left, CultureInfo.InvariantCulture);
                    double dr = Convert.ToDouble(right, CultureInfo.InvariantCulture);
                    return dl.CompareTo(dr);
                }
                decimal ml = Convert.ToDecimal(left, CultureInfo.InvariantCulture);
                decimal mr = Convert.ToDecimal(right, CultureInfo.InvariantCulture);
                return ml.CompareTo(mr);
            }

            if (left is DateTime ld && right is DateTime rd)
                return ld.CompareTo(rd);

            if (left is bool lb && right is bool rb)
                return lb.CompareTo(rb);

            // Остальное — как строки
            string ls = Convert.ToString(left, CultureInfo.InvariantCulture);
            string rs = Convert.ToString(right, CultureInfo.InvariantCulture);
            return string.Compare(ls, rs, comparison);
        }

        /// <summary>Шаблон LIKE → заякоренный regex.</summary>
        private static Regex LikeToRegex(string pattern, bool caseSensitive)
        {
            var sb = new StringBuilder();
            foreach (char c in pattern)
            {
                switch (c)
                {
                    case '%': sb.Append(".*"); break;
                    case '_': sb.Append('.'); break;
                    default:
                        sb.Append(Regex.Escape(c.ToString()));
                        break;
                }
            }

            var options = RegexOptions.Compiled | RegexOptions.Singleline;
            if (!caseSensitive) options |= RegexOptions.IgnoreCase;

            return new Regex($"^{sb}$", options, TimeSpan.FromSeconds(1));
        }
    }

    /// <summary>
    /// Результат фильтрации: две таблицы с сохранённым порядком строк.
    /// </summary>
    public class DataTableFilterResult
    {
        /// <summary>Строки, прошедшие условие (клон схемы исходной таблицы).</summary>
        public DataTable Filtered { get; set; }

        /// <summary>Строки, не прошедшие условие (клон схемы исходной таблицы).</summary>
        public DataTable Rejected { get; set; }

        /// <summary>Количество строк, прошедших условие.</summary>
        public int FilteredCount { get; set; }

        /// <summary>Количество отсеянных строк.</summary>
        public int RejectedCount { get; set; }
    }

    /// <summary>
    /// Одно условие конструктора фильтра: «Колонка + Оператор + Значение»
    /// со связкой (И/ИЛИ) с предыдущим условием.
    /// </summary>
    public class DataTableFilterCondition
    {
        /// <summary>Связка с предыдущим условием (для первого игнорируется).</summary>
        public DataTableConditionJoin Join { get; set; } = DataTableConditionJoin.And;

        /// <summary>Имя колонки таблицы (без учёта регистра).</summary>
        public string Column { get; set; }

        /// <summary>Оператор сравнения.</summary>
        public DataTableConditionOperator Operator { get; set; } = DataTableConditionOperator.Equal;

        /// <summary>Значение для сравнения (строка; приводится к типу колонки).</summary>
        public string Value { get; set; } = string.Empty;
    }
}
