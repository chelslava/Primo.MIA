// =============================================================================
// JsonActivityException.cs — исключение модуля Json.
//
// Выбрасывается при ошибках парсинга или запросов к JSON-данным.
// =============================================================================

using System;

namespace Primo.MIA
{
    /// <summary>
    /// Исключение, выбрасываемое при ошибке в модуле Json.
    /// </summary>
    public class JsonActivityException : Exception
    {
        /// <summary>Создаёт исключение с сообщением об ошибке.</summary>
        public JsonActivityException(string message)
            : base(message)
        {
        }

        /// <summary>Создаёт исключение с сообщением и внутренним исключением.</summary>
        public JsonActivityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
