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
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Статический ambient-контекст, хранящий стек ID сессий браузера в RepoDict.
    /// Позволяет активностям внутри контейнера получить sessionId
    /// без явной передачи через свойство Prop_SessionId.
    ///
    /// issue #20: снимается конкретная сессия (Pop(sessionId)), а не вершина стека —
    /// два чередующихся контейнера больше не снимают записи друг друга.
    /// Хранилище — lock-защищённый <see cref="SessionStack"/> (тот же подход, что
    /// в DatabaseTransactionManager): Pop(sessionId) атомарен и всегда находит свою
    /// сессию, в отличие от «размотай-и-верни» над ConcurrentStack, где параллельный
    /// Pop мог транзиентно не найти запись.
    /// Предполагается один робот на процесс (ограничение платформы Primo Studio).
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
                return stack?.Peek();
            }
        }

        /// <summary>
        /// Помещает ID сессии в стек контекста.
        /// Вызывается контейнером BrowserOpenBack перед выполнением дочерних активностей.
        /// Поддерживает вложенные контейнеры: каждый Push добавляет свою запись,
        /// соответствующий Pop(sessionId) снимает её.
        /// </summary>
        /// <param name="sessionId">ID сессии браузера.</param>
        /// <param name="isContainerEntry">True если вызов из BrowserOpen (контейнер), false если из других мест.</param>
        /// <exception cref="ArgumentNullException">Если sessionId пуст.</exception>
        public static void Push(string sessionId, bool isContainerEntry = true)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId),
                    "ID сессии не может быть пустым при регистрации контекста");

            // AddOrUpdate атомарен: две ветки не создадут два разных стека
            var stack = (SessionStack)RepoDict.ObjectDict.AddOrUpdate(
                SESSION_STACK_KEY,
                _ => new SessionStack(),
                (_, existing) => existing as SessionStack ?? new SessionStack());
            stack.Push(sessionId);
        }

        /// <summary>
        /// Снимает верхнюю сессию со стека контекста (устаревшая форма — без привязки к сессии).
        /// </summary>
        public static void Pop()
        {
            var stack = GetStack();
            stack?.PopAny();
        }

        /// <summary>
        /// Снимает из контекста конкретную сессию — не обязательно вершину.
        /// issue #20: контейнер закрывает СВОЮ сессию; чередующиеся/вложенные
        /// контейнеры не снимают записи друг друга. Атомарно: порядок остальных
        /// записей не меняется.
        /// </summary>
        /// <param name="sessionId">ID сессии для снятия.</param>
        /// <returns>true если сессия была найдена и снята.</returns>
        public static bool Pop(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            var stack = GetStack();
            return stack != null && stack.PopValue(sessionId);
        }

        /// <summary>
        /// Получает стек сессий из RepoDict.
        /// </summary>
        private static SessionStack GetStack()
        {
            return RepoDict.Get<SessionStack>(SESSION_STACK_KEY);
        }

        /// <summary>
        /// Lock-защищённый стек сессий (голова списка = вершина стека).
        /// Контендерность минимальна (Push/Pop — только на открытие/закрытие
        /// контейнеров), поэтому lock даёт точные семантики без гонок.
        /// </summary>
        internal sealed class SessionStack
        {
            private readonly object _sync = new object();
            private readonly LinkedList<string> _items = new LinkedList<string>();

            public void Push(string sessionId)
            {
                lock (_sync)
                {
                    _items.AddFirst(sessionId);
                }
            }

            public string Peek()
            {
                lock (_sync)
                {
                    var first = _items.First;
                    return first?.Value;
                }
            }

            public void PopAny()
            {
                lock (_sync)
                {
                    if (_items.First != null)
                        _items.RemoveFirst();
                }
            }

            /// <summary>
            /// Снимает конкретную сессию (первое вхождение с вершины). Атомарно.
            /// </summary>
            public bool PopValue(string sessionId)
            {
                lock (_sync)
                {
                    return _items.Remove(sessionId);
                }
            }
        }
    }
}
