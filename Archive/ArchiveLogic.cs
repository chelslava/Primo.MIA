// =============================================================================
// ArchiveLogic.cs — чистая логика ZIP-архивации (issue #28).
//
// Тестируемое ядро активностей ZipArchiveCreate / ZipArchiveExtract.
// System.IO.Compression (net481 в-box), внешних зависимостей нет.
//
// Ограничение (документировано в ToolTip активностей): стандартный
// System.IO.Compression не поддерживает AES-шифрование zip-архивов и
// пароли — парольные архивы не входят в v1.
// =============================================================================

using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>Результат создания ZIP-архива.</summary>
    public class ArchiveCreateResult
    {
        /// <summary>Путь к созданному архиву.</summary>
        public string ArchivePath { get; set; }

        /// <summary>Число записей, добавленных в архив.</summary>
        public int EntryCount { get; set; }

        /// <summary>Размер архива в байтах.</summary>
        public long ArchiveSizeBytes { get; set; }
    }

    /// <summary>Результат распаковки ZIP-архива.</summary>
    public class ArchiveExtractResult
    {
        /// <summary>Число извлечённых записей.</summary>
        public int ExtractedCount { get; set; }

        /// <summary>Число записей, пропущенных политикой перезаписи.</summary>
        public int SkippedCount { get; set; }

        /// <summary>Полный путь к папке распаковки.</summary>
        public string DestinationFolder { get; set; }
    }

    /// <summary>
    /// Логика создания и распаковки ZIP-архивов.
    /// </summary>
    public class ArchiveLogic
    {
        /// <summary>
        /// Создаёт ZIP-архив из списка источников: файлов (в т.ч. с масками
        /// * и ?) и папок (рекурсивно). Относительные имена записей строятся
        /// от общего корня источников, чтобы архив распаковывался со структурой.
        /// </summary>
        /// <param name="sourcePaths">Пути через точку с запятой: файлы, папки, маски.</param>
        /// <param name="destinationZip">Путь к создаваемому архиву.</param>
        /// <param name="level">Уровень сжатия.</param>
        /// <returns>Статистика созданного архива.</returns>
        public ArchiveCreateResult CreateZip(
            IEnumerable<string> sourcePaths,
            string destinationZip,
            ArchiveCompressionLevel level)
        {
            Guard.NotNullOrWhiteSpace(destinationZip, nameof(destinationZip));

            var sources = (sourcePaths ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (sources.Count == 0)
                throw new InvalidOperationException("Не указаны исходные файлы или папки для архивации");

            var destFullPath = Path.GetFullPath(destinationZip);
            var destDir = Path.GetDirectoryName(destFullPath);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);

            // Собираем пары (путь на диске → имя записи в архиве)
            var entries = CollectEntries(sources);
            if (entries.Count == 0)
                throw new InvalidOperationException("По указанным источникам не найдено ни одного файла");

            var compressionLevel = MapCompressionLevel(level);

            // Пересоздаём архив при повторном запуске (предсказуемый результат)
            if (File.Exists(destFullPath))
                File.Delete(destFullPath);

            using (var stream = new FileStream(destFullPath, FileMode.CreateNew))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                foreach (var entry in entries)
                {
                    // Имена записей — с '/', чтобы структура читалась на всех платформах
                    archive.CreateEntryFromFile(
                        entry.FullPath, entry.EntryName.Replace('\\', '/'), compressionLevel);
                }
            }

            return new ArchiveCreateResult
            {
                ArchivePath = destFullPath,
                EntryCount = entries.Count,
                ArchiveSizeBytes = new FileInfo(destFullPath).Length
            };
        }

        /// <summary>
        /// Распаковывает ZIP-архив в папку с политикой перезаписи и опциональной
        /// маской файлов. Защита от Zip Slip: путь записи не может выйти за
        /// пределы папки распаковки.
        /// </summary>
        /// <param name="zipPath">Путь к архиву.</param>
        /// <param name="destinationFolder">Папка распаковки (создаётся при отсутствии).</param>
        /// <param name="policy">Политика перезаписи существующих файлов.</param>
        /// <param name="fileMask">Маска записей (* и ?); null/пусто = все.</param>
        public ArchiveExtractResult ExtractZip(
            string zipPath,
            string destinationFolder,
            ArchiveOverwritePolicy policy,
            string fileMask = null)
        {
            Guard.NotNullOrWhiteSpace(zipPath, nameof(zipPath));
            Guard.NotNullOrWhiteSpace(destinationFolder, nameof(destinationFolder));

            if (!File.Exists(zipPath))
                throw new FileNotFoundException($"ZIP-архив не найден: {zipPath}", zipPath);

            var destRoot = Path.GetFullPath(destinationFolder);
            Directory.CreateDirectory(destRoot);

            var maskRegex = string.IsNullOrWhiteSpace(fileMask)
                ? null
                : new System.Text.RegularExpressions.Regex(
                    StringHelper.WildcardToRegex(fileMask.Trim()),
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            var result = new ArchiveExtractResult { DestinationFolder = destRoot };

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    // Каталоги-записи (имя оканчивается '/') — только создаём папку
                    if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                    {
                        Directory.CreateDirectory(SafeResolvePath(destRoot, entry.FullName));
                        continue;
                    }

                    var entryName = Path.GetFileName(entry.FullName);
                    if (maskRegex != null && (string.IsNullOrEmpty(entryName) || !maskRegex.IsMatch(entryName)))
                        continue;

                    var targetPath = SafeResolvePath(destRoot, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

                    if (File.Exists(targetPath))
                    {
                        if (policy == ArchiveOverwritePolicy.Skip)
                        {
                            result.SkippedCount++;
                            continue;
                        }
                        if (policy == ArchiveOverwritePolicy.Throw)
                            throw new InvalidOperationException(
                                $"Файл уже существует (политика перезаписи «Ошибка»): {targetPath}");
                    }

                    entry.ExtractToFile(targetPath, overwrite: policy == ArchiveOverwritePolicy.Overwrite);
                    result.ExtractedCount++;
                }
            }

            return result;
        }

        /// <summary>
        /// Разрешает путь записи внутри корня распаковки.
        /// Zip Slip: запись с "..", корневым префиксом или недопустимыми символами
        /// выбрасывает исключение, вместо записи за пределами корня.
        /// </summary>
        private static string SafeResolvePath(string destRoot, string entryName)
        {
            var candidate = Path.GetFullPath(Path.Combine(destRoot, entryName));

            if (!candidate.StartsWith(destRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(candidate, destRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Опасный путь записи в архиве (Zip Slip): '{entryName}' → {candidate}");

            return candidate;
        }

        /// <summary>
        /// Разбирает источники в пары «файл на диске → имя записи».
        /// Папки включаются рекурсивно, имя записи отсчитывается от самой папки.
        /// Для масок (в файловом пути) базой считается содержащая папка.
        /// </summary>
        private static List<(string FullPath, string EntryName)> CollectEntries(
            IReadOnlyList<string> sources)
        {
            var entries = new List<(string, string)>();
            var seenEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var source in sources)
            {
                var expanded = ExpandSource(source);
                foreach (var (fullPath, entryName) in expanded)
                {
                    if (seenEntries.Add(entryName))
                        entries.Add((fullPath, entryName));
                }
            }

            return entries;
        }

        private static IEnumerable<(string FullPath, string EntryName)> ExpandSource(string source)
        {
            // Маску (* и ?) нужно обнаружить ДО Path.GetFullPath: в net481
            // GetFullPath бросает ArgumentException на недопустимых знаках пути
            var lastSep = source.LastIndexOfAny(new[] { '\\', '/' });
            var rawPattern = lastSep >= 0 ? source.Substring(lastSep + 1) : source;
            var rawDir = lastSep >= 0 ? source.Substring(0, lastSep) : string.Empty;

            if (rawPattern.IndexOfAny(new[] { '*', '?' }) >= 0)
            {
                // Маска: каталог + шаблон (TopDirectoryOnly, как в файловых масках)
                if (string.IsNullOrEmpty(rawDir) || !Directory.Exists(rawDir))
                    yield break;

                foreach (var file in Directory.EnumerateFiles(rawDir, rawPattern, SearchOption.TopDirectoryOnly))
                    yield return (file, Path.GetFileName(file));

                yield break;
            }

            var fullPath = Path.GetFullPath(source);

            if (Directory.Exists(fullPath))
            {
                // Папка: все файлы рекурсивно, записи с именем папки как корнем
                foreach (var file in Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories))
                {
                    var relative = file.Substring(fullPath.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    yield return (file, Path.Combine(Path.GetFileName(fullPath.TrimEnd('\\', '/')), relative));
                }
                yield break;
            }

            if (File.Exists(fullPath))
            {
                yield return (fullPath, Path.GetFileName(fullPath));
                yield break;
            }

            // Источник не найден — молча пропускаем (маска могла не совпасть)
        }

        private static CompressionLevel MapCompressionLevel(ArchiveCompressionLevel level)
        {
            switch (level)
            {
                case ArchiveCompressionLevel.Fastest: return CompressionLevel.Fastest;
                case ArchiveCompressionLevel.NoCompression: return CompressionLevel.NoCompression;
                default: return CompressionLevel.Optimal;
            }
        }
    }
}
