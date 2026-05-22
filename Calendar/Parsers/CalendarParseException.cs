// =============================================================================
// CalendarParseException.cs — исключение при ошибке парсинга календаря.
//
// Выбрасывается когда парсер не может обработать содержимое файла календаря.
// =============================================================================

using System;

namespace Primo.MIA.Calendar.Parsers
{
    /// <summary>
    /// Исключение, выбрасываемое при ошибке парсинга файла календаря.
    /// </summary>
    public class CalendarParseException : Exception
    {
        /// <summary>
        /// Формат файла, который не удалось распарсить.
        /// </summary>
        public global::Primo.MIA.CalendarFormat Format { get; }

        /// <summary>
        /// Создаёт новое исключение с сообщением об ошибке.
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        public CalendarParseException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Создаёт новое исключение с сообщением и форматом файла.
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="format">Формат файла</param>
        public CalendarParseException(string message, global::Primo.MIA.CalendarFormat format)
            : base(message)
        {
            Format = format;
        }

        /// <summary>
        /// Создаёт новое исключение с сообщением и внутренним исключением.
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public CalendarParseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Создаёт новое исключение с сообщением, форматом и внутренним исключением.
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="format">Формат файла</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public CalendarParseException(string message, global::Primo.MIA.CalendarFormat format, Exception innerException)
            : base(message, innerException)
        {
            Format = format;
        }
    }
}
