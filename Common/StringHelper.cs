// =============================================================================
// StringHelper.cs — вспомогательные методы для работы со строками.
//
// Содержит общую логику парсинга, конвертации и валидации строк,
// используемую в различных активностях.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Вспомогательные методы для работы со строками.
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// Безопасный парсинг double с поддержкой точки и запятой как разделителя.
        /// </summary>
        public static double? TryParseDouble(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double d))
                return d;

            if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double d2))
                return d2;

            return null;
        }

        /// <summary>
        /// Безопасный парсинг int.
        /// </summary>
        public static int? TryParseInt(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            if (int.TryParse(s, out int result))
                return result;

            return null;
        }

        /// <summary>
        /// Простой CSV-парсер поддерживающий кавычки и экранирование удвоением.
        /// "value 1","value ""quoted""",plain → ["value 1", "value \"quoted\"", "plain"]
        /// </summary>
        public static List<string> ParseCsvRow(string row)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(row)) return result;

            int i = 0;
            while (i < row.Length)
            {
                if (row[i] == '"')
                {
                    // Quoted field
                    var sb = new StringBuilder();
                    i++; // пропускаем открывающую кавычку
                    while (i < row.Length)
                    {
                        if (row[i] == '"' && i + 1 < row.Length && row[i + 1] == '"')
                        {
                            sb.Append('"'); i += 2; // двойная кавычка → одиночная
                        }
                        else if (row[i] == '"')
                        {
                            i++; break; // закрывающая кавычка
                        }
                        else
                        {
                            sb.Append(row[i++]);
                        }
                    }
                    result.Add(sb.ToString());
                    if (i < row.Length && row[i] == ',') i++; // пропускаем запятую
                }
                else
                {
                    // Unquoted field
                    int start = i;
                    while (i < row.Length && row[i] != ',') i++;
                    result.Add(row.Substring(start, i - start));
                    if (i < row.Length) i++; // пропускаем запятую
                }
            }

            return result;
        }

        /// <summary>
        /// Конвертирует список строк в CSV-строку с экранированием кавычек.
        /// </summary>
        public static string ToCsvRow(List<string> items)
        {
            if (items == null || items.Count == 0)
                return string.Empty;

            return string.Join(",",
                items.Select(s => "\"" + (s ?? string.Empty).Replace("\"", "\"\"") + "\""));
        }

        /// <summary>
        /// Транслирует wildcard-паттерн (* и ?) в regex-паттерн.
        /// </summary>
        public static string WildcardToRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return ".*";

            return "^" + string.Concat(
                pattern.Select(c =>
                    c == '*' ? ".*"
                  : c == '?' ? "."
                  : Regex.Escape(c.ToString()))) + "$";
        }

        /// <summary>
        /// Нормализует разделитель: убирает кавычки и пробелы.
        /// </summary>
        public static string NormalizeSeparator(string separator, string defaultValue = ";")
        {
            if (string.IsNullOrEmpty(separator))
                return defaultValue;

            return separator.Trim().Trim('"');
        }

        /// <summary>
        /// Проверяет является ли строка числом.
        /// </summary>
        public static bool IsNumeric(string s)
        {
            return TryParseDouble(s).HasValue;
        }
    }
}
