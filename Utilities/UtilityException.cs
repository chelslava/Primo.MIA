// =============================================================================
// UtilityException.cs — исключение модуля Utilities.
//
// Выбрасывается при ошибках утилитарных активностей (генераторы, конвертеры, конфиг).
// =============================================================================

using System;

namespace Primo.MIA
{
    /// <summary>
    /// Исключение, выбрасываемое при ошибке в модуле Utilities.
    /// </summary>
    public class UtilityException : Exception
    {
        /// <summary>Создаёт исключение с сообщением об ошибке.</summary>
        public UtilityException(string message)
            : base(message)
        {
        }

        /// <summary>Создаёт исключение с сообщением и внутренним исключением.</summary>
        public UtilityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
