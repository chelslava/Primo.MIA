# Архитектура Browser модуля

## Обзор

Этот документ описывает архитектуру Browser модуля проекта Primo.MIA с использованием модели C4.

---

## Level 1: System Context

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│                    RPA Developer                            │
│         (Создает автоматизацию браузера)                    │
│                                                             │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Использует
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│                   Primo Platform                            │
│              (RPA платформа)                                │
│                                                             │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Вызывает активности
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│                  Primo.MIA.Browser                          │
│         (Модуль автоматизации браузера)                     │
│                                                             │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Управляет
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│              Selenium WebDriver                             │
│         (Библиотека управления браузером)                   │
│                                                             │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Контролирует
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│          Браузеры (Chrome, Firefox, Edge)                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Level 2: Container Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                      Primo.MIA.Browser                           │
│                                                                  │
│  ┌────────────────────┐      ┌────────────────────┐            │
│  │                    │      │                    │            │
│  │  Browser           │      │  Element           │            │
│  │  Activities        │      │  Activities        │            │
│  │                    │      │                    │            │
│  │  - BrowserOpen     │      │  - ElementClick    │            │
│  │  - BrowserNavigate │      │  - ElementInput    │            │
│  │  - BrowserClose    │      │  - ElementHover    │            │
│  │  - BrowserWaitFor  │      │  - ElementGetInfo  │            │
│  │                    │      │                    │            │
│  └─────────┬──────────┘      └─────────┬──────────┘            │
│            │                           │                        │
│            │                           │                        │
│            └───────────┬───────────────┘                        │
│                        │                                        │
│                        ▼                                        │
│            ┌───────────────────────┐                           │
│            │                       │                           │
│            │  BrowserActivityBase  │                           │
│            │  (Базовый класс)      │                           │
│            │                       │                           │
│            └───────────┬───────────┘                           │
│                        │                                        │
│                        ▼                                        │
│            ┌───────────────────────┐                           │
│            │                       │                           │
│            │  Core Services        │                           │
│            │                       │                           │
│            │  - SessionManager     │                           │
│            │  - ElementLocator     │                           │
│            │  - SessionResolver    │                           │
│            │  - SeleniumHelper     │                           │
│            │                       │                           │
│            └───────────┬───────────┘                           │
│                        │                                        │
└────────────────────────┼────────────────────────────────────────┘
                         │
                         ▼
              ┌──────────────────────┐
              │                      │
              │  Selenium WebDriver  │
              │                      │
              └──────────────────────┘
```

---

## Level 3: Component Diagram

### Browser Activities Component

```
┌─────────────────────────────────────────────────────────────┐
│                    Browser Activities                       │
│                                                             │
│  ┌──────────────────┐                                      │
│  │ BrowserOpenBack  │──┐                                   │
│  └──────────────────┘  │                                   │
│                        │                                   │
│  ┌──────────────────┐  │                                   │
│  │BrowserNavigateBack│─┤                                   │
│  └──────────────────┘  │                                   │
│                        │  extends                          │
│  ┌──────────────────┐  │                                   │
│  │ BrowserCloseBack │──┤                                   │
│  └──────────────────┘  │                                   │
│                        │                                   │
│  ┌──────────────────┐  │                                   │
│  │BrowserWaitForBack│──┘                                   │
│  └──────────────────┘                                      │
│                        │                                   │
│                        ▼                                   │
│            ┌───────────────────────┐                       │
│            │                       │                       │
│            │ BrowserActivityBase   │                       │
│            │                       │                       │
│            │ + GetDriverFromContext│                       │
│            │ + FindElement         │                       │
│            │ + CreateSuccessResult │                       │
│            │ + SafeExecute         │                       │
│            │ + ValidateUrl         │                       │
│            │                       │                       │
│            └───────────┬───────────┘                       │
│                        │                                   │
│                        │ uses                              │
│                        ▼                                   │
│            ┌───────────────────────┐                       │
│            │                       │                       │
│            │   ISessionManager     │                       │
│            │   IElementLocator     │                       │
│            │   IActivityLogger     │                       │
│            │                       │                       │
│            └───────────────────────┘                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Core Services Component

