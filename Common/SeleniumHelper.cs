// =============================================================================
// SeleniumHelper.cs — вспомогательные методы для работы с Selenium WebDriver.
//
// Содержит статические методы для:
//   - Получения драйвера из RepoDict
//   - Создания локаторов By из типа и значения
//   - Безопасного ожидания элементов
//   - Проверки существования элементов
//   - Создания скриншотов
//
// ВАЖНО: Все методы thread-safe и могут использоваться из разных активностей.
// =============================================================================

using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Вспомогательные методы для работы с Selenium WebDriver.
    /// Упрощают взаимодействие с браузером и элементами страницы.
    /// </summary>
    public static class SeleniumHelper
    {
        // ── Работа с сессиями браузера ─────────────────────────────────

        /// <summary>
        /// Получает экземпляр WebDriver из RepoDict по ID сессии.
        /// Выбрасывает исключение если сессия не найдена.
        /// </summary>
        /// <param name="sessionId">ID сессии браузера</param>
        /// <returns>Экземпляр IWebDriver</returns>
        /// <exception cref="ArgumentException">Если сессия не найдена</exception>
        public static IWebDriver GetDriver(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("ID сессии не может быть пустым");

            var driver = RepoDict.Get<IWebDriver>(sessionId);
            if (driver == null)
                throw new ArgumentException($"Сессия браузера '{sessionId}' не найдена. Возможно браузер был закрыт.");

            return driver;
        }

        /// <summary>
        /// Проверяет существование сессии браузера в RepoDict.
        /// </summary>
        /// <param name="sessionId">ID сессии браузера</param>
        /// <returns>true если сессия существует, иначе false</returns>
        public static bool SessionExists(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            try
            {
                var driver = RepoDict.Get<IWebDriver>(sessionId);
                return driver != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Получает экземпляр WebElement из RepoDict по ID элемента.
        /// Выбрасывает исключение если элемент не найден.
        /// </summary>
        /// <param name="elementId">ID элемента</param>
        /// <returns>Экземпляр IWebElement</returns>
        /// <exception cref="ArgumentException">Если элемент не найден</exception>
        public static IWebElement GetElement(string elementId)
        {
            if (string.IsNullOrWhiteSpace(elementId))
                throw new ArgumentException("ID элемента не может быть пустым");

            var element = RepoDict.Get<IWebElement>(elementId);
            if (element == null)
                throw new ArgumentException($"Элемент '{elementId}' не найден в кеше");

            return element;
        }

        // ── Создание локаторов ─────────────────────────────────────────

        /// <summary>
        /// Создаёт локатор By из типа и значения.
        /// Поддерживает все стандартные стратегии поиска Selenium.
        /// </summary>
        /// <param name="locatorType">Тип локатора</param>
        /// <param name="locatorValue">Значение локатора</param>
        /// <returns>Объект By для поиска элемента</returns>
        /// <exception cref="ArgumentException">Если значение локатора пустое</exception>
        /// <exception cref="NotSupportedException">Если тип локатора не поддерживается</exception>
        public static By CreateLocator(ElementLocatorType locatorType, string locatorValue)
        {
            if (string.IsNullOrWhiteSpace(locatorValue))
                throw new ArgumentException("Значение локатора не может быть пустым");

            switch (locatorType)
            {
                case ElementLocatorType.Id:
                    return By.Id(locatorValue);

                case ElementLocatorType.Name:
                    return By.Name(locatorValue);

                case ElementLocatorType.ClassName:
                    return By.ClassName(locatorValue);

                case ElementLocatorType.TagName:
                    return By.TagName(locatorValue);

                case ElementLocatorType.LinkText:
                    return By.LinkText(locatorValue);

                case ElementLocatorType.PartialLinkText:
                    return By.PartialLinkText(locatorValue);

                case ElementLocatorType.CssSelector:
                    return By.CssSelector(locatorValue);

                case ElementLocatorType.XPath:
                    return By.XPath(locatorValue);

                default:
                    throw new NotSupportedException($"Тип локатора {locatorType} не поддерживается");
            }
        }

        // ── Поиск и ожидание элементов ─────────────────────────────────

        /// <summary>
        /// Ожидает появления элемента на странице с явным ожиданием.
        /// Использует WebDriverWait для синхронизации.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="locator">Локатор элемента</param>
        /// <param name="timeoutSeconds">Таймаут ожидания в секундах</param>
        /// <returns>Найденный элемент</returns>
        /// <exception cref="WebDriverTimeoutException">Если элемент не найден за указанное время</exception>
        public static IWebElement WaitForElement(IWebDriver driver, By locator, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
            return wait.Until(drv => drv.FindElement(locator));
        }

        /// <summary>
        /// Ожидает видимости элемента на странице.
        /// Элемент должен существовать в DOM и быть видимым (Displayed = true).
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="locator">Локатор элемента</param>
        /// <param name="timeoutSeconds">Таймаут ожидания в секундах</param>
        /// <returns>Найденный видимый элемент</returns>
        public static IWebElement WaitForElementVisible(IWebDriver driver, By locator, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
            return wait.Until(drv =>
            {
                try
                {
                    var element = drv.FindElement(locator);
                    return element.Displayed ? element : null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
            });
        }

        /// <summary>
        /// Ожидает кликабельности элемента (видим и включён).
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="locator">Локатор элемента</param>
        /// <param name="timeoutSeconds">Таймаут ожидания в секундах</param>
        /// <returns>Найденный кликабельный элемент</returns>
        public static IWebElement WaitForElementClickable(IWebDriver driver, By locator, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
            return wait.Until(drv =>
            {
                try
                {
                    var element = drv.FindElement(locator);
                    return (element.Displayed && element.Enabled) ? element : null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
            });
        }

        /// <summary>
        /// Проверяет существование элемента на странице без ожидания.
        /// Не выбрасывает исключение если элемент не найден.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="locator">Локатор элемента</param>
        /// <returns>true если элемент существует, иначе false</returns>
        public static bool ElementExists(IWebDriver driver, By locator)
        {
            try
            {
                driver.FindElement(locator);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        /// <summary>
        /// Находит первый элемент по локатору.
        /// Выбрасывает исключение если элемент не найден.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="locator">Локатор элемента</param>
        /// <returns>Найденный элемент</returns>
        /// <exception cref="NoSuchElementException">Если элемент не найден</exception>
        public static IWebElement FindElement(IWebDriver driver, By locator)
        {
            return driver.FindElement(locator);
        }

        /// <summary>
        /// Находит все элементы по локатору.
        /// Возвращает пустой список если элементы не найдены.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="locator">Локатор элементов</param>
        /// <returns>Список найденных элементов</returns>
        public static IReadOnlyCollection<IWebElement> FindElements(IWebDriver driver, By locator)
        {
            try
            {
                return driver.FindElements(locator);
            }
            catch (NoSuchElementException)
            {
                return new List<IWebElement>();
            }
        }

        // ── Скриншоты ──────────────────────────────────────────────────

        /// <summary>
        /// Создаёт скриншот страницы и возвращает его в формате Base64.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <returns>Скриншот в формате Base64 строки</returns>
        public static string TakeScreenshotBase64(IWebDriver driver)
        {
            var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
            return screenshot.AsBase64EncodedString;
        }

        /// <summary>
        /// Создаёт скриншот страницы и сохраняет в файл.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="filePath">Путь для сохранения файла</param>
        public static void TakeScreenshotToFile(IWebDriver driver, string filePath)
        {
            var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
            screenshot.SaveAsFile(filePath);
        }

        // ── Работа с JavaScript ────────────────────────────────────────

        /// <summary>
        /// Выполняет JavaScript код в контексте страницы.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="script">JavaScript код для выполнения</param>
        /// <param name="args">Аргументы для передачи в скрипт</param>
        /// <returns>Результат выполнения скрипта</returns>
        public static object ExecuteJavaScript(IWebDriver driver, string script, params object[] args)
        {
            var jsExecutor = (IJavaScriptExecutor)driver;
            return jsExecutor.ExecuteScript(script, args);
        }

        /// <summary>
        /// Прокручивает страницу к элементу используя JavaScript.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="element">Элемент к которому нужно прокрутить</param>
        /// <param name="alignment">Выравнивание элемента в окне</param>
        public static void ScrollToElement(IWebDriver driver, IWebElement element, ScrollAlignment alignment = ScrollAlignment.Center)
        {
            string alignValue;
            switch (alignment)
            {
                case ScrollAlignment.Top:
                    alignValue = "true";
                    break;
                case ScrollAlignment.Bottom:
                    alignValue = "false";
                    break;
                case ScrollAlignment.Center:
                    alignValue = "{block: 'center'}";
                    break;
                default:
                    alignValue = "true";
                    break;
            }

            var script = $"arguments[0].scrollIntoView({alignValue});";
            ExecuteJavaScript(driver, script, element);
        }

        // ── Валидация и проверки ───────────────────────────────────────

        /// <summary>
        /// Валидирует таймаут и возвращает корректное значение.
        /// </summary>
        /// <param name="timeout">Таймаут для проверки</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        /// <returns>Валидный таймаут</returns>
        public static int ValidateTimeout(int timeout, int defaultValue = 10)
        {
            return timeout > 0 ? timeout : defaultValue;
        }

        /// <summary>
        /// Генерирует уникальный ID для элемента.
        /// </summary>
        /// <param name="prefix">Префикс для ID</param>
        /// <returns>Уникальный ID</returns>
        public static string GenerateElementId(string prefix = "Element")
        {
            return $"{prefix}_{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Генерирует уникальный ID для сессии браузера.
        /// </summary>
        /// <returns>Уникальный ID сессии</returns>
        public static string GenerateSessionId()
        {
            return $"Browser_{Guid.NewGuid():N}";
        }

        // ── Работа с состоянием элементов ──────────────────────────────

        /// <summary>
        /// Проверяет видимость элемента безопасным способом.
        /// </summary>
        /// <param name="element">Элемент для проверки</param>
        /// <returns>true если элемент видим, иначе false</returns>
        public static bool IsElementVisible(IWebElement element)
        {
            try
            {
                return element.Displayed;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        /// <summary>
        /// Проверяет включённость элемента безопасным способом.
        /// </summary>
        /// <param name="element">Элемент для проверки</param>
        /// <returns>true если элемент включён, иначе false</returns>
        public static bool IsElementEnabled(IWebElement element)
        {
            try
            {
                return element.Enabled;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        /// <summary>
        /// Проверяет выбранность элемента (для checkbox, radio) безопасным способом.
        /// </summary>
        /// <param name="element">Элемент для проверки</param>
        /// <returns>true если элемент выбран, иначе false</returns>
        public static bool IsElementSelected(IWebElement element)
        {
            try
            {
                return element.Selected;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        // ── Расширенные действия с элементами (Actions API) ────────────

        /// <summary>
        /// Выполняет двойной клик по элементу используя Actions API.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="element">Элемент для двойного клика</param>
        public static void DoubleClick(IWebDriver driver, IWebElement element)
        {
            var actions = new Actions(driver);
            actions.DoubleClick(element).Perform();
        }

        /// <summary>
        /// Выполняет двойной клик по элементу используя JavaScript.
        /// Полезно для элементов которые не реагируют на стандартный двойной клик.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="element">Элемент для двойного клика</param>
        public static void DoubleClickJS(IWebDriver driver, IWebElement element)
        {
            var script = @"
                var event = new MouseEvent('dblclick', {
                    bubbles: true,
                    cancelable: true,
                    view: window
                });
                arguments[0].dispatchEvent(event);
            ";
            ExecuteJavaScript(driver, script, element);
        }

        /// <summary>
        /// Выполняет правый клик (контекстное меню) по элементу.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="element">Элемент для правого клика</param>
        public static void RightClick(IWebDriver driver, IWebElement element)
        {
            var actions = new Actions(driver);
            actions.ContextClick(element).Perform();
        }

        /// <summary>
        /// Нажимает и удерживает кнопку мыши на элементе указанное время.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="element">Элемент для клика</param>
        /// <param name="durationMs">Длительность удержания в миллисекундах</param>
        public static void ClickAndHold(IWebDriver driver, IWebElement element, int durationMs)
        {
            var actions = new Actions(driver);
            actions.ClickAndHold(element).Perform();
            
            if (durationMs > 0)
            {
                System.Threading.Thread.Sleep(durationMs);
            }
            
            actions.Release().Perform();
        }

        /// <summary>
        /// Отпускает кнопку мыши (используется после ClickAndHold без автоматического Release).
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        public static void ReleaseClick(IWebDriver driver)
        {
            var actions = new Actions(driver);
            actions.Release().Perform();
        }

        // ── Drag and Drop ──────────────────────────────────────────────

        /// <summary>
        /// Перетаскивает элемент из исходной позиции в целевую.
        /// Использует Actions API для эмуляции drag and drop.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="source">Исходный элемент для перетаскивания</param>
        /// <param name="target">Целевой элемент куда перетащить</param>
        public static void DragAndDrop(IWebDriver driver, IWebElement source, IWebElement target)
        {
            var actions = new Actions(driver);
            actions.DragAndDrop(source, target).Perform();
        }

        /// <summary>
        /// Перетаскивает элемент на указанное смещение от его текущей позиции.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="source">Исходный элемент для перетаскивания</param>
        /// <param name="offsetX">Смещение по оси X в пикселях</param>
        /// <param name="offsetY">Смещение по оси Y в пикселях</param>
        public static void DragAndDropByOffset(IWebDriver driver, IWebElement source, int offsetX, int offsetY)
        {
            var actions = new Actions(driver);
            actions.DragAndDropToOffset(source, offsetX, offsetY).Perform();
        }

        /// <summary>
        /// Перетаскивает элемент используя JavaScript.
        /// Полезно для сложных случаев когда стандартный drag and drop не работает.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="source">Исходный элемент для перетаскивания</param>
        /// <param name="target">Целевой элемент куда перетащить</param>
        public static void DragAndDropJS(IWebDriver driver, IWebElement source, IWebElement target)
        {
            var script = @"
                function createEvent(typeOfEvent) {
                    var event = document.createEvent('CustomEvent');
                    event.initCustomEvent(typeOfEvent, true, true, null);
                    event.dataTransfer = {
                        data: {},
                        setData: function(key, value) { this.data[key] = value; },
                        getData: function(key) { return this.data[key]; }
                    };
                    return event;
                }
                
                function dispatchEvent(element, event, transferData) {
                    if (transferData !== undefined) {
                        event.dataTransfer = transferData;
                    }
                    if (element.dispatchEvent) {
                        element.dispatchEvent(event);
                    } else if (element.fireEvent) {
                        element.fireEvent('on' + event.type, event);
                    }
                }
                
                var source = arguments[0];
                var target = arguments[1];
                
                var dragStartEvent = createEvent('dragstart');
                dispatchEvent(source, dragStartEvent);
                
                var dropEvent = createEvent('drop');
                dispatchEvent(target, dropEvent, dragStartEvent.dataTransfer);
                
                var dragEndEvent = createEvent('dragend');
                dispatchEvent(source, dragEndEvent, dragStartEvent.dataTransfer);
            ";
            
            ExecuteJavaScript(driver, script, source, target);
        }

        // ── Управление окном браузера ──────────────────────────────────

        /// <summary>
        /// Разворачивает окно браузера на весь экран.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        public static void MaximizeWindow(IWebDriver driver)
        {
            driver.Manage().Window.Maximize();
        }

        /// <summary>
        /// Сворачивает окно браузера.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        public static void MinimizeWindow(IWebDriver driver)
        {
            driver.Manage().Window.Minimize();
        }

        /// <summary>
        /// Переводит браузер в полноэкранный режим (F11).
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        public static void FullScreenWindow(IWebDriver driver)
        {
            driver.Manage().Window.FullScreen();
        }

        /// <summary>
        /// Устанавливает размер окна браузера.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="width">Ширина окна в пикселях</param>
        /// <param name="height">Высота окна в пикселях</param>
        public static void SetWindowSize(IWebDriver driver, int width, int height)
        {
            driver.Manage().Window.Size = new System.Drawing.Size(width, height);
        }

        /// <summary>
        /// Устанавливает позицию окна браузера.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="x">Координата X левого верхнего угла</param>
        /// <param name="y">Координата Y левого верхнего угла</param>
        public static void SetWindowPosition(IWebDriver driver, int x, int y)
        {
            driver.Manage().Window.Position = new System.Drawing.Point(x, y);
        }

        /// <summary>
        /// Получает текущий размер окна браузера.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <returns>Кортеж (ширина, высота)</returns>
        public static (int width, int height) GetWindowSize(IWebDriver driver)
        {
            var size = driver.Manage().Window.Size;
            return (size.Width, size.Height);
        }

        /// <summary>
        /// Получает текущую позицию окна браузера.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <returns>Кортеж (x, y)</returns>
        public static (int x, int y) GetWindowPosition(IWebDriver driver)
        {
            var position = driver.Manage().Window.Position;
            return (position.X, position.Y);
        }

        // ── Управление вкладками ───────────────────────────────────────

        /// <summary>
        /// Открывает новую вкладку браузера.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="url">URL для открытия (опционально)</param>
        public static void OpenNewTab(IWebDriver driver, string url = "")
        {
            var jsExecutor = (IJavaScriptExecutor)driver;
            jsExecutor.ExecuteScript("window.open();");
            
            // Переключаемся на новую вкладку
            var handles = driver.WindowHandles;
            driver.SwitchTo().Window(handles[handles.Count - 1]);
            
            // Если указан URL, переходим на него
            if (!string.IsNullOrWhiteSpace(url))
            {
                driver.Navigate().GoToUrl(url);
            }
        }

        /// <summary>
        /// Закрывает текущую вкладку браузера.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        public static void CloseCurrentTab(IWebDriver driver)
        {
            driver.Close();
        }

        /// <summary>
        /// Закрывает вкладку по её handle.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="handle">Handle вкладки для закрытия</param>
        public static void CloseTabByHandle(IWebDriver driver, string handle)
        {
            var currentHandle = driver.CurrentWindowHandle;
            driver.SwitchTo().Window(handle);
            driver.Close();
            
            // Возвращаемся на предыдущую вкладку если она ещё существует
            var handles = driver.WindowHandles;
            if (handles.Count > 0)
            {
                if (handles.Contains(currentHandle))
                {
                    driver.SwitchTo().Window(currentHandle);
                }
                else
                {
                    driver.SwitchTo().Window(handles[0]);
                }
            }
        }

        /// <summary>
        /// Получает список всех handles открытых вкладок.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <returns>Список handles</returns>
        public static List<string> GetAllWindowHandles(IWebDriver driver)
        {
            return driver.WindowHandles.ToList();
        }

        /// <summary>
        /// Получает handle текущей активной вкладки.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <returns>Handle текущей вкладки</returns>
        public static string GetCurrentWindowHandle(IWebDriver driver)
        {
            return driver.CurrentWindowHandle;
        }

        /// <summary>
        /// Переключается на вкладку по индексу.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="index">Индекс вкладки (начиная с 0)</param>
        public static void SwitchToTabByIndex(IWebDriver driver, int index)
        {
            var handles = driver.WindowHandles;
            if (index < 0 || index >= handles.Count)
            {
                throw new ArgumentException($"Индекс вкладки {index} вне диапазона. Доступно вкладок: {handles.Count}");
            }
            driver.SwitchTo().Window(handles[index]);
        }

        // ── Работа с файлами и элементами ──────────────────────────────────────

        /// <summary>
        /// Загружает файл через input[type=file] элемент.
        /// </summary>
        /// <param name="fileInput">Элемент input[type=file]</param>
        /// <param name="filePath">Полный путь к файлу для загрузки</param>
        public static void UploadFile(IWebElement fileInput, string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                throw new System.IO.FileNotFoundException($"Файл не найден: {filePath}");

            fileInput.SendKeys(filePath);
        }

        /// <summary>
        /// Проверяет является ли элемент input[type=file].
        /// </summary>
        /// <param name="element">Элемент для проверки</param>
        /// <returns>true если это file input, иначе false</returns>
        public static bool IsFileInputElement(IWebElement element)
        {
            try
            {
                var tagName = element.TagName?.ToLower();
                var type = element.GetAttribute("type")?.ToLower();
                return tagName == "input" && type == "file";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Получает координаты и размеры элемента на странице.
        /// </summary>
        /// <param name="element">Элемент для получения размеров</param>
        /// <returns>Кортеж (x, y, width, height)</returns>
        public static (int x, int y, int width, int height) GetElementRect(IWebElement element)
        {
            var location = element.Location;
            var size = element.Size;
            return (location.X, location.Y, size.Width, size.Height);
        }

        /// <summary>
        /// Создаёт скриншот конкретного элемента и возвращает в формате Base64.
        /// </summary>
        /// <param name="element">Элемент для скриншота</param>
        /// <returns>Скриншот в формате Base64 строки</returns>
        public static string TakeElementScreenshotBase64(IWebElement element)
        {
            var screenshot = ((ITakesScreenshot)element).GetScreenshot();
            return screenshot.AsBase64EncodedString;
        }

        /// <summary>
        /// Создаёт скриншот конкретного элемента и сохраняет в файл.
        /// </summary>
        /// <param name="element">Элемент для скриншота</param>
        /// <param name="filePath">Путь для сохранения файла</param>
        public static void TakeElementScreenshotToFile(IWebElement element, string filePath)
        {
            var screenshot = ((ITakesScreenshot)element).GetScreenshot();
            screenshot.SaveAsFile(filePath);
        }

        /// <summary>
        /// Наводит курсор на элемент с указанным смещением от центра.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="element">Элемент для наведения</param>
        /// <param name="offsetX">Смещение по X от центра элемента</param>
        /// <param name="offsetY">Смещение по Y от центра элемента</param>
        public static void HoverWithOffset(IWebDriver driver, IWebElement element, int offsetX, int offsetY)
        {
            var actions = new Actions(driver);
            actions.MoveToElement(element, offsetX, offsetY).Perform();
        }

        // ── Работа с Web Storage (localStorage/sessionStorage) ─────────

        /// <summary>
        /// Получает значение из хранилища по ключу.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="storageType">Тип хранилища</param>
        /// <param name="key">Ключ</param>
        /// <returns>Значение или null если ключ не найден</returns>
        public static string GetStorageItem(IWebDriver driver, StorageType storageType, string key)
        {
            var storageName = storageType == StorageType.LocalStorage ? "localStorage" : "sessionStorage";
            var script = $"return {storageName}.getItem(arguments[0]);";
            var jsExecutor = (IJavaScriptExecutor)driver;
            return jsExecutor.ExecuteScript(script, key) as string;
        }

        /// <summary>
        /// Устанавливает значение в хранилище.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="storageType">Тип хранилища</param>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение</param>
        public static void SetStorageItem(IWebDriver driver, StorageType storageType, string key, string value)
        {
            var storageName = storageType == StorageType.LocalStorage ? "localStorage" : "sessionStorage";
            var script = $"{storageName}.setItem(arguments[0], arguments[1]);";
            var jsExecutor = (IJavaScriptExecutor)driver;
            jsExecutor.ExecuteScript(script, key, value);
        }

        /// <summary>
        /// Удаляет ключ из хранилища.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="storageType">Тип хранилища</param>
        /// <param name="key">Ключ для удаления</param>
        public static void RemoveStorageItem(IWebDriver driver, StorageType storageType, string key)
        {
            var storageName = storageType == StorageType.LocalStorage ? "localStorage" : "sessionStorage";
            var script = $"{storageName}.removeItem(arguments[0]);";
            var jsExecutor = (IJavaScriptExecutor)driver;
            jsExecutor.ExecuteScript(script, key);
        }

        /// <summary>
        /// Очищает всё хранилище.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="storageType">Тип хранилища</param>
        public static void ClearStorage(IWebDriver driver, StorageType storageType)
        {
            var storageName = storageType == StorageType.LocalStorage ? "localStorage" : "sessionStorage";
            var script = $"{storageName}.clear();";
            var jsExecutor = (IJavaScriptExecutor)driver;
            jsExecutor.ExecuteScript(script);
        }

        /// <summary>
        /// Получает все ключи из хранилища.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="storageType">Тип хранилища</param>
        /// <returns>Список ключей</returns>
        public static List<string> GetStorageKeys(IWebDriver driver, StorageType storageType)
        {
            var storageName = storageType == StorageType.LocalStorage ? "localStorage" : "sessionStorage";
            var script = $"return Object.keys({storageName});";
            var jsExecutor = (IJavaScriptExecutor)driver;
            var result = jsExecutor.ExecuteScript(script);
            
            if (result is System.Collections.IEnumerable enumerable)
            {
                return enumerable.Cast<object>().Select(x => x?.ToString() ?? string.Empty).ToList();
            }
            
            return new List<string>();
        }

        /// <summary>
        /// Получает количество элементов в хранилище.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="storageType">Тип хранилища</param>
        /// <returns>Количество элементов</returns>
        public static int GetStorageLength(IWebDriver driver, StorageType storageType)
        {
            var storageName = storageType == StorageType.LocalStorage ? "localStorage" : "sessionStorage";
            var script = $"return {storageName}.length;";
            var jsExecutor = (IJavaScriptExecutor)driver;
            var result = jsExecutor.ExecuteScript(script);
            
            if (result is long longValue)
                return (int)longValue;
            if (result is int intValue)
                return intValue;
                
            return 0;
        }

        // ── Работа с CSS стилями ───────────────────────────────────────

        /// <summary>
        /// Получает вычисленное значение CSS свойства элемента.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="element">Элемент для получения стиля</param>
        /// <param name="propertyName">Имя CSS свойства (например, "color", "font-size")</param>
        /// <returns>Значение CSS свойства</returns>
        public static string GetComputedStyle(IWebDriver driver, IWebElement element, string propertyName)
        {
            var script = "return window.getComputedStyle(arguments[0]).getPropertyValue(arguments[1]);";
            var jsExecutor = (IJavaScriptExecutor)driver;
            var result = jsExecutor.ExecuteScript(script, element, propertyName);
            return result?.ToString() ?? string.Empty;
        }

        // ── Работа с логами браузера ──────────────────────────────────────────────────

        /// <summary>
        /// Получает логи браузера указанного типа.
        /// </summary>
        /// <param name="driver">Экземпляр WebDriver</param>
        /// <param name="logType">Тип логов</param>
        /// <returns>Список логов</returns>
        public static List<string> GetBrowserLogs(IWebDriver driver, BrowserLogType logType)
        {
            var logs = new List<string>();
            
            try
            {
                var logTypeName = logType.ToString().ToLower();
                var logEntries = driver.Manage().Logs.GetLog(logTypeName);
                
                logs = logEntries
                    .Select(entry => $"[{entry.Timestamp}] [{entry.Level}] {entry.Message}")
                    .ToList();
            }
            catch (Exception ex)
            {
                // Некоторые браузеры не поддерживают все типы логов
                logs.Add($"Ошибка получения логов: {ex.Message}");
            }
            
            return logs;
        }

        // ── Работа с multiple select ──────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Выбирает опцию в multiple select по тексту.
        /// </summary>
        /// <param name="selectElement">Select элемент</param>
        /// <param name="text">Текст опции</param>
        public static void SelectMultipleByText(IWebElement selectElement, string text)
        {
            var select = new OpenQA.Selenium.Support.UI.SelectElement(selectElement);
            select.SelectByText(text);
        }

        /// <summary>
        /// Выбирает опцию в multiple select по value.
        /// </summary>
        /// <param name="selectElement">Select элемент</param>
        /// <param name="value">Value опции</param>
        public static void SelectMultipleByValue(IWebElement selectElement, string value)
        {
            var select = new OpenQA.Selenium.Support.UI.SelectElement(selectElement);
            select.SelectByValue(value);
        }

        /// <summary>
        /// Выбирает опцию в multiple select по индексу.
        /// </summary>
        /// <param name="selectElement">Select элемент</param>
        /// <param name="index">Индекс опции</param>
        public static void SelectMultipleByIndex(IWebElement selectElement, int index)
        {
            var select = new OpenQA.Selenium.Support.UI.SelectElement(selectElement);
            select.SelectByIndex(index);
        }

        /// <summary>
        /// Снимает выбор опции по тексту.
        /// </summary>
        /// <param name="selectElement">Select элемент</param>
        /// <param name="text">Текст опции</param>
        public static void DeselectByText(IWebElement selectElement, string text)
        {
            var select = new OpenQA.Selenium.Support.UI.SelectElement(selectElement);
            select.DeselectByText(text);
        }

        /// <summary>
        /// Снимает выбор опции по value.
        /// </summary>
        /// <param name="selectElement">Select элемент</param>
        /// <param name="value">Value опции</param>
        public static void DeselectByValue(IWebElement selectElement, string value)
        {
            var select = new OpenQA.Selenium.Support.UI.SelectElement(selectElement);
            select.DeselectByValue(value);
        }

        /// <summary>
        /// Снимает выбор опции по индексу.
        /// </summary>
        /// <param name="selectElement">Select элемент</param>
        /// <param name="index">Индекс опции</param>
        public static void DeselectByIndex(IWebElement selectElement, int index)
        {
            var select = new OpenQA.Selenium.Support.UI.SelectElement(selectElement);
            select.DeselectByIndex(index);
        }

        /// <summary>
        /// Снимает все выборы в multiple select.
        /// </summary>
        /// <param name="selectElement">Select элемент</param>
        public static void DeselectAll(IWebElement selectElement)
        {
            var select = new OpenQA.Selenium.Support.UI.SelectElement(selectElement);
            select.DeselectAll();
        }

        /// <summary>
        /// Получает все опции из select элемента.
        /// </summary>
        /// <param name="selectElement">Select элемент</param>
        /// <returns>Список текстов всех опций</returns>
        public static List<string> GetAllSelectOptions(IWebElement selectElement)
        {
            var select = new OpenQA.Selenium.Support.UI.SelectElement(selectElement);
            return select.Options.Select(o => o.Text).ToList();
        }

        /// <summary>
        /// Получает выбранные опции из select элемента.
        /// </summary>
        /// <param name="selectElement">Select элемент</param>
        /// <returns>Список текстов выбранных опций</returns>
        public static List<string> GetSelectedSelectOptions(IWebElement selectElement)
        {
            var select = new OpenQA.Selenium.Support.UI.SelectElement(selectElement);
            return select.AllSelectedOptions.Select(o => o.Text).ToList();
        }
    }
}
