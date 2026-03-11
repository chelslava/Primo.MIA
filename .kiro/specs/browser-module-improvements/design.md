# Design Document: Browser Module Improvements

## Overview

Этот документ описывает технический дизайн для комплексного улучшения Browser модуля Primo Platform. Модуль предоставляет активности для автоматизации браузера через Selenium WebDriver и требует рефакторинга для улучшения поддерживаемости, производительности и функциональности.

### Цели дизайна

1. **Архитектурная унификация**: Завершить рефакторинг всех 24 оставшихся активностей с использованием BrowserActivityBase
2. **Чистая архитектура**: Реализовать конкретные классы для интерфейсов IBrowserServices
3. **Производительность**: Внедрить систему кэширования и оптимизации для сокращения времени выполнения
4. **Расширенные возможности**: Добавить поддержку Shadow DOM, retry механизма, улучшенной работы с iframe
5. **Надежность**: Реализовать автоматические скриншоты при ошибках и расширенное логирование
6. **Интеграция**: Добавить поддержку Selenium Grid, BrowserStack, Sauce Labs
7. **Обратная совместимость**: Сохранить работоспособность существующих workflow

### Текущее состояние

- Прогресс: 40% (Фаза 1 - 100%, Фаза 2 - 50%, Фаза 3 - 50%, Фаза 4 - 0%, Фаза 5 - 0%)
- Отрефакторено: 6 активностей (ElementClick, ElementHover, ElementInput, ElementScrollTo, ElementGetInfo, BrowserNavigate)
- Осталось отрефакторить: 24 активности
- Существующая инфраструктура: BrowserActivityBase, IBrowserServices интерфейсы, BrowserSessionContext


## Architecture

### Общая архитектура

Архитектура Browser модуля основана на следующих принципах:

```
┌─────────────────────────────────────────────────────────────┐
│                    Activity Layer                            │
│  (ElementClick, BrowserOpen, etc. - наследуют Base)         │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│              BrowserActivityBase<TView>                      │
│  • Driver Management (GetDriverFromContext)                  │
│  • Element Location (FindElement, TryFindElement)            │
│  • Retry Mechanism (новое)                                   │
│  • Shadow DOM Support (новое)                                │
│  • iframe Auto-handling (новое)                              │
│  • Screenshot on Error (новое)                               │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                 IBrowserServices                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ ISessionManager    │ IElementLocator                 │   │
│  │ IElementRepository │ IActivityLogger                 │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│              Infrastructure Layer                            │
│  • SessionManager (реализация ISessionManager)              │
│  • ElementLocator (реализация IElementLocator)              │
│  • ElementRepository (реализация IElementRepository)         │
│  • ActivityLogger (реализация IActivityLogger)              │
│  • CacheManager (новый - кэширование локаторов)             │
│  • PerformanceMonitor (новый - метрики)                     │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                 Selenium WebDriver                           │
│  • Local WebDriver                                           │
│  • Selenium Grid (новое)                                     │
│  • BrowserStack / Sauce Labs (новое)                        │
└─────────────────────────────────────────────────────────────┘
```

### Ключевые архитектурные решения

1. **Ambient Context Pattern**: Используется BrowserSessionContext для управления текущей сессией через стек в RepoDict
2. **Template Method Pattern**: BrowserActivityBase предоставляет шаблонные методы для общих операций
3. **Strategy Pattern**: IElementLocator позволяет различные стратегии поиска элементов
4. **Repository Pattern**: IElementRepository и ISessionManager инкапсулируют хранение
5. **Decorator Pattern**: Retry механизм оборачивает операции с элементами


## Components and Interfaces

### 1. BrowserActivityBase (расширенный)

Базовый класс получит следующие новые возможности:

