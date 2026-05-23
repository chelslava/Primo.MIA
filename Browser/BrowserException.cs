// =============================================================================
// BrowserException.cs — исключение модуля Browser.
//
// Выбрасывается при ошибках автоматизации браузера через Selenium WebDriver.
// =============================================================================

using System;

namespace Primo.MIA
{
    /// <summary>
    /// Исключение, выбрасываемое при ошибке в модуле Browser.
    /// </summary>
    public class BrowserException : Exception
    {
        /// <summary>Создаёт исключение с сообщением об ошибке.</summary>
        public BrowserException(string message)
            : base(message)
        {
        }

        /// <summary>Создаёт исключение с сообщением и внутренним исключением.</summary>
        public BrowserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
