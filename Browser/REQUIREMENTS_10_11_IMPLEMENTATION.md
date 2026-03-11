# Requirements 10-11: Окна/Вкладки и Cookies/Storage - Реализация

## Статус: Завершено ✓

Реализованы Requirements 10 и 11 согласно requirements.md.

---

## Requirement 10: Поддержка множественных окон и вкладок

### Реализованные методы в BrowserActivityBase

#### 1. GetAllWindows()
**Описание:** Получает информацию о всех открытых окнах/вкладках

**Возвращает:** `List<WindowInfo>` с информацией о каждом окне:
- Handle - уникальный идентификатор
- Title - заголовок страницы
- Url - текущий URL
- OpenedAt - время открытия (примерное)
- IsActive - является ли окно активным

**Пример:**
```csharp
var windows = GetAllWindows(driver);
foreach (var window in windows)
{
    LogInfo($"Окно: {window.Title} ({window.Url})");
}
```

**Acceptance Criteria:** ✓ 10.1, 10.6

---

#### 2. SwitchToWindowByTitle()
**Описание:** Переключается на окно по заголовку (частичное совпадение)

**Параметры:**
- `driver` - WebDriver
- `title` - заголовок окна (частичное совпадение)

**Возвращает:** `bool` - true если окно найдено и переключение выполнено

**Пример:**
```csharp
if (SwitchToWindowByTitle(driver, "Google"))
{
    LogInfo("Переключено на окно Google");
}
```

**Acceptance Criteria:** ✓ 10.2, 10.5

---

#### 3. SwitchToWindowByUrl()
**Описание:** Переключается на окно по URL (частичное совпадение)

**Параметры:**
- `driver` - WebDriver
- `url` - URL страницы (частичное совпадение)

**Возвращает:** `bool` - true если окно найдено и переключение выполнено

**Пример:**
```csharp
if (SwitchToWindowByUrl(driver, "example.com"))
{
    LogInfo("Переключено на окно example.com");
}
```

**Acceptance Criteria:** ✓ 10.3, 10.5

---

#### 4. CloseAllExceptMain()
**Описание:** Закрывает все окна кроме основного

**Параметры:**
- `driver` - WebDriver
- `mainWindowHandle` - handle основного окна (если null, используется первое)

**Возвращает:** `int` - количество закрытых окон

**Пример:**
```csharp
int closed = CloseAllExceptMain(driver);
LogInfo($"Закрыто {closed} окон");
```

**Acceptance Criteria:** ✓ 10.4, 10.5, 10.7

---

### Существующие активности

Уже реализованы активности для управления вкладками:

**BrowserTabManageBack** - операции:
- OpenNewTab - открыть новую вкладку
- CloseCurrentTab - закрыть текущую вкладку
- CloseTabByHandle - закрыть по handle
- GetAllHandles - получить список handles
- GetCurrentHandle - получить текущий handle
- SwitchToTab - переключиться по индексу

**BrowserWindowManageBack** - операции:
- Maximize - развернуть окно
- Minimize - свернуть окно
- FullScreen - полноэкранный режим
- SetSize - установить размер
- SetPosition - установить позицию
- GetSize - получить размер
- GetPosition - получить позицию

---

## Requirement 11: Расширенная работа с Cookies и Storage

### Реализованные методы в BrowserActivityBase

#### 1. ExportCookiesToJson()
**Описание:** Экспортирует все cookies в JSON формат

**Возвращает:** `string` - JSON с cookies и метаданными

**Формат JSON:**
```json
{
  "Cookies": [
    {
      "Name": "session_id",
      "Value": "abc123",
      "Domain": ".example.com",
      "Path": "/",
      "Expiry": "2024-12-31T23:59:59",
      "Secure": true,
      "HttpOnly": true,
      "SameSite": "Lax"
    }
  ],
  "ExportedAt": "2024-01-15T10:30:00",
  "SourceUrl": "https://example.com"
}
```

