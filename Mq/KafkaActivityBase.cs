// =============================================================================
// KafkaActivityBase.cs — базовый класс Kafka-активностей (issue #19):
// общие свойства подключения (bootstrap, TLS/SASL) и шаблон выполнения.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using Confluent.Kafka;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Базовый класс для Kafka-активностей: свойства подключения
    /// (bootstrap-серверы, TLS/SASL) и шаблоны «создать клиент → выполнить»
    /// для продюсера и консьюмера.
    /// </summary>
    public abstract class KafkaActivityBase<TView> : PrimoComponentTO<TView>
        where TView : System.Windows.Controls.UserControl, new()
    {
        protected KafkaActivityBase(IWFContainer container) : base(container)
        {
        }

        // ── Свойства подключения ─────────────────────────────────────────

        private string _propBootstrapServers;
        /// <summary>Адреса bootstrap-серверов: "host1:9092,host2:9092".</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaBootstrapServers)]
        public string Prop_BootstrapServers
        {
            get => _propBootstrapServers;
            set { _propBootstrapServers = value; InvokePropertyChanged(this, nameof(Prop_BootstrapServers)); }
        }

        private KafkaSecurityProtocol _propSecurityProtocol = KafkaSecurityProtocol.Plaintext;
        /// <summary>Протокол безопасности (Plaintext/Ssl/SaslPlaintext/SaslSsl).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaSecurityProtocol)]
        public KafkaSecurityProtocol Prop_SecurityProtocol
        {
            get => _propSecurityProtocol;
            set { _propSecurityProtocol = value; InvokePropertyChanged(this, nameof(Prop_SecurityProtocol)); }
        }

        private KafkaSaslMechanism _propSaslMechanism = KafkaSaslMechanism.None;
        /// <summary>SASL-механизм (при SASL-протоколах).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaSaslMechanism)]
        public KafkaSaslMechanism Prop_SaslMechanism
        {
            get => _propSaslMechanism;
            set { _propSaslMechanism = value; InvokePropertyChanged(this, nameof(Prop_SaslMechanism)); }
        }

        private string _propSaslUsername;
        /// <summary>SASL-имя пользователя (при SASL-протоколах).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaSaslUsername)]
        public string Prop_SaslUsername
        {
            get => _propSaslUsername;
            set { _propSaslUsername = value; InvokePropertyChanged(this, nameof(Prop_SaslUsername)); }
        }

        private string _propSaslPassword;
        /// <summary>SASL-пароль (при SASL-протоколах).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaSaslPassword)]
        public string Prop_SaslPassword
        {
            get => _propSaslPassword;
            set { _propSaslPassword = value; InvokePropertyChanged(this, nameof(Prop_SaslPassword)); }
        }

        private string _propConnectionTimeoutSeconds = "30";
        /// <summary>Таймаут подключения/операций в секундах.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_KafkaConnectionTimeoutSeconds)]
        public string Prop_ConnectionTimeoutSeconds
        {
            get => _propConnectionTimeoutSeconds;
            set { _propConnectionTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_ConnectionTimeoutSeconds)); }
        }

        // ── Выполнение ───────────────────────────────────────────────────

        /// <summary>
        /// Резолвит настройки подключения из свойств.
        /// </summary>
        protected KafkaConnectionSettings ResolveSettings(ScriptingData sd)
        {
            return new KafkaConnectionSettings
            {
                BootstrapServers = GetPropertyValue<string>(this.Prop_BootstrapServers, nameof(Prop_BootstrapServers), sd),
                SecurityProtocol = this.Prop_SecurityProtocol,
                SaslMechanism = this.Prop_SaslMechanism,
                SaslUsername = GetPropertyValue<string>(this.Prop_SaslUsername, nameof(Prop_SaslUsername), sd),
                SaslPassword = GetPropertyValue<string>(this.Prop_SaslPassword, nameof(Prop_SaslPassword), sd),
                ConnectionTimeoutSeconds = int.TryParse(GetPropertyValue<string>(this.Prop_ConnectionTimeoutSeconds, nameof(Prop_ConnectionTimeoutSeconds), sd), out var t) && t > 0 ? t : 30
            };
        }

        /// <summary>
        /// Общий набор свойств подключения для панели свойств Studio.
        /// </summary>
        protected List<LTools.Common.Helpers.WFHelper.PropertiesItem> BuildConnectionProperties()
        {
            return new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_BootstrapServers", "Bootstrap-серверы (host:9092)"),
                PropertyBuilder.Enum<KafkaSecurityProtocol>("Prop_SecurityProtocol", "Протокол безопасности"),
                PropertyBuilder.Enum<KafkaSaslMechanism>("Prop_SaslMechanism", "SASL-механизм"),
                PropertyBuilder.String("Prop_SaslUsername", "SASL: имя пользователя"),
                PropertyBuilder.String("Prop_SaslPassword", "SASL: пароль"),
                PropertyBuilder.Int("Prop_ConnectionTimeoutSeconds", "Таймаут подключения (сек)")
            };
        }

        /// <summary>
        /// Шаблон «создать продюсер → выполнить → закрыть» с маппингом
        /// исключений Confluent.Kafka.
        /// </summary>
        protected ExecutionResult ExecuteKafkaProducer(ScriptingData sd, Func<IProducer<string, string>, ExecutionResult> operation)
        {
            try
            {
                var logic = new KafkaLogic();
                var config = logic.BuildProducerConfig(ResolveSettings(sd));
                using (var producer = new ProducerBuilder<string, string>(config).Build())
                {
                    return operation(producer);
                }
            }
            catch (ProduceException<string, string> ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка доставки: {ex.Error.Reason}" };
            }
            catch (KafkaException ex)
            {
                // включает и конфигурационные ошибки (Local_Configuration) —
                // отдельный ConstructException в Confluent.Kafka 2.12 отсутствует
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка Kafka: {ex.Error.Reason}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка Kafka: {ex.Message}" };
            }
        }

        /// <summary>
        /// Шаблон «создать консьюмер → выполнить → закрыть» с маппингом
        /// исключений Confluent.Kafka.
        /// </summary>
        protected ExecutionResult ExecuteKafkaConsumer(ScriptingData sd, Func<IConsumer<string, string>, ExecutionResult> operation)
        {
            try
            {
                var logic = new KafkaLogic();
                var config = logic.BuildConsumerConfig(ResolveSettings(sd), ResolveConsumerOptions());
                using (var consumer = new ConsumerBuilder<string, string>(config).Build())
                {
                    return operation(consumer);
                }
            }
            catch (ConsumeException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка потребления: {ex.Error.Reason}" };
            }
            catch (KafkaException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка Kafka: {ex.Error.Reason}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка Kafka: {ex.Message}" };
            }
        }

        /// <summary>
        /// Опции консьюмера; переопределяется в KafkaConsumeBack
        /// (Group ID, Auto Offset Reset, Auto Commit).
        /// </summary>
        protected virtual KafkaConsumerOptions ResolveConsumerOptions()
        {
            return new KafkaConsumerOptions();
        }
    }
}
