using System;
using System.Collections.Concurrent;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Ambient-контекст активной DB-транзакции через стек в RepoDict.
    /// Стек конкурентный (ConcurrentStack) — Push/Pop безопасны из параллельных веток робота.
    /// </summary>
    public static class DatabaseTransactionContext
    {
        private const string StackKey = "__DatabaseTransactionStack__";

        public static string Current
        {
            get
            {
                var stack = GetStack();
                return stack != null && stack.TryPeek(out var current) ? current : null;
            }
        }

        public static void Push(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentNullException(nameof(transactionId));

            // AddOrUpdate атомарен: две ветки не создадут два разных стека
            var stack = (ConcurrentStack<string>)RepoDict.ObjectDict.AddOrUpdate(
                StackKey,
                _ => new ConcurrentStack<string>(),
                (_, existing) => existing as ConcurrentStack<string> ?? new ConcurrentStack<string>());
            stack.Push(transactionId);
        }

        public static void Pop()
        {
            var stack = GetStack();
            stack?.TryPop(out _);
        }

        private static ConcurrentStack<string> GetStack()
        {
            return RepoDict.Get<ConcurrentStack<string>>(StackKey);
        }
    }
}
