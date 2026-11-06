// =============================================================================
// CalendarDateHelper.cs — парсинг дат для активностей календаря.
//
// Даты приходят из переменных процесса (строки или DateTime) и могут быть
// записаны в любой культуре. Порядок разбора:
//   1. InvariantCulture — детерминированные форматы (ISO "2024-01-15", ru "dd.MM.yyyy");
//   2. CurrentCulture   — совместимость с процессами, рассчитанными на локаль машины.
// =============================================================================

using System;
using System.Globalization;

namespace Primo.MIA
{
    /// <summary>
    /// Культуронезависимый разбор дат для активностей календаря.
    /// </summary>
    public static class CalendarDateHelper
    {
        /// <summary>
        /// Разбирает значение в DateTime: сначала инвариантная культура,
        /// затем культура машины. Ноль/null/пустая строка → false.
        /// </summary>
        public static bool TryParse(object value, out DateTime date)
        {
            string text = value?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                date = default;
                return false;
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces, out date))
                return true;

            return DateTime.TryParse(text, CultureInfo.CurrentCulture,
                DateTimeStyles.AllowWhiteSpaces, out date);
        }
    }
}
