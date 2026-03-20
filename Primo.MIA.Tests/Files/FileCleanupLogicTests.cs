// =============================================================================
// FileCleanupLogicTests.cs — тесты логики очистки файлов.
//
// Покрывает:
//   - Парсинг масок файлов
//   - Проверку временных порогов
//   - Форматирование размера
//   - Вычисление размера папки
//   - DryRun режим
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Primo.MIA.Tests.Logic;
using Xunit;

using TimeAttr = Primo.MIA.Tests.Logic.FileTimeAttribute;
using ThresholdMode = Primo.MIA.Tests.Logic.CleanupThresholdMode;

namespace Primo.MIA.Tests.Files
{
    /// <summary>Тесты логики очистки файлов.</summary>
    public class FileCleanupLogicTests : IDisposable
    {
        private readonly string _testFolder;

        public FileCleanupLogicTests()
        {
            // Создаём временную папку для тестов
            _testFolder = Path.Combine(Path.GetTempPath(), $"FileCleanupTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testFolder);
        }

        public void Dispose()
        {
            // Удаляем временную папку после тестов
            if (Directory.Exists(_testFolder))
            {
                try { Directory.Delete(_testFolder, recursive: true); }
                catch { /* игнорируем ошибки очистки */ }
            }
        }

        // =====================================================================
        // Тесты ParseFilePatterns
        // =====================================================================

        [Fact(DisplayName = "ParseFilePatterns: пустая строка → [*]")]
        public void ParseFilePatterns_EmptyString_ReturnsStar()
        {
            // Act
            var result = FileCleanupLogic.ParseFilePatterns("");

            // Assert
            result.Should().Equal("*");
        }

        [Fact(DisplayName = "ParseFilePatterns: null → [*]")]
        public void ParseFilePatterns_Null_ReturnsStar()
        {
            // Act
            var result = FileCleanupLogic.ParseFilePatterns(null);

            // Assert
            result.Should().Equal("*");
        }

        [Fact(DisplayName = "ParseFilePatterns: одна маска")]
        public void ParseFilePatterns_SinglePattern_ReturnsList()
        {
            // Act
            var result = FileCleanupLogic.ParseFilePatterns("*.tmp");

            // Assert
            result.Should().Equal("*.tmp");
        }

        [Fact(DisplayName = "ParseFilePatterns: несколько масок через запятую")]
        public void ParseFilePatterns_CommaSeparated_ReturnsList()
        {
            // Act
            var result = FileCleanupLogic.ParseFilePatterns("*.tmp, *.log, *.bak");

            // Assert
            result.Should().Equal("*.tmp", "*.log", "*.bak");
        }

        [Fact(DisplayName = "ParseFilePatterns: несколько масок через точку с запятой")]
        public void ParseFilePatterns_SemicolonSeparated_ReturnsList()
        {
            // Act
            var result = FileCleanupLogic.ParseFilePatterns("*.tmp;*.log;*.bak");

            // Assert
            result.Should().Equal("*.tmp", "*.log", "*.bak");
        }

        [Fact(DisplayName = "ParseFilePatterns: дубликаты удаляются")]
        public void ParseFilePatterns_Duplicates_Removed()
        {
            // Act
            var result = FileCleanupLogic.ParseFilePatterns("*.tmp, *.log, *.tmp");

            // Assert
            result.Should().Equal("*.tmp", "*.log");
        }

        // =====================================================================
        // Тесты FormatBytes
        // =====================================================================

        [Theory(DisplayName = "FormatBytes: форматирование размера")]
        [InlineData(0, "0 байт")]
        [InlineData(100, "100 байт")]
        [InlineData(1023, "1023 байт")]
        [InlineData(1024, "1,0 КБ")]
        [InlineData(1536, "1,5 КБ")]
        [InlineData(1048576, "1,0 МБ")]
        [InlineData(1572864, "1,5 МБ")]
        [InlineData(1073741824, "1,0 ГБ")]
        public void FormatBytes_VariousSizes_ReturnsCorrectFormat(long bytes, string expected)
        {
            // Act
            string result = FileCleanupLogic.FormatBytes(bytes);

            // Assert
            result.Should().Be(expected);
        }

        // =====================================================================
        // Тесты MeetsTimeThreshold
        // =====================================================================

        [Fact(DisplayName = "MeetsTimeThreshold: файл старше порога дней")]
        public void MeetsTimeThreshold_OlderThanDays_ReturnsTrue()
        {
            // Arrange
            var filePath = Path.Combine(_testFolder, "old_file.txt");
            File.WriteAllText(filePath, "test");
            var fi = new FileInfo(filePath);
            // Устанавливаем дату изменения в прошлом
            fi.LastWriteTime = DateTime.Now.AddDays(-10);

            // Act
            bool result = FileCleanupLogic.MeetsTimeThreshold(
                fi, TimeAttr.LastWriteTime, DateTime.Now.AddDays(-5), null);

            // Assert
            result.Should().BeTrue();
        }

        [Fact(DisplayName = "MeetsTimeThreshold: файл новее порога дней")]
        public void MeetsTimeThreshold_NewerThanDays_ReturnsFalse()
        {
            // Arrange
            var filePath = Path.Combine(_testFolder, "new_file.txt");
            File.WriteAllText(filePath, "test");
            var fi = new FileInfo(filePath);
            fi.LastWriteTime = DateTime.Now.AddDays(-2);

            // Act
            bool result = FileCleanupLogic.MeetsTimeThreshold(
                fi, TimeAttr.LastWriteTime, DateTime.Now.AddDays(-5), null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact(DisplayName = "MeetsTimeThreshold: оба порога (Both) — выполняются оба")]
        public void MeetsTimeThreshold_BothThresholds_ReturnsTrue()
        {
            // Arrange
            var filePath = Path.Combine(_testFolder, "both_file.txt");
            File.WriteAllText(filePath, "test");
            var fi = new FileInfo(filePath);
            fi.LastWriteTime = DateTime.Now.AddDays(-10);

            // Act — файл старше 5 дней И раньше конкретной даты
            bool result = FileCleanupLogic.MeetsTimeThreshold(
                fi, TimeAttr.LastWriteTime,
                DateTime.Now.AddDays(-5),
                DateTime.Now.AddDays(-3));

            // Assert
            result.Should().BeTrue();
        }

        [Fact(DisplayName = "MeetsTimeThreshold: оба порога (Both) — один не выполняется")]
        public void MeetsTimeThreshold_BothThresholds_OneFails_ReturnsFalse()
        {
            // Arrange
            var filePath = Path.Combine(_testFolder, "both_fail_file.txt");
            File.WriteAllText(filePath, "test");
            var fi = new FileInfo(filePath);
            fi.LastWriteTime = DateTime.Now.AddDays(-10);

            // Act — файл старше 5 дней, но НЕ раньше даты (дата = 15 дней назад)
            bool result = FileCleanupLogic.MeetsTimeThreshold(
                fi, TimeAttr.LastWriteTime,
                DateTime.Now.AddDays(-5),
                DateTime.Now.AddDays(-15));

            // Assert
            result.Should().BeFalse();
        }

        [Fact(DisplayName = "MeetsTimeThreshold: разные временные атрибуты")]
        public void MeetsTimeThreshold_DifferentAttributes_WorksCorrectly()
        {
            // Arrange
            var filePath = Path.Combine(_testFolder, "attrs_file.txt");
            File.WriteAllText(filePath, "test");
            var fi = new FileInfo(filePath);
            fi.CreationTime = DateTime.Now.AddDays(-20);
            fi.LastWriteTime = DateTime.Now.AddDays(-5);
            fi.LastAccessTime = DateTime.Now.AddDays(-10);

            var threshold = DateTime.Now.AddDays(-7);

            // Act & Assert
            FileCleanupLogic.MeetsTimeThreshold(fi, TimeAttr.CreationTime, threshold, null)
                .Should().BeTrue("CreationTime старее порога");
            FileCleanupLogic.MeetsTimeThreshold(fi, TimeAttr.LastWriteTime, threshold, null)
                .Should().BeFalse("LastWriteTime новее порога");
            FileCleanupLogic.MeetsTimeThreshold(fi, TimeAttr.LastAccessTime, threshold, null)
                .Should().BeTrue("LastAccessTime старее порога");
        }

        // =====================================================================
        // Тесты GetDirectorySize
        // =====================================================================

        [Fact(DisplayName = "GetDirectorySize: пустая папка → 0")]
        public void GetDirectorySize_EmptyFolder_ReturnsZero()
        {
            // Arrange
            var emptyFolder = Path.Combine(_testFolder, "empty");
            Directory.CreateDirectory(emptyFolder);
            var di = new DirectoryInfo(emptyFolder);

            // Act
            long size = FileCleanupLogic.GetDirectorySize(di);

            // Assert
            size.Should().Be(0);
        }

        [Fact(DisplayName = "GetDirectorySize: папка с файлами")]
        public void GetDirectorySize_WithFiles_ReturnsCorrectSize()
        {
            // Arrange
            var folder = Path.Combine(_testFolder, "with_files");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "file1.txt"), new string('a', 100));
            File.WriteAllText(Path.Combine(folder, "file2.txt"), new string('b', 200));
            var di = new DirectoryInfo(folder);

            // Act
            long size = FileCleanupLogic.GetDirectorySize(di);

            // Assert
            size.Should().Be(300);
        }

