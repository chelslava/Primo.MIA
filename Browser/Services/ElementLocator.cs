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
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

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
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            if (string.IsNullOrWhiteSpace(locatorValue))
                throw new ArgumentException("Значение локатора не может быть пустым", nameof(locatorValue));

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
        /// Находит все элементы по локатору.
        /// </summary>
        public IReadOnlyCollection<IWebElement> FindElements(
            IWebDriver driver, 
            LocatorType locatorType, 
            string locatorValue)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            if (string.IsNullOrWhiteSpace(locatorValue))
                throw new ArgumentException("Значение локатора не может быть пустым", nameof(locatorValue));

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
            try
            {
                return FindElement(driver, locatorType, locatorValue, timeoutSeconds);
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
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            if (string.IsNullOrWhiteSpace(locatorValue))
                throw new ArgumentException("Значение локатора не может быть пустым", nameof(locatorValue));

            var by = CreateLocator(locatorType, locatorValue);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                return wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(by));
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
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            if (string.IsNullOrWhiteSpace(locatorValue))
                throw new ArgumentException("Значение локатора не может быть пустым", nameof(locatorValue));

            var by = CreateLocator(locatorType, locatorValue);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                return wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(by));
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
            return locatorType switch
            {
                LocatorType.Id => By.Id(locatorValue),
                LocatorType.Name => By.Name(locatorValue),
                LocatorType.ClassName => By.ClassName(locatorValue),
                LocatorType.TagName => By.TagName(locatorValue),
                LocatorType.LinkText => By.LinkText(locatorValue),
                LocatorType.PartialLinkText => By.PartialLinkText(locatorValue),
                LocatorType.CssSelector => By.CssSelector(locatorValue),
                LocatorType.XPath => By.XPath(locatorValue),
                _ => throw new ArgumentException($"Неподдерживаемый тип локатора: {locatorType}")
            };
        }
    }
}
