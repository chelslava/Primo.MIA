// =============================================================================
// ActivityFileLogger.cs — файловый sink для логов активностей.
//
// Console.WriteLine в среде Primo Studio не виден пользователю и не
// сохраняется, поэтому диагностика роботов теряется. Этот класс пишет
// те же строки в файл (по умолчанию %LOCALAPPDATA%\Primo.MIA\logs),
// дублируя их в консоль (полезно при запуске вне Studio, например в тестах).
//
// Включение: статическое свойство Enabled или переменная окружения
// PRIMO_MIA_LOG_FILE=1 (позволяет включить без изменения конфигурации робота).
// Ротация: файл на календарный день; при превышении MaxFileBytes
// текущий файл переносится в *.old (перезаписывается).
// Все операции с файлами защищены try/catch — логирование не может
// уронить выполнение робота.
// =============================================================================

using System;
using System.IO;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Потокобезопасный файловый логгер активностей.
    /// </summary>
    public static class ActivityFileLogger
    {
        private const string LevelInfo = "INFO";
        private const string LevelWarning = "WARN";
        private const string LevelError = "ERROR";
        private const string LevelDebug = "DEBUG";

        private static readonly object SyncRoot = new object();

        /// <summary>Лимит размера файла лога до ротации (10 МБ).</summary>
        private const long MaxFileBytes = 10 * 1024 * 1024;

        /// <summary>
        /// Файл лог ведётся только при True.
        /// По умолчанию — из переменной окружения PRIMO_MIA_LOG_FILE ("1"/"true").
        /// </summary>
        public static bool Enabled { get; set; } = IsEnabledFromEnvironment();

        /// <summary>
        /// Каталог файлов лога. По умолчанию %LOCALAPPDATA%\Primo.MIA\logs.
        /// </summary>
        public static string LogDirectory { get; set; } = DefaultLogDirectory();

        /// <summary>Информационное сообщение.</summary>
        public static void Info(string source, string message)
            => Write(LevelInfo, source, message);

        /// <summary>Предупреждение.</summary>
        public static void Warning(string source, string message)
            => Write(LevelWarning, source, message);

        /// <summary>Ошибка.</summary>
        public static void Error(string source, string message)
            => Write(LevelError, source, message);

        /// <summary>Отладочное сообщение.</summary>
        public static void Debug(string source, string message)
            => Write(LevelDebug, source, message);

        /// <summary>
        /// Записывает строку в консоль и (при Enabled) в файл лога.
        /// Никогда не бросает исключений.
        /// </summary>
        public static void Write(string level, string source, string message)
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{source}] {message}";

            try
            {
                Console.WriteLine(line);
            }
            catch
            {
                // консоль может быть недоступна — не критично
            }

            if (!Enabled)
                return;

            try
            {
                lock (SyncRoot)
                {
                    Directory.CreateDirectory(LogDirectory);
                    var filePath = Path.Combine(LogDirectory, $"Primo.MIA.{DateTime.Now:yyyyMMdd}.log");
                    RotateIfNeeded(filePath);
                    File.AppendAllText(filePath, line + Environment.NewLine);
                }
            }
            catch
            {
                // файловый лог не может ломать выполнение робота
            }
        }

        private static void RotateIfNeeded(string filePath)
        {
            var info = new FileInfo(filePath);
            if (!info.Exists || info.Length < MaxFileBytes)
                return;

            var oldPath = filePath + ".old";
            if (File.Exists(oldPath))
                File.Delete(oldPath);
            File.Move(filePath, oldPath);
        }

        private static bool IsEnabledFromEnvironment()
        {
            var value = Environment.GetEnvironmentVariable("PRIMO_MIA_LOG_FILE");
            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string DefaultLogDirectory()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(localAppData))
                localAppData = AppDomain.CurrentDomain.BaseDirectory ?? ".";
            return Path.Combine(localAppData, "Primo.MIA", "logs");
        }
    }
}
