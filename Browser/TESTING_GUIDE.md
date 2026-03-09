# Testing Guide - Browser Activities

## Предварительные требования

### 1. Установка WebDriver

**Для Chrome:**
```powershell
# Проверить версию Chrome
(Get-Item "C:\Program Files\Google\Chrome\Application\chrome.exe").VersionInfo.FileVersion

# Скачать соответствующий ChromeDriver с:
# https://chromedriver.chromium.org/downloads

# Поместить chromedriver.exe в:
# - C:\Windows\System32\
# - Или в папку проекта
# - Или добавить в PATH
```

**Для Firefox:**
```powershell
# Скачать GeckoDriver:
# https://github.com/mozilla/geckodriver/releases

# Поместить geckodriver.exe в PATH
```

### 2. Проверка установки

```powershell
# Проверить доступность драйвера
chromedriver --version
# Должно вывести: ChromeDriver 120.x.xxxx.xx

geckodriver --version
# Должно вывести: geckodriver 0.34.x
```

## Тестовые сценарии

### Тест 1: Базовая функциональность

**Цель:** Проверить открытие браузера, навигацию и закрытие.

**Шаги:**
1. Создать новый процесс в Primo RPA Studio
2. Добавить активность **BrowserOpen**
   - BrowserType: Chrome
   - Headless: false
   - SessionId: `browserSession`
3. Добавить активность **BrowserNavigate**
   - SessionId: `browserSession`
   - URL: `"https://example.com"`
4. Добавить активность **BrowserGetInfo**
   - SessionId: `browserSession`
   - CurrentUrl: `currentUrl`
   - PageTitle: `pageTitle`
5. Добавить активность **Log Message** (из Utilities)
   - Message: `$"Заголовок: {pageTitle}"`
6. Добавить активность **BrowserClose**
   - SessionId: `browserSession`

**Ожидаемый результат:**
- Браузер открывается
- Загружается example.com
- В логе появляется: "Заголовок: Example Domain"
- Браузер закрывается

---

### Тест 2: Поиск и взаимодействие с элементами

**Цель:** Проверить поиск элементов и ввод текста.

**Тестовый сайт:** https://www.google.com

**Шаги:**
1. **BrowserOpen** → `browserSession`
2. **BrowserNavigate**
   - URL: `"https://www.google.com"`
3. **ElementFind**
   - SessionId: `browserSession`
   - LocatorType: Name
   - LocatorValue: `"q"`
   - Timeout: 10
   - ElementId: `searchBox`
   - Found: `found`
4. **If** `found == true`:
   - **ElementTypeText**
     - ElementId: `searchBox`
     - Text: `"Primo RPA"`
     - ClearBeforeType: true
   - **ElementGetProperty**
     - ElementId: `searchBox`
     - PropertyName: `"attribute:value"`
     - Value: `searchValue`
   - **Log Message**: `$"Введено: {searchValue}"`
5. **BrowserClose**

**Ожидаемый результат:**
- Поле поиска найдено
- Текст введён
- Значение прочитано: "Primo RPA"

---

### Тест 3: Ожидание элементов

**Цель:** Проверить работу явных ожиданий.

**Тестовый сайт:** https://the-internet.herokuapp.com/dynamic_loading/1

**Шаги:**
1. **BrowserOpen** → `browserSession`
2. **BrowserNavigate**
   - URL: `"https://the-internet.herokuapp.com/dynamic_loading/1"`
3. **ElementFind** (кнопка Start)
   - LocatorType: CssSelector
   - LocatorValue: `"#start button"`
   - ElementId: `startButton`
4. **ElementClick**
   - ElementId: `startButton`
5. **BrowserWaitFor**
   - Condition: ElementVisible
   - LocatorType: Id
   - LocatorValue: `"finish"`
   - Timeout: 10
   - ConditionMet: `finished`
   - WaitTime: `waitTime`
6. **Log Message**: `$"Ожидание заняло {waitTime}мс"`
7. **ElementFind**
   - LocatorType: Id
   - LocatorValue: `"finish"`
   - ElementId: `finishElement`
8. **ElementGetProperty**
   - ElementId: `finishElement`
   - PropertyName: `"text"`
   - Value: `finishText`
9. **Log Message**: `$"Результат: {finishText}"`
10. **BrowserClose**

**Ожидаемый результат:**
- Кнопка нажата
- Ожидание ~5 секунд
- Текст "Hello World!" получен

---

### Тест 4: Работа с формами

**Цель:** Проверить заполнение формы и select элементы.

**Тестовый сайт:** https://the-internet.herokuapp.com/dropdown

