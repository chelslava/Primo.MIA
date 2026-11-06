// =============================================================================
// RabbitMqLogic.cs — чистая логика RabbitMQ-активностей: настройки подключения,
// валидация, подключение и ожидание сообщения с таймаутом.
// =============================================================================

using Primo.MIA.Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Primo.MIA
{
    /// <summary>
    /// Настройки подключения к RabbitMQ.
    /// </summary>
    public class RabbitMqConnectionSettings
    {
        public string Host { get; set; }
        public int Port { get; set; } = 5672;

        /// <summary>0 — дефолтный порт клиента (5672, TLS 5671).</summary>
        public string UserName { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; } = "/";

        /// <summary>Использовать TLS (амqp+TLS, порт по умолчанию 5671).</summary>
        public bool UseTls { get; set; }

        /// <summary>Таймаут подключения в секундах.</summary>
        public int ConnectionTimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Результат ожидания сообщения из очереди.
    /// </summary>
    public class RabbitMqMessage
    {
        public string Body { get; set; }
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
        public string ContentType { get; set; }
        public ulong DeliveryTag { get; set; }
        public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Логика RabbitMQ-модуля. Чистые функции (валидация, фабрика) покрыты тестами.
    /// </summary>
    public class RabbitMqLogic
    {
        /// <summary>
        /// Валидирует настройки подключения.
        /// </summary>
        public void ValidateSettings(RabbitMqConnectionSettings settings)
        {
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNullOrWhiteSpace(settings.Host, nameof(settings.Host));
            Guard.NotNullOrWhiteSpace(settings.UserName, nameof(settings.UserName));
            Guard.Positive(settings.Port, nameof(settings.Port));
        }

        /// <summary>
        /// Создаёт ConnectionFactory по настройкам (без подключения).
        /// </summary>
        public ConnectionFactory CreateFactory(RabbitMqConnectionSettings settings)
        {
            ValidateSettings(settings);

            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password ?? string.Empty,
                VirtualHost = string.IsNullOrEmpty(settings.VirtualHost) ? "/" : settings.VirtualHost,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(settings.ConnectionTimeoutSeconds),
                AutomaticRecoveryEnabled = false
            };

            if (settings.UseTls)
            {
                factory.Ssl = new SslOption
                {
                    Enabled = true,
                    ServerName = settings.Host
                };
            }

            return factory;
        }

        /// <summary>
        /// Ждёт одно сообщение из очереди с таймаутом. При таймауте возвращает
        /// null (активность выставит HasMessage = false).
        /// </summary>
        public RabbitMqMessage WaitForMessage(
            IModel model,
            string queue,
            bool autoAck,
            int timeoutSeconds)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            Guard.NotNullOrWhiteSpace(queue, nameof(queue));
            if (timeoutSeconds <= 0)
                timeoutSeconds = 30;

            var received = new TaskCompletionSource<RabbitMqMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            var consumer = new EventingBasicConsumer(model);

            consumer.Received += (sender, args) =>
            {
                try
                {
                    received.TrySetResult(new RabbitMqMessage
                    {
                        Body = System.Text.Encoding.UTF8.GetString(args.Body.ToArray()),
                        Exchange = args.Exchange,
                        RoutingKey = args.RoutingKey,
                        ContentType = args.BasicProperties?.ContentType,
                        DeliveryTag = args.DeliveryTag
                    });
                }
                catch
                {
                    received.TrySetResult(null);
                }
            };

            var consumerTag = model.BasicConsume(queue, autoAck, consumer);

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                using (cts.Token.Register(() => received.TrySetCanceled()))
                {
                    try
                    {
                        return received.Task.GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException)
                    {
                        return null; // таймаут ожидания
                    }
                }
            }
            finally
            {
                try
                {
                    model.BasicCancel(consumerTag);
                }
                catch
                {
                    // канал мог быть закрыт — не критично
                }
            }
        }

        /// <summary>
        /// Краткое описание сообщения для лога выполнения.
        /// </summary>
        public string DescribeMessage(RabbitMqMessage message)
        {
            if (message == null)
                return "сообщение не получено";

            var preview = message.Body ?? string.Empty;
            if (preview.Length > 60)
                preview = preview.Substring(0, 60) + "...";
            return $"routingKey='{message.RoutingKey}' body='{preview}'";
        }
    }
}
