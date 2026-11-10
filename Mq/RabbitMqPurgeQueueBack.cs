// =============================================================================
// RabbitMqPurgeQueueBack.cs — активность «RabbitMQ: Очистить очередь»
// (issue #33). Удаляет все сообщения из очереди (деструктивная операция).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность очистки очереди RabbitMQ.</summary>
    public class RabbitMqPurgeQueueBack : RabbitMqActivityBase<RabbitMqPurgeQueue>
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

        private string _propPurgedCount;
        /// <summary>Удалено сообщений (имя выходной переменной).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MqPurgedCount)]
        public string Prop_PurgedCount
        {
            get => _propPurgedCount;
            set { _propPurgedCount = value; InvokePropertyChanged(this, nameof(Prop_PurgedCount)); }
        }

        public RabbitMqPurgeQueueBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_RabbitMqPurgeQueue;
            sdkComponentHelp = @"Компонент ""RabbitMQ: Очистить очередь""
Удаляет ВСЕ сообщения из очереди. Операция необратима — проверьте имя очереди.

Основные:
Хост*: [String] Порт: [Int32] Пользователь*: [String] Пароль: [String]
Очередь*: [String] Имя очереди для очистки.

Выходные данные:
Удалено сообщений: [Int32]";
            sdkComponentIcon = ActivityIcons.MessageQueue;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Host", "Хост RabbitMQ"),
                PropertyBuilder.Int("Prop_Port", "Порт (5672; TLS — 5671)"),
                PropertyBuilder.String("Prop_UserName", "Имя пользователя"),
                PropertyBuilder.String("Prop_Password", "Пароль"),
                PropertyBuilder.String("Prop_VirtualHost", "VirtualHost (/ по умолчанию)"),
                PropertyBuilder.BooleanObject("Prop_UseTls", "Использовать TLS"),
                PropertyBuilder.String("Prop_Queue", "Имя очереди для очистки"),
                PropertyBuilder.Variable<int>("Prop_PurgedCount", "Удалено сообщений")
            };

            InitClass(container);

            this.Prop_Queue = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return ExecuteRabbitMq(sd, channel =>
            {
                var queue = GetPropertyValue<string>(this.Prop_Queue, nameof(Prop_Queue), sd);

                var logic = new RabbitMqLogic();
                var purged = logic.PurgeQueue(channel, queue);

                SetVariableValue(this.Prop_PurgedCount, (int)purged, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[RabbitMQ] Очередь {queue} очищена: удалено {purged} сообщений"
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
