using System;
using FluentAssertions;
using Renci.SshNet;
using Xunit;

namespace Primo.MIA.Tests
{
    /// <summary>
    /// Тесты fail-closed политики host key (issue #11):
    /// пустой отпечаток без явного AllowAnyHostKey → SftpException,
    /// а не молчаливое принятие любого ключа (дефолт SSH.NET).
    /// </summary>
    public class SftpHostKeyPolicyTests
    {
        private readonly SftpLogic _logic = new SftpLogic();

        [Fact]
        [Trait("Category", "Fast")]
        public void ApplyHostKeyPolicy_EmptyFingerprint_ThrowsFailClosed()
        {
            var client = new SftpClient("localhost", 22, "user", "pass");
            var settings = new SftpConnectionSettings
            {
                Host = "localhost",
                UserName = "user",
                ExpectedFingerprint = null
            };

            Action act = () => _logic.ApplyHostKeyPolicy(client, settings);

            act.Should().Throw<SftpException>()
                .WithMessage("*ExpectedFingerprint*");
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void ApplyHostKeyPolicy_WhitespaceFingerprint_ThrowsFailClosed()
        {
            var client = new SftpClient("localhost", 22, "user", "pass");
            var settings = new SftpConnectionSettings
            {
                Host = "localhost",
                UserName = "user",
                ExpectedFingerprint = "   "
            };

            Action act = () => _logic.ApplyHostKeyPolicy(client, settings);

            act.Should().Throw<SftpException>();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void ApplyHostKeyPolicy_ExplicitAllowAnyHostKey_Allowed()
        {
            var client = new SftpClient("localhost", 22, "user", "pass");
            var settings = new SftpConnectionSettings
            {
                Host = "localhost",
                UserName = "user",
                AllowAnyHostKey = true
            };

            Action act = () => _logic.ApplyHostKeyPolicy(client, settings);
            act.Should().NotThrow();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void ApplyHostKeyPolicy_FingerprintSet_AttachesValidator()
        {
            var client = new SftpClient("localhost", 22, "user", "pass");
            var settings = new SftpConnectionSettings
            {
                Host = "localhost",
                UserName = "user",
                ExpectedFingerprint = "sha256:AAAA="
            };

            Action act = () => _logic.ApplyHostKeyPolicy(client, settings);
            act.Should().NotThrow();
        }
    }
}
