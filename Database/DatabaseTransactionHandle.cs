using System;
using System.Data.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Открытая транзакция БД вместе с соединением и метаданными.
    /// </summary>
    public sealed class DatabaseTransactionHandle : IDisposable
    {
        public string TransactionId { get; set; }

        public string ProviderInvariantName { get; set; }

        public string ConnectionString { get; set; }

        public DbConnection Connection { get; set; }

        public DbTransaction Transaction { get; set; }

        public DateTime StartedAtUtc { get; set; }

        public bool IsCompleted { get; set; }

        public void Dispose()
        {
            try
            {
                Transaction?.Dispose();
            }
            finally
            {
                Connection?.Dispose();
            }
        }
    }
}
