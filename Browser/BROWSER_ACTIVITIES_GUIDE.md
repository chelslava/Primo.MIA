# Руководство по Browser-активностям Primo MIA

## Обзор

Browser-активности в Primo MIA предоставляют полнофункциональную автоматизацию веб-браузеров через Selenium WebDriver. Все активности следуют единой архитектуре и используют централизованные helper-классы.

## Архитектура

### Структура активности

Каждая Browser-активность состоит из двух файлов:

1. **`[Name]Back.cs`** — серверная логика (бэкенд)
   - Наследуется от `PrimoComponentTO<[Name]>`
   - Содержит свойства, логику выполнения, валидацию
   - Точка входа — метод `TimedAction(ScriptingData sd)`

2. **`[Name].xaml` + `[Name].xaml.cs`** — визуальный компонент
   - Отображение в дизайнере Primo RPA Studio
   - Биндинги к свойствам бэкенда

### Управление сессиями

Все Browser-активности работают через систему сессий:

```csharp
// Создание сессии (BrowserOpen)
string sessionId = SeleniumHelper.GenerateSessionId();
IWebDriver driver = CreateDriver(...);
RepoDict.Set(sessionId, driver);

// Использование сессии (все остальные активности)
var driver = SeleniumHelper.GetDriver(sessionId);

// Закрытие сессии (BrowserClose)
driver.Quit();
RepoDict.Remove(sessionId);
```

### Управление элементами

Элементы могут быть найдены и сохранены для повторного использования:

```csharp
// Поиск и сохранение элемента (ElementFind)
string elementId = SeleniumHelper.GenerateElementId();
IWebElement element = SeleniumHelper.WaitForElement(driver, locator, timeout);
RepoDict.Set(elementId, element);

// Использование сохранённого элемента
var element = SeleniumHelper.GetElement(elementId);
```

## Категории активностей

### 1. Управление браузером

| Активность | Назначение |
|---|---|
| `BrowserOpen` | Открытие браузера и создание сессии |
| `BrowserClose` | Закрытие браузера и освобождение ресурсов |
| `BrowserNavigate` | Переход по URL |
| `BrowserGetInfo` | Получение информации (URL, Title, PageSource) |
| `BrowserWaitFor` | Ожидание условий (элементы, текст, URL, алерты) |
| `BrowserExecuteJavaScript` | Выполнение JavaScript кода |
| `BrowserScreenshot` | Создание скриншота страницы |

### 2. Поиск элементов

| Активность | Назначение |
|---|---|
| `ElementFind` | Найти один элемент по локатору |
| `ElementFindAll` | Найти все элементы по локатору |
| `ElementExists` | Проверить существование элемента |
| `ElementIsVisible` | Проверить состояние элемента |

### 3. Взаимодействие с элементами

| Активность | Назначение |
|---|---|
| `ElementClick` | Клик по элементу |
| `ElementDoubleClick` | Двойной клик |
| `ElementRightClick` | Правый клик (контекстное меню) |
| `ElementClickAndHold` | Нажать и удерживать |
| `ElementTypeText` | Ввод текста |
| `ElementClear` | Очистка поля |
| `ElementSendKeys` | Отправка специальных клавиш |
| `ElementSubmit` | Отправка формы |
| `ElementHover` | Наведение курсора |
| `ElementHoverWithOffset` | Наведение со смещением |
| `ElementScrollTo` | Прокрутка к элементу |
| `ElementDragDrop` | Перетаскивание элемента |

### 4. Получение данных элемента

| Активность | Назначение |
|---|---|
| `ElementGetProperty` | Получить свойство/атрибут |
| `ElementGetComputedStyle` | Получить CSS свойство |
| `ElementGetRect` | Получить координаты и размеры |
| `ElementGetScreenshot` | Скриншот элемента |

### 5. Работа с формами

| Активность | Назначение |
|---|---|
| `ElementSelect` | Выбор в dropdown (single select) |
| `ElementSelectMultiple` | Выбор в multiple select |
| `ElementUploadFile` | Загрузка файла |

### 6. Управление окнами и вкладками

| Активность | Назначение |
|---|---|
| `BrowserWindowManage` | Управление окном (размер, позиция, maximize) |
| `BrowserTabManage` | Управление вкладками (открыть, закрыть, переключить) |
| `BrowserSwitchTo` | Переключение контекста (frame, window, alert) |
| `AlertHandle` | Работа с алертами |

