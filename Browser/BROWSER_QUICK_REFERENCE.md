# Browser Activities — Краткий справочник

## Быстрый старт

### Минимальный пример

```csharp
// 1. Открыть браузер
BrowserOpen → sessionId

// 2. Перейти на страницу
BrowserNavigate(sessionId, "https://example.com")

// 3. Найти элемент и кликнуть
ElementClick(sessionId, locatorType: Id, locatorValue: "submit-button")

// 4. Закрыть браузер
BrowserClose(sessionId)
```

## Все активности (алфавитный порядок)

### A-B

| Активность | Краткое описание | Входные параметры | Выходные параметры |
|---|---|---|---|
| **AlertHandle** | Работа с алертами | SessionId, Action, InputText | AlertText |
| **BrowserClose** | Закрыть браузер | SessionId | — |
| **BrowserExecuteJavaScript** | Выполнить JS код | SessionId, Script, Arguments | Result |
| **BrowserGetInfo** | Получить информацию | SessionId | CurrentUrl, PageTitle, PageSource |
| **BrowserGetLogs** | Получить логи | SessionId, LogType | Logs |
| **BrowserManageCookies** | Управление cookies | SessionId, Operation, CookieName, CookieValue | Cookies |
| **BrowserNavigate** | Перейти по URL | SessionId, Url | — |
| **BrowserOpen** | Открыть браузер | BrowserType, Headless, Options | SessionId |
| **BrowserScreenshot** | Скриншот страницы | SessionId, FilePath | ScreenshotBase64 |
| **BrowserStorageManage** | Управление Storage | SessionId, StorageType, Operation, Key, Value | Value, Keys, Length |
| **BrowserSwitchTo** | Переключить контекст | SessionId, SwitchToType, Target | — |
| **BrowserTabManage** | Управление вкладками | SessionId, Operation, Handle | WindowHandles, CurrentHandle |
| **BrowserWaitFor** | Ожидание условия | SessionId, Condition, Locator, Timeout | ConditionMet, WaitTime |
| **BrowserWindowManage** | Управление окном | SessionId, Operation, Width, Height | Size, Position |

### E

| Активность | Краткое описание | Входные параметры | Выходные параметры |
|---|---|---|---|
| **ElementClear** | Очистить поле | SessionId, ElementId/Locator | — |
| **ElementClick** | Клик по элементу | SessionId, ElementId/Locator, UseJavaScript | — |
| **ElementClickAndHold** | Нажать и удерживать | SessionId, ElementId/Locator, Duration | — |
| **ElementDoubleClick** | Двойной клик | SessionId, ElementId/Locator, UseJavaScript | — |
| **ElementDragDrop** | Перетащить элемент | SessionId, SourceId/Locator, TargetId/Locator | — |
| **ElementExists** | Проверить существование | SessionId, Locator | Exists |
| **ElementFind** | Найти элемент | SessionId, Locator, Timeout | ElementId |
| **ElementFindAll** | Найти все элементы | SessionId, Locator, Timeout | ElementIds, Count |
| **ElementGetComputedStyle** | Получить CSS свойство | SessionId, ElementId/Locator, PropertyName | Value |
| **ElementGetProperty** | Получить свойство | SessionId, ElementId, PropertyName | Value |
| **ElementGetRect** | Получить координаты | SessionId, ElementId/Locator | X, Y, Width, Height |
| **ElementGetScreenshot** | Скриншот элемента | SessionId, ElementId/Locator, FilePath | ScreenshotBase64 |
| **ElementHover** | Навести курсор | SessionId, ElementId/Locator | — |
| **ElementHoverWithOffset** | Навести со смещением | SessionId, ElementId/Locator, OffsetX, OffsetY | — |
| **ElementIsVisible** | Проверить состояние | SessionId, ElementId/Locator | IsVisible, IsEnabled, IsSelected |
| **ElementRightClick** | Правый клик | SessionId, ElementId/Locator | — |
| **ElementScrollTo** | Прокрутить к элементу | SessionId, ElementId/Locator, Alignment | — |
| **ElementSelect** | Выбор в dropdown | SessionId, ElementId/Locator, SelectMode, Value | — |
| **ElementSelectMultiple** | Выбор в multiple select | SessionId, ElementId/Locator, Operation, Values | SelectedOptions, AllOptions |
| **ElementSendKeys** | Отправить клавиши | SessionId, ElementId/Locator, SpecialKey, RepeatCount | — |
| **ElementSubmit** | Отправить форму | SessionId, ElementId/Locator | — |
| **ElementTypeText** | Ввести текст | SessionId, ElementId/Locator, Text, Options | — |
| **ElementUploadFile** | Загрузить файл | SessionId, ElementId/Locator, FilePath | — |

