// =============================================================================
// FileCleanupLogic.cs — логика очистки файлов по временному критерию.
//
// Удаляет файлы и/или папки по временному критерию.
// Поддерживает DryRun режим для безопасной проверки.
//
// Критерии времени:
//   CreationTime   — дата создания
//   LastWriteTime  — дата последнего изменения
//   LastAccessTime — дата последнего доступа
//
// Способы задания порога:
//   OlderThanDays — старше N дней от текущего момента
//   BeforeDate    — временной атрибут раньше указанной даты
//   Both          — оба условия одновременно (И)
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>Временной атрибут файла/папки.</summary>
    public enum FileTimeAttribute
    {
        /// <summary>Дата создания.</summary>
        CreationTime,
        /// <summary>Дата последнего изменения.</summary>
        LastWriteTime,
        /// <summary>Дата последнего доступа.</summary>
        LastAccessTime
    }

    /// <summary>Способ задания порога удаления.</summary>
    public enum CleanupThresholdMode
    {
        /// <summary>Старше N дней от текущего момента.</summary>
        OlderThanDays,
        /// <summary>Временной атрибут раньше указанной даты.</summary>
        BeforeDate,
        /// <summary>Оба условия одновременно (И).</summary>
        Both
    }

    /// <summary>Результат очистки файлов.</summary>
    public class FileCleanupResult
    {
        /// <summary>Количество удалённых (или найденных в DryRun) объектов.</summary>
        public int DeletedCount { get; set; }

        /// <summary>Список путей удалённых объектов.</summary>
        public List<string> DeletedPaths { get; set; } = new List<string>();

        /// <summary>Суммарный размер удалённых файлов в байтах.</summary>
        public long FreedBytes { get; set; }

        /// <summary>Список ошибок удаления.</summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>Сообщение об ошибке (null если успешно).</summary>
        public string ErrorMessage { get; set; }

        /// <summary>true если операция успешна.</summary>
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }

    /// <summary>
    /// Логика очистки файлов по временному критерию.
    /// Удаляет файлы и папки с поддержкой режима DryRun.
    /// </summary>
    public static class FileCleanupLogic
    {
        // =====================================================================
        // Статические словари: FileTimeAttribute → геттер времени
        // =====================================================================

        /// <summary>
        /// Маппинг атрибута времени на функцию получения значения из FileSystemInfo.
        /// </summary>
        private static readonly Dictionary<FileTimeAttribute, Func<FileSystemInfo, DateTime>>
            TimeGetters = new Dictionary<FileTimeAttribute, Func<FileSystemInfo, DateTime>>
            {
                { FileTimeAttribute.CreationTime,   fsi => fsi.CreationTime   },
                { FileTimeAttribute.LastWriteTime,  fsi => fsi.LastWriteTime  },
                { FileTimeAttribute.LastAccessTime, fsi => fsi.LastAccessTime }
            };

        // =====================================================================
        // Основной метод
        // =====================================================================

        /// <summary>
        /// Выполняет очистку папки по временному критерию.
        /// </summary>
        /// <param name="folderPath">Путь к папке для очистки.</param>
        /// <param name="deleteFiles">Удалять файлы.</param>
        /// <param name="deleteEmptyFolders">Удалять пустые папки.</param>
        /// <param name="deleteFoldersWithContent">Удалять папки целиком.</param>
        /// <param name="timeAttribute">Временной атрибут для сравнения.</param>
        /// <param name="thresholdMode">Способ задания порога.</param>
        /// <param name="olderThanDays">Удалять объекты старше N дней.</param>
        /// <param name="thresholdDate">Удалять объекты раньше указанной даты.</param>
        /// <param name="filePattern">Маска имён файлов.</param>
        /// <param name="recursive">Обходить подпапки.</param>
        /// <param name="minSizeBytes">Не удалять файлы меньше N байт.</param>
        /// <param name="maxItems">Ограничить количество удалений.</param>
        /// <param name="dryRun">Режим пробного запуска.</param>
        /// <param name="ignoreErrors">Продолжать при ошибках.</param>
        /// <returns>Результат очистки.</returns>
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
            // Валидация
            if (string.IsNullOrWhiteSpace(folderPath))
                return new FileCleanupResult { ErrorMessage = "Путь к папке не указан" };

            if (!Directory.Exists(folderPath))
                return new FileCleanupResult { ErrorMessage = $"Папка не найдена: {folderPath}" };

            if (!deleteFiles && !deleteEmptyFolders && !deleteFoldersWithContent)
                return new FileCleanupResult { ErrorMessage = "Выберите хотя бы один тип объектов для удаления" };

            // Вычисляем пороговые даты
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

            var result = new FileCleanupResult();
            var deletedPaths = new List<string>();
            var errors = new List<string>();
            long freedBytes = 0;

            SearchOption searchOption = recursive
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            Func<FileSystemInfo, DateTime> getTime = TimeGetters[timeAttribute];
            List<string> patterns = ParseFilePatterns(filePattern);

            // Удаляем файлы
            if (deleteFiles)
            {
                ProcessFiles(folderPath, patterns, searchOption,
                    getTime, thresholdDays, thresholdDateValue,
                    minSizeBytes, maxItems,
                    dryRun, ignoreErrors,
                    deletedPaths, errors, ref freedBytes);
            }

            // Удаляем пустые папки
            if (deleteEmptyFolders && !dryRun)
            {
                ProcessEmptyFolders(folderPath, getTime,
                    thresholdDays, thresholdDateValue,
                    ignoreErrors, deletedPaths, errors);
            }

            // Удаляем папки целиком
            if (deleteFoldersWithContent)
            {
                ProcessFolders(folderPath, getTime,
                    thresholdDays, thresholdDateValue,
                    maxItems, dryRun, ignoreErrors,
                    deletedPaths, errors, ref freedBytes);
            }

            result.DeletedCount = deletedPaths.Count;
            result.DeletedPaths = deletedPaths;
            result.FreedBytes = freedBytes;
            result.Errors = errors;

            return result;
        }

        // =====================================================================
        // Методы обработки файлов и папок
        // =====================================================================

        /// <summary>
        /// Обрабатывает файлы по списку масок: проверяет критерии и удаляет.
        /// </summary>
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
                    if (!ignoreErrors) throw;
                    errors.Add($"{fi.FullName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Удаляет пустые папки удовлетворяющие временному критерию.
        /// </summary>
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
                    if (!ignoreErrors) throw;
                    errors.Add($"{di.FullName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Удаляет папки целиком с содержимым.
        /// </summary>
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
                    if (!ignoreErrors) throw;
                    errors.Add($"{di.FullName}: {ex.Message}");
                }
            }
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>
        /// Проверяет соответствие объекта временному критерию.
        /// </summary>
        public static bool MeetsTimeThreshold(
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

        /// <summary>
        /// Проверяет соответствие объекта временному критерию (перегрузка).
        /// </summary>
        public static bool MeetsTimeThreshold(
            FileSystemInfo fsi,
            FileTimeAttribute timeAttribute,
            DateTime? thresholdDays,
            DateTime? thresholdDate)
        {
            return MeetsTimeThreshold(fsi, TimeGetters[timeAttribute], thresholdDays, thresholdDate);
        }

        /// <summary>
        /// Рекурсивно вычисляет суммарный размер содержимого папки в байтах.
        /// </summary>
        public static long GetDirectorySize(DirectoryInfo di)
        {
            try
            {
                return di.GetFiles("*", SearchOption.AllDirectories)
                    .Sum(fi => fi.Length);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Форматирует размер в байтах в читаемый вид (KB, MB, GB).
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            var thresholds = new Dictionary<long, string>
            {
                { 1024L * 1024 * 1024, "ГБ" },
                { 1024L * 1024,        "МБ" },
                { 1024L,               "КБ" }
            };

            foreach (var kv in thresholds)
            {
                if (bytes >= kv.Key)
                    return $"{bytes / (double)kv.Key:F1} {kv.Value}";
            }

            return $"{bytes} байт";
        }

        /// <summary>
        /// Разбирает строку масок файлов в список.
        /// </summary>
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
    }
}
