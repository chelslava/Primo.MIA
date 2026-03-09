# План реализации недостающих активностей Selenium

## Приоритет 1: Критичные активности (реализовать в первую очередь)

### 1.1 ElementDragDrop — Перетаскивание элементов

**Описание:** Drag and Drop элемента из одной позиции в другую

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_SourceElementId` / `Prop_SourceLocator` — исходный элемент
- `Prop_TargetElementId` / `Prop_TargetLocator` — целевой элемент
- `Prop_OffsetX` / `Prop_OffsetY` — смещение (альтернатива целевому элементу)
- `Prop_WaitTimeout` — таймаут ожидания
- `Prop_UseJavaScript` — использовать JS для drag-drop (для сложных случаев)

**Требуемые методы в SeleniumHelper:**
```csharp
public static void DragAndDrop(IWebDriver driver, IWebElement source, IWebElement target)
public static void DragAndDropByOffset(IWebDriver driver, IWebElement source, int offsetX, int offsetY)
public static void DragAndDropJS(IWebDriver driver, IWebElement source, IWebElement target)
```

**Зависимости:** OpenQA.Selenium.Interactions.Actions

**Сложность:** Средняя

---

### 1.2 ElementDoubleClick — Двойной клик

**Описание:** Выполняет двойной клик по элементу

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_ElementId` / `Prop_Locator` — элемент для клика
- `Prop_WaitTimeout` — таймаут ожидания
- `Prop_UseJavaScript` — использовать JS для двойного клика
- `Prop_WaitAfterClick` — ожидание после клика (мс)

**Требуемые методы в SeleniumHelper:**
```csharp
public static void DoubleClick(IWebDriver driver, IWebElement element)
public static void DoubleClickJS(IWebDriver driver, IWebElement element)
```

**Зависимости:** OpenQA.Selenium.Interactions.Actions

**Сложность:** Низкая

---

### 1.3 ElementRightClick — Правый клик (контекстное меню)

**Описание:** Выполняет правый клик по элементу для вызова контекстного меню

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_ElementId` / `Prop_Locator` — элемент для клика
- `Prop_WaitTimeout` — таймаут ожидания
- `Prop_WaitAfterClick` — ожидание после клика (мс)

**Требуемые методы в SeleniumHelper:**
```csharp
public static void RightClick(IWebDriver driver, IWebElement element)
```

**Зависимости:** OpenQA.Selenium.Interactions.Actions

**Сложность:** Низкая

---

### 1.4 ElementUploadFile — Загрузка файла

**Описание:** Загружает файл через input[type=file]

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_ElementId` / `Prop_Locator` — элемент input[type=file]
- `Prop_FilePath` — путь к файлу для загрузки
- `Prop_WaitTimeout` — таймаут ожидания
- `Prop_VerifyFileExists` — проверить существование файла перед загрузкой

**Требуемые методы в SeleniumHelper:**
```csharp
public static void UploadFile(IWebElement fileInput, string filePath)
public static bool IsFileInputElement(IWebElement element)
```

**Зависимости:** System.IO для проверки файла

**Сложность:** Низкая

---

### 1.5 ElementGetRect — Получить размер и позицию элемента

