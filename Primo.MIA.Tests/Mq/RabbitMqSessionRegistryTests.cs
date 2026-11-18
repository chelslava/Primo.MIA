using System;
using FluentAssertions;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace Primo.MIA.Tests.Mq
{
    /// <summary>
    /// Тесты реестра RabbitMQ-сессий manual-ack (issue #49): регистрация,
    /// поиск, закрытие, TTL-эвикция брошенных сессий (по образцу
    /// DatabaseTransactionManager). Соединения и каналы — Moq-фейки.
    /// Реестр статический — каждый тест стартует с полной очисткой.
    /// </summary>
    [Trait("Category", "Fast")]
    public class RabbitMqSessionRegistryTests : IDisposable
    {
        private static readonly DateTime T0 = new DateTime(2026, 11, 18, 10, 0, 0, DateTimeKind.Utc);

        public RabbitMqSessionRegistryTests()
        {
            RabbitMqSessionRegistry.Clear();
        }

        public void Dispose()
        {
            RabbitMqSessionRegistry.Clear();
        }

        [Fact(DisplayName = "Register: сессия создаётся с GUID-идентификатором и доступна по Get")]
        public void Register_ReturnsHandle_FindableByGet()
        {
            var handle = RabbitMqSessionRegistry.Register(
                new Mock<IConnection>().Object, new Mock<IModel>().Object,
                "orders", 5, "тело", 2, nowUtc: T0);

            handle.SessionId.Should().NotBeNullOrEmpty();
            handle.Queue.Should().Be("orders");
            handle.LastDeliveryTag.Should().Be(5UL);
            handle.LastBody.Should().Be("тело");
            handle.LastRetryCount.Should().Be(2);
            handle.StartedAtUtc.Should().Be(T0);

            var found = RabbitMqSessionRegistry.Get(handle.SessionId);
            found.Should().BeSameAs(handle);
            RabbitMqSessionRegistry.Count.Should().Be(1);
        }

        [Fact(DisplayName = "Get: неизвестный/пустой id → null")]
        public void Get_UnknownOrNull_ReturnsNull()
        {
            RabbitMqSessionRegistry.Get("no-such-session").Should().BeNull();
            RabbitMqSessionRegistry.Get(null).Should().BeNull();
            RabbitMqSessionRegistry.Get("").Should().BeNull();
            RabbitMqSessionRegistry.Get("   ").Should().BeNull();
        }

        [Fact(DisplayName = "Close: сессия удаляется, канал и соединение освобождаются")]
        public void Close_RemovesAndDisposes()
        {
            var connection = new Mock<IConnection>();
            var channel = new Mock<IModel>();
            var handle = RabbitMqSessionRegistry.Register(connection.Object, channel.Object, "q", 1, "b", 0, nowUtc: T0);

            var closed = RabbitMqSessionRegistry.Close(handle.SessionId);

            closed.Should().BeTrue();
            RabbitMqSessionRegistry.Get(handle.SessionId).Should().BeNull();
            RabbitMqSessionRegistry.Count.Should().Be(0);
            channel.Verify(c => c.Close(), Times.Once);
            channel.Verify(c => c.Dispose(), Times.Once);
            connection.Verify(c => c.Dispose(), Times.Once);
        }

        [Fact(DisplayName = "Close: неизвестная сессия → false (идемпотентность)")]
        public void Close_Unknown_ReturnsFalse()
        {
            RabbitMqSessionRegistry.Close("no-such").Should().BeFalse();
            RabbitMqSessionRegistry.Close(null).Should().BeFalse();
        }

        [Fact(DisplayName = "TTL: сессия старше TTL эвицируется при следующей регистрации")]
        public void Register_EvictsExpiredSessions()
        {
            var staleChannel = new Mock<IModel>();
            var stale = RabbitMqSessionRegistry.Register(
                new Mock<IConnection>().Object, staleChannel.Object, "q", 1, "b", 0,
                nowUtc: T0, ttl: TimeSpan.FromHours(24));

            // регистрация через 25 часов: старая сессия выкидывается
            var fresh = RabbitMqSessionRegistry.Register(
                new Mock<IConnection>().Object, new Mock<IModel>().Object, "q", 2, "b", 0,
                nowUtc: T0.AddHours(25), ttl: TimeSpan.FromHours(24));

            RabbitMqSessionRegistry.Get(stale.SessionId).Should().BeNull("сессия старше TTL эвицируется");
            RabbitMqSessionRegistry.Get(fresh.SessionId).Should().NotBeNull();
            RabbitMqSessionRegistry.Count.Should().Be(1);
            staleChannel.Verify(c => c.Dispose(), Times.Once, "ресурсы брошенной сессии освобождаются");
        }

        [Fact(DisplayName = "TTL: сессия моложе TTL переживает регистрацию других")]
        public void Register_KeepsYoungSessions()
        {
            var keeper = RabbitMqSessionRegistry.Register(
                new Mock<IConnection>().Object, new Mock<IModel>().Object, "q", 1, "b", 0,
                nowUtc: T0, ttl: TimeSpan.FromHours(24));

            RabbitMqSessionRegistry.Register(
                new Mock<IConnection>().Object, new Mock<IModel>().Object, "q", 2, "b", 0,
                nowUtc: T0.AddHours(23), ttl: TimeSpan.FromHours(24));

            RabbitMqSessionRegistry.Get(keeper.SessionId).Should().NotBeNull();
            RabbitMqSessionRegistry.Count.Should().Be(2);
        }

        [Fact(DisplayName = "Register: null-соединение или канал → ArgumentNullException")]
        public void Register_NullArguments_Throws()
        {
            Action actNullConnection = () => RabbitMqSessionRegistry.Register(null, new Mock<IModel>().Object, "q", 1, "b", 0);
            Action actNullChannel = () => RabbitMqSessionRegistry.Register(new Mock<IConnection>().Object, null, "q", 1, "b", 0);
            actNullConnection.Should().Throw<ArgumentNullException>();
            actNullChannel.Should().Throw<ArgumentNullException>();
        }

        [Fact(DisplayName = "DefaultTtl = 24 часа (как у транзакций БД)")]
        public void DefaultTtl_Is24Hours()
        {
            RabbitMqSessionRegistry.DefaultTtl.Should().Be(TimeSpan.FromHours(24));
        }
    }
}
