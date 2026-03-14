// =============================================================================
// ComparisonHelper.cs — вспомогательные методы для сравнения строк.
//
// Централизует логику выбора StringComparison и StringComparer
// на основе флага CaseSensitive, используемого во многих активностях.
// =============================================================================

using System;

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
    }
}
