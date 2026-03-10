// =============================================================================
// BrowserSessionContext.cs — ambient-контекст сессии браузера.
//
// Реализует паттерн "ambient context" через стек сессий, хранящийся в RepoDict.
// Контейнер BrowserOpenBack при старте кладёт sessionId в стек,
// при завершении — снимает. Активности внутри контейнера могут
// получить текущую сессию через BrowserSessionContext.Current,
// не зная ничего о контейнере.
//
// Поддерживает вложенные контейнеры — каждый Push добавляет свою сессию,
// каждый Pop возвращает к предыдущей.
//
// Использует RepoDict для хранения стека, что решает проблемы с многопоточностью
// и async/await в SDK Primo.
// =============================================================================

using System;
using System.Collections.Generic;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Статический ambient-контекст, хранящий стек ID сессий браузера в RepoDict.
    /// Позволяет активностям внутри контейнера получить sessionId
    /// без явной передачи через свойство Prop_SessionId.
    /// </summary>
    public static class BrowserSessionContext
    {
        // Ключ для хранения стека сессий в RepoDict
        private const string SESSION_STACK_KEY = "__BrowserSessionStack__";

        /// <summary>
        /// Возвращает ID текущей активной сессии браузера,
        /// или null если контекст пуст (активность запущена вне контейнера).
        /// </summary>
        public static string Current
        {
            get
            {
                var stack = GetStack();
                if (stack == null || stack.Count == 0)
                    return null;

                return stack.Peek();
            }
        }

        /// <summary>
        /// Помещает ID сессии в стек контекста.
        /// Вызывается контейнером BrowserOpenBack перед выполнением дочерних активностей.
        /// 
        /// ВАЖНО: При каждом вызове Push() из BrowserOpen автоматически вызывается Pop()
        /// для предыдущей сессии, что обеспечивает корректную работу вложенных контейнеров
        /// и автоматическую очистку при выходе из контейнера.
        /// </summary>
        /// <param name="sessionId">ID сессии браузера.</param>
        /// <param name="isContainerEntry">True если вызов из BrowserOpen (контейнер), false если из других мест.</param>
        /// <exception cref="ArgumentNullException">Если sessionId пуст.</exception>
        public static void Push(string sessionId, bool isContainerEntry = true)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId),
                    "ID сессии не может быть пустым при регистрации контекста");

            var stack = GetStack();
            if (stack == null)
            {
                stack = new Stack<string>();
                RepoDict.Set(SESSION_STACK_KEY, stack);
            }

            stack.Push(sessionId);
        }

        /// <summary>
        /// Снимает верхнюю сессию со стека контекста.
        /// Вызывается контейнером BrowserOpenBack в блоке finally.
        /// </summary>
        public static void Pop()
        {
            var stack = GetStack();
            if (stack != null && stack.Count > 0)
                stack.Pop();
        }

        /// <summary>
        /// Получает стек сессий из RepoDict.
        /// </summary>
        private static Stack<string> GetStack()
        {
            return RepoDict.Get<Stack<string>>(SESSION_STACK_KEY);
        }
    }
}