        [Fact(DisplayName = "GetDirectorySize: папка с подпапками")]
        public void GetDirectorySize_WithSubfolders_ReturnsCorrectSize()
        {
            // Arrange
            var folder = Path.Combine(_testFolder, "with_subfolders");
            Directory.CreateDirectory(folder);
            var subfolder = Path.Combine(folder, "sub");
            Directory.CreateDirectory(subfolder);
            File.WriteAllText(Path.Combine(folder, "file1.txt"), new string('a', 100));
            File.WriteAllText(Path.Combine(subfolder, "file2.txt"), new string('b', 200));
            var di = new DirectoryInfo(folder);

            // Act
            long size = FileCleanupLogic.GetDirectorySize(di);

            // Assert
            size.Should().Be(300);
        }

        // =====================================================================
        // Тесты Cleanup - DryRun режим
        // =====================================================================

        [Fact(DisplayName = "Cleanup: DryRun=true — файлы не удаляются")]
        public void Cleanup_DryRun_FilesNotDeleted()
        {
            // Arrange
            var oldFile = Path.Combine(_testFolder, "old.txt");
            File.WriteAllText(oldFile, "test");
            var fi = new FileInfo(oldFile);
            fi.LastWriteTime = DateTime.Now.AddDays(-100);

            // Act
            var result = FileCleanupLogic.Cleanup(
                _testFolder,
                deleteFiles: true,
                olderThanDays: 30,
                dryRun: true);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.DeletedCount.Should().Be(1);
            result.DeletedPaths.Should().Contain(oldFile);
            File.Exists(oldFile).Should().BeTrue("DryRun не должен удалять файлы");
        }

