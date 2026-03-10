# ADR-001: Использование RepoDict для хранения состояния сессий

**Статус:** Принято  
**Дата:** 2026-03-10  
**Авторы:** Команда разработки Primo.MIA

---

## Контекст

Необходимо хранить состояние сессий браузера между вызовами активностей в Primo Platform. Активности выполняются в контексте Primo SDK, который имеет особенности работы с потоками и контекстом выполнения.

### Проблема

1. `ThreadStatic` не работает в среде Primo SDK из-за особенностей выполнения workflow
2. `AsyncLocal<T>` также не работает корректно
3. Необходимо хранить состояние между вызовами разных активностей
4. Нужна поддержка вложенных контейнеров (Sequence, If, While и т.д.)

### Требования

- Хранение WebDriver экземпляров по sessionId
- Доступ из любой активности
- Поддержка стека сессий для вложенных контейнеров
- Работа в среде Primo Platform

---

## Рассмотренные варианты

### Вариант 1: ThreadStatic

```csharp
[ThreadStatic]
private static Stack<string> _sessionStack;
```

**Преимущества:**
- Простая реализация
- Изоляция между потоками

**Недостатки:**
- ❌ Не работает в Primo SDK
- ❌ Теряется контекст между вызовами

**Решение:** Отклонено

### Вариант 2: AsyncLocal<T>

```csharp
private static AsyncLocal<Stack<string>> _sessionStack = new AsyncLocal<Stack<string>>();
```

**Преимущества:**
- Работает с async/await
- Сохраняет контекст

**Недостатки:**
- ❌ Не работает в Primo SDK
- ❌ Сложности с сериализацией

**Решение:** Отклонено

### Вариант 3: RepoDict (Выбрано)

```csharp
private const string SESSION_STACK_KEY = "Primo.MIA.Browser.SessionStack";

public static void Push(string sessionId, bool isContainerEntry)
{
    var stack = RepoDict.Get<Stack<string>>(SESSION_STACK_KEY) ?? new Stack<string>();
    if (isContainerEntry && stack.Count > 0)
        stack.Clear();
    stack.Push(sessionId);
    RepoDict.Set(SESSION_STACK_KEY, stack);
}
```

**Преимущества:**
- ✅ Работает в Primo SDK
- ✅ Доступ из любого места
- ✅ Простая реализация
- ✅ Поддержка сериализации

**Недостатки:**
- ⚠️ Глобальное состояние
- ⚠️ Сложность тестирования
- ⚠️ Потенциальные проблемы с многопоточностью

**Решение:** Принято

### Вариант 4: Dependency Injection

```csharp
public class SessionManager : ISessionManager
{
    private readonly ConcurrentDictionary<string, IWebDriver> _sessions;
    // ...
}
```

**Преимущества:**
- Чистая архитектура
- Легко тестировать
- Изоляция зависимостей

**Недостатки:**
- ❌ Требует изменения архитектуры Primo Platform
- ❌ Сложная интеграция
- ❌ Не поддерживается SDK

**Решение:** Отложено на будущее

---

## Решение

Использовать `RepoDict` - централизованное хранилище Primo Platform для хранения стека сессий.

### Реализация

```csharp
public static class BrowserSessionContext
{
    private const string SESSION_STACK_KEY = "Primo.MIA.Browser.SessionStack";

    public static string CurrentSessionId
    {
        get
        {
            var stack = GetStack();
            return stack.Count > 0 ? stack.Peek() : null;
        }
    }

    public static void Push(string sessionId, bool isContainerEntry)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("SessionId cannot be empty");

        var stack = GetStack();
        
        if (isContainerEntry && stack.Count > 0)
            stack.Clear();
        
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

1. **Работает в Primo SDK** - основное требование выполнено
2. **Простая реализация** - минимальный код, легко понять
3. **Доступность** - доступ из любой активности
4. **Поддержка вложенности** - стек сессий для контейнеров
5. **Сериализация** - RepoDict поддерживает сериализацию

### Отрицательные

1. **Глобальное состояние** - нарушает принципы чистой архитектуры
2. **Сложность тестирования** - требуется мокирование RepoDict
3. **Многопоточность** - потенциальные race conditions
4. **Связанность** - зависимость от Primo Platform

### Риски и митигация

| Риск | Вероятность | Влияние | Митигация |
|------|-------------|---------|-----------|
| Race conditions | Средняя | Высокое | Использовать lock при доступе |
| Утечки памяти | Низкая | Среднее | Очистка стека при закрытии браузера |
| Сложность отладки | Средняя | Среднее | Добавить логирование |

---

## Альтернативы на будущее

### Когда Primo Platform добавит поддержку DI

```csharp
public class BrowserOpenBack : BrowserActivityBase<BrowserOpen>
{
    private readonly ISessionManager _sessionManager;
    
    public BrowserOpenBack(IWFContainer container, ISessionManager sessionManager) 
        : base(container)
    {
        _sessionManager = sessionManager;
    }
}
```

### Использование Context API

```csharp
public interface IExecutionContext
{
    T GetValue<T>(string key);
    void SetValue<T>(string key, T value);
}
```

---

## Связанные решения

- ADR-002: Ambient Context для SessionResolver
- ADR-003: Автоматическая очистка стека при входе в контейнер

---

## Ссылки

- [Browser/BrowserSessionContext.cs](BrowserSessionContext.cs)
- [Browser/SessionResolver.cs](SessionResolver.cs)
- [Tests/Unit/Browser/BrowserSessionContextTests.cs](../Tests/Unit/Browser/BrowserSessionContextTests.cs)
