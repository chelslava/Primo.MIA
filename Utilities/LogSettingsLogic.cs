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

        /// <summary>
        /// Уровень сообщения активности «Логирование» проходит глобальный
        /// фильтр? None подавляет всё; Info скрывает «Отладка»; Debug — все.
        /// Warning/Error/Critical имеют ранг Info (как в ActivityFileLogger).
        /// </summary>
        public static bool PassesMessageLevel(LogLevel messageLevel)
        {
            if (ActivityFileLogger.MinLevel == ActivityLogLevel.None)
                return false;
            var messageRank = messageLevel == LogLevel.Debug
                ? (int)ActivityLogLevel.Debug
                : (int)ActivityLogLevel.Info;
            return messageRank >= (int)ActivityFileLogger.MinLevel;
        }
    }
}
