using System;
using OpenQA.Selenium;
using Primo.MIA.Common;
using PrimoSdk.Scripting;

namespace Primo.MIA.Browser
{
    /// <summary>
    /// Базовый класс для всех активностей работы с браузером.
    /// Предоставляет общую функциональность для работы с WebDriver и элементами.
    /// </summary>
    /// <typeparam name="TView">Тип представления активности (XAML)</typeparam>
    public abstract class BrowserActivityBase<TView> : PrimoComponentTO<TView>
        where TView : class
    {
        #region Driver Management

        /// <summary>
        /// Получает WebDriver из контекста, используя SessionResolver для разрешения sessionId.
        /// </summary>
        /// <param name="sd">Контекст выполнения</param>
        /// <param name="sessionIdProperty">Имя свойства с sessionId</param>
        /// <returns>Экземпляр WebDriver</returns>
        /// <exception cref="InvalidOperationException">Если драйвер не найден</exception>
        protected IWebDriver GetDriverFromContext(ScriptingData sd, string sessionIdProperty)
        {
            string sessionId = GetPropertyValue<string>(sessionIdProperty, nameof(sessionIdProperty), sd);
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
        protected IWebDriver TryGetDriverFromContext(ScriptingData sd, string sessionIdProperty)
        {
            try
            {
                string sessionId = GetPropertyValue<string>(sessionIdProperty, nameof(sessionIdProperty), sd);
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
            LocatorType locatorType,
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
            LocatorType locatorType,
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
            LocatorType locatorType,
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
    }
}
