// =============================================================================
// KafkaConsumeBack.cs — активность «Kafka: Получить сообщение» (issue #19).
// Ожидает одно сообщение из топика с таймаутом.
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность ожидания и получения одного сообщения из Kafka-топика.</summary>
    public class KafkaConsumeBack : KafkaActivityBase<KafkaConsume>
    {
        public override string GroupName { get => ActivityCategories.MessageQueues; protected set { } }

        protected override int sdkTimeOut
        {
            // запас +10 с поверх таймаута ожидания сообщения (как в RabbitMqConsume)
            get { int s; return int.TryParse(Prop_TimeoutSeconds, out s) && s > 0 ? (s + 10) * 1000 : 40000; }
            set { }
        }

        private string _propTopic;
        /// <summary>Имя топика.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaTopic)]
        public string Prop_Topic { get => _propTopic; set { _propTopic = value; InvokePropertyChanged(this, nameof(Prop_Topic)); } }

        private string _propGroupId;
        /// <summary>Consumer Group ID.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaGroupId)]
        public string Prop_GroupId { get => _propGroupId; set { _propGroupId = value; InvokePropertyChanged(this, nameof(Prop_GroupId)); } }

        private KafkaAutoOffsetReset _propAutoOffsetReset = KafkaAutoOffsetReset.Latest;
        /// <summary>Начальное смещение для новой группы.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaAutoOffsetReset)]
        public KafkaAutoOffsetReset Prop_AutoOffsetReset
        {
            get => _propAutoOffsetReset;
            set { _propAutoOffsetReset = value; InvokePropertyChanged(this, nameof(Prop_AutoOffsetReset)); }
        }

        private bool _propAutoCommit = true;
        /// <summary>Auto Commit: False — «прочитать без удаления».</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaAutoCommit)]
        public bool Prop_AutoCommit { get => _propAutoCommit; set { _propAutoCommit = value; InvokePropertyChanged(this, nameof(Prop_AutoCommit)); } }

        private string _propTimeoutSeconds = "30";
        /// <summary>Таймаут ожидания сообщения в секундах.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_MqTimeoutSeconds)]
        public string Prop_TimeoutSeconds { get => _propTimeoutSeconds; set { _propTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_TimeoutSeconds)); } }

        private string _propMessageBody;
        /// <summary>Тело полученного сообщения (UTF-8).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaMessageBody)]
        public string Prop_MessageBody { get => _propMessageBody; set { _propMessageBody = value; InvokePropertyChanged(this, nameof(Prop_MessageBody)); } }

        private string _propMessageKey;
        /// <summary>Ключ полученного сообщения.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaMessageKey)]
        public string Prop_MessageKey { get => _propMessageKey; set { _propMessageKey = value; InvokePropertyChanged(this, nameof(Prop_MessageKey)); } }

        private string _propMessageOffset;
        /// <summary>Offset полученного сообщения (topic-partition#offset).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaMessageOffset)]
        public string Prop_MessageOffset { get => _propMessageOffset; set { _propMessageOffset = value; InvokePropertyChanged(this, nameof(Prop_MessageOffset)); } }

        private string _propHasMessage;
        /// <summary>True, если сообщение получено до истечения таймаута.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MqHasMessage)]
        public string Prop_HasMessage { get => _propHasMessage; set { _propHasMessage = value; InvokePropertyChanged(this, nameof(Prop_HasMessage)); } }

        public KafkaConsumeBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_KafkaConsume;
            sdkComponentHelp = @"Компонент ""Kafka: Получить сообщение""
Ожидает одно сообщение из Kafka-топика. При истечении таймаута завершается успешно с HasMessage = False (без исключения) — удобно для опроса топика в цикле.

Основные:
Bootstrap-серверы*: [String] Адреса кластера через запятую: ""host1:9092,host2:9092"".
Топик*: [String] Имя топика.
Group ID*: [String] Consumer-группа. Сообщения распределяются по группам; одна группа читает топик совместно.
Начальное смещение: [Enum] С начала (Earliest) / Только новые (Latest) — для группы БЕЗ закоммиченного offset.
Auto Commit: [Boolean] True — offset коммитится автоматически (по умолчанию). False — коммита нет: при следующем запуске группы сообщение будет прочитано ПОВТОРНО (режим «прочитать без удаления»).
Таймаут ожидания: [Int32] Секунды; по истечении HasMessage = False.

