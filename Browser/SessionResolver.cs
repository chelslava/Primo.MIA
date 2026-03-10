// =============================================================================
// SessionResolver.cs — вспомогательный метод разрешения ID сессии.
//
// Содержит единственный метод Resolve, который используется во ВСЕХ активностях
// вместо прямого чтения и проверки Prop_SessionId.
//
// Логика разрешения (приоритет сверху вниз):
//   1. Если sessionId задан явно (уже прочитан через GetPropertyValue) — используем его
//   2. Если пуст — берём из BrowserSessionContext.Current (ambient-контекст контейнера)
//   3. Если и там пусто — бросаем ArgumentException с понятным сообщением
//
// Важно: метод принимает уже прочитанную строку, а не свойство и ScriptingData.
// GetPropertyValue вызывается в самой активности до передачи сюда —
// это позволяет избежать зависимости от базового класса PrimoComponentTO.
// =============================================================================

using Primo.MIA.Common;
using System;

namespace Primo.MIA
{
    /// <summary>
    /// Вспомогательный статический класс для разрешения ID сессии браузера.
    /// Используется всеми активностями, которым нужен WebDriver.
    /// </summary>
    public static class SessionResolver
    {
        /// <summary>
        /// Разрешает ID сессии браузера по следующему приоритету:
        /// 1. Явно переданный sessionId (прочитанный через GetPropertyValue в активности)
        /// 2. Текущий ambient-контекст контейнера (BrowserSessionContext.Current)
        /// </summary>
        /// <param name="sessionId">
        /// Уже прочитанное значение Prop_SessionId через GetPropertyValue.
        /// Если пустое — метод попробует взять сессию из контекста контейнера.
        /// </param>
        /// <returns>Валидный ID сессии браузера.</returns>
        /// <exception cref="ArgumentException">
        /// Если сессия не найдена ни в свойстве, ни в контексте контейнера.
        /// </exception>
        public static string Resolve(string sessionId)
        {
            // Шаг 1: если свойство задано явно — сразу возвращаем
            if (!string.IsNullOrWhiteSpace(sessionId))
                return sessionId;

            // Шаг 2: свойство пусто — пробуем взять из ambient-контекста контейнера
            string contextSession = BrowserSessionContext.Current;
            if (!string.IsNullOrWhiteSpace(contextSession))
                return contextSession;

            // Шаг 3: сессия не найдена нигде — понятная ошибка пользователю
            throw new ArgumentException(
                "ID сессии не задан и активность запущена вне контейнера «Открыть браузер». " +
                "Укажите Prop_SessionId явно или поместите активность внутрь контейнера.");
        }
    }
}
