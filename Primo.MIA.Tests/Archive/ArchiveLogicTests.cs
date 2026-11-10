using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.Archive
{
    /// <summary>
    /// Тесты чистой логики ZIP-архивации (issue #28):
    /// ArchiveLogic.CreateZip / ExtractZip — round-trip на временных папках.
    /// </summary>
    [Trait("Category", "Fast")]
    public class ArchiveLogicTests : IDisposable
    {
        private readonly ArchiveLogic _logic = new ArchiveLogic();

        private readonly string _workDir = Path.Combine(
            Path.GetTempPath(), "mia-archive-tests-" + Guid.NewGuid().ToString("N"));

        public ArchiveLogicTests()
        {
            // Рабочая папка нужна всем тестам (в т.ч. не создающим дерево источников)
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
                // временная папка может быть занята антивирусом — не роняем тесты уборкой
            }
        }

        // ── Хелперы ────────────────────────────────────────────────────

        private string CreateSourceTree()
        {
            var src = Path.Combine(_workDir, "src");
            Directory.CreateDirectory(Path.Combine(src, "sub"));
            File.WriteAllText(Path.Combine(src, "a.txt"), "alpha");
            File.WriteAllText(Path.Combine(src, "b.log"), "beta");
            File.WriteAllText(Path.Combine(src, "sub", "c.txt"), "gamma");
            return src;
        }

        private string CreateZipFrom(params string[] sources)
        {
            var zip = Path.Combine(_workDir, "out.zip");
            _logic.CreateZip(sources, zip, ArchiveCompressionLevel.Optimal);
            return zip;
        }

        // ── CreateZip ──────────────────────────────────────────────────

        [Fact(DisplayName = "CreateZip: папка со структурой — записи с именем папки как корнем")]
        public void CreateZip_Folder_PreservesStructure()
        {
            var src = CreateSourceTree();
            var zip = Path.Combine(_workDir, "struct.zip");

            var result = _logic.CreateZip(new[] { src }, zip, ArchiveCompressionLevel.Optimal);

            result.ArchivePath.Should().Be(Path.GetFullPath(zip));
            result.EntryCount.Should().Be(3);
            File.Exists(zip).Should().BeTrue();
            result.ArchiveSizeBytes.Should().BeGreaterThan(0);

            using var archive = ZipFile.OpenRead(zip);
            archive.Entries.Select(e => e.FullName)
                .Should().Contain(new[] { "src/a.txt", "src/b.log", "src/sub/c.txt" });
        }

        [Fact(DisplayName = "CreateZip: отдельные файлы и маски — записи по имени файла")]
        public void CreateZip_FilesAndMask_FlatEntries()
        {
            var src = CreateSourceTree();
            var zip = Path.Combine(_workDir, "mixed.zip");

            // файл + маска: маска TopDirectoryOnly матчит только a.txt из корня
            // (b.log не попадает — *.txt; sub/c.txt — в подпапке)
            var result = _logic.CreateZip(new[] { Path.Combine(src, "a.txt"), Path.Combine(src, "*.txt") },
                zip, ArchiveCompressionLevel.Fastest);

            result.EntryCount.Should().Be(1, "a.txt из явного пути и из маски — дедуплицированы");
            using var archive = ZipFile.OpenRead(zip);
            archive.Entries.Select(e => e.FullName)
                .Should().BeEquivalentTo("a.txt");
        }

        [Fact(DisplayName = "CreateZip: дубликаты источников не удваивают записи")]
        public void CreateZip_DuplicateSources_Deduplicated()
        {
            var src = CreateSourceTree();
            var zip = Path.Combine(_workDir, "dedup.zip");

            var result = _logic.CreateZip(new[] { Path.Combine(src, "a.txt"), Path.Combine(src, "a.txt") },
                zip, ArchiveCompressionLevel.Optimal);

            result.EntryCount.Should().Be(1);
        }

        [Fact(DisplayName = "CreateZip: пустые источники — InvalidOperationException")]
        public void CreateZip_EmptySources_Throws()
        {
            var zip = Path.Combine(_workDir, "empty.zip");

            Action act = () => _logic.CreateZip(new[] { "  ", null }, zip, ArchiveCompressionLevel.Optimal);
            act.Should().Throw<InvalidOperationException>().WithMessage("*исходные файлы*");
        }

        [Fact(DisplayName = "CreateZip: маска без совпадений — InvalidOperationException")]
        public void CreateZip_NoMatchingFiles_Throws()
        {
            var src = CreateSourceTree();
            var zip = Path.Combine(_workDir, "nomatch.zip");

            Action act = () => _logic.CreateZip(new[] { Path.Combine(src, "*.xyz") },
                zip, ArchiveCompressionLevel.Optimal);
            act.Should().Throw<InvalidOperationException>().WithMessage("*не найдено*");
        }

        [Fact(DisplayName = "CreateZip: существующий архив пересоздаётся, а не дополняется")]
        public void CreateZip_ExistingArchive_Recreated()
        {
            var src = CreateSourceTree();
            var zip = Path.Combine(_workDir, "recreate.zip");
            File.WriteAllText(zip, "stale content");

            var result = _logic.CreateZip(new[] { Path.Combine(src, "a.txt") }, zip, ArchiveCompressionLevel.Optimal);

            result.EntryCount.Should().Be(1);
            using var archive = ZipFile.OpenRead(zip);
            archive.Entries.Should().HaveCount(1, "старое содержимое архива удалено");
        }

        [Fact(DisplayName = "CreateZip: пустой путь архива — ArgumentException (Guard)")]
        public void CreateZip_EmptyDestination_Throws()
        {
            Action act = () => _logic.CreateZip(new[] { "c:\\x.txt" }, "", ArchiveCompressionLevel.Optimal);
            act.Should().Throw<ArgumentException>();
        }

        // ── ExtractZip ─────────────────────────────────────────────────

        [Fact(DisplayName = "ExtractZip: round-trip — содержимое совпадает")]
        public void ExtractZip_RoundTrip_ContentMatches()
        {
            var src = CreateSourceTree();
            var zip = CreateZipFrom(src);
            var dest = Path.Combine(_workDir, "dest");

            var result = _logic.ExtractZip(zip, dest, ArchiveOverwritePolicy.Skip);

            result.ExtractedCount.Should().Be(3);
            result.SkippedCount.Should().Be(0);
            result.DestinationFolder.Should().Be(Path.GetFullPath(dest));
            File.ReadAllText(Path.Combine(dest, "src", "a.txt")).Should().Be("alpha");
            File.ReadAllText(Path.Combine(dest, "src", "sub", "c.txt")).Should().Be("gamma");
        }

        [Fact(DisplayName = "ExtractZip: папки-записи создаются как каталоги")]
        public void ExtractZip_DirectoryEntries_Created()
        {
            var zip = Path.Combine(_workDir, "dirs.zip");
            using (var stream = new FileStream(zip, FileMode.CreateNew))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                archive.CreateEntry("folder/");
                var entry = archive.CreateEntry("folder/file.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.Write("data");
            }

            var dest = Path.Combine(_workDir, "dest-dirs");
            var result = _logic.ExtractZip(zip, dest, ArchiveOverwritePolicy.Skip);

            Directory.Exists(Path.Combine(dest, "folder")).Should().BeTrue();
            File.ReadAllText(Path.Combine(dest, "folder", "file.txt")).Should().Be("data");
            result.ExtractedCount.Should().Be(1, "каталог-запись не считается файлом");
        }

        [Fact(DisplayName = "ExtractZip: политика Skip пропускает существующие файлы")]
        public void ExtractZip_SkipPolicy_PreservesExisting()
        {
            var src = CreateSourceTree();
            var zip = CreateZipFrom(src);
            var dest = Path.Combine(_workDir, "dest-skip");
            Directory.CreateDirectory(Path.Combine(dest, "src"));
            File.WriteAllText(Path.Combine(dest, "src", "a.txt"), "ORIGINAL");

            var result = _logic.ExtractZip(zip, dest, ArchiveOverwritePolicy.Skip);

            result.ExtractedCount.Should().Be(2);
            result.SkippedCount.Should().Be(1);
            File.ReadAllText(Path.Combine(dest, "src", "a.txt")).Should().Be("ORIGINAL");
        }

        [Fact(DisplayName = "ExtractZip: политика Overwrite заменяет существующие файлы")]
        public void ExtractZip_OverwritePolicy_ReplacesExisting()
        {
            var src = CreateSourceTree();
            var zip = CreateZipFrom(src);
            var dest = Path.Combine(_workDir, "dest-overwrite");
            Directory.CreateDirectory(Path.Combine(dest, "src"));
            File.WriteAllText(Path.Combine(dest, "src", "a.txt"), "ORIGINAL");

            var result = _logic.ExtractZip(zip, dest, ArchiveOverwritePolicy.Overwrite);

            result.ExtractedCount.Should().Be(3);
            result.SkippedCount.Should().Be(0);
            File.ReadAllText(Path.Combine(dest, "src", "a.txt")).Should().Be("alpha");
        }

        [Fact(DisplayName = "ExtractZip: политика Throw — InvalidOperationException при существующем файле")]
        public void ExtractZip_ThrowPolicy_FailsOnExisting()
        {
            var src = CreateSourceTree();
            var zip = CreateZipFrom(src);
            var dest = Path.Combine(_workDir, "dest-throw");
            Directory.CreateDirectory(Path.Combine(dest, "src"));
            File.WriteAllText(Path.Combine(dest, "src", "a.txt"), "ORIGINAL");

            Action act = () => _logic.ExtractZip(zip, dest, ArchiveOverwritePolicy.Throw);

            act.Should().Throw<InvalidOperationException>().WithMessage("*уже существует*");
        }

        [Fact(DisplayName = "ExtractZip: маска извлекает только совпавшие файлы")]
        public void ExtractZip_Mask_FiltersEntries()
        {
            var src = CreateSourceTree();
            var zip = CreateZipFrom(src);
            var dest = Path.Combine(_workDir, "dest-mask");

            var result = _logic.ExtractZip(zip, dest, ArchiveOverwritePolicy.Skip, "*.txt");

            result.ExtractedCount.Should().Be(2, "a.txt + sub/c.txt");
            File.Exists(Path.Combine(dest, "src", "b.log")).Should().BeFalse();
            File.Exists(Path.Combine(dest, "src", "a.txt")).Should().BeTrue();
        }

        [Fact(DisplayName = "ExtractZip: Zip Slip — путь записи за пределами корня отклоняется")]
        public void ExtractZip_ZipSlip_Throws()
        {
            var zip = Path.Combine(_workDir, "evil.zip");
            using (var stream = new FileStream(zip, FileMode.CreateNew))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("..\\evil.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.Write("malicious");
            }

            var dest = Path.Combine(_workDir, "dest-slip");
            Action act = () => _logic.ExtractZip(zip, dest, ArchiveOverwritePolicy.Skip);

            act.Should().Throw<InvalidOperationException>().WithMessage("*Zip Slip*");
            File.Exists(Path.Combine(_workDir, "evil.txt")).Should().BeFalse("запись не должна выйти за пределы корня");
        }

        [Fact(DisplayName = "ExtractZip: несуществующий архив — FileNotFoundException")]
        public void ExtractZip_MissingArchive_ThrowsFileNotFound()
        {
            var zip = Path.Combine(_workDir, "missing.zip");
            var dest = Path.Combine(_workDir, "dest-missing");

            Action act = () => _logic.ExtractZip(zip, dest, ArchiveOverwritePolicy.Skip);
            act.Should().Throw<FileNotFoundException>();
        }

        [Fact(DisplayName = "ExtractZip: папка распаковки создаётся при отсутствии")]
        public void ExtractZip_CreatesDestinationFolders()
        {
            var src = CreateSourceTree();
            var zip = CreateZipFrom(src);
            var dest = Path.Combine(_workDir, "deep", "nested", "dest");

            _logic.ExtractZip(zip, dest, ArchiveOverwritePolicy.Skip);

            Directory.Exists(dest).Should().BeTrue();
        }

        [Fact(DisplayName = "ExtractZip: пустые аргументы — ArgumentException (Guard)")]
        public void ExtractZip_EmptyArguments_Throw()
        {
            Action noZip = () => _logic.ExtractZip("", "dest", ArchiveOverwritePolicy.Skip);
            Action noFolder = () => _logic.ExtractZip("a.zip", null, ArchiveOverwritePolicy.Skip);

            noZip.Should().Throw<ArgumentException>();
            noFolder.Should().Throw<ArgumentException>();
        }

        // ── Round-trip всех уровней сжатия ─────────────────────────────

        [Theory(DisplayName = "Round-trip при всех уровнях сжатия")]
        [InlineData(ArchiveCompressionLevel.Optimal)]
        [InlineData(ArchiveCompressionLevel.Fastest)]
        [InlineData(ArchiveCompressionLevel.NoCompression)]
        public void RoundTrip_AllCompressionLevels(ArchiveCompressionLevel level)
        {
            var src = CreateSourceTree();
            var zip = Path.Combine(_workDir, $"rt-{level}.zip");
            _logic.CreateZip(new[] { src }, zip, level);

            var dest = Path.Combine(_workDir, $"rt-{level}-dest");
            var result = _logic.ExtractZip(zip, dest, ArchiveOverwritePolicy.Skip);

            result.ExtractedCount.Should().Be(3);
            File.ReadAllText(Path.Combine(dest, "src", "sub", "c.txt")).Should().Be("gamma");
        }
    }
}