## Типы и перечисления

### BrowserType
```csharp
Chrome, Firefox, Edge
```

### ElementLocatorType
```csharp
Id, Name, ClassName, TagName, LinkText, PartialLinkText, CssSelector, XPath
```

### WaitConditionType
```csharp
ElementExists, ElementVisible, ElementClickable, ElementInvisible,
TextPresent, TitleContains, UrlContains, AlertPresent
```

### SelectMode
```csharp
ByText, ByValue, ByIndex
```

### ScrollAlignment
```csharp
Top, Center, Bottom
```

### StorageType
```csharp
LocalStorage, SessionStorage
```

### CookieOperation
```csharp
Get, GetAll, Set, Delete, DeleteAll
```

### StorageOperation
```csharp
GetItem, SetItem, RemoveItem, Clear, GetAllKeys, GetLength
```

### AlertAction
```csharp
Accept, Dismiss, GetText, SendKeys
```

### TabOperation
```csharp
OpenNewTab, CloseCurrentTab, CloseTabByHandle, GetAllHandles, 
GetCurrentHandle, SwitchToTab
```

### WindowOperation
```csharp
Maximize, Minimize, FullScreen, SetSize, SetPosition, GetSize, GetPosition
```

### SwitchToType
```csharp
Frame, Window, Alert, DefaultContent, ParentFrame
```

### SpecialKeyType
```csharp
Enter, Tab, Escape, Backspace, Delete, Space,
ArrowUp, ArrowDown, ArrowLeft, ArrowRight,
Home, End, PageUp, PageDown,
F1-F12, CtrlA, CtrlC, CtrlV, CtrlX, CtrlZ, ShiftTab, AltF4
```

### BrowserLogType
```csharp
Browser, Driver, Client, Server, Performance
```

### MultiSelectOperation
```csharp
SelectByText, SelectByValue, SelectByIndex,
DeselectByText, DeselectByValue, DeselectByIndex,
DeselectAll, GetAllOptions, GetSelectedOptions
```

## Частые комбинации

### Заполнение формы

```
1. ElementFind (Id="username") → usernameId
2. ElementTypeText (usernameId, "user@example.com")
3. ElementFind (Id="password") → passwordId
4. ElementTypeText (passwordId, "password123")
5. ElementFind (Id="submit") → submitId
6. ElementClick (submitId)
```

### Работа с dropdown

```
1. ElementFind (Id="country") → selectId
2. ElementSelect (selectId, ByText, "United States")
```

### Работа с multiple select

```
1. ElementFind (Id="skills") → selectId
2. ElementSelectMultiple (selectId, SelectByText, ["JavaScript", "Python"])
3. ElementSelectMultiple (selectId, GetSelectedOptions) → selected
```

### Проверка элемента перед действием

```
1. ElementExists (Id="submit-button") → exists
2. Если exists:
   - ElementIsVisible (Id="submit-button") → isVisible, isEnabled
   - Если isVisible И isEnabled:
     - ElementClick (Id="submit-button")
```

### Ожидание загрузки

```
1. ElementClick (Id="load-data")
2. BrowserWaitFor (ElementVisible, Id="data-table", Timeout=30)
3. ElementFindAll (CssSelector=".data-row") → rows
```

### Работа с алертом

```
1. ElementClick (Id="delete-button")
2. BrowserWaitFor (AlertPresent, Timeout=5)
3. AlertHandle (Accept)
```

### Извлечение данных таблицы

```
1. ElementFindAll (CssSelector="table tr") → rows
2. Для каждого row:
   - ElementFindAll (CssSelector="td") → cells
   - Для каждого cell:
     - ElementGetProperty (cell, "text") → cellText
```

### Работа с iframe

```
1. BrowserSwitchTo (Frame, Id="content-frame")
2. ElementClick (Id="button-in-frame")
3. BrowserSwitchTo (DefaultContent)
```

### Работа с новой вкладкой

```
1. ElementClick (Id="open-new-tab")
2. BrowserTabManage (GetAllHandles) → handles
3. BrowserTabManage (SwitchToTab, Index=1)
4. BrowserNavigate ("https://example.com")
5. BrowserTabManage (CloseCurrentTab)
6. BrowserTabManage (SwitchToTab, Index=0)
```

### Загрузка файла

```
1. ElementFind (CssSelector="input[type='file']") → fileInputId
2. ElementUploadFile (fileInputId, "C:\\path\\to\\file.pdf")
3. ElementClick (Id="upload-button")
```

### Скриншот при ошибке

