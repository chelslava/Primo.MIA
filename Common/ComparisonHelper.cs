// =============================================================================
// ComparisonHelper.cs — вспомогательные методы для сравнения строк.
//
// Централизует логику выбора StringComparison и StringComparer
// на основе флага CaseSensitive, используемого во многих активностях.
// =============================================================================

using System;
using System.Text.RegularExpressions;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Вспомогательные методы для работы со сравнением строк.
    /// Упрощает выбор правильного компаратора на основе флага CaseSensitive.
    /// </summary>
    public static class ComparisonHelper
    {
        /// <summary>
        /// Возвращает StringComparison на основе флага учёта регистра.
        /// </summary>
        /// <param name="caseSensitive">True — учитывать регистр, False — игнорировать</param>
        /// <returns>StringComparison.Ordinal или StringComparison.OrdinalIgnoreCase</returns>
        public static StringComparison GetStringComparison(bool caseSensitive)
        {
            return caseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;
        }

        /// <summary>
        /// Возвращает StringComparer на основе флага учёта регистра.
        /// Используется для LINQ операций (Distinct, Union, Intersect и т.д.).
        /// </summary>
        /// <param name="caseSensitive">True — учитывать регистр, False — игнорировать</param>
        /// <returns>StringComparer.Ordinal или StringComparer.OrdinalIgnoreCase</returns>
        public static StringComparer GetStringComparer(bool caseSensitive)
        {
            return caseSensitive
                ? StringComparer.Ordinal
                : StringComparer.OrdinalIgnoreCase;
        }

        /// <summary>
        /// Создаёт скомпилированный Regex с учётом флага регистра.
        /// </summary>
        /// <param name="pattern">Regex-паттерн</param>
        /// <param name="caseSensitive">True — учитывать регистр, False — игнорировать</param>
        /// <returns>Скомпилированный Regex</returns>
        /// <exception cref="ArgumentException">Если паттерн некорректен</exception>
        public static Regex CreateRegex(string pattern, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException("Regex-паттерн не может быть пустым", nameof(pattern));

            var options = RegexOptions.Compiled;
            if (!caseSensitive)
                options |= RegexOptions.IgnoreCase;

            try
            {
                return new Regex(pattern, options);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Некорректный regex-паттерн '{pattern}': {ex.Message}", nameof(pattern), ex);
            }
        }

        /// <summary>
        /// Проверяет содержит ли строка подстроку с учётом флага регистра.
        /// </summary>
        /// <param name="source">Исходная строка</param>
        /// <param name="value">Искомая подстрока</param>
        /// <param name="caseSensitive">True — учитывать регистр, False — игнорировать</param>
        /// <returns>True если source содержит value</returns>
        public static bool Contains(string source, string value, bool caseSensitive)
        {
            if (source == null || value == null)
                return false;

            return source.IndexOf(value, GetStringComparison(caseSensitive)) >= 0;
        }

        /// <summary>
        /// Проверяет равны ли две строки с учётом флага регистра.
        /// </summary>
        /// <param name="a">Первая строка</param>
        /// <param name="b">Вторая строка</param>
        /// <param name="caseSensitive">True — учитывать регистр, False — игнорировать</param>
        /// <returns>True если строки равны</returns>
        public static bool Equals(string a, string b, bool caseSensitive)
        {
            return string.Equals(a, b, GetStringComparison(caseSensitive));
        }
    }
}
