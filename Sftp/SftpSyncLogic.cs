// =============================================================================
// SftpSyncLogic.cs — логика синхронизации каталогов SFTP ↔ локальная папка
// (issue #48).
//
// rsync-паттерн: снимки деревьев (относительные пути '/'-разделённые),
// чистое построение плана (полностью тестируется на синтетических снимках),
// затем тонкий исполнитель против реальных сторон. Сравнение — размер + mtime
// (допуск 2 с на гранулярность ФС) или только размер. После переноса mtime
// выравнивается — повторный прогон ничего не переносит (идемпотентность).
// =============================================================================

using Primo.MIA.Common;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>Метаданные файла для сравнения деревьев.</summary>
    public class SyncFileInfo
    {
        public long SizeBytes { get; set; }
        public DateTime ModifiedAtUtc { get; set; }
    }

    /// <summary>
    /// Снимок дерева каталогов: относительные пути с '/' без ведущего слэша.
    /// </summary>
    public class SyncTreeSnapshot
    {
        public Dictionary<string, SyncFileInfo> Files { get; } =
            new Dictionary<string, SyncFileInfo>(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> Directories { get; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Тип действия плана синхронизации.</summary>
    public enum SyncActionKind
    {
        /// <summary>Создать каталог на приёмнике.</summary>
        CreateDirectory,
        /// <summary>Перенести файл источник → приёмник (Push: upload, Pull: download).</summary>
        Transfer,
        /// <summary>Удалить лишний файл на приёмнике.</summary>
        DeleteFile,
        /// <summary>Удалить пустой лишний каталог на приёмнике.</summary>
        DeleteDirectory
    }

    /// <summary>Действие плана: относительный путь + причина.</summary>
    public class SyncAction
    {
        public SyncActionKind Kind { get; set; }
        public string RelativePath { get; set; }

        /// <summary>Человекочитаемая причина: «новый файл», «изменился размер», «лишний на приёмнике».</summary>
        public string Reason { get; set; }
    }

    /// <summary>План синхронизации (без побочных эффектов — DryRun возвращает его текстом).</summary>
    public class SyncPlan
    {
        public List<SyncAction> Actions { get; } = new List<SyncAction>();

        /// <summary>Число файлов источника (для подсчёта пропущенных).</summary>
        public int SourceFileCount { get; set; }

        /// <summary>Файлы источника, совпавшие по критерию — не переносятся.</summary>
        public int SkippedCount => SourceFileCount - Actions.Count(a => a.Kind == SyncActionKind.Transfer);
    }

    /// <summary>
    /// Логика синхронизации: снимки деревьев и построение плана.
    /// Построение плана — чистая функция над снимками (unit-тесты без моков SFTP).
    /// </summary>
    public class SftpSyncLogic
    {
        /// <summary>Допуск сравнения mtime (гранулярность файловых систем, с).</summary>
        public const int MTimeToleranceSeconds = 2;

        private readonly SftpLogic _sftpLogic = new SftpLogic();

        // ── Нормализация путей ─────────────────────────────────────────

        /// <summary>
        /// Нормализует относительный путь: обратные слэши → прямые,
        /// ведущий/хвостовой слэши срезаются. Пустой вход → "".
        /// </summary>
        public static string NormalizeRelative(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            var normalized = path.Trim().Replace('\\', '/').Trim('/');
            return normalized ?? string.Empty;
        }

        /// <summary>Полный удалённый путь из корня синхронизации и относительного пути.</summary>
        public string JoinRemotePath(string remoteRoot, string relativePath)
        {
            var root = _sftpLogic.NormalizeRemotePath(remoteRoot).TrimEnd('/');
            var rel = NormalizeRelative(relativePath);
            return rel.Length == 0 ? root : root + "/" + rel;
        }

        // ── Снимки деревьев ────────────────────────────────────────────

        /// <summary>Снимок локального дерева (рекурсивно).</summary>
        public SyncTreeSnapshot ListLocalSnapshot(string rootPath)
        {
            Guard.NotNullOrWhiteSpace(rootPath, nameof(rootPath));

            var root = Path.GetFullPath(rootPath);
            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException($"Локальный каталог не найден: {root}");

            var snapshot = new SyncTreeSnapshot();
            var rootDir = new DirectoryInfo(root);

            foreach (var dir in rootDir.EnumerateDirectories("*", SearchOption.AllDirectories))
                snapshot.Directories.Add(NormalizeRelative(dir.FullName.Substring(root.Length)));

            foreach (var file in rootDir.EnumerateFiles("*", SearchOption.AllDirectories))
                snapshot.Files[NormalizeRelative(file.FullName.Substring(root.Length))] =
                    new SyncFileInfo
                    {
                        SizeBytes = file.Length,
                        ModifiedAtUtc = file.LastWriteTimeUtc
                    };

            return snapshot;
        }

        /// <summary>Снимок удалённого дерева (рекурсивный обход ListDirectory).</summary>
        public SyncTreeSnapshot ListRemoteSnapshot(SftpClient client, string rootPath)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            Guard.NotNullOrWhiteSpace(rootPath, nameof(rootPath));

            var root = _sftpLogic.NormalizeRemotePath(rootPath).TrimEnd('/');
            if (!client.Exists(root))
                throw new DirectoryNotFoundException($"Удалённый каталог не найден: {root}");

            var snapshot = new SyncTreeSnapshot();
            WalkRemote(client, root, string.Empty, snapshot);
            return snapshot;
        }

        private void WalkRemote(SftpClient client, string remoteDir, string relative, SyncTreeSnapshot snapshot)
        {
            foreach (var entry in client.ListDirectory(remoteDir))
            {
                if (entry.Name == "." || entry.Name == "..")
                    continue;

                var childRelative = relative.Length == 0
                    ? NormalizeRelative(entry.Name)
                    : relative + "/" + NormalizeRelative(entry.Name);

                if (entry.IsDirectory)
                {
                    snapshot.Directories.Add(childRelative);
                    WalkRemote(client, entry.FullName, childRelative, snapshot);
                }
                else
                {
                    snapshot.Files[childRelative] = new SyncFileInfo
                    {
                        SizeBytes = entry.Length,
                        ModifiedAtUtc = entry.LastWriteTimeUtc
                    };
                }
            }
        }

        // ── План ───────────────────────────────────────────────────────

        /// <summary>
        /// Чистое построение плана по двум снимкам (rsync-семантика).
        /// Порядок исполнения: каталоги (сверху вниз) → перенос файлов →
        /// удаление лишних файлов → удаление лишних каталогов (снизу вверх).
        /// </summary>
        public SyncPlan BuildPlan(
            SyncTreeSnapshot source,
            SyncTreeSnapshot target,
            bool deleteExtra,
            SftpSyncCompareMode compareMode)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var plan = new SyncPlan { SourceFileCount = source.Files.Count };

            // 1) каталоги источника, отсутствующие на приёмнике (родители раньше детей)
            foreach (var dir in source.Directories
                .Where(d => !target.Directories.Contains(d))
                .OrderBy(d => d.Count(c => c == '/'))
                .ThenBy(d => d, StringComparer.Ordinal))
            {
                plan.Actions.Add(new SyncAction
                {
                    Kind = SyncActionKind.CreateDirectory,
                    RelativePath = dir,
                    Reason = "новый каталог"
                });
            }

            // 2) перенос файлов источника
            foreach (var pair in source.Files
                .OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var relative = pair.Key;
                var sourceInfo = pair.Value;

                if (!target.Files.TryGetValue(relative, out var targetInfo))
                {
                    plan.Actions.Add(Transfer(relative, "новый файл"));
                    continue;
                }

                if (targetInfo.SizeBytes != sourceInfo.SizeBytes)
                {
                    plan.Actions.Add(Transfer(relative, "изменился размер"));
                    continue;
                }

                if (compareMode == SftpSyncCompareMode.SizeAndTime
                    && !TimesEqual(targetInfo.ModifiedAtUtc, sourceInfo.ModifiedAtUtc))
                {
                    plan.Actions.Add(Transfer(relative, "изменилось время"));
                    continue;
                }

                // совпал — не переносится
            }

            // 3) лишнее на приёмнике: файлы, затем каталоги (дети раньше родителей)
            if (deleteExtra)
            {
                foreach (var file in target.Files.Keys
                    .Where(f => !source.Files.ContainsKey(f))
                    .OrderBy(f => f, StringComparer.Ordinal))
                {
                    plan.Actions.Add(new SyncAction
                    {
                        Kind = SyncActionKind.DeleteFile,
                        RelativePath = file,
                        Reason = "лишний на приёмнике"
                    });
                }

                foreach (var dir in target.Directories
                    .Where(d => !source.Directories.Contains(d))
                    .OrderByDescending(d => d.Count(c => c == '/'))
                    .ThenByDescending(d => d, StringComparer.Ordinal))
                {
                    plan.Actions.Add(new SyncAction
                    {
                        Kind = SyncActionKind.DeleteDirectory,
                        RelativePath = dir,
                        Reason = "лишний на приёмнике"
                    });
                }
            }

            return plan;
        }

        private static SyncAction Transfer(string relative, string reason)
        {
            return new SyncAction
            {
                Kind = SyncActionKind.Transfer,
                RelativePath = relative,
                Reason = reason
            };
        }

        /// <summary>Сравнение mtime с допуском (гранулярность файловых систем).</summary>
        public static bool TimesEqual(DateTime a, DateTime b)
        {
            return Math.Abs((a - b).TotalSeconds) <= MTimeToleranceSeconds;
        }

        /// <summary>Краткая сводка плана для лога/выходной переменной.</summary>
        public string FormatPlan(SyncPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            var byKind = plan.Actions.GroupBy(a => a.Kind).ToDictionary(g => g.Key, g => g.Count());
            int Count(SyncActionKind kind) => byKind.TryGetValue(kind, out var n) ? n : 0;

            return $"каталогов создать: {Count(SyncActionKind.CreateDirectory)}, " +
                   $"файлов перенести: {Count(SyncActionKind.Transfer)}, " +
                   $"пропустить: {plan.SkippedCount}, " +
                   $"файлов удалить: {Count(SyncActionKind.DeleteFile)}, " +
                   $"каталогов удалить: {Count(SyncActionKind.DeleteDirectory)}";
        }
    }
}
