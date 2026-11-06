// =============================================================================
// RabbitMqConsumeBack.cs — активность «RabbitMQ: Получить сообщение».
// Ожидает одно сообщение из очереди с настраиваемым таймаутом.
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность ожидания и получения одного сообщения из очереди RabbitMQ.</summary>
    public class RabbitMqConsumeBack : RabbitMqActivityBase<RabbitMqConsume>
    {
        public override string GroupName { get => ActivityCategories.MessageQueues; protected set { } }

        protected override int sdkTimeOut
        {
            get { int s; return int.TryParse(Prop_TimeoutSeconds, out s) && s > 0 ? (s + 10) * 1000 : 40000; }
            set { }
        }

        private string _propQueue;
        /// <summary>Имя очереди.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_MqQueue)]
        public string Prop_Queue { get => _propQueue; set { _propQueue = value; InvokePropertyChanged(this, nameof(Prop_Queue)); } }

        private bool _propAutoAck;
        /// <summary>Auto Ack (ack отправляется сразу при доставке).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_MqAutoAck)]
        public bool Prop_AutoAck { get => _propAutoAck; set { _propAutoAck = value; InvokePropertyChanged(this, nameof(Prop_AutoAck)); } }

        private string _propTimeoutSeconds = "30";
        /// <summary>Таймаут ожидания сообщения в секундах.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Consume), System.ComponentModel.DisplayName(ActivityStrings.Field_MqTimeoutSeconds)]
        public string Prop_TimeoutSeconds { get => _propTimeoutSeconds; set { _propTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_TimeoutSeconds)); } }

        private string _propMessageBody;
        /// <summary>Тело полученного сообщения (UTF-8).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MqMessageBody)]
        public string Prop_MessageBody { get => _propMessageBody; set { _propMessageBody = value; InvokePropertyChanged(this, nameof(Prop_MessageBody)); } }

        private string _propMessageRoutingKey;
        /// <summary>Routing key полученного сообщения.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MqMessageRoutingKey)]
        public string Prop_MessageRoutingKey { get => _propMessageRoutingKey; set { _propMessageRoutingKey = value; InvokePropertyChanged(this, nameof(Prop_MessageRoutingKey)); } }

        private string _propDeliveryTag;
        /// <summary>Delivery tag (для ручного ack/nack).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(ulong))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MqDeliveryTag)]
        public string Prop_DeliveryTag { get => _propDeliveryTag; set { _propDeliveryTag = value; InvokePropertyChanged(this, nameof(Prop_DeliveryTag)); } }

        private string _propHasMessage;
        /// <summary>True, если сообщение получено до истечения таймаута.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MqHasMessage)]
        public string Prop_HasMessage { get => _propHasMessage; set { _propHasMessage = value; InvokePropertyChanged(this, nameof(Prop_HasMessage)); } }

        public RabbitMqConsumeBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_RabbitMqConsume;
            sdkComponentHelp = @"Компонент ""RabbitMQ: Получить сообщение""
Ожидает одно сообщение из очереди RabbitMQ. При истечении таймаута завершается успешно с HasMessage = False (без исключения) — удобно для опроса очереди в цикле.

Основные:
Хост*: [String] Адрес брокера RabbitMQ.
Пользователь*: [String] Имя пользователя.
Пароль: [String] Пароль.
Очередь*: [String] Имя очереди.

Получение:
Auto Ack: [Boolean] True — сообщение подтверждается и УДАЛЯЕТСЯ из очереди сразу при доставке (по умолчанию False). False — подтверждения нет: после завершения активности сообщение ВОЗВРАЩАЕТСЯ В ОЧЕРЕДЬ (режим «прочитать без удаления»; Delivery Tag в этом режиме не используется).
Таймаут ожидания: [Int32] Секунды; по истечении HasMessage = False.

Выходные данные:
Тело сообщения: [String] Текст сообщения.
Routing Key: [String] Routing key сообщения.
Delivery Tag: [UInt64] Тег доставки (при Auto Ack = True).
Сообщение получено: [Boolean] True — сообщение доставлено до таймаута.";
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
                PropertyBuilder.BooleanObject("Prop_AutoAck", "Auto Ack"),
                PropertyBuilder.Int("Prop_TimeoutSeconds", "Таймаут ожидания в секундах"),
                PropertyBuilder.Variable<string>("Prop_MessageBody", "Тело полученного сообщения"),
                PropertyBuilder.Variable<string>("Prop_MessageRoutingKey", "Routing Key сообщения"),
                PropertyBuilder.Variable<ulong>("Prop_DeliveryTag", "Delivery Tag"),
                PropertyBuilder.Variable<bool>("Prop_HasMessage", "Сообщение получено")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var queue = GetPropertyValue<string>(this.Prop_Queue, nameof(Prop_Queue), sd);
                if (string.IsNullOrWhiteSpace(queue))
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = ActivityStrings.Error_MqQueueRequired };

                int timeout; int.TryParse(GetPropertyValue<string>(this.Prop_TimeoutSeconds, nameof(Prop_TimeoutSeconds), sd), out timeout);
                if (timeout <= 0) timeout = 30;

                var logic = new RabbitMqLogic();
                return ExecuteRabbitMq(sd, channel =>
                {
                    var message = logic.WaitForMessage(channel, queue, this.Prop_AutoAck, timeout);

                    if (message == null)
                    {
                        SetVariableValue(this.Prop_HasMessage, false, sd);
                        return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Очередь '{queue}' пуста (таймаут {timeout} с)" };
                    }

                    SetVariableValue(this.Prop_MessageBody, message.Body, sd);
                    SetVariableValue(this.Prop_MessageRoutingKey, message.RoutingKey, sd);
                    SetVariableValue(this.Prop_DeliveryTag, message.DeliveryTag, sd);
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
            ret.ValidateRequired(this.Prop_Host, ActivityStrings.Field_MqHost, ActivityStrings.Error_MqHostRequired);
            ret.ValidateRequired(this.Prop_Queue, ActivityStrings.Field_MqQueue, ActivityStrings.Error_MqQueueRequired);
            return ret;
        }
    }
}