### 7. Хранилище и cookies

| Активность | Назначение |
|---|---|
| `BrowserManageCookies` | Управление cookies |
| `BrowserStorageManage` | Управление localStorage/sessionStorage |
| `BrowserGetLogs` | Получение логов браузера |

## Типы локаторов

```csharp
public enum ElementLocatorType
{
    Id,                  // По атрибуту id
    Name,                // По атрибуту name
    ClassName,           // По CSS классу
    TagName,             // По имени тега
    LinkText,            // По полному тексту ссылки
    PartialLinkText,     // По частичному тексту ссылки
    CssSelector,         // По CSS селектору
    XPath                // По XPath выражению
}
```

## Типы условий ожидания

```csharp
public enum WaitConditionType
{
    ElementExists,       // Элемент существует в DOM
    ElementVisible,      // Элемент видим на странице
    ElementClickable,    // Элемент кликабелен (видим и включён)
    ElementInvisible,    // Элемент невидим или отсутствует
    TextPresent,         // Текст присутствует в элементе
    TitleContains,       // Заголовок содержит текст
    UrlContains,         // URL содержит текст
    AlertPresent         // Алерт присутствует
}
```

## SeleniumHelper — централизованные методы

### Управление сессиями

```csharp
// Получить драйвер по ID сессии
IWebDriver driver = SeleniumHelper.GetDriver(sessionId);

// Проверить существование сессии
bool exists = SeleniumHelper.SessionExists(sessionId);

// Генерация ID
string sessionId = SeleniumHelper.GenerateSessionId();
string elementId = SeleniumHelper.GenerateElementId();
```

### Создание локаторов

```csharp
By locator = SeleniumHelper.CreateLocator(
    ElementLocatorType.Id, 
    "myElementId"
);
```

### Поиск и ожидание элементов

```csharp
// Ожидание появления элемента
IWebElement element = SeleniumHelper.WaitForElement(
    driver, locator, timeoutSeconds: 10
);

// Ожидание видимости
IWebElement element = SeleniumHelper.WaitForElementVisible(
    driver, locator, timeoutSeconds: 10
);

// Ожидание кликабельности
IWebElement element = SeleniumHelper.WaitForElementClickable(
    driver, locator, timeoutSeconds: 10
);

// Проверка существования без ожидания
bool exists = SeleniumHelper.ElementExists(driver, locator);
```

### Работа с JavaScript

```csharp
// Выполнение JavaScript
object result = SeleniumHelper.ExecuteJavaScript(
    driver, 
    "return document.title;", 
    args
);

// Прокрутка к элементу
SeleniumHelper.ScrollToElement(
    driver, 
    element, 
    ScrollAlignment.Center
);
```

### Скриншоты

```csharp
// Скриншот страницы в Base64
string base64 = SeleniumHelper.TakeScreenshotBase64(driver);

// Скриншот в файл
SeleniumHelper.TakeScreenshotToFile(driver, filePath);

// Скриншот элемента
string base64 = SeleniumHelper.TakeElementScreenshotBase64(element);
```

### Расширенные действия

```csharp
// Двойной клик
SeleniumHelper.DoubleClick(driver, element);

// Правый клик
SeleniumHelper.RightClick(driver, element);

// Drag and Drop
SeleniumHelper.DragAndDrop(driver, sourceElement, targetElement);

// Наведение со смещением
SeleniumHelper.HoverWithOffset(driver, element, offsetX, offsetY);
```

### Управление окном

```csharp
// Развернуть окно
SeleniumHelper.MaximizeWindow(driver);

// Установить размер
SeleniumHelper.SetWindowSize(driver, width, height);

// Получить размер
var (width, height) = SeleniumHelper.GetWindowSize(driver);
```

### Управление вкладками

```csharp
// Открыть новую вкладку
SeleniumHelper.OpenNewTab(driver, url);

// Закрыть текущую вкладку
SeleniumHelper.CloseCurrentTab(driver);

// Получить все handles
List<string> handles = SeleniumHelper.GetAllWindowHandles(driver);

// Переключиться на вкладку по индексу
SeleniumHelper.SwitchToTabByIndex(driver, index);
```

