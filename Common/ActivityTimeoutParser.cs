// =============================================================================
// ActivityTimeoutParser.cs — парсинг строковых таймаутов активностей.
//
// Значения свойств активностей приходят из дизайнера Primo как строки
// (например, Prop_CommandTimeoutSeconds = "30"). До появления этого хелпера
// один и тот же паттерн разбора повторялся в ~30 getter'ах sdkTimeOut:
//
//     int.TryParse(Prop_X, out int s) && s > 0 ? s * 1000 : 30000
//
// Хелпер централизует разбор: положительное число секунд → миллисекунды
// (или TimeSpan), любое иное значение (null, не-число, ноль, отрицательное)
// → значение по умолчанию, заданное вызывающим кодом.
// =============================================================================

using System.Globalization;

namespace Primo.MIA
{
    /// <summary>
    /// Парсинг строковых таймаутов активностей: секунды → миллисекунды / TimeSpan.
    /// Централизует повторяющийся в активностях паттерн
    /// «int.TryParse &amp;&amp; &gt; 0 ? секунды * 1000 : значение по умолчанию».
    /// </summary>
    /// <remarks>
    /// Разбор выполняется в инвариантной культуре (InvariantCulture), чтобы
    /// поведение не зависело от региональных настроек робота.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override int sdkTimeOut
    /// {
    ///     get { return ActivityTimeoutParser.ParseSecondsToMs(Prop_TimeoutSeconds, 30000); }
    ///     set { }
    /// }
    /// </code>
    /// </example>
    public static class ActivityTimeoutParser
    {
        /// <summary>
        /// Разбирает строку с таймаутом в секундах и возвращает его в миллисекундах.
        /// </summary>
        /// <param name="value">Строка со значением таймаута в секундах (обычно значение свойства Prop_*).</param>
        /// <param name="defaultMs">Значение по умолчанию в миллисекундах — возвращается,
        /// если строка null/пустая, не разбирается в число или меньше либо равна нулю.</param>
        /// <returns>Таймаут в миллисекундах: положительные секунды * 1000, иначе <paramref name="defaultMs"/>.</returns>
        public static int ParseSecondsToMs(string value, int defaultMs)
        {
            return TryParsePositiveSeconds(value, out int seconds)
                ? seconds * 1000
                : defaultMs;
        }

        /// <summary>
        /// Разбирает строку с таймаутом в секундах и возвращает TimeSpan.
        /// </summary>
        /// <param name="value">Строка со значением таймаута в секундах (обычно значение свойства Prop_*).</param>
        /// <param name="defaultSeconds">Значение по умолчанию в секундах — возвращается,
        /// если строка null/пустая, не разбирается в число или меньше либо равна нулю.</param>
        /// <returns>TimeSpan.FromSeconds для положительных секунд, иначе — для <paramref name="defaultSeconds"/>.</returns>
        public static System.TimeSpan ParseSeconds(string value, int defaultSeconds)
        {
            return System.TimeSpan.FromSeconds(
                TryParsePositiveSeconds(value, out int seconds) ? seconds : defaultSeconds);
        }

        /// <summary>
        /// Разбирает строку в положительное число секунд (InvariantCulture).
        /// </summary>
        /// <param name="value">Исходная строка.</param>
        /// <param name="seconds">Результат разбора; 0 при неудаче.</param>
        /// <returns>true, если строка разбирается в целое число больше нуля.</returns>
        private static bool TryParsePositiveSeconds(string value, out int seconds)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds)
                && seconds > 0;
        }
    }
}
