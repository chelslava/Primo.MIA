using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Primo.MIA;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>
    /// Бизнес-логика ожидания файла
    /// </summary>
    public class WaitForFileLogic
    {
        /// <summary>
        /// Результат ожидания файла
        /// </summary>
        public class WaitResult
        {
            public bool FileFound { get; set; }
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public long FileSize { get; set; }
            public long WaitTimeMs { get; set; }
        }

        /// <summary>
        /// Ожидание появления файла
        /// </summary>
        public WaitResult WaitForFile(
            string directoryPath,
            string filePattern,
            FileFilterType filterType,
            WaitFileMode mode,
            int timeoutMs,
            int checkIntervalMs)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Путь к директории не может быть пустым");

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Директория не найдена: {directoryPath}");

            if (string.IsNullOrWhiteSpace(filePattern))
                throw new ArgumentException("Маска файла не может быть пустой");

            if (timeoutMs <= 0)
                throw new ArgumentException("Таймаут должен быть больше нуля");

            if (checkIntervalMs <= 0)
                throw new ArgumentException("Интервал проверки должен быть больше нуля");

            var startTime = DateTime.UtcNow;
            HashSet<string> existingFiles = null;

            // Для режима WaitForNewFile запоминаем существующие файлы
            if (mode == WaitFileMode.WaitForNewFile)
            {
                existingFiles = new HashSet<string>(
                    new DirectoryInfo(directoryPath).GetFiles().Select(f => f.FullName),
                    StringComparer.OrdinalIgnoreCase
                );
            }

            // Создание фильтра
            Func<FileInfo, bool> fileFilter = CreateFileFilter(filePattern, filterType);

            // Поиск файла
            while (true)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                if (elapsed > timeoutMs)
                {
                    return new WaitResult
                    {
                        FileFound = false,
                        FilePath = string.Empty,
                        FileName = string.Empty,
                        FileSize = 0,
                        WaitTimeMs = (long)elapsed
                    };
                }

                var dirInfo = new DirectoryInfo(directoryPath);
                var matchedFiles = dirInfo.GetFiles()
                    .Where(f => fileFilter(f))
                    .Where(f => mode == WaitFileMode.WaitForAnyFile || !existingFiles.Contains(f.FullName))
                    .OrderBy(f => f.CreationTime)
                    .ToList();

                if (matchedFiles.Any())
                {
                    var file = matchedFiles.First();
                    file.Refresh();

                    return new WaitResult
                    {
                        FileFound = true,
                        FilePath = file.FullName,
                        FileName = file.Name,
                        FileSize = file.Length,
                        WaitTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
                    };
                }

                Thread.Sleep(checkIntervalMs);
            }
        }

        /// <summary>
        /// Создает функцию фильтрации файлов
        /// </summary>
        private Func<FileInfo, bool> CreateFileFilter(string pattern, FileFilterType filterType)
        {
            switch (filterType)
            {
                case FileFilterType.Exact:
                    return file => file.Name.Equals(pattern, StringComparison.OrdinalIgnoreCase);

                case FileFilterType.Wildcard:
                    string regexPattern = "^" + Regex.Escape(pattern)
                        .Replace("\\*", ".*")
                        .Replace("\\?", ".") + "$";
                    var wildcardRegex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    return file => wildcardRegex.IsMatch(file.Name);

                case FileFilterType.Regex:
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    return file => regex.IsMatch(file.Name);

                default:
                    throw new ArgumentException($"Неизвестный тип фильтрации: {filterType}");
            }
        }
    }
}
