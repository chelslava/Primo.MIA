// =============================================================================
// TextRegexLogic.cs — чистая логика TextRegex (issue #30).
//
// Match: все совпадения + группы → DataTable (колонки Match, Index,
// группа 1..N и именованные группы). Replace: подстановки $1/${name}.
// Обязательный match timeout — защита от ReDoS пользовательских паттернов.
// =============================================================================

using Primo.MIA.Common;
using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    /// <summary>
    /// Логика поиска/замены по регулярному выражению с обязательным таймаутом.
    /// </summary>
    public class TextRegexLogic
    {
        public const int DefaultTimeoutMs = 1000;
        public const int MaxTimeoutMs = 60000;

        /// <summary>Собирает RegexOptions из переключателей.</summary>
        public RegexOptions BuildOptions(bool ignoreCase, bool multiline, bool singleline)
        {
            var options = RegexOptions.None;
            if (ignoreCase) options |= RegexOptions.IgnoreCase;
            if (multiline) options |= RegexOptions.Multiline;
            if (singleline) options |= RegexOptions.Singleline;
            return options;
        }

        /// <summary>
        /// Компилирует Regex с обязательным match timeout (ReDoS-защита, issue #24/#30).
        /// </summary>
        public Regex BuildRegex(string pattern, RegexOptions options, int timeoutMs)
        {
            Guard.NotNullOrWhiteSpace(pattern, nameof(pattern));

            if (timeoutMs <= 0)
                timeoutMs = DefaultTimeoutMs;
            if (timeoutMs > MaxTimeoutMs)
                timeoutMs = MaxTimeoutMs;

            try
            {
                return new Regex(pattern, options, TimeSpan.FromMilliseconds(timeoutMs));
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException($"Некорректное регулярное выражение: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Находит все совпадения и возвращает DataTable:
        /// колонки Match (значение), Index (позиция) + группа 1..N и именованные группы.
        /// Несовпавшая (опциональная) группа — пустая строка.
        /// </summary>
        public DataTable FindMatches(string input, Regex regex)
        {
            Guard.NotNull(input, nameof(input));
            Guard.NotNull(regex, nameof(regex));

            var table = new DataTable("RegexMatches");
            table.Columns.Add("Match", typeof(string));
            table.Columns.Add("Index", typeof(int));

            foreach (var name in regex.GetGroupNames())
            {
                if (name == "0")
                    continue; // группа 0 — всё совпадение, уже в Match
                table.Columns.Add(name, typeof(string));
            }

            foreach (Match match in regex.Matches(input))
            {
                var row = table.NewRow();
                row["Match"] = match.Value;
                row["Index"] = match.Index;

                foreach (var name in regex.GetGroupNames())
                {
                    if (name == "0")
                        continue;
                    var group = match.Groups[name];
                    // Несовпавшая опциональная группа: Success=false, значение пустое
                    row[name] = group.Success ? group.Value : string.Empty;
                }

                table.Rows.Add(row);
            }

            return table;
        }

        /// <summary>
        /// Заменяет все совпадения: подстановки $1, ${name}, ${name:-по-умолчанию}
        /// обрабатывает сам Regex.Replace.
        /// </summary>
        public string ReplaceMatches(string input, Regex regex, string replacement)
        {
            Guard.NotNull(input, nameof(input));
            Guard.NotNull(regex, nameof(regex));

            return regex.Replace(input, replacement ?? string.Empty);
        }

        /// <summary>
        /// Считает совпадения (для счётчика в выходе активности).
        /// </summary>
        public int CountMatches(string input, Regex regex)
        {
            Guard.NotNull(input, nameof(input));
            Guard.NotNull(regex, nameof(regex));

            return regex.Matches(input).Count;
        }

        /// <summary>
        /// Форматирует число для сообщений результата (InvariantCulture).
        /// </summary>
        public string FormatCount(int count)
        {
            return count.ToString(CultureInfo.InvariantCulture);
        }
    }
}
