using System;
using System.Collections.Generic;

namespace Primo.MIA.Models
{
    /// <summary>
    /// Данные cookie для импорта/экспорта.
    /// </summary>
    public class CookieData
    {
        /// <summary>
        /// Имя cookie.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Значение cookie.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Домен cookie.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Путь cookie.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Время истечения cookie.
        /// </summary>
        public DateTime? Expiry { get; set; }

        /// <summary>
        /// Флаг Secure.
        /// </summary>
        public bool Secure { get; set; }

        /// <summary>
        /// Флаг HttpOnly.
        /// </summary>
        public bool HttpOnly { get; set; }

        /// <summary>
        /// Атрибут SameSite.
        /// </summary>
        public string SameSite { get; set; }
    }

    /// <summary>
    /// Коллекция cookies для экспорта/импорта.
    /// </summary>
    public class CookieCollection
    {
        /// <summary>
        /// Список cookies.
        /// </summary>
        public List<CookieData> Cookies { get; set; }

        /// <summary>
        /// Время экспорта.
        /// </summary>
        public DateTime ExportedAt { get; set; }

        /// <summary>
        /// URL источника.
        /// </summary>
        public string SourceUrl { get; set; }

        public CookieCollection()
        {
            Cookies = new List<CookieData>();
            ExportedAt = DateTime.Now;
        }
    }
}