**Описание:** Получает координаты и размеры элемента на странице

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_ElementId` / `Prop_Locator` — элемент
- `Prop_WaitTimeout` — таймаут ожидания
- `Prop_X` — выходная переменная: координата X (int)
- `Prop_Y` — выходная переменная: координата Y (int)
- `Prop_Width` — выходная переменная: ширина (int)
- `Prop_Height` — выходная переменная: высота (int)

**Требуемые методы в SeleniumHelper:**
```csharp
public static (int x, int y, int width, int height) GetElementRect(IWebElement element)
```

**Зависимости:** Нет

**Сложность:** Низкая

---

### 1.6 BrowserWindowManage — Управление окном браузера

**Описание:** Управление размером, позицией и состоянием окна браузера

**Enum WindowOperation:**
```csharp
public enum WindowOperation
{
    Maximize,      // Развернуть на весь экран
    Minimize,      // Свернуть окно
    FullScreen,    // Полноэкранный режим (F11)
    SetSize,       // Установить размер
    SetPosition,   // Установить позицию
    GetSize,       // Получить размер
    GetPosition    // Получить позицию
}
```

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_Operation` — тип операции (WindowOperation)
- `Prop_Width` — ширина окна (для SetSize)
- `Prop_Height` — высота окна (для SetSize)
- `Prop_X` — позиция X (для SetPosition)
- `Prop_Y` — позиция Y (для SetPosition)
- `Prop_OutWidth` — выходная переменная: текущая ширина
- `Prop_OutHeight` — выходная переменная: текущая высота
- `Prop_OutX` — выходная переменная: текущая позиция X
- `Prop_OutY` — выходная переменная: текущая позиция Y

**Требуемые методы в SeleniumHelper:**
```csharp
public static void MaximizeWindow(IWebDriver driver)
public static void MinimizeWindow(IWebDriver driver)
public static void FullScreenWindow(IWebDriver driver)
public static void SetWindowSize(IWebDriver driver, int width, int height)
public static void SetWindowPosition(IWebDriver driver, int x, int y)
public static (int width, int height) GetWindowSize(IWebDriver driver)
public static (int x, int y) GetWindowPosition(IWebDriver driver)
```

**Зависимости:** Нет

**Сложность:** Средняя

---

### 1.7 BrowserTabManage — Управление вкладками

**Описание:** Открытие, закрытие, переключение между вкладками

**Enum TabOperation:**
```csharp
public enum TabOperation
{
    OpenNewTab,        // Открыть новую вкладку
    CloseCurrentTab,   // Закрыть текущую вкладку
    CloseTabByHandle,  // Закрыть вкладку по handle
    GetAllHandles,     // Получить список всех handles
    GetCurrentHandle,  // Получить handle текущей вкладки
    SwitchToTab        // Переключиться на вкладку по индексу
}
```

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_Operation` — тип операции
- `Prop_TabIndex` — индекс вкладки (для SwitchToTab)
- `Prop_TabHandle` — handle вкладки (для CloseTabByHandle)
- `Prop_Url` — URL для открытия в новой вкладке
- `Prop_OutHandles` — выходная переменная: List<string> handles
- `Prop_OutCurrentHandle` — выходная переменная: текущий handle
- `Prop_OutTabCount` — выходная переменная: количество вкладок

**Требуемые методы в SeleniumHelper:**
```csharp
public static void OpenNewTab(IWebDriver driver, string url = "")
public static void CloseCurrentTab(IWebDriver driver)
public static void CloseTabByHandle(IWebDriver driver, string handle)
public static List<string> GetAllWindowHandles(IWebDriver driver)
public static string GetCurrentWindowHandle(IWebDriver driver)
public static void SwitchToTabByIndex(IWebDriver driver, int index)
```

**Зависимости:** Нет

**Сложность:** Средняя

---

## Приоритет 2: Важные активности

### 2.1 BrowserStorageManage — Управление localStorage/sessionStorage

**Описание:** Чтение, запись, удаление данных из Web Storage

**Enum StorageType:**
```csharp
public enum StorageType
{
    LocalStorage,   // localStorage
    SessionStorage  // sessionStorage
}
```

**Enum StorageOperation:**
```csharp
public enum StorageOperation
{
    GetItem,      // Получить значение по ключу
    SetItem,      // Установить значение
    RemoveItem,   // Удалить ключ
    Clear,        // Очистить всё хранилище
    GetAllKeys,   // Получить все ключи
    GetLength     // Получить количество элементов
}
```

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_StorageType` — тип хранилища (localStorage/sessionStorage)
- `Prop_Operation` — операция
- `Prop_Key` — ключ
- `Prop_Value` — значение (для SetItem)
- `Prop_OutValue` — выходная переменная: значение
- `Prop_OutKeys` — выходная переменная: List<string> ключей
- `Prop_OutLength` — выходная переменная: количество элементов