**Пример:**
```csharp
string json = ExportCookiesToJson(driver);
File.WriteAllText("cookies.json", json);
```

**Acceptance Criteria:** ✓ 11.1

---

#### 2. ImportCookiesFromJson()
**Описание:** Импортирует cookies из JSON формата

**Параметры:**
- `driver` - WebDriver
- `json` - JSON строка с cookies

**Возвращает:** `int` - количество импортированных cookies

**Пример:**
```csharp
string json = File.ReadAllText("cookies.json");
int imported = ImportCookiesFromJson(driver, json);
LogInfo($"Импортировано {imported} cookies");
```

**Acceptance Criteria:** ✓ 11.2, 11.8

---

#### 3. FilterCookiesByDomain()
**Описание:** Фильтрует cookies по домену

**Параметры:**
- `driver` - WebDriver
- `domain` - домен для фильтрации

**Возвращает:** `List<Cookie>` - список cookies для домена

**Пример:**
```csharp
var cookies = FilterCookiesByDomain(driver, "example.com");
foreach (var cookie in cookies)
{
    LogInfo($"Cookie: {cookie.Name} = {cookie.Value}");
}
```

**Acceptance Criteria:** ✓ 11.3

---

#### 4. FilterCookiesByName()
**Описание:** Фильтрует cookies по имени (частичное совпадение)

**Параметры:**
- `driver` - WebDriver
- `namePattern` - паттерн имени

**Возвращает:** `List<Cookie>` - список cookies с совпадающим именем

**Пример:**
```csharp
var cookies = FilterCookiesByName(driver, "session");
// Найдет: session_id, user_session, session_token
```

**Acceptance Criteria:** ✓ 11.4

---

#### 5. ManageLocalStorage()
**Описание:** Работает с localStorage

**Параметры:**
- `driver` - WebDriver
- `operation` - операция: "get", "set", "remove", "clear"
- `key` - ключ (для get/set/remove)
- `value` - значение (для set)

**Возвращает:** `string` - значение (для get) или null

**Примеры:**
```csharp
// Установить значение
ManageLocalStorage(driver, "set", "username", "john");

// Получить значение
string username = ManageLocalStorage(driver, "get", "username");

// Удалить ключ
ManageLocalStorage(driver, "remove", "username");

// Очистить всё
ManageLocalStorage(driver, "clear");
```

**Acceptance Criteria:** ✓ 11.5

---

#### 6. ManageSessionStorage()
**Описание:** Работает с sessionStorage

**Параметры:** Аналогично ManageLocalStorage()

**Примеры:**
```csharp
// Установить значение
ManageSessionStorage(driver, "set", "temp_data", "value");

// Получить значение
string data = ManageSessionStorage(driver, "get", "temp_data");

// Очистить всё
ManageSessionStorage(driver, "clear");
```

**Acceptance Criteria:** ✓ 11.6

---

#### 7. ClearAllCookiesAndStorage()
**Описание:** Очищает все cookies и storage одной операцией

**Параметры:**
- `driver` - WebDriver

**Пример:**
```csharp
ClearAllCookiesAndStorage(driver);
LogInfo("Все cookies и storage очищены");
```

**Acceptance Criteria:** ✓ 11.7

---

### Существующая активность

**BrowserManageCookiesBack** - операции:
- Get - получить cookie по имени
- GetAll - получить все cookies (JSON)
- Set - установить cookie
- Delete - удалить cookie
- DeleteAll - удалить все cookies

---

## Модели данных

### WindowInfo
```csharp
public class WindowInfo
{
    public string Handle { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public DateTime OpenedAt { get; set; }
    public bool IsActive { get; set; }
}
```

### CookieData
```csharp
public class CookieData
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Domain { get; set; }
    public string Path { get; set; }
    public DateTime? Expiry { get; set; }
    public bool Secure { get; set; }
    public bool HttpOnly { get; set; }
    public string SameSite { get; set; }
}
```

