// =============================================================================
// XmlActivityException.cs — исключение модуля Xml.
//
// Выбрасывается при ошибках парсинга или XPath-запросов к XML-данным.
// =============================================================================

using System;

namespace Primo.MIA
{
    /// <summary>
    /// Исключение, выбрасываемое при ошибке в модуле Xml.
    /// </summary>
    public class XmlActivityException : Exception
    {
        /// <summary>Создаёт исключение с сообщением об ошибке.</summary>
        public XmlActivityException(string message)
            : base(message)
        {
        }

        /// <summary>Создаёт исключение с сообщением и внутренним исключением.</summary>
        public XmlActivityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
