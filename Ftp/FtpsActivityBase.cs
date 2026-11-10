// =============================================================================
// FtpsActivityBase.cs — базовый класс FTPS-активностей (issue #31).
//
// Общие свойства подключения (хост, порт, учётные данные, таймауты) и
// шаблон выполнения с маппингом WebException в понятные сообщения.
// explicit TLS (EnableSsl) всегда включён; валидация сертификата —
// стандартная (fail-closed), опции «принять всё» нет (см. FtpsLogic).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Net;

namespace Primo.MIA
{
    /// <summary>
    /// Базовый класс для FTPS-активностей: общие свойства подключения.
    /// </summary>
    public abstract class FtpsActivityBase<TView> : PrimoComponentTO<TView>
        where TView : System.Windows.Controls.UserControl, new()
    {
        protected FtpsActivityBase(IWFContainer container) : base(container)
        {
        }

        // ── Свойства подключения ─────────────────────────────────────────

        private string _propHost;
        /// <summary>Хост FTPS-сервера.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpHost)]
        public string Prop_Host
        {
            get => _propHost;
            set { _propHost = value; InvokePropertyChanged(this, nameof(Prop_Host)); }
        }

        private string _propPort = "21";
        /// <summary>Порт FTPS-сервера (21 по умолчанию).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpPort)]
        public string Prop_Port
        {
            get => _propPort;
            set { _propPort = value; InvokePropertyChanged(this, nameof(Prop_Port)); }
        }

        private string _propUserName;
        /// <summary>Имя пользователя (пусто = anonymous).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpUserName)]
        public string Prop_UserName
        {
            get => _propUserName;
            set { _propUserName = value; InvokePropertyChanged(this, nameof(Prop_UserName)); }
        }

        private string _propPassword;
        /// <summary>Пароль (пусто = anonymous).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpPassword)]
        public string Prop_Password
        {
            get => _propPassword;
            set { _propPassword = value; InvokePropertyChanged(this, nameof(Prop_Password)); }
        }

        private string _propConnectionTimeoutSeconds = "30";
        /// <summary>Таймаут подключения, сек.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpConnectionTimeout)]
        public string Prop_ConnectionTimeoutSeconds
        {
            get => _propConnectionTimeoutSeconds;
            set { _propConnectionTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_ConnectionTimeoutSeconds)); }
        }

        private string _propOperationTimeoutSeconds = "300";
        /// <summary>Таймаут операции (чтение/запись), сек.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpOperationTimeout)]
        public string Prop_OperationTimeoutSeconds
        {
            get => _propOperationTimeoutSeconds;
            set { _propOperationTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_OperationTimeoutSeconds)); }
        }

        // ── Хелперы ──────────────────────────────────────────────────────

        /// <summary>
        /// Свойства подключения для панели Studio (общие для всех FTPS-активностей).
        /// </summary>
        protected static LTools.Common.Helpers.WFHelper.PropertiesItem[] ConnectionProperties()
        {
            return new[]
            {
                PropertyBuilder.String("Prop_Host", "Хост FTPS-сервера"),
                PropertyBuilder.Int("Prop_Port", "Порт (по умолчанию 21)"),
                PropertyBuilder.String("Prop_UserName", "Имя пользователя (пусто = anonymous)"),
                PropertyBuilder.String("Prop_Password", "Пароль"),
                PropertyBuilder.Int("Prop_ConnectionTimeoutSeconds", "Таймаут подключения, сек"),
                PropertyBuilder.Int("Prop_OperationTimeoutSeconds", "Таймаут операции, сек")
            };
        }

        /// <summary>Собирает настройки подключения из свойств активности.</summary>
        protected FtpsConnectionSettings ResolveSettings(ScriptingData sd)
        {
            return new FtpsConnectionSettings
            {
                Host = GetPropertyValue<string>(this.Prop_Host, nameof(Prop_Host), sd),
                Port = ParseInt(GetPropertyValue<string>(this.Prop_Port, nameof(Prop_Port), sd), 21),
                UserName = GetPropertyValue<string>(this.Prop_UserName, nameof(Prop_UserName), sd),
                Password = GetPropertyValue<string>(this.Prop_Password, nameof(Prop_Password), sd),
                ConnectionTimeoutSeconds = ParseInt(GetPropertyValue<string>(this.Prop_ConnectionTimeoutSeconds, nameof(Prop_ConnectionTimeoutSeconds), sd), 30),
                OperationTimeoutSeconds = ParseInt(GetPropertyValue<string>(this.Prop_OperationTimeoutSeconds, nameof(Prop_OperationTimeoutSeconds), sd), 300)
            };
        }

        /// <summary>Разбор целых значений с fallback'ом на значение по умолчанию.</summary>
        protected static int ParseInt(string raw, int fallback)
        {
            return StringHelper.TryParseInt(raw) ?? fallback;
        }

        /// <summary>
        /// Шаблон выполнения FTPS-операции с маппингом WebException
        /// (ошибки TLS/аутентификации/пути) в понятные сообщения.
        /// Настройки подключения разбираются один раз и передаются операции.
        /// </summary>
        protected ExecutionResult ExecuteFtps(
            ScriptingData sd, Func<FtpsConnectionSettings, FtpsLogic, ExecutionResult> operation)
        {
            try
            {
                var settings = ResolveSettings(sd);
                if (string.IsNullOrWhiteSpace(settings.Host))
                    return new ExecutionResult
                    {
                        IsSuccess = false,
                        ErrorMessage = ActivityStrings.Error_FtpHostRequired
                    };

                return operation(settings, new FtpsLogic());
            }
            catch (System.Net.WebException ex)
            {
                // FtpWebResponse.StatusCode уточняет причину (530 — логин, 550 — путь)
                var status = (ex.Response as FtpWebResponse)?.StatusCode;
                var statusInfo = status.HasValue ? $" (код {status.Value})" : string.Empty;
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка FTPS{statusInfo}: {ex.Message}"
                };
            }
            catch (InvalidOperationException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка FTPS: {ex.Message}"
                };
            }
        }
    }
}
