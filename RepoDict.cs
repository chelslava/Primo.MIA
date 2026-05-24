using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Security;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    /// <summary>
    /// Статический класс для централизованного хранения глобальных переменных,
    /// коллекций и настроек, используемых в приложении MIA.
    /// Данный класс предназначен для упрощения доступа к общим данным,
    /// чтобы избежать передачи параметров по всему проекту.
    /// Все словари создаются один раз и доступны в любом месте проекта.
    /// </summary>
    public static class RepoDict
    {
        // Статический конструктор вызывается один раз при первом обращении к классу.
        static RepoDict()
        {
            // Инициализация всех словарей
            StringDict = new Dictionary<string, string>();
            BoolDict = new Dictionary<string, bool>();
            IntDict = new Dictionary<string, int>();
            DoubleDict = new Dictionary<string, double>();
            DateDict = new Dictionary<string, DateTime>();
            PathDict = new Dictionary<string, string>();
            RegexDict = new Dictionary<string, Regex>();
            TableDict = new Dictionary<string, DataTable>();
            StringListDict = new Dictionary<string, List<string>>();
            SecDict = new Dictionary<string, SecureString>();
            CredDict = new Dictionary<string, (string login, SecureString password)>();
            ObjectDict = new Dictionary<string, object>();
            JsonDict = new Dictionary<string, JToken>();

            // Запуск методов инициализации по умолчанию
            InitializePaths();
            InitializeRegex();
            InitializeDates();
        }

        //---------------------------------------------------------------------
        //  ОБЩИЕ СЛОВАРИ
        //---------------------------------------------------------------------

        /// <summary>
        /// Универсальный словарь строковых значений.
        /// </summary>
        public static Dictionary<string, string> StringDict { get; private set; }

        /// <summary>
        /// Словарь логических значений (настройки, флаги и т.п.).
        /// </summary>
        public static Dictionary<string, bool> BoolDict { get; private set; }

        /// <summary>
        /// Словарь для хранения целочисленных параметров.
        /// </summary>
        public static Dictionary<string, int> IntDict { get; private set; }

        /// <summary>
        /// Словарь для хранения чисел с плавающей точкой.
        /// </summary>
        public static Dictionary<string, double> DoubleDict { get; private set; }

        /// <summary>
        /// Словарь для хранения дат.
        /// </summary>
        public static Dictionary<string, DateTime> DateDict { get; private set; }

        /// <summary>
        /// Словарь путей.
        /// </summary>
        public static Dictionary<string, string> PathDict { get; private set; }

        /// <summary>
        /// Словарь регулярных выражений для повторного использования.
        /// </summary>
        public static Dictionary<string, Regex> RegexDict { get; private set; }

        /// <summary>
        /// Словарь DataTable по ключу.
        /// </summary>
        public static Dictionary<string, DataTable> TableDict { get; private set; }

        /// <summary>
        /// Словарь списков строк.
        /// </summary>
        public static Dictionary<string, List<string>> StringListDict { get; private set; }

        /// <summary>
        /// Словарь для безопасного хранения строк (SecureString).
        /// </summary>
        public static Dictionary<string, SecureString> SecDict { get; private set; }

        /// <summary>
        /// Словарь для учетных данных.
        /// </summary>
        public static Dictionary<string, (string login, SecureString password)> CredDict { get; private set; }

        /// <summary>
        /// Универсальный словарь для хранения произвольных объектов.
        /// </summary>
        public static Dictionary<string, object> ObjectDict { get; private set; }

        /// <summary>
        /// Словарь для хранения JSON данных (JToken).
        /// </summary>
        public static Dictionary<string, JToken> JsonDict { get; private set; }

        //---------------------------------------------------------------------
        //  HELPER METHODS
        //---------------------------------------------------------------------

        /// <summary>
        /// Сохраняет объект в ObjectDict по ключу.
        /// </summary>
        public static void Set(string key, object value)
        {
            ObjectDict[key] = value;
        }

        /// <summary>
        /// Получает объект из ObjectDict по ключу.
        /// </summary>
        public static object Get(string key)
        {
            return ObjectDict.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Получает типизированный объект из ObjectDict по ключу.
        /// </summary>
        public static T Get<T>(string key) where T : class
        {
            return ObjectDict.TryGetValue(key, out var value) ? value as T : null;
        }

        /// <summary>
        /// Проверяет наличие ключа в ObjectDict.
        /// </summary>
        public static bool Contains(string key)
        {
            return ObjectDict.ContainsKey(key);
        }

        /// <summary>
        /// Удаляет объект из ObjectDict по ключу.
        /// </summary>
        public static bool Remove(string key)
        {
            return ObjectDict.Remove(key);
        }


        //---------------------------------------------------------------------
        //  ОЧИСТКА
        //---------------------------------------------------------------------

        /// <summary>
        /// Очищает пользовательские данные из всех словарей, сохраняя системные значения
        /// PathDict (пути) и RegexDict (pre-compiled regex).
        /// Вызывайте в конце сценария или между независимыми запусками,
        /// чтобы избежать утечек данных между запусками.
        /// </summary>
        /// <example>
        /// // В конце сценария:
        /// RepoDict.Clear();
        ///
        /// // Или в начале нового сценария:
        /// RepoDict.Clear();
        /// </example>
        public static void Clear()
        {
            StringDict.Clear();
            BoolDict.Clear();
            IntDict.Clear();
            DoubleDict.Clear();

            foreach (var sec in SecDict.Values)
                sec?.Dispose();
            SecDict.Clear();

            foreach (var cred in CredDict.Values)
                cred.password?.Dispose();
            CredDict.Clear();

            TableDict.Clear();
            StringListDict.Clear();
            ObjectDict.Clear();
            JsonDict.Clear();

            // Восстанавливаем дату инициализации для нового запуска
            InitializeDates();

            // PathDict и RegexDict не очищаются — они системные
        }

        /// <summary>
        /// Полностью сбрасывает ВСЕ словари (включая PathDict и RegexDict) к начальным значениям.
        /// Используйте ClearAll() когда нужна полная реинициализация,
        /// например после изменения рабочей директории.
        /// </summary>
        public static void ClearAll()
        {
            Clear();
            InitializePaths();
            InitializeRegex();
        }

        //---------------------------------------------------------------------
        //  ИНИЦИАЛИЗАЦИЯ
        //---------------------------------------------------------------------

        /// <summary>
        /// Инициализация словаря путей.
        /// </summary>
        private static void InitializePaths()
        {
            string root = Environment.CurrentDirectory;

            PathDict["Root"] = root;
            PathDict["Templates"] = System.IO.Path.Combine(root, "Templates");
            PathDict["ScreenShots"] = System.IO.Path.Combine(root, "ScreenShots");
            PathDict["Config"] = System.IO.Path.Combine(root, "Config");
            PathDict["Temp"] = System.IO.Path.Combine(root, "Temp");
            PathDict["Data"] = System.IO.Path.Combine(root, "Data");
        }

        /// <summary>
        /// Инициализация регулярных выражений по умолчанию.
        /// </summary>
        private static void InitializeRegex()
        {
            RegexDict["Digits"] = new Regex(@"\d+", RegexOptions.Compiled);
            RegexDict["Letters"] = new Regex(@"[A-Za-zА-Яа-я]+", RegexOptions.Compiled);
            RegexDict["Email"] = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
            RegexDict["GUID"] = new Regex(@"^[{(]?[0-9A-Fa-f]{8}[-]?(?:[0-9A-Fa-f]{4}[-]?){3}[0-9A-Fa-f]{12}[)}]?$", RegexOptions.Compiled);
        }

        /// <summary>
        /// Инициализация дат по умолчанию.
        /// </summary>
        private static void InitializeDates()
        {
            DateDict["Init"] = DateTime.Now;
        }
    }
}
