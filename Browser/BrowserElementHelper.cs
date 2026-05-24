using System;
using System.Collections.ObjectModel;
using System.IO;
using OpenQA.Selenium;
using Primo.MIA.Common;
using Primo.MIA.Models;

namespace Primo.MIA
{
    internal static class BrowserElementHelper
    {
        internal static IWebElement FindElementWithRetry(
            IWebDriver driver,
            ElementLocatorType locatorType,
            string locatorValue,
            int timeout,
            RetryConfiguration retryConfig,
            Func<IWebDriver, ElementLocatorType, string, int, IWebElement> findElement,
            Action<string> logInfo,
            Action<string> logWarning)
        {
            retryConfig = retryConfig ?? RetryConfiguration.Default;
            int attempt = 0;
            Exception lastException = null;

            while (attempt < retryConfig.MaxRetries)
            {
                try
                {
                    attempt++;
                    logInfo($"Попытка {attempt}/{retryConfig.MaxRetries} поиска элемента [{locatorType}={locatorValue}]");

                    IWebElement element = findElement(driver, locatorType, locatorValue, timeout);

                    if (attempt > 1)
                        logInfo($"Элемент найден после {attempt} попыток");

                    return element;
                }
                catch (Exception ex) when (IsRetryableException(ex, retryConfig))
                {
                    lastException = ex;
                    logWarning($"Попытка {attempt} не удалась: {ex.Message}");

                    if (attempt < retryConfig.MaxRetries)
                    {
                        int delay = retryConfig.ExponentialBackoff
                            ? retryConfig.RetryDelayMs * (int)Math.Pow(2, attempt - 1)
                            : retryConfig.RetryDelayMs;

                        logInfo($"Ожидание {delay}ms перед следующей попыткой");
                        System.Threading.Thread.Sleep(delay);
                    }
                }
            }

            throw new NoSuchElementException(
                $"Элемент не найден после {retryConfig.MaxRetries} попыток. Последняя ошибка: {lastException?.Message}",
                lastException);
        }

        private static bool IsRetryableException(Exception ex, RetryConfiguration config)
        {
            Type exType = ex.GetType();
            foreach (Type retryableType in config.RetryableExceptions)
            {
                if (retryableType.IsAssignableFrom(exType))
                    return true;
            }
            return false;
        }

        internal static IWebElement FindElementInShadowRoot(
            IWebElement shadowHost,
            ElementLocatorType locatorType,
            string locatorValue,
            Action<string, Exception> logError)
        {
            if (shadowHost == null)
                throw new ArgumentNullException(nameof(shadowHost));
            if (string.IsNullOrWhiteSpace(locatorValue))
                throw new ArgumentException("Значение локатора не может быть пустым", nameof(locatorValue));

            try
            {
                IJavaScriptExecutor js = shadowHost as IJavaScriptExecutor;
                if (js == null && shadowHost is IWrapsDriver wrapsDriver)
                    js = wrapsDriver.WrappedDriver as IJavaScriptExecutor;

                if (js == null)
                    throw new InvalidOperationException("WebDriver не поддерживает JavaScript");

                IWebElement shadowRoot = js.ExecuteScript("return arguments[0].shadowRoot", shadowHost) as IWebElement;
                if (shadowRoot == null)
                    throw new InvalidOperationException("Элемент не содержит Shadow Root");

                By locator = SeleniumHelper.CreateLocator(locatorType, locatorValue);
                return shadowRoot.FindElement(locator);
            }
            catch (Exception ex)
            {
                logError($"Ошибка поиска в Shadow DOM: {ex.Message}", ex);
                throw;
            }
        }

        internal static IWebElement FindElementInShadowChain(
            IWebDriver driver,
            string[] shadowPath,
            ElementLocatorType finalLocatorType,
            string finalLocatorValue,
            Action<string> logInfo,
            Action<string, Exception> logError)
        {
            if (shadowPath == null || shadowPath.Length == 0)
                throw new ArgumentException("Shadow path не может быть пустым", nameof(shadowPath));

            IJavaScriptExecutor js = driver as IJavaScriptExecutor;
            if (js == null)
                throw new InvalidOperationException("WebDriver не поддерживает JavaScript");

            try
            {
                string script = "let element = document;";
                for (int i = 0; i < shadowPath.Length; i++)
                    script += $"\nelement = element.querySelector('{shadowPath[i]}').shadowRoot;";
                script += $"\nreturn element.querySelector('{finalLocatorValue}');";

                logInfo($"Поиск через Shadow DOM цепочку: {string.Join(" -> ", shadowPath)} -> {finalLocatorValue}");

                IWebElement element = js.ExecuteScript(script) as IWebElement;
                if (element == null)
                    throw new NoSuchElementException("Элемент не найден в Shadow DOM цепочке");

                return element;
            }
            catch (Exception ex)
            {
                logError($"Ошибка поиска в Shadow DOM цепочке: {ex.Message}", ex);
                throw;
            }
        }

