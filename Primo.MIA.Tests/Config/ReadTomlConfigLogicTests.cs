using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Primo.MIA;
using Primo.MIA.Tests.Logic;
using Xunit;

namespace Primo.MIA.Tests.Config
{
    public class ReadTomlConfigLogicTests : IDisposable
    {
        private readonly ReadTomlConfigLogic _logic;
        private readonly string _testDirectory;

        public ReadTomlConfigLogicTests()
        {
            _logic = new ReadTomlConfigLogic();
            _testDirectory = Path.Combine(Path.GetTempPath(), "TomlConfigTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        #region SingleValue Mode Tests

        [Fact]
        public void ReadSingleValue_SimpleKey_ReturnsValue()
        {
            // Arrange
            var tomlContent = @"
name = ""TestApp""
version = ""1.0.0""
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var (found, value) = _logic.ReadSingleValue(filePath, "name");

            // Assert
            found.Should().BeTrue();
            value.Should().Be("TestApp");
        }

        [Fact]
        public void ReadSingleValue_NestedKey_ReturnsValue()
        {
            // Arrange
            var tomlContent = @"
[database]
host = ""localhost""
port = 5432
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var (found, value) = _logic.ReadSingleValue(filePath, "database.host");

            // Assert
            found.Should().BeTrue();
            value.Should().Be("localhost");
        }

        [Fact]
        public void ReadSingleValue_DeepNesting_ReturnsValue()
        {
            // Arrange
            var tomlContent = @"
[app]
[app.database]
[app.database.connection]
timeout = 30
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var (found, value) = _logic.ReadSingleValue(filePath, "app.database.connection.timeout");

            // Assert
            found.Should().BeTrue();
            value.Should().Be("30");
        }

        [Fact]
        public void ReadSingleValue_KeyNotFound_ReturnsDefaultValue()
        {
            // Arrange
            var tomlContent = @"
name = ""TestApp""
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var (found, value) = _logic.ReadSingleValue(filePath, "nonexistent", "default_value");

            // Assert
            found.Should().BeFalse();
            value.Should().Be("default_value");
        }

        [Fact]
        public void ReadSingleValue_BooleanValue_ReturnsLowerCase()
        {
            // Arrange
            var tomlContent = @"
enabled = true
disabled = false
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var (found1, value1) = _logic.ReadSingleValue(filePath, "enabled");
            var (found2, value2) = _logic.ReadSingleValue(filePath, "disabled");

            // Assert
            found1.Should().BeTrue();
            value1.Should().Be("true");
            found2.Should().BeTrue();
            value2.Should().Be("false");
        }

        [Fact]
        public void ReadSingleValue_NumericValue_ReturnsString()
        {
            // Arrange
            var tomlContent = @"
port = 8080
timeout = 30.5
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var (found1, value1) = _logic.ReadSingleValue(filePath, "port");
            var (found2, value2) = _logic.ReadSingleValue(filePath, "timeout");

            // Assert
            found1.Should().BeTrue();
            value1.Should().Be("8080");
            found2.Should().BeTrue();
            value2.Should().Be("30.5");
        }

        #endregion

        #region SectionToDictionary Mode Tests

        [Fact]
        public void ReadSectionToDictionary_SimpleSection_ReturnsDictionary()
        {
            // Arrange
            var tomlContent = @"
[database]
host = ""localhost""
port = 5432
user = ""admin""
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var result = _logic.ReadSectionToDictionary(filePath, "database");

            // Assert
            result.Should().HaveCount(3);
            result["host"].Should().Be("localhost");
            result["port"].Should().Be("5432");
            result["user"].Should().Be("admin");
        }

        [Fact]
        public void ReadSectionToDictionary_NestedSection_ReturnsDictionary()
        {
            // Arrange
            var tomlContent = @"
[app]
name = ""TestApp""

[app.database]
host = ""localhost""
port = 5432
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var result = _logic.ReadSectionToDictionary(filePath, "app.database");

            // Assert
            result.Should().HaveCount(2);
            result["host"].Should().Be("localhost");
            result["port"].Should().Be("5432");
        }

        [Fact]
        public void ReadSectionToDictionary_SectionNotFound_ReturnsEmptyDictionary()
        {
            // Arrange
            var tomlContent = @"
[database]
host = ""localhost""
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var result = _logic.ReadSectionToDictionary(filePath, "nonexistent");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ReadSectionToDictionary_SectionWithSubsections_ExcludesSubsections()
        {
            // Arrange
            var tomlContent = @"
[server]
host = ""localhost""
port = 8080

[server.ssl]
enabled = true
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var result = _logic.ReadSectionToDictionary(filePath, "server");

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainKey("host");
            result.Should().ContainKey("port");
            result.Should().NotContainKey("ssl");
        }

        #endregion

        #region FullFileToDictionary Mode Tests

        [Fact]
        public void ReadFullFileToDictionary_SimpleFile_ReturnsAllKeys()
        {
            // Arrange
            var tomlContent = @"
name = ""TestApp""
version = ""1.0.0""

[database]
host = ""localhost""
port = 5432
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var result = _logic.ReadFullFileToDictionary(filePath);

            // Assert
            result.Should().HaveCount(4);
            result["name"].Should().Be("TestApp");
            result["version"].Should().Be("1.0.0");
            result["database.host"].Should().Be("localhost");
            result["database.port"].Should().Be("5432");
        }

        [Fact]
        public void ReadFullFileToDictionary_NestedSections_FlattenedKeys()
        {
            // Arrange
            var tomlContent = @"
[app]
name = ""TestApp""

[app.database]
host = ""localhost""

[app.database.connection]
timeout = 30
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var result = _logic.ReadFullFileToDictionary(filePath);

            // Assert
            result.Should().HaveCount(3);
            result["app.name"].Should().Be("TestApp");
            result["app.database.host"].Should().Be("localhost");
            result["app.database.connection.timeout"].Should().Be("30");
        }

        [Fact]
        public void ReadFullFileToDictionary_EmptyFile_ReturnsEmptyDictionary()
        {
            // Arrange
            var tomlContent = "";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var result = _logic.ReadFullFileToDictionary(filePath);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region ReadProfile Mode Tests

        [Fact]
        public void ReadProfile_DefaultThenProfile_MergesCorrectly()
        {
            // Arrange
            var tomlContent = @"
[default]
host = ""localhost""
port = 8080
timeout = 30

[production]
host = ""prod.example.com""
port = 443
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var (merged, fromProfile, fromDefault, profiles) = _logic.ReadProfile(
                filePath,
                "production",
                "default",
                ProfileMergeStrategy.DefaultThenProfile);

            // Assert
            merged.Should().HaveCount(3);
            merged["host"].Should().Be("prod.example.com"); // Overridden by profile
            merged["port"].Should().Be("443"); // Overridden by profile
            merged["timeout"].Should().Be("30"); // From default
            fromProfile.Should().Be(2);
            fromDefault.Should().Be(1);
            profiles.Should().Contain("default");
            profiles.Should().Contain("production");
        }

        [Fact]
        public void ReadProfile_ProfileOnly_ReturnsOnlyProfile()
        {
            // Arrange
            var tomlContent = @"
[default]
host = ""localhost""
port = 8080

[production]
host = ""prod.example.com""
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var (merged, fromProfile, fromDefault, profiles) = _logic.ReadProfile(
                filePath,
                "production",
                "default",
                ProfileMergeStrategy.ProfileOnly);

            // Assert
            merged.Should().HaveCount(1);
            merged["host"].Should().Be("prod.example.com");
            merged.Should().NotContainKey("port");
            fromProfile.Should().Be(1);
            fromDefault.Should().Be(0);
        }

        [Fact]
        public void ReadProfile_ProfileThenDefault_AddsOnlyMissingKeys()
        {
            // Arrange
            var tomlContent = @"
[default]
host = ""localhost""
port = 8080
timeout = 30

[production]
host = ""prod.example.com""
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var (merged, fromProfile, fromDefault, profiles) = _logic.ReadProfile(
                filePath,
                "production",
                "default",
                ProfileMergeStrategy.ProfileThenDefault);

            // Assert
            merged.Should().HaveCount(3);
            merged["host"].Should().Be("prod.example.com"); // From profile
            merged["port"].Should().Be("8080"); // Added from default
            merged["timeout"].Should().Be("30"); // Added from default
            fromProfile.Should().Be(1);
            fromDefault.Should().Be(2);
        }

        [Fact]
        public void ReadProfile_ProfileNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var tomlContent = @"
[default]
host = ""localhost""
";
            var filePath = CreateTomlFile(tomlContent);

            // Act & Assert
            Action act = () => _logic.ReadProfile(filePath, "nonexistent");
            act.Should().Throw<KeyNotFoundException>().WithMessage("*nonexistent*");
        }

        [Fact]
        public void ReadProfile_AvailableProfiles_ReturnsAllSections()
        {
            // Arrange
            var tomlContent = @"
[default]
host = ""localhost""

[production]
host = ""prod.example.com""

[staging]
host = ""staging.example.com""

[development]
host = ""dev.example.com""
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var (_, _, _, profiles) = _logic.ReadProfile(filePath, "production");

            // Assert
            profiles.Should().HaveCount(4);
            profiles.Should().Contain("default");
            profiles.Should().Contain("production");
            profiles.Should().Contain("staging");
            profiles.Should().Contain("development");
        }

        #endregion

        #region Validation Tests

        [Fact]
        public void ReadSingleValue_EmptyFilePath_ThrowsArgumentException()
        {
            // Act & Assert
            Action act = () => _logic.ReadSingleValue("", "key");
            act.Should().Throw<ArgumentException>().WithMessage("*Путь к файлу*");
        }

        [Fact]
        public void ReadSingleValue_FileNotFound_ThrowsFileNotFoundException()
        {
            // Act & Assert
            Action act = () => _logic.ReadSingleValue(@"C:\NonExistent\file.toml", "key");
            act.Should().Throw<FileNotFoundException>();
        }

        [Fact]
        public void ReadSingleValue_EmptyKey_ThrowsArgumentException()
        {
            // Arrange
            var filePath = CreateTomlFile("name = \"test\"");

            // Act & Assert
            Action act = () => _logic.ReadSingleValue(filePath, "");
            act.Should().Throw<ArgumentException>().WithMessage("*Ключ*");
        }

        [Fact]
        public void ReadSectionToDictionary_EmptyFilePath_ThrowsArgumentException()
        {
            // Act & Assert
            Action act = () => _logic.ReadSectionToDictionary("", "section");
            act.Should().Throw<ArgumentException>().WithMessage("*Путь к файлу*");
        }

        [Fact]
        public void ReadSectionToDictionary_EmptySectionName_ThrowsArgumentException()
        {
            // Arrange
            var filePath = CreateTomlFile("[section]\nkey = \"value\"");

            // Act & Assert
            Action act = () => _logic.ReadSectionToDictionary(filePath, "");
            act.Should().Throw<ArgumentException>().WithMessage("*Имя секции*");
        }

        [Fact]
        public void ReadProfile_EmptyProfileName_ThrowsArgumentException()
        {
            // Arrange
            var filePath = CreateTomlFile("[default]\nkey = \"value\"");

            // Act & Assert
            Action act = () => _logic.ReadProfile(filePath, "");
            act.Should().Throw<ArgumentException>().WithMessage("*Имя профиля*");
        }

        #endregion

        #region Complex Data Types Tests

        [Fact]
        public void ReadSingleValue_ArrayValue_ReturnsFormattedString()
        {
            // Arrange
            var tomlContent = @"
ports = [8080, 8081, 8082]
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var (found, value) = _logic.ReadSingleValue(filePath, "ports");

            // Assert
            found.Should().BeTrue();
            value.Should().Contain("8080");
            value.Should().Contain("8081");
            value.Should().Contain("8082");
        }

        [Fact]
        public void ReadFullFileToDictionary_WithArrays_IncludesArrays()
        {
            // Arrange
            var tomlContent = @"
name = ""TestApp""
ports = [8080, 8081]
";
            var filePath = CreateTomlFile(tomlContent);

            // Act
            var result = _logic.ReadFullFileToDictionary(filePath);

            // Assert
            result.Should().HaveCount(2);
            result["name"].Should().Be("TestApp");
            result["ports"].Should().Contain("8080");
        }

        #endregion

        #region Helper Methods

        private string CreateTomlFile(string content, string fileName = null)
        {
            fileName = fileName ?? $"test_{Guid.NewGuid():N}.toml";
            var filePath = Path.Combine(_testDirectory, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        #endregion
    }
}
