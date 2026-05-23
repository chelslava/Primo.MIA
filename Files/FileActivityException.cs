// =============================================================================
// FileActivityException.cs — исключение модуля Files.
//
// Выбрасывается при ошибках файловых операций (поиск, очистка, ожидание).
// =============================================================================

using System;

namespace Primo.MIA
{
    /// <summary>
    /// Исключение, выбрасываемое при ошибке в модуле Files.
    /// </summary>
    public class FileActivityException : Exception
    {
        /// <summary>Путь к файлу или директории, с которым возникла ошибка.</summary>
        public string FilePath { get; }

        /// <summary>Создаёт исключение с сообщением об ошибке.</summary>
        public FileActivityException(string message)
            : base(message)
        {
        }

        /// <summary>Создаёт исключение с сообщением и путём к файлу.</summary>
        public FileActivityException(string message, string filePath)
            : base(message)
        {
            FilePath = filePath;
        }

        /// <summary>Создаёт исключение с сообщением и внутренним исключением.</summary>
        public FileActivityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>Создаёт исключение с сообщением, путём и внутренним исключением.</summary>
        public FileActivityException(string message, string filePath, Exception innerException)
            : base(message, innerException)
        {
            FilePath = filePath;
        }
    }
}
