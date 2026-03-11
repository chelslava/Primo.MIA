// =============================================================================
// ElementWaitingActivityBase.cs — базовый класс для активностей с ожиданием элементов
//
// Предоставляет унифицированную логику ожидания элементов для всех
// браузерных активностей, работающих с элементами.
//
// Особенности:
//   - Поддержка различных режимов ожидания через ElementWaitMode
//   - Унифицированные методы разрешения элементов
//   - Автоматическая валидация параметров ожидания
//   - Логирование процесса ожидания
// =============================================================================

using LTools.Common.Model;
using LTools.SDK;
using OpenQA.Selenium;
using Primo.MIA.Common;
using System;

namespace Primo.MIA
{
    /// <summary>
    /// Базовый класс для активностей, работающих с элементами и ожиданием.
    /// </summary>
    /// <typeparam name="T">Тип UI активности</typeparam>
    public abstract class ElementWaitingActivityBase<T> : BrowserActivityBase<T> where T : class
    {
        protected ElementWaitingActivityBase(IWFContainer container) : base(container)
        {
        }

        /// <summary>
        /// Разрешает элемент с указанным режимом ожидания.
        /// </summary>
        /// <param name="sd">Данные скрипта</param>
        /// <param name="driver">Драйвер браузера</param>
        /// <param name="elementId">ID элемента в репозитории</param>
        /// <param name="locatorValue">Значение локатора</param>
        /// <param name="locatorType">Тип локатора</param>
        /// <param name="waitMode">Режим ожидания</param>
        /// <param name="timeoutSeconds">Таймаут ожидания в секундах</param>
        /// <returns>Найденный элемент или null</returns>
        protected IWebElement ResolveElementWithWait(
            ScriptingData sd,
            IWebDriver driver,
            string elementId,
            string locatorValue,
            ElementLocatorType locatorType,
            ElementWaitMode waitMode,
            int timeoutSeconds)
        {
            var elementLocator = new ElementLocator();

            // Если указан ID элемента из репозитория
            if (!string.IsNullOrWhiteSpace(elementId))
            {
                try
                {
                    var element = ElementRepository.GetElement(elementId);
                    
                    // Проверяем соответствие элемента режиму ожидания
                    if (element != null && ValidateElementForWaitMode(element, waitMode, elementId))
                    {
                        return element;
                    }
                    
                    return null;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(sdkComponentName, 
                        "Не удалось получить элемент из репозитория. ID: {0}, Ошибка: {1}", 
                        elementId, ex.Message);
                    return null;
                }
            }

            // Если указан локатор
            if (!string.IsNullOrWhiteSpace(locatorValue))
            {
                var seleniumLocatorType = ConvertLocatorType(locatorType);
                
                Logger.LogDebug(sdkComponentName, 
                    "Ожидание элемента: {0}='{1}', режим: {2}, таймаут: {3}с", 
                    seleniumLocatorType, locatorValue, waitMode, timeoutSeconds);

                return elementLocator.TryFindElementWithWaitMode(
                    driver, seleniumLocatorType, locatorValue, timeoutSeconds, waitMode);
            }

            return null;
        }

        /// <summary>
        /// Разрешает элемент с ожиданием кликабельности (по умолчанию).
        /// </summary>
        protected IWebElement ResolveClickableElement(
            ScriptingData sd,
            IWebDriver driver,
            string elementId,
            string locatorValue,
            ElementLocatorType locatorType,
            int timeoutSeconds)
        {
            return ResolveElementWithWait(sd, driver, elementId, locatorValue, locatorType, 
                ElementWaitMode.Clickable, timeoutSeconds);
        }

        /// <summary>
        /// Разрешает элемент с ожиданием видимости.
        /// </summary>
        protected IWebElement ResolveVisibleElement(
            ScriptingData sd,
            IWebDriver driver,
            string elementId,
            string locatorValue,
            ElementLocatorType locatorType,
            int timeoutSeconds)
        {
            return ResolveElementWithWait(sd, driver, elementId, locatorValue, locatorType, 
                ElementWaitMode.Visible, timeoutSeconds);
        }

        /// <summary>
        /// Разрешает элемент с ожиданием появления в DOM.
        /// </summary>
        protected IWebElement ResolvePresentElement(
            ScriptingData sd,
            IWebDriver driver,
            string elementId,
            string locatorValue,
            ElementLocatorType locatorType,
            int timeoutSeconds)
        {
            return ResolveElementWithWait(sd, driver, elementId, locatorValue, locatorType, 
                ElementWaitMode.Present, timeoutSeconds);
        }

        /// <summary>
        /// Валидирует элемент из репозитория в соответствии с режимом ожидания.
        /// </summary>
        private bool ValidateElementForWaitMode(IWebElement element, ElementWaitMode waitMode, string elementId)
        {
            try
            {
                switch (waitMode)
                {
                    case ElementWaitMode.Present:
                        return true; // Элемент уже получен из репозитория, значит присутствует

                    case ElementWaitMode.Visible:
                        if (!element.Displayed)
                        {
                            Logger.LogWarning(sdkComponentName, 
                                "Элемент с ID '{0}' не видим на странице", elementId);
                            return false;
                        }
                        return true;

                    case ElementWaitMode.Clickable:
                        if (!element.Displayed || !element.Enabled)
                        {
                            Logger.LogWarning(sdkComponentName, 
                                "Элемент с ID '{0}' не кликабельный (Displayed: {1}, Enabled: {2})", 
                                elementId, element.Displayed, element.Enabled);
                            return false;
                        }
                        return true;

                    case ElementWaitMode.None:
                        return true; // Без проверок

                    default:
                        Logger.LogWarning(sdkComponentName, 
                            "Неподдерживаемый режим ожидания: {0}", waitMode);
                        return false;
                }
            }
            catch (StaleElementReferenceException)
            {
                Logger.LogWarning(sdkComponentName, 
                    "Элемент с ID '{0}' устарел (StaleElementReferenceException)", elementId);
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(sdkComponentName, 
                    "Ошибка при проверке элемента с ID '{0}': {1}", elementId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Парсит таймаут из строкового значения с валидацией.
        /// </summary>
        protected int ParseAndValidateTimeout(string timeoutStr, string propertyName, int defaultValue = 10)
        {
            int timeout = int.TryParse(timeoutStr?.Trim('"'), out int t) ? t : defaultValue;
            ValidatePositive(timeout, propertyName);
            return timeout;
        }
    }
}