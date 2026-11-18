// =============================================================================
// RabbitMqSessionRegistry.cs — реестр открытых RabbitMQ-сессий manual-ack
// (issue #49).
//
// Delivery tag действителен только на канале, принявшем сообщение, поэтому
// RabbitMqConsume (режим «Держать сессию») НЕ закрывает соединение: оно
// регистрируется здесь, и RabbitMqAck позже подтверждает доставку на том же
// канале. Паттерн — DatabaseTransactionManager: потокобезопасный статический
// реестр + TTL-эвикция брошенных сессий при каждой регистрации (аварийно
// завершённый робот без Ack/CloseSession не держит соединение вечно).
// По одному сообщению на сессию: после Ack/Requeue/DLQ сессия закрывается.
// =============================================================================

using Primo.MIA.Common;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Открытая RabbitMQ-сессия manual-ack: живое соединение и канал, last
    /// delivery tag и данные сообщения (для requeue/DLQ-копии).
    /// </summary>
    public sealed class RabbitMqSessionHandle
    {
        /// <summary>Идентификатор сессии (GUID без дефисов).</summary>
        public string SessionId { get; set; }

        /// <summary>Живое соединение (владеет реестр).</summary>
        public IConnection Connection { get; set; }

        /// <summary>Канал, на котором доставлено сообщение (ack только на нём).</summary>
        public IModel Channel { get; set; }

        /// <summary>Очередь, из которой получено сообщение (для requeue-копии).</summary>
        public string Queue { get; set; }

        /// <summary>Delivery tag последней доставки в этой сессии.</summary>
        public ulong LastDeliveryTag { get; set; }

        /// <summary>Тело сообщения (для requeue/DLQ-копии).</summary>
        public string LastBody { get; set; }

        /// <summary>Счётчик x-retry-count полученного сообщения.</summary>
        public long LastRetryCount { get; set; }

        /// <summary>Момент регистрации (UTC) — для TTL-эвикции.</summary>
        public DateTime StartedAtUtc { get; set; }

        /// <summary>
        /// Закрывает канал и соединение (порядок: канал → соединение).
        /// Неподтверждённые сообщения брокер вернёт в очередь.
        /// </summary>
        public void Dispose()
        {
            SafeDispose.Quiet(
                () =>
                {
                    Channel?.Close();
                    Channel?.Dispose();
                },
                "RabbitMQ session channel close/dispose");

            SafeDispose.Quiet(
                () =>
                {
                    Connection?.Close();
                    Connection?.Dispose();
                },
                "RabbitMQ session connection close/dispose");

            Connection = null;
            Channel = null;
        }
    }

    /// <summary>
    /// Потокобезопасный реестр RabbitMQ-сессий manual-ack.
    /// </summary>
    public static class RabbitMqSessionRegistry
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, RabbitMqSessionHandle> Sessions =
            new Dictionary<string, RabbitMqSessionHandle>(StringComparer.Ordinal);

        /// <summary>
        /// TTL брошенной сессии (issue #49, по образцу транзакций #18): робот,
        /// упавший между Consume и Ack, не держит соединение вечно. Брокер при
        /// закрытии канала сам вернёт неподтверждённые сообщения в очередь.
        /// </summary>
        public static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

        /// <summary>
        /// Регистрирует сессию и эвицирует брошенные (старше TTL).
        /// </summary>
        /// <param name="connection">Живое соединение (владение переходит реестру).</param>
        /// <param name="channel">Канал доставки (владение переходит реестру).</param>
        /// <param name="queue">Очередь источника.</param>
        /// <param name="deliveryTag">Delivery tag полученного сообщения.</param>
        /// <param name="body">Тело сообщения (для requeue/DLQ).</param>
        /// <param name="retryCount">Счётчик x-retry-count сообщения.</param>
        /// <param name="nowUtc">Переопределение «сейчас» (тесты).</param>
        /// <param name="ttl">Переопределение TTL (тесты).</param>
        public static RabbitMqSessionHandle Register(
            IConnection connection,
            IModel channel,
            string queue,
            ulong deliveryTag,
            string body,
            long retryCount,
            DateTime? nowUtc = null,
            TimeSpan? ttl = null)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            var handle = new RabbitMqSessionHandle
            {
                SessionId = Guid.NewGuid().ToString("N"),
                Connection = connection,
                Channel = channel,
                Queue = queue,
                LastDeliveryTag = deliveryTag,
                LastBody = body,
                LastRetryCount = retryCount,
                StartedAtUtc = nowUtc ?? DateTime.UtcNow
            };

            lock (SyncRoot)
            {
                EvictExpiredLocked(nowUtc ?? DateTime.UtcNow, ttl ?? DefaultTtl);
                Sessions[handle.SessionId] = handle;
            }

            return handle;
        }

        /// <summary>Сессия по идентификатору; null — не найдена (истекла/закрыта).</summary>
        public static RabbitMqSessionHandle Get(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return null;

            lock (SyncRoot)
            {
                RabbitMqSessionHandle handle;
                return Sessions.TryGetValue(sessionId, out handle) ? handle : null;
            }
        }

        /// <summary>
        /// Закрывает и удаляет сессию. Неподтверждённые сообщения брокер
        /// возвращает в очередь. False — сессии не было (уже закрыта/эвицирована).
        /// </summary>
        public static bool Close(string sessionId)
        {
            RabbitMqSessionHandle handle;
            lock (SyncRoot)
            {
                if (string.IsNullOrWhiteSpace(sessionId) || !Sessions.TryGetValue(sessionId, out handle))
                    return false;
                Sessions.Remove(sessionId);
            }

            handle.Dispose();
            return true;
        }

        /// <summary>Число открытых сессий (диагностика, тесты).</summary>
        public static int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return Sessions.Count;
                }
            }
        }

        /// <summary>Полная очистка (тесты).</summary>
        public static void Clear()
        {
            lock (SyncRoot)
            {
                foreach (var pair in Sessions)
                    pair.Value.Dispose();
                Sessions.Clear();
            }
        }

        /// <summary>Эвикция брошенных сессий. Вызывается под <see cref="SyncRoot"/>.</summary>
        private static void EvictExpiredLocked(DateTime now, TimeSpan ttl)
        {
            List<string> staleIds = null;
            foreach (var pair in Sessions)
            {
                if (now - pair.Value.StartedAtUtc > ttl)
                    (staleIds ?? (staleIds = new List<string>())).Add(pair.Key);
            }

            if (staleIds == null)
                return;

            foreach (var id in staleIds)
            {
                RabbitMqSessionHandle handle;
                if (!Sessions.TryGetValue(id, out handle) || !Sessions.Remove(id))
                    continue;

                handle.Dispose();
                Common.ActivityFileLogger.Warning(
                    "RabbitMqSessionRegistry",
                    $"Сессия '{id}' закрыта автоматически: превышен TTL {(int)ttl.TotalHours} ч " +
                    "(защита от утечки соединений; неподтверждённые сообщения возвращены брокером в очередь).");
            }
        }
    }
}
