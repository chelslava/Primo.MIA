using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Primo.MIA
{
    public class FileCleanupResult
    {
        public int DeletedCount { get; set; }

        public List<string> DeletedPaths { get; set; } = new List<string>();

        public long FreedBytes { get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        public string ErrorMessage { get; set; }

        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }

    public static class FileCleanupLogic
    {
        private static readonly Dictionary<FileTimeAttribute, Func<FileSystemInfo, DateTime>>
            TimeGetters = new Dictionary<FileTimeAttribute, Func<FileSystemInfo, DateTime>>
            {
                { FileTimeAttribute.CreationTime,   fsi => fsi.CreationTime },
                { FileTimeAttribute.LastWriteTime,  fsi => fsi.LastWriteTime },
                { FileTimeAttribute.LastAccessTime, fsi => fsi.LastAccessTime }
            };

        public static FileCleanupResult Cleanup(
            string folderPath,
            bool deleteFiles = true,
            bool deleteEmptyFolders = false,
            bool deleteFoldersWithContent = false,
            FileTimeAttribute timeAttribute = FileTimeAttribute.LastWriteTime,
            CleanupThresholdMode thresholdMode = CleanupThresholdMode.OlderThanDays,
            int olderThanDays = 30,
            DateTime? thresholdDate = null,
            string filePattern = "*",
            bool recursive = false,
            long minSizeBytes = 0,
            int maxItems = 0,
            bool dryRun = false,
            bool ignoreErrors = true)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return new FileCleanupResult { ErrorMessage = "Путь к папке не указан" };

            if (!Directory.Exists(folderPath))
                return new FileCleanupResult { ErrorMessage = $"Папка не найдена: {folderPath}" };

            if (!deleteFiles && !deleteEmptyFolders && !deleteFoldersWithContent)
                return new FileCleanupResult { ErrorMessage = "Выберите хотя бы один тип объектов для удаления" };

            DateTime? thresholdDays = null;
            DateTime? thresholdDateValue = null;

            if (thresholdMode == CleanupThresholdMode.OlderThanDays || thresholdMode == CleanupThresholdMode.Both)
            {
                if (olderThanDays < 0)
                    return new FileCleanupResult { ErrorMessage = "Количество дней должно быть >= 0" };

                thresholdDays = DateTime.Now.AddDays(-olderThanDays);
            }

            if (thresholdMode == CleanupThresholdMode.BeforeDate || thresholdMode == CleanupThresholdMode.Both)
            {
                if (thresholdDate == null)
                    return new FileCleanupResult { ErrorMessage = "Пороговая дата не указана" };

                thresholdDateValue = thresholdDate;
            }

            var deletedPaths = new List<string>();
            var errors = new List<string>();
            long freedBytes = 0;

            SearchOption searchOption = recursive
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            Func<FileSystemInfo, DateTime> getTime = TimeGetters[timeAttribute];
            List<string> patterns = ParseFilePatterns(filePattern);

            if (deleteFiles)
            {
                ProcessFiles(folderPath, patterns, searchOption,
                    getTime, thresholdDays, thresholdDateValue,
                    minSizeBytes, maxItems, dryRun, ignoreErrors,
                    deletedPaths, errors, ref freedBytes);
            }

            if (deleteEmptyFolders && !dryRun)
            {
                ProcessEmptyFolders(folderPath, getTime, thresholdDays, thresholdDateValue,
                    ignoreErrors, deletedPaths, errors);
            }

            if (deleteFoldersWithContent)
            {
                ProcessFolders(folderPath, getTime, thresholdDays, thresholdDateValue,
                    maxItems, dryRun, ignoreErrors, deletedPaths, errors, ref freedBytes);
            }

            return new FileCleanupResult
            {
                DeletedCount = deletedPaths.Count,
                DeletedPaths = deletedPaths,
                FreedBytes = freedBytes,
                Errors = errors
            };
        }

        public static bool MeetsTimeThreshold(
            FileSystemInfo fsi,
            FileTimeAttribute timeAttribute,
            DateTime? thresholdDays,
            DateTime? thresholdDate)
        {
            return MeetsTimeThreshold(fsi, TimeGetters[timeAttribute], thresholdDays, thresholdDate);
        }

        public static long GetDirectorySize(DirectoryInfo di)
        {
            try
            {
                return di.GetFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
            }
            catch
            {
                return 0;
            }
        }

        public static string FormatBytes(long bytes)
        {
            var thresholds = new Dictionary<long, string>
            {
                { 1024L * 1024 * 1024, "ГБ" },
                { 1024L * 1024, "МБ" },
                { 1024L, "КБ" }
            };

            foreach (var kv in thresholds)
            {
                if (bytes >= kv.Key)
                    return $"{bytes / (double)kv.Key:F1} {kv.Value}";
            }

            return $"{bytes} байт";
        }

        public static List<string> ParseFilePatterns(string patternsString)
        {
            if (string.IsNullOrWhiteSpace(patternsString))
                return new List<string> { "*" };

            var result = patternsString
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return result.Count > 0 ? result : new List<string> { "*" };
        }

        private static bool MeetsTimeThreshold(
            FileSystemInfo fsi,
            Func<FileSystemInfo, DateTime> getTime,
            DateTime? thresholdDays,
            DateTime? thresholdDate)
        {
            DateTime fileTime = getTime(fsi);
            bool meetsDays = thresholdDays == null || fileTime < thresholdDays.Value;
            bool meetsDate = thresholdDate == null || fileTime < thresholdDate.Value;

            if (thresholdDays != null && thresholdDate != null)
                return meetsDays && meetsDate;

            return meetsDays && meetsDate;
        }

        private static void ProcessFiles(
            string folderPath, List<string> patterns, SearchOption searchOption,
            Func<FileSystemInfo, DateTime> getTime,
            DateTime? thresholdDays, DateTime? thresholdDate,
            long minSizeBytes, int maxItems,
            bool dryRun, bool ignoreErrors,
            List<string> deletedPaths, List<string> errors, ref long freedBytes)
        {
            IEnumerable<string> files;
            try
            {
                files = patterns
                    .SelectMany(p =>
                    {
                        try { return Directory.GetFiles(folderPath, p, searchOption); }
                        catch { return Array.Empty<string>(); }
                    })
                    .Distinct(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                errors.Add($"{folderPath}: {ex.Message}");
                return;
            }

            var candidates = files
                .Select(f => new FileInfo(f))
                .Where(fi => { try { return fi.Exists; } catch { return false; } })
                .Where(fi => MeetsTimeThreshold(fi, getTime, thresholdDays, thresholdDate))
                .Where(fi => minSizeBytes <= 0 || fi.Length >= minSizeBytes)
                .OrderBy(fi => getTime(fi))
                .AsEnumerable();

            if (maxItems > 0)
                candidates = candidates.Take(maxItems);

            foreach (FileInfo fi in candidates)
            {
                try
                {
                    long size = fi.Length;
                    if (!dryRun)
                        fi.Delete();
                    deletedPaths.Add(fi.FullName);
                    freedBytes += size;
                }
                catch (Exception ex)
                {
                    if (!ignoreErrors)
                        throw;

                    errors.Add($"{fi.FullName}: {ex.Message}");
                }
            }
        }

        private static void ProcessEmptyFolders(
            string rootPath, Func<FileSystemInfo, DateTime> getTime,
            DateTime? thresholdDays, DateTime? thresholdDate,
            bool ignoreErrors,
            List<string> deletedPaths, List<string> errors)
        {
            var subDirs = Directory
                .GetDirectories(rootPath, "*", SearchOption.AllDirectories)
                .Select(d => new DirectoryInfo(d))
                .Where(di => MeetsTimeThreshold(di, getTime, thresholdDays, thresholdDate))
                .OrderByDescending(di => di.FullName.Count(c => c == Path.DirectorySeparatorChar))
                .ToList();

            foreach (DirectoryInfo di in subDirs)
            {
                try
                {
                    if (di.Exists && !di.GetFileSystemInfos().Any())
                    {
                        di.Delete(false);
                        deletedPaths.Add(di.FullName);
                    }
                }
                catch (Exception ex)
                {
                    if (!ignoreErrors)
                        throw;

                    errors.Add($"{di.FullName}: {ex.Message}");
                }
            }
        }

        private static void ProcessFolders(
            string rootPath, Func<FileSystemInfo, DateTime> getTime,
            DateTime? thresholdDays, DateTime? thresholdDate,
            int maxItems, bool dryRun, bool ignoreErrors,
            List<string> deletedPaths, List<string> errors, ref long freedBytes)
        {
            IEnumerable<DirectoryInfo> candidates = Directory
                .GetDirectories(rootPath, "*", SearchOption.TopDirectoryOnly)
                .Select(d => new DirectoryInfo(d))
                .Where(di => MeetsTimeThreshold(di, getTime, thresholdDays, thresholdDate))
                .OrderBy(di => getTime(di));

            if (maxItems > 0)
                candidates = candidates.Take(maxItems);

            foreach (DirectoryInfo di in candidates)
            {
                try
                {
                    long folderSize = GetDirectorySize(di);
                    if (!dryRun)
                        di.Delete(recursive: true);
                    deletedPaths.Add(di.FullName);
                    freedBytes += folderSize;
                }
                catch (Exception ex)
                {
                    if (!ignoreErrors)
                        throw;

                    errors.Add($"{di.FullName}: {ex.Message}");
                }
            }
        }
    }
}
