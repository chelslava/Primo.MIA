// =============================================================================
// SafeTry.cs — безопасное выполнение с fallback-значением (issue #38).
//
// Пара к SafeDispose: для «попробуй, а при ошибке верни значение по
// умолчанию» (поиск элемента мог устареть, сессия могла умереть и т.п.).
// Выполняет делегат; исключение глушится, при включённом файловом логе
// (ActivityFileLogger.Enabled) фиксируется уровнем Debug — раньше такие
// анонимные catch { } молчали и не оставляли следов в диагностике.
// =============================================================================

using System;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Безопасное выполнение функции с fallback-значением.
    /// </summary>
    /// <example>
    /// <code>
    /// // было:
    /// try { found = element != null; } catch { found = false; }
    /// // стало:
    /// found = SafeTry.Result(() => repository.GetElement(id) != null, false, "поиск элемента");
    /// </code>
    /// </example>
    public static class SafeTry
    {
        /// <summary>
        /// Выполняет функцию, при исключении возвращает fallback.
        /// </summary>
        /// <typeparam name="T">Тип результата.</typeparam>
        /// <param name="action">Функция (может бросать).</param>
        /// <param name="fallback">Значение при исключении (по умолчанию для T).</param>
        /// <param name="what">Пометка для отладочного лога; может быть null.</param>
        /// <returns>Результат функции или fallback.</returns>
        public static T Result<T>(Func<T> action, T fallback = default, string what = null)
        {
            Guard.NotNull(action, nameof(action));

            try
            {
                return action();
            }
            catch (Exception ex)
            {
                // fallback-семантика — не критично; фиксируем при включённом логе
                if (ActivityFileLogger.Enabled)
                {
                    ActivityFileLogger.Debug(
                        what ?? nameof(SafeTry),
                        $"Fallback '{fallback}' после исключения: {ex.Message}");
                }

                return fallback;
            }
        }
    }
}
