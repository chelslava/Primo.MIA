// =============================================================================
// LogSettingsLogic.cs — применяемая логика «Лог: Настройки» (тестируемая).
// =============================================================================

using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>Применение настроек логирования к ActivityFileLogger.</summary>
    public static class LogSettingsLogic
    {
        /// <summary>
        /// Применяет уровень, файловый sink и каталог к статике логгера.
        /// Каталог в кавычках/пробелах очищается; пустой — оставляет текущий.
        /// Возврат — фактически действующий каталог лога.
        /// </summary>
        public static string Apply(ActivityLogLevel level, bool fileLog, string directory)
        {
            ActivityFileLogger.MinLevel = level;
            ActivityFileLogger.Enabled = fileLog;

            var clean = (directory ?? string.Empty).Trim().Trim('"').Trim();
            if (!string.IsNullOrWhiteSpace(clean))
                ActivityFileLogger.LogDirectory = clean;

            return ActivityFileLogger.LogDirectory;
        }
    }
}
