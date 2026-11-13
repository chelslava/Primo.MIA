// =============================================================================
// RegexCache.cs — кэш скомпилированных Regex + обязательный match timeout
// (issue #24, ReDoS-защита пользовательских паттернов).
//
// Паттерны приходят от пользователя сценария: катастрофический бэктрекинг
// ((a+)+$ на длинной строке) без таймаута подвешивает робота навсегда.
// Все места использования пользовательских regex обязаны идти через этот
// хелпер: кэш (задвоение компиляции в циклах убрано) + таймаут (fast-fail).
// Хранилище — RepoDict.RegexDict (глобальный кэш процесса робота, 1 робот
// на процесс). Таймаут входит в ключ: экземпляры с разными таймаутами
// не пересекаются.
// =============================================================================

using System;
using System.Text.RegularExpressions;

namespace Primo.MIA.Common
{
    /// <summary>Кэш скомпилированных Regex с обязательным match timeout.</summary>
    public static class RegexCache
    {
        /// <summary>Таймаут совпадения по умолчанию, мс (совпадает с Text:Regex).</summary>
        public const int DefaultTimeoutMs = 1000;

        /// <summary>Верхняя граница таймаута, мс.</summary>
        public const int MaxTimeoutMs = 60000;

        private static readonly string KeySeparator = "\u0001";

        /// <summary>
        /// Приводит таймаут (мс) к допустимому диапазону: null/&lt;=0 — по умолчанию,
        /// больше максимума — максимум.
        /// </summary>
        public static TimeSpan ResolveTimeout(int? timeoutMs = null)
        {
            var value = timeoutMs ?? DefaultTimeoutMs;
            if (value <= 0)
                value = DefaultTimeoutMs;
            if (value > MaxTimeoutMs)
                value = MaxTimeoutMs;
            return TimeSpan.FromMilliseconds(value);
        }

        /// <summary>
        /// Возвращает скомпилированный Regex из кэша (создаёт при отсутствии).
        /// Таймаут обязателен: null — по умолчанию (1 с), 0/&lt;=0 — по умолчанию,
        /// больше 60 с — 60 с.
        /// </summary>
        public static Regex Get(
            string pattern,
            RegexOptions options = RegexOptions.Compiled,
            int? timeoutMs = null)
        {
            Guard.NotNullOrWhiteSpace(pattern, nameof(pattern));
            var timeout = ResolveTimeout(timeoutMs);

            var key = pattern + KeySeparator + (int)options + KeySeparator
                      + timeout.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture);

            return RepoDict.RegexDict.GetOrAdd(key, _ => new Regex(pattern, options, timeout));
        }
    }
}
