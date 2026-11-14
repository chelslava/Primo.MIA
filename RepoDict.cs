using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
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
    /// Словари потокобезопасны (ConcurrentDictionary) — безопасны при
    /// параллельном выполнении активностей платформой.
    /// </summary>
    public static class RepoDict
    {
        // Статический конструктор вызывается один раз при первом обращении к классу.
        static RepoDict()
        {
            // Инициализация всех словарей
            StringDict = new ConcurrentDictionary<string, string>();
            BoolDict = new ConcurrentDictionary<string, bool>();
            IntDict = new ConcurrentDictionary<string, int>();
            DoubleDict = new ConcurrentDictionary<string, double>();
            DateDict = new ConcurrentDictionary<string, DateTime>();
            PathDict = new ConcurrentDictionary<string, string>();
            RegexDict = new ConcurrentDictionary<string, Regex>();
            TableDict = new ConcurrentDictionary<string, DataTable>();
            StringListDict = new ConcurrentDictionary<string, List<string>>();
            SecDict = new ConcurrentDictionary<string, SecureString>();
            CredDict = new ConcurrentDictionary<string, (string login, SecureString password)>();
            ObjectDict = new ConcurrentDictionary<string, object>();
            JsonDict = new ConcurrentDictionary<string, JToken>();

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
        public static ConcurrentDictionary<string, string> StringDict { get; private set; }

        /// <summary>
        /// Словарь логических значений (настройки, флаги и т.п.).
        /// </summary>
        public static ConcurrentDictionary<string, bool> BoolDict { get; private set; }

        /// <summary>
        /// Словарь для хранения целочисленных параметров.
        /// </summary>
        public static ConcurrentDictionary<string, int> IntDict { get; private set; }

        /// <summary>
        /// Словарь для хранения чисел с плавающей точкой.
        /// </summary>
        public static ConcurrentDictionary<string, double> DoubleDict { get; private set; }

        /// <summary>
        /// Словарь для хранения дат.
        /// </summary>
        public static ConcurrentDictionary<string, DateTime> DateDict { get; private set; }

        /// <summary>
        /// Словарь путей.
        /// </summary>
        public static ConcurrentDictionary<string, string> PathDict { get; private set; }

        /// <summary>
        /// Словарь регулярных выражений для повторного использования.
        /// </summary>
        public static ConcurrentDictionary<string, Regex> RegexDict { get; private set; }

        /// <summary>
        /// Словарь DataTable по ключу.
        /// </summary>
        public static ConcurrentDictionary<string, DataTable> TableDict { get; private set; }

        /// <summary>
        /// Словарь списков строк.
        /// </summary>
        public static ConcurrentDictionary<string, List<string>> StringListDict { get; private set; }

        /// <summary>
        /// Словарь для безопасного хранения строк (SecureString).
        /// </summary>
        public static ConcurrentDictionary<string, SecureString> SecDict { get; private set; }

        /// <summary>
        /// Словарь для учетных данных.
        /// </summary>
        public static ConcurrentDictionary<string, (string login, SecureString password)> CredDict { get; private set; }

        /// <summary>
        /// Универсальный словарь для хранения произвольных объектов.
        /// </summary>
        public static ConcurrentDictionary<string, object> ObjectDict { get; private set; }

        /// <summary>
        /// Словарь для хранения JSON данных (JToken).
        /// </summary>
        public static ConcurrentDictionary<string, JToken> JsonDict { get; private set; }

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
            return ObjectDict.TryRemove(key, out _);
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

        /// <summary>
        /// Очищает состояние прогона робота (issue #27):
        /// 1) закрывает браузерные сессии — Quit + Dispose драйверов, снятие
        /// с учёта <see cref="BrowserSessionRegistry"/> (стек контекстов
        /// браузера живёт в ObjectDict и уходит вместе с ним);
        /// 2) очищает пользовательские данные всех словарей (<see cref="Clear"/>):
        /// TableDict, ObjectDict, строки/числа/списки, SecureString-пароли.
        ///
        /// Primo Studio исполняет роботов в разделяемом процессе: без очистки
        /// данные и браузеры утекают между запусками разных роботов.
        /// Вызывайте в конце прогона — активность
        /// «Утилиты: Очистка состояния прогона» или из кода финализации.
        /// PathDict и RegexDict (системные) сохраняются.
        /// </summary>
        /// <returns>Количество закрытых браузерных сессий.</returns>
        public static int ClearRunScope()
        {
            // Сначала сессии: CloseAll читает драйверы из ObjectDict
            var closed = BrowserSessionRegistry.CloseAll("ClearRunScope").Count;
            Clear();
            return closed;
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
