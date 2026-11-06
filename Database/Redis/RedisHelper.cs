using StackExchange.Redis;
using System;
using System.Collections.Concurrent;

namespace Primo.MIA
{
    /// <summary>
    /// Пул мультиплексоров Redis. Мультиплексор кэшируется по эффективной
    /// строке подключения (connection string + syncTimeout), поэтому разные
    /// таймауты создают отдельные пулы соединений — осознанный компромисс:
    /// StackExchange.Redis не поддерживает per-call таймаут для синхронных
    /// операций.
    /// <para>
    /// issue #18: вытеснение неактивных мультиплексоров. Раньше записи жили
    /// вечно: невалидная строка (ошибочный хост) оставалась в пуле, а
    /// варианты «на каждый таймаут» накапливались за время жизни процесса.
    /// Теперь записи, к которым не обращались дольше <see cref="IdleTtl"/>,
    /// выводятся из пула; dispose — после <see cref="RetireGrace"/> (защита
    /// in-flight операций; паттерн как в <see cref="HttpClientPool"/>).
    /// Вытеснение ленивое, при вызовах GetDatabase — без таймерных потоков.
    /// </para>
    /// </summary>
    internal static class RedisHelper
    {
        /// <summary>Время, после которого неиспользуемый мультиплексор выводится из пула.</summary>
        private static readonly TimeSpan IdleTtl = TimeSpan.FromMinutes(30);

        /// <summary>Отсрочка dispose выведенного мультиплекса (in-flight операции).</summary>
        private static readonly TimeSpan RetireGrace = TimeSpan.FromMinutes(30);

        private sealed class PoolEntry
        {
            public ConnectionMultiplexer Mux;
            public DateTime LastAccessUtc;
            public DateTime RetireAtUtc = DateTime.MaxValue;
        }

        private static readonly ConcurrentDictionary<string, Lazy<PoolEntry>> Pool =
            new ConcurrentDictionary<string, Lazy<PoolEntry>>(StringComparer.Ordinal);

        private static readonly ConcurrentQueue<PoolEntry> Retired =
            new ConcurrentQueue<PoolEntry>();

        /// <summary>
        /// Возвращает БД из пула мультиплексоров (с ленивым вытеснением неактивных).
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
            var now = DateTime.UtcNow;
            PurgeRetired(now);
            EvictIdle(now);

            // Lazy: двойное подключение при гонке GetOrAdd исключено;
            // исключение в Connect не оставляет записи в пуле.
            var entry = Pool.GetOrAdd(
                ApplySyncTimeout(connectionString, syncTimeoutSeconds),
                cs => new Lazy<PoolEntry>(() => new PoolEntry
                {
                    Mux = ConnectionMultiplexer.Connect(cs),
                    LastAccessUtc = DateTime.UtcNow
                })).Value;

            entry.LastAccessUtc = DateTime.UtcNow;
            return entry.Mux.GetDatabase(databaseIndex);
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

        /// <summary>Выводит из пула мультиплексоры без обращений дольше <see cref="IdleTtl"/>.</summary>
        private static void EvictIdle(DateTime now)
        {
            foreach (var pair in Pool)
            {
                var entry = pair.Value.IsValueCreated ? pair.Value.Value : null;
                if (entry == null || now - entry.LastAccessUtc <= IdleTtl)
                    continue;
                if (!Pool.TryRemove(pair.Key, out _))
                    continue;
                entry.RetireAtUtc = now;
                Retired.Enqueue(entry);
            }
        }

        /// <summary>Dispose выведенных мультиплексоров, у которых истёк grace-период.</summary>
        private static void PurgeRetired(DateTime now)
        {
            while (Retired.TryPeek(out var oldest) && now - oldest.RetireAtUtc > RetireGrace)
            {
                if (!Retired.TryDequeue(out var entry))
                    break;
                Common.SafeDispose.Quiet(() => entry.Mux.Dispose(), "RedisHelper: dispose multiplexer");
            }
        }
    }
}