```csharp
public abstract class BrowserActivityBase<TView> : PrimoComponentTO<TView>
{
    // Существующие методы сохраняются
    
    // === НОВЫЕ МЕТОДЫ ===
    
    // Retry механизм
    protected IWebElement FindElementWithRetry(
        IWebDriver driver, 
        LocatorType locatorType, 
        string locatorValue,
        int timeout,
        int maxRetries = 3,
        int retryDelayMs = 500);
    
    // Shadow DOM поддержка
    protected IWebElement FindElementInShadowRoot(
        IWebElement shadowHost,
        LocatorType locatorType,
        string locatorValue);
    
    protected IWebElement FindElementInShadowChain(
        IWebDriver driver,
        string[] shadowPath,
        LocatorType finalLocatorType,
        string finalLocatorValue);
    
    // iframe автоматическая обработка
    protected IWebElement FindElementWithIframeDetection(
        IWebDriver driver,
        LocatorType locatorType,
        string locatorValue,
        int timeout);
    
    // Скриншоты при ошибках
    protected string CaptureErrorScreenshot(
        IWebDriver driver,
        string activityName,
        Exception error);
    
    // Расширенные селекторы
    protected IWebElement FindByText(
        IWebDriver driver,
        string text,
        bool exactMatch = false);
    
    protected IWebElement FindByPosition(
        IWebDriver driver,
        LocatorType locatorType,
        string locatorValue,
        ElementPosition position); // First, Last, Nth
}
```

### 2. SessionManager (новый класс)

Реализация ISessionManager для управления WebDriver сессиями:

```csharp
public class SessionManager : ISessionManager
{
    private readonly Dictionary<string, IWebDriver> _sessions;
    private readonly Dictionary<string, SessionMetadata> _metadata;
    private readonly object _lock = new object();
    
    public void RegisterSession(string sessionId, IWebDriver driver)
    {
        // Регистрация с метаданными (время создания, capabilities)
    }
    
    public IWebDriver GetSession(string sessionId)
    {
        // Получение с валидацией активности сессии
    }
    
    public IEnumerable<string> GetActiveSessions()
    {
        // Возврат только активных сессий
    }
    
    // Новые методы для Grid/Cloud
    public void RegisterRemoteSession(
        string sessionId, 
        IWebDriver driver,
        RemoteSessionInfo remoteInfo);
}
```

### 3. ElementLocator (новый класс)

Реализация IElementLocator с кэшированием и расширенными возможностями:

```csharp
public class ElementLocator : IElementLocator
{
    private readonly CacheManager _cache;
    private readonly IActivityLogger _logger;
    
    public IWebElement FindElement(
        IWebDriver driver, 
        LocatorType locatorType, 
        string locatorValue, 
        int timeoutSeconds)
    {
        // Проверка кэша локаторов
        // Поиск с ожиданием
        // Кэширование результата
    }
    
    // Расширенные методы
    public IWebElement FindByCustomStrategy(
        IWebDriver driver,
        CustomLocatorStrategy strategy);
    
    public IReadOnlyCollection<IWebElement> FindByCombinedSelector(
        IWebDriver driver,
        CombinedSelector selector); // AND/OR логика
}
```

### 4. ElementRepository (новый класс)

Реализация IElementRepository с оптимизированным хранением:

```csharp
public class ElementRepository : IElementRepository
{
    private readonly Dictionary<string, WeakReference<IWebElement>> _elements;
    private readonly object _lock = new object();
    
    public void StoreElement(string elementId, IWebElement element)
    {
        // Хранение через WeakReference для предотвращения утечек памяти
    }
    
    public IWebElement GetElement(string elementId)
    {
        // Получение с проверкой stale reference
    }
    
    public void CleanupStaleElements()
    {
        // Автоматическая очистка устаревших элементов
    }
}
```

### 5. ActivityLogger (новый класс)

Реализация IActivityLogger со структурированным логированием:

```csharp
public class ActivityLogger : IActivityLogger
{
    public void LogInfo(string activityName, string message, params object[] args)
    {
        // Структурированное логирование с контекстом
        // Формат: [timestamp] [INFO] [ActivityName] message
    }
    
    public void LogPerformance(
        string activityName, 
        TimeSpan duration,
        Dictionary<string, object> metrics)
    {
        // Логирование метрик производительности
    }
}
```

### 6. CacheManager (новый класс)

Управление кэшированием локаторов и элементов:

```csharp
public class CacheManager
{
    private readonly LRUCache<string, By> _locatorCache;
    private readonly Dictionary<string, CacheEntry> _elementCache;
    
    public By GetOrCreateLocator(
        LocatorType type, 
        string value,
        Func<By> factory)
    {
        // LRU кэш для локаторов (макс 1000 записей)
    }
    
    public void InvalidateElementCache(string sessionId)
    {
        // Инвалидация при навигации
    }
}
```