**Требуемые методы в SeleniumHelper:**
```csharp
public static string GetStorageItem(IWebDriver driver, StorageType type, string key)
public static void SetStorageItem(IWebDriver driver, StorageType type, string key, string value)
public static void RemoveStorageItem(IWebDriver driver, StorageType type, string key)
public static void ClearStorage(IWebDriver driver, StorageType type)
public static List<string> GetStorageKeys(IWebDriver driver, StorageType type)
public static int GetStorageLength(IWebDriver driver, StorageType type)
```

**Зависимости:** IJavaScriptExecutor

**Сложность:** Средняя

---

### 2.2 ElementClickAndHold — Клик с удержанием

**Описание:** Нажимает и удерживает кнопку мыши на элементе

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_ElementId` / `Prop_Locator` — элемент
- `Prop_HoldDuration` — длительность удержания (мс)
- `Prop_WaitTimeout` — таймаут ожидания

**Требуемые методы в SeleniumHelper:**
```csharp
public static void ClickAndHold(IWebDriver driver, IWebElement element, int durationMs)
public static void ReleaseClick(IWebDriver driver)
```

**Зависимости:** OpenQA.Selenium.Interactions.Actions

**Сложность:** Низкая

---

### 2.3 ElementHoverWithOffset — Наведение с точным смещением

**Описание:** Наводит курсор на элемент с указанным смещением от центра

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_ElementId` / `Prop_Locator` — элемент
- `Prop_OffsetX` — смещение по X от центра элемента
- `Prop_OffsetY` — смещение по Y от центра элемента
- `Prop_WaitTimeout` — таймаут ожидания

**Требуемые методы в SeleniumHelper:**
```csharp
public static void HoverWithOffset(IWebDriver driver, IWebElement element, int offsetX, int offsetY)
```

**Зависимости:** OpenQA.Selenium.Interactions.Actions

**Сложность:** Низкая

---

### 2.4 ElementGetScreenshot — Скриншот элемента

**Описание:** Создаёт скриншот конкретного элемента (не всей страницы)

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_ElementId` / `Prop_Locator` — элемент
- `Prop_FilePath` — путь для сохранения (опционально)
- `Prop_WaitTimeout` — таймаут ожидания
- `Prop_OutBase64` — выходная переменная: скриншот в Base64

**Требуемые методы в SeleniumHelper:**
```csharp
public static string TakeElementScreenshotBase64(IWebElement element)
public static void TakeElementScreenshotToFile(IWebElement element, string filePath)
```

**Зависимости:** ITakesScreenshot

**Сложность:** Низкая

---

### 2.5 BrowserGetLogs — Получение логов браузера

**Описание:** Получает логи консоли, сети, браузера

**Enum LogType:**
```csharp
public enum BrowserLogType
{
    Browser,    // Логи браузера
    Driver,     // Логи драйвера
    Client,     // Логи клиента
    Server,     // Логи сервера
    Performance // Логи производительности
}
```

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_LogType` — тип логов
- `Prop_OutLogs` — выходная переменная: List<string> логов
- `Prop_OutLogCount` — выходная переменная: количество записей

**Требуемые методы в SeleniumHelper:**
```csharp
public static List<string> GetBrowserLogs(IWebDriver driver, BrowserLogType logType)
```

**Зависимости:** ILogs

**Сложность:** Средняя

---

## Приоритет 3: Дополнительные активности

### 3.1 ElementSelectMultiple — Работа с multiple select

**Описание:** Выбор/снятие выбора нескольких опций в select[multiple]

