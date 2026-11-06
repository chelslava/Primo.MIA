// =============================================================================
// RabbitMqActivityBase.cs — базовый класс RabbitMQ-активностей:
// общие свойства подключения и шаблон выполнения.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using RabbitMQ.Client;
using Primo.MIA.Common;
using System;

namespace Primo.MIA
{
    /// <summary>
    /// Базовый класс для RabbitMQ-активностей: свойства подключения,
    /// шаблон «подключить → выполнить → закрыть».
    /// </summary>
    public abstract class RabbitMqActivityBase<TView> : PrimoComponentTO<TView>
        where TView : System.Windows.Controls.UserControl, new()
    {
        protected RabbitMqActivityBase(IWFContainer container) : base(container)
        {
        }

        // ── Свойства подключения ─────────────────────────────────────────

        private string _propHost;
        /// <summary>Хост RabbitMQ.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MqHost)]
        public string Prop_Host
        {
            get => _propHost;
            set { _propHost = value; InvokePropertyChanged(this, nameof(Prop_Host)); }
        }

        private string _propPort = "5672";
        /// <summary>Порт RabbitMQ (5672; TLS — 5671).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MqPort)]
        public string Prop_Port
        {
            get => _propPort;
            set { _propPort = value; InvokePropertyChanged(this, nameof(Prop_Port)); }
        }

        private string _propUserName;
        /// <summary>Имя пользователя.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MqUserName)]
        public string Prop_UserName
        {
            get => _propUserName;
            set { _propUserName = value; InvokePropertyChanged(this, nameof(Prop_UserName)); }
        }

        private string _propPassword;
        /// <summary>Пароль.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MqPassword)]
        public string Prop_Password
        {
            get => _propPassword;
            set { _propPassword = value; InvokePropertyChanged(this, nameof(Prop_Password)); }
        }

        private string _propVirtualHost = "/";
        /// <summary>VirtualHost (/ по умолчанию).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MqVirtualHost)]
        public string Prop_VirtualHost
        {
            get => _propVirtualHost;
            set { _propVirtualHost = value; InvokePropertyChanged(this, nameof(Prop_VirtualHost)); }
        }

        private bool _propUseTls;
        /// <summary>Использовать TLS (amqps).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MqUseTls)]
        public bool Prop_UseTls
        {
            get => _propUseTls;
            set { _propUseTls = value; InvokePropertyChanged(this, nameof(Prop_UseTls)); }
        }

        private string _propConnectionTimeoutSeconds = "30";
        /// <summary>Таймаут подключения в секундах.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MqConnectionTimeoutSeconds)]
        public string Prop_ConnectionTimeoutSeconds
        {
            get => _propConnectionTimeoutSeconds;
            set { _propConnectionTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_ConnectionTimeoutSeconds)); }
        }

        // ── Выполнение ───────────────────────────────────────────────────

        /// <summary>
        /// Резолвит настройки подключения из свойств.
        /// </summary>
        protected RabbitMqConnectionSettings ResolveSettings(ScriptingData sd)
        {
            return new RabbitMqConnectionSettings
            {
                Host = GetPropertyValue<string>(this.Prop_Host, nameof(Prop_Host), sd),
                Port = int.TryParse(GetPropertyValue<string>(this.Prop_Port, nameof(Prop_Port), sd), out var port) && port > 0 ? port : 5672,
                UserName = GetPropertyValue<string>(this.Prop_UserName, nameof(Prop_UserName), sd),
                Password = GetPropertyValue<string>(this.Prop_Password, nameof(Prop_Password), sd),
                VirtualHost = GetPropertyValue<string>(this.Prop_VirtualHost, nameof(Prop_VirtualHost), sd),
                UseTls = this.Prop_UseTls,
                ConnectionTimeoutSeconds = int.TryParse(GetPropertyValue<string>(this.Prop_ConnectionTimeoutSeconds, nameof(Prop_ConnectionTimeoutSeconds), sd), out var t) && t > 0 ? t : 30
            };
        }

        /// <summary>
        /// Шаблон «подключить → открыть канал → выполнить → закрыть»
        /// с маппингом исключений RabbitMQ (API 6.x — синхронный).
        /// </summary>
        protected ExecutionResult ExecuteRabbitMq(ScriptingData sd, Func<IModel, ExecutionResult> operation)
        {
            var settings = ResolveSettings(sd);
            IConnection connection = null;
            IModel channel = null;
            try
            {
                var logic = new RabbitMqLogic();
                var factory = logic.CreateFactory(settings);
                connection = factory.CreateConnection();
                channel = connection.CreateModel();

                return operation(channel);
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Брокер недоступен: {ex.Message}" };
            }
            catch (RabbitMQ.Client.Exceptions.AuthenticationFailureException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка аутентификации: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка RabbitMQ: {ex.Message}" };
            }
            finally
            {
                // порядок сохранён: сначала канал, затем подключение;
                // Close → Dispose внутри каждого шага. Исключения освобождения
                // уже закрытых ресурсов глушатся, при включённом файловом логе
                // фиксируются уровнем Debug
                SafeDispose.Quiet(
                    () =>
                    {
                        channel?.Close();
                        channel?.Dispose();
                    },
                    "RabbitMQ channel close/dispose");

                SafeDispose.Quiet(
                    () =>
                    {
                        connection?.Close();
                        connection?.Dispose();
                    },
                    "RabbitMQ connection close/dispose");
            }
        }
    }
}