**Шаги:**
1. **BrowserOpen** → `browserSession`
2. **BrowserNavigate**
   - URL: `"https://the-internet.herokuapp.com/dropdown"`
3. **ElementFind**
   - LocatorType: Id
   - LocatorValue: `"dropdown"`
   - ElementId: `dropdownElement`
4. **ElementSelect**
   - ElementId: `dropdownElement`
   - SelectMode: ByText
   - Value: `"Option 1"`
5. **ElementGetProperty**
   - ElementId: `dropdownElement`
   - PropertyName: `"attribute:value"`
   - Value: `selectedValue`
6. **Log Message**: `$"Выбрано: {selectedValue}"`
7. **BrowserClose**

**Ожидаемый результат:**
- Dropdown найден
- Опция выбрана
- Значение: "1"

---

### Тест 5: JavaScript выполнение

**Цель:** Проверить выполнение JavaScript кода.

**Шаги:**
1. **BrowserOpen** → `browserSession`
2. **BrowserNavigate**
   - URL: `"https://example.com"`
3. **BrowserExecuteJavaScript**
   - Script: `"return document.title;"`
   - Result: `jsTitle`
4. **Log Message**: `$"JS Title: {jsTitle}"`
5. **BrowserExecuteJavaScript**
   - Script: `"return document.querySelectorAll('p').length;"`
   - Result: `paragraphCount`
6. **Log Message**: `$"Параграфов: {paragraphCount}"`
7. **BrowserExecuteJavaScript**
   - Script: `"window.scrollTo(0, document.body.scrollHeight);"`
8. **BrowserClose**

**Ожидаемый результат:**
- Заголовок получен через JS
- Количество параграфов подсчитано
- Страница прокручена вниз

---

### Тест 6: Скриншоты

**Цель:** Проверить создание скриншотов.

**Шаги:**
1. **BrowserOpen** → `browserSession`
2. **BrowserNavigate**
   - URL: `"https://example.com"`
3. **BrowserScreenshot**
   - SessionId: `browserSession`
   - FilePath: `"C:\\Temp\\screenshot1.png"`
   - Base64: `screenshotBase64`
4. **Log Message**: `$"Скриншот сохранён, Base64 длина: {screenshotBase64.Length}"`
5. **BrowserClose**

**Ожидаемый результат:**
- Файл C:\Temp\screenshot1.png создан
- Base64 строка получена (длина > 10000)

---

### Тест 7: Cookies

**Цель:** Проверить управление cookies.

**Шаги:**
1. **BrowserOpen** → `browserSession`
2. **BrowserNavigate**
   - URL: `"https://example.com"`
3. **BrowserManageCookies**
   - Operation: Set
   - CookieName: `"test_cookie"`
   - CookieValue: `"test_value"`
4. **BrowserManageCookies**
   - Operation: Get
   - CookieName: `"test_cookie"`
   - Result: `cookieValue`
5. **Log Message**: `$"Cookie: {cookieValue}"`
6. **BrowserManageCookies**
   - Operation: GetAll
   - Result: `allCookies`
7. **Log Message**: `$"Все cookies: {allCookies}"`
8. **BrowserManageCookies**
   - Operation: Delete
   - CookieName: `"test_cookie"`
9. **BrowserClose**

**Ожидаемый результат:**
- Cookie установлен
- Cookie прочитан: "test_value"
- Все cookies получены в JSON
- Cookie удалён

---

### Тест 8: Множественные элементы

**Цель:** Проверить поиск всех элементов.

**Тестовый сайт:** https://example.com

**Шаги:**
1. **BrowserOpen** → `browserSession`
2. **BrowserNavigate**
   - URL: `"https://example.com"`
3. **ElementFindAll**
   - LocatorType: TagName
   - LocatorValue: `"p"`
   - ElementIds: `paragraphIds`
   - Count: `paragraphCount`
4. **Log Message**: `$"Найдено параграфов: {paragraphCount}"`
5. **For Each** `elementId` in `paragraphIds`:
   - **ElementGetProperty**
     - ElementId: `elementId`
     - PropertyName: `"text"`
     - Value: `paragraphText`
   - **Log Message**: `$"Параграф: {paragraphText}"`
6. **BrowserClose**

**Ожидаемый результат:**
- Найдено 2 параграфа
- Текст каждого выведен в лог

---

### Тест 9: Headless режим

**Цель:** Проверить работу в headless режиме.

**Шаги:**
1. **BrowserOpen**
   - BrowserType: Chrome
   - Headless: **true**
   - SessionId: `browserSession`
2. **BrowserNavigate**
   - URL: `"https://example.com"`
3. **BrowserGetInfo**
   - PageTitle: `pageTitle`