**Enum MultiSelectOperation:**
```csharp
public enum MultiSelectOperation
{
    SelectByText,      // Выбрать по тексту
    SelectByValue,     // Выбрать по value
    SelectByIndex,     // Выбрать по индексу
    DeselectByText,    // Снять выбор по тексту
    DeselectByValue,   // Снять выбор по value
    DeselectByIndex,   // Снять выбор по индексу
    DeselectAll,       // Снять все выборы
    GetAllOptions,     // Получить все опции
    GetSelectedOptions // Получить выбранные опции
}
```

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_ElementId` / `Prop_Locator` — select элемент
- `Prop_Operation` — операция
- `Prop_Value` — значение для выбора
- `Prop_WaitTimeout` — таймаут ожидания
- `Prop_OutOptions` — выходная переменная: List<string> опций
- `Prop_OutSelectedOptions` — выходная переменная: List<string> выбранных

**Требуемые методы в SeleniumHelper:**
```csharp
public static void SelectMultipleByText(IWebElement selectElement, string text)
public static void DeselectByText(IWebElement selectElement, string text)
public static void DeselectAll(IWebElement selectElement)
public static List<string> GetAllOptions(IWebElement selectElement)
public static List<string> GetSelectedOptions(IWebElement selectElement)
```

**Зависимости:** OpenQA.Selenium.Support.UI.SelectElement

**Сложность:** Средняя

---

### 3.2 BrowserWaitForDownload — Ожидание завершения загрузки файла

**Описание:** Ожидает завершения загрузки файла в указанную директорию

**Свойства:**
- `Prop_DownloadDirectory` — директория загрузок
- `Prop_FilePattern` — маска файла (wildcard или regex)
- `Prop_Timeout` — таймаут ожидания (сек)
- `Prop_CheckInterval` — интервал проверки (мс)
- `Prop_OutFilePath` — выходная переменная: путь к загруженному файлу
- `Prop_OutFileSize` — выходная переменная: размер файла

**Требуемые методы в SeleniumHelper:**
```csharp
public static string WaitForFileDownload(string directory, string pattern, int timeoutSec, int checkIntervalMs)
public static bool IsFileDownloadComplete(string filePath)
```

**Зависимости:** System.IO, FileSystemWatcher

**Сложность:** Высокая

---

### 3.3 ElementGetComputedStyle — Получить вычисленные CSS стили

**Описание:** Получает вычисленные CSS свойства элемента

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_ElementId` / `Prop_Locator` — элемент
- `Prop_CssProperty` — имя CSS свойства (color, font-size и т.д.)
- `Prop_WaitTimeout` — таймаут ожидания
- `Prop_OutValue` — выходная переменная: значение свойства

**Требуемые методы в SeleniumHelper:**
```csharp
public static string GetComputedStyle(IWebDriver driver, IWebElement element, string propertyName)
```

**Зависимости:** IJavaScriptExecutor

**Сложность:** Низкая

---

### 3.4 BrowserSetDownloadDirectory — Настройка директории загрузок

**Описание:** Устанавливает директорию для загрузки файлов (требует настройки при открытии браузера)

**Примечание:** Эта функция должна быть интегрирована в BrowserOpen, так как настройки загрузки устанавливаются при создании драйвера.

**Дополнение к BrowserOpenBack:**
- `Prop_DownloadDirectory` — директория для загрузок
- `Prop_AutoDownload` — автоматически загружать файлы без диалога

**Сложность:** Средняя (требует модификации BrowserOpenBack)

---

### 3.5 ElementWaitForAttribute — Ожидание изменения атрибута

**Описание:** Ожидает пока атрибут элемента примет определённое значение

**Свойства:**
- `Prop_SessionId` — ID сессии браузера
- `Prop_ElementId` / `Prop_Locator` — элемент
- `Prop_AttributeName` — имя атрибута
- `Prop_ExpectedValue` — ожидаемое значение
- `Prop_Timeout` — таймаут ожидания (сек)
- `Prop_OutActualValue` — выходная переменная: фактическое значение

**Требуемые методы в SeleniumHelper:**
```csharp
public static bool WaitForAttributeValue(IWebDriver driver, By locator, string attributeName, string expectedValue, int timeoutSec)
```

**Зависимости:** WebDriverWait

**Сложность:** Средняя

---

## Порядок реализации (рекомендуемый)

### Этап 1: Базовые Actions (1-2 дня)
1. ElementDoubleClick
2. ElementRightClick
3. ElementClickAndHold

