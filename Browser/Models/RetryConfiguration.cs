using OpenQA.Selenium;
using System;
using System.Collections.Generic;

namespace Primo.MIA.Models
{
    /// <summary>
    /// Конфигурация retry механизма для операций с элементами.
    /// </summary>
    public class RetryConfiguration
    {
        /// <summary>
        /// Максимальное количество попыток (по умолчанию 3).
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Задержка между попытками в миллисекундах (по умолчанию 500ms).
        /// </summary>
        public int RetryDelayMs { get; set; } = 500;

        /// <summary>
        /// Использовать экспоненциальную задержку (по умолчанию false).
        /// </summary>
        public bool ExponentialBackoff { get; set; } = false;

        /// <summary>
        /// Типы исключений, для которых выполняется retry.
        /// </summary>
        public List<Type> RetryableExceptions { get; set; } = new List<Type>
        {
            typeof(StaleElementReferenceException),
            typeof(NoSuchElementException),
            typeof(ElementNotInteractableException)
        };

        /// <summary>
        /// Конфигурация по умолчанию.
        /// </summary>
        public static RetryConfiguration Default => new RetryConfiguration();
    }
}
