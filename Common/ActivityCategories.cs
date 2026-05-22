using LTools.Common.UIElements;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Централизованные константы категорий для активностей.
    /// Используются в свойстве GroupName каждой активности.
    /// </summary>
    public static class ActivityCategories
    {
        private const string Prefix = "MIA" + WFPublishedElementBase.TREE_SEPARATOR;

        /// <summary>Категория "MIA → Словари"</summary>
        public const string Dictionaries = Prefix + "Словари";

        /// <summary>Категория "MIA → Списки"</summary>
        public const string Lists = Prefix + "Списки";

        /// <summary>Категория "MIA → Кортежи"</summary>
        public const string Tuples = Prefix + "Кортежи";

        /// <summary>Категория "MIA → HTTP/Web"</summary>
        public const string HttpWeb = Prefix + "HTTP/Web";

        /// <summary>Категория "MIA → Файлы"</summary>
        public const string Files = Prefix + "Файлы";

        /// <summary>Категория "MIA → Утилиты"</summary>
        public const string Utilities = Prefix + "Утилиты";

        /// <summary>Категория "MIA → Браузер"</summary>
        public const string Browser = Prefix + "Браузер";

        /// <summary>Категория "MIA → Календарь"</summary>
        public const string Calendar = Prefix + "Календарь";
        
        /// <summary>Категория "MIA → Текст"</summary>
        public const string Text = Prefix + "Текст";

        /// <summary>Категория "MIA → База данных"</summary>
        public const string Database = Prefix + "База данных";
    }
}
