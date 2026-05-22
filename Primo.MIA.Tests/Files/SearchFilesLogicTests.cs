using System;
using System.IO;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Files
{
    public class SearchFilesLogicTests : IDisposable
    {
        private readonly SearchFilesLogic _logic;
        private readonly string _testDirectory;

        public SearchFilesLogicTests()
        {
            _logic = new SearchFilesLogic();
            _testDirectory = Path.Combine(Path.GetTempPath(), "SearchFilesTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        #region Wildcard Tests

        [Fact]
        public void SearchFiles_WildcardAllFiles_ReturnsAllFiles()
        {
            // Arrange
            CreateTestFile("file1.txt");
            CreateTestFile("file2.log");
            CreateTestFile("document.pdf");

            // Act
            var result = _logic.SearchFiles(_testDirectory, "*", SearchFilterType.Wildcard, SearchType.FilesOnly, false);

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(f => f.EndsWith("file1.txt"));
            result.Should().Contain(f => f.EndsWith("file2.log"));
            result.Should().Contain(f => f.EndsWith("document.pdf"));
        }

        [Fact]
        public void SearchFiles_WildcardByExtension_ReturnsMatchingFiles()
        {
            // Arrange
            CreateTestFile("file1.txt");
            CreateTestFile("file2.txt");
            CreateTestFile("document.pdf");

            // Act
            var result = _logic.SearchFiles(_testDirectory, "*.txt", SearchFilterType.Wildcard, SearchType.FilesOnly, false);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(f => f.EndsWith("file1.txt"));
            result.Should().Contain(f => f.EndsWith("file2.txt"));
        }

        [Fact]
        public void SearchFiles_WildcardWithPrefix_ReturnsMatchingFiles()
        {
            // Arrange
            CreateTestFile("report_2025.xlsx");
            CreateTestFile("report_2024.xlsx");
            CreateTestFile("data_2025.xlsx");

            // Act
            var result = _logic.SearchFiles(_testDirectory, "report_*", SearchFilterType.Wildcard, SearchType.FilesOnly, false);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(f => f.EndsWith("report_2025.xlsx"));
            result.Should().Contain(f => f.EndsWith("report_2024.xlsx"));
        }

        #endregion

        #region Regex Tests

        [Fact]
        public void SearchFiles_RegexPattern_ReturnsMatchingFiles()
        {
            // Arrange
            CreateTestFile("report_20250101.xlsx");
            CreateTestFile("report_20250215.xlsx");
            CreateTestFile("report_invalid.xlsx");

            // Act
            var result = _logic.SearchFiles(_testDirectory, @"^report_\d{8}\.xlsx$", SearchFilterType.Regex, SearchType.FilesOnly, false);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(f => f.EndsWith("report_20250101.xlsx"));
            result.Should().Contain(f => f.EndsWith("report_20250215.xlsx"));
        }

        [Fact]
        public void SearchFiles_RegexCaseInsensitive_ReturnsMatchingFiles()
        {
            // Arrange
            CreateTestFile("File.TXT");
            CreateTestFile("FILE.LOG");
            CreateTestFile("data.txt");

            // Act - regex should match case-insensitively
            var result = _logic.SearchFiles(_testDirectory, @"^file\.(txt|log)$", SearchFilterType.Regex, SearchType.FilesOnly, false);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(f => f.EndsWith("File.TXT"));
            result.Should().Contain(f => f.EndsWith("FILE.LOG"));
        }

        #endregion

        #region Recursive Search Tests

        [Fact]
        public void SearchFiles_RecursiveSearch_ReturnsFilesFromSubfolders()
        {
            // Arrange
            CreateTestFile("root.txt");
            var subDir = Path.Combine(_testDirectory, "subfolder");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "nested.txt"), "content");

            // Act
            var result = _logic.SearchFiles(_testDirectory, "*.txt", SearchFilterType.Wildcard, SearchType.FilesOnly, true);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(f => f.EndsWith("root.txt"));
            result.Should().Contain(f => f.EndsWith("nested.txt"));
        }

        [Fact]
        public void SearchFiles_NonRecursiveSearch_ReturnsOnlyRootFiles()
        {
            // Arrange
            CreateTestFile("root.txt");
            var subDir = Path.Combine(_testDirectory, "subfolder");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "nested.txt"), "content");

            // Act
            var result = _logic.SearchFiles(_testDirectory, "*.txt", SearchFilterType.Wildcard, SearchType.FilesOnly, false);

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(f => f.EndsWith("root.txt"));
        }

        #endregion

        #region Folder Search Tests

        [Fact]
        public void SearchFiles_FoldersOnly_ReturnsOnlyFolders()
        {
            // Arrange
            CreateTestFile("file.txt");
            Directory.CreateDirectory(Path.Combine(_testDirectory, "folder1"));
            Directory.CreateDirectory(Path.Combine(_testDirectory, "folder2"));

            // Act
            var result = _logic.SearchFiles(_testDirectory, "*", SearchFilterType.Wildcard, SearchType.FoldersOnly, false);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(path => path.Should().EndWith("\\"));
            result.Should().Contain(f => f.Contains("folder1"));
            result.Should().Contain(f => f.Contains("folder2"));
        }

        [Fact]
        public void SearchFiles_FilesAndFolders_ReturnsBoth()
        {
            // Arrange
            CreateTestFile("file.txt");
            Directory.CreateDirectory(Path.Combine(_testDirectory, "folder1"));

            // Act
            var result = _logic.SearchFiles(_testDirectory, "*", SearchFilterType.Wildcard, SearchType.FilesAndFolders, false);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(f => f.EndsWith("file.txt"));
            result.Should().Contain(f => f.EndsWith("folder1\\"));
        }

        [Fact]
        public void SearchFiles_FoldersWithWildcard_ReturnsMatchingFolders()
        {
            // Arrange
            Directory.CreateDirectory(Path.Combine(_testDirectory, "test_folder1"));
            Directory.CreateDirectory(Path.Combine(_testDirectory, "test_folder2"));
            Directory.CreateDirectory(Path.Combine(_testDirectory, "other_folder"));

            // Act
            var result = _logic.SearchFiles(_testDirectory, "test_*", SearchFilterType.Wildcard, SearchType.FoldersOnly, false);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(f => f.Contains("test_folder1"));
            result.Should().Contain(f => f.Contains("test_folder2"));
        }

        #endregion

        #region Edge Cases and Validation

        [Fact]
        public void SearchFiles_EmptyDirectory_ReturnsEmptyList()
        {
            // Act
            var result = _logic.SearchFiles(_testDirectory, "*", SearchFilterType.Wildcard, SearchType.FilesOnly, false);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void SearchFiles_NoMatchingFiles_ReturnsEmptyList()
        {
            // Arrange
            CreateTestFile("file.txt");

            // Act
            var result = _logic.SearchFiles(_testDirectory, "*.pdf", SearchFilterType.Wildcard, SearchType.FilesOnly, false);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void SearchFiles_EmptyDirectoryPath_ThrowsArgumentException()
        {
            // Act & Assert
            Action act = () => _logic.SearchFiles("", "*", SearchFilterType.Wildcard, SearchType.FilesOnly, false);
            act.Should().Throw<ArgumentException>().WithMessage("*Путь к директории*");
        }

        [Fact]
        public void SearchFiles_NonExistentDirectory_ThrowsDirectoryNotFoundException()
        {
            // Act & Assert
            Action act = () => _logic.SearchFiles(@"C:\NonExistentDirectory", "*", SearchFilterType.Wildcard, SearchType.FilesOnly, false);
            act.Should().Throw<DirectoryNotFoundException>();
        }

        [Fact]
        public void SearchFiles_EmptyPattern_ThrowsArgumentException()
        {
            // Act & Assert
            Action act = () => _logic.SearchFiles(_testDirectory, "", SearchFilterType.Wildcard, SearchType.FilesOnly, false);
            act.Should().Throw<ArgumentException>().WithMessage("*Паттерн*");
        }

        [Fact]
        public void SearchFiles_InvalidRegexPattern_ThrowsArgumentException()
        {
            // Arrange
            CreateTestFile("file.txt");

            // Act & Assert
            Action act = () => _logic.SearchFiles(_testDirectory, "[invalid(regex", SearchFilterType.Regex, SearchType.FilesOnly, false);
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region Sorting Tests

        [Fact]
        public void SearchFiles_ResultsAreSorted_ReturnsAlphabeticallySortedList()
        {
            // Arrange
            CreateTestFile("zebra.txt");
            CreateTestFile("apple.txt");
            CreateTestFile("banana.txt");

            // Act
            var result = _logic.SearchFiles(_testDirectory, "*.txt", SearchFilterType.Wildcard, SearchType.FilesOnly, false);

            // Assert
            result.Should().HaveCount(3);
            result[0].Should().EndWith("apple.txt");
            result[1].Should().EndWith("banana.txt");
            result[2].Should().EndWith("zebra.txt");
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
