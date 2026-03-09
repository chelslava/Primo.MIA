# Browser Activities Module — Primo MIA

## Обзор модуля

Browser Activities — это полнофункциональный модуль для автоматизации веб-браузеров в Primo RPA Studio. Модуль построен на базе Selenium WebDriver и предоставляет 40+ активностей для работы с браузерами Chrome, Firefox и Edge.

## Возможности

✅ **Управление браузером**
- Открытие/закрытие браузера с настройками
- Навигация по URL
- Управление окнами и вкладками
- Выполнение JavaScript кода
- Создание скриншотов

✅ **Поиск и взаимодействие с элементами**
- 8 типов локаторов (Id, Name, XPath, CSS и др.)
- Клики (обычный, двойной, правый, с удержанием)
- Ввод текста с симуляцией печати
- Drag & Drop
- Наведение курсора
- Прокрутка к элементам

✅ **Работа с формами**
- Заполнение полей ввода
- Выбор в dropdown (single/multiple)
- Загрузка файлов
- Отправка форм
- Работа с checkbox/radio

✅ **Ожидания и проверки**
- 8 типов условий ожидания
- Проверка существования элементов
- Проверка видимости и состояния
- Ожидание текста, URL, алертов

✅ **Расширенные функции**
- Управление cookies
- Работа с localStorage/sessionStorage
- Переключение между frame/window
- Обработка алертов
- Получение логов браузера
- Извлечение CSS свойств

## Архитектура

```
Browser/
├── Common/
│   ├── SeleniumHelper.cs      # Централизованные методы Selenium
│   ├── ActivityStrings.cs     # Константы для UI
│   └── ActivityIcons.cs       # Иконки активностей
├── [Activity]Back.cs          # Серверная логика (40+ файлов)
├── [Activity].xaml            # UI компонент
├── [Activity].xaml.cs         # Code-behind
└── Documentation/
    ├── BROWSER_ACTIVITIES_GUIDE.md      # Полное руководство
    ├── BROWSER_QUICK_REFERENCE.md       # Краткий справочник
    └── README_BROWSER_MODULE.md         # Этот файл
```

### Ключевые компоненты

**SeleniumHelper** — централизованный класс с 50+ методами для:
- Управления сессиями браузера
- Поиска и ожидания элементов
- Выполнения JavaScript
- Работы с окнами и вкладками
- Создания скриншотов
- Drag & Drop операций
- Работы с Web Storage

**RepoDict** — глобальное хранилище для:
- Сессий браузера (IWebDriver)
- Найденных элементов (IWebElement)
- Любых объектов процесса

**Система локаторов** — 8 типов поиска элементов:
- Id, Name, ClassName, TagName
- LinkText, PartialLinkText
- CssSelector, XPath

## Быстрый старт

### Минимальный пример

```
1. BrowserOpen
   - BrowserType: Chrome
   - Headless: false
   → Выход: sessionId

2. BrowserNavigate
   - SessionId: sessionId
   - Url: "https://example.com"

3. ElementClick
   - SessionId: sessionId
   - LocatorType: Id
   - LocatorValue: "submit-button"

4. BrowserClose
   - SessionId: sessionId
```

### Пример с извлечением данных

```
1. BrowserOpen → sessionId
2. BrowserNavigate (sessionId, "https://example.com/products")
3. ElementFindAll (CssSelector=".product-item") → productIds
4. Для каждого productId:
   - ElementGetProperty (productId, "text") → name
   - ElementGetProperty (productId, "attribute:data-price") → price
   - Добавить в список
5. BrowserClose (sessionId)
```

## Документация

### 📘 Полное руководство
**Файл:** `BROWSER_ACTIVITIES_GUIDE.md`

Содержит:
- Детальное описание архитектуры
- Все 40+ активностей с примерами
- Полный API SeleniumHelper
- Паттерны использования
- Типичные сценарии
- Рекомендации и best practices
- Решение частых проблем

### 📗 Краткий справочник
**Файл:** `BROWSER_QUICK_REFERENCE.md`

Содержит:
- Таблицу всех активностей
- Все типы и перечисления
- Частые комбинации действий
- Советы по производительности
- Чек-лист отладки
- Известные ограничения