        [Fact(DisplayName = "Cleanup: DryRun=false — файлы удаляются")]
        public void Cleanup_NotDryRun_FilesDeleted()
        {
            // Arrange
            var oldFile = Path.Combine(_testFolder, "old_delete.txt");
            File.WriteAllText(oldFile, "test");
            var fi = new FileInfo(oldFile);
            fi.LastWriteTime = DateTime.Now.AddDays(-100);

            // Act
            var result = FileCleanupLogic.Cleanup(
                _testFolder,
                deleteFiles: true,
                olderThanDays: 30,
                dryRun: false);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.DeletedCount.Should().Be(1);
            File.Exists(oldFile).Should().BeFalse("Файл должен быть удалён");
        }

        // =====================================================================
        // Тесты Cleanup - валидация
        // =====================================================================

        [Fact(DisplayName = "Cleanup: пустой путь — ошибка")]
        public void Cleanup_EmptyPath_ReturnsError()
        {
            // Act
            var result = FileCleanupLogic.Cleanup("");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Путь к папке не указан");
        }

        [Fact(DisplayName = "Cleanup: несуществующая папка — ошибка")]
        public void Cleanup_NonExistentFolder_ReturnsError()
        {
            // Act
            var result = FileCleanupLogic.Cleanup(@"C:\NonExistentFolder_12345");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Папка не найдена");
        }