```
┌─────────────────────────────────────────────────────────────┐
│                      Core Services                          │
│                                                             │
│  ┌──────────────────────────────────────────────┐          │
│  │          SessionManager                      │          │
│  │                                              │          │
│  │  - RegisterSession(id, driver)               │          │
│  │  - GetSession(id): IWebDriver                │          │
│  │  - RemoveSession(id): bool                   │          │
│  │  - GetCurrentSessionId(): string             │          │
│  │                                              │          │
│  └──────────────────┬───────────────────────────┘          │
│                     │                                       │
│                     │ uses                                  │
│                     ▼                                       │
│  ┌──────────────────────────────────────────────┐          │
│  │       BrowserSessionContext                  │          │
│  │                                              │          │
│  │  - Push(sessionId, isContainerEntry)         │          │
│  │  - Pop()                                     │          │
│  │  - CurrentSessionId: string                  │          │
│  │  - ClearStack()                              │          │
│  │                                              │          │
│  └──────────────────┬───────────────────────────┘          │
│                     │                                       │
│                     │ stores in                             │
│                     ▼                                       │
│  ┌──────────────────────────────────────────────┐          │
│  │            RepoDict                          │          │
│  │      (Primo Platform Storage)                │          │
│  │                                              │          │
│  │  - Set<T>(key, value)                        │          │
│  │  - Get<T>(key): T                            │          │
│  │  - Contains(key): bool                       │          │
│  │                                              │          │
│  └──────────────────────────────────────────────┘          │
│                                                             │
│  ┌──────────────────────────────────────────────┐          │
│  │          ElementLocator                      │          │
│  │                                              │          │
│  │  - FindElement(driver, type, value, timeout) │          │
│  │  - FindElements(driver, type, value)         │          │
│  │  - WaitForClickable(...)                     │          │
│  │  - WaitForVisible(...)                       │          │
│  │                                              │          │
│  └──────────────────┬───────────────────────────┘          │
│                     │                                       │
│                     │ uses                                  │
│                     ▼                                       │
│  ┌──────────────────────────────────────────────┐          │
│  │         SeleniumHelper                       │          │
│  │                                              │          │
│  │  - CreateLocator(type, value): By            │          │
│  │  - WaitForElement(driver, locator, timeout)  │          │
│  │  - GetDriver(sessionId): IWebDriver          │          │
│  │  - RegisterDriver(sessionId, driver)         │          │
│  │                                              │          │
│  └──────────────────────────────────────────────┘          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Ключевые компоненты

### 1. BrowserActivityBase

**Назначение:** Базовый класс для всех Browser активностей

**Ответственности:**
- Управление WebDriver (получение, валидация)
- Поиск элементов с ожиданием
- Создание результатов выполнения
- Валидация входных данных
- Безопасное выполнение с обработкой ошибок
- Логирование

**Преимущества:**
- Устраняет дублирование кода
- Обеспечивает единообразие
- Упрощает тестирование
- Облегчает поддержку

### 2. SessionManager

**Назначение:** Управление сессиями браузера

**Ответственности:**
- Регистрация новых сессий
- Получение существующих сессий
- Удаление сессий
- Отслеживание текущей сессии

**Реализация:** Использует RepoDict для хранения

### 3. BrowserSessionContext

**Назначение:** Ambient Context для управления стеком сессий

**Ответственности:**
- Поддержка стека сессий
- Автоматическая очистка при входе в контейнер
- Предоставление текущей сессии

**Паттерн:** Ambient Context

### 4. SessionResolver

**Назначение:** Разрешение sessionID с учетом контекста

**Ответственности:**
- Разрешение явного sessionId
- Использование текущей сессии из контекста
- Валидация sessionId

### 5. ElementLocator

**Назначение:** Поиск элементов на странице

**Ответственности:**
- Создание локаторов
- Ожидание элементов
- Проверка видимости/кликабельности

---

## Потоки данных

### Поток выполнения активности

```
1. Primo Platform
   │
   ├─> Вызывает TimedAction(ScriptingData)
   │
2. Activity (например, ElementClickBack)
   │
   ├─> SafeExecute(() => {
   │     ├─> GetDriverFromContext(sd, "Prop_SessionId")
   │     │     ├─> SessionResolver.Resolve(sessionId)
   │     │     │     ├─> Если пустой → BrowserSessionContext.CurrentSessionId
   │     │     │     └─> Иначе → возвращает sessionId
   │     │     └─> SeleniumHelper.GetDriver(resolvedSessionId)
   │     │           └─> RepoDict.Get<IWebDriver>(sessionId)
   │     │
   │     ├─> FindElement(driver, locatorType, locatorValue, timeout)
   │     │     ├─> SeleniumHelper.CreateLocator(type, value)
   │     │     └─> SeleniumHelper.WaitForElement(driver, locator, timeout)
   │     │
   │     ├─> PerformAction(element)
   │     │
   │     └─> CreateSuccessResult(message)
   │   })
   │
