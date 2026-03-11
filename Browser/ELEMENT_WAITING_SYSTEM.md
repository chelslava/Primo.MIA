# Система ожидания элементов

## Обзор

Новая система ожидания элементов унифицирует логику поиска и ожидания элементов во всех браузерных активностях. Система основана на enum `ElementWaitMode` и предоставляет четыре режима ожидания.

## Режимы ожидания (ElementWaitMode)

### Present
- **Описание**: Ожидает появления элемента в DOM
- **Условие**: Элемент существует в структуре страницы
- **Использование**: Для элементов, которые могут быть невидимыми, но должны присутствовать
- **Пример**: Скрытые поля формы, элементы с `display: none`

### Visible  
- **Описание**: Ожидает видимости элемента на странице
- **Условие**: `element.Displayed == true`
- **Использование**: Для элементов, с которыми пользователь должен взаимодействовать визуально
- **Пример**: Кнопки, текстовые поля, изображения

### Clickable
- **Описание**: Ожидает кликабельности элемента
- **Условие**: `element.Displayed == true && element.Enabled == true`
- **Использование**: Для интерактивных элементов (по умолчанию для кликов и ввода)
- **Пример**: Активные кнопки, поля ввода, ссылки

### None
- **Описание**: Не ожидает, пытается найти элемент немедленно
- **Условие**: Без дополнительных проверок
- **Использование**: Когда элемент гарантированно присутствует
- **Пример**: Статические элементы, уже проверенные элементы

## Архитектура

### ElementLocator (обновлен)
```csharp
// Новый метод с поддержкой режимов ожидания
IWebElement FindElementWithWaitMode(
    IWebDriver driver,
    LocatorType locatorType, 
    string locatorValue,
    int timeoutSeconds,
    ElementWaitMode waitMode)

// Безопасная версия (возвращает null вместо исключения)
IWebElement TryFindElementWithWaitMode(...)
```

### ElementWaitingActivityBase<T>
Базовый класс для активностей с унифицированной логикой ожидания:

```csharp
// Универсальный метод разрешения элементов
protected IWebElement ResolveElementWithWait(
    ScriptingData sd,
    IWebDriver driver, 
    string elementId,
    string locatorValue,
    ElementLocatorType locatorType,
    ElementWaitMode waitMode,
    int timeoutSeconds)

// Специализированные методы
protected IWebElement ResolveClickableElement(...)
protected IWebElement ResolveVisibleElement(...)
protected IWebElement ResolvePresentElement(...)
```

### ElementWaitAndCheckBack
Объединенная активность для ожидания и проверки состояния элемента:

- Заменяет `ElementIsVisibleBack` с расширенной функциональностью
- Поддерживает все режимы ожидания через enum
- Возвращает флаги: `ElementFound`, `IsVisible`, `IsEnabled`, `IsSelected`

## Миграция существующих активностей

### Обновленные активности

1. **ElementClickBack.Refactored.cs** ✅
   - Уже использует `ElementLocator.WaitForClickable`
   - Режим: `Clickable` (по умолчанию)

2. **ElementInputBack.Refactored.cs** ✅ 
   - Обновлен для ожидания кликабельности
   - Режим: `Clickable`

3. **ElementHoverBack.Refactored.cs** ✅
   - Обновлен для ожидания видимости  
   - Режим: `Visible`

4. **ElementGetInfoBack.Enhanced.cs** ✅
   - Новая версия с поддержкой всех режимов
   - Режим: `Present` (по умолчанию)

### Активности требующие обновления

- `ElementScrollToBack.cs` → режим `Visible`
- `ElementGetPropertyBack.cs` → режим `Present` 
- `ElementGetRectBack.cs` → режим `Visible`
- `ElementGetScreenshotBack.cs` → режим `Visible`
- `ElementDragDropBack.cs` → режим `Clickable`
- `ElementSelectBack.cs` → режим `Clickable`
- `ElementSelectMultipleBack.cs` → режим `Clickable`
- `ElementSubmitBack.cs` → режим `Clickable`
- `ElementUploadFileBack.cs` → режим `Clickable`
- `ElementExistsBack.cs` → режим `Present`

## Рекомендации по использованию

### Выбор режима ожидания

| Тип активности | Рекомендуемый режим | Обоснование |
|----------------|-------------------|-------------|
| Клик, ввод текста | `Clickable` | Элемент должен быть интерактивным |
| Наведение курсора | `Visible` | Достаточно видимости |
| Получение свойств | `Present` | Свойства доступны у невидимых элементов |
| Скриншот | `Visible` | Нужен видимый элемент |
| Проверка существования | `Present` | Проверяем только наличие в DOM |

### Таймауты

- **По умолчанию**: 10 секунд
- **Быстрые проверки**: 3-5 секунд  
- **Медленные загрузки**: 15-30 секунд
- **Режим None**: таймаут игнорируется

### Обработка ошибок

```csharp
// Безопасный поиск (возвращает null)
var element = elementLocator.TryFindElementWithWaitMode(...);
if (element == null) {
    // Элемент не найден - обработать gracefully
}

// Поиск с исключением
try {
    var element = elementLocator.FindElementWithWaitMode(...);
} catch (NoSuchElementException) {
    // Элемент не найден
} catch (ElementNotInteractableException) {
    // Элемент найден, но не соответствует режиму ожидания
}
```

## Преимущества новой системы

1. **Унификация**: Единый подход к ожиданию во всех активностях
2. **Гибкость**: Четыре режима покрывают все сценарии использования  
3. **Производительность**: Оптимальные стратегии ожидания для каждого случая
4. **Надежность**: Явные проверки состояния элементов
5. **Читаемость**: Понятные названия режимов ожидания
6. **Совместимость**: Обратная совместимость с существующим кодом

## Примеры использования

### Базовое ожидание и проверка
```csharp
// Ожидание видимого элемента
var element = ResolveVisibleElement(sd, driver, elementId, locatorValue, locatorType, 10);

// Ожидание кликабельного элемента  
var button = ResolveClickableElement(sd, driver, "", "button#submit", ElementLocatorType.CssSelector, 15);
```

### Использование ElementWaitAndCheckBack
```csharp
// В активности настроить:
Prop_WaitMode = ElementWaitMode.Visible;
Prop_WaitTimeout = "10";

// Результат в переменных:
// ElementFound = true/false
// IsVisible = true/false  
// IsEnabled = true/false
// IsSelected = true/false
```

### Миграция с ElementIsVisibleBack
```csharp
// Старый код:
ElementIsVisibleBack.TimedAction(sd);

// Новый код:
ElementWaitAndCheckBack activity = new ElementWaitAndCheckBack(container);
activity.Prop_WaitMode = ElementWaitMode.Visible; // или другой режим
activity.TimedAction(sd);
```