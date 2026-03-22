using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Files
{
    public class WaitForFileLogicTests : IDisposable
    {
        private readonly WaitForFileLogic _logic;
        private readonly string _testDirectory;

        public WaitForFileLogicTests()
        {
            _logic = new WaitForFileLogic();
            _testDirectory = Path.Combine(Path.GetTempPath(), "WaitForFileTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        #region WaitForAnyFile Mode Tests

        [Fact]
        public void WaitForFile_ExistingFile_ReturnsImmediately()
        {
            // Arrange
            var fileName = "existing.txt";
            CreateTestFile(fileName);

            // Act
            var result = _logic.WaitForFile(
                _testDirectory,
                fileName,
                FileFilterType.Exact,
                WaitFileMode.WaitForAnyFile,
                5000,
                100);

            // Assert
            result.FileFound.Should().BeTrue();
            result.FileName.Should().Be(fileName);
            result.FilePath.Should().EndWith(fileName);
            result.FileSize.Should().BeGreaterThan(0);
            result.WaitTimeMs.Should().BeLessThan(1000);
        }

        [Fact]
        public void WaitForFile_WildcardExistingFile_ReturnsImmediately()
        {
            // Arrange
            CreateTestFile("report_2025.xlsx");

            // Act
            var result = _logic.WaitForFile(
                _testDirectory,
                "*.xlsx",
                FileFilterType.Wildcard,
                WaitFileMode.WaitForAnyFile,
                5000,
                100);

            // Assert
            result.FileFound.Should().BeTrue();
            result.FileName.Should().Be("report_2025.xlsx");
        }

        [Fact]
        public void WaitForFile_RegexExistingFile_ReturnsImmediately()
        {
            // Arrange
            CreateTestFile("report_20250215.xlsx");

            // Act
            var result = _logic.WaitForFile(
                _testDirectory,
                @"^report_\d{8}\.xlsx$",
                FileFilterType.Regex,
                WaitFileMode.WaitForAnyFile,
                5000,
                100);

            // Assert
            result.FileFound.Should().BeTrue();
            result.FileName.Should().Be("report_20250215.xlsx");
        }

        #endregion

        #region WaitForNewFile Mode Tests

        [Fact]
        public void WaitForFile_NewFileMode_IgnoresExistingFiles()
        {
            // Arrange
            CreateTestFile("existing.txt");

            // Act
            var result = _logic.WaitForFile(
                _testDirectory,
                "*.txt",
                FileFilterType.Wildcard,
                WaitFileMode.WaitForNewFile,
                1000,
                100);

            // Assert
            result.FileFound.Should().BeFalse();
            result.WaitTimeMs.Should().BeGreaterThan(900);
        }

        [Fact]
        public void WaitForFile_NewFileMode_DetectsNewFile()
        {
            // Arrange
            var fileName = "new_file.txt";

            // Act - создаем файл в фоновом потоке
            Task.Run(async () =>
            {
                await Task.Delay(300);
                CreateTestFile(fileName);
            });

            var result = _logic.WaitForFile(
                _testDirectory,
                fileName,
                FileFilterType.Exact,
                WaitFileMode.WaitForNewFile,
                5000,
                100);

            // Assert
            result.FileFound.Should().BeTrue();
            result.FileName.Should().Be(fileName);
            result.WaitTimeMs.Should().BeGreaterThan(200);
            result.WaitTimeMs.Should().BeLessThan(2000);
        }

        #endregion

        #region Timeout Tests

        [Fact]
        public void WaitForFile_Timeout_ReturnsNotFound()
        {
            // Act
            var result = _logic.WaitForFile(
                _testDirectory,
                "nonexistent.txt",
                FileFilterType.Exact,
                WaitFileMode.WaitForAnyFile,
                500,
                100);

            // Assert
            result.FileFound.Should().BeFalse();
            result.FilePath.Should().BeEmpty();
            result.FileName.Should().BeEmpty();
            result.FileSize.Should().Be(0);
            result.WaitTimeMs.Should().BeGreaterThan(400);
        }

        [Fact]
        public void WaitForFile_FileAppearsBeforeTimeout_ReturnsFound()
        {
            // Arrange
            var fileName = "delayed_file.txt";

            // Act - создаем файл с задержкой
            Task.Run(async () =>
            {
                await Task.Delay(200);
                CreateTestFile(fileName);
            });

            var result = _logic.WaitForFile(
                _testDirectory,
                fileName,
                FileFilterType.Exact,
                WaitFileMode.WaitForAnyFile,
                3000,
                100);

            // Assert
            result.FileFound.Should().BeTrue();
            result.FileName.Should().Be(fileName);
        }

        #endregion

        #region Filter Type Tests

        [Fact]
        public void WaitForFile_ExactMatch_FindsOnlyExactName()
        {
            // Arrange
            CreateTestFile("file.txt");
            CreateTestFile("file2.txt");

            // Act
            var result = _logic.WaitForFile(
                _testDirectory,
                "file.txt",
                FileFilterType.Exact,
                WaitFileMode.WaitForAnyFile,
                1000,
                100);

            // Assert
            result.FileFound.Should().BeTrue();
            result.FileName.Should().Be("file.txt");
        }

        [Fact]
        public void WaitForFile_WildcardPattern_FindsMatchingFiles()
        {
            // Arrange
            CreateTestFile("report_2025.xlsx");
            CreateTestFile("data_2025.xlsx");

            // Act
            var result = _logic.WaitForFile(
                _testDirectory,
                "report_*.xlsx",
                FileFilterType.Wildcard,
                WaitFileMode.WaitForAnyFile,
                1000,
                100);

            // Assert
            result.FileFound.Should().BeTrue();
            result.FileName.Should().Be("report_2025.xlsx");
        }

        [Fact]
        public void WaitForFile_RegexPattern_FindsMatchingFiles()
        {
            // Arrange
            CreateTestFile("log_20250215.txt");
            CreateTestFile("log_invalid.txt");

            // Act
            var result = _logic.WaitForFile(
                _testDirectory,
                @"^log_\d{8}\.txt$",
                FileFilterType.Regex,
                WaitFileMode.WaitForAnyFile,
                1000,
                100);

            // Assert
            result.FileFound.Should().BeTrue();
            result.FileName.Should().Be("log_20250215.txt");
        }

        #endregion

        #region Multiple Files Tests

        [Fact]
        public void WaitForFile_MultipleMatchingFiles_ReturnsOldestFile()
        {
            // Arrange
            CreateTestFile("file1.txt");
            Thread.Sleep(100);
            CreateTestFile("file2.txt");
            Thread.Sleep(100);
            CreateTestFile("file3.txt");

            // Act
            var result = _logic.WaitForFile(
                _testDirectory,
                "*.txt",
                FileFilterType.Wildcard,
                WaitFileMode.WaitForAnyFile,
                1000,
                100);

            // Assert
            result.FileFound.Should().BeTrue();
            result.FileName.Should().Be("file1.txt");
        }

        #endregion

        #region Validation Tests

        [Fact]
        public void WaitForFile_EmptyDirectoryPath_ThrowsArgumentException()
        {
            // Act & Assert
            Action act = () => _logic.WaitForFile(
                "",
                "*.txt",
                FileFilterType.Wildcard,
                WaitFileMode.WaitForAnyFile,
                1000,
                100);

            act.Should().Throw<ArgumentException>().WithMessage("*Путь к директории*");
        }

        [Fact]
        public void WaitForFile_NonExistentDirectory_ThrowsDirectoryNotFoundException()
        {
            // Act & Assert
            Action act = () => _logic.WaitForFile(
                @"C:\NonExistentDirectory",
                "*.txt",
                FileFilterType.Wildcard,
                WaitFileMode.WaitForAnyFile,
                1000,
                100);

            act.Should().Throw<DirectoryNotFoundException>();
        }

        [Fact]
        public void WaitForFile_EmptyPattern_ThrowsArgumentException()
        {
            // Act & Assert
            Action act = () => _logic.WaitForFile(
                _testDirectory,
                "",
                FileFilterType.Wildcard,
                WaitFileMode.WaitForAnyFile,
                1000,
                100);

            act.Should().Throw<ArgumentException>().WithMessage("*Маска файла*");
        }

        [Fact]
        public void WaitForFile_ZeroTimeout_ThrowsArgumentException()
        {
            // Act & Assert
            Action act = () => _logic.WaitForFile(
                _testDirectory,
                "*.txt",
                FileFilterType.Wildcard,
                WaitFileMode.WaitForAnyFile,
                0,
                100);

            act.Should().Throw<ArgumentException>().WithMessage("*Таймаут*");
        }

        [Fact]
        public void WaitForFile_NegativeTimeout_ThrowsArgumentException()
        {
            // Act & Assert
            Action act = () => _logic.WaitForFile(
                _testDirectory,
                "*.txt",
                FileFilterType.Wildcard,
                WaitFileMode.WaitForAnyFile,
                -1000,
                100);

            act.Should().Throw<ArgumentException>().WithMessage("*Таймаут*");
        }

        [Fact]
        public void WaitForFile_ZeroCheckInterval_ThrowsArgumentException()
        {
            // Act & Assert
            Action act = () => _logic.WaitForFile(
                _testDirectory,
                "*.txt",
                FileFilterType.Wildcard,
                WaitFileMode.WaitForAnyFile,
                1000,
                0);

            act.Should().Throw<ArgumentException>().WithMessage("*Интервал проверки*");
        }

        [Fact]
        public void WaitForFile_InvalidRegexPattern_ThrowsArgumentException()
        {
            // Act & Assert
            Action act = () => _logic.WaitForFile(
                _testDirectory,
                "[invalid(regex",
                FileFilterType.Regex,
                WaitFileMode.WaitForAnyFile,
                1000,
                100);

            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region Case Sensitivity Tests

        [Fact]
        public void WaitForFile_CaseInsensitiveMatch_FindsFile()
        {
            // Arrange
            CreateTestFile("FILE.TXT");

            // Act
            var result = _logic.WaitForFile(
                _testDirectory,
                "file.txt",
                FileFilterType.Exact,
                WaitFileMode.WaitForAnyFile,
                1000,
                100);

            // Assert
            result.FileFound.Should().BeTrue();
            result.FileName.Should().Be("FILE.TXT");
        }

        #endregion

        #region Helper Methods

        private void CreateTestFile(string fileName, string content = "test content")
        {
            File.WriteAllText(Path.Combine(_testDirectory, fileName), content);
        }

        #endregion
    }
}