        internal static IWebElement FindElementWithIframeDetection(
            IWebDriver driver,
            ElementLocatorType locatorType,
            string locatorValue,
            int timeout,
            Func<IWebDriver, ElementLocatorType, string, int, IWebElement> tryFindElement,
            Action<string> logInfo,
            Action<string> logWarning)
        {
            IWebElement element = tryFindElement(driver, locatorType, locatorValue, timeout);
            if (element != null)
                return element;

            logInfo("Элемент не найден в текущем контексте, ищем в iframe");
            var iframes = driver.FindElements(By.TagName("iframe"));
            logInfo($"Найдено {iframes.Count} iframe на странице");

            foreach (var iframe in iframes)
            {
                try
                {
                    driver.SwitchTo().Frame(iframe);
                    logInfo("Переключились в iframe");

                    element = tryFindElement(driver, locatorType, locatorValue, timeout);
                    if (element != null)
                    {
                        logInfo("Элемент найден в iframe");
                        return element;
                    }

                    driver.SwitchTo().DefaultContent();
                }
                catch (Exception ex)
                {
                    logWarning($"Ошибка при поиске в iframe: {ex.Message}");
                    driver.SwitchTo().DefaultContent();
                }
            }

            throw new NoSuchElementException(
                $"Элемент [{locatorType}={locatorValue}] не найден ни в основном контексте, ни в {iframes.Count} iframe");
        }

        internal static string CaptureErrorScreenshot(
            IWebDriver driver,
            string activityName,
            Exception error,
            Action<string> logInfo,
            Action<string> logWarning,
            Action<string, Exception> logError)
        {
            try
            {
                ITakesScreenshot screenshotDriver = driver as ITakesScreenshot;
                if (screenshotDriver == null)
                {
                    logWarning("WebDriver не поддерживает создание скриншотов");
                    return null;
                }

                string screenshotDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Primo.MIA",
                    "Screenshots");

                if (!Directory.Exists(screenshotDir))
                    Directory.CreateDirectory(screenshotDir);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string filepath = Path.Combine(screenshotDir, $"{timestamp}_{activityName}.png");

                Screenshot screenshot = screenshotDriver.GetScreenshot();
                screenshot.SaveAsFile(filepath);

                logInfo($"Скриншот сохранен: {filepath}");
                return filepath;
            }
            catch (Exception ex)
            {
                logError($"Ошибка создания скриншота: {ex.Message}", ex);
                return null;
            }
        }

        internal static IWebElement FindByText(
            IWebDriver driver,
            string text,
            bool exactMatch,
            Action<string> logInfo)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Значение не может быть пустым", nameof(text));

            string xpath = exactMatch
                ? $"//*[text()='{text}']"
                : $"//*[contains(text(), '{text}')]";

            logInfo($"Поиск элемента по тексту: '{text}' (точное совпадение: {exactMatch})");

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

        internal static IWebElement FindByPosition(
            IWebDriver driver,
            ElementLocatorType locatorType,
            string locatorValue,
            ElementPosition position,
            int index,
            Func<IWebDriver, ElementLocatorType, string, ReadOnlyCollection<IWebElement>> findElements,
            Action<string> logInfo)
        {
            if (string.IsNullOrWhiteSpace(locatorValue))
                throw new ArgumentException("Значение локатора не может быть пустым", nameof(locatorValue));

            var elements = findElements(driver, locatorType, locatorValue);
            if (elements.Count == 0)
                throw new NoSuchElementException($"Элементы [{locatorType}={locatorValue}] не найдены");

            switch (position)
            {
                case ElementPosition.First:
                    logInfo($"Возвращаем первый элемент из {elements.Count}");
                    return elements[0];

                case ElementPosition.Last:
                    logInfo($"Возвращаем последний элемент из {elements.Count}");
                    return elements[elements.Count - 1];

                case ElementPosition.Nth:
                    if (index < 0 || index >= elements.Count)
                        throw new ArgumentOutOfRangeException(nameof(index),
                            $"Индекс {index} вне диапазона (доступно элементов: {elements.Count})");
                    logInfo($"Возвращаем элемент #{index} из {elements.Count}");
                    return elements[index];

                default:
                    throw new ArgumentException($"Неизвестная позиция: {position}", nameof(position));
            }
        }
    }
}
