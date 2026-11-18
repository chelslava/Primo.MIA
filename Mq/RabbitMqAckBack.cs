// =============================================================================
// RabbitMqAckBack.cs — активность «RabbitMQ: Подтверждение сообщения»
// (issue #49).
//
// Ручное подтверждение доставки на канале сессии, открытой RabbitMqConsume
// (режим «Держать сессию»): delivery tag действителен только на канале-
// получателе, поэтому подтверждение обязано идти через реестр сессий.
// Действия: Ack — удалить из очереди; Requeue — копия в ту же очередь с
// инкрементом x-retry-count; Nack → DLQ — копия в очередь отказов + ack
// оригинала (DLQ проверяется QueueDeclarePassive — fail-closed).
// После любого действия сессия закрывается (одно сообщение на сессию).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;

namespace Primo.MIA
{
    /// <summary>Активность подтверждения/возврата/DLQ для сообщения из сессии manual-ack.</summary>
    public class RabbitMqAckBack : RabbitMqActivityBase<RabbitMqAck>
    {
        public override string GroupName { get => ActivityCategories.MessageQueues; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        // ── Основные ─────────────────────────────────────────────────────

        private string _propSessionId;
        /// <summary>ID сессии (выход RabbitMQ: Получить сообщение).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MqSessionId)]
        public string Prop_SessionId
        {
            get => _propSessionId;
            set { _propSessionId = value; InvokePropertyChanged(this, nameof(Prop_SessionId)); }
        }

        private string _propDeliveryTag;
        /// <summary>Delivery Tag (выход RabbitMQ: Получить сообщение).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(ulong))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MqDeliveryTag)]
        public string Prop_DeliveryTag
        {
            get => _propDeliveryTag;
            set { _propDeliveryTag = value; InvokePropertyChanged(this, nameof(Prop_DeliveryTag)); }
        }

        private RabbitMqAckAction _propAction = RabbitMqAckAction.Ack;
        /// <summary>Действие: Ack / Requeue (счётчик +1) / Nack → DLQ.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MqAckAction)]
        public RabbitMqAckAction Prop_Action
        {
            get => _propAction;
            set { _propAction = value; InvokePropertyChanged(this, nameof(Prop_Action)); }
        }

        private string _propDlqQueue;
        /// <summary>Очередь мёртвых писем (для действия Nack → DLQ).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MqDlqQueue)]
        public string Prop_DlqQueue
        {
            get => _propDlqQueue;
            set { _propDlqQueue = value; InvokePropertyChanged(this, nameof(Prop_DlqQueue)); }
        }

        // ── Выходные ─────────────────────────────────────────────────────

        private string _propNewRetryCount;
        /// <summary>Счётчик повторов requeue-копии (x-retry-count).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(long))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MqNewRetryCount)]
        public string Prop_NewRetryCount
        {
            get => _propNewRetryCount;
            set { _propNewRetryCount = value; InvokePropertyChanged(this, nameof(Prop_NewRetryCount)); }
        }

        public RabbitMqAckBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_RabbitMqAck;
            sdkComponentHelp = @"Компонент ""RabbitMQ: Подтверждение сообщения""
Подтверждает (или возвращает в очередь) сообщение, полученное активностью «RabbitMQ: Получить сообщение» в режиме «Держать сессию». Работает через ID сессии: delivery tag действителен только на канале-получателе, поэтому подтверждение обязано идти через ту же сессию.

Основные:
ID сессии*: [String] Выход активности «Получить сообщение».
Delivery Tag*: [UInt64] Выход активности «Получить сообщение».
Действие: [Enum] Ack — сообщение удалено из очереди (успешная обработка); Requeue — копия вернётся в ту же очередь с x-retry-count +1 (оригинал подтверждается); Nack → DLQ — копия уйдёт в очередь отказов (оригинал подтверждается).

Настройки:
Очередь DLQ: [String] Для действия «Nack → DLQ». Должна существовать (проверяется перед отправкой — иначе сообщение НЕ теряется).

Выходные данные:
Новый счётчик повторов: [Int64] Значение x-retry-count requeue-копии (для действия Requeue).

Примечания:
- После любого действия сессия закрывается (одно сообщение на сессию).
- Requeue/DLQ публикуют тело сообщения как UTF-8 с persistent-доставкой; прочие заголовки исходного сообщения не сохраняются, кроме x-retry-count.
- Закрыть сессию без решения об обработке — активностью «RabbitMQ: Закрыть сессию»: неподтверждённое сообщение вернётся в очередь.";
            sdkComponentIcon = ActivityIcons.MessageQueue;
            sdkProperties = new System.Collections.Generic.List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_SessionId", "ID сессии (выход «Получить сообщение»)"),
                PropertyBuilder.Variable<ulong>("Prop_DeliveryTag", "Delivery Tag (выход «Получить сообщение»)"),
                PropertyBuilder.Enum<RabbitMqAckAction>("Prop_Action", "Действие: Ack / Requeue / Nack → DLQ"),
                PropertyBuilder.String("Prop_DlqQueue", "Очередь DLQ (для действия «Nack → DLQ»)"),
                PropertyBuilder.Variable<long>("Prop_NewRetryCount", "Новый счётчик повторов (для Requeue)")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var sessionId = GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd);
                if (string.IsNullOrWhiteSpace(sessionId))
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = ActivityStrings.Error_MqSessionRequired };

