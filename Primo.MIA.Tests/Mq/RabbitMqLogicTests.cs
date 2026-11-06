using System;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Mq
{
    /// <summary>
    /// Тесты чистой логики RabbitMQ-модуля: валидация настроек,
    /// фабрика ConnectionFactory, форматирование описания сообщения.
    /// </summary>
    [Trait("Category", "Fast")]
    public class RabbitMqLogicTests
    {
        private readonly RabbitMqLogic _logic;

        public RabbitMqLogicTests()
        {
            _logic = new RabbitMqLogic();
        }

        #region ValidateSettings Tests

        [Fact]
        public void ValidateSettings_EmptyHost_Throws()
        {
            Action act = () => _logic.ValidateSettings(new RabbitMqConnectionSettings { Host = "", UserName = "u" });
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ValidateSettings_EmptyUserName_Throws()
        {
            Action act = () => _logic.ValidateSettings(new RabbitMqConnectionSettings { Host = "h", UserName = "" });
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ValidateSettings_NullSettings_Throws()
        {
            Action act = () => _logic.ValidateSettings(null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ValidateSettings_ValidSettings_DoesNotThrow()
        {
            Action act = () => _logic.ValidateSettings(new RabbitMqConnectionSettings { Host = "h", UserName = "u", Port = 5672 });
            act.Should().NotThrow();
        }

        #endregion

        #region CreateFactory Tests

        [Fact]
        public void CreateFactory_ValidSettings_MapsProperties()
        {
            var settings = new RabbitMqConnectionSettings
            {
                Host = "broker.example",
                Port = 5672,
                UserName = "robot",
                Password = "secret",
                VirtualHost = "/prod",
                ConnectionTimeoutSeconds = 10
            };

            var factory = _logic.CreateFactory(settings);

            factory.HostName.Should().Be("broker.example");
            factory.Port.Should().Be(5672);
            factory.UserName.Should().Be("robot");
            factory.Password.Should().Be("secret");
            factory.VirtualHost.Should().Be("/prod");
            factory.RequestedConnectionTimeout.Should().Be(TimeSpan.FromSeconds(10));
            factory.AutomaticRecoveryEnabled.Should().BeFalse();
        }

        [Fact]
        public void CreateFactory_EmptyVirtualHost_FallsBackToRoot()
        {
            var settings = new RabbitMqConnectionSettings { Host = "h", UserName = "u", VirtualHost = "" };
            _logic.CreateFactory(settings).VirtualHost.Should().Be("/");
        }

        [Fact]
        public void CreateFactory_TlsEnabled_EnablesSslOption()
        {
            var settings = new RabbitMqConnectionSettings { Host = "h", UserName = "u", UseTls = true };
            var factory = _logic.CreateFactory(settings);
            factory.Ssl.Enabled.Should().BeTrue();
            factory.Ssl.ServerName.Should().Be("h");
        }

        [Fact]
        public void CreateFactory_TlsDisabled_SslDisabled()
        {
            var settings = new RabbitMqConnectionSettings { Host = "h", UserName = "u", UseTls = false };
            _logic.CreateFactory(settings).Ssl.Enabled.Should().BeFalse();
        }

        [Fact]
        public void CreateFactory_InvalidSettings_Throws()
        {
            Action act = () => _logic.CreateFactory(new RabbitMqConnectionSettings { Host = "h", UserName = "" });
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region DescribeMessage Tests

        [Fact]
        public void DescribeMessage_Null_ReturnsNotReceived()
        {
            _logic.DescribeMessage(null).Should().Contain("не получено");
        }

        [Fact]
        public void DescribeMessage_ShortBody_IncludedFully()
        {
            var message = new RabbitMqMessage { Body = "hello", RoutingKey = "tasks.q" };
            _logic.DescribeMessage(message).Should().Contain("hello").And.Contain("tasks.q");
        }

        [Fact]
        public void DescribeMessage_LongBody_TruncatedTo60PlusEllipsis()
        {
            var message = new RabbitMqMessage { Body = new string('x', 200) };
            var description = _logic.DescribeMessage(message);
            description.Should().Contain("...");
            description.Should().NotContain(new string('x', 100));
        }

        #endregion
    }
}
