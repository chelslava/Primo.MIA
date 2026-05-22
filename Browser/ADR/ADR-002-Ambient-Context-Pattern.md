# ADR-002: Ambient Context Pattern для SessionResolver

**Статус:** Принято  
**Дата:** 2026-03-10  
**Авторы:** Команда разработки Primo.MIA

---

## Контекст

После принятия решения использовать RepoDict для хранения стека сессий (ADR-001), необходимо определить, как активности будут получать доступ к текущей сессии браузера.

### Проблема

1. Каждая активность может иметь параметр `sessionId` (опциональный)
2. Если `sessionId` не указан, нужно использовать текущую сессию
3. Необходимо избежать передачи sessionId через все слои
4. Код должен быть простым и понятным

### Требования

- Автоматическое разрешение sessionId
- Поддержка явного указания sessionId
- Минимальные изменения в существующем коде
- Простота использования

---

## Рассмотренные варианты

### Вариант 1: Передача через параметры

```csharp
public IWebDriver GetDriver(string sessionId, ScriptingData sd)
{
    if (string.IsNullOrEmpty(sessionId))
        sessionId = GetSessionIdFromSomewhere();
    return SeleniumHelper.GetDriver(sessionId);
}
```

**Преимущества:**
- Явная передача зависимостей
- Легко тестировать

**Недостатки:**
- ❌ Дублирование кода в каждой активности
- ❌ Сложность поддержки
- ❌ Много boilerplate кода

**Решение:** Отклонено

### Вариант 2: Singleton SessionManager

```csharp
public class SessionManager
{
    private static SessionManager _instance;
    public static SessionManager Instance => _instance ??= new SessionManager();
    
    public string CurrentSessionId { get; set; }
}
```

**Преимущества:**
- Глобальный доступ
- Простая реализация

**Недостатки:**
- ❌ Не поддерживает вложенные контексты
- ❌ Проблемы с многопоточностью
- ❌ Сложно тестировать

**Решение:** Отклонено

### Вариант 3: Ambient Context (Выбрано)

```csharp
public static class SessionResolver
{
    public static string Resolve(string explicitSessionId)
    {
        if (!string.IsNullOrWhiteSpace(explicitSessionId))
            return explicitSessionId;
        
        var currentSessionId = BrowserSessionContext.CurrentSessionId;
        if (string.IsNullOrWhiteSpace(currentSessionId))
            throw new InvalidOperationException("No active browser session");
        
        return currentSessionId;
    }
}
```

**Преимущества:**
- ✅ Простое использование
- ✅ Поддержка явного и неявного sessionId
- ✅ Работает с вложенными контекстами
- ✅ Минимальный код

**Недостатки:**
- ⚠️ Скрытая зависимость от BrowserSessionContext
- ⚠️ Сложность тестирования

**Решение:** Принято

---

## Решение

Использовать паттерн Ambient Context через статический класс `SessionResolver`, который разрешает sessionId с учетом контекста.

### Реализация

```csharp
/// <summary>
/// Разрешает sessionId с учетом текущего контекста выполнения.
/// Если sessionId явно указан - использует его.
/// Если нет - берет из BrowserSessionContext.
/// </summary>
public static class SessionResolver
{
    /// <summary>
    /// Разрешает sessionId
    /// </summary>
    /// <param name="explicitSessionId">Явно указанный sessionId (может быть null/empty)</param>
    /// <returns>Разрешенный sessionId</returns>
    /// <exception cref="InvalidOperationException">Если нет активной сессии</exception>
    public static string Resolve(string explicitSessionId)
    {
        // Если sessionId указан явно - используем его
        if (!string.IsNullOrWhiteSpace(explicitSessionId))
        {
            return explicitSessionId;
        }
        
        // Иначе берем из контекста
        var currentSessionId = BrowserSessionContext.CurrentSessionId;
        
        if (string.IsNullOrWhiteSpace(currentSessionId))
        {
            throw new InvalidOperationException(
                "No active browser session found. " +
                "Please use BrowserOpen activity first or specify sessionId explicitly.");
        }
        
        return currentSessionId;
    }
    
    /// <summary>
    /// Проверяет, есть ли активная сессия
    /// </summary>
    public static bool HasActiveSession()
    {
        return !string.IsNullOrWhiteSpace(BrowserSessionContext.CurrentSessionId);
    }
}
```

### Использование в активностях

**До:**
```csharp
public override ExecutionResult TimedAction(ScriptingData sd)
{
    try
    {
        string sessionId = Prop_SessionId.Get(sd);
        if (string.IsNullOrEmpty(sessionId))
        {
            // Какая-то логика получения текущей сессии
            sessionId = GetCurrentSessionSomehow();
        }
        
        IWebDriver driver = SeleniumHelper.GetDriver(sessionId);
        // ...
    }
    catch (Exception ex)
    {
        return CreateErrorResult(ex);
    }
}
```

