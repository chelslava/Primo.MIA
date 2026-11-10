// =============================================================================
// BrowserSessionRegistry.cs — реестр живых сессий браузера с TTL-свипом.
//
// issue #20: при аварийном прерывании робота между BrowserOpen и BrowserClose
// ни один механизм платформы не вызывает Quit() — WebDriver и процесс браузера
// оставались в системе до смерти хоста. SDK не предоставляет хук на abort,
// поэтому применяется тот же приём, что и в DatabaseTransactionManager
// (issue #18): при каждом Push новой сессии «заброшенные» сессии старше TTL
// закрываются и удаляются из RepoDict.
//
// TTL консервативный (24 ч — как у транзакций): легитимный робот, живущий
// дольше суток с открытым браузером, потеряет сессию — это документированное
// ограничение (предполагается один робот на процесс).
// =============================================================================

using OpenQA.Selenium;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Потокобезопасный реестр живых сессий браузера с автоматической очисткой
    /// «заброшенных» сессий по TTL. Дополняет <see cref="SessionManager"/>,
    /// который хранит только ссылки без времени жизни.
    /// </summary>
    public static class BrowserSessionRegistry
    {
        private static readonly object SyncRoot = new object();

        /// <summary>sessionId → момент открытия сессии (UTC).</summary>
        private static readonly Dictionary<string, DateTime> RegisteredAtUtc =
            new Dictionary<string, DateTime>();

        /// <summary>
        /// TTL «заброшенной» сессии. Консервативно — 24 ч, как у
        /// <see cref="DatabaseTransactionManager"/> (issue #18/#20).
        /// </summary>
        public static readonly TimeSpan DefaultAbandonedSessionTtl = TimeSpan.FromHours(24);

        /// <summary>
        /// Регистрирует новую сессию и заодно выметает заброшенные (старше TTL).
        /// Вызывается из BrowserOpenBack при создании сессии.
        /// </summary>
        /// <param name="sessionId">ID сессии.</param>
        /// <param name="nowUtc">Момент регистрации; null = текущее время (параметр для тестов).</param>
        public static void Register(string sessionId, DateTime? nowUtc = null)
        {
            Guard.NotNullOrWhiteSpace(sessionId, nameof(sessionId));

            var now = nowUtc ?? DateTime.UtcNow;

            lock (SyncRoot)
            {
                EvictAbandonedLocked(now, DefaultAbandonedSessionTtl);
                RegisteredAtUtc[sessionId] = now;
            }
        }

        /// <summary>
        /// Снимает сессию с учёта. Вызывается из BrowserCloseBack после Quit().
        /// </summary>
        public static void Unregister(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            lock (SyncRoot)
            {
                RegisteredAtUtc.Remove(sessionId);
            }
        }

        /// <summary>
        /// Закрывает (Quit + Dispose) и удаляет из RepoDict сессии, открытые
        /// дольше <paramref name="ttl"/> назад. Возвращает список выселенных ID.
        /// </summary>
        /// <param name="ttl">Порог «заброшенности».</param>
        /// <param name="nowUtc">Текущий момент; null = UtcNow (параметр для тестов).</param>
        public static IReadOnlyList<string> EvictAbandoned(TimeSpan ttl, DateTime? nowUtc = null)
        {
            if (ttl <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ttl), "TTL должен быть положительным");

            List<string> evicted;
            lock (SyncRoot)
            {
                evicted = EvictAbandonedLocked(nowUtc ?? DateTime.UtcNow, ttl);
            }

            return evicted ?? (IReadOnlyList<string>)Array.Empty<string>();
        }

        /// <summary>
        /// Число сессий на учёте (для тестов и диагностики).
        /// </summary>
        public static int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return RegisteredAtUtc.Count;
                }
            }
        }

        /// <summary>
        /// Выметает заброшенные сессии. Вызывается под <see cref="SyncRoot"/>
        /// (Quit может быть небыстрым — принят тот же компромисс, что и в
        /// <see cref="DatabaseTransactionManager.EvictAbandonedLocked"/>).
        /// </summary>
        private static List<string> EvictAbandonedLocked(DateTime now, TimeSpan ttl)
        {
            List<string> staleIds = null;
            foreach (var pair in RegisteredAtUtc)
            {
                if (now - pair.Value > ttl)
                    (staleIds ?? (staleIds = new List<string>())).Add(pair.Key);
            }

            if (staleIds == null)
                return null;

            foreach (var id in staleIds)
            {
                RegisteredAtUtc.Remove(id);

                // Quit может упасть (драйвер уже мёртв) — освобождение глушится
                var driver = RepoDict.Get<IWebDriver>(id);
                if (driver != null)
                {
                    SafeDispose.Quiet(() => driver.Quit(), $"BrowserSessionRegistry: Quit сессии {id}");
                    SafeDispose.Quiet(() => driver.Dispose(), $"BrowserSessionRegistry: Dispose сессии {id}");
                }
                RepoDict.Remove(id);

                ActivityFileLogger.Warning(
                    "BrowserSessionRegistry",
                    $"Сессия браузера '{id}' закрыта и удалена автоматически: " +
                    $"превышен TTL {ttl} (защита от утечки WebDriver, issue #20).");
            }

            return staleIds;
        }
    }
}
