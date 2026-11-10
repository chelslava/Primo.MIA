// =============================================================================
// KafkaConsumeBatchBack.cs — активность «Kafka: Получить пакет» (issue #33).
//
// Батч-потребление: до MaxMessages или истечения общего таймаута пакета.
// Явный коммит offset'ов — опция CommitAfterBatch (при AutoCommit=False
// пакет фиксируется целиком после успешного выполнения дочерней обработки).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;

namespace Primo.MIA
{
    /// <summary>Активность батч-потребления сообщений Kafka.</summary>
    public class KafkaConsumeBatchBack : KafkaActivityBase<KafkaConsumeBatch>
    {
        public override string GroupName { get => ActivityCategories.MessageQueues; protected set { } }

        // запас +10 с поверх таймаута пакета (как в KafkaConsume)
        protected override int sdkTimeOut { get => 300000; set { } }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propTopic;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaTopic)]
        /// <summary>Топик.</summary>
        public string Prop_Topic
        {
            get => _propTopic;
            set { _propTopic = value; InvokePropertyChanged(this, nameof(Prop_Topic)); }
        }

        private string _propGroupId;
        /// <summary>Consumer Group ID.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaGroupId)]
        public string Prop_GroupId
        {
            get => _propGroupId;
            set { _propGroupId = value; InvokePropertyChanged(this, nameof(Prop_GroupId)); }
        }

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
        /// <summary>Auto Commit: False + CommitAfterBatch — ручное подтверждение пакета.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaAutoCommit)]
        public bool Prop_AutoCommit
        {
            get => _propAutoCommit;
            set { _propAutoCommit = value; InvokePropertyChanged(this, nameof(Prop_AutoCommit)); }
        }

        private string _propMaxMessages;
        /// <summary>Максимум сообщений в пакете.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaMaxMessages)]
        public string Prop_MaxMessages
        {
            get => _propMaxMessages;
            set { _propMaxMessages = value; InvokePropertyChanged(this, nameof(Prop_MaxMessages)); }
        }

        private string _propTimeoutSeconds;
        /// <summary>Общий таймаут ожидания пакета, сек.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaBatchTimeout)]
        public string Prop_TimeoutSeconds
        {
            get => _propTimeoutSeconds;
            set { _propTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_TimeoutSeconds)); }
        }

        private bool _propCommitAfterBatch;
        /// <summary>Коммит offset'ов после получения пакета (только при AutoCommit=False).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaCommitAfterBatch)]
        public bool Prop_CommitAfterBatch
        {
            get => _propCommitAfterBatch;
            set { _propCommitAfterBatch = value; InvokePropertyChanged(this, nameof(Prop_CommitAfterBatch)); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propMessages;
        /// <summary>Тела сообщений (имя выходной переменной).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaBatchMessages)]
        public string Prop_Messages
        {
            get => _propMessages;
            set { _propMessages = value; InvokePropertyChanged(this, nameof(Prop_Messages)); }
        }

        private string _propMessagesInfo;
        /// <summary>Метаданные пакета: topic, partition, offset, key, body (имя выходной переменной).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaBatchInfo)]
        public string Prop_MessagesInfo
        {
            get => _propMessagesInfo;
            set { _propMessagesInfo = value; InvokePropertyChanged(this, nameof(Prop_MessagesInfo)); }
        }

        private string _propCount;
        /// <summary>Число полученных сообщений (имя выходной переменной).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Kafka_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaBatchCount)]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, nameof(Prop_Count)); }
        }

        public KafkaConsumeBatchBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_KafkaConsumeBatch;
            sdkComponentHelp = @"Компонент ""Kafka: Получить пакет""
Батч-потребление: до N сообщений или истечения общего таймаута пакета.

Основные:
Bootstrap-серверы*: [String] host:port через запятую.
Топик*: [String]
Group ID*: [String]
Начальное смещение: [Enum] Latest / Earliest (для новой группы).
Auto Commit: [Boolean] False + Коммит пакета — ручное подтверждение offset'ов.
Максимум сообщений: [Int32] Размер пакета (по умолчанию 10).
Таймаут пакета (сек): [Int32] Общий (по умолчанию 30) — меньше, если сообщений меньше.
Коммит пакета: [Boolean] Commit после получения (при AutoCommit=False).

Выходные данные:
Сообщения: [List<string>] Тела.
Пакет (метаданные): [DataTable] topic, partition, offset, key, body.
Получено: [Int32]";
            sdkComponentIcon = ActivityIcons.MessageQueue;

            sdkProperties = BuildConnectionProperties();
            sdkProperties.AddRange(new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Topic", "Топик"),
                PropertyBuilder.String("Prop_GroupId", "Group ID"),
                PropertyBuilder.Enum<KafkaAutoOffsetReset>("Prop_AutoOffsetReset", "Начальное смещение"),
                PropertyBuilder.BooleanObject("Prop_AutoCommit", "Auto Commit"),
                PropertyBuilder.Int("Prop_MaxMessages", "Максимум сообщений в пакете (по умолчанию 10)"),
                PropertyBuilder.Int("Prop_TimeoutSeconds", "Общий таймаут пакета, сек (по умолчанию 30)"),
                PropertyBuilder.BooleanObject("Prop_CommitAfterBatch", "Коммит offset'ов после пакета"),
                PropertyBuilder.Variable<List<string>>("Prop_Messages", "Тела сообщений"),
                PropertyBuilder.Variable<DataTable>("Prop_MessagesInfo", "Метаданные пакета"),
                PropertyBuilder.Variable<int>("Prop_Count", "Получено сообщений")
            });

            InitClass(container);

            this.Prop_Topic = "\"\"";
            this.Prop_GroupId = "\"\"";
            this.Prop_MaxMessages = "10";
            this.Prop_TimeoutSeconds = "30";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new KafkaLogic();
            return ExecuteKafkaConsumer(sd, consumer =>
            {
                var topic = GetPropertyValue<string>(this.Prop_Topic, nameof(Prop_Topic), sd);
                var maxMessages = StringHelper.TryParseInt(GetPropertyValue<string>(this.Prop_MaxMessages, nameof(Prop_MaxMessages), sd)) ?? 10;
                var timeoutSeconds = StringHelper.TryParseInt(GetPropertyValue<string>(this.Prop_TimeoutSeconds, nameof(Prop_TimeoutSeconds), sd)) ?? 30;

                var messages = logic.WaitForMessageBatch(consumer, topic, maxMessages, timeoutSeconds);

                // Метаданные пакета
                var table = new DataTable("KafkaBatch");
                table.Columns.Add("topic", typeof(string));
                table.Columns.Add("partition", typeof(int));
                table.Columns.Add("offset", typeof(long));
                table.Columns.Add("key", typeof(string));
                table.Columns.Add("body", typeof(string));
                foreach (var m in messages)
                    table.Rows.Add(m.Topic, m.Partition, m.Offset, m.Key, m.Body);

                var bodies = new List<string>(messages.Count);
                foreach (var m in messages)
                    bodies.Add(m.Body);

                SetVariableValue(this.Prop_Messages, bodies, sd);
                SetVariableValue(this.Prop_MessagesInfo, table, sd);
                SetVariableValue(this.Prop_Count, messages.Count, sd);

                // Явный коммит пакета: только при AutoCommit=False и опции
                if (this.Prop_CommitAfterBatch && !this.Prop_AutoCommit && messages.Count > 0)
                {
                    consumer.Commit();
                }

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Kafka: Получить пакет] получено {messages.Count} сообщений ({logic.DescribeMessage(messages.Count > 0 ? messages[0] : null)}...)"
                };
            });
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Topic, ActivityStrings.Field_KafkaTopic, ActivityStrings.Error_KafkaTopicRequired);
            ret.ValidateRequired(this.Prop_GroupId, ActivityStrings.Field_KafkaGroupId, ActivityStrings.Error_KafkaGroupIdRequired);
            return ret;
        }
    }
}