### CookieCollection
```csharp
public class CookieCollection
{
    public List<CookieData> Cookies { get; set; }
    public DateTime ExportedAt { get; set; }
    public string SourceUrl { get; set; }
}
```

---

## Использование в активностях

Все методы доступны в активностях, наследующих `BrowserActivityBase`:

```csharp
public class MyCustomActivity : BrowserActivityBase<MyView>
{
    protected override ExecutionResult ExecuteActivity(ScriptingData sd)
    {
        string sessionId = GetPropertyValue(nameof(View.SessionID), sd);
        IWebDriver driver = GetDriverFromContext(sessionId);
        
        // Работа с окнами
        var windows = GetAllWindows(driver);
        SwitchToWindowByTitle(driver, "Google");
        CloseAllExceptMain(driver);
        
        // Работа с cookies
        string json = ExportCookiesToJson(driver);
        ImportCookiesFromJson(driver, json);
        var cookies = FilterCookiesByDomain(driver, "example.com");
        
        // Работа с storage
        ManageLocalStorage(driver, "set", "key", "value");
        string value = ManageLocalStorage(driver, "get", "key");
        ClearAllCookiesAndStorage(driver);
        
        return CreateSuccessResult();
    }
}
```

---

## Соответствие требованиям

| Requirement | Метод | Статус |
|------------|-------|--------|
| 10.1 - Отслеживание окон | GetAllWindows() | ✓ |
| 10.2 - Переключение по заголовку | SwitchToWindowByTitle() | ✓ |
| 10.3 - Переключение по URL | SwitchToWindowByUrl() | ✓ |
| 10.4 - Закрытие всех кроме основного | CloseAllExceptMain() | ✓ |
| 10.5 - Сохранение SessionID | Все методы | ✓ |
| 10.6 - Автодобавление окон | GetAllWindows() | ✓ |
| 10.7 - Автоудаление окон | CloseAllExceptMain() | ✓ |
| 11.1 - Экспорт cookies в JSON | ExportCookiesToJson() | ✓ |
| 11.2 - Импорт cookies из JSON | ImportCookiesFromJson() | ✓ |
| 11.3 - Фильтрация по домену | FilterCookiesByDomain() | ✓ |
| 11.4 - Фильтрация по имени | FilterCookiesByName() | ✓ |
| 11.5 - Работа с localStorage | ManageLocalStorage() | ✓ |
| 11.6 - Работа с sessionStorage | ManageSessionStorage() | ✓ |
| 11.7 - Очистка всего | ClearAllCookiesAndStorage() | ✓ |
| 11.8 - Валидация формата | ImportCookiesFromJson() | ✓ |

---

## Файлы

**Новые файлы:**
- `Browser/Models/WindowInfo.cs`
- `Browser/Models/CookieData.cs`
- `Browser/REQUIREMENTS_10_11_IMPLEMENTATION.md` (этот файл)

**Измененные файлы:**
- `Browser/BrowserActivityBase.cs` - добавлены методы для Requirements 10-11
- `Primo.MIA.csproj` - добавлены ссылки на новые модели

**Существующие активности:**
- `Browser/BrowserTabManageBack.cs` - управление вкладками
- `Browser/BrowserWindowManageBack.cs` - управление окном
- `Browser/BrowserManageCookiesBack.cs` - управление cookies

---

## Тестирование

Все новые методы:
- ✓ Компилируются без ошибок
- ✓ Следуют архитектурным принципам проекта
- ✓ Совместимы с существующим кодом
- ✓ Документированы XML-комментариями
- ✓ Используют существующие helper-методы

---

## Заключение

Requirements 10 и 11 успешно реализованы. Добавлены методы для:
- Управления множественными окнами и вкладками
- Экспорта/импорта cookies в JSON
- Фильтрации cookies
- Работы с localStorage и sessionStorage
- Комплексной очистки cookies и storage

Все методы интегрированы в `BrowserActivityBase` и готовы к использованию в активностях.
