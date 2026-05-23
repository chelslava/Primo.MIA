// =============================================================================
// HttpActivityException.cs — исключение модуля HttpWeb.
//
// Выбрасывается при ошибках выполнения HTTP-запросов и вебхуков.
// =============================================================================

using System;

namespace Primo.MIA
{
    /// <summary>
    /// Исключение, выбрасываемое при ошибке в модуле HttpWeb.
    /// </summary>
    public class HttpActivityException : Exception
    {
        /// <summary>HTTP-статус код, если применимо.</summary>
        public int? StatusCode { get; }

        /// <summary>Создаёт исключение с сообщением об ошибке.</summary>
        public HttpActivityException(string message)
            : base(message)
        {
        }

        /// <summary>Создаёт исключение с сообщением и HTTP-статусом.</summary>
        public HttpActivityException(string message, int statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>Создаёт исключение с сообщением и внутренним исключением.</summary>
        public HttpActivityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>Создаёт исключение с сообщением, статусом и внутренним исключением.</summary>
        public HttpActivityException(string message, int statusCode, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
