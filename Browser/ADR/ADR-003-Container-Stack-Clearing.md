# ADR-003: Автоматическая очистка стека при входе в контейнер

**Статус:** Принято  
**Дата:** 2026-03-10  
**Авторы:** Команда разработки Primo.MIA

---

## Контекст

В Primo Platform workflow состоит из активностей и контейнеров (Sequence, If, While, ForEach и т.д.). При использовании BrowserOpen внутри контейнера возникает вопрос: что делать со стеком сессий?

### Проблема

```
Workflow:
1. BrowserOpen (sessionId = "session1") -> стек: ["session1"]
2. Sequence {
     3. BrowserOpen (sessionId = "session2") -> стек: ["session1", "session2"] или ["session2"]?
     4. ElementClick (без sessionId) -> какую сессию использовать?
   }
5. ElementClick (без sessionId) -> какую сессию использовать?
```

### Требования

- Предсказуемое поведение
- Изоляция контейнеров
- Простота понимания
- Совместимость с существующими workflow

---

## Рассмотренные варианты

### Вариант 1: Накопление в стеке

```csharp
public static void Push(string sessionId)
{
    var stack = GetStack();
    stack.Push(sessionId);
    SetStack(stack);
}
```

**Поведение:**
```
1. BrowserOpen("session1") -> стек: ["session1"]
2. Sequence {
     3. BrowserOpen("session2") -> стек: ["session1", "session2"]
     4. ElementClick -> использует "session2"
   }
5. ElementClick -> использует "session2" (неожиданно!)
```

**Преимущества:**
- Простая реализация
- Поддержка вложенных сессий

**Недостатки:**
- ❌ Неожиданное поведение после выхода из контейнера
- ❌ Утечка состояния между контейнерами
- ❌ Сложность отладки

**Решение:** Отклонено

### Вариант 2: Очистка при каждом Push

```csharp
public static void Push(string sessionId)
{
    var stack = new Stack<string>();
    stack.Push(sessionId);
    SetStack(stack);
}
```

**Поведение:**
```
1. BrowserOpen("session1") -> стек: ["session1"]
2. Sequence {
     3. BrowserOpen("session2") -> стек: ["session2"]
     4. ElementClick -> использует "session2"
   }
5. ElementClick -> ошибка! (стек пуст)
```

**Преимущества:**
- Изоляция контейнеров
- Предсказуемое поведение

**Недостатки:**
- ❌ Теряется контекст после выхода из контейнера
- ❌ Невозможно использовать сессию родителя

**Решение:** Отклонено

### Вариант 3: Флаг isContainerEntry (Выбрано)

```csharp
public static void Push(string sessionId, bool isContainerEntry)
{
    var stack = GetStack();
    
    // Если это вход в контейнер - очищаем стек
    if (isContainerEntry && stack.Count > 0)
    {
        stack.Clear();
    }
    
    stack.Push(sessionId);
    SetStack(stack);
}
```

**Поведение:**
```
1. BrowserOpen("session1", isContainerEntry: true) -> стек: ["session1"]
2. Sequence {
     3. BrowserOpen("session2", isContainerEntry: true) -> стек: ["session2"]
     4. ElementClick -> использует "session2"
   }
5. ElementClick -> использует "session1" (восстановлено через Pop)
```

**Преимущества:**
- ✅ Изоляция контейнеров
- ✅ Предсказуемое поведение
- ✅ Гибкость (можно не очищать стек)
- ✅ Поддержка вложенных сессий

**Недостатки:**
- ⚠️ Требует явного указания флага
- ⚠️ Сложность реализации Pop

**Решение:** Принято

---

## Решение

Использовать флаг `isContainerEntry` в методе `Push`, который указывает, является ли вызов входом в новый контейнер. При `isContainerEntry = true` стек очищается.

### Реализация

```csharp
public static class BrowserSessionContext
{
    private const string SESSION_STACK_KEY = "Primo.MIA.Browser.SessionStack";

    public static void Push(string sessionId, bool isContainerEntry)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("SessionId cannot be empty", nameof(sessionId));

        var stack = GetStack();
        
        if (isContainerEntry && stack.Count > 0)
        {
            stack.Clear();
        }
        
        stack.Push(sessionId);
        SetStack(stack);
    }

    public static void Pop()
    {
        var stack = GetStack();
        if (stack.Count > 0)
        {
            stack.Pop();
            SetStack(stack);
        }
    }

    public static string CurrentSessionId
    {
        get
        {
            var stack = GetStack();
            return stack.Count > 0 ? stack.Peek() : null;
        }
    }

    private static Stack<string> GetStack()
    {
        return RepoDict.Get<Stack<string>>(SESSION_STACK_KEY) ?? new Stack<string>();
    }

    private static void SetStack(Stack<string> stack)
    {
        RepoDict.Set(SESSION_STACK_KEY, stack);
    }
}
```

---

## Последствия

### Положительные

1. **Изоляция контейнеров** - каждый контейнер имеет свою сессию
2. **Предсказуемое поведение** - понятно, какая сессия используется
3. **Гибкость** - можно создавать вложенные сессии (isContainerEntry = false)
4. **Простота отладки** - четкая логика работы стека

### Отрицательные

1. **Требует явного флага** - нужно помнить про isContainerEntry
2. **Сложность Pop** - нужно правильно восстанавливать стек
3. **Потенциальные утечки** - если забыть вызвать Pop

---

## Связанные решения

- ADR-001: Использование RepoDict для хранения состояния сессий
- ADR-002: Ambient Context Pattern для SessionResolver

---

## Ссылки

- [Browser/BrowserSessionContext.cs](../BrowserSessionContext.cs)
- [Browser/BrowserOpenBack.cs](../BrowserOpenBack.cs)
- [Browser/BrowserCloseBack.cs](../BrowserCloseBack.cs)
- [Tests/Unit/Browser/BrowserSessionContextTests.cs](../../Tests/Unit/Browser/BrowserSessionContextTests.cs)
