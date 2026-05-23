// =============================================================================
// DatabaseException.cs — исключение модуля Database.
//
// Выбрасывается при ошибках выполнения операций с базой данных.
// =============================================================================

using System;

namespace Primo.MIA
{
    /// <summary>
    /// Исключение, выбрасываемое при ошибке в модуле Database.
    /// </summary>
    public class DatabaseException : Exception
    {
        /// <summary>Создаёт исключение с сообщением об ошибке.</summary>
        public DatabaseException(string message)
            : base(message)
        {
        }

        /// <summary>Создаёт исключение с сообщением и внутренним исключением.</summary>
        public DatabaseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
