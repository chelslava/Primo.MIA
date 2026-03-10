# Руководство по рефакторингу Browser активностей

## Обзор

Этот документ описывает процесс рефакторинга Browser активностей для использования базового класса `BrowserActivityBase<TView>`.

## Цели рефакторинга

1. **Устранение дублирования кода** - общая логика вынесена в базовый класс
2. **Улучшение читаемости** - методы стали короче и понятнее
3. **Упрощение тестирования** - базовый класс можно тестировать отдельно
4. **Единообразие** - все активности используют одинаковые паттерны
5. **Упрощение поддержки** - изменения в одном месте применяются ко всем активностям

## Что предоставляет BrowserActivityBase

### 1. Управление драйверами

```csharp
// Было (в каждой активности)
string sessionId = SessionResolver.Resolve(
    GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
var driver = SeleniumHelper.GetDriver(sessionId);

// Стало (один вызов)
IWebDriver driver = GetDriverFromContext(sd, nameof(Prop_SessionId));
```

### 2. Поиск элементов

```csharp
// Было
By locator = SeleniumHelper.CreateLocator(locatorType, locatorValue);
IWebElement element = SeleniumHelper.WaitForElement(driver, locator, timeout);

// Стало
IWebElement element = FindElement(driver, locatorType, locatorValue, timeout);
```

### 3. Создание результатов

```csharp
// Было
return new ExecutionResult
{
    IsSuccess = true,
    SuccessMessage = "Операция выполнена"
};

// Стало
return CreateSuccessResult("Операция выполнена");
```

### 4. Обработка ошибок

```csharp
// Было
try
{
    // логика
    return new ExecutionResult { IsSuccess = true };
}
catch (Exception ex)
{
    return new ExecutionResult
    {
        IsSuccess = false,
        ErrorMessage = $"Ошибка: {ex.Message}"
    };
}

// Стало
return SafeExecute(() =>
{
    // логика
    return CreateSuccessResult();
}, "Контекст операции");
```

### 5. Валидация

```csharp
// Было
if (string.IsNullOrWhiteSpace(value))
    throw new ArgumentException($"Параметр не может быть пустым");

// Стало
ValidateNotEmpty(value, "parameterName");
```

## Пример рефакторинга: ElementClickBack

### До рефакторинга

**Проблемы:**
- 436 строк кода
- Дублирование логики получения драйвера
- Дублирование логики поиска элементов
- Повторяющийся код создания результатов
- Вложенные try-catch блоки

### После рефакторинга

**Улучшения:**
- ~350 строк кода (-20%)
- Использование базового класса
- Guard clauses вместо вложенных if
- Разделение на маленькие методы (SRP)
- Единообразная обработка ошибок

### Сравнение кода

#### Получение драйвера

```csharp
// ❌ До (повторяется в каждой активности)
string sessionId = SessionResolver.Resolve(
    GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
var driver = SeleniumHelper.GetDriver(sessionId);
if (driver == null)
    throw new InvalidOperationException("Driver not found");

// ✅ После (один раз в базовом классе)
IWebDriver driver = GetDriverFromContext(sd, nameof(Prop_SessionId));
```

#### Поиск элемента

```csharp
// ❌ До (сложная вложенная логика)
IWebElement element;
if (!string.IsNullOrWhiteSpace(locatorValue))
{
    string timeoutStr = GetPropertyValue<string>(this.Prop_WaitTimeout, nameof(Prop_WaitTimeout), sd) ?? "10";
    int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
    timeout = SeleniumHelper.ValidateTimeout(timeout, 10);
    var locator = SeleniumHelper.CreateLocator(this.Prop_LocatorType, locatorValue);
    element = SeleniumHelper.WaitForElementClickable(driver, locator, timeout);
}
else if (!string.IsNullOrWhiteSpace(elementId))
{
    element = SeleniumHelper.GetElement(elementId);
}
else
{
    throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор");
}

// ✅ После (чистая логика с guard clauses)
private IWebElement ResolveElement(ScriptingData sd, IWebDriver driver)
{
    string elementId = GetPropertyValue<string>(Prop_ElementId, nameof(Prop_ElementId), sd);
    string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

    // Guard clause
    if (string.IsNullOrWhiteSpace(locatorValue) && string.IsNullOrWhiteSpace(elementId))
        throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор");

    // Приоритет: локатор > elementId
    if (!string.IsNullOrWhiteSpace(locatorValue))
    {
        int timeout = ParseTimeout(sd);
        return FindElementByLocator(driver, locatorValue, timeout);
    }

    return SeleniumHelper.GetElement(elementId);
}
```

#### Выполнение действия

