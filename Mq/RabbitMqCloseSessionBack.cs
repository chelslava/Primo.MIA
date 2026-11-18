// =============================================================================
// RabbitMqCloseSessionBack.cs — активность «RabbitMQ: Закрыть сессию»
// (issue #49).
//
// Явное закрытие сессии manual-ack без подтверждения: при закрытии канала
// брокер возвращает неподтверждённое сообщение в очередь. Полезно, когда
// сценарий решил не обрабатывать сообщение (например, не тот формат).
// Идемпотентно: несуществующая сессия — успех (сценарные пути очистки).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;

namespace Primo.MIA
{
    /// <summary>Активность закрытия сессии manual-ack без подтверждения доставки.</summary>
    public class RabbitMqCloseSessionBack : RabbitMqActivityBase<RabbitMqCloseSession>
    {
        public override string GroupName { get => ActivityCategories.MessageQueues; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

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

        private string _propClosed;
        /// <summary>True, если сессия была открыта и закрыта; False — не найдена (уже закрыта).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MqClosed)]
        public string Prop_Closed
        {
            get => _propClosed;
            set { _propClosed = value; InvokePropertyChanged(this, nameof(Prop_Closed)); }
        }

        public RabbitMqCloseSessionBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_RabbitMqCloseSession;
            sdkComponentHelp = @"Компонент ""RabbitMQ: Закрыть сессию""
Закрывает сессию, открытую «RabbitMQ: Получить сообщение» в режиме «Держать сессию», БЕЗ подтверждения доставки: при закрытии канала брокер сам вернёт неподтверждённое сообщение в очередь. Используйте, когда сценарий решил не обрабатывать сообщение (не тот формат, служебное сообщение и т.п.).

Основные:
ID сессии*: [String] Выход активности «Получить сообщение».

Выходные данные:
Сессия закрыта: [Boolean] True — сессия была открыта и закрыта; False — не найдена (уже закрыта, истекла по TTL 24 ч или Studio перезапущен). Оба исхода — успех: активность идемпотентна и безопасна в путях очистки сценария.";
            sdkComponentIcon = ActivityIcons.MessageQueue;
            sdkProperties = new System.Collections.Generic.List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_SessionId", "ID сессии (выход «Получить сообщение»)"),
                PropertyBuilder.Variable<bool>("Prop_Closed", "Сессия закрыта")
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

                var closed = RabbitMqSessionRegistry.Close(sessionId);
                SetVariableValue(this.Prop_Closed, closed, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = closed
                        ? $"Сессия '{sessionId}' закрыта; неподтверждённое сообщение вернётся в очередь"
                        : $"Сессия '{sessionId}' не найдена (уже закрыта или истекла по TTL) — идемпотентный успех"
                };
            }
            catch (Exception ex)
            {
                Common.ActivityFileLogger.Error(sdkComponentName, ex.GetType().Name + ": " + ex.Message + Environment.NewLine + ex.StackTrace);
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка закрытия сессии: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_SessionId, ActivityStrings.Field_MqSessionId, ActivityStrings.Error_MqSessionRequired);
            return ret;
        }
    }
}