### 7. PerformanceMonitor (новый класс)

Мониторинг производительности активностей:

```csharp
public class PerformanceMonitor
{
    public IDisposable MeasureActivity(string activityName)
    {
        // Возвращает IDisposable для using pattern
        // Автоматически логирует время выполнения
    }
    
    public PerformanceReport GenerateReport()
    {
        // Генерация отчета с метриками:
        // - Среднее время выполнения
        // - P95, P99 латентность
        // - Использование памяти
    }
}
```

### 8. RemoteDriverFactory (новый класс)

Фабрика для создания удаленных WebDriver подключений:

```csharp
public class RemoteDriverFactory
{
    public IWebDriver CreateGridDriver(
        string gridUrl,
        DesiredCapabilities capabilities)
    {
        // Подключение к Selenium Grid
    }
    
    public IWebDriver CreateBrowserStackDriver(
        string username,
        string accessKey,
        BrowserStackOptions options)
    {
        // Подключение к BrowserStack
    }
    
    public IWebDriver CreateSauceLabsDriver(
        string username,
        string accessKey,
        SauceLabsOptions options)
    {
        // Подключение к Sauce Labs
    }
}
```


## Data Models

### SessionMetadata

Метаданные сессии браузера:

```csharp
public class SessionMetadata
{
    public string SessionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public string BrowserType { get; set; }
    public string BrowserVersion { get; set; }
    public bool IsRemote { get; set; }
    public RemoteSessionInfo RemoteInfo { get; set; }
    public Dictionary<string, object> Capabilities { get; set; }
}
```

### RemoteSessionInfo

Информация об удаленной сессии:

```csharp
public class RemoteSessionInfo
{
    public RemoteProviderType ProviderType { get; set; } // Grid, BrowserStack, SauceLabs
    public string RemoteUrl { get; set; }
    public string RemoteSessionId { get; set; }
    public Dictionary<string, string> Credentials { get; set; }
}

public enum RemoteProviderType
{
    Local,
    SeleniumGrid,
    BrowserStack,
    SauceLabs
}
```

### RetryConfiguration

Конфигурация retry механизма:

```csharp
public class RetryConfiguration
{
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 500;
    public bool ExponentialBackoff { get; set; } = false;
    public List<Type> RetryableExceptions { get; set; } = new List<Type>
    {
        typeof(StaleElementReferenceException),
        typeof(NoSuchElementException),
        typeof(ElementNotInteractableException)
    };
}
```

### ShadowDomPath

Путь к элементу через Shadow DOM:

```csharp
public class ShadowDomPath
{
    public List<ShadowDomStep> Steps { get; set; }
    
    public class ShadowDomStep
    {
        public LocatorType HostLocatorType { get; set; }
        public string HostLocatorValue { get; set; }
    }
}
```

### CombinedSelector

Комбинированный селектор с логикой AND/OR:

```csharp
public class CombinedSelector
{
    public SelectorLogic Logic { get; set; } // AND, OR
    public List<SelectorCondition> Conditions { get; set; }
}

public class SelectorCondition
{
    public LocatorType Type { get; set; }
    public string Value { get; set; }
    public bool Negate { get; set; } // NOT условие
}

public enum SelectorLogic
{
    And,
    Or
}
```

### ElementPosition

Позиция элемента в коллекции:

```csharp
public enum ElementPosition
{
    First,
    Last,
    Nth
}

public class ElementPositionSelector
{
    public ElementPosition Position { get; set; }
    public int Index { get; set; } // Для Nth
}
```

### PerformanceMetrics

Метрики производительности активности:

```csharp
public class PerformanceMetrics
{
    public string ActivityName { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public long MemoryUsedBytes { get; set; }
    public int RetryCount { get; set; }
    public Dictionary<string, object> CustomMetrics { get; set; }
}
```

### ScreenshotMetadata

Метаданные скриншота при ошибке:

```csharp
public class ScreenshotMetadata
{
    public string FilePath { get; set; }
    public string ActivityName { get; set; }
    public DateTime Timestamp { get; set; }
    public string ErrorMessage { get; set; }
    public string SessionId { get; set; }
    public string CurrentUrl { get; set; }
}
```