3. Возврат ExecutionResult в Primo Platform
```

### Поток управления сессией

```
1. BrowserOpen
   │
   ├─> Создает IWebDriver
   │
   ├─> Генерирует sessionId (если не указан)
   │
   ├─> SeleniumHelper.RegisterDriver(sessionId, driver)
   │     └─> RepoDict.Set(sessionId, driver)
   │
   ├─> BrowserSessionContext.Push(sessionId, isContainerEntry: true)
   │     ├─> Если isContainerEntry → ClearStack()
   │     └─> Добавляет sessionId в стек
   │
   └─> Возвращает sessionId

2. Последующие активности
   │
   ├─> SessionResolver.Resolve("")
   │     └─> BrowserSessionContext.CurrentSessionId
   │           └─> Возвращает верхний элемент стека
   │
   └─> Используют сессию

3. BrowserClose
   │
   ├─> driver.Quit()
   │
   ├─> SeleniumHelper.UnregisterDriver(sessionId)
   │     └─> RepoDict.Remove(sessionId)
   │
   └─> BrowserSessionContext.Pop()
```

---

## Паттерны проектирования

### 1. Template Method (Шаблонный метод)

**Где:** BrowserActivityBase.TimedAction()

**Как:**
```csharp
public override ExecutionResult TimedAction(ScriptingData sd)
{
    return SafeExecute(() =>
    {
        IWebDriver driver = GetDriverFromContext(sd, nameof(Prop_SessionId));
        IWebElement element = ResolveElement(sd, driver);
        string result = PerformAction(sd, driver, element);
        return CreateSuccessResult(result);
    }, "Context");
}
```

### 2. Ambient Context

**Где:** BrowserSessionContext

**Как:**
```csharp
BrowserSessionContext.Push(sessionId, isContainerEntry: true);
// Текущая сессия доступна везде
var current = BrowserSessionContext.CurrentSessionId;
BrowserSessionContext.Pop();
```

### 3. Strategy (Стратегия)

**Где:** Локаторы элементов

**Как:** Разные типы локаторов (CSS, XPath, ID) используют разные стратегии поиска

### 4. Repository

**Где:** SessionManager, ElementRepository

**Как:** Абстракция доступа к хранилищу драйверов и элементов

### 5. Facade (Фасад)

**Где:** SeleniumHelper

**Как:** Упрощенный интерфейс к Selenium WebDriver API

---

## Принципы SOLID

### Single Responsibility Principle (SRP)
- ✅ Каждый класс имеет одну ответственность
- ✅ BrowserActivityBase - базовая функциональность
- ✅ SessionManager - управление сессиями
- ✅ ElementLocator - поиск элементов

### Open/Closed Principle (OCP)
- ✅ Базовый класс открыт для расширения
- ✅ Закрыт для модификации
- ✅ Новые активности наследуют BrowserActivityBase

### Liskov Substitution Principle (LSP)
- ✅ Все активности взаимозаменяемы через базовый класс
- ✅ Интерфейсы ISessionManager, IElementLocator

### Interface Segregation Principle (ISP)
- ✅ Интерфейсы разделены по функциональности
- ✅ ISessionManager, IElementLocator, IActivityLogger

### Dependency Inversion Principle (DIP)
- ⏳ В процессе внедрения
- ⏳ Зависимости от интерфейсов, а не от конкретных классов

---

## Масштабируемость

### Горизонтальная масштабируемость
- Поддержка множественных сессий браузера
- Изоляция сессий через sessionId
- Возможность параллельного выполнения

### Вертикальная масштабируемость
- Расширение через наследование
- Добавление новых типов локаторов
- Кастомные стратегии поиска

---

## Безопасность

### Валидация входных данных
- URL валидация (только HTTP/HTTPS)
- XPath санитизация
- Таймауты с ограничениями

### Изоляция
- Каждая сессия изолирована
- Нет утечек между сессиями
- Безопасное хранение credentials

---

## Производительность

### Оптимизации
- Кэширование локаторов
- Переиспользование WebDriver
- Минимизация ожиданий

### Метрики
- Время выполнения активности: < 100ms (без Selenium)
- Memory footprint: < 50MB
- CPU usage: < 10%

---

## Тестируемость

### Unit-тесты
- Изолированное тестирование компонентов
- Mocking через интерфейсы
- 27 тестов для sessionID

### Integration-тесты
- Тестирование взаимодействия компонентов
- Реальные сценарии использования

### E2E-тесты
- Полные сценарии автоматизации
- Тестирование с реальным браузером