```csharp
// ❌ До (один большой метод)
private string ExecuteClick(ScriptingData sd, IWebDriver driver, IWebElement element, string elementId, string locatorValue)
{
    Func<string> elementLabel = () => !string.IsNullOrWhiteSpace(locatorValue)
        ? $"{this.Prop_LocatorType}={locatorValue}"
        : elementId;

    switch (this.Prop_ClickMode)
    {
        case ClickMode.Click:
            {
                if (this.Prop_UseJavaScript)
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
                else
                    element.Click();
                ApplyWaitAfterClick(sd);
                return $"[Click] Выполнен клик: {elementLabel()}";
            }
        // ... еще 3 case блока
    }
}

// ✅ После (разделено на отдельные методы)
private string PerformClickAction(ScriptingData sd, IWebDriver driver, IWebElement element)
{
    string elementLabel = GetElementLabel();

    switch (Prop_ClickMode)
    {
        case ClickMode.Click:
            return PerformSingleClick(driver, element, elementLabel, sd);
        case ClickMode.DoubleClick:
            return PerformDoubleClick(driver, element, elementLabel, sd);
        case ClickMode.RightClick:
            return PerformRightClick(driver, element, elementLabel, sd);
        case ClickMode.ClickAndHold:
            return PerformClickAndHold(driver, element, elementLabel, sd);
        default:
            throw new ArgumentOutOfRangeException(nameof(Prop_ClickMode));
    }
}

private string PerformSingleClick(IWebDriver driver, IWebElement element, string label, ScriptingData sd)
{
    if (Prop_UseJavaScript)
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
    else
        element.Click();

    ApplyWaitAfterClick(sd);
    return $"[Click] Выполнен клик: {label}";
}
```

## Пошаговая инструкция по рефакторингу

### Шаг 1: Изменить базовый класс

```csharp
// Было
public class ElementClickBack : PrimoComponentTO<ElementClick>

// Стало
public class ElementClickBack : BrowserActivityBase<ElementClick>
```

### Шаг 2: Упростить TimedAction

```csharp
public override ExecutionResult TimedAction(ScriptingData sd)
{
    return SafeExecute(() =>
    {
        // 1. Получить драйвер
        IWebDriver driver = GetDriverFromContext(sd, nameof(Prop_SessionId));
        
        // 2. Найти элемент
        IWebElement element = ResolveElement(sd, driver);
        
        // 3. Выполнить действие
        string result = PerformAction(sd, driver, element);
        
        return CreateSuccessResult(result);
    }, 
    "Контекст операции");
}
```

### Шаг 3: Извлечь методы

Разбейте большие методы на маленькие с одной ответственностью:

```csharp
// Один метод = одна задача
private IWebElement ResolveElement(ScriptingData sd, IWebDriver driver) { }
private string PerformAction(ScriptingData sd, IWebDriver driver, IWebElement element) { }
private int ParseTimeout(ScriptingData sd) { }
private void ApplyWaitAfter(ScriptingData sd) { }
```

### Шаг 4: Применить Guard Clauses

```csharp
// ❌ Вложенные if
if (condition1)
{
    if (condition2)
    {
        // логика
    }
}

// ✅ Guard clauses
if (!condition1)
    throw new ArgumentException("Error 1");

if (!condition2)
    throw new ArgumentException("Error 2");

// логика на верхнем уровне
```

### Шаг 5: Использовать методы базового класса

```csharp
// Получение драйвера
IWebDriver driver = GetDriverFromContext(sd, nameof(Prop_SessionId));

// Поиск элемента
IWebElement element = FindElement(driver, locatorType, locatorValue, timeout);

// Создание результата
return CreateSuccessResult("Успех");
return CreateErrorResult(ex, "Контекст");

// Валидация
ValidateNotEmpty(value, "parameterName");
ValidatePositive(timeout, "timeout");
ValidateUrl(url);

// Логирование
LogInfo("Информация");
LogWarning("Предупреждение");
LogError("Ошибка", ex);
```

## Метрики улучшения

### ElementClickBack

| Метрика | До | После | Улучшение |
|---------|-----|-------|-----------|
| Строк кода | 436 | ~350 | -20% |
| Cyclomatic Complexity | 15 | 8 | -47% |
| Методов > 20 строк | 3 | 0 | -100% |
| Уровней вложенности | 4 | 2 | -50% |
| Дублирование | Высокое | Нет | ✅ |

## Чек-лист рефакторинга

- [ ] Изменить базовый класс на `BrowserActivityBase<TView>`
- [ ] Заменить получение драйвера на `GetDriverFromContext()`
- [ ] Заменить поиск элементов на `FindElement()`
- [ ] Использовать `SafeExecute()` для обработки ошибок
- [ ] Применить Guard Clauses
- [ ] Разбить большие методы (>20 строк) на маленькие
- [ ] Использовать `CreateSuccessResult()` / `CreateErrorResult()`
- [ ] Добавить логирование через `LogInfo()` / `LogError()`
- [ ] Убрать дублирование кода
- [ ] Проверить что все тесты проходят

## Следующие активности для рефакторинга

**Приоритет 1 (высокий):**
- ElementInputBack
- ElementHoverBack
- ElementScrollToBack
- BrowserNavigateBack

**Приоритет 2 (средний):**
- ElementGetInfoBack
- ElementExistsBack
- BrowserWaitForBack

**Приоритет 3 (низкий):**
- BrowserScreenshotBack
- BrowserExecuteJavaScriptBack

## Преимущества после рефакторинга

1. **Меньше кода** - на 15-25% меньше строк
2. **Проще читать** - методы короткие и понятные
3. **Легче тестировать** - каждый метод тестируется отдельно
4. **Меньше ошибок** - единообразная обработка
5. **Быстрее разработка** - переиспользование базового класса
6. **Проще поддержка** - изменения в одном месте

## Заключение

Рефакторинг с использованием `BrowserActivityBase` значительно улучшает качество кода и упрощает разработку новых активностей. Рекомендуется применить этот паттерн ко всем Browser активностям.
