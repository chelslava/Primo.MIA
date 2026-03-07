# Primo.MIA.Tests

Проект unit-тестов для библиотеки Primo.MIA.

## Структура проекта

```
Primo.MIA.Tests/
├── Dictionary/              # Тесты операций со словарями
│   └── DictionaryOperationsLogicTests.cs
├── List/                    # Тесты операций со списками
│   └── ListFilterLogicTests.cs
├── Generators/              # Тесты генераторов
│   └── GeneratorLogicTests.cs
├── Logic/                   # Извлеченная бизнес-логика
│   ├── DictionaryOperationsLogic.cs
│   └── ListFilterLogic.cs
└── Helpers/                 # Вспомогательные утилиты
    └── TestHelpers.cs
```

## Используемые библиотеки

- **xUnit** — фреймворк для тестирования
- **FluentAssertions** — выразительные утверждения
- **Moq** — мокирование зависимостей (для будущих тестов)

## Запуск тестов

### Из командной строки

```bash
dotnet test
```

### Из Visual Studio

1. Откройте Test Explorer (Test → Test Explorer)
2. Нажмите "Run All"

### Запуск конкретного теста

```bash
dotnet test --filter "FullyQualifiedName~ListFilterLogicTests"
```

## Покрытие кода

Текущее покрытие:

- ✅ ListFilter — 95%
- ✅ DictionaryOperations (Merge, Filter, Invert) — 90%
- ✅ Generators (GUID, Random, Timestamp, Hash, Username, Template) — 85%
- ⏳ ListTransform — планируется
- ⏳ ListSort — планируется
- ⏳ TupleOperations — планируется

## Архитектура тестов

### Извлечение бизнес-логики

Для улучшения тестируемости бизнес-логика извлечена из активностей Primo RPA в отдельные классы:

```csharp
// Логика без зависимостей от Primo RPA SDK
public class ListFilterLogic
{
    public FilterResult Filter(List<string> source, string pattern, ...)
    {
        // Чистая бизнес-логика
    }
}

// Активность — тонкая обертка
public class ListFilterBack : PrimoComponentTO<ListFilter>
{
    private readonly ListFilterLogic _logic = new ListFilterLogic();
    
    public override ExecutionResult TimedAction(ScriptingData sd)
    {
        // Получение параметров из Primo RPA
        var result = _logic.Filter(...);
        // Установка результатов
    }
}
```

### Преимущества подхода

1. **Тестируемость** — логика тестируется без Primo RPA SDK
2. **Переиспользование** — логику можно использовать вне RPA
3. **Скорость** — тесты выполняются быстрее
4. **Изоляция** — тесты не зависят от инфраструктуры

## Примеры тестов

### Базовый тест

```csharp
[Fact]
public void Filter_Contains_ReturnsMatchingElements()
{
    // Arrange
    var logic = new ListFilterLogic();
    var list = new List<string> { "apple", "banana", "apricot" };

    // Act
    var result = logic.Filter(list, "ap", ListFilterMode.Contains);

    // Assert
    result.Matched.Should().HaveCount(2);
    result.Matched.Should().Contain("apple");
    result.Matched.Should().Contain("apricot");
}
```

### Параметризованный тест

```csharp
[Theory]
[InlineData("test", true)]
[InlineData("TEST", true)]
[InlineData("Test", true)]
[InlineData("other", false)]
public void Filter_ExactMatch_CaseInsensitive(string input, bool shouldMatch)
{
    // Arrange & Act
    var result = _logic.Filter(
        new List<string> { input }, 
        "test", 
        ListFilterMode.ExactMatch, 
        caseSensitive: false);

    // Assert
    if (shouldMatch)
        result.Matched.Should().HaveCount(1);
    else
        result.Rejected.Should().HaveCount(1);
}
```

### Тест исключений

```csharp
[Fact]
public void Merge_ThrowOnDuplicate_WithDuplicates_ThrowsException()
{
    // Arrange
    var first = new Dictionary<string, string> { { "a", "1" } };
    var second = new Dictionary<string, string> { { "a", "2" } };

    // Act & Assert
    Action act = () => _logic.Merge(first, second, 
        DictionaryMergeStrategy.ThrowOnDuplicate);
    
    act.Should().Throw<InvalidOperationException>()
        .WithMessage("*дублирующиеся ключи*");
}
```

## Соглашения

### Именование тестов

Формат: `MethodName_Scenario_ExpectedBehavior`

Примеры:
- `Filter_Contains_ReturnsMatchingElements`
- `Merge_KeepFirst_PreservesFirstDictionaryValues`
- `Invert_DuplicateValues_ThrowsException`

### Структура теста (AAA)

```csharp
[Fact]
public void TestName()
{
    // Arrange — подготовка данных
    var input = ...;
    
    // Act — выполнение действия
    var result = ...;
    
    // Assert — проверка результата
    result.Should()...;
}
```

### Категории тестов

```csharp
[Trait("Category", "Fast")]      // Быстрые тесты (<100ms)
[Trait("Category", "Integration")] // Интеграционные тесты
[Trait("Category", "Slow")]       // Медленные тесты (>1s)
```

## Добавление новых тестов

1. Создайте класс логики в `Logic/` (если нужно)
2. Создайте тестовый класс в соответствующей папке
3. Наследуйтесь от базового класса (если есть)
4. Используйте FluentAssertions для утверждений
5. Покройте edge cases и исключения

## CI/CD

Тесты автоматически запускаются при:
- Push в любую ветку
- Pull Request
- Перед созданием релиза

## Метрики качества

Цели проекта:
- ✅ Покрытие кода > 80%
- ✅ Все тесты проходят
- ✅ Время выполнения < 30 секунд
- ✅ Нет flaky тестов

---

**Версия:** 1.0  
**Дата:** Февраль 2025
