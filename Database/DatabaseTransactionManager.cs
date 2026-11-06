using System;
using System.Collections.Generic;
using System.Data;

namespace Primo.MIA
{
    /// <summary>
    /// Потокобезопасный реестр открытых транзакций БД.
    /// </summary>
    public static class DatabaseTransactionManager
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, DatabaseTransactionHandle> Transactions =
            new Dictionary<string, DatabaseTransactionHandle>();

        /// <summary>
        /// issue #18: TTL брошенной транзакции. Аварийно завершённый робот (без
        /// Commit/Rollback) оставлял открытое соединение в реестре навсегда.
        /// При каждом Begin транзакции старше TTL откатываются и закрываются.
        /// </summary>
        private static readonly TimeSpan AbandonedTransactionTtl = TimeSpan.FromHours(24);

        public static DatabaseTransactionHandle Begin(
            string providerInvariantName,
            string connectionString,
            DatabaseIsolationLevel isolationLevel)
        {
            var connection = DatabaseHelper.OpenConnection(providerInvariantName, connectionString);
            var transaction = connection.BeginTransaction(MapIsolationLevel(isolationLevel));
            var handle = new DatabaseTransactionHandle
            {
                TransactionId = Guid.NewGuid().ToString("N"),
                ProviderInvariantName = providerInvariantName,
                ConnectionString = connectionString,
                Connection = connection,
                Transaction = transaction,
                StartedAtUtc = DateTime.UtcNow
            };

            lock (SyncRoot)
            {
                EvictAbandonedLocked(DateTime.UtcNow);
                Transactions[handle.TransactionId] = handle;
            }

            return handle;
        }

        /// <summary>
        /// Откатывает и закрывает транзакции, «забытые» дольше
        /// <see cref="AbandonedTransactionTtl"/>. Вызывается под <see cref="SyncRoot"/>.
        /// </summary>
        private static void EvictAbandonedLocked(DateTime now)
        {
            List<string> staleIds = null;
            foreach (var pair in Transactions)
            {
                if (now - pair.Value.StartedAtUtc > AbandonedTransactionTtl)
                    (staleIds ?? (staleIds = new List<string>())).Add(pair.Key);
            }

            if (staleIds == null)
                return;

            foreach (var id in staleIds)
            {
                DatabaseTransactionHandle handle;
                if (!Transactions.TryGetValue(id, out handle) || !Transactions.Remove(id))
                    continue;

                try
                {
                    if (!handle.IsCompleted)
                        handle.Transaction?.Rollback();
                    handle.IsCompleted = true;
                }
                catch
                {
                    // соединение может быть уже разорвано — не критично
                }
                finally
                {
                    handle.Dispose();
                }

                Common.ActivityFileLogger.Warning(
                    "DatabaseTransactionManager",
                    $"Транзакция '{id}' откатана и закрыта автоматически: " +
                    "превышен TTL 24 ч (защита от утечки соединений).");
            }
        }

        public static DatabaseTransactionHandle Get(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                return null;

            lock (SyncRoot)
            {
                DatabaseTransactionHandle handle;
                return Transactions.TryGetValue(transactionId, out handle) ? handle : null;
            }
        }

        public static bool Exists(string transactionId)
        {
            lock (SyncRoot)
            {
                return !string.IsNullOrWhiteSpace(transactionId) && Transactions.ContainsKey(transactionId);
            }
        }

        public static void Commit(string transactionId)
        {
            var handle = RemoveInternal(transactionId);
            if (handle == null)
                throw new InvalidOperationException($"Транзакция '{transactionId}' не найдена.");

            try
            {
                handle.Transaction.Commit();
                handle.IsCompleted = true;
            }
            finally
            {
                handle.Dispose();
            }
        }

        public static void Rollback(string transactionId)
        {
            var handle = RemoveInternal(transactionId);
            if (handle == null)
                throw new InvalidOperationException($"Транзакция '{transactionId}' не найдена.");

            try
            {
                handle.Transaction.Rollback();
                handle.IsCompleted = true;
            }
            finally
            {
                handle.Dispose();
            }
        }

        private static DatabaseTransactionHandle RemoveInternal(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                return null;

            lock (SyncRoot)
            {
                DatabaseTransactionHandle handle;
                if (!Transactions.TryGetValue(transactionId, out handle))
                    return null;

                Transactions.Remove(transactionId);
                return handle;
            }
        }

        private static IsolationLevel MapIsolationLevel(DatabaseIsolationLevel level)
        {
            switch (level)
            {
                case DatabaseIsolationLevel.ReadCommitted:
                    return IsolationLevel.ReadCommitted;
                case DatabaseIsolationLevel.ReadUncommitted:
                    return IsolationLevel.ReadUncommitted;
                case DatabaseIsolationLevel.RepeatableRead:
                    return IsolationLevel.RepeatableRead;
                case DatabaseIsolationLevel.Serializable:
                    return IsolationLevel.Serializable;
                case DatabaseIsolationLevel.Snapshot:
                    return IsolationLevel.Snapshot;
                default:
                    return IsolationLevel.Unspecified;
            }
        }
    }
}
