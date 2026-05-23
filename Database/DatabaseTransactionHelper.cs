using System;

namespace Primo.MIA
{
    public static class DatabaseTransactionHelper
    {
        public static void EnsureTransactionHandle(DatabaseTransactionHandle transactionHandle)
        {
            if (transactionHandle == null)
                throw new InvalidOperationException("Транзакция БД не найдена.");
            if (transactionHandle.Connection == null || transactionHandle.Transaction == null)
                throw new InvalidOperationException("Транзакция БД не инициализирована корректно.");
        }
    }
}
