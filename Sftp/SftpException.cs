using System;

namespace Primo.MIA
{
    /// <summary>
    /// Исключение операций SFTP-модуля Primo.MIA. Аналог <see cref="CalendarParseException"/>
    /// для Calendar: заменяет голые <see cref="Exception"/> и позволяет пользователям
    /// отличать ошибки SFTP от прочих ошибок сценария.
    /// </summary>
    [Serializable]
    public class SftpException : Exception
    {
        /// <summary>Создаёт исключение с сообщением.</summary>
        public SftpException(string message)
            : base(message)
        {
        }

        /// <summary>Создаёт исключение с сообщением и внутренней ошибкой.</summary>
        public SftpException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
