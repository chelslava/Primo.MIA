// =============================================================================
// RabbitMqLogic.cs — чистая логика RabbitMQ-активностей: настройки подключения,
// валидация, подключение и ожидание сообщения с таймаутом.
// =============================================================================

using Primo.MIA.Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
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

        /// <summary>Заголовки сообщения (issue #49: x-retry-count и пр.).</summary>
        public IDictionary<string, object> Headers { get; set; }

        /// <summary>Счётчик повторов x-retry-count (0 — не задан).</summary>
        public long RetryCount { get; set; }

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
        /// Чистое решение «нужна ли привязка к exchange» — тестируется без
        /// брокера (QueueBind — extension-метод клиента, Moq его не мокает).
        /// </summary>
        public bool ShouldBind(string bindToExchange)
        {
            return !string.IsNullOrWhiteSpace(bindToExchange);
        }

        /// <summary>
        /// Топология: объявляет очередь (durable/exclusive/autoDelete) и,
        /// опционально, привязывает её к exchange по routing key (issue #33).
        /// </summary>
        /// <returns>Число сообщений, уже находящихся в очереди.</returns>
        public uint DeclareQueue(
            IModel model,
            string queue,
            bool durable,
            bool exclusive,
            bool autoDelete,
            string bindToExchange,
            string routingKey)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            Guard.NotNullOrWhiteSpace(queue, nameof(queue));

            var result = model.QueueDeclare(
                queue: queue,
                durable: durable,
                exclusive: exclusive,
                autoDelete: autoDelete,
                arguments: null);

            if (ShouldBind(bindToExchange))
            {
                model.QueueBind(queue, bindToExchange, routingKey ?? string.Empty);
            }

            return result.MessageCount;
        }

        /// <summary>
        /// Очищает очередь: удаляет все сообщения (issue #33).
        /// </summary>
        /// <returns>Число удалённых сообщений.</returns>
        public uint PurgeQueue(IModel model, string queue)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            Guard.NotNullOrWhiteSpace(queue, nameof(queue));

            return model.QueuePurge(queue);
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
                    var headers = args.BasicProperties?.Headers;
                    received.TrySetResult(new RabbitMqMessage
                    {
                        Body = System.Text.Encoding.UTF8.GetString(args.Body.ToArray()),
                        Exchange = args.Exchange,
                        RoutingKey = args.RoutingKey,
                        ContentType = args.BasicProperties?.ContentType,
                        DeliveryTag = args.DeliveryTag,
                        Headers = headers,
                        RetryCount = TryGetRetryCount(headers, out var retryCount) ? retryCount : 0
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

        // ── Ack-паттерн (issue #49) ────────────────────────────────────

        /// <summary>Заголовок счётчика повторов.</summary>
        public const string RetryCountHeaderKey = "x-retry-count";

        /// <summary>
        /// Достаёт счётчик повторов из заголовков. RabbitMQ-клиент декодирует
        /// AMQP-числа в int/long/sbyte/short — принимаем всё числовое и
        /// строковое; byte[] (нестандартные клиенты) — через UTF-8 строку.
        /// </summary>
        public static bool TryGetRetryCount(IDictionary<string, object> headers, out long retryCount)
        {
            retryCount = 0;
            if (headers == null || !headers.TryGetValue(RetryCountHeaderKey, out var raw) || raw == null)
                return false;

            switch (raw)
            {
                case sbyte v: retryCount = v; return true;
                case short v: retryCount = v; return true;
                case int v: retryCount = v; return true;
                case long v: retryCount = v; return true;
                case byte[] bytes:
                    return long.TryParse(System.Text.Encoding.UTF8.GetString(bytes), out retryCount);
                default:
                    return long.TryParse(raw.ToString(), out retryCount);
            }
        }

        /// <summary>Заголовки для requeue/DLQ-копии: инкрементированный счётчик повторов.</summary>
        public static IDictionary<string, object> BuildRetryHeaders(long retryCount)
        {
            return new Dictionary<string, object>
            {
                [RetryCountHeaderKey] = retryCount
            };
        }

        /// <summary>
        /// Свойства для requeue/DLQ-копии: persistent + счётчик повторов.
        /// Прочие заголовки исходного сообщения не сохраняются (ограничение
        /// задокументировано в help активности Ack).
        /// </summary>
        public IBasicProperties BuildRetryProperties(IModel channel, long retryCount)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            var properties = channel.CreateBasicProperties();
            properties.DeliveryMode = 2; // persistent
            properties.ContentType = "text/plain; charset=utf-8";
            properties.Headers = BuildRetryHeaders(retryCount);
            return properties;
        }

        /// <summary>Подтверждает доставку (сообщение удаляется из очереди).</summary>
        public void AckMessage(IModel channel, ulong deliveryTag)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (deliveryTag == 0)
                throw new ArgumentException("Delivery tag должен быть положительным", nameof(deliveryTag));

            channel.BasicAck(deliveryTag, multiple: false);
        }

        /// <summary>
        /// Возвращает сообщение в очередь с инкрементом x-retry-count:
        /// копия публикуется в ту же очередь (persistent), оригинал
        /// подтверждается. Возвращает новый счётчик.
        /// </summary>
        public long RequeueMessage(IModel channel, string queue, ulong deliveryTag, string body, long retryCount)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            Guard.NotNullOrWhiteSpace(queue, nameof(queue));
            if (body == null)
                throw new ArgumentNullException(nameof(body));

            var nextRetryCount = retryCount + 1;
            // интерфейсная перегрузка (не extension): обязательная часть
            // контракта клиента, удобна и для верификации в тестах
            channel.BasicPublish(
                string.Empty,
                queue,
                mandatory: false,
                basicProperties: BuildRetryProperties(channel, nextRetryCount),
                body: System.Text.Encoding.UTF8.GetBytes(body));
            channel.BasicAck(deliveryTag, multiple: false);
            return nextRetryCount;
        }

        /// <summary>
        /// Отправляет сообщение в очередь мёртвых писем и подтверждает
        /// оригинал. DLQ должна существовать (QueueDeclarePassive — fail-closed:
        /// без проверки копия молча потерялась бы через default exchange).
        /// </summary>
        public void SendToDlq(IModel channel, string dlqQueue, ulong deliveryTag, string body, long retryCount)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            Guard.NotNullOrWhiteSpace(dlqQueue, nameof(dlqQueue));
            if (body == null)
                throw new ArgumentNullException(nameof(body));

            channel.QueueDeclarePassive(dlqQueue);
            channel.BasicPublish(
                string.Empty,
                dlqQueue,
                mandatory: false,
                basicProperties: BuildRetryProperties(channel, retryCount),
                body: System.Text.Encoding.UTF8.GetBytes(body));
            channel.BasicAck(deliveryTag, multiple: false);
        }
    }
}
