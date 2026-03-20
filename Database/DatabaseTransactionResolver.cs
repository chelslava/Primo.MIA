using Primo.MIA.Common;
using System;

namespace Primo.MIA
{
    /// <summary>
    /// Разрешает transactionId с учётом ambient-контекста.
    /// </summary>
    public static class DatabaseTransactionResolver
    {
        public static string ResolveOptional(string transactionId)
        {
            if (!string.IsNullOrWhiteSpace(transactionId))
                return transactionId;

            return DatabaseTransactionContext.Current;
        }

        public static string ResolveRequired(string transactionId)
        {
            var resolved = ResolveOptional(transactionId);
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved;

            throw new ArgumentException(
                "ID транзакции не задан и активность запущена вне контекста открытой DB-транзакции.");
        }
    }
}