**После:**
```csharp
public override ExecutionResult TimedAction(ScriptingData sd)
{
    return SafeExecute(() =>
    {
        IWebDriver driver = GetDriverFromContext(sd, nameof(Prop_SessionId));
        // ...
        return CreateSuccessResult("Success");
    }, "ElementClick");
}

// В BrowserActivityBase:
protected IWebDriver GetDriverFromContext(ScriptingData sd, string propertyName)
{
    string explicitSessionId = GetPropertyValue<string>(sd, propertyName);
    string resolvedSessionId = SessionResolver.Resolve(explicitSessionId);
    return SeleniumHelper.GetDriver(resolvedSessionId);
}
```

---

## Последствия

### Положительные

1. **Простота использования** - одна строка кода вместо 5-10
2. **Единообразие** - все активности используют одинаковый подход
3. **Поддержка вложенности** - работает с контейнерами
4. **Явный fallback** - можно указать sessionId явно
5. **Понятные ошибки** - четкое сообщение при отсутствии сессии

### Отрицательные

1. **Скрытая зависимость** - не видно из сигнатуры метода
2. **Сложность тестирования** - нужно мокировать BrowserSessionContext
3. **Глобальное состояние** - нарушает принципы чистой архитектуры

### Компромиссы

Мы жертвуем чистотой архитектуры ради:
- Простоты использования
- Минимального boilerplate кода
- Совместимости с Primo Platform

---

## Примеры использования

### Пример 1: Использование текущей сессии

```csharp
// Workflow:
// 1. BrowserOpen -> sessionId = "session1"
// 2. ElementClick (sessionId не указан)

// В ElementClick:
string sessionId = SessionResolver.Resolve(""); // Вернет "session1"
```

### Пример 2: Явное указание сессии

```csharp
// Workflow:
// 1. BrowserOpen -> sessionId = "session1"
// 2. BrowserOpen -> sessionId = "session2"
// 3. ElementClick (sessionId = "session1")

// В ElementClick:
string sessionId = SessionResolver.Resolve("session1"); // Вернет "session1"
```

### Пример 3: Вложенные контейнеры

```csharp
// Workflow:
// 1. BrowserOpen -> sessionId = "session1"
// 2. Sequence {
//      3. ElementClick (sessionId не указан) -> использует "session1"
//      4. If {
//           5. ElementInput (sessionId не указан) -> использует "session1"
//         }
//    }

// Стек сессий: ["session1"]
// Все активности используют "session1"
```

---

## Тестирование

### Unit-тесты

```csharp
[Test]
public void Resolve_WithExplicitSessionId_ReturnsExplicitSessionId()
{
    // Arrange
    string explicitSessionId = "explicit-session";
    
    // Act
    string result = SessionResolver.Resolve(explicitSessionId);
    
    // Assert
    Assert.AreEqual(explicitSessionId, result);
}

[Test]
public void Resolve_WithoutExplicitSessionId_ReturnsCurrentSessionId()
{
    // Arrange
    BrowserSessionContext.Push("current-session", true);
    
    // Act
    string result = SessionResolver.Resolve("");
    
    // Assert
    Assert.AreEqual("current-session", result);
    
    // Cleanup
    BrowserSessionContext.ClearStack();
}

[Test]
public void Resolve_WithoutSessionId_ThrowsException()
{
    // Arrange
    BrowserSessionContext.ClearStack();
    
    // Act & Assert
    Assert.Throws<InvalidOperationException>(() => 
        SessionResolver.Resolve(""));
}
```

---

## Альтернативы на будущее

### Когда появится DI

```csharp
public class SessionResolver : ISessionResolver
{
    private readonly ISessionContext _context;
    
    public SessionResolver(ISessionContext context)
    {
        _context = context;
    }
    
    public string Resolve(string explicitSessionId)
    {
        return !string.IsNullOrWhiteSpace(explicitSessionId) 
            ? explicitSessionId 
            : _context.CurrentSessionId;
    }
}
```

---

## Связанные решения

- ADR-001: Использование RepoDict для хранения состояния сессий
- ADR-003: Автоматическая очистка стека при входе в контейнер

---

## Ссылки

- [Browser/SessionResolver.cs](../SessionResolver.cs)
- [Browser/BrowserSessionContext.cs](../BrowserSessionContext.cs)
- [Tests/Unit/Browser/SessionResolverTests.cs](../../Tests/Unit/Browser/SessionResolverTests.cs)
- [Martin Fowler - Ambient Context](https://martinfowler.com/bliki/AmbientContext.html)
