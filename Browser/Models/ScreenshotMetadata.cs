using System;

namespace Primo.MIA.Models
{
    /// <summary>
    /// Метаданные скриншота при ошибке.
    /// </summary>
    public class ScreenshotMetadata
    {
        /// <summary>
        /// Путь к файлу скриншота.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Имя активности.
        /// </summary>
        public string ActivityName { get; set; }

        /// <summary>
        /// Время создания скриншота.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Сообщение об ошибке.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// ID сессии браузера.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Текущий URL страницы.
        /// </summary>
        public string CurrentUrl { get; set; }
    }
}
