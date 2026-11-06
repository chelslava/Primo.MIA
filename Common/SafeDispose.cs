// =============================================================================
// SafeDispose.cs — безопасное освобождение ресурсов.
//
// При закрытии соединений/каналов (SSH, RabbitMQ, HTTP и т.п.) в блоках finally
// исключения освобождения не должны маскировать результат активности. Хелпер
// выполняет dispose-делегат и глушит любое исключение, при включённом файловом
// логе (ActivityFileLogger.Enabled) фиксируя его на уровне Debug.
// =============================================================================

using System;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Безопасное освобождение ресурсов: выполняет делегат и глушит исключения.
    /// </summary>
    /// <example>
    /// <code>
    /// finally
    /// {
    ///     // Порядок освобождения сохраняется вызывающим кодом:
    ///     SafeDispose.Quiet(() => { client?.Disconnect(); client?.Dispose(); }, "SFTP disconnect");
    /// }
    /// </code>
    /// </example>
    public static class SafeDispose
    {
        /// <summary>
        /// Выполняет dispose-делегат, глуша возможное исключение.
        /// </summary>
        /// <param name="dispose">Делегат освобождения ресурсов (порядок вызовов внутри — на усмотрение вызывающего).</param>
        /// <param name="what">Пометка для отладочного лога (что освобождаем); может быть null.</param>
        /// <remarks>
        /// При <see cref="ActivityFileLogger.Enabled"/> = true исключение записывается в лог уровнем Debug,
        /// иначе — полностью игнорируется. Никогда не бросает исключений сам.
        /// </remarks>
        public static void Quiet(Action dispose, string what = null)
        {
            Guard.NotNull(dispose, nameof(dispose));

            try
            {
                dispose();
            }
            catch (Exception ex)
            {
                // освобождение уже разорванного ресурса — не критично; фиксируем при включённом логе
                if (ActivityFileLogger.Enabled)
                {
                    ActivityFileLogger.Debug(
                        what ?? nameof(SafeDispose),
                        $"Исключение при освобождении ресурсов: {ex.Message}");
                }
            }
        }
    }
}
