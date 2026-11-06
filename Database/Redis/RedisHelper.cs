using StackExchange.Redis;
using System;
using System.Collections.Concurrent;

namespace Primo.MIA
{
    internal static class RedisHelper
    {
        private static readonly ConcurrentDictionary<string, ConnectionMultiplexer> _pool
            = new ConcurrentDictionary<string, ConnectionMultiplexer>();

        /// <summary>
        /// Возвращает БД из пула мультиплексоров. Мультиплексор кэшируется по
        /// эффективной строке подключения (connection string + syncTimeout),
        /// поэтому разные таймауты создают отдельные пулы соединений —
        /// это осознанный компромисс: StackExchange.Redis не поддерживает
        /// per-call таймаут для синхронных операций.
        /// </summary>
        /// <param name="connectionString">Строка подключения Redis.</param>
        /// <param name="databaseIndex">Индекс базы (0–15).</param>
        /// <param name="syncTimeoutSeconds">
        /// Таймаут синхронных операций в секундах; 0 — берётся из строки
        /// подключения или дефолт драйвера (5 с). Строки вида "syncTimeout=..."
        /// в самой connection string имеют приоритет.
        /// </param>
        public static IDatabase GetDatabase(string connectionString, int databaseIndex = 0, int syncTimeoutSeconds = 0)
        {
            var effective = ApplySyncTimeout(connectionString, syncTimeoutSeconds);
            var mux = _pool.GetOrAdd(effective, cs => ConnectionMultiplexer.Connect(cs));
            return mux.GetDatabase(databaseIndex);
        }

        /// <summary>
        /// Добавляет syncTimeout к строке подключения, если задан и ещё не указан в ней.
        /// </summary>
        private static string ApplySyncTimeout(string connectionString, int syncTimeoutSeconds)
        {
            if (syncTimeoutSeconds <= 0 || string.IsNullOrWhiteSpace(connectionString))
                return connectionString;

            // Пользовательская настройка в самой строке подключения имеет приоритет
            if (connectionString.IndexOf("syncTimeout", StringComparison.OrdinalIgnoreCase) >= 0)
                return connectionString;

            return connectionString + $";syncTimeout={syncTimeoutSeconds}";
        }
    }
}