## Список всех активностей

### Управление браузером (7)
- BrowserOpen
- BrowserClose
- BrowserNavigate
- BrowserGetInfo
- BrowserWaitFor
- BrowserExecuteJavaScript
- BrowserScreenshot

### Поиск элементов (4)
- ElementFind
- ElementFindAll
- ElementExists
- ElementIsVisible

### Взаимодействие с элементами (13)
- ElementClick
- ElementDoubleClick
- ElementRightClick
- ElementClickAndHold
- ElementTypeText
- ElementClear
- ElementSendKeys
- ElementSubmit
- ElementHover
- ElementHoverWithOffset
- ElementScrollTo
- ElementDragDrop
- ElementUploadFile

### Получение данных (4)
- ElementGetProperty
- ElementGetComputedStyle
- ElementGetRect
- ElementGetScreenshot

### Работа с формами (2)
- ElementSelect
- ElementSelectMultiple

### Управление окнами и вкладками (4)
- BrowserWindowManage
- BrowserTabManage
- BrowserSwitchTo
- AlertHandle

### Хранилище и cookies (3)
- BrowserManageCookies
- BrowserStorageManage
- BrowserGetLogs

**Итого: 37 активностей**

## Типы браузеров

| Браузер | Поддержка | Примечания |
|---|---|---|
| Chrome | ✅ Полная | Рекомендуется для production |
| Firefox | ✅ Полная | Альтернатива Chrome |
| Edge | ✅ Полная | Chromium-based |

## Режимы работы

### Headless режим
```
BrowserOpen (Chrome, Headless=true)
```
- Браузер работает без GUI
- Быстрее на 30-50%
- Меньше потребление ресурсов
- Идеально для серверов

### Режим инкогнито
```
BrowserOpen (Chrome, IncognitoMode=true)
```
- Не сохраняются cookies
- Не сохраняется история
- Чистая сессия каждый раз

### Отключение изображений
```
BrowserOpen (Chrome, DisableImages=true)
```
- Ускорение загрузки страниц
- Экономия трафика
- Полезно для парсинга данных

## Производительность

### Рекомендации

1. **Переиспользуйте элементы**
   ```
   ElementFind → elementId
   ElementClick (elementId)
   ElementGetProperty (elementId, "text")
   ```

2. **Используйте эффективные локаторы**
   - Id > Name > ClassName > CssSelector > XPath

3. **Явные ожидания вместо Sleep**
   ```
   ✅ BrowserWaitFor (ElementVisible, timeout=10)
   ❌ Thread.Sleep(5000)
   ```

4. **Headless режим для фоновых задач**
   ```
   BrowserOpen (Chrome, Headless=true)
   ```

5. **Отключайте изображения при парсинге**
   ```
   BrowserOpen (Chrome, DisableImages=true)
   ```

### Типичная производительность

| Операция | Время |
|---|---|
| Открытие браузера | 2-5 сек |
| Переход по URL | 1-3 сек |
| Поиск элемента | 0.1-1 сек |
| Клик по элементу | 0.1-0.5 сек |
| Ввод текста | 0.1-0.5 сек |
| Скриншот страницы | 0.5-2 сек |

## Отладка

### Инструменты отладки

1. **Скриншоты**
   ```
   BrowserScreenshot → "debug_screenshot.png"
   ElementGetScreenshot → "element_screenshot.png"
   ```

2. **Логи браузера**
   ```
   BrowserGetLogs (Browser) → consoleLogs
   ```

3. **Информация о странице**
   ```
   BrowserGetInfo → url, title, pageSource
   ```

4. **Проверка элементов**
   ```
   ElementExists → exists
   ElementIsVisible → isVisible, isEnabled
   ElementGetProperty ("text") → text
   ```

5. **Пауза в DevTools**
   ```
   BrowserExecuteJavaScript ("debugger;")
   ```

### Частые проблемы

| Проблема | Решение |
|---|---|
| Сессия не найдена | Проверить sessionId, пересоздать браузер |
| Элемент не найден | Увеличить таймаут, проверить локатор |
| Stale element | Искать элемент заново перед использованием |
| Element not clickable | Прокрутить к элементу или использовать JS клик |
| Timeout | Увеличить таймаут, использовать BrowserWaitFor |

