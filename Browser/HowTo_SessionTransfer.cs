// =============================================================================
// КАК sessionId ПЕРЕДАЁТСЯ ДОЧЕРНИМ АКТИВНОСТЯМ — ДВА СПОСОБА
//
// Способ 1 (автоматический): BrowserSessionContext — ambient-контекст
// Способ 2 (явный):          переменная ScriptingData через Prop_SessionId
//
// Оба способа работают одновременно и не мешают друг другу.
// =============================================================================


// ─────────────────────────────────────────────────────────────────────────────
// СПОСОБ 1 — АВТОМАТИЧЕСКИЙ (ambient-контекст)
//
// Пользователь оставляет Prop_SessionId дочерней активности ПУСТЫМ.
// SessionResolver сам находит сессию через BrowserSessionContext.Current.
//
// Схема:
//   BrowserOpenBack.SimpleAction → BrowserSessionContext.Push(sessionId)
//   Дочерняя активность         → SessionResolver.Resolve("") → берёт из стека
//   BrowserCloseBack             → BrowserSessionContext.Pop()
//
// Плюсы:  ничего не нужно настраивать, просто перетащил активность в контейнер
// Минусы: не видно явной связи в дизайнере
// ─────────────────────────────────────────────────────────────────────────────

// В BrowserOpenBack.SimpleAction (уже реализовано):
BrowserSessionContext.Push(sessionId);   // кладём в стек ПЕРЕД return

// В дочерней активности (уже реализовано через SessionResolver):
string sessionId = SessionResolver.Resolve(
    GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
// если Prop_SessionId == "" → SessionResolver вернёт BrowserSessionContext.Current


// ─────────────────────────────────────────────────────────────────────────────
// СПОСОБ 2 — ЯВНЫЙ (через переменную ScriptingData)
//
// Пользователь задаёт в BrowserOpenBack.Prop_SessionId имя переменной,
// например "sessionVar". BrowserOpenBack записывает туда sessionId.
// Дочерние активности получают переменную в своём Prop_SessionId.
//
// Схема в дизайнере:
//   [BrowserOpenBack]  Prop_SessionId = sessionVar  (выходная переменная)
//       [ElementClickBack]  Prop_SessionId = sessionVar  (входная переменная)
//       [BrowserCloseBack]  Prop_SessionId = sessionVar
//
// Плюсы:  явная связь видна в дизайнере, можно передать в другой контейнер
// Минусы: нужно прописывать переменную в каждой дочерней активности
// ─────────────────────────────────────────────────────────────────────────────

// В BrowserOpenBack.SimpleAction (уже реализовано):
if (!string.IsNullOrWhiteSpace(this.Prop_SessionId))
    SetVariableValue(this.Prop_SessionId, sessionId, sd);
// Записывает sessionId в переменную ScriptingData["sessionVar"]

// В дочерней активности пользователь заполняет Prop_SessionId = "sessionVar"
// SessionResolver.Resolve читает это через GetPropertyValue → возвращает реальный ID


// ─────────────────────────────────────────────────────────────────────────────
// КАК SessionResolver ОБРАБАТЫВАЕТ ОБА СЛУЧАЯ
// ─────────────────────────────────────────────────────────────────────────────

public static string Resolve(string sessionId)
{
    // Способ 2: пользователь явно задал переменную — GetPropertyValue уже
    // вычислил её значение и передал сюда как строку с реальным ID
    if (!string.IsNullOrWhiteSpace(sessionId))
        return sessionId;

    // Способ 1: свойство пустое — берём из ambient-контекста контейнера
    string contextSession = BrowserSessionContext.Current;
    if (!string.IsNullOrWhiteSpace(contextSession))
        return contextSession;

    // Ни один из способов не сработал
    throw new ArgumentException(
        "ID сессии не задан и активность запущена вне контейнера «Открыть браузер». " +
        "Укажите Prop_SessionId явно или поместите активность внутрь контейнера.");
}


// ─────────────────────────────────────────────────────────────────────────────
// ИТОГОВАЯ СХЕМА В ДИЗАЙНЕРЕ
// ─────────────────────────────────────────────────────────────────────────────

// Вариант А — без переменной (Способ 1, рекомендуется):
//
//   [BrowserOpenBack]
//       Prop_BrowserType  = Chrome
//       Prop_SessionId    = (пусто)        ← выходная переменная не нужна
//     ┌─────────────────────────────────┐
//     │ [ElementFindBack]               │
//     │     Prop_SessionId = (пусто)    │  ← SessionResolver берёт из контекста
//     │ [ElementClickBack]              │
//     │     Prop_SessionId = (пусто)    │  ← SessionResolver берёт из контекста
//     │ [BrowserCloseBack]              │
//     │     Prop_SessionId = (пусто)    │  ← SessionResolver берёт из контекста
//     └─────────────────────────────────┘

// Вариант Б — с переменной (Способ 2):
//
//   [BrowserOpenBack]
//       Prop_BrowserType  = Chrome
//       Prop_SessionId    = mySession     ← записывает ID в переменную mySession
//     ┌─────────────────────────────────┐
//     │ [ElementFindBack]               │
//     │     Prop_SessionId = mySession  │  ← читает из переменной
//     │ [ElementClickBack]              │
//     │     Prop_SessionId = mySession  │  ← читает из переменной
//     │ [BrowserCloseBack]              │
//     │     Prop_SessionId = mySession  │  ← читает из переменной
//     └─────────────────────────────────┘
