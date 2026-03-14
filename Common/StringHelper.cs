// =============================================================================
// StringHelper.cs — вспомогательные методы для работы со строками.
//
// Содержит общую логику парсинга и валидации строк,
// используемую в различных активностях.
// =============================================================================

using System.Globalization;
using System.Linq;
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

            if (double.TryParse(s, NumberStyles.Any,
                CultureInfo.InvariantCulture, out double d))
                return d;

            if (double.TryParse(s, NumberStyles.Any,
                CultureInfo.CurrentCulture, out double d2))
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
    }
}
