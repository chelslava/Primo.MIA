using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.Sftp
{
    /// <summary>
    /// Тесты синхронизации каталогов (issue #48): чистое построение плана
    /// над синтетическими снимками деревьев (rsync-семантика), нормализация
    /// путей, снимок локального дерева, DryRun-сводка.
    /// </summary>
    [Trait("Category", "Fast")]
    public class SftpSyncLogicTests : IDisposable
    {
        private readonly SftpSyncLogic _logic = new SftpSyncLogic();

        private readonly string _workDir = Path.Combine(
            Path.GetTempPath(), "mia-sftp-sync-tests-" + Guid.NewGuid().ToString("N"));

        public SftpSyncLogicTests()
        {
            Directory.CreateDirectory(_workDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_workDir))
                    Directory.Delete(_workDir, recursive: true);
            }
            catch
            {
                // временная папка может быть занята — не роняем тесты уборкой
            }
        }

        private static SyncTreeSnapshot Snapshot(
            (string path, long size, DateTime mtime)[] files,
            string[] dirs = null)
        {
            var snapshot = new SyncTreeSnapshot();
            foreach (var (path, size, mtime) in files)
                snapshot.Files[path] = new SyncFileInfo { SizeBytes = size, ModifiedAtUtc = mtime };
            if (dirs != null)
                foreach (var dir in dirs)
                    snapshot.Directories.Add(dir);
            return snapshot;
        }

        private static readonly DateTime T1 = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime T2 = new DateTime(2026, 11, 2, 9, 30, 0, DateTimeKind.Utc);

        // ── План: перенос файлов ───────────────────────────────────────

        [Fact(DisplayName = "Push: новый файл на приёмнике → Transfer")]
        public void BuildPlan_NewFile_Transfer()
        {
            var source = Snapshot(new[] { ("report.csv", 100L, T1) }, new[] { "sub" });
            var target = Snapshot(Array.Empty<(string, long, DateTime)>());

            var plan = _logic.BuildPlan(source, target, deleteExtra: false, SftpSyncCompareMode.SizeAndTime);

            plan.Actions.Should().ContainSingle(a => a.Kind == SyncActionKind.Transfer
                && a.RelativePath == "report.csv" && a.Reason == "новый файл");
            plan.Actions.Should().ContainSingle(a => a.Kind == SyncActionKind.CreateDirectory
                && a.RelativePath == "sub");
            plan.SkippedCount.Should().Be(0);
        }

        [Fact(DisplayName = "Push: одинаковые файлы по размеру+времени → пропущены")]
        public void BuildPlan_SameFiles_Skip()
        {
            var source = Snapshot(new[] { ("a.txt", 10L, T1), ("b.txt", 20L, T2) });
            var target = Snapshot(new[] { ("a.txt", 10L, T1), ("b.txt", 20L, T2) });

            var plan = _logic.BuildPlan(source, target, false, SftpSyncCompareMode.SizeAndTime);

            plan.Actions.Should().BeEmpty();
            plan.SkippedCount.Should().Be(2);
        }

        [Fact(DisplayName = "Push: допуск 2 с на гранулярность ФС — не переносится")]
        public void BuildPlan_MTimeWithinTolerance_Skip()
        {
            var source = Snapshot(new[] { ("a.txt", 10L, T1) });
            var target = Snapshot(new[] { ("a.txt", 10L, T1.AddSeconds(2)) });

            var plan = _logic.BuildPlan(source, target, false, SftpSyncCompareMode.SizeAndTime);

            plan.Actions.Should().BeEmpty();
        }

        [Fact(DisplayName = "Push: размер изменился → Transfer даже при том же времени")]
        public void BuildPlan_SizeDiffers_Transfer()
        {
            var source = Snapshot(new[] { ("a.txt", 11L, T1) });
            var target = Snapshot(new[] { ("a.txt", 10L, T1) });

            var plan = _logic.BuildPlan(source, target, false, SftpSyncCompareMode.SizeAndTime);

            plan.Actions.Should().ContainSingle(a => a.Kind == SyncActionKind.Transfer
                && a.Reason == "изменился размер");
        }

        [Fact(DisplayName = "Push: время изменилось (размер тот же) → Transfer в режиме SizeAndTime")]
        public void BuildPlan_MTimeDiffers_Transfer()
        {
            var source = Snapshot(new[] { ("a.txt", 10L, T2) });
            var target = Snapshot(new[] { ("a.txt", 10L, T1) });

            var plan = _logic.BuildPlan(source, target, false, SftpSyncCompareMode.SizeAndTime);

            plan.Actions.Should().ContainSingle(a => a.Kind == SyncActionKind.Transfer
                && a.Reason == "изменилось время");
        }

        [Fact(DisplayName = "Push: SizeOnly — время игнорируется, размер совпал → пропуск")]
        public void BuildPlan_SizeOnly_IgnoresMTime()
        {
            var source = Snapshot(new[] { ("a.txt", 10L, T2) });
            var target = Snapshot(new[] { ("a.txt", 10L, T1) });

            var plan = _logic.BuildPlan(source, target, false, SftpSyncCompareMode.SizeOnly);

            plan.Actions.Should().BeEmpty();
            plan.SkippedCount.Should().Be(1);
        }

        // ── План: порядок действий ─────────────────────────────────────

        [Fact(DisplayName = "План: каталоги создаются сверху вниз, до переноса файлов")]
        public void BuildPlan_DirsBeforeFiles_ParentsFirst()
        {
            var source = Snapshot(
                new[] { ("deep/nested/dir/file.txt", 5L, T1) },
                new[] { "deep", "deep/nested", "deep/nested/dir" });
            var target = Snapshot(Array.Empty<(string, long, DateTime)>());

            var plan = _logic.BuildPlan(source, target, false, SftpSyncCompareMode.SizeAndTime);

            var dirActions = plan.Actions.Where(a => a.Kind == SyncActionKind.CreateDirectory).ToList();
            dirActions.Select(a => a.RelativePath).Should().ContainInOrder("deep", "deep/nested", "deep/nested/dir");
            plan.Actions.Should().ContainSingle(a => a.Kind == SyncActionKind.Transfer);
            // каталоги — до файла
            plan.Actions.FindIndex(a => a.Kind == SyncActionKind.Transfer)
                .Should().BeGreaterThan(plan.Actions.FindIndex(a => a.Kind == SyncActionKind.CreateDirectory));
        }

        // ── План: удаление лишнего ─────────────────────────────────────

        [Fact(DisplayName = "DeleteExtra: лишние файлы удаляются, лишние каталоги снизу вверх")]
        public void BuildPlan_DeleteExtra_DeletesTargetOnly()
        {
            var source = Snapshot(new[] { ("keep.txt", 1L, T1) }, new[] { "data" });
            var target = Snapshot(
                new[] { ("keep.txt", 1L, T1), ("stale.log", 2L, T1), ("extra/old.txt", 3L, T1) },
                new[] { "data", "extra", "extra/nested" });

            var plan = _logic.BuildPlan(source, target, deleteExtra: true, SftpSyncCompareMode.SizeAndTime);

            plan.Actions.Where(a => a.Kind == SyncActionKind.DeleteFile)
                .Select(a => a.RelativePath).Should().BeEquivalentTo("stale.log", "extra/old.txt");
            plan.Actions.Where(a => a.Kind == SyncActionKind.DeleteDirectory)
                .Select(a => a.RelativePath).Should().ContainInOrder("extra/nested", "extra");
            // нужные файлы не удаляются и не переносятся
            plan.Actions.Should().NotContain(a => a.RelativePath == "keep.txt");
        }

        [Fact(DisplayName = "DeleteExtra=false: лишнее на приёмнике остаётся")]
        public void BuildPlan_NoDeleteExtra_KeepsTargetOnly()
        {
            var source = Snapshot(new[] { ("keep.txt", 1L, T1) });
            var target = Snapshot(new[] { ("keep.txt", 1L, T1), ("stale.log", 2L, T1) });

            var plan = _logic.BuildPlan(source, target, deleteExtra: false, SftpSyncCompareMode.SizeAndTime);

            plan.Actions.Should().BeEmpty();
        }

        [Fact(DisplayName = "DeleteExtra: каталог, существующий в источнике, не удаляется")]
        public void BuildPlan_DeleteExtra_KeepsSourceDirs()
        {
            var source = Snapshot(new[] { ("data/f.txt", 1L, T1) }, new[] { "data" });
            var target = Snapshot(new[] { ("data/f.txt", 1L, T1), ("data/gone.txt", 2L, T1) }, new[] { "data" });

            var plan = _logic.BuildPlan(source, target, deleteExtra: true, SftpSyncCompareMode.SizeAndTime);

            plan.Actions.Where(a => a.Kind == SyncActionKind.DeleteDirectory).Should().BeEmpty();
            plan.Actions.Where(a => a.Kind == SyncActionKind.DeleteFile)
                .Select(a => a.RelativePath).Should().BeEquivalentTo("data/gone.txt");
        }

        // ── Pull-семантика: симметрия плана ────────────────────────────

        [Fact(DisplayName = "Pull: источник/приёмник просто меняются местами (Back), план симметричен")]
        public void BuildPlan_Symmetric_SwapSides()
        {
            // Pull: remote — источник, local — приёмник (Back подставляет снимки)
            var remote = Snapshot(new[] { ("out.csv", 10L, T2) });
            var local = Snapshot(new[] { ("out.csv", 10L, T2), ("local-only.txt", 1L, T1) });

            var plan = _logic.BuildPlan(remote, local, deleteExtra: true, SftpSyncCompareMode.SizeAndTime);

            // совпавший out.csv пропускается; лишний НА ПРИЁМНИКЕ (локальный) файл удаляется
            plan.Actions.Should().ContainSingle(a => a.Kind == SyncActionKind.DeleteFile
                && a.RelativePath == "local-only.txt");
            plan.SkippedCount.Should().Be(1);
        }

        // ── Нормализация путей ─────────────────────────────────────────

        [Theory(DisplayName = "NormalizeRelative: слэши, ведущие/хвостовые разделители")]
        [InlineData(@"sub\dir\file.txt", "sub/dir/file.txt")]
        [InlineData("/sub/dir/", "sub/dir")]
        [InlineData(@"\", "")]
        [InlineData("", "")]
        [InlineData(null, "")]
        [InlineData("  file.txt  ", "file.txt")]
        public void NormalizeRelative_VariousInputs(string input, string expected)
        {
            SftpSyncLogic.NormalizeRelative(input).Should().Be(expected);
        }

        [Fact(DisplayName = "JoinRemotePath: корень + относительный путь")]
        public void JoinRemotePath_Combines()
        {
            _logic.JoinRemotePath("/exports/reports", "nov/file.csv").Should().Be("/exports/reports/nov/file.csv");
            _logic.JoinRemotePath("/exports/reports/", "").Should().Be("/exports/reports");
            // «~» без информации о домашней директории → корень «/»
            _logic.JoinRemotePath("~/data", "sub/x.txt").Should().Be("/data/sub/x.txt");
        }

        // ── Снимок локального дерева ───────────────────────────────────

        [Fact(DisplayName = "ListLocalSnapshot: рекурсивный обход с относительными путями")]
        public void ListLocalSnapshot_Recursive()
        {
            var root = Path.Combine(_workDir, "local-root");
            Directory.CreateDirectory(Path.Combine(root, "sub", "nested"));
            File.WriteAllText(Path.Combine(root, "a.txt"), "hello");
            File.WriteAllText(Path.Combine(root, "sub", "b.txt"), "x");
            File.WriteAllText(Path.Combine(root, "sub", "nested", "c.txt"), "yy");

            var snapshot = _logic.ListLocalSnapshot(root);

            snapshot.Files.Keys.Should().BeEquivalentTo("a.txt", "sub/b.txt", "sub/nested/c.txt");
            snapshot.Directories.Should().BeEquivalentTo("sub", "sub/nested");
            snapshot.Files["sub/nested/c.txt"].SizeBytes.Should().Be(2);
        }

        [Fact(DisplayName = "ListLocalSnapshot: несуществующий корень → DirectoryNotFoundException")]
        public void ListLocalSnapshot_MissingRoot_Throws()
        {
            Action act = () => _logic.ListLocalSnapshot(Path.Combine(_workDir, "no-such-dir"));
            act.Should().Throw<DirectoryNotFoundException>().WithMessage("*Локальный каталог не найден*");
        }

        // ── Сводка ─────────────────────────────────────────────────────

        [Fact(DisplayName = "FormatPlan: человекочитаемая сводка по типам действий")]
        public void FormatPlan_Sums()
        {
            var source = Snapshot(
                new[] { ("new.txt", 1L, T1), ("same.txt", 1L, T1), ("chg.txt", 2L, T1) },
                new[] { "sub" });
            var target = Snapshot(new[] { ("same.txt", 1L, T1), ("old.txt", 1L, T1) });

            var plan = _logic.BuildPlan(source, target, deleteExtra: true, SftpSyncCompareMode.SizeAndTime);
            var summary = _logic.FormatPlan(plan);

            summary.Should().Contain("каталогов создать: 1");
            summary.Should().Contain("файлов перенести: 2");
            summary.Should().Contain("пропустить: 1");
            summary.Should().Contain("файлов удалить: 1");
        }

        [Fact(DisplayName = "TimesEqual: допуск константа = 2 секунды")]
        public void TimesEqual_Tolerance()
        {
            SftpSyncLogic.MTimeToleranceSeconds.Should().Be(2);
            SftpSyncLogic.TimesEqual(T1, T1.AddSeconds(2)).Should().BeTrue();
            SftpSyncLogic.TimesEqual(T1, T1.AddSeconds(2.1)).Should().BeFalse();
        }
    }
}