### Web Storage

```csharp
// Получить значение из localStorage
string value = SeleniumHelper.GetStorageItem(
    driver, 
    StorageType.LocalStorage, 
    "key"
);

// Установить значение
SeleniumHelper.SetStorageItem(
    driver, 
    StorageType.LocalStorage, 
    "key", 
    "value"
);

// Очистить хранилище
SeleniumHelper.ClearStorage(driver, StorageType.LocalStorage);
```

### Multiple Select

```csharp
// Выбрать опцию
SeleniumHelper.SelectMultipleByText(selectElement, "Option 1");
SeleniumHelper.SelectMultipleByValue(selectElement, "value1");
SeleniumHelper.SelectMultipleByIndex(selectElement, 0);

// Снять выбор
SeleniumHelper.DeselectByText(selectElement, "Option 1");
SeleniumHelper.DeselectAll(selectElement);

// Получить опции
List<string> allOptions = SeleniumHelper.GetAllSelectOptions(selectElement);
List<string> selected = SeleniumHelper.GetSelectedSelectOptions(selectElement);
```

## Паттерны использования

### Паттерн 1: Поиск элемента с альтернативами

Многие активности поддерживают два способа указания элемента:

```csharp
// Вариант 1: Использование сохранённого элемента
if (!string.IsNullOrWhiteSpace(elementId))
{
    element = SeleniumHelper.GetElement(elementId);
}
// Вариант 2: Поиск по локатору
else if (!string.IsNullOrWhiteSpace(locatorValue))
{
    var locator = SeleniumHelper.CreateLocator(locatorType, locatorValue);
    element = SeleniumHelper.WaitForElement(driver, locator, timeout);
}
else
{
    throw new ArgumentException(
        "Необходимо указать либо ID элемента, либо локатор"
    );
}
```

### Паттерн 2: Безопасное чтение параметров

```csharp
// Строка с fallback
string value = GetPropertyValue<string>(
    this.Prop_Value, 
    "Prop_Value", 
    sd
) ?? "default";

// Целое число с парсингом
string timeoutStr = GetPropertyValue<string>(
    this.Prop_Timeout, 
    "Prop_Timeout", 
    sd
) ?? "10";
int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
timeout = SeleniumHelper.ValidateTimeout(timeout, 10);
```

### Паттерн 3: Обработка ошибок

```csharp
public override ExecutionResult TimedAction(ScriptingData sd)
{
    try
    {
        // Логика выполнения
        
        return new ExecutionResult
        {
            IsSuccess = true,
            SuccessMessage = $"[Активность] Результат"
        };
    }
    catch (Exception ex)
    {
        return new ExecutionResult
        {
            IsSuccess = false,
            ErrorMessage = $"Ошибка [Активность]: {ex.Message}"
        };
    }
}
```

### Паттерн 4: Валидация

```csharp
public override ValidationResult Validate()
{
    var ret = new ValidationResult();
    
    // Обязательные поля
    ret.ValidateRequired(
        this.Prop_SessionId, 
        ActivityStrings.Field_SessionId, 
        "ID сессии обязателен"
    );
    
    // Условная валидация
    bool hasElementId = !string.IsNullOrWhiteSpace(this.Prop_ElementId);
    bool hasLocator = !string.IsNullOrWhiteSpace(this.Prop_LocatorValue);
    
    if (!hasElementId && !hasLocator)
    {
        ret.Items.Add(new ValidationResult.ValidationItem()
        {
            PropertyName = "ElementId/LocatorValue",
            Error = "Необходимо указать либо ID элемента, либо локатор"
        });
    }
    
    return ret;
}
```

## Типичные сценарии использования

### Сценарий 1: Базовая автоматизация формы

```
1. BrowserOpen → sessionId
2. BrowserNavigate → URL формы
3. ElementFind (локатор: Id="username") → elementId1
4. ElementTypeText (elementId1, текст: "user@example.com")
5. ElementFind (локатор: Id="password") → elementId2
6. ElementTypeText (elementId2, текст: "password123")
7. ElementFind (локатор: Id="submit") → elementId3
8. ElementClick (elementId3)
9. BrowserWaitFor (условие: UrlContains, текст: "/dashboard")
10. BrowserClose (sessionId)
```

### Сценарий 2: Извлечение данных

