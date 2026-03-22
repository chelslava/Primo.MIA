using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Primo.MIA
{
    public class WaitForFileResult
    {
        public bool FileFound { get; set; }

        public string FilePath { get; set; }

        public string FileName { get; set; }

        public long FileSize { get; set; }

        public long WaitTimeMs { get; set; }
    }

    public class WaitForFileLogic
    {
        public WaitForFileResult WaitForFile(
            string directoryPath,
            string filePattern,
            FileFilterType filterType,
            WaitFileMode mode,
            int timeoutMs,
            int checkIntervalMs,
            bool waitForStability = false,
            int stabilityTimeoutMs = 2000)
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

            if (waitForStability && stabilityTimeoutMs <= 0)
                throw new ArgumentException("Время стабильности должно быть больше нуля");

            var startTime = DateTime.UtcNow;
            HashSet<string> existingFiles = null;

            if (mode == WaitFileMode.WaitForNewFile)
            {
                existingFiles = new HashSet<string>(
                    new DirectoryInfo(directoryPath).GetFiles().Select(f => f.FullName),
                    StringComparer.OrdinalIgnoreCase);
            }

            Func<FileInfo, bool> fileFilter = CreateFileFilter(filePattern, filterType);

            while (true)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                if (elapsed > timeoutMs)
                {
                    return new WaitForFileResult
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

                    if (waitForStability)
                    {
                        var remainingTimeout = Math.Max(1, timeoutMs - (int)elapsed);
                        if (!WaitForFileStability(file, stabilityTimeoutMs, remainingTimeout, checkIntervalMs))
                        {
                            return new WaitForFileResult
                            {
                                FileFound = false,
                                FilePath = string.Empty,
                                FileName = string.Empty,
                                FileSize = 0,
                                WaitTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
                            };
                        }
                    }

                    file.Refresh();

                    return new WaitForFileResult
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
                    try
                    {
                        var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                        return file => regex.IsMatch(file.Name);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new ArgumentException($"Некорректный regex паттерн: {pattern}. Ошибка: {ex.Message}");
                    }

                default:
                    throw new ArgumentException($"Неизвестный тип фильтрации: {filterType}");
            }
        }

        private bool WaitForFileStability(FileInfo fileInfo, int stabilityTimeoutMs, int remainingTimeoutMs, int checkIntervalMs)
        {
            var stabilityStart = DateTime.UtcNow;
            long previousSize = -1;

            while (true)
            {
                fileInfo.Refresh();

                if (!fileInfo.Exists)
                    return false;

                long currentSize = fileInfo.Length;
                if ((DateTime.UtcNow - stabilityStart).TotalMilliseconds > remainingTimeoutMs)
                    return false;

                if (currentSize != previousSize)
                {
                    previousSize = currentSize;
                    stabilityStart = DateTime.UtcNow;
                }
                else if ((DateTime.UtcNow - stabilityStart).TotalMilliseconds >= stabilityTimeoutMs)
                {
                    return true;
                }

                Thread.Sleep(checkIntervalMs);
            }
        }
    }
}
