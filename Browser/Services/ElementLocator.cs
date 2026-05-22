// =============================================================================
// ElementLocator.cs — реализация IElementLocator
//
// Предоставляет методы для поиска элементов на странице с различными
// стратегиями ожидания и валидации.
//
// Особенности:
//   - Явные ожидания через WebDriverWait
//   - Поддержка всех типов локаторов Selenium
//   - Методы для ожидания кликабельности и видимости
//   - Безопасный TryFind без исключений
// =============================================================================

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Локатор элементов с поддержкой явных ожиданий.
    /// </summary>
    public class ElementLocator : IElementLocator
    {
        /// <summary>
        /// Находит элемент по локатору с ожиданием.
        /// </summary>
        public IWebElement FindElement(
            IWebDriver driver,
            LocatorType locatorType,
            string locatorValue,
            int timeoutSeconds)
        {
            return FindElementWithWaitMode(driver, locatorType, locatorValue, timeoutSeconds, ElementWaitMode.Present);
        }

        /// <summary>
        /// Находит элемент с указанным режимом ожидания.
        /// </summary>
        public IWebElement FindElementWithWaitMode(
            IWebDriver driver,
            LocatorType locatorType,
            string locatorValue,
            int timeoutSeconds,
            ElementWaitMode waitMode)
        {
            Guard.NotNull(driver, nameof(driver));
            Guard.NotNullOrWhiteSpace(locatorValue, nameof(locatorValue));

            switch (waitMode)
            {
                case ElementWaitMode.Present:
                    return FindElementPresent(driver, locatorType, locatorValue, timeoutSeconds);
                case ElementWaitMode.Visible:
                    return WaitForVisible(driver, locatorType, locatorValue, timeoutSeconds);
                case ElementWaitMode.Clickable:
                    return WaitForClickable(driver, locatorType, locatorValue, timeoutSeconds);
                case ElementWaitMode.None:
                    return FindElementImmediate(driver, locatorType, locatorValue);
                default:
                    throw new ArgumentException($"Неподдерживаемый режим ожидания: {waitMode}");
            }
        }

        /// <summary>
        /// Находит элемент с ожиданием появления в DOM.
        /// </summary>
        private IWebElement FindElementPresent(
            IWebDriver driver,
            LocatorType locatorType,
            string locatorValue,
            int timeoutSeconds)
        {
            var by = CreateLocator(locatorType, locatorValue);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                return wait.Until(d => d.FindElement(by));
            }
            catch (WebDriverTimeoutException ex)
            {
                throw new NoSuchElementException(
                    $"Элемент не найден: {locatorType}='{locatorValue}' (таймаут: {timeoutSeconds}с)",
                    ex);
            }
        }

        /// <summary>
        /// Находит элемент немедленно без ожидания.
        /// </summary>
        private IWebElement FindElementImmediate(
            IWebDriver driver,
            LocatorType locatorType,
            string locatorValue)
        {
            var by = CreateLocator(locatorType, locatorValue);
            return driver.FindElement(by);
        }

        /// <summary>
        /// Находит все элементы по локатору.
        /// </summary>
        public IReadOnlyCollection<IWebElement> FindElements(
            IWebDriver driver,
            LocatorType locatorType,
            string locatorValue)
        {
            Guard.NotNull(driver, nameof(driver));
            Guard.NotNullOrWhiteSpace(locatorValue, nameof(locatorValue));

            var by = CreateLocator(locatorType, locatorValue);
            return driver.FindElements(by);
        }

        /// <summary>
        /// Пытается найти элемент, возвращая null если не найден.
        /// </summary>
        public IWebElement TryFindElement(
            IWebDriver driver,
            LocatorType locatorType,
            string locatorValue,
            int timeoutSeconds)
        {
            return TryFindElementWithWaitMode(driver, locatorType, locatorValue, timeoutSeconds, ElementWaitMode.Present);
        }

        /// <summary>
        /// Пытается найти элемент с указанным режимом ожидания, возвращая null если не найден.
        /// </summary>
        public IWebElement TryFindElementWithWaitMode(
            IWebDriver driver,
            LocatorType locatorType,
            string locatorValue,
            int timeoutSeconds,
            ElementWaitMode waitMode)
        {
            try
            {
                return FindElementWithWaitMode(driver, locatorType, locatorValue, timeoutSeconds, waitMode);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Ожидает пока элемент станет кликабельным.
        /// </summary>
        public IWebElement WaitForClickable(
            IWebDriver driver,
            LocatorType locatorType,
            string locatorValue,
            int timeoutSeconds)
        {
            Guard.NotNull(driver, nameof(driver));
            Guard.NotNullOrWhiteSpace(locatorValue, nameof(locatorValue));

            var by = CreateLocator(locatorType, locatorValue);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                return wait.Until(webDriver => 
                {
                    var element = webDriver.FindElement(by);
                    return element.Enabled && element.Displayed ? element : null;
                });
            }
            catch (WebDriverTimeoutException ex)
            {
                throw new ElementNotInteractableException(
                    $"Элемент не стал кликабельным: {locatorType}='{locatorValue}' (таймаут: {timeoutSeconds}с)",
                    ex);
            }
        }

        /// <summary>
        /// Ожидает пока элемент станет видимым.
        /// </summary>
        public IWebElement WaitForVisible(
            IWebDriver driver,
            LocatorType locatorType,
            string locatorValue,
            int timeoutSeconds)
        {
            Guard.NotNull(driver, nameof(driver));
            Guard.NotNullOrWhiteSpace(locatorValue, nameof(locatorValue));

            var by = CreateLocator(locatorType, locatorValue);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                return wait.Until(webDriver => 
                {
                    var element = webDriver.FindElement(by);
                    return element.Displayed ? element : null;
                });
            }
            catch (WebDriverTimeoutException ex)
            {
                throw new NoSuchElementException(
                    $"Элемент не стал видимым: {locatorType}='{locatorValue}' (таймаут: {timeoutSeconds}с)",
                    ex);
            }
        }

        /// <summary>
        /// Создаёт объект By из типа и значения локатора.
        /// </summary>
        private By CreateLocator(LocatorType locatorType, string locatorValue)
        {
            switch (locatorType)
            {
                case LocatorType.Id:
                    return By.Id(locatorValue);
                case LocatorType.Name:
                    return By.Name(locatorValue);
                case LocatorType.ClassName:
                    return By.ClassName(locatorValue);
                case LocatorType.TagName:
                    return By.TagName(locatorValue);
                case LocatorType.LinkText:
                    return By.LinkText(locatorValue);
                case LocatorType.PartialLinkText:
                    return By.PartialLinkText(locatorValue);
                case LocatorType.CssSelector:
                    return By.CssSelector(locatorValue);
                case LocatorType.XPath:
                    return By.XPath(locatorValue);
                default:
                    throw new ArgumentException($"Неподдерживаемый тип локатора: {locatorType}");
            }
        }
    }
}