```
Try:
  - [Основная логика]
Catch:
  - BrowserScreenshot (sessionId, "error_screenshot.png")
  - Логировать ошибку
```

### Работа с cookies

```
1. BrowserManageCookies (GetAll) → allCookies
2. BrowserManageCookies (Set, "session_id", "abc123")
3. BrowserNavigate (url)
4. BrowserManageCookies (Delete, "temp_cookie")
```

### Работа с localStorage

```
1. BrowserStorageManage (LocalStorage, SetItem, "user_id", "12345")
2. BrowserStorageManage (LocalStorage, GetItem, "user_id") → userId
3. BrowserStorageManage (LocalStorage, GetAllKeys) → keys
4. BrowserStorageManage (LocalStorage, Clear)
```

### Выполнение JavaScript

```
1. BrowserExecuteJavaScript (
     "return document.querySelectorAll('.item').length"
   ) → itemCount
2. BrowserExecuteJavaScript (
     "arguments[0].style.border = '2px solid red'",
     [element]
   )
```

## Советы по производительности

### 1. Переиспользование элементов
```
✅ Хорошо:
ElementFind → elementId
ElementClick (elementId)
ElementGetProperty (elementId, "text")

❌ Плохо:
ElementClick (Locator="...")
ElementGetProperty (Locator="...")  // Повторный поиск
```

### 2. Headless режим
```
BrowserOpen (Chrome, Headless=true)  // Быстрее на 30-50%
```

### 3. Отключение изображений
```
BrowserOpen (Chrome, DisableImages=true)  // Быстрее загрузка
```

### 4. Правильные таймауты
```
✅ Хорошо:
BrowserWaitFor (ElementVisible, Timeout=10)

❌ Плохо:
Thread.Sleep(5000)  // Всегда ждёт 5 секунд
```

### 5. Эффективные локаторы
```
Скорость (от быстрого к медленному):
1. Id
2. Name
3. ClassName
4. CssSelector
5. XPath
```

## Отладка

### Проверка состояния браузера
```
BrowserGetInfo → url, title, pageSource
```

### Проверка элемента
```
ElementExists → exists
ElementIsVisible → isVisible, isEnabled, isSelected
ElementGetProperty ("text") → text
ElementGetRect → x, y, width, height
```

### Скриншоты
```
BrowserScreenshot → screenshot.png
ElementGetScreenshot → element_screenshot.png
```

### Логи браузера
```
BrowserGetLogs (Browser) → consoleLogs
BrowserGetLogs (Driver) → driverLogs
```

### Пауза для отладки
```
BrowserExecuteJavaScript ("debugger;")  // Остановка в DevTools
```

## Обработка ошибок

### Проверка сессии
```
Try:
  BrowserGetInfo (sessionId)
Catch:
  "Сессия не существует" → Создать новую
```

### Проверка элемента
```
ElementExists (locator) → exists
Если НЕ exists:
  BrowserWaitFor (ElementExists, locator, Timeout=10)
```

### Retry логика
```
Повторить 3 раза:
  Try:
    ElementClick (locator)
    Break
  Catch:
    Если попытка < 3:
      Ждать 1 секунду
    Иначе:
      Выбросить ошибку
```

## Ограничения и известные проблемы

### 1. Stale Element Reference
**Проблема:** Элемент пересоздан DOM'ом после получения ссылки

**Решение:** Не сохраняйте элементы надолго, ищите заново

### 2. Element Not Clickable
**Проблема:** Элемент перекрыт или вне видимости

**Решение:** 
- ElementScrollTo перед кликом
- Или ElementClick с UseJavaScript=true

### 3. Timeout
**Проблема:** Элемент не появляется в течение таймаута

**Решение:**
- Увеличить таймаут
- Проверить локатор
- Использовать BrowserWaitFor

### 4. Frame/Window Context
**Проблема:** Элемент в другом frame или окне

**Решение:**
- BrowserSwitchTo (Frame/Window)
- Выполнить действия
- BrowserSwitchTo (DefaultContent)

### 5. Alert Blocking
**Проблема:** Алерт блокирует выполнение

**Решение:**
- BrowserWaitFor (AlertPresent)
- AlertHandle (Accept/Dismiss)

## Полезные ссылки

- **Selenium Documentation:** https://www.selenium.dev/documentation/
- **CSS Selectors Reference:** https://www.w3schools.com/cssref/css_selectors.asp
- **XPath Tutorial:** https://www.w3schools.com/xml/xpath_intro.asp
- **Primo RPA SDK:** https://docs.primo-rpa.ru

## Версия

Документ актуален для Primo MIA Browser Activities v1.0
