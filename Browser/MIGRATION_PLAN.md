# План миграции на новую систему ожидания элементов

## Выполнено ✅

### 1. Базовая инфраструктура
- ✅ `ElementWaitMode.cs` - enum с режимами ожидания
- ✅ `ElementLocator.cs` - обновлен с поддержкой режимов ожидания
- ✅ `ElementWaitingActivityBase.cs` - базовый класс для унифицированной логики
- ✅ `ElementWaitAndCheckBack.cs` - объединенная активность ожидания и проверки

### 2. Обновленные активности
- ✅ `ElementInputBack.Refactored.cs` - ожидание кликабельности
- ✅ `ElementHoverBack.Refactored.cs` - ожидание видимости  
- ✅ `ElementExistsBack.cs` - поддержка режимов ожидания
- ✅ `ElementGetInfoBack.Enhanced.cs` - новая версия с полной поддержкой

### 3. Высокий приоритет (интерактивные элементы) - ВЫПОЛНЕНО ✅
- ✅ `ElementDragDropBack.cs` - режим `Clickable` для исходного элемента, `Visible` для целевого
- ✅ `ElementSelectBack.cs` - режим `Clickable` для select элементов
- ✅ `ElementSelectMultipleBack.cs` - режим `Clickable` для multiple select
- ✅ `ElementSubmitBack.cs` - режим `Clickable` для форм
- ✅ `ElementUploadFileBack.cs` - режим `Clickable` для input[type=file]

### 4. Средний приоритет (визуальные операции) - ВЫПОЛНЕНО ✅
- ✅ `ElementScrollToBack.cs` - режим `Visible` для прокрутки к элементу
- ✅ `ElementGetRectBack.cs` - режим `Visible` для получения размеров
- ✅ `ElementGetScreenshotBack.cs` - режим `Visible` для скриншотов

### 5. Низкий приоритет (информационные операции) - ВЫПОЛНЕНО ✅
- ✅ `ElementGetPropertyBack.cs` - режим `Present` с поддержкой локаторов
- ✅ `ElementGetComputedStyleBack.cs` - режим `Present` (уже использует правильную логику)

### 5. Документация
- ✅ `ELEMENT_WAITING_SYSTEM.md` - полное описание системы
- ✅ `MIGRATION_PLAN.md` - план миграции
- ✅ `IMPLEMENTATION_SUMMARY.md` - резюме реализации

## Требуется выполнить 🔄

### Оставшиеся активности (опционально)

Все основные активности мигрированы! Оставшиеся активности уже используют правильную логику или имеют низкий приоритет:

- `ElementGetComputedStyleBack.cs` - уже использует правильную логику ожидания
- Другие специализированные активности могут быть обновлены по мере необходимости

## МИГРАЦИЯ ЗАВЕРШЕНА! ✅

Все активности высокого, среднего и низкого приоритета успешно мигрированы на новую систему ожидания элементов.

### Шаблон миграции

```csharp
// 1. Добавить поля для режима ожидания
private string _propWaitMode;
[LTools.Common.Model.Serialization.StoringProperty]
[System.ComponentModel.Category(ActivityStrings.Category_Wait),
 System.ComponentModel.DisplayName("Режим ожидания")]
public ElementWaitMode Prop_WaitMode
{
    get => (ElementWaitMode)Enum.Parse(typeof(ElementWaitMode), _propWaitMode ?? "Clickable");
    set { _propWaitMode = value.ToString(); InvokePropertyChanged(this, "Prop_WaitMode"); }
}

private string _propWaitTimeout;
[LTools.Common.Model.Serialization.StoringProperty]
[LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
[System.ComponentModel.Category(ActivityStrings.Category_Wait),
 System.ComponentModel.DisplayName(ActivityStrings.Field_WaitTimeout)]
public string Prop_WaitTimeout
{
    get => _propWaitTimeout;
    set { _propWaitTimeout = value; InvokePropertyChanged(this, "Prop_WaitTimeout"); }
}

// 2. Обновить sdkProperties
PropertyBuilder.Enum<ElementWaitMode>("Prop_WaitMode", "Режим ожидания"),
PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)"),

// 3. Обновить конструктор
this.Prop_WaitMode = ElementWaitMode.Clickable; // или другой подходящий режим
this.Prop_WaitTimeout = "10";

// 4. Обновить ResolveElement метод
private IWebElement ResolveElement(ScriptingData sd, IWebDriver driver)
{
    string elementId = GetPropertyValue<string>(Prop_ElementId, nameof(Prop_ElementId), sd);
    string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), sd);
    
    if (string.IsNullOrWhiteSpace(locatorValue) && string.IsNullOrWhiteSpace(elementId))
        throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор для поиска");

    if (!string.IsNullOrWhiteSpace(locatorValue))
    {
        string waitStr = GetPropertyValue<string>(Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
        int timeout = int.TryParse(waitStr, out int t) ? t : 10;
        ValidatePositive(timeout, "Prop_WaitTimeout");
        
        var elementLocator = new ElementLocator();
        var locatorType = ConvertLocatorType(Prop_LocatorType);
        
        return elementLocator.FindElementWithWaitMode(
            driver, locatorType, locatorValue, timeout, Prop_WaitMode);
    }

    if (!string.IsNullOrWhiteSpace(elementId))
    {
        var element = ElementRepository.GetElement(elementId);
        
        // Валидация элемента из репозитория согласно режиму ожидания
        if (element != null)
        {
            switch (Prop_WaitMode)
            {
                case ElementWaitMode.Visible:
                    if (!element.Displayed)
                        throw new ElementNotInteractableException($"Элемент с ID '{elementId}' не видим");
                    break;
                case ElementWaitMode.Clickable:
                    if (!element.Displayed || !element.Enabled)
                        throw new ElementNotInteractableException($"Элемент с ID '{elementId}' не кликабельный");
                    break;
            }
        }
        
        return element;
    }

    return null;
}
```

## Альтернативный подход: Наследование от ElementWaitingActivityBase

Для новых активностей рекомендуется наследование от `ElementWaitingActivityBase<T>`:

```csharp
public class ElementDragDropBackEnhanced : ElementWaitingActivityBase<ElementDragDrop>
{
    // Использовать методы базового класса:
    // - ResolveElementWithWait()
    // - ResolveClickableElement()
    // - ResolveVisibleElement()
    // - ResolvePresentElement()
}
```

## Тестирование

### Сценарии для проверки
1. **Режим Present**: Поиск скрытых элементов (`display: none`)
2. **Режим Visible**: Ожидание появления элементов после анимации
3. **Режим Clickable**: Ожидание активации disabled кнопок
4. **Режим None**: Быстрый поиск статических элементов

### Регрессионное тестирование
- Проверить совместимость с существующими процессами
- Убедиться в корректности таймаутов
- Проверить обработку ошибок

## Преимущества после миграции

1. **Единообразие**: Все активности используют одинаковую логику ожидания
2. **Гибкость**: Пользователь может выбрать оптимальный режим ожидания
3. **Производительность**: Правильные стратегии ожидания для каждого случая
4. **Надежность**: Явные проверки состояния элементов
5. **Отладка**: Подробное логирование процесса ожидания

## Временные рамки

- **Неделя 1**: Высокий приоритет (5 активностей)
- **Неделя 2**: Средний приоритет (3 активности)  
- **Неделя 3**: Низкий приоритет (2 активности)
- **Неделя 4**: Тестирование и документация

## Обратная совместимость

Все изменения сохраняют обратную совместимость:
- Существующие процессы продолжат работать
- Новые свойства имеют разумные значения по умолчанию
- Старые методы поиска элементов остаются доступными