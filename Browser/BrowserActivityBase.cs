using System;
using System.Collections.Generic;
using LTools.SDK;
using LTools.Common.UIElements;
using OpenQA.Selenium;
using Primo.MIA.Common;
using Primo.MIA.Models;
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
            Guard.NotNullOrWhiteSpace(value, parameterName);
        }

        /// <summary>
        /// Валидирует что значение положительное.
        /// </summary>
        protected void ValidatePositive(int value, string parameterName)
        {
            Guard.Positive(value, parameterName);
        }

        /// <summary>
        /// Валидирует что значение не отрицательное.
        /// </summary>
        protected void ValidateNotNegative(int value, string parameterName)
        {
            Guard.NotNegative(value, parameterName);
        }

        /// <summary>
        /// Валидирует URL.
        /// </summary>
        protected void ValidateUrl(string url)
        {
            Guard.NotNullOrWhiteSpace(url, "url");

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                throw new ArgumentException($"Некорректный URL: {url}");

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Поддерживаются только HTTP и HTTPS протоколы");
        }

        /// <summary>
        /// Валидирует что объект не null.
        /// </summary>
        protected void ValidateNotNull(object value, string parameterName)
        {
            Guard.NotNull(value, parameterName);
        }

        /// <summary>
        /// Валидирует что коллекция не пустая.
        /// </summary>
        protected void ValidateNotEmpty<T>(ICollection<T> collection, string parameterName)
        {
            Guard.NotEmpty(collection, parameterName);
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
        protected IWebElement FindElementWithRetry(
            IWebDriver driver,
            ElementLocatorType locatorType,
            string locatorValue,
            int timeout,
            RetryConfiguration retryConfig = null)
            => BrowserElementHelper.FindElementWithRetry(
                driver, locatorType, locatorValue, timeout, retryConfig, FindElement, LogInfo, LogWarning);

        #endregion

        #region Shadow DOM Support

        /// <summary>
        /// Находит элемент внутри Shadow Root.
        /// </summary>
        protected IWebElement FindElementInShadowRoot(
            IWebElement shadowHost,
            ElementLocatorType locatorType,
            string locatorValue)
            => BrowserElementHelper.FindElementInShadowRoot(shadowHost, locatorType, locatorValue, LogError);

        /// <summary>
        /// Находит элемент через цепочку Shadow DOM.
        /// </summary>
        protected IWebElement FindElementInShadowChain(
            IWebDriver driver,
            string[] shadowPath,
            ElementLocatorType finalLocatorType,
            string finalLocatorValue)
            => BrowserElementHelper.FindElementInShadowChain(
                driver, shadowPath, finalLocatorType, finalLocatorValue, LogInfo, LogError);

        #endregion

        #region iframe Auto-handling

        /// <summary>
        /// Находит элемент с автоматическим обнаружением iframe.
        /// </summary>
        protected IWebElement FindElementWithIframeDetection(
            IWebDriver driver,
            ElementLocatorType locatorType,
            string locatorValue,
            int timeout)
            => BrowserElementHelper.FindElementWithIframeDetection(
                driver, locatorType, locatorValue, timeout, TryFindElement, LogInfo, LogWarning);

        #endregion

        #region Screenshot on Error

        /// <summary>
        /// Создает скриншот при ошибке.
        /// </summary>
        protected string CaptureErrorScreenshot(
            IWebDriver driver,
            string activityName,
            Exception error)
            => BrowserElementHelper.CaptureErrorScreenshot(driver, activityName, error, LogInfo, LogWarning, LogError);

        #endregion

        #region Extended Selectors

        /// <summary>
        /// Находит элемент по тексту.
        /// </summary>
        protected IWebElement FindByText(
            IWebDriver driver,
            string text,
            bool exactMatch = false)
            => BrowserElementHelper.FindByText(driver, text, exactMatch, LogInfo);

        /// <summary>
        /// Находит элемент по позиции в коллекции.
        /// </summary>
        protected IWebElement FindByPosition(
            IWebDriver driver,
            ElementLocatorType locatorType,
            string locatorValue,
            ElementPosition position,
            int index = 0)
            => BrowserElementHelper.FindByPosition(
                driver, locatorType, locatorValue, position, index, FindElements, LogInfo);

        #endregion

        #endregion

        #region Window and Tab Management

        /// <summary>
        /// Получает информацию о всех открытых окнах.
        /// </summary>
        protected List<WindowInfo> GetAllWindows(IWebDriver driver)
            => BrowserWindowHelper.GetAllWindows(driver, LogInfo, LogWarning);

        /// <summary>
        /// Переключается на окно по заголовку.
        /// </summary>
        protected bool SwitchToWindowByTitle(IWebDriver driver, string title)
            => BrowserWindowHelper.SwitchToWindowByTitle(driver, title, LogInfo, LogWarning);

        /// <summary>
        /// Переключается на окно по URL.
        /// </summary>
        protected bool SwitchToWindowByUrl(IWebDriver driver, string url)
            => BrowserWindowHelper.SwitchToWindowByUrl(driver, url, LogInfo, LogWarning);

        /// <summary>
        /// Закрывает все окна кроме основного.
        /// </summary>
        protected int CloseAllExceptMain(IWebDriver driver, string mainWindowHandle = null)
            => BrowserWindowHelper.CloseAllExceptMain(driver, mainWindowHandle, LogInfo, LogWarning);

        #endregion

        #region Cookie and Storage Management

        /// <summary>
        /// Экспортирует все cookies в JSON формат.
        /// </summary>
        protected string ExportCookiesToJson(IWebDriver driver)
            => BrowserStorageHelper.ExportCookiesToJson(driver, LogInfo, LogError);

        /// <summary>
        /// Импортирует cookies из JSON формата.
        /// </summary>
        protected int ImportCookiesFromJson(IWebDriver driver, string json)
            => BrowserStorageHelper.ImportCookiesFromJson(driver, json, LogInfo, LogWarning, LogError);

        /// <summary>
        /// Фильтрует cookies по домену.
        /// </summary>
        protected List<Cookie> FilterCookiesByDomain(IWebDriver driver, string domain)
            => BrowserStorageHelper.FilterCookiesByDomain(driver, domain, LogInfo);

        /// <summary>
        /// Фильтрует cookies по имени.
        /// </summary>
        protected List<Cookie> FilterCookiesByName(IWebDriver driver, string namePattern)
            => BrowserStorageHelper.FilterCookiesByName(driver, namePattern, LogInfo);

        /// <summary>
        /// Работает с localStorage.
        /// </summary>
        protected string ManageLocalStorage(IWebDriver driver, string operation, string key = null, string value = null)
            => BrowserStorageHelper.ManageLocalStorage(driver, operation, key, value, LogInfo, LogError);

        /// <summary>
        /// Работает с sessionStorage.
        /// </summary>
        protected string ManageSessionStorage(IWebDriver driver, string operation, string key = null, string value = null)
            => BrowserStorageHelper.ManageSessionStorage(driver, operation, key, value, LogInfo, LogError);

        /// <summary>
        /// Очищает все cookies и storage одной операцией.
        /// </summary>
        protected void ClearAllCookiesAndStorage(IWebDriver driver)
            => BrowserStorageHelper.ClearAllCookiesAndStorage(driver, LogInfo, LogError);

        #endregion

    }
}
