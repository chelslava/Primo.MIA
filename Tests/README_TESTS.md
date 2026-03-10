# Тесты для работы с SessionID

## Обзор

Созданы комплексные тесты для проверки функциональности управления сессиями браузера через `BrowserSessionContext` и `SessionResolver`.

## Структура тестов

```
Tests/
├── Unit/
│   └── Browser/
│       ├── BrowserSessionContextTests.cs      # Unit-тесты для BrowserSessionContext
│       └── SessionResolverTests.cs            # Unit-тесты для SessionResolver
└── Integration/
    └── Browser/
        └── SessionIdIntegrationTests.cs       # Integration-тесты для реальных сценариев
```

## Unit-тесты

### BrowserSessionContextTests.cs

Проверяет базовую функциональность управления стеком сессий:

**Тесты:**
- ✅ `CurrentSessionId_WhenStackEmpty_ReturnsNull` - возврат null при пустом стеке
- ✅ `Push_WithValidSessionId_SetsCurrentSessionId` - установка текущей сессии
- ✅ `Push_WithContainerEntry_ClearsStackFirst` - очистка стека при входе в контейнер
- ✅ `Push_MultipleSessionsWithoutContainer_CreatesStack` - создание стека сессий
- ✅ `Pop_WhenStackEmpty_DoesNotThrow` - безопасный Pop на пустом стеке
- ✅ `Pop_RemovesTopSessionFromStack` - удаление верхней сессии
- ✅ `ClearStack_RemovesAllSessions` - полная очистка стека
- ✅ `Push_WithNullSessionId_ThrowsArgumentNullException` - валидация null
- ✅ `Push_WithEmptySessionId_ThrowsArgumentException` - валидация пустой строки
- ✅ `Push_WithWhitespaceSessionId_ThrowsArgumentException` - валидация пробелов

**Покрытие:** Все публичные методы `BrowserSessionContext`

### SessionResolverTests.cs

Проверяет логику разрешения sessionID:

**Тесты:**
- ✅ `Resolve_WithValidSessionId_ReturnsSessionId` - возврат явного ID
- ✅ `Resolve_WithEmptyString_ReturnsCurrentSessionId` - использование контекста
- ✅ `Resolve_WithEmptyStringAndNoContext_ReturnsEmptyString` - поведение без контекста
- ✅ `Resolve_WithNull_ThrowsArgumentNullException` - валидация null
- ✅ `Resolve_WithWhitespace_ReturnsCurrentSessionId` - обработка пробелов
- ✅ `Resolve_WithNestedContext_ReturnsTopSessionId` - работа с вложенным контекстом
- ✅ `Resolve_PreferExplicitSessionIdOverContext` - приоритет явного ID
- ✅ `Resolve_WithGuidSessionId_ReturnsGuid` - работа с GUID

**Покрытие:** Все сценарии использования `SessionResolver.Resolve()`

## Integration-тесты

### SessionIdIntegrationTests.cs

Проверяет реальные сценарии использования:

**Сценарии:**

1. **BrowserOpen внутри контейнера**
   - Проверяет что активности используют sessionId контейнера

2. **Вложенные активности**
   - Проверяет поддержку стека сессий при вложенности

3. **Явный sessionId переопределяет контекст**
   - Проверяет приоритет явно указанного ID

4. **Множественные контейнеры последовательно**
   - Проверяет что каждый контейнер очищает стек предыдущего

5. **BrowserOpen создает новую сессию**
   - Проверяет что последующие активности используют новую сессию

6. **Параллельные контейнеры**
   - Проверяет изоляцию сессий между контейнерами

7. **Присоединение к существующей сессии**
   - Проверяет возможность переиспользования сессии

8. **Пустой sessionId во вложенной активности**
   - Проверяет наследование родительской сессии

9. **Сложный workflow с множественной вложенностью**
   - Проверяет корректность работы при глубокой вложенности

## Запуск тестов

### Все тесты
```bash
dotnet test
```

### Только unit-тесты
```bash
dotnet test --filter Category=Unit
```

### Только integration-тесты
```bash
dotnet test --filter Category=Integration
```

### С покрытием кода
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Используя PowerShell скрипт
```powershell
.\run-tests.ps1
```

## Требования

- NUnit 3.x
- .NET Framework 4.8 или выше
- NuGet пакеты:
  - NUnit
  - NUnit3TestAdapter
  - Microsoft.NET.Test.Sdk

## Ожидаемые результаты

Все тесты должны проходить успешно:
- ✅ 10 unit-тестов для BrowserSessionContext
- ✅ 8 unit-тестов для SessionResolver
- ✅ 9 integration-тестов для сценариев

**Итого:** 27 тестов

## Покрытие кода

Целевые метрики:
- Line Coverage: ≥ 90% для BrowserSessionContext
- Line Coverage: ≥ 95% для SessionResolver
- Branch Coverage: ≥ 85%

## Примеры использования

### Пример 1: Базовый тест
```csharp
[Test]
public void Push_WithValidSessionId_SetsCurrentSessionId()
{
    // Arrange
    const string sessionId = "test-session-123";

    // Act
    BrowserSessionContext.Push(sessionId, isContainerEntry: false);

    // Assert
    Assert.That(BrowserSessionContext.CurrentSessionId, Is.EqualTo(sessionId));
}
```

### Пример 2: Тест с очисткой
```csharp
[Test]
public void Resolve_WithEmptyString_ReturnsCurrentSessionId()
{
    // Arrange
    const string currentSessionId = "current-session";
    BrowserSessionContext.Push(currentSessionId, isContainerEntry: true);
    
    try
    {
        // Act
        var result = SessionResolver.Resolve("");
        
        // Assert
        Assert.That(result, Is.EqualTo(currentSessionId));
    }
    finally
    {
        BrowserSessionContext.Pop();
    }
}
```

## Troubleshooting

### Тесты падают с NullReferenceException
**Причина:** Стек не был очищен перед тестом  
**Решение:** Убедитесь что в `[SetUp]` вызывается `BrowserSessionContext.ClearStack()`

### Тесты проходят локально, но падают в CI
**Причина:** Возможно проблемы с многопоточностью  
**Решение:** Проверьте что тесты не выполняются параллельно

### ArgumentNullException в SessionResolver
**Причина:** Передан null вместо пустой строки  
**Решение:** Используйте `""` вместо `null` для получения текущей сессии

## Дальнейшие улучшения

1. ⏳ Добавить performance тесты
2. ⏳ Добавить stress тесты для многопоточности
3. ⏳ Добавить тесты для edge cases
4. ⏳ Настроить автоматический запуск в CI/CD
5. ⏳ Добавить mutation testing

## Связанные документы

- [IMPROVEMENT_PLAN.md](../IMPROVEMENT_PLAN.md) - План улучшения проекта
- [Browser/BrowserSessionContext.cs](../Browser/BrowserSessionContext.cs) - Исходный код
- [Browser/SessionResolver.cs](../Browser/SessionResolver.cs) - Исходный код
- [Browser/HowTo_SessionTransfer.cs](../Browser/HowTo_SessionTransfer.cs) - Руководство

## Changelog

### 2026-03-10
- ✅ Созданы unit-тесты для BrowserSessionContext (10 тестов)
- ✅ Созданы unit-тесты для SessionResolver (8 тестов)
- ✅ Созданы integration-тесты для сценариев (9 тестов)
- ✅ Добавлена документация по тестам