### CookieData

Данные cookie для импорта/экспорта:

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

public class CookieCollection
{
    public List<CookieData> Cookies { get; set; }
    public DateTime ExportedAt { get; set; }
    public string SourceUrl { get; set; }
}
```

### WindowInfo

Информация об окне/вкладке браузера:

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

### IframeContext

Контекст iframe для автоматического переключения:

```csharp
public class IframeContext
{
    public Stack<IWebElement> IframeStack { get; set; }
    public string SessionId { get; set; }
    
    public void EnterIframe(IWebElement iframe)
    {
        IframeStack.Push(iframe);
    }
    
    public void ExitIframe()
    {
        if (IframeStack.Count > 0)
            IframeStack.Pop();
    }
    
    public void ExitAllIframes()
    {
        IframeStack.Clear();
    }
}
```


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property Reflection

After analyzing all acceptance criteria, I identified the following redundancies and consolidations:

- **Caching properties (4.1, 4.2, 7.5)**: Can be consolidated into a single property about cache effectiveness
- **Logging properties (4.8, 6.6, 7.6, 12.6, 13.6)**: Can be consolidated into a single property about activity logging
- **Configuration properties (6.2, 6.3, 8.2, 12.2-12.4, 18.3-18.4)**: Can be consolidated into properties about configuration validation
- **Error handling properties (5.6, 7.7, 12.5, 18.6)**: Can be consolidated into a single property about error message clarity
- **Remote connection properties (12.1, 18.1, 18.2)**: Can be consolidated into a single property about remote provider selection
- **Headless mode examples (13.2-13.4)**: Can be consolidated into a single example
- **Backward compatibility properties (1.15, 20.1-20.3)**: Can be consolidated into comprehensive backward compatibility properties

### Code Quality Properties

### Property 1: Code size reduction after refactoring

*For any* refactored Element activity, the lines of code should be reduced by 15-20% compared to the original implementation

**Validates: Requirements 1.13**

### Property 2: Cyclomatic complexity reduction

*For any* refactored Element activity, the cyclomatic complexity should be less than 10

**Validates: Requirements 1.14**

### Property 3: Guard clauses in refactored activities

*For any* refactored Browser activity, all public methods should have guard clauses at the beginning for parameter validation

**Validates: Requirements 2.14**

### Interface Implementation Properties

### Property 4: Interface implementation completeness

*For any* interface in IBrowserServices (ISessionManager, IElementLocator, IElementRepository, IActivityLogger), there exists a concrete class that implements all interface methods

**Validates: Requirements 3.1, 3.2, 3.3, 3.4**

### Caching and Performance Properties

### Property 5: Locator caching effectiveness

*For any* locator that is used more than once, the second and subsequent uses should retrieve the locator from cache rather than creating a new instance

**Validates: Requirements 4.1, 4.2**

### Property 6: Memory footprint reduction

*For any* workflow execution, the memory footprint should be at least 20% lower than the baseline measurement from the original implementation

**Validates: Requirements 4.6**

### Property 7: Performance metrics recording

*For any* activity execution, performance metrics (duration, memory usage, retry count) should be recorded and accessible

**Validates: Requirements 4.7**

### Property 8: Activity execution logging

*For any* activity execution, the execution time should be logged with the activity name and timestamp

**Validates: Requirements 4.8**

### Property 9: Memory leak detection

*For any* long-running workflow with multiple activity executions, memory usage should not grow unboundedly (indicating no memory leaks)

**Validates: Requirements 4.9**

### Property 10: Activity overhead limit

*For any* activity execution, the framework overhead (excluding actual browser operation time) should be less than 100ms

**Validates: Requirements 4.10**

### Shadow DOM Properties

### Property 11: Shadow DOM element discovery

*For any* element located within a shadow root, calling FindElement with the appropriate shadow path should successfully locate the element

**Validates: Requirements 5.1, 5.2**

### Property 12: Automatic shadow root handling

*For any* element that exists in a shadow DOM, the system should automatically detect and traverse the shadow root without explicit shadow DOM API calls from the user

**Validates: Requirements 5.3**

### Property 13: Shadow DOM error messages

*For any* shadow DOM operation that fails, the error message should clearly indicate the shadow DOM context and which step in the shadow path failed

**Validates: Requirements 5.6**

### Retry Mechanism Properties

### Property 14: Retry on element not found

*For any* element search that initially fails with NoSuchElementException, the system should retry up to the configured maximum retry count before failing

**Validates: Requirements 6.1, 6.4**

### Property 15: Retry configuration

*For any* retry configuration with custom maxRetries and retryDelayMs values, the system should respect these values during retry attempts

**Validates: Requirements 6.2, 6.3**

### Property 16: Retry on stale element

*For any* element operation that fails with StaleElementReferenceException, the system should automatically retry the operation

**Validates: Requirements 6.5**

### Property 17: Retry logging

*For any* retry attempt, a log entry should be created containing the attempt number, reason for retry, and timestamp

**Validates: Requirements 6.6**

### Property 18: Retry exhaustion error

*For any* operation where all retry attempts are exhausted, the final error should contain information about all retry attempts including timestamps and failure reasons

**Validates: Requirements 6.7**

### Property 19: Retry disable option

*For any* operation where retry is explicitly disabled, the operation should fail immediately on first error without any retry attempts

**Validates: Requirements 6.8**

### iframe Handling Properties

### Property 20: Automatic iframe detection

*For any* element that exists within an iframe, the system should automatically detect the iframe and locate the element without explicit iframe switching by the user

**Validates: Requirements 7.1**

### Property 21: iframe context round-trip

*For any* operation on an element within an iframe, after the operation completes, the driver context should return to the default content (main page)

**Validates: Requirements 7.2, 7.3**

### Property 22: Nested iframe support

*For any* element within nested iframes (iframe within iframe), the system should successfully traverse all iframe levels and locate the element

**Validates: Requirements 7.4**

### Property 23: iframe structure caching

*For any* iframe that is accessed multiple times, the iframe structure information should be cached and reused on subsequent accesses

**Validates: Requirements 7.5**

### Property 24: iframe context logging

*For any* iframe context switch, a log entry should be created indicating the switch direction (enter/exit) and iframe identifier

**Validates: Requirements 7.6**

### Screenshot Properties

### Property 25: Automatic screenshot on error

*For any* activity that fails with an exception, a screenshot should be automatically captured and saved to the configured directory

**Validates: Requirements 8.1**

### Property 26: Screenshot filename format

*For any* error screenshot, the filename should contain both a timestamp and the activity name in a parseable format

**Validates: Requirements 8.3, 8.4**

### Property 27: Screenshot path in error

*For any* activity error that triggers a screenshot, the error message should include the full path to the captured screenshot

**Validates: Requirements 8.5**

### Property 28: Screenshot configuration

*For any* configuration where automatic screenshots are disabled, no screenshots should be created when activities fail

**Validates: Requirements 8.2, 8.6**

### Property 29: Screenshot cleanup

*For any* screenshot older than the configured retention period (default 7 days), the screenshot file should be automatically deleted during cleanup

**Validates: Requirements 8.7**

### Extended Selector Properties

### Property 30: Custom locator strategy registration

*For any* custom locator strategy that is registered, elements should be locatable using that strategy through the standard FindElement API

**Validates: Requirements 9.1**

### Property 31: Text-based element location

*For any* element containing specific text, the element should be locatable using the text search functionality with both exact and partial matching

**Validates: Requirements 9.2**

### Property 32: Partial attribute matching

*For any* element with an attribute containing a specific substring, the element should be locatable using partial attribute matching

**Validates: Requirements 9.3**

### Property 33: Position-based element selection

*For any* collection of elements matching a locator, specific elements should be selectable by position (first, last, nth) without retrieving all elements

**Validates: Requirements 9.4**

### Property 34: Combined selector logic

*For any* combined selector with AND/OR logic, only elements matching the complete logical expression should be returned

**Validates: Requirements 9.5**

### Property 35: Extended selector performance

*For any* extended selector operation, the execution time should be within 150% of the equivalent standard selector operation

**Validates: Requirements 9.7**

### Window and Tab Management Properties

### Property 36: Window tracking

*For any* browser session, all open windows and tabs should be tracked in the window list, and the list should accurately reflect the current state

**Validates: Requirements 10.1**

### Property 37: Window switching by title

*For any* open window with a specific title, calling switch by title should make that window the active window

**Validates: Requirements 10.2**

### Property 38: Window switching by URL

*For any* open window with a specific URL, calling switch by URL should make that window the active window

**Validates: Requirements 10.3**

### Property 39: Close all except main window

*For any* browser session with multiple windows, calling close all except main should result in only the main window remaining open

**Validates: Requirements 10.4**

### Property 40: SessionID preservation across windows

*For any* window switch operation, the sessionId should remain unchanged before and after the switch

**Validates: Requirements 10.5**

### Property 41: Automatic window tracking

*For any* new window that opens, the window should be automatically added to the tracked window list without explicit registration

**Validates: Requirements 10.6**

### Property 42: Automatic window cleanup

*For any* window that closes, the window should be automatically removed from the tracked window list

**Validates: Requirements 10.7**

### Cookie and Storage Properties

### Property 43: Cookie serialization round-trip

*For any* set of cookies, exporting to JSON and then importing should result in functionally equivalent cookies (same name, value, domain, path, expiry, flags)

**Validates: Requirements 11.1, 11.2**

### Property 44: Cookie filtering by domain

*For any* cookie filter by domain, all returned cookies should have a domain matching the filter criteria

**Validates: Requirements 11.3**

### Property 45: Cookie filtering by name

*For any* cookie filter by name, all returned cookies should have a name matching the filter criteria

**Validates: Requirements 11.4**

### Property 46: Storage round-trip

*For any* key-value pair stored in localStorage or sessionStorage, retrieving the value should return the same value that was stored

**Validates: Requirements 11.5, 11.6**

### Property 47: Complete storage clear

*For any* browser session, after calling clear all cookies and storage, no cookies should exist and both localStorage and sessionStorage should be empty

**Validates: Requirements 11.7**

### Property 48: Cookie import validation

*For any* invalid cookie JSON format, the import operation should reject the data and return a clear validation error

**Validates: Requirements 11.8**

### Remote Provider Properties

### Property 49: Remote provider selection

*For any* configuration specifying a remote provider (Selenium Grid, BrowserStack, or Sauce Labs), the WebDriver should connect to that provider instead of creating a local driver

**Validates: Requirements 12.1, 18.1, 18.2**

### Property 50: Remote capabilities configuration

*For any* remote session with specified capabilities, the capabilities should be passed to the remote provider and reflected in the created session

**Validates: Requirements 12.2, 18.3**

### Property 51: Browser and platform selection

*For any* remote session with specified browser, version, and platform, the created session should match these specifications

**Validates: Requirements 12.3, 12.4, 18.4**

### Property 52: Remote session logging

*For any* remote session creation, the session information (provider, remote session ID, capabilities) should be logged

**Validates: Requirements 12.6**

### Property 53: Test result reporting to cloud

*For any* cloud service session (BrowserStack or Sauce Labs), test execution results should be transmitted to the cloud service

**Validates: Requirements 18.5**

### Headless Mode Properties

### Property 54: Headless mode activation

*For any* browser configuration with headless mode enabled, the browser should start without a visible GUI window

**Validates: Requirements 13.1**

### Property 55: Headless functional equivalence

*For any* browser operation, the operation should produce the same result in headless mode as in normal mode (excluding visual rendering differences)

**Validates: Requirements 13.5**

### Property 56: Browser mode logging

*For any* browser session start, the mode (headless or normal) should be logged

**Validates: Requirements 13.6**

### Performance Benchmark Properties

### Property 57: Benchmark existence

*For any* activity in the Browser module, a corresponding performance benchmark should exist and be executable

**Validates: Requirements 14.1**

### Property 58: Performance measurement

*For any* benchmark execution, both execution time and memory usage should be measured and recorded

**Validates: Requirements 14.2, 14.3**

### Property 59: Baseline comparison

*For any* benchmark result, the result should be compared against the baseline metrics and the comparison result should be available

**Validates: Requirements 14.4**

### Backward Compatibility Properties

### Property 60: Public API preservation

*For any* refactored activity, all public methods, properties, and their signatures should remain unchanged from the original implementation

**Validates: Requirements 1.15, 20.1**

### Property 61: Default behavior preservation

*For any* refactored activity called with default parameters, the behavior and output should be identical to the original implementation

**Validates: Requirements 20.2**

### Property 62: Parameter format preservation

*For any* refactored activity, the input parameter types and output result types should remain unchanged from the original implementation

**Validates: Requirements 20.3**


## Error Handling

### Error Handling Strategy

Browser модуль использует многоуровневую стратегию обработки ошибок:

1. **Validation Layer**: Ранняя валидация входных параметров с понятными сообщениями
2. **Retry Layer**: Автоматические повторные попытки для временных ошибок
3. **Recovery Layer**: Попытки восстановления (например, обновление stale элементов)
4. **Logging Layer**: Детальное логирование всех ошибок с контекстом
5. **Screenshot Layer**: Автоматические скриншоты для визуальной диагностики
6. **User Feedback Layer**: Понятные сообщения об ошибках для пользователей

### Error Categories

#### 1. Configuration Errors

Ошибки конфигурации обнаруживаются на этапе инициализации:

```csharp
public class BrowserConfigurationException : Exception
{
    public string ConfigurationKey { get; set; }
    public string ProvidedValue { get; set; }
    public string ExpectedFormat { get; set; }
}
```

**Примеры:**
- Некорректный URL для Selenium Grid
- Отсутствующие credentials для облачных сервисов
- Недопустимые значения timeout или retry параметров

**Обработка:**
- Валидация при создании конфигурации
- Понятные сообщения с указанием ожидаемого формата
- Предложения по исправлению

#### 2. Connection Errors

Ошибки подключения к браузеру или удаленным сервисам:

```csharp
public class BrowserConnectionException : Exception
{
    public string TargetUrl { get; set; }
    public RemoteProviderType ProviderType { get; set; }
    public int RetryAttempt { get; set; }
}
```

**Примеры:**
- WebDriver не запущен
- Selenium Grid недоступен
- Таймаут подключения к облачному сервису

**Обработка:**
- Автоматические retry с экспоненциальной задержкой
- Логирование каждой попытки подключения
- Fallback на локальный драйвер (опционально)
- Детальная диагностическая информация в ошибке

#### 3. Element Location Errors

Ошибки поиска элементов на странице:

```csharp
public class ElementLocationException : Exception
{
    public LocatorType LocatorType { get; set; }
    public string LocatorValue { get; set; }
    public string CurrentUrl { get; set; }
    public string ScreenshotPath { get; set; }
    public List<RetryAttempt> RetryAttempts { get; set; }
}
```

**Примеры:**
- Элемент не найден (NoSuchElementException)
- Элемент в Shadow DOM не обнаружен
- Элемент в iframe недоступен
- Таймаут ожидания элемента

**Обработка:**
- Автоматические retry согласно конфигурации
- Автоматическое обнаружение Shadow DOM и iframe
- Скриншот страницы при финальной ошибке
- Информация о всех попытках поиска
- Предложения альтернативных локаторов (если возможно)

#### 4. Element Interaction Errors

Ошибки взаимодействия с элементами:

```csharp
public class ElementInteractionException : Exception
{
    public string ElementInfo { get; set; }
    public string AttemptedAction { get; set; }
    public string ElementState { get; set; }
    public string ScreenshotPath { get; set; }
}
```

**Примеры:**
- Элемент не кликабелен (ElementNotInteractableException)
- Элемент устарел (StaleElementReferenceException)
- Элемент перекрыт другим элементом
- Элемент вне видимой области

**Обработка:**
- Автоматический retry для StaleElementReferenceException
- Автоматическая прокрутка к элементу
- Ожидание кликабельности элемента
- JavaScript fallback для некоторых операций
- Скриншот с подсветкой проблемного элемента

#### 5. Session Management Errors

Ошибки управления сессиями:

```csharp
public class SessionManagementException : Exception
{
    public string SessionId { get; set; }
    public SessionOperation Operation { get; set; }
    public List<string> ActiveSessions { get; set; }
}