## Примеры использования

### Авторизация на сайте
```
1. BrowserOpen → sessionId
2. BrowserNavigate (sessionId, "https://example.com/login")
3. ElementTypeText (Id="email", "user@example.com")
4. ElementTypeText (Id="password", "password123")
5. ElementClick (Id="login-button")
6. BrowserWaitFor (UrlContains, "/dashboard", Timeout=10)
7. BrowserClose (sessionId)
```

### Парсинг данных таблицы
```
1. BrowserOpen (Headless=true, DisableImages=true) → sessionId
2. BrowserNavigate (sessionId, url)
3. ElementFindAll (CssSelector="table tr") → rows
4. Для каждого row:
   - ElementFindAll (CssSelector="td") → cells
   - Извлечь данные из cells
5. BrowserClose (sessionId)
```

### Заполнение формы с файлом
```
1. BrowserOpen → sessionId
2. BrowserNavigate (sessionId, formUrl)
3. ElementTypeText (Id="name", "John Doe")
4. ElementSelect (Id="country", ByText, "United States")
5. ElementUploadFile (CssSelector="input[type='file']", filePath)
6. ElementClick (Id="submit")
7. BrowserWaitFor (ElementVisible, Id="success-message")
8. BrowserClose (sessionId)
```

### Работа с динамическим контентом
```
1. BrowserOpen → sessionId
2. BrowserNavigate (sessionId, url)
3. ElementClick (Id="load-more")
4. BrowserWaitFor (ElementVisible, CssSelector=".new-items", Timeout=15)
5. ElementFindAll (CssSelector=".item") → items
6. Обработать items
7. BrowserClose (sessionId)
```

## Интеграция с другими модулями

### С Dictionary активностями
```
1. DictionaryCreate (FromLists) → credentials
2. BrowserOpen → sessionId
3. DictionaryGetValue (credentials, "username") → username
4. ElementTypeText (Id="username", username)
5. ...
```

### С List активностями
```
1. ListCreate → urls
2. Для каждого url в urls:
   - BrowserNavigate (sessionId, url)
   - Извлечь данные
   - ListAdd (results, data)
```

### С File активностями
```
1. SearchFiles (directory, "*.csv") → files
2. Для каждого file:
   - Прочитать данные из file
   - BrowserNavigate (sessionId, url)
   - Заполнить форму данными
   - ElementClick (Id="submit")
```

## Требования

### Системные требования
- Windows 10/11 или Windows Server 2016+
- .NET Framework 4.7.2+
- 4 GB RAM (рекомендуется 8 GB)
- 500 MB свободного места

### Зависимости
- Selenium.WebDriver 4.x
- Selenium.Support 4.x
- Selenium.WebDriver.ChromeDriver (для Chrome)
- Selenium.WebDriver.GeckoDriver (для Firefox)
- Selenium.WebDriver.EdgeDriver (для Edge)

### Драйверы браузеров
Драйверы должны быть:
- В PATH системы, или
- Указаны явно через параметр DriverPath

## Лицензия и поддержка

Модуль Browser Activities является частью Primo MIA и распространяется под той же лицензией.

### Получение помощи
- Документация: `BROWSER_ACTIVITIES_GUIDE.md`
- Краткий справочник: `BROWSER_QUICK_REFERENCE.md`
- Примеры: См. раздел "Примеры использования"
- Исходный код: См. файлы `*Back.cs`

## История версий

### v1.0 (текущая)
- ✅ 37 активностей
- ✅ Поддержка Chrome, Firefox, Edge
- ✅ Headless режим
- ✅ Полная документация
- ✅ SeleniumHelper с 50+ методами
- ✅ Система управления сессиями
- ✅ Поддержка всех типов локаторов

## Roadmap

### Планируется в будущих версиях
- [ ] Поддержка Safari
- [ ] Запись действий пользователя
- [ ] AI-powered локаторы
- [ ] Визуальное тестирование
- [ ] Параллельное выполнение
- [ ] Интеграция с Selenium Grid

## Контрибьюторы

Модуль разработан командой Primo MIA.

---

**Последнее обновление:** 2024
**Версия документа:** 1.0
