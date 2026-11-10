// =============================================================================
// KafkaLogic.cs — чистая логика Kafka-активностей (issue #19): валидация
// настроек, построение конфигураций Producer/Consumer, ожидание одного
// сообщения. Чистые функции (валидация, конфиги, маппинги) покрыты тестами
// без живого брокера.
// =============================================================================

using Primo.MIA.Common;
using Confluent.Kafka;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Настройки подключения к Kafka-кластеру.
    /// </summary>
    public class KafkaConnectionSettings
    {
        /// <summary>Адреса bootstrap-серверов, через запятую: "host1:9092,host2:9092".</summary>
        public string BootstrapServers { get; set; }

        /// <summary>Протокол безопасности.</summary>
        public KafkaSecurityProtocol SecurityProtocol { get; set; } = KafkaSecurityProtocol.Plaintext;

        /// <summary>SASL-механизм (используется при SASL-протоколах).</summary>
        public KafkaSaslMechanism SaslMechanism { get; set; } = KafkaSaslMechanism.None;

        /// <summary>SASL-имя пользователя (при SASL-протоколах).</summary>
        public string SaslUsername { get; set; }

        /// <summary>SASL-пароль (при SASL-протоколах).</summary>
        public string SaslPassword { get; set; }

        /// <summary>Таймаут подключения/операций в секундах.</summary>
        public int ConnectionTimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Настройки консьюмера поверх подключения.
    /// </summary>
    public class KafkaConsumerOptions
    {
        /// <summary>Consumer Group ID (обязателен; Kafka распределяет партиции по группам).</summary>
        public string GroupId { get; set; }

        /// <summary>Начальное смещение для новой группы.</summary>
        public KafkaAutoOffsetReset AutoOffsetReset { get; set; } = KafkaAutoOffsetReset.Latest;

        /// <summary>
        /// Auto Commit: True — offset коммитится автоматически (сообщение считается
        /// обработанным); False — коммита нет: при следующем запуске группы сообщение
        /// будет прочитано повторно (режим «прочитать без удаления»).
        /// </summary>
        public bool AutoCommit { get; set; } = true;
    }

    /// <summary>
    /// Полученное сообщение Kafka.
    /// </summary>
    public class KafkaMessage
    {
        public string Topic { get; set; }
        public int Partition { get; set; }
        public long Offset { get; set; }
        public string Key { get; set; }
        public string Body { get; set; }
        public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Логика Kafka-модуля: валидация, построение конфигураций, маппинги enum'ов.
    /// Создание клиентов — в <see cref="KafkaActivityBase{T}"/> (исключения
    /// конфигурации маппятся там же).
    /// </summary>
    public class KafkaLogic
    {
        /// <summary>
        /// Валидирует настройки подключения: bootstrap-серверы обязательны;
        /// для SASL-протоколов обязателен механизм и имя пользователя.
        /// </summary>
        public void ValidateSettings(KafkaConnectionSettings settings)
        {
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNullOrWhiteSpace(settings.BootstrapServers, nameof(settings.BootstrapServers));

            var useSasl = settings.SecurityProtocol == KafkaSecurityProtocol.SaslPlaintext
                          || settings.SecurityProtocol == KafkaSecurityProtocol.SaslSsl;
            if (useSasl)
            {
                Guard.That(
                    settings.SaslMechanism != KafkaSaslMechanism.None,
                    "Для SASL-протоколов необходимо выбрать механизм аутентификации (PLAIN/SCRAM)");
                Guard.NotNullOrWhiteSpace(settings.SaslUsername, nameof(settings.SaslUsername));
            }
        }

        /// <summary>
        /// Валидирует опции консьюмера: Group ID обязателен.
        /// </summary>
        public void ValidateConsumerOptions(KafkaConsumerOptions options)
        {
            Guard.NotNull(options, nameof(options));
            Guard.NotNullOrWhiteSpace(options.GroupId, nameof(options.GroupId));
        }

        /// <summary>
        /// Строит ProducerConfig (без подключения к брокеру) — чистая функция.
        /// </summary>
        public ProducerConfig BuildProducerConfig(KafkaConnectionSettings settings)
        {
            ValidateSettings(settings);
            var config = new ProducerConfig
            {
                BootstrapServers = settings.BootstrapServers
            };
            ApplySecurity(config, settings);
            return config;
        }

        /// <summary>
        /// Строит ConsumerConfig (без подключения к брокеру) — чистая функция.
        /// </summary>
        public ConsumerConfig BuildConsumerConfig(KafkaConnectionSettings settings, KafkaConsumerOptions options)
        {
            ValidateSettings(settings);
            ValidateConsumerOptions(options);

            var config = new ConsumerConfig
            {
                BootstrapServers = settings.BootstrapServers,
                GroupId = options.GroupId,
                AutoOffsetReset = MapAutoOffsetReset(options.AutoOffsetReset),
                EnableAutoCommit = options.AutoCommit
            };
            ApplySecurity(config, settings);
            return config;
        }

        /// <summary>
        /// Применяет настройки безопасности (TLS/SASL) к конфигурации.
        /// </summary>
        private void ApplySecurity(ClientConfig config, KafkaConnectionSettings settings)
        {
            config.SecurityProtocol = MapSecurityProtocol(settings.SecurityProtocol);

            var useSasl = settings.SecurityProtocol == KafkaSecurityProtocol.SaslPlaintext
                          || settings.SecurityProtocol == KafkaSecurityProtocol.SaslSsl;
            if (!useSasl)
                return;

            config.SaslMechanism = MapSaslMechanism(settings.SaslMechanism);
            config.SaslUsername = settings.SaslUsername ?? string.Empty;
            config.SaslPassword = settings.SaslPassword ?? string.Empty;
        }

        /// <summary>
        /// Ожидает одно сообщение из топика с таймаутом. При таймауте возвращает
        /// null (активность выставит HasMessage = false) — удобно для опроса в цикле.
        /// </summary>
        public KafkaMessage WaitForMessage(IConsumer<string, string> consumer, string topic, int timeoutSeconds)
        {
            if (consumer == null)
                throw new ArgumentNullException(nameof(consumer));
            Guard.NotNullOrWhiteSpace(topic, nameof(topic));
            if (timeoutSeconds <= 0)
                timeoutSeconds = 30;

            consumer.Subscribe(topic);
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(timeoutSeconds));
                return result == null
                    ? null
                    : ToKafkaMessage(result);
            }
            finally
            {
                SafeDispose.Quiet(() => consumer.Unsubscribe(), "Kafka: unsubscribe");
            }
        }

        /// <summary>
        /// Ждёт пакет сообщений из топика: до maxMessages или истечения
        /// общего таймаута (issue #33). Таймаут — НА ВЕСЬ пакет, не на одно
        /// сообщение: продовые потребители вычитывают доступные сообщения
        /// и не ждут полного размера.
        /// </summary>
        /// <param name="consumer">Консьюмер (конфигурация задаёт AutoCommit).</param>
        /// <param name="topic">Топик.</param>
        /// <param name="maxMessages">Максимум сообщений в пакете.</param>
        /// <param name="timeoutSeconds">Общий таймаут ожидания пакета, сек.</param>
        public List<KafkaMessage> WaitForMessageBatch(
            IConsumer<string, string> consumer,
            string topic,
            int maxMessages,
            int timeoutSeconds)
        {
            if (consumer == null)
                throw new ArgumentNullException(nameof(consumer));
            Guard.NotNullOrWhiteSpace(topic, nameof(topic));
            if (maxMessages <= 0)
                maxMessages = 10;
            if (timeoutSeconds <= 0)
                timeoutSeconds = 30;

            consumer.Subscribe(topic);
            try
            {
                var messages = new List<KafkaMessage>();
                var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

                while (messages.Count < maxMessages)
                {
                    var remaining = deadline - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                        break;

                    var result = consumer.Consume(remaining);
                    if (result == null)
                        break; // больше сообщений нет до истечения бюджета

                    messages.Add(ToKafkaMessage(result));
                }

                return messages;
            }
            finally
            {
                SafeDispose.Quiet(() => consumer.Unsubscribe(), "Kafka: unsubscribe");
            }
        }

        /// <summary>
        /// Преобразует результат потребления в DTO.
        /// </summary>
        public KafkaMessage ToKafkaMessage(ConsumeResult<string, string> result)
        {
            if (result == null)
                return null;

            return new KafkaMessage
            {
                Topic = result.Topic,
                Partition = result.Partition.Value,
                Offset = result.Offset.Value,
                Key = result.Message?.Key,
                Body = result.Message?.Value
            };
        }

        /// <summary>
        /// Краткое описание сообщения для лога выполнения.
        /// </summary>
        public string DescribeMessage(KafkaMessage message)
        {
            if (message == null)
                return "сообщение не получено";

            var preview = message.Body ?? string.Empty;
            if (preview.Length > 60)
                preview = preview.Substring(0, 60) + "...";
            return $"topic='{message.Topic}' partition={message.Partition} offset={message.Offset} body='{preview}'";
        }

        /// <summary>
        /// ContentType по выбранному формату сообщения.
        /// </summary>
        public string GetContentType(KafkaMessageFormat format)
        {
            return format == KafkaMessageFormat.Json
                ? "application/json"
                : "text/plain; charset=utf-8";
        }

        /// <summary>Маппинг протокола безопасности в enum Confluent.Kafka.</summary>
        public SecurityProtocol MapSecurityProtocol(KafkaSecurityProtocol protocol)
        {
            switch (protocol)
            {
                case KafkaSecurityProtocol.Ssl:
                    return SecurityProtocol.Ssl;
                case KafkaSecurityProtocol.SaslPlaintext:
                    return SecurityProtocol.SaslPlaintext;
                case KafkaSecurityProtocol.SaslSsl:
                    return SecurityProtocol.SaslSsl;
                default:
                    return SecurityProtocol.Plaintext;
            }
        }

        /// <summary>Маппинг SASL-механизма в enum Confluent.Kafka.</summary>
        public SaslMechanism MapSaslMechanism(KafkaSaslMechanism mechanism)
        {
            switch (mechanism)
            {
                case KafkaSaslMechanism.Plain:
                    return SaslMechanism.Plain;
                case KafkaSaslMechanism.ScramSha256:
                    return SaslMechanism.ScramSha256;
                case KafkaSaslMechanism.ScramSha512:
                    return SaslMechanism.ScramSha512;
                default:
                    // None не используется напрямую; для SASL-протоколов валидация
                    // отсекает None до этого места
                    return SaslMechanism.Plain;
            }
        }

        /// <summary>Маппинг начального смещения в enum Confluent.Kafka.</summary>
        public AutoOffsetReset MapAutoOffsetReset(KafkaAutoOffsetReset reset)
        {
            return reset == KafkaAutoOffsetReset.Earliest
                ? AutoOffsetReset.Earliest
                : AutoOffsetReset.Latest;
        }
    }
}