Подключение (см. панель свойств):
Протокол безопасности: Plaintext / TLS / SASL (без TLS) / SASL + TLS.
SASL: механизм (PLAIN / SCRAM-SHA-256 / SCRAM-SHA-512), имя пользователя, пароль.

Выходные данные:
Тело сообщения: [String] Значение сообщения.
Ключ: [String] Ключ сообщения (если задавался).
Offset: [String] Позиция сообщения в формате «топик#партиция@offset».
Сообщение получено: [Boolean] True — сообщение доставлено до таймаута.";
            sdkComponentIcon = ActivityIcons.MessageQueue;
            sdkProperties = BuildConnectionProperties();
            sdkProperties.AddRange(new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Topic", "Топик"),
                PropertyBuilder.String("Prop_GroupId", "Group ID"),
                PropertyBuilder.Enum<KafkaAutoOffsetReset>("Prop_AutoOffsetReset", "Начальное смещение"),
                PropertyBuilder.BooleanObject("Prop_AutoCommit", "Auto Commit"),
                PropertyBuilder.Int("Prop_TimeoutSeconds", "Таймаут ожидания в секундах"),
                PropertyBuilder.Variable<string>("Prop_MessageBody", "Тело полученного сообщения"),
                PropertyBuilder.Variable<string>("Prop_MessageKey", "Ключ сообщения"),
                PropertyBuilder.Variable<string>("Prop_MessageOffset", "Offset (топик#партиция@offset)"),
                PropertyBuilder.Variable<bool>("Prop_HasMessage", "Сообщение получено")
            });
            InitClass(container);
        }

        /// <summary>Опции консьюмера из свойств активности.</summary>
        protected override KafkaConsumerOptions ResolveConsumerOptions()
        {
            return new KafkaConsumerOptions
            {
                GroupId = Prop_GroupId,
                AutoOffsetReset = Prop_AutoOffsetReset,
                AutoCommit = Prop_AutoCommit
            };
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var topic = GetPropertyValue<string>(this.Prop_Topic, nameof(Prop_Topic), sd);
                if (string.IsNullOrWhiteSpace(topic))
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = ActivityStrings.Error_KafkaTopicRequired };

                if (string.IsNullOrWhiteSpace(this.Prop_GroupId))
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = ActivityStrings.Error_KafkaGroupIdRequired };

                int timeout; int.TryParse(GetPropertyValue<string>(this.Prop_TimeoutSeconds, nameof(Prop_TimeoutSeconds), sd), out timeout);
                if (timeout <= 0) timeout = 30;

                var logic = new KafkaLogic();
                return ExecuteKafkaConsumer(sd, consumer =>
                {
                    var message = logic.WaitForMessage(consumer, topic, timeout);

                    if (message == null)
                    {
                        SetVariableValue(this.Prop_HasMessage, false, sd);
                        return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Топик '{topic}' пуст (таймаут {timeout} с)" };
                    }

                    SetVariableValue(this.Prop_MessageBody, message.Body, sd);
                    SetVariableValue(this.Prop_MessageKey, message.Key, sd);
                    SetVariableValue(this.Prop_MessageOffset, $"{message.Topic}#{message.Partition}@{message.Offset}", sd);
                    SetVariableValue(this.Prop_HasMessage, true, sd);

                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = $"Сообщение получено: {logic.DescribeMessage(message)}"
                    };
                });
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка получения: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_BootstrapServers, ActivityStrings.Field_KafkaBootstrapServers, ActivityStrings.Error_KafkaBootstrapRequired);
            ret.ValidateRequired(this.Prop_Topic, ActivityStrings.Field_KafkaTopic, ActivityStrings.Error_KafkaTopicRequired);
            ret.ValidateRequired(this.Prop_GroupId, ActivityStrings.Field_KafkaGroupId, ActivityStrings.Error_KafkaGroupIdRequired);
            return ret;
        }
    }
}
