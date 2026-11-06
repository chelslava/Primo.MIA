// =============================================================================
// ActivityLogger.cs — реализация IActivityLogger
//
// Структурированное логирование для активностей Browser модуля.
// Использует LTools.Workflow.PrimoApp.AddToLog для интеграции с платформой.
//
// Особенности:
//   - Структурированный формат логов
//   - Контекст активности в каждом сообщении
//   - Форматирование с параметрами
//   - Интеграция с платформой Primo через PrimoApp
// =============================================================================

using System;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Логгер для активностей с структурированным форматом.
    /// Пишет в консоль и (при включённом ActivityFileLogger.Enabled) в файл лога.
    /// </summary>
    public class ActivityLogger : IActivityLogger
    {
        /// <summary>
        /// Логирует информационное сообщение.
        /// </summary>
        public void LogInfo(string activityName, string message, params object[] args)
        {
            ActivityFileLogger.Info(activityName, FormatMessage(activityName, message, args));
        }

        /// <summary>
        /// Логирует предупреждение.
        /// </summary>
        public void LogWarning(string activityName, string message, params object[] args)
        {
            ActivityFileLogger.Warning(activityName, FormatMessage(activityName, message, args));
        }

        /// <summary>
        /// Логирует ошибку.
        /// </summary>
        public void LogError(string activityName, Exception ex, string message, params object[] args)
        {
            var formattedMessage = FormatMessage(activityName, message, args);

            if (ex != null)
            {
                formattedMessage += $"\nException: {ex.GetType().Name}\nMessage: {ex.Message}";

                if (ex.InnerException != null)
                {
                    formattedMessage += $"\nInner Exception: {ex.InnerException.Message}";
                }
            }

            ActivityFileLogger.Error(activityName, formattedMessage);
        }

        /// <summary>
        /// Логирует отладочное сообщение.
        /// </summary>
        public void LogDebug(string activityName, string message, params object[] args)
        {
            ActivityFileLogger.Debug(activityName, FormatMessage(activityName, message, args));
        }

        /// <summary>
        /// Форматирует сообщение с контекстом активности.
        /// </summary>
        private string FormatMessage(string activityName, string message, params object[] args)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var formattedMsg = args != null && args.Length > 0
                ? string.Format(message, args)
                : message;

            return $"[{timestamp}] [{activityName}] {formattedMsg}";
        }
    }
}
