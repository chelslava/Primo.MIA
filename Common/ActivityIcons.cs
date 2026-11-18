namespace Primo.MIA.Common
{
    /// <summary>
    /// Централизованные пути к иконкам активностей.
    /// Используются в свойстве sdkComponentIcon и в IconSource XAML-панелей.
    /// Иконка соответствует категории и функции активности: у каждой
    /// категории — своя плитка (см. images/*.png).
    /// </summary>
    public static class ActivityIcons
    {
        private const string BasePath = "pack://application:,,,/Primo.MIA;component/images/";

        /// <summary>Иконка для активностей работы со словарями</summary>
        public const string Dictionary = BasePath + "dict.png";

        /// <summary>Иконка для активностей работы со списками</summary>
        public const string List = BasePath + "list.png";

        /// <summary>Иконка для активностей работы с кортежами</summary>
        public const string Tuple = BasePath + "tuple.png";

        /// <summary>Иконка для активностей работы с конфигурацией</summary>
        public const string Config = BasePath + "config.png";

        /// <summary>Иконка для активностей логирования</summary>
        public const string Log = BasePath + "log.png";

        /// <summary>Иконка для активностей-генераторов</summary>
        public const string Generator = BasePath + "generator.png";

        /// <summary>Иконка для активностей работы с Excel</summary>
        public const string Excel = BasePath + "excel.png";

        /// <summary>Иконка для HTTP активностей</summary>
        public const string Http = BasePath + "http.png";

        /// <summary>Иконка для активностей поиска файлов</summary>
        public const string FileSearch = BasePath + "file_search.png";

        /// <summary>Иконка для активностей ожидания файлов</summary>
        public const string FileWait = BasePath + "file_wait.png";

        /// <summary>Иконка для активностей работы с JSON</summary>
        public const string Json = BasePath + "json.png";

        /// <summary>Иконка для активностей работы с XML</summary>
        public const string Xml = BasePath + "xml.png";

        /// <summary>Иконка для активностей работы с текстом (парсинг по шаблонам)</summary>
        public const string Text = BasePath + "text.png";

        /// <summary>Иконка для активностей работы с браузером (Selenium)</summary>
        public const string Browser = BasePath + "browser.png";

        /// <summary>Иконка для активностей работы с таблицами (DataTable)</summary>
        public const string Table = BasePath + "table.png";

        /// <summary>Иконка для активностей работы с календарём</summary>
        public const string Calendar = BasePath + "calendar.png";

        /// <summary>Иконка для активностей работы с текстом (Шаблонизатор)</summary>
        public const string TextTemplate = BasePath + "template.png";

        /// <summary>Иконка для активностей работы с текстом (Транслитерация)</summary>
        public const string TextTranslit = BasePath + "translit.png";

        /// <summary>Иконка для активностей работы с текстом (Regex)</summary>
        public const string Regex = BasePath + "regex.png";

        /// <summary>Иконка для активности Файл: Очистка по времени</summary>
        public const string Clean = BasePath + "delete.png";

        /// <summary>Иконка для активностей работы с базой данных (SQL, транзакции)</summary>
        public const string Base = BasePath + "bd.png";

        /// <summary>Иконка для активностей MongoDB (категория «Базы данных»)</summary>
        public const string MongoDB = BasePath + "bd.png";

        /// <summary>Иконка для активностей Redis (категория «Базы данных»)</summary>
        public const string Redis = BasePath + "bd.png";

        /// <summary>Иконка для активностей криптографии (AES/PGP/хэш/Base64)</summary>
        public const string Crypto = BasePath + "crypto.png";

        /// <summary>Иконка для активностей SFTP (передача файлов по SSH)</summary>
        public const string Sftp = BasePath + "sftp.png";

        /// <summary>Иконка для активностей очередей сообщений (RabbitMQ/Kafka)</summary>
        public const string MessageQueue = BasePath + "queue.png";

        /// <summary>Иконка для активностей работы с архивами (ZIP)</summary>
        public const string Archive = BasePath + "archive.png";

        /// <summary>Иконка для активностей FTP/FTPS (передача файлов)</summary>
        public const string Ftp = BasePath + "ftp.png";

        /// <summary>Иконка для почтовых активностей (SMTP/IMAP/POP3/EML)</summary>
        public const string Mail = BasePath + "mail.png";
    }
}
