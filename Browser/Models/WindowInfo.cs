using System;

namespace Primo.MIA.Models
{
    /// <summary>
    /// Информация об окне/вкладке браузера.
    /// </summary>
    public class WindowInfo
    {
        /// <summary>
        /// Handle окна.
        /// </summary>
        public string Handle { get; set; }

        /// <summary>
        /// Заголовок окна.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// URL страницы.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Время открытия окна.
        /// </summary>
        public DateTime OpenedAt { get; set; }

        /// <summary>
        /// Является ли окно активным.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
