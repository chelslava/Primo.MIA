// =============================================================================
// RabbitMqPublishBack.cs — активность «RabbitMQ: Отправить сообщение».
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primo.MIA
{
    /// <summary>Активность публикации сообщения в RabbitMQ.</summary>
    public class RabbitMqPublishBack : RabbitMqActivityBase<RabbitMqPublish>
    {
        public override string GroupName { get => ActivityCategories.MessageQueues; protected set { } }

        protected override int sdkTimeOut
        {
            get { return ActivityTimeoutParser.ParseSecondsToMs(Prop_ConnectionTimeoutSeconds, 30000); }
            set { }
        }

        private string _propExchange = "\"\"";
        /// <summary>Exchange (пустая строка — дефолтный).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Publish), System.ComponentModel.DisplayName(ActivityStrings.Field_MqExchange)]
        public string Prop_Exchange { get => _propExchange; set { _propExchange = value; InvokePropertyChanged(this, nameof(Prop_Exchange)); } }

        private string _propRoutingKey;
        /// <summary>Routing key (для дефолтного exchange — имя очереди).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Publish), System.ComponentModel.DisplayName(ActivityStrings.Field_MqRoutingKey)]
        public string Prop_RoutingKey { get => _propRoutingKey; set { _propRoutingKey = value; InvokePropertyChanged(this, nameof(Prop_RoutingKey)); } }

        private string _propBody;
        /// <summary>Тело сообщения (UTF-8).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Publish), System.ComponentModel.DisplayName(ActivityStrings.Field_MqBody)]
        public string Prop_Body { get => _propBody; set { _propBody = value; InvokePropertyChanged(this, nameof(Prop_Body)); } }

        private bool _propPersistent = true;
        /// <summary>Persistent доставка (сообщение переживёт перезапуск брокера).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Mq_Category_Publish), System.ComponentModel.DisplayName(ActivityStrings.Field_MqPersistent)]
        public bool Prop_Persistent { get => _propPersistent; set { _propPersistent = value; InvokePropertyChanged(this, nameof(Prop_Persistent)); } }

        public RabbitMqPublishBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_RabbitMqPublish;
            sdkComponentHelp = @"Компонент ""RabbitMQ: Отправить сообщение""
Публикует сообщение в RabbitMQ. Если exchange пуст — используется дефолтный (""""), routing key тогда задаёт имя очереди напрямую.

Основные:
Хост*: [String] Адрес брокера RabbitMQ.
Пользователь*: [String] Имя пользователя.
Пароль: [String] Пароль.
Exchange: [String] Имя exchange; пусто — дефолтный.
Routing Key: [String] Routing key (для дефолтного exchange — имя очереди).
Тело сообщения*: [String] Текст сообщения (UTF-8).

Публикация:
Persistent: [Boolean] DeliveryMode=2 (переживает перезапуск брокера). По умолчанию True.

Настройки:
VirtualHost: [String] По умолчанию ""/"".
TLS: [Boolean] Использовать amqps (порт по умолчанию 5671).";
            sdkComponentIcon = ActivityIcons.MessageQueue;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Host", "Хост RabbitMQ"),
                PropertyBuilder.Int("Prop_Port", "Порт (5672; TLS — 5671)"),
                PropertyBuilder.String("Prop_UserName", "Имя пользователя"),
                PropertyBuilder.String("Prop_Password", "Пароль"),
                PropertyBuilder.String("Prop_VirtualHost", "VirtualHost (/ по умолчанию)"),
                PropertyBuilder.BooleanObject("Prop_UseTls", "Использовать TLS"),
                PropertyBuilder.String("Prop_Exchange", "Exchange (пусто = дефолтный)"),
                PropertyBuilder.String("Prop_RoutingKey", "Routing Key"),
                PropertyBuilder.String("Prop_Body", "Тело сообщения"),
                PropertyBuilder.BooleanObject("Prop_Persistent", "Persistent доставка")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var exchange = GetPropertyValue<string>(this.Prop_Exchange, nameof(Prop_Exchange), sd) ?? string.Empty;
                var routingKey = GetPropertyValue<string>(this.Prop_RoutingKey, nameof(Prop_RoutingKey), sd) ?? string.Empty;
                var body = GetPropertyValue<string>(this.Prop_Body, nameof(Prop_Body), sd);

                if (string.IsNullOrWhiteSpace(body))
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = ActivityStrings.Error_MqBodyRequired };

                return ExecuteRabbitMq(sd, channel =>
                {
                    var properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = (byte)(this.Prop_Persistent ? 2 : 1);
                    properties.ContentType = "text/plain; charset=utf-8";

                    channel.BasicPublish(exchange, routingKey, properties, Encoding.UTF8.GetBytes(body));
                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = $"Сообщение опубликовано: exchange='{exchange}', routingKey='{routingKey}'"
                    };
                });
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка публикации: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Host, ActivityStrings.Field_MqHost, ActivityStrings.Error_MqHostRequired);
            return ret;
        }
    }
}