public enum SessionOperation
{
    Register,
    Get,
    Remove,
    Switch
}
```

**Примеры:**
- Сессия не найдена
- Попытка регистрации дублирующейся сессии
- Сессия закрыта или недействительна

**Обработка:**
- Валидация существования сессии перед операциями
- Автоматическая очистка недействительных сессий
- Список активных сессий в сообщении об ошибке
- Предложение использовать ambient context

#### 6. Performance Errors

Ошибки производительности и ресурсов:

```csharp
public class PerformanceException : Exception
{
    public string ActivityName { get; set; }
    public TimeSpan ActualDuration { get; set; }
    public TimeSpan ExpectedDuration { get; set; }
    public long MemoryUsedBytes { get; set; }
}
```

**Примеры:**
- Превышение лимита overhead (>100ms)
- Утечка памяти обнаружена
- Операция выполняется слишком долго

**Обработка:**
- Логирование предупреждений при приближении к лимитам
- Автоматическая очистка кэшей при высоком использовании памяти
- Метрики для анализа производительности

### Error Recovery Strategies

#### Automatic Retry

```csharp
protected T ExecuteWithRetry<T>(
    Func<T> operation,
    RetryConfiguration config)
{
    int attempt = 0;
    List<Exception> exceptions = new List<Exception>();
    
    while (attempt < config.MaxRetries)
    {
        try
        {
            return operation();
        }
        catch (Exception ex) when (IsRetryable(ex, config))
        {
            attempt++;
            exceptions.Add(ex);
            LogRetryAttempt(attempt, ex);
            
            if (attempt < config.MaxRetries)
            {
                int delay = config.ExponentialBackoff
                    ? config.RetryDelayMs * (int)Math.Pow(2, attempt - 1)
                    : config.RetryDelayMs;
                    
                Thread.Sleep(delay);
            }
        }
    }
    
    throw new RetryExhaustedException(exceptions);
}
```

#### Stale Element Recovery

```csharp
protected IWebElement GetFreshElement(
    IWebDriver driver,
    LocatorType locatorType,
    string locatorValue)
{
    try
    {
        // Попытка использовать кэшированный элемент
        return _cachedElement;
    }
    catch (StaleElementReferenceException)
    {
        // Автоматический повторный поиск
        _cachedElement = FindElement(driver, locatorType, locatorValue, timeout);
        return _cachedElement;
    }
}
```

#### iframe Context Recovery

```csharp
protected void EnsureCorrectContext(IWebDriver driver)
{
    try
    {
        // Проверка текущего контекста
        driver.FindElement(By.TagName("body"));
    }
    catch (NoSuchElementException)
    {
        // Возврат к default content
        driver.SwitchTo().DefaultContent();
        LogWarning("Context was incorrect, switched to default content");
    }
}
```

### Error Logging Format

Все ошибки логируются в структурированном формате:

```json
{
  "timestamp": "2024-01-15T10:30:45.123Z",
  "level": "ERROR",
  "activityName": "ElementClick",
  "sessionId": "browser-session-1",
  "errorType": "ElementLocationException",
  "errorMessage": "Element not found after 3 retry attempts",
  "locator": {
    "type": "CssSelector",
    "value": "#submit-button"
  },
  "currentUrl": "https://example.com/form",
  "screenshotPath": "/screenshots/2024-01-15_10-30-45_ElementClick.png",
  "retryAttempts": [
    {
      "attempt": 1,
      "timestamp": "2024-01-15T10:30:42.100Z",
      "error": "NoSuchElementException"
    },
    {
      "attempt": 2,
      "timestamp": "2024-01-15T10:30:43.150Z",
      "error": "NoSuchElementException"
    },
    {
      "attempt": 3,
      "timestamp": "2024-01-15T10:30:44.200Z",
      "error": "NoSuchElementException"
    }
  ],
  "stackTrace": "..."
}
```


## Testing Strategy

### Dual Testing Approach

Browser модуль использует комбинацию unit-тестов и property-based тестов для обеспечения комплексного покрытия:

- **Unit tests**: Проверяют конкретные примеры, граничные случаи и интеграционные точки
- **Property tests**: Проверяют универсальные свойства на большом количестве сгенерированных входных данных

Оба подхода дополняют друг друга: unit-тесты ловят конкретные баги, property-тесты проверяют общую корректность.

### Property-Based Testing Configuration