using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Sftp
{
    /// <summary>
    /// Тесты чистой логики SFTP-модуля: нормализация отпечатков host key,
    /// нормализация путей, сортировка листинга, валидация настроек подключения.
    /// </summary>
    [Trait("Category", "Fast")]
    public class SftpLogicTests
    {
        private readonly SftpLogic _logic;

        public SftpLogicTests()
        {
            _logic = new SftpLogic();
        }

        #region NormalizeFingerprint Tests

        [Fact]
        public void NormalizeFingerprint_SshKeygenFormat_StripsPrefixAndPadding()
        {
            _logic.NormalizeFingerprint("SHA256:AbCdEf123456=")
                .Should().Be("sha256:abcdef123456");
        }

        [Fact]
        public void NormalizeFingerprint_WithoutPrefix_AddsPrefix()
        {
            _logic.NormalizeFingerprint("ABC123")
                .Should().Be("sha256:abc123");
        }

        [Fact]
        public void NormalizeFingerprint_MixedCase_IsLowercased()
        {
            _logic.NormalizeFingerprint("sha256:DeAdBeEf").Should().Be("sha256:deadbeef");
            _logic.NormalizeFingerprint("DeAdBeEf").Should().Be("sha256:deadbeef");
        }

        [Fact]
        public void NormalizeFingerprint_Empty_ReturnsNull()
        {
            _logic.NormalizeFingerprint("").Should().BeNull();
            _logic.NormalizeFingerprint("  ").Should().BeNull();
            _logic.NormalizeFingerprint(null).Should().BeNull();
        }

        [Fact]
        public void HostKeyMatches_EquivalentFormats_ReturnsTrue()
        {
            _logic.HostKeyMatches("SHA256:abcDef=", "ABCDEF").Should().BeTrue();
            _logic.HostKeyMatches("abcdef", "sha256:ABCDEF").Should().BeTrue();
        }

        [Fact]
        public void HostKeyMatches_Different_ReturnsFalse()
        {
            _logic.HostKeyMatches("SHA256:aaa", "SHA256:bbb").Should().BeFalse();
        }

        [Fact]
        public void HostKeyMatches_EmptyExpected_ReturnsFalse()
        {
            _logic.HostKeyMatches("", "SHA256:bbb").Should().BeFalse();
        }

        #endregion

        #region NormalizeRemotePath Tests

        [Theory]
        [InlineData("/home/user/file.txt", null, "/home/user/file.txt")]
        [InlineData("home/user", null, "/home/user")]
        [InlineData("C:\\data\\file.txt", null, "/C:/data/file.txt")]
        [InlineData("/a//b///c", null, "/a/b/c")]
        [InlineData("", "/home/user", "/home/user")]
        [InlineData(".", null, "/.")]
        [InlineData("~/file.txt", "/home/user", "/home/user/file.txt")]
        public void NormalizeRemotePath_VariousInputs_Normalizes(string input, string home, string expected)
        {
            _logic.NormalizeRemotePath(input, home).Should().Be(expected);
        }

        #endregion

        #region OrderEntries Tests

        [Fact]
        public void OrderEntries_DirectoriesFirst_ThenByName()
        {
            var entries = new List<SftpEntry>
            {
                new SftpEntry { Name = "zebra.txt", IsDirectory = false },
                new SftpEntry { Name = "Docs", IsDirectory = true },
                new SftpEntry { Name = "apple.txt", IsDirectory = false },
                new SftpEntry { Name = "Archive", IsDirectory = true },
            };

            var ordered = _logic.OrderEntries(entries);

            ordered.Select(e => e.Name).Should().ContainInOrder("Archive", "Docs", "apple.txt", "zebra.txt");
        }

        [Fact]
        public void OrderEntries_RemovesDotEntries()
        {
            var entries = new List<SftpEntry>
            {
                new SftpEntry { Name = "." },
                new SftpEntry { Name = ".." },
                new SftpEntry { Name = "file.txt" },
            };

            var ordered = _logic.OrderEntries(entries);
            ordered.Select(e => e.Name).Should().ContainSingle().Which.Should().Be("file.txt");
        }

        [Fact]
        public void OrderEntries_NullEntries_Skipped()
        {
            var entries = new List<SftpEntry> { null, new SftpEntry { Name = "a" } };
            _logic.OrderEntries(entries).Should().ContainSingle();
        }

        #endregion

        #region CreateClient Validation Tests

        [Fact]
        public void CreateClient_MissingAuth_ThrowsArgument()
        {
            var settings = new SftpConnectionSettings { Host = "srv", UserName = "user" };
            Action act = () => _logic.CreateClient(settings);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*пароль или путь к приватному ключу*");
        }

        [Fact]
        public void CreateClient_EmptyHost_ThrowsArgument()
        {
            var settings = new SftpConnectionSettings { Host = "", UserName = "user", Password = "p" };
            Action act = () => _logic.CreateClient(settings);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateClient_MissingKeyFile_ThrowsFileNotFound()
        {
            var settings = new SftpConnectionSettings
            {
                Host = "srv", UserName = "user",
                PrivateKeyPath = "Z:/no/such/key.pem"
            };
            Action act = () => _logic.CreateClient(settings);
            act.Should().Throw<FileNotFoundException>();
        }

        [Fact]
        public void CreateClient_ValidPasswordConfig_AppliesTimeouts()
        {
            var settings = new SftpConnectionSettings
            {
                Host = "srv", UserName = "user", Password = "pass",
                // issue #11: без отпечатка/AllowAnyHostKey теперь fail-closed
                ExpectedFingerprint = "sha256:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
                ConnectionTimeoutSeconds = 5, OperationTimeoutSeconds = 7
            };
            using (var client = _logic.CreateClient(settings))
            {
                client.ConnectionInfo.Timeout.Should().Be(TimeSpan.FromSeconds(5));
                client.OperationTimeout.Should().Be(TimeSpan.FromSeconds(7));
            }
        }

        #endregion
    }
}
