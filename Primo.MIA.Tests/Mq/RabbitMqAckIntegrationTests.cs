using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using FluentAssertions;
using RabbitMQ.Client;
using Xunit;

namespace Primo.MIA.Tests.Mq
{
    /// <summary>
    /// Интеграционные тесты ack-паттерна RabbitMQ (issue #49) с реальным
    /// брокером: полный цикл publish → consume (manual-ack) → реестр →
    /// Ack / Requeue (счётчик x-retry-count) / DLQ / CloseSession → requeue
    /// при закрытии канала. Запускаются в отдельном integration-воркфлоу;
    /// без доступного брокера на localhost:5672 — самопропуск.
    /// </summary>
    [Trait("Category", "Integration")]
    public class RabbitMqAckIntegrationTests : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _queue;
        private readonly string _dlq;

        public RabbitMqAckIntegrationTests()
        {
            _connection = TryConnect();
            if (_connection == null)
                return; // брокер недоступен — тесты самопропускаются

            _channel = _connection.CreateModel();
            _queue = "mia-ack-tests-" + Guid.NewGuid().ToString("N");
            _dlq = _queue + ".dlq";
            _channel.QueueDeclare(_queue, durable: false, exclusive: false, autoDelete: true);
            _channel.QueueDeclare(_dlq, durable: false, exclusive: false, autoDelete: true);
        }

        public void Dispose()
        {
            RabbitMqSessionRegistry.Clear();
            try
            {
                _channel?.Close();
                _channel?.Dispose();
            }
            catch { /* канал мог быть закрыт сессией */ }
            try
            {
                _connection?.Close();
                _connection?.Dispose();
            }
            catch { /* соединение могло быть закрыто сессией */ }
        }

