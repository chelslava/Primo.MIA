// =============================================================================
// RabbitMqDeclareQueueBack.cs — активность «RabbitMQ: Объявить очередь»
// (issue #33). Топология: durable/exclusive/autoDelete + опциональная
// привязка к exchange по routing key.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность объявления очереди RabbitMQ.</summary>
    public class RabbitMqDeclareQueueBack : RabbitMqActivityBase<RabbitMqDeclareQueue>
    {
        public override string GroupName { get => ActivityCategories.MessageQueues; protected set { } }

        protected override int sdkTimeOut { get => 60000; set { } }

        private string _propQueue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MqQueue)]
        /// <summary>Имя очереди.</summary>
        public string Prop_Queue
        {
            get => _propQueue;
            set { _propQueue = value; InvokePropertyChanged(this, nameof(Prop_Queue)); }
        }

        private bool _propDurable = true;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MqDurable)]
        /// <summary>Durable: очередь переживёт перезапуск брокера.</summary>
        public bool Prop_Durable
        {
            get => _propDurable;
            set { _propDurable = value; InvokePropertyChanged(this, nameof(Prop_Durable)); }
        }

        private bool _propExclusive;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MqExclusive)]
        /// <summary>Exclusive: только одно подключение, удаляется при разрыве.</summary>
        public bool Prop_Exclusive
        {
            get => _propExclusive;
            set { _propExclusive = value; InvokePropertyChanged(this, nameof(Prop_Exclusive)); }
        }

        private bool _propAutoDelete;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MqAutoDelete)]
        /// <summary>Auto Delete: удалить, когда отключится последний потребитель.</summary>
        public bool Prop_AutoDelete
        {
            get => _propAutoDelete;
            set { _propAutoDelete = value; InvokePropertyChanged(this, nameof(Prop_AutoDelete)); }
        }

        private string _propBindToExchange;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MqBindToExchange)]
        /// <summary>Exchange для привязки (пусто — не привязывать).</summary>
        public string Prop_BindToExchange
        {
            get => _propBindToExchange;
            set { _propBindToExchange = value; InvokePropertyChanged(this, nameof(Prop_BindToExchange)); }
        }

        private string _propRoutingKey;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MqRoutingKey)]
        /// <summary>Routing key привязки.</summary>
        public string Prop_RoutingKey
        {
            get => _propRoutingKey;
            set { _propRoutingKey = value; InvokePropertyChanged(this, nameof(Prop_RoutingKey)); }
        }

        private string _propMessageCount;
        /// <summary>Сообщений уже в очереди (имя выходной переменной).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MqMessageCount)]
        public string Prop_MessageCount
        {
            get => _propMessageCount;
            set { _propMessageCount = value; InvokePropertyChanged(this, nameof(Prop_MessageCount)); }
        }

        public RabbitMqDeclareQueueBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_RabbitMqDeclareQueue;
            sdkComponentHelp = @"Компонент ""RabbitMQ: Объявить очередь""
Объявляет очередь (durable/exclusive/autoDelete) и опционально привязывает её к exchange.

Основные:
Хост*: [String] Порт: [Int32] Пользователь*: [String] Пароль: [String]
Очередь*: [String] Имя очереди.

Настройки:
Durable: [Boolean] Переживает перезапуск брокера (по умолчанию да).
Exclusive: [Boolean] Одно подключение, удаление при разрыве.
Auto Delete: [Boolean] Удаление при отключении последнего потребителя.
Привязать к Exchange: [String] Пусто — не привязывать.
Routing Key: [String] Ключ привязки (пусто = все сообщения exchange).

Выходные данные:
Сообщений в очереди: [Int32]";
            sdkComponentIcon = ActivityIcons.MessageQueue;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Host", "Хост RabbitMQ"),
                PropertyBuilder.Int("Prop_Port", "Порт (5672; TLS — 5671)"),
                PropertyBuilder.String("Prop_UserName", "Имя пользователя"),
                PropertyBuilder.String("Prop_Password", "Пароль"),
                PropertyBuilder.String("Prop_VirtualHost", "VirtualHost (/ по умолчанию)"),
                PropertyBuilder.BooleanObject("Prop_UseTls", "Использовать TLS"),
                PropertyBuilder.String("Prop_Queue", "Имя очереди"),
                PropertyBuilder.BooleanObject("Prop_Durable", "Durable (переживает перезапуск брокера)"),
                PropertyBuilder.BooleanObject("Prop_Exclusive", "Exclusive"),
                PropertyBuilder.BooleanObject("Prop_AutoDelete", "Auto Delete"),
                PropertyBuilder.String("Prop_BindToExchange", "Exchange для привязки (пусто — нет)"),
                PropertyBuilder.String("Prop_RoutingKey", "Routing key привязки"),
                PropertyBuilder.Variable<int>("Prop_MessageCount", "Сообщений уже в очереди")
            };

            InitClass(container);

            this.Prop_Queue = "\"\"";
            this.Prop_BindToExchange = "\"\"";
            this.Prop_RoutingKey = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return ExecuteRabbitMq(sd, channel =>
            {
                var queue = GetPropertyValue<string>(this.Prop_Queue, nameof(Prop_Queue), sd);
                var bindToExchange = GetPropertyValue<string>(this.Prop_BindToExchange, nameof(Prop_BindToExchange), sd);
                var routingKey = GetPropertyValue<string>(this.Prop_RoutingKey, nameof(Prop_RoutingKey), sd);

                var logic = new RabbitMqLogic();
                var messageCount = logic.DeclareQueue(
                    channel, queue, this.Prop_Durable, this.Prop_Exclusive, this.Prop_AutoDelete,
                    bindToExchange, routingKey);

                SetVariableValue(this.Prop_MessageCount, (int)messageCount, sd);

                var bindInfo = string.IsNullOrWhiteSpace(bindToExchange)
                    ? string.Empty
                    : $", привязана к {bindToExchange}";
                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[RabbitMQ] Очередь {queue} объявлена{bindInfo} ({messageCount} сообщений)"
                };
            });
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Host, ActivityStrings.Field_MqHost, ActivityStrings.Error_MqHostRequired);
            ret.ValidateRequired(this.Prop_UserName, ActivityStrings.Field_MqUserName, ActivityStrings.Error_MqUserNameRequired);
            ret.ValidateRequired(this.Prop_Queue, ActivityStrings.Field_MqQueue, ActivityStrings.Error_MqQueueRequired);
            return ret;
        }
    }
}
