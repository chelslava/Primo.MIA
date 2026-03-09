# Selenium Browser Activities

Набор активностей для автоматизации браузера через Selenium WebDriver.

## Реализованные активности

### Фаза 1: Базовое управление браузером

1. **BrowserOpen** — Открыть браузер
   - Поддержка Chrome, Firefox, Edge
   - Headless режим
   - Режим инкогнито
   - Отключение изображений
   - Пользовательский User-Agent

2. **BrowserClose** — Закрыть браузер
   - Корректное освобождение ресурсов
   - Удаление сессии из RepoDict

3. **BrowserNavigate** — Навигация
   - Переход по URL
   - Поддержка относительных и абсолютных путей

4. **BrowserGetInfo** — Получить информацию
   - Текущий URL
   - Заголовок страницы
   - HTML код страницы

5. **BrowserScreenshot** — Скриншот
   - Сохранение в файл PNG
   - Получение Base64 строки

### Фаза 2: Поиск и проверка элементов

6. **ElementFind** — Найти элемент
   - Поддержка всех типов локаторов (Id, Name, ClassName, TagName, LinkText, PartialLinkText, CssSelector, XPath)
   - Явное ожидание появления элемента
   - Сохранение в RepoDict

7. **ElementFindAll** — Найти все элементы
   - Поиск множественных элементов
   - Возврат списка ID

8. **ElementExists** — Проверить существование
   - Быстрая проверка без ожидания
   - Не выбрасывает исключение

9. **ElementGetProperty** — Получить свойство
   - Текст элемента
   - HTML атрибуты
   - CSS свойства

10. **ElementIsVisible** — Проверить видимость
    - Displayed (видимость)
    - Enabled (включённость)
    - Selected (выбранность)

### Фаза 3: Взаимодействие с элементами

11. **ElementClick** — Клик по элементу
    - Опциональное ожидание после клика

12. **ElementTypeText** — Ввод текста
    - Очистка поля перед вводом
    - Эмуляция печати (посимвольный ввод)
    - Настраиваемая задержка

## Архитектура

### Управление сессиями

Все сессии браузера хранятся в `RepoDict` с уникальными ID:
```csharp
string sessionId = "Browser_abc123...";
RepoDict.Instance.Set(sessionId, webDriver);
```

### Управление элементами

Найденные элементы также сохраняются в `RepoDict`:
```csharp
string elementId = "Element_xyz789...";
RepoDict.Instance.Set(elementId, webElement);
```

### Helper-класс

`SeleniumHelper.cs` содержит вспомогательные методы:
- `GetDriver(sessionId)` — получение драйвера
- `GetElement(elementId)` — получение элемента
- `CreateLocator(type, value)` — создание локатора
- `WaitForElement(driver, locator, timeout)` — ожидание элемента
- `ElementExists(driver, locator)` — проверка существования
- `TakeScreenshotBase64(driver)` — создание скриншота
- `ExecuteJavaScript(driver, script, args)` — выполнение JS
- `ScrollToElement(driver, element, alignment)` — прокрутка

## Типы локаторов

| Тип | Описание | Пример |
|-----|----------|--------|
| `Id` | По атрибуту id | `"username"` |
| `Name` | По атрибуту name | `"email"` |
| `ClassName` | По CSS классу | `"btn-primary"` |
| `TagName` | По имени тега | `"input"` |
| `LinkText` | По тексту ссылки | `"Войти"` |
| `PartialLinkText` | По частичному тексту | `"Вой"` |
| `CssSelector` | CSS селектор | `"div.container > p"` |
| `XPath` | XPath выражение | `"//input[@type='text']"` |

## Примеры использования

### Базовый сценарий

```
1. BrowserOpen
   - BrowserType: Chrome
   - Headless: false
   → SessionId: browser_session

2. BrowserNavigate
   - SessionId: browser_session
   - URL: "https://example.com"

3. ElementFind
   - SessionId: browser_session
   - LocatorType: Id
   - LocatorValue: "username"
   → ElementId: element_username

4. ElementTypeText
   - ElementId: element_username
   - Text: "user@example.com"

5. ElementFind
   - SessionId: browser_session
   - LocatorType: Id
   - LocatorValue: "submit"
   → ElementId: element_submit

6. ElementClick
   - ElementId: element_submit

7. BrowserClose
   - SessionId: browser_session
```

### Работа с множественными элементами

```
1. ElementFindAll
   - SessionId: browser_session
   - LocatorType: ClassName
   - LocatorValue: "product-item"
   → ElementIds: [element_1, element_2, element_3]
   → Count: 3

2. Для каждого element_id в ElementIds:
   - ElementGetProperty
     - ElementId: element_id
     - PropertyName: "text"
     → Value: название товара
```

## Зависимости

- Selenium.WebDriver 4.16.2
- Selenium.Support 4.16.2
- OpenQA.Selenium (включено в WebDriver)

## Следующие фазы

### Фаза 4: Ожидания и синхронизация
- BrowserWaitFor — универсальное ожидание
- BrowserWaitForPageLoad — ожидание загрузки
- BrowserWaitForAjax — ожидание AJAX
- ElementWaitForState — ожидание состояния элемента

### Фаза 5: JavaScript и продвинутые функции
- BrowserExecuteJavaScript — выполнение JS
- ElementScrollTo — прокрутка к элементу
- BrowserScroll — прокрутка страницы
- BrowserManageCookies — управление cookies
- BrowserSwitchTo — переключение контекста

### Фаза 6: Работа с формами
- ElementSelect — работа с select
- ElementClear — очистка поля
- ElementSubmit — отправка формы
- ElementHover — наведение курсора
- ElementDragDrop — drag & drop

### Фаза 7: Работа с диалогами
- AlertHandle — обработка алертов
- FileUpload — загрузка файлов
- FileDownloadWait — ожидание загрузки

## Известные ограничения

1. **WebDriver должен быть установлен**: ChromeDriver, GeckoDriver или EdgeDriver должны быть доступны в PATH или в папке с приложением.

2. **Stale элементы**: Элементы могут стать недействительными после обновления страницы. Используйте ElementFind повторно.

3. **Headless режим**: Некоторые сайты могут определять headless браузер и блокировать доступ.

4. **Производительность**: Отключение изображений ускоряет загрузку, но может нарушить работу некоторых сайтов.

## Отладка

Для отладки используйте:
- `BrowserGetInfo` — проверка текущего состояния
- `BrowserScreenshot` — визуальная проверка
- `ElementGetProperty` с `"text"` — чтение содержимого
- `ElementExists` — проверка наличия элемента

## Безопасность

- Не храните пароли в коде активностей
- Используйте переменные для чувствительных данных
- Закрывайте браузер через `BrowserClose` для освобождения ресурсов
- В headless режиме скриншоты помогают отладке без GUI