        private static IConnection TryConnect()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = "localhost",
                    UserName = "guest",
                    Password = "guest",
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(2)
                };
                var connection = factory.CreateConnection();
                return connection.IsOpen ? connection : null;
            }
            catch
            {
                return null; // брокера нет — самопропуск
            }
        }

        private bool BrokerAvailable() => _connection != null;

        private ulong ConsumeFirst(out RabbitMqMessage message)
        {
            var logic = new RabbitMqLogic();
            message = logic.WaitForMessage(_channel, _queue, autoAck: false, timeoutSeconds: 5);
            return message?.DeliveryTag ?? 0;
        }

        private void Publish(string body, long retryCount = 0)
        {
            var logic = new RabbitMqLogic();
            _channel.BasicPublish(
                string.Empty, _queue,
                logic.BuildRetryProperties(_channel, retryCount),
                Encoding.UTF8.GetBytes(body));
            // publication асинхронен — даём брокеру дозаписать
            Thread.Sleep(50);
        }

        [Fact(DisplayName = "Цикл Ack: consume manual-ack → подтверждение → очередь пуста")]
        public void FullCycle_Ack_RemovesMessage()
        {
            if (!BrokerAvailable()) return;

            Publish("заказ-1");
            var message = ConsumeFirst(out var msg);
            message.Should().BeGreaterThan(0UL, "сообщение опубликовано и должно быть доставлено");

            new RabbitMqLogic().AckMessage(_channel, message);

            // после ack следующее ожидание не находит ничего
            var empty = new RabbitMqLogic().WaitForMessage(_channel, _queue, autoAck: true, timeoutSeconds: 2);
            empty.Should().BeNull("подтверждённое сообщение удалено из очереди");
        }

        [Fact(DisplayName = "Цикл Requeue: копия с x-retry-count+1 возвращается в очередь")]
        public void FullCycle_Requeue_IncrementsCounter()
        {
            if (!BrokerAvailable()) return;

            Publish("заказ-2", retryCount: 4);
            var message = ConsumeFirst(out var msg);
            message.Should().BeGreaterThan(0UL);
            msg.RetryCount.Should().Be(4, "счётчик читается из заголовка при доставке");

            var newCount = new RabbitMqLogic().RequeueMessage(_channel, _queue, message, msg.Body, msg.RetryCount);
            newCount.Should().Be(5);

            // повторное получение видит копию с новым счётчиком
            var redelivered = new RabbitMqLogic().WaitForMessage(_channel, _queue, autoAck: true, timeoutSeconds: 5);
            redelivered.Should().NotBeNull("requeue-копия вернулась в очередь");
            redelivered.RetryCount.Should().Be(5, "копия несёт инкрементированный x-retry-count");
            redelivered.Body.Should().Be("заказ-2");
            new RabbitMqLogic().AckMessage(_channel, redelivered.DeliveryTag);
        }

        [Fact(DisplayName = "Цикл DLQ: копия уходит в очередь отказов, оригинал подтверждается")]
        public void FullCycle_Dlq_MovesMessageToDeadLetter()
        {
            if (!BrokerAvailable()) return;

            Publish("битый-заказ", retryCount: 3);
            var message = ConsumeFirst(out var msg);
            message.Should().BeGreaterThan(0UL);

            new RabbitMqLogic().SendToDlq(_channel, _dlq, message, msg.Body, msg.RetryCount);

            var fromDlq = new RabbitMqLogic().WaitForMessage(_channel, _dlq, autoAck: true, timeoutSeconds: 5);
            fromDlq.Should().NotBeNull("копия доставлена в DLQ");
            fromDlq.Body.Should().Be("битый-заказ");
            fromDlq.RetryCount.Should().Be(3, "DLQ-копия сохраняет счётчик");

            // основная очередь пуста — оригинал был подтверждён
            var empty = new RabbitMqLogic().WaitForMessage(_channel, _queue, autoAck: true, timeoutSeconds: 2);
            empty.Should().BeNull();
        }

        [Fact(DisplayName = "Цикл CloseSession: закрытие канала возвращает неподтверждённое сообщение")]
        public void FullCycle_CloseSession_RequeuesUnacked()
        {
            if (!BrokerAvailable()) return;

            Publish("заказ-3");

            // сессия на ВЫДЕЛЕННЫХ ресурсах: Close() закроет именно их,
            // а общий канал останется жив для контрольного чтения
            var sessionConnection = new ConnectionFactory
            {
                HostName = "localhost", UserName = "guest", Password = "guest"
            }.CreateConnection();
            var sessionChannel = sessionConnection.CreateModel();

            var logic = new RabbitMqLogic();
            var message = logic.WaitForMessage(sessionChannel, _queue, autoAck: false, timeoutSeconds: 5);
            message.Should().NotBeNull();

            var handle = RabbitMqSessionRegistry.Register(
                sessionConnection, sessionChannel, _queue, message.DeliveryTag, message.Body, message.RetryCount);

            // сессия держится; сообщение неподтверждено — закрываем без ack
            RabbitMqSessionRegistry.Close(handle.SessionId);

            // сообщение вернулось в очередь (close → requeue) — получаем снова
            var redelivered = logic.WaitForMessage(_channel, _queue, autoAck: true, timeoutSeconds: 5);
            redelivered.Should().NotBeNull("закрытие канала вернуло неподтверждённое сообщение");
            redelivered.Body.Should().Be("заказ-3");
            logic.AckMessage(_channel, redelivered.DeliveryTag);
        }

        [Fact(DisplayName = "Реестр: consume-сессия регистрируется, Get находит, Close удаляет")]
        public void RegistryLifecycle_WithinBrokerSession()
        {
            if (!BrokerAvailable()) return;

            Publish("заказ-4");

            // выделенные ресурсы — Close() не тронет общий канал теста
            var sessionConnection = new ConnectionFactory
            {
                HostName = "localhost", UserName = "guest", Password = "guest"
            }.CreateConnection();
            var sessionChannel = sessionConnection.CreateModel();

            var logic = new RabbitMqLogic();
            var message = logic.WaitForMessage(sessionChannel, _queue, autoAck: false, timeoutSeconds: 5);
            message.Should().NotBeNull();

            var handle = RabbitMqSessionRegistry.Register(
                sessionConnection, sessionChannel, _queue, message.DeliveryTag, message.Body, message.RetryCount);
            RabbitMqSessionRegistry.Get(handle.SessionId).Should().NotBeNull();

            // ack на канале сессии (как делает RabbitMqAckBack)
            logic.AckMessage(handle.Channel, message.DeliveryTag);
            RabbitMqSessionRegistry.Close(handle.SessionId).Should().BeTrue();

            RabbitMqSessionRegistry.Get(handle.SessionId).Should().BeNull();
        }
    }
}