        [Fact(DisplayName = "Cleanup: не выбран тип объектов — ошибка")]
        public void Cleanup_NoTargetSelected_ReturnsError()
        {
            // Act
            var result = FileCleanupLogic.Cleanup(
                _testFolder,
                deleteFiles: false,
                deleteEmptyFolders: false,
                deleteFoldersWithContent: false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Выберите хотя бы один тип");
        }

        [Fact(DisplayName = "Cleanup: отрицательное количество дней — ошибка")]
        public void Cleanup_NegativeDays_ReturnsError()
        {
            // Act
            var result = FileCleanupLogic.Cleanup(
                _testFolder,
                thresholdMode: ThresholdMode.OlderThanDays,
                olderThanDays: -5);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Количество дней должно быть >= 0");
        }

        // =====================================================================
        // Тесты Cleanup - фильтрация
        // =====================================================================

        [Fact(DisplayName = "Cleanup: маска файлов — фильтрует по расширению")]
        public void Cleanup_FilePattern_FiltersByExtension()
        {
            // Arrange
            var logFile = Path.Combine(_testFolder, "file.log");
            var txtFile = Path.Combine(_testFolder, "file.txt");
            File.WriteAllText(logFile, "test");
            File.WriteAllText(txtFile, "test");
            new FileInfo(logFile).LastWriteTime = DateTime.Now.AddDays(-100);
            new FileInfo(txtFile).LastWriteTime = DateTime.Now.AddDays(-100);

            // Act
            var result = FileCleanupLogic.Cleanup(
                _testFolder,
                deleteFiles: true,
                filePattern: "*.log",
                olderThanDays: 30,
                dryRun: true);

            // Assert
            result.DeletedCount.Should().Be(1);
            result.DeletedPaths.Should().Contain(logFile);
            result.DeletedPaths.Should().NotContain(txtFile);
        }

        [Fact(DisplayName = "Cleanup: минимальный размер — фильтрует маленькие файлы")]
        public void Cleanup_MinSize_FiltersSmallFiles()
        {
            // Arrange
            var smallFile = Path.Combine(_testFolder, "small.txt");
            var largeFile = Path.Combine(_testFolder, "large.txt");
            File.WriteAllText(smallFile, "x"); // 1 байт
            File.WriteAllText(largeFile, new string('x', 1000)); // 1000 байт
            new FileInfo(smallFile).LastWriteTime = DateTime.Now.AddDays(-100);
            new FileInfo(largeFile).LastWriteTime = DateTime.Now.AddDays(-100);

            // Act
            var result = FileCleanupLogic.Cleanup(
                _testFolder,
                deleteFiles: true,
                minSizeBytes: 100,
                olderThanDays: 30,
                dryRun: true);

            // Assert
            result.DeletedCount.Should().Be(1);
            result.DeletedPaths.Should().Contain(largeFile);
            result.DeletedPaths.Should().NotContain(smallFile);
        }

        [Fact(DisplayName = "Cleanup: maxItems — ограничивает количество")]
        public void Cleanup_MaxItems_LimitsCount()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                var file = Path.Combine(_testFolder, $"file{i}.txt");
                File.WriteAllText(file, "test");
                new FileInfo(file).LastWriteTime = DateTime.Now.AddDays(-100);
            }

            // Act
            var result = FileCleanupLogic.Cleanup(
                _testFolder,
                deleteFiles: true,
                maxItems: 2,
                olderThanDays: 30,
                dryRun: true);

            // Assert
            result.DeletedCount.Should().Be(2);
        }

        // =====================================================================
        // Тесты Cleanup - освобождённое место
        // =====================================================================

        [Fact(DisplayName = "Cleanup: подсчёт освобождённого места")]
        public void Cleanup_FreedBytes_CalculatedCorrectly()
        {
            // Arrange
            var file1 = Path.Combine(_testFolder, "file1.txt");
            var file2 = Path.Combine(_testFolder, "file2.txt");
            File.WriteAllText(file1, new string('a', 500));
            File.WriteAllText(file2, new string('b', 300));
            new FileInfo(file1).LastWriteTime = DateTime.Now.AddDays(-100);
            new FileInfo(file2).LastWriteTime = DateTime.Now.AddDays(-100);

            // Act
            var result = FileCleanupLogic.Cleanup(
                _testFolder,
                deleteFiles: true,
                olderThanDays: 30,
                dryRun: true);

            // Assert
            result.FreedBytes.Should().Be(800);
        }
    }
}
