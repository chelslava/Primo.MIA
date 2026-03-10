// =============================================================================
// BrowserSessionContext.cs — ambient-контекст сессии браузера.
//
// Реализует паттерн "ambient context" через стек сессий.
// Контейнер BrowserOpenBack при старте кладёт sessionId в стек,
// при завершении — снимает. Активности внутри контейнера могут
// получить текущую сессию через BrowserSessionContext.Current,
// не зная ничего о контейнере.
//
// Поддерживает вложенные контейнеры — каждый Push добавляет свою сессию,
// каждый Pop возвращает к предыдущей.
//
// Использование в контейнере:
//   BrowserSessionContext.Push(sessionId);   // перед выполнением дочерних
//   ...
//   BrowserSessionContext.Pop();             // после завершения
//
// Использование в активности:
//   string sid = ResolveSessionId(this.Prop_SessionId, sd);
//   // если Prop_SessionId пуст — берётся из контекста
// =============================================================================

using System;
using System.Collections.Generic;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Статический ambient-контекст, хранящий стек ID сессий браузера.
    /// Позволяет активностям внутри контейнера получить sessionId
    /// без явной передачи через свойство Prop_SessionId.
    /// </summary>
    public static class BrowserSessionContext
    {
        // Стек сессий — поддерживает вложенные контейнеры браузера
        [ThreadStatic]
        private static Stack<string> _sessionStack;

        /// <summary>
        /// Возвращает ID текущей активной сессии браузера,
        /// или null если контекст пуст (активность запущена вне контейнера).
        /// </summary>
        public static string Current
        {
            get
            {
                // ThreadStatic поле не инициализируется автоматически на новых потоках
                if (_sessionStack == null || _sessionStack.Count == 0)
                    return null;

                return _sessionStack.Peek();
            }
        }

        /// <summary>
        /// Помещает ID сессии в стек контекста.
        /// Вызывается контейнером BrowserOpenBack перед выполнением дочерних активностей.
        /// </summary>
        /// <param name="sessionId">ID сессии браузера.</param>
        /// <exception cref="ArgumentNullException">Если sessionId пуст.</exception>
        public static void Push(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId),
                    "ID сессии не может быть пустым при регистрации контекста");

            // Инициализируем стек при первом использовании на потоке
            if (_sessionStack == null)
                _sessionStack = new Stack<string>();

            _sessionStack.Push(sessionId);
        }

        /// <summary>
        /// Снимает верхнюю сессию со стека контекста.
        /// Вызывается контейнером BrowserOpenBack в блоке finally.
        /// </summary>
        public static void Pop()
        {
            if (_sessionStack != null && _sessionStack.Count > 0)
                _sessionStack.Pop();
        }
    }
}
