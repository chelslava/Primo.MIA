// =============================================================================
// TextActivityException.cs — исключение модуля Text.
//
// Выбрасывается при ошибках обработки текста (шаблоны, разбор, транслитерация).
// =============================================================================

using System;

namespace Primo.MIA
{
    /// <summary>
    /// Исключение, выбрасываемое при ошибке в модуле Text.
    /// </summary>
    public class TextActivityException : Exception
    {
        /// <summary>Создаёт исключение с сообщением об ошибке.</summary>
        public TextActivityException(string message)
            : base(message)
        {
        }

        /// <summary>Создаёт исключение с сообщением и внутренним исключением.</summary>
        public TextActivityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