                var deliveryTagRaw = GetPropertyValue<string>(this.Prop_DeliveryTag, nameof(Prop_DeliveryTag), sd);
                if (!ulong.TryParse(deliveryTagRaw, out var deliveryTag) || deliveryTag == 0)
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = ActivityStrings.Error_MqDeliveryTagRequired };

                var dlqQueue = GetPropertyValue<string>(this.Prop_DlqQueue, nameof(Prop_DlqQueue), sd);
                if (this.Prop_Action == RabbitMqAckAction.NackDlq && string.IsNullOrWhiteSpace(dlqQueue))
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = ActivityStrings.Error_MqDlqQueueRequired };

                var handle = RabbitMqSessionRegistry.Get(sessionId);
                if (handle == null)
                {
                    return new ExecutionResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Сессия '{sessionId}' не найдена: истекла (TTL 24 ч), уже закрыта или Studio был перезапущен. Неподтверждённое сообщение вернулось в очередь — получите его заново."
                    };
                }

                if (!handle.Channel.IsOpen)
                {
                    RabbitMqSessionRegistry.Close(sessionId);
                    return new ExecutionResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Канал сессии '{sessionId}' закрыт (разрыв соединения с брокером). Сессия удалена; неподтверждённое сообщение вернулось в очередь — получите его заново."
                    };
                }

                if (deliveryTag > handle.LastDeliveryTag)
                {
                    return new ExecutionResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Delivery Tag {deliveryTag} вне диапазона сессии '{sessionId}' (последняя доставка: {handle.LastDeliveryTag})."
                    };
                }

                var logic = new RabbitMqLogic();
                switch (this.Prop_Action)
                {
                    case RabbitMqAckAction.Ack:
                        logic.AckMessage(handle.Channel, deliveryTag);
                        break;

                    case RabbitMqAckAction.Requeue:
                        // тело и счётчик хранятся в сессии со времени получения —
                        // дополнительные входы не нужны
                        var newRetryCount = logic.RequeueMessage(
                            handle.Channel, handle.Queue, deliveryTag, handle.LastBody, handle.LastRetryCount);
                        SetVariableValue(this.Prop_NewRetryCount, newRetryCount, sd);
                        break;

                    case RabbitMqAckAction.NackDlq:
                        logic.SendToDlq(handle.Channel, dlqQueue, deliveryTag, handle.LastBody, handle.LastRetryCount);
                        break;
                }

                RabbitMqSessionRegistry.Close(sessionId);

                var actionName = this.Prop_Action == RabbitMqAckAction.Ack
                    ? "подтверждено"
                    : this.Prop_Action == RabbitMqAckAction.Requeue
                        ? "возвращено в очередь (x-retry-count +1)"
                        : $"отправлено в DLQ '{dlqQueue}'";
                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Сообщение {actionName}; сессия '{sessionId}' закрыта"
                };
            }
            catch (Exception ex)
            {
                // сессию сознательно НЕ закрываем: сбой публикации/ack может быть
                // транзиентным, а закрытие канала вернуло бы сообщение в очередь —
                // решение за сценарием (повторить Ack или Закрыть сессию)
                Common.ActivityFileLogger.Error(sdkComponentName, ex.GetType().Name + ": " + ex.Message + Environment.NewLine + ex.StackTrace);
                return new ExecutionResult { IsSuccess = false, ErrorMessage = MapRabbitMqException(ex) };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_SessionId, ActivityStrings.Field_MqSessionId, ActivityStrings.Error_MqSessionRequired);
            ret.ValidateRequired(this.Prop_DeliveryTag, ActivityStrings.Field_MqDeliveryTag, ActivityStrings.Error_MqDeliveryTagRequired);
            if (this.Prop_Action == RabbitMqAckAction.NackDlq)
                ret.ValidateRequired(this.Prop_DlqQueue, ActivityStrings.Field_MqDlqQueue, ActivityStrings.Error_MqDlqQueueRequired);
            return ret;
        }
    }
}