**Причина:** Простые, используют один и тот же механизм Actions API

---

### Этап 2: Drag and Drop (1 день)
4. ElementDragDrop

**Причина:** Более сложная, но очень востребованная функция

---

### Этап 3: Управление окнами и вкладками (2 дня)
5. BrowserWindowManage
6. BrowserTabManage

**Причина:** Критичные для многооконных сценариев

---

### Этап 4: Работа с файлами (1-2 дня)
7. ElementUploadFile
8. ElementGetRect
9. ElementGetScreenshot

**Причина:** Часто используемые функции

---

### Этап 5: Web Storage (1 день)
10. BrowserStorageManage

**Причина:** Важно для работы с SPA приложениями

---

### Этап 6: Расширенные функции (2-3 дня)
11. ElementHoverWithOffset
12. ElementSelectMultiple
13. ElementGetComputedStyle
14. ElementWaitForAttribute

**Причина:** Специфичные сценарии

---

### Этап 7: Логи и загрузки (2-3 дня)
15. BrowserGetLogs
16. BrowserWaitForDownload
17. BrowserSetDownloadDirectory (модификация BrowserOpen)

**Причина:** Сложные, требуют дополнительной настройки

---

## Необходимые изменения в существующих файлах

### Enums.cs
Добавить новые enum:
- `WindowOperation`
- `TabOperation`
- `StorageType`
- `StorageOperation`
- `MultiSelectOperation`
- `BrowserLogType`

### ActivityStrings.cs
Добавить константы для новых активностей и полей

### SeleniumHelper.cs
Добавить все методы, перечисленные выше

### Common/ActivityIcons.cs
Проверить наличие иконок для новых активностей

---

## Оценка трудозатрат

| Приоритет | Активностей | Дней разработки | Дней тестирования |
|-----------|-------------|-----------------|-------------------|
| 1         | 7           | 8-10            | 3-4               |
| 2         | 5           | 6-8             | 2-3               |
| 3         | 5           | 6-8             | 2-3               |
| **Итого** | **17**      | **20-26**       | **7-10**          |

**Общая оценка:** 27-36 рабочих дней (5-7 недель)

---

## Риски и сложности

### Высокий риск:
- **BrowserWaitForDownload** — зависит от файловой системы, может быть нестабильным
- **BrowserGetLogs** — не все браузеры поддерживают все типы логов
- **ElementDragDrop** — может не работать с некоторыми JS фреймворками

### Средний риск:
- **BrowserStorageManage** — требует выполнения JavaScript
- **BrowserSetDownloadDirectory** — требует модификации существующего кода

### Низкий риск:
- Все остальные активности используют стандартный Selenium API

---

## Зависимости между активностями

```
BrowserOpen (существует)
    ↓
BrowserSetDownloadDirectory (модификация)
    ↓
BrowserWaitForDownload

ElementFind (существует)
    ↓
ElementDoubleClick, ElementRightClick, ElementDragDrop, ElementUploadFile, ElementGetRect
```

---

## Рекомендации по тестированию

### Для каждой активности создать тесты:
1. **Позитивный сценарий** — стандартное использование
2. **Негативный сценарий** — некорректные параметры
3. **Граничные случаи** — таймауты, отсутствие элементов
4. **Кросс-браузерность** — Chrome, Firefox, Edge

### Тестовые страницы:
Создать HTML страницы с примерами для каждой активности:
- `test_dragdrop.html`
- `test_upload.html`
- `test_storage.html`
- и т.д.

---

## Следующие шаги

1. ✅ Создан план реализации
2. ⏳ Утверждение приоритетов с командой
3. ⏳ Создание тестовых HTML страниц
4. ⏳ Реализация Этапа 1 (базовые Actions)
5. ⏳ Код-ревью и тестирование
6. ⏳ Переход к следующим этапам

---

**Дата создания плана:** 2025-02-15  
**Версия:** 1.0  
**Автор:** AI Assistant
