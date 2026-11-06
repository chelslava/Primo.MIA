// =============================================================================
// KafkaPublishBack.cs — активность «Kafka: Отправить сообщение» (issue #19).
// Синхронная публикация с ожиданием ack брокера.
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность публикации сообщения в Kafka-топик.</summary>
    public class KafkaPublishBack : KafkaActivityBase<KafkaPublish>
    {
        public override string GroupName { get => ActivityCategories.MessageQueues; protected set { } }

        protected override int sdkTimeOut
        {
            get { return ActivityTimeoutParser.ParseSecondsToMs(Prop_ConnectionTimeoutSeconds, 30000); }
            set { }
        }

        private string _propTopic;
        /// <summary>Имя топика.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Publish), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaTopic)]
        public string Prop_Topic { get => _propTopic; set { _propTopic = value; InvokePropertyChanged(this, nameof(Prop_Topic)); } }

        private string _propKey;
        /// <summary>Ключ сообщения (определяет партицию; пусто — round-robin).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Publish), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaKey)]
        public string Prop_Key { get => _propKey; set { _propKey = value; InvokePropertyChanged(this, nameof(Prop_Key)); } }

        private string _propMessage;
        /// <summary>Значение сообщения (UTF-8; JSON — как текст).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Publish), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaMessage)]
        public string Prop_Message { get => _propMessage; set { _propMessage = value; InvokePropertyChanged(this, nameof(Prop_Message)); } }

        private KafkaMessageFormat _propMessageFormat = KafkaMessageFormat.String;
        /// <summary>Формат сообщения (ContentType метаданные).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Publish), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaMessageFormat)]
        public KafkaMessageFormat Prop_MessageFormat
        {
            get => _propMessageFormat;
            set { _propMessageFormat = value; InvokePropertyChanged(this, nameof(Prop_MessageFormat)); }
        }

        private string _propOutputPartition;
        /// <summary>Переменная: номер партиции, куда записано сообщение.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaOutputPartition)]
        public string Prop_OutputPartition { get => _propOutputPartition; set { _propOutputPartition = value; InvokePropertyChanged(this, nameof(Prop_OutputPartition)); } }

        private string _propOutputOffset;
        /// <summary>Переменная: offset сообщения в партиции.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(long))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaOutputOffset)]
        public string Prop_OutputOffset { get => _propOutputOffset; set { _propOutputOffset = value; InvokePropertyChanged(this, nameof(Prop_OutputOffset)); } }

        public KafkaPublishBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_KafkaPublish;
            sdkComponentHelp = @"Компонент ""Kafka: Отправить сообщение""
Публикует сообщение в Kafka-топик (синхронно, с ожиданием подтверждения брокера).

Основные:
Bootstrap-серверы*: [String] Адреса кластера через запятую: ""host1:9092,host2:9092"".
Топик*: [String] Имя топика.
Ключ: [String] Ключ сообщения — определяет партицию (одинаковый ключ → одна партиция); пусто — round-robin.
Сообщение*: [String] Значение сообщения (UTF-8; JSON передаётся как текст).
Формат: [Enum] Строка / JSON — влияет на ContentType метаданные.

Подключение (см. панель свойств):
Протокол безопасности: Plaintext / TLS / SASL (без TLS) / SASL + TLS.
SASL: механизм (PLAIN / SCRAM-SHA-256 / SCRAM-SHA-512), имя пользователя, пароль.
Таймаут подключения: [Int32] Секунды, по умолчанию 30.

Выходные данные:
Партиция: [Int32] Партиция, в которую записано сообщение.
Offset: [Int64] Offset сообщения.";
            sdkComponentIcon = ActivityIcons.MessageQueue;
            sdkProperties = BuildConnectionProperties();
            sdkProperties.AddRange(new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Topic", "Топик"),
                PropertyBuilder.String("Prop_Key", "Ключ сообщения"),
                PropertyBuilder.String("Prop_Message", "Сообщение (значение)"),
                PropertyBuilder.Enum<KafkaMessageFormat>("Prop_MessageFormat", "Формат сообщения"),
                PropertyBuilder.Variable<int>("Prop_OutputPartition", "Партиция"),
                PropertyBuilder.Variable<long>("Prop_OutputOffset", "Offset")
            });
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var topic = GetPropertyValue<string>(this.Prop_Topic, nameof(Prop_Topic), sd);
                if (string.IsNullOrWhiteSpace(topic))
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = ActivityStrings.Error_KafkaTopicRequired };

                var key = GetPropertyValue<string>(this.Prop_Key, nameof(Prop_Key), sd) ?? string.Empty;
                var message = GetPropertyValue<string>(this.Prop_Message, nameof(Prop_Message), sd);
                if (string.IsNullOrWhiteSpace(message))
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = ActivityStrings.Error_KafkaMessageRequired };

                var logic = new KafkaLogic();
                return ExecuteKafkaProducer(sd, producer =>
                {
                    // ProduceAsync + ожидание = синхронная доставка с ack брокера
                    // (возвращает DeliveryResult с партицией/offset) — детерминированное
                    // поведение для RPA-сценария
                    var result = producer.ProduceAsync(topic, new Confluent.Kafka.Message<string, string>
                    {
                        Key = key,
                        Value = message,
                        Headers = BuildHeaders(logic)
                    }).ConfigureAwait(false).GetAwaiter().GetResult();

                    SetVariableValue(this.Prop_OutputPartition, result.Partition.Value, sd);
                    SetVariableValue(this.Prop_OutputOffset, result.Offset.Value, sd);

                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = $"Сообщение опубликовано: topic='{result.Topic}', partition={result.Partition.Value}, offset={result.Offset.Value}"
                    };
                });
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка публикации: {ex.Message}" };
            }
        }

        /// <summary>Заголовок ContentType по выбранному формату.</summary>
        private Confluent.Kafka.Headers BuildHeaders(KafkaLogic logic)
        {
            var headers = new Confluent.Kafka.Headers();
            headers.Add("content-type", System.Text.Encoding.UTF8.GetBytes(logic.GetContentType(this.Prop_MessageFormat)));
            return headers;
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_BootstrapServers, ActivityStrings.Field_KafkaBootstrapServers, ActivityStrings.Error_KafkaBootstrapRequired);
            ret.ValidateRequired(this.Prop_Topic, ActivityStrings.Field_KafkaTopic, ActivityStrings.Error_KafkaTopicRequired);
            return ret;
        }
    }
}
