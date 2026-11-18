using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace Primo.MIA.Tests.Mq
{
    /// <summary>
    /// Тесты ack-паттерна RabbitMQ (issue #49): разбор заголовка
    /// x-retry-count, сборка заголовков requeue-копии, thin-методы
    /// Ack/Requeue/DLQ над IModel (Moq — верификация публикаций и ack).
    /// </summary>
    [Trait("Category", "Fast")]
    public class RabbitMqAckLogicTests
    {
        private readonly RabbitMqLogic _logic = new RabbitMqLogic();

        // ── TryGetRetryCount: типы AMQP field table ────────────────────

        [Fact(DisplayName = "TryGetRetryCount: null/пустые заголовки → false")]
        public void TryGetRetryCount_NullOrMissing_False()
        {
            RabbitMqLogic.TryGetRetryCount(null, out _).Should().BeFalse();
            RabbitMqLogic.TryGetRetryCount(new Dictionary<string, object>(), out _).Should().BeFalse();
            RabbitMqLogic.TryGetRetryCount(
                new Dictionary<string, object> { ["другой"] = 1 }, out _).Should().BeFalse();
            RabbitMqLogic.TryGetRetryCount(
                new Dictionary<string, object> { [RabbitMqLogic.RetryCountHeaderKey] = null }, out _).Should().BeFalse();
        }

        [Theory(DisplayName = "TryGetRetryCount: числовые типы AMQP декодируются")]
        [InlineData(0L)]
        [InlineData(3)]
        [InlineData(7L)]
        public void TryGetRetryCount_NumericTypes(object raw)
        {
            var headers = new Dictionary<string, object> { [RabbitMqLogic.RetryCountHeaderKey] = raw };

            var ok = RabbitMqLogic.TryGetRetryCount(headers, out var retryCount);

            ok.Should().BeTrue();
            retryCount.Should().Be(Convert.ToInt64(raw));
        }

        [Fact(DisplayName = "TryGetRetryCount: sbyte/short принимаются")]
        public void TryGetRetryCount_SmallIntegers()
        {
            RabbitMqLogic.TryGetRetryCount(
                new Dictionary<string, object> { [RabbitMqLogic.RetryCountHeaderKey] = (sbyte)2 }, out var a);
            a.Should().Be(2);

            RabbitMqLogic.TryGetRetryCount(
                new Dictionary<string, object> { [RabbitMqLogic.RetryCountHeaderKey] = (short)4 }, out var b);
            b.Should().Be(4);
        }

        [Fact(DisplayName = "TryGetRetryCount: строка и byte[] парсятся")]
        public void TryGetRetryCount_StringAndBytes()
        {
            RabbitMqLogic.TryGetRetryCount(
                new Dictionary<string, object> { [RabbitMqLogic.RetryCountHeaderKey] = "5" }, out var fromString);
            fromString.Should().Be(5);

            RabbitMqLogic.TryGetRetryCount(
                new Dictionary<string, object>
                {
                    [RabbitMqLogic.RetryCountHeaderKey] = System.Text.Encoding.UTF8.GetBytes("9")
                }, out var fromBytes);
            fromBytes.Should().Be(9);
        }

        [Fact(DisplayName = "TryGetRetryCount: мусор → false, счётчик 0")]
        public void TryGetRetryCount_Garbage_False()
        {
            var ok = RabbitMqLogic.TryGetRetryCount(
                new Dictionary<string, object> { [RabbitMqLogic.RetryCountHeaderKey] = "abc" }, out var retryCount);

            ok.Should().BeFalse();
            retryCount.Should().Be(0);
        }

        [Fact(DisplayName = "BuildRetryHeaders: заголовок x-retry-count с инкрементом")]
        public void BuildRetryHeaders_CreatesCounterHeader()
        {
            var headers = RabbitMqLogic.BuildRetryHeaders(4);

            headers.Should().ContainKey("x-retry-count");
            ((long)headers["x-retry-count"]).Should().Be(4);
        }

        // ── AckMessage ─────────────────────────────────────────────────

        [Fact(DisplayName = "AckMessage: BasicAck с multiple=false")]
        public void AckMessage_CallsBasicAck()
        {
            var channel = new Mock<IModel>();

            _logic.AckMessage(channel.Object, 42);

            channel.Verify(c => c.BasicAck(42, false), Times.Once);
        }

        [Fact(DisplayName = "AckMessage: tag 0 и null-канал → исключение")]
        public void AckMessage_InvalidArguments_Throws()
        {
            Action actZeroTag = () => _logic.AckMessage(new Mock<IModel>().Object, 0);
            Action actNullChannel = () => _logic.AckMessage(null, 1);
            actZeroTag.Should().Throw<ArgumentException>();
            actNullChannel.Should().Throw<ArgumentNullException>();
        }

        // ── RequeueMessage: копия в очередь + ack оригинала ────────────

        [Fact(DisplayName = "RequeueMessage: publish persistent-копии с x-retry-count+1, затем ack")]
        public void RequeueMessage_PublishesAndAcks()
        {
            var channel = new Mock<IModel>();
            var props = new Mock<IBasicProperties>();
            channel.Setup(c => c.CreateBasicProperties()).Returns(props.Object);

            var newCount = _logic.RequeueMessage(channel.Object, "orders", 7, "тело", 2);

            newCount.Should().Be(3, "счётчик инкрементируется");
            channel.Verify(c => c.BasicPublish(
                string.Empty, "orders", false, props.Object,
                It.Is<ReadOnlyMemory<byte>>(b => System.Text.Encoding.UTF8.GetString(b.ToArray()) == "тело")), Times.Once);
            channel.Verify(c => c.BasicAck(7, false), Times.Once);
            // заголовки копии — инкрементированный счётчик
            props.VerifySet(p => p.Headers = It.Is<Dictionary<string, object>>(
                h => (long)h[RabbitMqLogic.RetryCountHeaderKey] == 3), "копия несёт x-retry-count = старый + 1");
            props.VerifySet(p => p.DeliveryMode = 2, "requeue-копия persistent");
        }

        [Fact(DisplayName = "RequeueMessage: пустая очередь → исключение, ack не выполняется")]
        public void RequeueMessage_EmptyQueue_Throws()
        {
            var channel = new Mock<IModel>();
            channel.Setup(c => c.CreateBasicProperties()).Returns(new Mock<IBasicProperties>().Object);

            Action act = () => _logic.RequeueMessage(channel.Object, "  ", 1, "тело", 0);

            act.Should().Throw<ArgumentException>();
            channel.Verify(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Never,
                "при ошибке валидации подтверждение не делается");
        }

        // ── SendToDlq: passive-проверка DLQ + копия + ack ──────────────

        [Fact(DisplayName = "SendToDlq: QueueDeclarePassive, publish в DLQ, ack оригинала")]
        public void SendToDlq_VerifiesQueuePublishesAndAcks()
        {
            var channel = new Mock<IModel>();
            var props = new Mock<IBasicProperties>();
            channel.Setup(c => c.CreateBasicProperties()).Returns(props.Object);

            _logic.SendToDlq(channel.Object, "orders.dlq", 11, "плохое сообщение", 5);

            channel.Verify(c => c.QueueDeclarePassive("orders.dlq"), Times.Once,
                "DLQ проверяется до публикации — иначе копия молча потерялась бы");
            channel.Verify(c => c.BasicPublish(
                string.Empty, "orders.dlq", false, props.Object,
                It.Is<ReadOnlyMemory<byte>>(b => System.Text.Encoding.UTF8.GetString(b.ToArray()) == "плохое сообщение")), Times.Once);
            channel.Verify(c => c.BasicAck(11, false), Times.Once);
            props.VerifySet(p => p.Headers = It.Is<Dictionary<string, object>>(
                h => (long)h[RabbitMqLogic.RetryCountHeaderKey] == 5), "DLQ-копия сохраняет текущий счётчик");
        }

        [Fact(DisplayName = "SendToDlq: очередь не существует → исключение ДО публикации и ack")]
        public void SendToDlq_MissingQueue_FailsClosed()
        {
            var channel = new Mock<IModel>();
            channel.Setup(c => c.QueueDeclarePassive("no-such.dlq"))
                .Throws(new RabbitMQ.Client.Exceptions.OperationInterruptedException(new ShutdownEventArgs(
                    ShutdownInitiator.Peer, 404, "NOT_FOUND - no queue 'no-such.dlq' in vhost '/'")));
            channel.Setup(c => c.CreateBasicProperties()).Returns(new Mock<IBasicProperties>().Object);

            Action act = () => _logic.SendToDlq(channel.Object, "no-such.dlq", 1, "тело", 0);

            act.Should().Throw<RabbitMQ.Client.Exceptions.OperationInterruptedException>();
            channel.Verify(c => c.BasicPublish(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()),
                Times.Never, "fail-closed: без DLQ копия не публикуется");
            channel.Verify(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Never,
                "оригинал остаётся неподтверждённым — вернётся в очередь");
        }

        [Fact(DisplayName = "BuildRetryProperties: persistent + ContentType + заголовки")]
        public void BuildRetryProperties_SetsPersistentAndHeaders()
        {
            var channel = new Mock<IModel>();
            var props = new Mock<IBasicProperties>();

            channel.Setup(c => c.CreateBasicProperties()).Returns(props.Object);

            var result = _logic.BuildRetryProperties(channel.Object, 6);

            result.Should().BeSameAs(props.Object);
            props.VerifySet(p => p.DeliveryMode = 2);
            props.VerifySet(p => p.ContentType = "text/plain; charset=utf-8");
            props.VerifySet(p => p.Headers = It.Is<Dictionary<string, object>>(
                h => (long)h[RabbitMqLogic.RetryCountHeaderKey] == 6));
        }
    }
}
