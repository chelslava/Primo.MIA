using System;
using System.Collections.Generic;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Ambient-контекст активной DB-транзакции через стек в RepoDict.
    /// </summary>
    public static class DatabaseTransactionContext
    {
        private const string StackKey = "__DatabaseTransactionStack__";

        public static string Current
        {
            get
            {
                var stack = GetStack();
                return stack != null && stack.Count > 0 ? stack.Peek() : null;
            }
        }

        public static void Push(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentNullException(nameof(transactionId));

            var stack = GetStack();
            if (stack == null)
            {
                stack = new Stack<string>();
                RepoDict.Set(StackKey, stack);
            }

            stack.Push(transactionId);
        }

        public static void Pop()
        {
            var stack = GetStack();
            if (stack != null && stack.Count > 0)
                stack.Pop();
        }

        private static Stack<string> GetStack()
        {
            return RepoDict.Get<Stack<string>>(StackKey);
        }
    }
}
