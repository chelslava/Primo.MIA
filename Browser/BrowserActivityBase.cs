using System;
using System.Collections.Generic;
using LTools.SDK;
using LTools.Common.UIElements;
using OpenQA.Selenium;
using Primo.MIA.Common;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Базовый класс для всех активностей работы с браузером.
    /// Предоставляет общую функциональность для работы с WebDriver и элементами.
    /// </summary>
    /// <typeparam name="TView">Тип представления активности (XAML)</typeparam>
    public abstract class BrowserActivityBase<TView> : PrimoComponentTO<TView>
        where TView : UserControl, new()
    {
        #region Services

        /// <summary>
        /// Менеджер сессий браузера.
        /// </summary>
        protected ISessionManager SessionManager => BrowserServicesFactory.SessionManager;

        /// <summary>
        /// Репозиторий элементов.
        /// </summary>
        protected IElementRepository ElementRepository => BrowserServicesFactory.ElementRepository;

        /// <summary>
        /// Логгер активностей.
        /// </summary>
        protected IActivityLogger Logger => BrowserServicesFactory.ActivityLogger;

        /// <summary>
        /// Локатор элементов (создаётся новый экземпляр при каждом обращении).
        /// </summary>
        protected IElementLocator ElementLocator => BrowserServicesFactory.CreateElementLocator();

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор базового класса.
        /// </summary>
        protected BrowserActivityBase(IWFContainer container) : base(container)
        {
        }

        #endregion

        #region Driver Management

        /// <summary>
        /// Получает WebDriver из контекста, используя SessionResolver для разрешения sessionId.
        /// </summary>
        /// <param name="sessionId">ID сессии браузера</param>
        /// <returns>Экземпляр WebDriver</returns>
        /// <exception cref="InvalidOperationException">Если драйвер не найден</exception>
        protected IWebDriver GetDriverFromContext(string sessionId)
        {
            string resolvedSessionId = SessionResolver.Resolve(sessionId);

            IWebDriver driver = SeleniumHelper.GetDriver(resolvedSessionId);

            if (driver == null)
            {
                throw new InvalidOperationException(
                    $"WebDriver не найден для сессии '{resolvedSessionId}'. " +
                    $"Убедитесь что браузер был открыт с этим sessionId.");
            }

            return driver;
        }

        /// <summary>
        /// Безопасно получает WebDriver, возвращая null если драйвер не найден.
        /// </summary>
        protected IWebDriver TryGetDriverFromContext(string sessionId)
        {
            try
            {
                string resolvedSessionId = SessionResolver.Resolve(sessionId);
                return SeleniumHelper.GetDriver(resolvedSessionId);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Element Location

        /// <summary>
        /// Находит элемент на странице с ожиданием.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="locatorType">Тип локатора</param>
        /// <param name="locatorValue">Значение локатора</param>
        /// <param name="timeout">Таймаут ожидания в миллисекундах</param>
        /// <returns>Найденный элемент</returns>
        /// <exception cref="NoSuchElementException">Если элемент не найден</exception>
        protected IWebElement FindElement(
            IWebDriver driver,
            ElementLocatorType locatorType,
            string locatorValue,
            int timeout)
        {
            if (string.IsNullOrWhiteSpace(locatorValue))
                throw new ArgumentException("Значение локатора не может быть пустым", nameof(locatorValue));

            By locator = SeleniumHelper.CreateLocator(locatorType, locatorValue);
            return SeleniumHelper.WaitForElement(driver, locator, timeout);
        }

        /// <summary>
        /// Безопасно находит элемент, возвращая null если элемент не найден.
        /// </summary>
        protected IWebElement TryFindElement(
            IWebDriver driver,
            ElementLocatorType locatorType,
            string locatorValue,
            int timeout)
        {
            try
            {
                return FindElement(driver, locatorType, locatorValue, timeout);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
            catch (WebDriverTimeoutException)
            {
                return null;
            }
        }

        /// <summary>
        /// Находит все элементы по локатору.
        /// </summary>
        protected System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElements(
            IWebDriver driver,
            ElementLocatorType locatorType,
            string locatorValue)
        {
            if (string.IsNullOrWhiteSpace(locatorValue))
                throw new ArgumentException("Значение локатора не может быть пустым", nameof(locatorValue));

            By locator = SeleniumHelper.CreateLocator(locatorType, locatorValue);
            return driver.FindElements(locator);
        }

        #endregion

        #region Result Creation

        /// <summary>
        /// Создает успешный результат выполнения.
        /// </summary>
        protected ExecutionResult CreateSuccessResult(string message = null)
        {
            return new ExecutionResult
            {
                IsSuccess = true,
                SuccessMessage = message ?? "Операция выполнена успешно"
            };
        }

        /// <summary>
        /// Создает результат с ошибкой.
        /// </summary>
        protected ExecutionResult CreateErrorResult(string errorMessage)
        {
            return new ExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// Создает результат с ошибкой из исключения.
        /// </summary>
        protected ExecutionResult CreateErrorResult(Exception ex, string context = null)
        {
            string message = context != null
                ? $"Ошибка [{context}]: {ex.Message}"
                : $"Ошибка: {ex.Message}";

            return new ExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = message
            };
        }

        #endregion

        #region Validation

        /// <summary>
        /// Валидирует что строка не пустая.
        /// </summary>
        protected void ValidateNotEmpty(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Параметр '{parameterName}' не может быть пустым", parameterName);
        }

        /// <summary>
        /// Валидирует что значение положительное.
        /// </summary>
        protected void ValidatePositive(int value, string parameterName)
        {
            if (value <= 0)
                throw new ArgumentException($"Параметр '{parameterName}' должен быть положительным", parameterName);
        }

        /// <summary>
        /// Валидирует URL.
        /// </summary>
        protected void ValidateUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL не может быть пустым");

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                throw new ArgumentException($"Некорректный URL: {url}");

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Поддерживаются только HTTP и HTTPS протоколы");
        }

        #endregion

        #region Safe Execution

        /// <summary>
        /// Безопасно выполняет действие с обработкой исключений.
        /// </summary>
        protected ExecutionResult SafeExecute(Action action, string context = null)
        {
            try
            {
                action();
                return CreateSuccessResult();
            }
            catch (Exception ex)
            {
                return CreateErrorResult(ex, context);
            }
        }

        /// <summary>
        /// Безопасно выполняет функцию с обработкой исключений.
        /// </summary>
        protected ExecutionResult SafeExecute<T>(Func<T> func, Action<T> onSuccess, string context = null)
        {
            try
            {
                T result = func();
                onSuccess?.Invoke(result);
                return CreateSuccessResult();
            }
            catch (Exception ex)
            {
                return CreateErrorResult(ex, context);
            }
        }

        /// <summary>
        /// Безопасно выполняет функцию, возвращающую ExecutionResult.
        /// </summary>
        protected ExecutionResult SafeExecute(Func<ExecutionResult> func, string context = null)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                return CreateErrorResult(ex, context);
            }
        }

        #endregion

        #region Logging Helpers

        /// <summary>
        /// Логирует информационное сообщение.
        /// </summary>
        protected void LogInfo(string message)
        {
            // TODO: Implement structured logging
            System.Diagnostics.Debug.WriteLine($"[INFO] {GetType().Name}: {message}");
        }

        /// <summary>
        /// Логирует предупреждение.
        /// </summary>
        protected void LogWarning(string message)
        {
            // TODO: Implement structured logging
            System.Diagnostics.Debug.WriteLine($"[WARN] {GetType().Name}: {message}");
        }

        /// <summary>
        /// Логирует ошибку.
        /// </summary>
        protected void LogError(string message, Exception ex = null)
        {
            // TODO: Implement structured logging
            string logMessage = ex != null
                ? $"[ERROR] {GetType().Name}: {message} - {ex.Message}"
                : $"[ERROR] {GetType().Name}: {message}";

            System.Diagnostics.Debug.WriteLine(logMessage);
        }

        #endregion

        #region Element Locator Conversion

        /// <summary>
        /// Преобразует ElementLocatorType в LocatorType для использования с сервисами.
        /// </summary>
        protected LocatorType ConvertLocatorType(ElementLocatorType elementLocatorType)
        {
            switch (elementLocatorType)
            {
                case ElementLocatorType.Id:
                    return LocatorType.Id;
                case ElementLocatorType.Name:
                    return LocatorType.Name;
                case ElementLocatorType.ClassName:
                    return LocatorType.ClassName;
                case ElementLocatorType.TagName:
                    return LocatorType.TagName;
                case ElementLocatorType.LinkText:
                    return LocatorType.LinkText;
                case ElementLocatorType.PartialLinkText:
                    return LocatorType.PartialLinkText;
                case ElementLocatorType.CssSelector:
                    return LocatorType.CssSelector;
                case ElementLocatorType.XPath:
                    return LocatorType.XPath;
                default:
                    throw new ArgumentException($"Неподдерживаемый тип локатора: {elementLocatorType}");
            }
        }

        #endregion

        #region Phase 4: Extended Features

        #region Retry Mechanism

        /// <summary>
        /// Находит элемент с автоматическими повторными попытками.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="locatorType">Тип локатора</param>
        /// <param name="locatorValue">Значение локатора</param>
        /// <param name="timeout">Таймаут ожидания в миллисекундах</param>
        /// <param name="retryConfig">Конфигурация retry (null = использовать по умолчанию)</param>
        /// <returns>Найденный элемент</returns>
        protected IWebElement FindElementWithRetry(
            IWebDriver driver,
            ElementLocatorType locatorType,
            string locatorValue,
            int timeout,
            Primo.MIA.Models.RetryConfiguration retryConfig = null)
        {
            retryConfig = retryConfig ?? Primo.MIA.Models.RetryConfiguration.Default;

            int attempt = 0;
            Exception lastException = null;

            while (attempt < retryConfig.MaxRetries)
            {
                try
                {
                    attempt++;
                    LogInfo($"Попытка {attempt}/{retryConfig.MaxRetries} поиска элемента [{locatorType}={locatorValue}]");

                    IWebElement element = FindElement(driver, locatorType, locatorValue, timeout);

                    if (attempt > 1)
                    {
                        LogInfo($"Элемент найден после {attempt} попыток");
                    }

                    return element;
                }
                catch (Exception ex) when (IsRetryableException(ex, retryConfig))
                {
                    lastException = ex;
                    LogWarning($"Попытка {attempt} не удалась: {ex.Message}");

                    if (attempt < retryConfig.MaxRetries)
                    {
                        int delay = retryConfig.ExponentialBackoff
                            ? retryConfig.RetryDelayMs * (int)Math.Pow(2, attempt - 1)
                            : retryConfig.RetryDelayMs;

                        LogInfo($"Ожидание {delay}ms перед следующей попыткой");
                        System.Threading.Thread.Sleep(delay);
                    }
                }
            }

            string errorMsg = $"Элемент не найден после {retryConfig.MaxRetries} попыток. " +
                            $"Последняя ошибка: {lastException?.Message}";
            throw new NoSuchElementException(errorMsg, lastException);
        }

        /// <summary>
        /// Проверяет, является ли исключение подходящим для retry.
        /// </summary>
        private bool IsRetryableException(Exception ex, Primo.MIA.Models.RetryConfiguration config)
        {
            Type exType = ex.GetType();
            foreach (Type retryableType in config.RetryableExceptions)
            {
                if (retryableType.IsAssignableFrom(exType))
                    return true;
            }
            return false;
        }

        #endregion

        #region Shadow DOM Support

        /// <summary>
        /// Находит элемент внутри Shadow Root.
        /// </summary>
        /// <param name="shadowHost">Элемент-хост Shadow DOM</param>
        /// <param name="locatorType">Тип локатора</param>
        /// <param name="locatorValue">Значение локатора</param>
        /// <returns>Найденный элемент в Shadow DOM</returns>
        protected IWebElement FindElementInShadowRoot(
            IWebElement shadowHost,
            ElementLocatorType locatorType,
            string locatorValue)
        {
            if (shadowHost == null)
                throw new ArgumentNullException(nameof(shadowHost));

            ValidateNotEmpty(locatorValue, nameof(locatorValue));

            try
            {
                IJavaScriptExecutor js = shadowHost as IJavaScriptExecutor;
                if (js == null && shadowHost is IWrapsDriver wrapsDriver)
                {
                    js = wrapsDriver.WrappedDriver as IJavaScriptExecutor;
                }

                if (js == null)
                    throw new InvalidOperationException("WebDriver не поддерживает JavaScript");

                // Получаем Shadow Root
                IWebElement shadowRoot = js.ExecuteScript("return arguments[0].shadowRoot", shadowHost) as IWebElement;

                if (shadowRoot == null)
                    throw new InvalidOperationException("Элемент не содержит Shadow Root");

                // Ищем элемент внутри Shadow Root
                By locator = SeleniumHelper.CreateLocator(locatorType, locatorValue);
                return shadowRoot.FindElement(locator);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка поиска в Shadow DOM: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Находит элемент через цепочку Shadow DOM.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="shadowPath">Путь через Shadow DOM (массив локаторов для каждого уровня)</param>
        /// <param name="finalLocatorType">Тип финального локатора</param>
        /// <param name="finalLocatorValue">Значение финального локатора</param>
        /// <returns>Найденный элемент</returns>
        protected IWebElement FindElementInShadowChain(
            IWebDriver driver,
            string[] shadowPath,
            ElementLocatorType finalLocatorType,
            string finalLocatorValue)
        {
            if (shadowPath == null || shadowPath.Length == 0)
                throw new ArgumentException("Shadow path не может быть пустым", nameof(shadowPath));

            IJavaScriptExecutor js = driver as IJavaScriptExecutor;
            if (js == null)
                throw new InvalidOperationException("WebDriver не поддерживает JavaScript");

            try
            {
                // Строим JavaScript для прохода по цепочке Shadow DOM
                string script = "let element = document;";

                for (int i = 0; i < shadowPath.Length; i++)
                {
                    script += $"\nelement = element.querySelector('{shadowPath[i]}').shadowRoot;";
                }

                script += $"\nreturn element.querySelector('{finalLocatorValue}');";

                LogInfo($"Поиск через Shadow DOM цепочку: {string.Join(" -> ", shadowPath)} -> {finalLocatorValue}");

                IWebElement element = js.ExecuteScript(script) as IWebElement;

                if (element == null)
                    throw new NoSuchElementException($"Элемент не найден в Shadow DOM цепочке");

                return element;
            }
            catch (Exception ex)
            {
                LogError($"Ошибка поиска в Shadow DOM цепочке: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region iframe Auto-handling

        /// <summary>
        /// Находит элемент с автоматическим обнаружением iframe.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="locatorType">Тип локатора</param>
        /// <param name="locatorValue">Значение локатора</param>
        /// <param name="timeout">Таймаут ожидания в миллисекундах</param>
        /// <returns>Найденный элемент</returns>
        protected IWebElement FindElementWithIframeDetection(
            IWebDriver driver,
            ElementLocatorType locatorType,
            string locatorValue,
            int timeout)
        {
            // Сначала пробуем найти в текущем контексте
            IWebElement element = TryFindElement(driver, locatorType, locatorValue, timeout);
            if (element != null)
                return element;

            LogInfo("Элемент не найден в текущем контексте, ищем в iframe");

            // Ищем во всех iframe
            var iframes = driver.FindElements(By.TagName("iframe"));
            LogInfo($"Найдено {iframes.Count} iframe на странице");

            foreach (var iframe in iframes)
            {
                try
                {
                    driver.SwitchTo().Frame(iframe);
                    LogInfo($"Переключились в iframe");

                    element = TryFindElement(driver, locatorType, locatorValue, timeout);
                    if (element != null)
                    {
                        LogInfo("Элемент найден в iframe");
                        return element;
                    }

                    // Возвращаемся в основной контекст
                    driver.SwitchTo().DefaultContent();
                }
                catch (Exception ex)
                {
                    LogWarning($"Ошибка при поиске в iframe: {ex.Message}");
                    driver.SwitchTo().DefaultContent();
                }
            }

            // Элемент не найден ни в одном iframe
            throw new NoSuchElementException(
                $"Элемент [{locatorType}={locatorValue}] не найден ни в основном контексте, ни в {iframes.Count} iframe");
        }

        #endregion

        #region Screenshot on Error

        /// <summary>
        /// Создает скриншот при ошибке.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="activityName">Имя активности</param>
        /// <param name="error">Исключение</param>
        /// <returns>Путь к файлу скриншота</returns>
        protected string CaptureErrorScreenshot(
            IWebDriver driver,
            string activityName,
            Exception error)
        {
            try
            {
                ITakesScreenshot screenshotDriver = driver as ITakesScreenshot;
                if (screenshotDriver == null)
                {
                    LogWarning("WebDriver не поддерживает создание скриншотов");
                    return null;
                }

                // Создаем директорию для скриншотов
                string screenshotDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Primo.MIA",
                    "Screenshots");

                if (!System.IO.Directory.Exists(screenshotDir))
                {
                    System.IO.Directory.CreateDirectory(screenshotDir);
                }

                // Формируем имя файла
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string filename = $"{timestamp}_{activityName}.png";
                string filepath = System.IO.Path.Combine(screenshotDir, filename);

                // Создаем скриншот
                Screenshot screenshot = screenshotDriver.GetScreenshot();
                screenshot.SaveAsFile(filepath);

                LogInfo($"Скриншот сохранен: {filepath}");
                return filepath;
            }
            catch (Exception ex)
            {
                LogError($"Ошибка создания скриншота: {ex.Message}", ex);
                return null;
            }
        }

        #endregion

        #region Extended Selectors

        /// <summary>
        /// Находит элемент по тексту.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="text">Текст для поиска</param>
        /// <param name="exactMatch">Точное совпадение (true) или частичное (false)</param>
        /// <returns>Найденный элемент</returns>
        protected IWebElement FindByText(
            IWebDriver driver,
            string text,
            bool exactMatch = false)
        {
            ValidateNotEmpty(text, nameof(text));

            string xpath = exactMatch
                ? $"//*[text()='{text}']"
                : $"//*[contains(text(), '{text}')]";

            LogInfo($"Поиск элемента по тексту: '{text}' (точное совпадение: {exactMatch})");

            try
            {
                return driver.FindElement(By.XPath(xpath));
            }
            catch (NoSuchElementException)
            {
                throw new NoSuchElementException(
                    $"Элемент с текстом '{text}' не найден (точное совпадение: {exactMatch})");
            }
        }

        /// <summary>
        /// Находит элемент по позиции в коллекции.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="locatorType">Тип локатора</param>
        /// <param name="locatorValue">Значение локатора</param>
        /// <param name="position">Позиция элемента</param>
        /// <param name="index">Индекс для позиции Nth (начиная с 0)</param>
        /// <returns>Найденный элемент</returns>
        protected IWebElement FindByPosition(
            IWebDriver driver,
            ElementLocatorType locatorType,
            string locatorValue,
            Primo.MIA.Models.ElementPosition position,
            int index = 0)
        {
            ValidateNotEmpty(locatorValue, nameof(locatorValue));

            var elements = FindElements(driver, locatorType, locatorValue);

            if (elements.Count == 0)
                throw new NoSuchElementException($"Элементы [{locatorType}={locatorValue}] не найдены");

            switch (position)
            {
                case Primo.MIA.Models.ElementPosition.First:
                    LogInfo($"Возвращаем первый элемент из {elements.Count}");
                    return elements[0];

                case Primo.MIA.Models.ElementPosition.Last:
                    LogInfo($"Возвращаем последний элемент из {elements.Count}");
                    return elements[elements.Count - 1];

                case Primo.MIA.Models.ElementPosition.Nth:
                    if (index < 0 || index >= elements.Count)
                        throw new ArgumentOutOfRangeException(
                            nameof(index),
                            $"Индекс {index} вне диапазона (доступно элементов: {elements.Count})");

                    LogInfo($"Возвращаем элемент #{index} из {elements.Count}");
                    return elements[index];

                default:
                    throw new ArgumentException($"Неизвестная позиция: {position}", nameof(position));
            }
        }

        #endregion

        #endregion

        #region Window and Tab Management (Requirement 10)

        /// <summary>
        /// Получает информацию о всех открытых окнах.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <returns>Список информации об окнах</returns>
        protected List<Primo.MIA.Models.WindowInfo> GetAllWindows(IWebDriver driver)
        {
            var windows = new List<Primo.MIA.Models.WindowInfo>();
            string currentHandle = driver.CurrentWindowHandle;

            foreach (string handle in driver.WindowHandles)
            {
                try
                {
                    driver.SwitchTo().Window(handle);

                    windows.Add(new Primo.MIA.Models.WindowInfo
                    {
                        Handle = handle,
                        Title = driver.Title,
                        Url = driver.Url,
                        OpenedAt = DateTime.Now, // Примерное время
                        IsActive = handle == currentHandle
                    });
                }
                catch (Exception ex)
                {
                    LogWarning($"Не удалось получить информацию об окне {handle}: {ex.Message}");
                }
            }

            // Возвращаемся к исходному окну
            driver.SwitchTo().Window(currentHandle);

            LogInfo($"Найдено {windows.Count} окон");
            return windows;
        }

        /// <summary>
        /// Переключается на окно по заголовку.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="title">Заголовок окна (частичное совпадение)</param>
        /// <returns>True если окно найдено и переключение выполнено</returns>
        protected bool SwitchToWindowByTitle(IWebDriver driver, string title)
        {
            ValidateNotEmpty(title, nameof(title));

            string currentHandle = driver.CurrentWindowHandle;

            foreach (string handle in driver.WindowHandles)
            {
                try
                {
                    driver.SwitchTo().Window(handle);

                    if (driver.Title.Contains(title))
                    {
                        LogInfo($"Переключено на окно с заголовком: {driver.Title}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"Ошибка при проверке окна {handle}: {ex.Message}");
                }
            }

            // Окно не найдено, возвращаемся к исходному
            driver.SwitchTo().Window(currentHandle);
            LogWarning($"Окно с заголовком '{title}' не найдено");
            return false;
        }

        /// <summary>
        /// Переключается на окно по URL.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="url">URL (частичное совпадение)</param>
        /// <returns>True если окно найдено и переключение выполнено</returns>
        protected bool SwitchToWindowByUrl(IWebDriver driver, string url)
        {
            ValidateNotEmpty(url, nameof(url));

            string currentHandle = driver.CurrentWindowHandle;

            foreach (string handle in driver.WindowHandles)
            {
                try
                {
                    driver.SwitchTo().Window(handle);

                    if (driver.Url.Contains(url))
                    {
                        LogInfo($"Переключено на окно с URL: {driver.Url}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"Ошибка при проверке окна {handle}: {ex.Message}");
                }
            }

            // Окно не найдено, возвращаемся к исходному
            driver.SwitchTo().Window(currentHandle);
            LogWarning($"Окно с URL '{url}' не найдено");
            return false;
        }

        /// <summary>
        /// Закрывает все окна кроме основного.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="mainWindowHandle">Handle основного окна (если null, используется первое окно)</param>
        /// <returns>Количество закрытых окон</returns>
        protected int CloseAllExceptMain(IWebDriver driver, string mainWindowHandle = null)
        {
            var handles = driver.WindowHandles;

            if (handles.Count <= 1)
            {
                LogInfo("Открыто только одно окно, нечего закрывать");
                return 0;
            }

            // Если основное окно не указано, используем первое
            if (string.IsNullOrEmpty(mainWindowHandle))
            {
                mainWindowHandle = handles[0];
            }

            int closedCount = 0;

            foreach (string handle in handles)
            {
                if (handle != mainWindowHandle)
                {
                    try
                    {
                        driver.SwitchTo().Window(handle);
                        driver.Close();
                        closedCount++;
                        LogInfo($"Закрыто окно: {handle}");
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Не удалось закрыть окно {handle}: {ex.Message}");
                    }
                }
            }

            // Переключаемся на основное окно
            driver.SwitchTo().Window(mainWindowHandle);
            LogInfo($"Закрыто {closedCount} окон, осталось основное");

            return closedCount;
        }

        #endregion

        #region Cookie and Storage Management (Requirement 11)

        /// <summary>
        /// Экспортирует все cookies в JSON формат.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <returns>JSON строка с cookies</returns>
        protected string ExportCookiesToJson(IWebDriver driver)
        {
            try
            {
                var cookieCollection = new Primo.MIA.Models.CookieCollection
                {
                    SourceUrl = driver.Url,
                    ExportedAt = DateTime.Now
                };

                foreach (var cookie in driver.Manage().Cookies.AllCookies)
                {
                    cookieCollection.Cookies.Add(new Primo.MIA.Models.CookieData
                    {
                        Name = cookie.Name,
                        Value = cookie.Value,
                        Domain = cookie.Domain,
                        Path = cookie.Path,
                        Expiry = cookie.Expiry,
                        Secure = cookie.Secure,
                        HttpOnly = cookie.IsHttpOnly,
                        SameSite = cookie.SameSite
                    });
                }

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(cookieCollection, Newtonsoft.Json.Formatting.Indented);
                LogInfo($"Экспортировано {cookieCollection.Cookies.Count} cookies");
                return json;
            }
            catch (Exception ex)
            {
                LogError("Ошибка экспорта cookies", ex);
                throw;
            }
        }

        /// <summary>
        /// Импортирует cookies из JSON формата.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="json">JSON строка с cookies</param>
        /// <returns>Количество импортированных cookies</returns>
        protected int ImportCookiesFromJson(IWebDriver driver, string json)
        {
            ValidateNotEmpty(json, nameof(json));

            try
            {
                var cookieCollection = Newtonsoft.Json.JsonConvert.DeserializeObject<Primo.MIA.Models.CookieCollection>(json);

                if (cookieCollection == null || cookieCollection.Cookies == null)
                    throw new ArgumentException("Некорректный формат JSON cookies");

                int importedCount = 0;

                foreach (var cookieData in cookieCollection.Cookies)
                {
                    try
                    {
                        var cookie = new Cookie(
                            cookieData.Name,
                            cookieData.Value,
                            cookieData.Domain,
                            cookieData.Path,
                            cookieData.Expiry
                        );

                        driver.Manage().Cookies.AddCookie(cookie);
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Не удалось импортировать cookie '{cookieData.Name}': {ex.Message}");
                    }
                }

                LogInfo($"Импортировано {importedCount} из {cookieCollection.Cookies.Count} cookies");
                return importedCount;
            }
            catch (Exception ex)
            {
                LogError("Ошибка импорта cookies", ex);
                throw;
            }
        }

        /// <summary>
        /// Фильтрует cookies по домену.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="domain">Домен для фильтрации</param>
        /// <returns>Список cookies для указанного домена</returns>
        protected List<Cookie> FilterCookiesByDomain(IWebDriver driver, string domain)
        {
            ValidateNotEmpty(domain, nameof(domain));

            var filtered = new List<Cookie>();

            foreach (var cookie in driver.Manage().Cookies.AllCookies)
            {
                if (cookie.Domain != null && cookie.Domain.Contains(domain))
                {
                    filtered.Add(cookie);
                }
            }

            LogInfo($"Найдено {filtered.Count} cookies для домена '{domain}'");
            return filtered;
        }

        /// <summary>
        /// Фильтрует cookies по имени.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="namePattern">Паттерн имени (частичное совпадение)</param>
        /// <returns>Список cookies с совпадающим именем</returns>
        protected List<Cookie> FilterCookiesByName(IWebDriver driver, string namePattern)
        {
            ValidateNotEmpty(namePattern, nameof(namePattern));

            var filtered = new List<Cookie>();

            foreach (var cookie in driver.Manage().Cookies.AllCookies)
            {
                if (cookie.Name != null && cookie.Name.Contains(namePattern))
                {
                    filtered.Add(cookie);
                }
            }

            LogInfo($"Найдено {filtered.Count} cookies с именем содержащим '{namePattern}'");
            return filtered;
        }

        /// <summary>
        /// Работает с localStorage.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="operation">Операция: get, set, remove, clear</param>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение (для set)</param>
        /// <returns>Значение (для get) или null</returns>
        protected string ManageLocalStorage(IWebDriver driver, string operation, string key = null, string value = null)
        {
            IJavaScriptExecutor js = driver as IJavaScriptExecutor;
            if (js == null)
                throw new InvalidOperationException("WebDriver не поддерживает JavaScript");

            try
            {
                switch (operation.ToLower())
                {
                    case "get":
                        ValidateNotEmpty(key, nameof(key));
                        var result = js.ExecuteScript($"return localStorage.getItem('{key}');");
                        LogInfo($"localStorage.getItem('{key}') = {result}");
                        return result?.ToString();

                    case "set":
                        ValidateNotEmpty(key, nameof(key));
                        js.ExecuteScript($"localStorage.setItem('{key}', '{value}');");
                        LogInfo($"localStorage.setItem('{key}', '{value}')");
                        return null;

                    case "remove":
                        ValidateNotEmpty(key, nameof(key));
                        js.ExecuteScript($"localStorage.removeItem('{key}');");
                        LogInfo($"localStorage.removeItem('{key}')");
                        return null;

                    case "clear":
                        js.ExecuteScript("localStorage.clear();");
                        LogInfo("localStorage.clear()");
                        return null;

                    default:
                        throw new ArgumentException($"Неизвестная операция: {operation}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка работы с localStorage: {operation}", ex);
                throw;
            }
        }

        /// <summary>
        /// Работает с sessionStorage.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="operation">Операция: get, set, remove, clear</param>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение (для set)</param>
        /// <returns>Значение (для get) или null</returns>
        protected string ManageSessionStorage(IWebDriver driver, string operation, string key = null, string value = null)
        {
            IJavaScriptExecutor js = driver as IJavaScriptExecutor;
            if (js == null)
                throw new InvalidOperationException("WebDriver не поддерживает JavaScript");

            try
            {
                switch (operation.ToLower())
                {
                    case "get":
                        ValidateNotEmpty(key, nameof(key));
                        var result = js.ExecuteScript($"return sessionStorage.getItem('{key}');");
                        LogInfo($"sessionStorage.getItem('{key}') = {result}");
                        return result?.ToString();

                    case "set":
                        ValidateNotEmpty(key, nameof(key));
                        js.ExecuteScript($"sessionStorage.setItem('{key}', '{value}');");
                        LogInfo($"sessionStorage.setItem('{key}', '{value}')");
                        return null;

                    case "remove":
                        ValidateNotEmpty(key, nameof(key));
                        js.ExecuteScript($"sessionStorage.removeItem('{key}');");
                        LogInfo($"sessionStorage.removeItem('{key}')");
                        return null;

                    case "clear":
                        js.ExecuteScript("sessionStorage.clear();");
                        LogInfo("sessionStorage.clear()");
                        return null;

                    default:
                        throw new ArgumentException($"Неизвестная операция: {operation}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка работы с sessionStorage: {operation}", ex);
                throw;
            }
        }

        /// <summary>
        /// Очищает все cookies и storage одной операцией.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        protected void ClearAllCookiesAndStorage(IWebDriver driver)
        {
            try
            {
                // Очистка cookies
                driver.Manage().Cookies.DeleteAllCookies();
                LogInfo("Все cookies удалены");

                // Очистка localStorage и sessionStorage
                IJavaScriptExecutor js = driver as IJavaScriptExecutor;
                if (js != null)
                {
                    js.ExecuteScript("localStorage.clear(); sessionStorage.clear();");
                    LogInfo("localStorage и sessionStorage очищены");
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка очистки cookies и storage", ex);
                throw;
            }
        }

        #endregion

    }
}