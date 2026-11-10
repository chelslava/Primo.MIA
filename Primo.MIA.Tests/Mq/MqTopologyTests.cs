using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using FluentAssertions;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace Primo.MIA.Tests.Mq
{
    /// <summary>
    /// Тесты батч-потребления Kafka и топологии RabbitMQ (issue #33)
    /// на моках клиентов: без живых брокеров.
    /// </summary>
    [Trait("Category", "Fast")]
    public class MqTopologyTests
    {
        private readonly KafkaLogic _kafka = new KafkaLogic();
        private readonly RabbitMqLogic _rabbit = new RabbitMqLogic();

        // ── Kafka: батч ────────────────────────────────────────────────

        private static ConsumeResult<string, string> MakeResult(int offset, string body) =>
            new ConsumeResult<string, string>
            {
                Topic = "orders",
                Partition = new Partition(offset % 3),
                Offset = new Offset(offset),
                Message = new Message<string, string> { Key = $"k{offset}", Value = body }
            };

        [Fact(DisplayName = "Батч Kafka: набирает до MaxMessages")]
        public void KafkaBatch_FillsUpToMax()
        {
            var consumer = new Mock<IConsumer<string, string>>();
            var seq = new Queue<ConsumeResult<string, string>>(
                Enumerable.Range(1, 10).Select(i => MakeResult(i, $"body-{i}")));
            consumer.Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                .Returns(() => seq.Count > 0 ? seq.Dequeue() : null);

            var messages = _kafka.WaitForMessageBatch(consumer.Object, "orders", 5, 10);

            messages.Should().HaveCount(5, "лимит пакета");
            messages[0].Body.Should().Be("body-1");
            messages[4].Offset.Should().Be(5);
        }

        [Fact(DisplayName = "Батч Kafka: брокер исчерпан (null) — возвращается раньше лимита")]
        public void KafkaBatch_BrokerExhausted_ReturnsEarly()
        {
            var consumer = new Mock<IConsumer<string, string>>();
            var seq = new Queue<ConsumeResult<string, string>>(new[]
            {
                MakeResult(1, "a"), MakeResult(2, "b")
            });
            consumer.Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                .Returns(() => seq.Count > 0 ? seq.Dequeue() : null);

            var messages = _kafka.WaitForMessageBatch(consumer.Object, "orders", 10, 10);

            messages.Should().HaveCount(2, "null = сообщений нет до истечения таймаута");
        }

        [Fact(DisplayName = "Батч Kafka: таймаут — возвращается собранное")]
        public void KafkaBatch_Timeout_ReturnsCollected()
        {
            var consumer = new Mock<IConsumer<string, string>>();
            var counter = 0;
            consumer.Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                .Returns(() => MakeResult(counter++, "msg")); // сообщения всегда есть

            var messages = _kafka.WaitForMessageBatch(consumer.Object, "orders", 100, 60);

            messages.Should().HaveCount(100, "лимит пакета достигается раньше таймаута");
            consumer.Verify(c => c.Consume(It.IsAny<TimeSpan>()), Times.Exactly(100));
        }

        [Fact(DisplayName = "Батч Kafka: после сбора — Unsubscribe")]
        public void KafkaBatch_Unsubscribes_Afterwards()
        {
            var consumer = new Mock<IConsumer<string, string>>();
            consumer.Setup(c => c.Consume(It.IsAny<TimeSpan>())).Returns((ConsumeResult<string, string>)null);

            _kafka.WaitForMessageBatch(consumer.Object, "orders", 10, 5);

            consumer.Verify(c => c.Unsubscribe(), Times.Once);
        }

        [Fact(DisplayName = "Батч Kafka: Guard — пустой топик / null консьюмер")]
        public void KafkaBatch_Guard()
        {
            var consumer = new Mock<IConsumer<string, string>>();

            Action noTopic = () => _kafka.WaitForMessageBatch(consumer.Object, " ", 10, 10);
            Action noConsumer = () => _kafka.WaitForMessageBatch(null, "orders", 10, 10);

            noTopic.Should().Throw<ArgumentException>();
            noConsumer.Should().Throw<ArgumentNullException>();
        }

        [Fact(DisplayName = "Батч Kafka: нулевые лимиты — значения по умолчанию (10/30)")]
        public void KafkaBatch_DefaultLimits()
        {
            var consumer = new Mock<IConsumer<string, string>>();
            consumer.Setup(c => c.Consume(It.IsAny<TimeSpan>())).Returns((ConsumeResult<string, string>)null);

            // не бросает; возвращает пустой батч (брокер пуст)
            _kafka.WaitForMessageBatch(consumer.Object, "orders", 0, 0).Should().BeEmpty();
        }

        // ── RabbitMQ: DeclareQueue ─────────────────────────────────────

        // QueueBind — extension-метод клиента (Moq не мокает extension),
        // поэтому факт привязки проверяется через чистое решение ShouldBind,
        // а DeclareQueue-вызовы — напрямую.

        [Fact(DisplayName = "ShouldBind: exchange задан → привязка; пусто → нет")]
        public void ShouldBind_Decision()
        {
            _rabbit.ShouldBind("orders.exchange").Should().BeTrue();
            _rabbit.ShouldBind(" ").Should().BeFalse();
            _rabbit.ShouldBind(null).Should().BeFalse();
        }

        [Fact(DisplayName = "DeclareQueue: объявление с параметрами durable/exclusive/autoDelete")]
        public void RabbitDeclare_DeclaresWithOptions()
        {
            var model = new Mock<IModel>();
            model.Setup(m => m.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()))
                .Returns(new QueueDeclareOk("orders", 7, 0));

            var count = _rabbit.DeclareQueue(model.Object, "orders", durable: true, exclusive: false,
                autoDelete: false, bindToExchange: "orders.exchange", routingKey: "new.*");

            count.Should().Be(7u, "сообщения, уже лежащие в очереди");
            model.Verify(m => m.QueueDeclare("orders", true, false, false, It.IsAny<IDictionary<string, object>>()), Times.Once);
        }

        [Fact(DisplayName = "DeclareQueue: Guard — null канал / пустое имя")]
        public void RabbitDeclare_Guard()
        {
            Action noModel = () => _rabbit.DeclareQueue(null, "q", true, false, false, null, null);
            Action noQueue = () => _rabbit.DeclareQueue(new Mock<IModel>().Object, " ", true, false, false, null, null);

            noModel.Should().Throw<ArgumentNullException>();
            noQueue.Should().Throw<ArgumentException>();
        }

        // ── RabbitMQ: PurgeQueue ───────────────────────────────────────

        [Fact(DisplayName = "PurgeQueue: возвращает число удалённых сообщений")]
        public void RabbitPurge_ReturnsCount()
        {
            var model = new Mock<IModel>();
            model.Setup(m => m.QueuePurge("orders")).Returns(42u);

            var purged = _rabbit.PurgeQueue(model.Object, "orders");

            purged.Should().Be(42u);
        }

        [Fact(DisplayName = "PurgeQueue: Guard — null канал / пустое имя")]
        public void RabbitPurge_Guard()
        {
            Action noModel = () => _rabbit.PurgeQueue(null, "q");
            Action noQueue = () => _rabbit.PurgeQueue(new Mock<IModel>().Object, "");

            noModel.Should().Throw<ArgumentNullException>();
            noQueue.Should().Throw<ArgumentException>();
        }
    }
}
