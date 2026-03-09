# Browser Activities - Quick Start Guide

## Установка

1. Установите WebDriver для вашего браузера:
   - **Chrome**: [ChromeDriver](https://chromedriver.chromium.org/)
   - **Firefox**: [GeckoDriver](https://github.com/mozilla/geckodriver/releases)
   - **Edge**: [EdgeDriver](https://developer.microsoft.com/en-us/microsoft-edge/tools/webdriver/)

2. Добавьте драйвер в PATH или в папку с проектом

## Базовый пример

### Сценарий: Открыть сайт и получить заголовок

```
┌─────────────────────────────────────┐
│ 1. BrowserOpen                      │
│    BrowserType: Chrome              │
│    Headless: false                  │
│    → SessionId: browserSession      │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│ 2. BrowserNavigate                  │
│    SessionId: browserSession        │
│    URL: "https://example.com"       │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│ 3. BrowserGetInfo                   │
│    SessionId: browserSession        │
│    → CurrentUrl: currentUrl         │
│    → PageTitle: pageTitle           │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│ 4. BrowserClose                     │
│    SessionId: browserSession        │
└─────────────────────────────────────┘
```

### Сценарий: Заполнить форму

```
1. BrowserOpen → sessionId
2. BrowserNavigate (URL: "https://example.com/form")
3. ElementFind (Id: "name") → elementName
4. ElementTypeText (elementName, "Иван Иванов")
5. ElementFind (Id: "email") → elementEmail
6. ElementTypeText (elementEmail, "ivan@example.com")
7. ElementFind (Id: "submit") → elementSubmit
8. ElementClick (elementSubmit)
9. BrowserWaitFor (UrlContains: "/success")
10. BrowserClose (sessionId)
```

## Типы локаторов

| Локатор | Когда использовать | Пример |
|---------|-------------------|--------|
| **Id** | Элемент имеет уникальный id | `"username"` |
| **Name** | Элемент имеет атрибут name | `"email"` |
| **ClassName** | Поиск по CSS классу | `"btn-primary"` |
| **CssSelector** | Сложные селекторы | `"div.form > input[type='text']"` |
| **XPath** | Навигация по DOM | `"//button[contains(text(),'Войти')]"` |
| **LinkText** | Точный текст ссылки | `"Подробнее"` |
| **PartialLinkText** | Часть текста ссылки | `"Подроб"` |
| **TagName** | Поиск по тегу | `"button"` |

## Частые сценарии

### Работа с выпадающим списком

```
1. ElementFind (Id: "country") → selectElement
2. ElementSelect
   - ElementId: selectElement
   - SelectMode: ByText
   - Value: "Россия"
```

### Ожидание элемента

```
1. BrowserWaitFor
   - Condition: ElementVisible
   - LocatorType: Id
   - LocatorValue: "result"
   - Timeout: 10
```

### Работа с iframe

```
1. BrowserSwitchTo (Frame, "0")
2. ElementFind (Id: "inner-element") → element
3. ElementClick (element)
4. BrowserSwitchTo (DefaultContent)
```

### Обработка алерта

```
1. ElementClick (buttonElement)
2. BrowserWaitFor (AlertPresent, timeout: 5)
3. AlertHandle
   - Action: GetText
   → AlertText: alertMessage
4. AlertHandle (Action: Accept)
```

### Скриншот при ошибке

```
Try:
  [основная логика]
Catch:
  1. BrowserScreenshot
     - SessionId: browserSession
     - FilePath: "error_screenshot.png"
  2. [обработка ошибки]
```

## Советы по производительности

1. **Используйте явные ожидания** вместо Sleep:
   - ✅ BrowserWaitFor (ElementVisible)
   - ❌ Delay (5000)

2. **Переиспользуйте сессию браузера**:
   - Открывайте браузер один раз для серии действий
   - Закрывайте только в конце

3. **Headless режим для фоновых задач**:
   - Быстрее на 30-50%
   - Меньше потребление ресурсов

4. **Отключайте изображения** если не нужны:
   - DisableImages: true
   - Ускоряет загрузку страниц

## Отладка

### Проблема: Элемент не найден

**Решение:**
1. Увеличьте таймаут в ElementFind
2. Используйте BrowserWaitFor перед поиском
3. Проверьте локатор через BrowserScreenshot
4. Убедитесь что элемент не в iframe

### Проблема: Stale element

**Решение:**
1. Не сохраняйте элементы надолго
2. Ищите элемент заново после обновления страницы
3. Используйте ElementExists для проверки

### Проблема: Клик не работает

**Решение:**
1. Используйте ElementScrollTo перед кликом
2. Проверьте ElementIsVisible
3. Попробуйте ElementHover перед кликом
4. Используйте JavaScript: BrowserExecuteJavaScript("arguments[0].click()", element)

## Ограничения

- WebDriver должен быть установлен и доступен
- Версия драйвера должна соответствовать версии браузера
- Headless режим может не работать на некоторых сайтах
- Элементы становятся stale после обновления страницы
- Popup окна требуют BrowserSwitchTo

## Дополнительные ресурсы

- [Selenium Documentation](https://www.selenium.dev/documentation/)
- [CSS Selectors Reference](https://www.w3schools.com/cssref/css_selectors.asp)
- [XPath Tutorial](https://www.w3schools.com/xml/xpath_intro.asp)
- [Browser/README.md](./README.md) - полная документация