4. **Log Message**: `$"Headless Title: {pageTitle}"`
5. **BrowserScreenshot**
   - FilePath: `"C:\\Temp\\headless_screenshot.png"`
6. **BrowserClose**

**Ожидаемый результат:**
- Браузер НЕ виден на экране
- Заголовок получен
- Скриншот создан

---

### Тест 10: Обработка ошибок

**Цель:** Проверить обработку ошибок.

**Шаги:**
1. **BrowserOpen** → `browserSession`
2. **BrowserNavigate**
   - URL: `"https://example.com"`
3. **Try-Catch:**
   - **Try:**
     - **ElementFind**
       - LocatorType: Id
       - LocatorValue: `"nonexistent_element"`
       - Timeout: 3
       - Found: `found`
     - **If** `found == false`:
       - **Log Message**: "Элемент не найден (ожидаемо)"
   - **Catch:**
     - **Log Message**: `$"Ошибка: {exception.Message}"`
     - **BrowserScreenshot**
       - FilePath: `"C:\\Temp\\error_screenshot.png"`
4. **BrowserClose**

**Ожидаемый результат:**
- Элемент не найден
- Ошибка обработана
- Скриншот создан

---

## Чек-лист проверки

### Базовые функции
- [ ] BrowserOpen открывает браузер
- [ ] BrowserClose закрывает браузер
- [ ] BrowserNavigate переходит по URL
- [ ] BrowserGetInfo возвращает корректные данные
- [ ] BrowserScreenshot создаёт файл

### Поиск элементов
- [ ] ElementFind находит элемент по Id
- [ ] ElementFind находит элемент по CssSelector
- [ ] ElementFind находит элемент по XPath
- [ ] ElementFindAll возвращает список элементов
- [ ] ElementExists корректно определяет наличие

### Взаимодействие
- [ ] ElementClick выполняет клик
- [ ] ElementTypeText вводит текст
- [ ] ElementClear очищает поле
- [ ] ElementSelect выбирает опцию
- [ ] ElementGetProperty читает свойства

### Ожидания
- [ ] BrowserWaitFor ожидает ElementVisible
- [ ] BrowserWaitFor ожидает ElementClickable
- [ ] BrowserWaitFor ожидает TitleContains
- [ ] BrowserWaitFor ожидает UrlContains

### Продвинутые функции
- [ ] BrowserExecuteJavaScript выполняет код
- [ ] ElementScrollTo прокручивает к элементу
- [ ] BrowserManageCookies работает с cookies
- [ ] ElementHover наводит курсор
- [ ] AlertHandle обрабатывает алерты

### Режимы
- [ ] Headless режим работает
- [ ] Режим инкогнито работает
- [ ] Отключение изображений работает

## Известные проблемы при тестировании

### Проблема: ChromeDriver не найден
**Решение:**
```powershell
# Добавить в PATH
$env:Path += ";C:\path\to\chromedriver"
```

### Проблема: Версия драйвера не совпадает
**Решение:**
- Проверить версию Chrome: `chrome://version`
- Скачать соответствующий ChromeDriver

### Проблема: Элемент не найден
**Решение:**
- Увеличить таймаут
- Проверить локатор через DevTools (F12)
- Использовать BrowserScreenshot для отладки

### Проблема: Headless не работает
**Решение:**
- Некоторые сайты блокируют headless
- Попробовать обычный режим
- Добавить User-Agent

## Автоматизация тестирования

Создайте процесс с последовательным выполнением всех тестов:

```
For Each test in [Test1, Test2, ..., Test10]:
  Try:
    Execute test
    Log "✓ {test} passed"
  Catch:
    Log "✗ {test} failed: {exception.Message}"
    Screenshot "error_{test}.png"
```

## Метрики производительности

Замерьте время выполнения:

| Операция | Обычный режим | Headless | С отключением изображений |
|----------|---------------|----------|---------------------------|
| BrowserOpen | ~3-5 сек | ~2-3 сек | ~2-3 сек |
| BrowserNavigate | ~1-3 сек | ~0.5-2 сек | ~0.3-1 сек |
| ElementFind | ~0.1-1 сек | ~0.1-1 сек | ~0.1-1 сек |
| ElementClick | ~0.1 сек | ~0.1 сек | ~0.1 сек |

## Отчёт о тестировании

После прохождения всех тестов заполните:

```
Дата: __________
Версия Primo.MIA: 1.1.0
Браузер: Chrome/Firefox/Edge версия: __________
WebDriver версия: __________

Пройдено тестов: __ / 10
Найдено проблем: __

Комментарии:
_________________________________
_________________________________
```