```
1. BrowserOpen → sessionId
2. BrowserNavigate → URL страницы
3. ElementFindAll (локатор: CssSelector=".product-item") → elementIds
4. Для каждого elementId:
   - ElementGetProperty (elementId, "text") → название
   - ElementGetProperty (elementId, "attribute:data-price") → цена
   - Сохранить в список
5. BrowserClose (sessionId)
```

### Сценарий 3: Условная логика

```
1. BrowserOpen → sessionId
2. BrowserNavigate → URL
3. ElementExists (локатор: Id="error-message") → hasError
4. Если hasError:
   - ElementGetProperty ("error-message", "text") → errorText
   - Логировать ошибку
   - Выход
5. Иначе:
   - Продолжить обработку
6. BrowserClose (sessionId)
```

## Рекомендации

### 1. Управление сессиями

- Всегда закрывайте браузер через `BrowserClose`
- Используйте try-finally для гарантированного закрытия
- Не создавайте множество сессий без необходимости

### 2. Ожидания

- Используйте явные ожидания через `BrowserWaitFor` или `WaitForElement`
- Избегайте `Thread.Sleep` — используйте условия ожидания
- Устанавливайте разумные таймауты (10-30 секунд)

### 3. Локаторы

- Предпочитайте `Id` и `Name` — они самые быстрые
- `CssSelector` быстрее чем `XPath`
- Избегайте сложных XPath выражений
- Используйте уникальные локаторы

### 4. Производительность

- Переиспользуйте найденные элементы через `ElementFind`
- Отключайте изображения для ускорения (`DisableImages`)
- Используйте headless режим где возможно
- Минимизируйте количество поисков элементов

### 5. Отладка

- Используйте `BrowserScreenshot` для диагностики
- Проверяйте `BrowserGetLogs` для ошибок JavaScript
- Используйте `ElementExists` перед взаимодействием
- Логируйте важные шаги процесса

## Частые ошибки и решения

### Ошибка: "Сессия не найдена"

**Причина:** Браузер был закрыт или sessionId неверный

**Решение:**
```csharp
if (!SeleniumHelper.SessionExists(sessionId))
{
    throw new Exception("Сессия браузера не существует");
}
```

### Ошибка: "Элемент не найден"

**Причина:** Элемент ещё не загрузился или локатор неверный

**Решение:**
- Увеличьте таймаут ожидания
- Используйте `BrowserWaitFor` с условием `ElementVisible`
- Проверьте локатор через DevTools браузера

### Ошибка: "Stale element reference"

**Причина:** Элемент был пересоздан DOM'ом после получения ссылки

**Решение:**
- Ищите элемент заново перед каждым использованием
- Не сохраняйте элементы надолго
- Используйте локаторы вместо сохранённых элементов

### Ошибка: "Element not clickable"

**Причина:** Элемент перекрыт другим элементом или невидим

**Решение:**
```csharp
// Прокрутить к элементу
SeleniumHelper.ScrollToElement(driver, element, ScrollAlignment.Center);

// Или использовать JavaScript клик
var jsExecutor = (IJavaScriptExecutor)driver;
jsExecutor.ExecuteScript("arguments[0].click();", element);
```

## Расширение функциональности

### Добавление новой активности

1. Создайте файлы `[Name]Back.cs`, `[Name].xaml`, `[Name].xaml.cs`
2. Следуйте структуре существующих активностей
3. Используйте `SeleniumHelper` для общих операций
4. Добавьте константы в `ActivityStrings`
5. Обновите документацию

### Добавление метода в SeleniumHelper

```csharp
/// <summary>
/// Описание метода на русском языке.
/// </summary>
/// <param name="driver">Экземпляр WebDriver</param>
/// <param name="param">Описание параметра</param>
/// <returns>Описание возвращаемого значения</returns>
public static ReturnType MethodName(IWebDriver driver, ParamType param)
{
    // Реализация
}
```

## Заключение

Browser-активности Primo MIA предоставляют мощный и гибкий инструментарий для автоматизации веб-браузеров. Следуя описанным паттернам и рекомендациям, вы сможете создавать надёжные и эффективные процессы автоматизации.

Для получения дополнительной информации обращайтесь к:
- Исходному коду существующих активностей
- Документации Selenium WebDriver
- Документации Primo RPA SDK
