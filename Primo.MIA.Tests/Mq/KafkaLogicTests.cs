using System;
using System.Linq;
using Confluent.Kafka;
using FluentAssertions;
using Moq;
using Xunit;

namespace Primo.MIA.Tests
{
    /// <summary>
    /// Тесты чистой логики Kafka-модуля (issue #19): валидация, построение
    /// конфигураций Producer/Consumer, маппинги enum'ов, DTO — без живого брокера.
    /// </summary>
    public class KafkaLogicTests
    {
        private readonly KafkaLogic _logic = new KafkaLogic();

        private static KafkaConnectionSettings DefaultSettings(
            Action<KafkaConnectionSettings> setup = null)
        {
            var settings = new KafkaConnectionSettings
            {
                BootstrapServers = "localhost:9092"
            };
            setup?.Invoke(settings);
            return settings;
        }

        // =====================================================================
        // Валидация подключения
        // =====================================================================

        #region ValidateSettings

        [Fact]
        [Trait("Category", "Fast")]
        public void ValidateSettings_Valid_Plaintext_NoThrow()
        {
            Action act = () => _logic.ValidateSettings(DefaultSettings());
            act.Should().NotThrow();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void ValidateSettings_NullSettings_ThrowsArgumentNullException()
        {
            Action act = () => _logic.ValidateSettings(null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void ValidateSettings_EmptyBootstrapServers_ThrowsArgumentException()
        {
            var settings = DefaultSettings(s => s.BootstrapServers = "");
            Action act = () => _logic.ValidateSettings(settings);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void ValidateSettings_SaslWithoutUsername_ThrowsArgumentException()
        {
            var settings = DefaultSettings(s =>
            {
                s.SecurityProtocol = KafkaSecurityProtocol.SaslSsl;
                s.SaslMechanism = KafkaSaslMechanism.ScramSha256;
                s.SaslUsername = null;
            });
            Action act = () => _logic.ValidateSettings(settings);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void ValidateSettings_SaslWithUsernameAndMechanism_NoThrow()
        {
            var settings = DefaultSettings(s =>
            {
                s.SecurityProtocol = KafkaSecurityProtocol.SaslPlaintext;
                s.SaslMechanism = KafkaSaslMechanism.Plain;
                s.SaslUsername = "user";
                s.SaslPassword = "pass";
            });
            Action act = () => _logic.ValidateSettings(settings);
            act.Should().NotThrow();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void ValidateSettings_SaslPlaintextWithMechanismNone_Throws()
        {
            var settings = DefaultSettings(s =>
            {
                s.SecurityProtocol = KafkaSecurityProtocol.SaslPlaintext;
                s.SaslMechanism = KafkaSaslMechanism.None;
                s.SaslUsername = "user";
            });
            Action act = () => _logic.ValidateSettings(settings);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*механизм*");
        }

        #endregion

        // =====================================================================
        // Валидация опций консьюмера
        // =====================================================================

        #region ValidateConsumerOptions

        [Fact]
        [Trait("Category", "Fast")]
        public void ValidateConsumerOptions_ValidGroupId_NoThrow()
        {
            var options = new KafkaConsumerOptions { GroupId = "rpa-group" };
            Action act = () => _logic.ValidateConsumerOptions(options);
            act.Should().NotThrow();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void ValidateConsumerOptions_EmptyGroupId_Throws()
        {
            var options = new KafkaConsumerOptions { GroupId = "  " };
            Action act = () => _logic.ValidateConsumerOptions(options);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void ValidateConsumerOptions_Null_ThrowsArgumentNullException()
        {
            Action act = () => _logic.ValidateConsumerOptions(null);
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        // =====================================================================
        // Конфигурации (чистые функции)
        // =====================================================================

        #region BuildProducerConfig

        [Fact]
        [Trait("Category", "Fast")]
        public void BuildProducerConfig_Plaintext_OnlyBootstrapServers()
        {
            var config = _logic.BuildProducerConfig(DefaultSettings());

            config.BootstrapServers.Should().Be("localhost:9092");
            config.SecurityProtocol.Should().Be(SecurityProtocol.Plaintext);
            config.SaslMechanism.Should().BeNull();
            config.SaslUsername.Should().BeNull();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void BuildProducerConfig_SaslSsl_SetsSecurityAndSaslKeys()
        {
            var config = _logic.BuildProducerConfig(DefaultSettings(s =>
            {
                s.SecurityProtocol = KafkaSecurityProtocol.SaslSsl;
                s.SaslMechanism = KafkaSaslMechanism.ScramSha256;
                s.SaslUsername = "svc-rpa";
                s.SaslPassword = "s3cret";
            }));

            config.SecurityProtocol.Should().Be(SecurityProtocol.SaslSsl);
            config.SaslMechanism.Should().Be(SaslMechanism.ScramSha256);
            config.SaslUsername.Should().Be("svc-rpa");
            config.SaslPassword.Should().Be("s3cret");
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void BuildProducerConfig_InvalidSettings_Throws()
        {
            Action act = () => _logic.BuildProducerConfig(DefaultSettings(s => s.BootstrapServers = null));
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region BuildConsumerConfig

        [Fact]
        [Trait("Category", "Fast")]
        public void BuildConsumerConfig_Defaults_GroupIdLatestAutoCommit()
        {
            var config = _logic.BuildConsumerConfig(
                DefaultSettings(),
                new KafkaConsumerOptions { GroupId = "g1" });

            config.BootstrapServers.Should().Be("localhost:9092");
            config.GroupId.Should().Be("g1");
            config.AutoOffsetReset.Should().Be(AutoOffsetReset.Latest);
            config.EnableAutoCommit.Should().BeTrue();
            config.SecurityProtocol.Should().Be(SecurityProtocol.Plaintext);
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void BuildConsumerConfig_EarliestNoCommit_Applied()
        {
            var config = _logic.BuildConsumerConfig(
                DefaultSettings(),
                new KafkaConsumerOptions
                {
                    GroupId = "g1",
                    AutoOffsetReset = KafkaAutoOffsetReset.Earliest,
                    AutoCommit = false
                });

            config.AutoOffsetReset.Should().Be(AutoOffsetReset.Earliest);
            config.EnableAutoCommit.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void BuildConsumerConfig_SaslCredentials_Applied()
        {
            var config = _logic.BuildConsumerConfig(
                DefaultSettings(s =>
                {
                    s.SecurityProtocol = KafkaSecurityProtocol.SaslSsl;
                    s.SaslMechanism = KafkaSaslMechanism.ScramSha512;
                    s.SaslUsername = "u";
                    s.SaslPassword = "p";
                }),
                new KafkaConsumerOptions { GroupId = "g1" });

            config.SaslMechanism.Should().Be(SaslMechanism.ScramSha512);
            config.SecurityProtocol.Should().Be(SecurityProtocol.SaslSsl);
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void BuildConsumerConfig_EmptyGroupId_Throws()
        {
            Action act = () => _logic.BuildConsumerConfig(
                DefaultSettings(), new KafkaConsumerOptions { GroupId = null });
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        // =====================================================================
        // Маппинги enum
        // =====================================================================

        #region Mappings

        [Theory]
        [Trait("Category", "Fast")]
        [InlineData(KafkaSecurityProtocol.Plaintext, SecurityProtocol.Plaintext)]
        [InlineData(KafkaSecurityProtocol.Ssl, SecurityProtocol.Ssl)]
        [InlineData(KafkaSecurityProtocol.SaslPlaintext, SecurityProtocol.SaslPlaintext)]
        [InlineData(KafkaSecurityProtocol.SaslSsl, SecurityProtocol.SaslSsl)]
        public void MapSecurityProtocol_AllValues_MapsCorrectly(
            KafkaSecurityProtocol input, SecurityProtocol expected)
        {
            _logic.MapSecurityProtocol(input).Should().Be(expected);
        }

        [Theory]
        [Trait("Category", "Fast")]
        [InlineData(KafkaSaslMechanism.Plain, SaslMechanism.Plain)]
        [InlineData(KafkaSaslMechanism.ScramSha256, SaslMechanism.ScramSha256)]
        [InlineData(KafkaSaslMechanism.ScramSha512, SaslMechanism.ScramSha512)]
        public void MapSaslMechanism_UsedValues_MapsCorrectly(
            KafkaSaslMechanism input, SaslMechanism expected)
        {
            _logic.MapSaslMechanism(input).Should().Be(expected);
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void MapSaslMechanism_None_FallsBackToPlain()
        {
            // None для SASL-протоколов отсекается валидацией; хелпер консервативен
            _logic.MapSaslMechanism(KafkaSaslMechanism.None).Should().Be(SaslMechanism.Plain);
        }

        [Theory]
        [Trait("Category", "Fast")]
        [InlineData(KafkaAutoOffsetReset.Earliest, AutoOffsetReset.Earliest)]
        [InlineData(KafkaAutoOffsetReset.Latest, AutoOffsetReset.Latest)]
        public void MapAutoOffsetReset_AllValues_MapsCorrectly(
            KafkaAutoOffsetReset input, AutoOffsetReset expected)
        {
            _logic.MapAutoOffsetReset(input).Should().Be(expected);
        }

        #endregion

        // =====================================================================
        // ContentType и описания
        // =====================================================================

        #region ContentType / Describe

        [Fact]
        [Trait("Category", "Fast")]
        public void GetContentType_String_ReturnsTextPlain()
        {
            _logic.GetContentType(KafkaMessageFormat.String)
                .Should().Be("text/plain; charset=utf-8");
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void GetContentType_Json_ReturnsApplicationJson()
        {
            _logic.GetContentType(KafkaMessageFormat.Json)
                .Should().Be("application/json");
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void DescribeMessage_Null_ReturnsNotReceived()
        {
            _logic.DescribeMessage(null).Should().Contain("не получено");
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void DescribeMessage_LongBody_TruncatesTo60Chars()
        {
            var message = new KafkaMessage
            {
                Topic = "t",
                Partition = 0,
                Offset = 5,
                Body = new string('x', 100)
            };

            var description = _logic.DescribeMessage(message);

            description.Should().Contain("topic='t'").And.Contain("offset=5").And.Contain("...");
            description.Should().NotContain(new string('x', 100));
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void DescribeMessage_ShortBody_NoTruncation()
        {
            var message = new KafkaMessage { Topic = "t", Body = "привет" };
            _logic.DescribeMessage(message).Should().Contain("body='привет'");
        }

        #endregion

        // =====================================================================
        // Ожидание сообщения (мок IConsumer — без брокера)
        // =====================================================================

        #region WaitForMessage

        [Fact]
        [Trait("Category", "Fast")]
        public void WaitForMessage_ConsumeReturnsNull_ReturnsNull()
        {
            var consumerMock = new Mock<IConsumer<string, string>>();
            consumerMock
                .Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                .Returns((ConsumeResult<string, string>)null);

            var result = _logic.WaitForMessage(consumerMock.Object, "orders", 1);

            result.Should().BeNull();
            consumerMock.Verify(c => c.Subscribe("orders"), Times.Once);
            consumerMock.Verify(c => c.Unsubscribe(), Times.Once);
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void WaitForMessage_ConsumeReturnsResult_MapsFieldsAndUnsubscribes()
        {
            var consumeResult = new ConsumeResult<string, string>
            {
                Topic = "orders",
                Partition = new Partition(3),
                Offset = new Offset(42),
                Message = new Message<string, string> { Key = "k1", Value = "payload" }
            };
            var consumerMock = new Mock<IConsumer<string, string>>();
            consumerMock
                .Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                .Returns(consumeResult);

            var result = _logic.WaitForMessage(consumerMock.Object, "orders", 5);

            result.Should().NotBeNull();
            result.Topic.Should().Be("orders");
            result.Partition.Should().Be(3);
            result.Offset.Should().Be(42);
            result.Key.Should().Be("k1");
            result.Body.Should().Be("payload");
            result.ReceivedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            consumerMock.Verify(c => c.Unsubscribe(), Times.Once);
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void WaitForMessage_NullConsumer_ThrowsArgumentNullException()
        {
            Action act = () => _logic.WaitForMessage(null, "orders", 1);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void WaitForMessage_EmptyTopic_ThrowsArgumentException()
        {
            var consumerMock = new Mock<IConsumer<string, string>>();
            Action act = () => _logic.WaitForMessage(consumerMock.Object, " ", 1);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void WaitForMessage_NonPositiveTimeout_UsesDefault30()
        {
            // 0 секунд → поднимается до дефолта; проверяем через то, что Consume
            // вызван с таймаутом ≥ 30 с (а не 0)
            TimeSpan captured = default;
            var consumerMock = new Mock<IConsumer<string, string>>();
            consumerMock
                .Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                .Callback<TimeSpan>(t => captured = t)
                .Returns((ConsumeResult<string, string>)null);

            _logic.WaitForMessage(consumerMock.Object, "orders", 0);

            captured.TotalSeconds.Should().BeGreaterOrEqualTo(30);
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void WaitForMessage_UnsubscribeThrows_DoesNotPropagate()
        {
            var consumerMock = new Mock<IConsumer<string, string>>();
            consumerMock
                .Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                .Returns((ConsumeResult<string, string>)null);
            consumerMock
                .Setup(c => c.Unsubscribe())
                .Throws(new InvalidOperationException("уже закрыт"));

            Action act = () => _logic.WaitForMessage(consumerMock.Object, "orders", 1);
            act.Should().NotThrow();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void ToKafkaMessage_Null_ReturnsNull()
        {
            _logic.ToKafkaMessage(null).Should().BeNull();
        }

        #endregion
    }
}
