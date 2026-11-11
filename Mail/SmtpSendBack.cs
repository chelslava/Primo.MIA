// =============================================================================
// SmtpSendBack.cs — активность «Почта: Отправить письмо» (issue #37, фаза 1).
//
// MailKit SmtpClient: STARTTLS/implicit TLS/без шифрования, вложения,
// plain/HTML тело. IMAP/POP3 — фазы 2/3 (#37).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using MailKit.Net.Smtp;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Primo.MIA
{
    /// <summary>Активность отправки письма по SMTP.</summary>
    public class SmtpSendBack : PrimoComponentTO<SmtpSend>
    {
        public override string GroupName { get => ActivityCategories.Email; protected set { } }

        protected override int sdkTimeOut { get => 300000; set { } }

        // ── Подключение ────────────────────────────────────────────────

        private string _propHost;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MailHost)]
        /// <summary>Хост SMTP-сервера.</summary>
        public string Prop_Host
        {
            get => _propHost;
            set { _propHost = value; InvokePropertyChanged(this, nameof(Prop_Host)); }
        }

        private string _propPort = "587";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MailPort)]
        /// <summary>Порт (587 STARTTLS / 465 TLS / 25 без шифрования).</summary>
        public string Prop_Port
        {
            get => _propPort;
            set { _propPort = value; InvokePropertyChanged(this, nameof(Prop_Port)); }
        }

        private string _propUserName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MailUserName)]
        /// <summary>Имя пользователя (пусто = без авторизации).</summary>
        public string Prop_UserName
        {
            get => _propUserName;
            set { _propUserName = value; InvokePropertyChanged(this, nameof(Prop_UserName)); }
        }

        private string _propPassword;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MailPassword)]
        /// <summary>Пароль.</summary>
        public string Prop_Password
        {
            get => _propPassword;
            set { _propPassword = value; InvokePropertyChanged(this, nameof(Prop_Password)); }
        }

        // ── Письмо ─────────────────────────────────────────────────────

        private string _propFrom;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Message), System.ComponentModel.DisplayName(ActivityStrings.Field_MailFrom)]
        /// <summary>Отправитель (email или «Имя &lt;email&gt;»).</summary>
        public string Prop_From
        {
            get => _propFrom;
            set { _propFrom = value; InvokePropertyChanged(this, nameof(Prop_From)); }
        }

        private string _propTo;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Message), System.ComponentModel.DisplayName(ActivityStrings.Field_MailTo)]
        /// <summary>Получатели (через ; или ,).</summary>
        public string Prop_To
        {
            get => _propTo;
            set { _propTo = value; InvokePropertyChanged(this, nameof(Prop_To)); }
        }

        private string _propCc;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Message), System.ComponentModel.DisplayName(ActivityStrings.Field_MailCc)]
        /// <summary>Копия (через ; или ,).</summary>
        public string Prop_Cc
        {
            get => _propCc;
            set { _propCc = value; InvokePropertyChanged(this, nameof(Prop_Cc)); }
        }

        private string _propSubject;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Message), System.ComponentModel.DisplayName(ActivityStrings.Field_MailSubject)]
        /// <summary>Тема письма.</summary>
        public string Prop_Subject
        {
            get => _propSubject;
            set { _propSubject = value; InvokePropertyChanged(this, nameof(Prop_Subject)); }
        }

        private string _propBody;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Message), System.ComponentModel.DisplayName(ActivityStrings.Field_MailBody)]
        /// <summary>Тело письма.</summary>
        public string Prop_Body
        {
            get => _propBody;
            set { _propBody = value; InvokePropertyChanged(this, nameof(Prop_Body)); }
        }

        private bool _propBodyIsHtml;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Message), System.ComponentModel.DisplayName(ActivityStrings.Field_MailBodyHtml)]
        /// <summary>Тело — HTML.</summary>
        public bool Prop_BodyIsHtml
        {
            get => _propBodyIsHtml;
            set { _propBodyIsHtml = value; InvokePropertyChanged(this, nameof(Prop_BodyIsHtml)); }
        }

        private string _propAttachments;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Message), System.ComponentModel.DisplayName(ActivityStrings.Field_MailAttachments)]
        /// <summary>Вложения (пути через ;).</summary>
        public string Prop_Attachments
        {
            get => _propAttachments;
            set { _propAttachments = value; InvokePropertyChanged(this, nameof(Prop_Attachments)); }
        }

        // ── Настройки ──────────────────────────────────────────────────

        private string _propTlsMode = "StartTls";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MailTlsMode)]
        /// <summary>Режим TLS: StartTls / ImplicitTls / None.</summary>
        public string Prop_TlsMode
        {
            get => _propTlsMode;
            set { _propTlsMode = value; InvokePropertyChanged(this, nameof(Prop_TlsMode)); }
        }

        private string _propTimeoutSeconds = "60";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MailTimeout)]
        /// <summary>Таймаут подключения/операции, сек.</summary>
        public string Prop_TimeoutSeconds
        {
            get => _propTimeoutSeconds;
            set { _propTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_TimeoutSeconds)); }
        }

        public SmtpSendBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_SmtpSend;
            sdkComponentHelp = @"Компонент ""Почта: Отправить письмо""
Отправляет письмо через SMTP (MailKit).

Подключение:
Хост*: [String] Порт: [Int32] 587 (STARTTLS) / 465 (TLS) / 25.
Пользователь: [String] Пусто = без авторизации. Пароль: [String]

Письмо:
Отправитель*: [String] email или «Имя <email>».
Получатели*: [String] Через ; или , .
Копия: [String]
Тема: [String] Тело: [String]
Тело HTML: [Boolean] Вложения: [String] Пути через ;.

Настройки:
TLS: [String] StartTls / ImplicitTls / None (по умолчанию StartTls).
Таймаут (сек): [Int32] По умолчанию 60.";
            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Host", "Хост SMTP-сервера"),
                PropertyBuilder.Int("Prop_Port", "Порт (587/465/25)"),
                PropertyBuilder.String("Prop_UserName", "Пользователь (пусто = без авторизации)"),
                PropertyBuilder.String("Prop_Password", "Пароль"),
                PropertyBuilder.String("Prop_From", "Отправитель"),
                PropertyBuilder.String("Prop_To", "Получатели, через ; или ,"),
                PropertyBuilder.String("Prop_Cc", "Копия, через ; или ,"),
                PropertyBuilder.String("Prop_Subject", "Тема"),
                PropertyBuilder.String("Prop_Body", "Тело письма"),
                PropertyBuilder.BooleanObject("Prop_BodyIsHtml", "Тело HTML"),
                PropertyBuilder.String("Prop_Attachments", "Вложения, пути через ;"),
                PropertyBuilder.String("Prop_TlsMode", "TLS: StartTls / ImplicitTls / None"),
                PropertyBuilder.Int("Prop_TimeoutSeconds", "Таймаут, сек")
            };

            InitClass(container);

            this.Prop_Host = "\"\"";
            this.Prop_UserName = "\"\"";
            this.Prop_Password = "\"\"";
            this.Prop_From = "\"\"";
            this.Prop_To = "\"\"";
            this.Prop_Cc = "\"\"";
            this.Prop_Subject = "\"\"";
            this.Prop_Body = "\"\"";
            this.Prop_Attachments = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new SmtpLogic();
            try
            {
                var host = GetPropertyValue<string>(this.Prop_Host, nameof(Prop_Host), sd);
                var userName = GetPropertyValue<string>(this.Prop_UserName, nameof(Prop_UserName), sd);
                var password = GetPropertyValue<string>(this.Prop_Password, nameof(Prop_Password), sd);
                var from = GetPropertyValue<string>(this.Prop_From, nameof(Prop_From), sd);
                var to = GetPropertyValue<string>(this.Prop_To, nameof(Prop_To), sd);
                var cc = GetPropertyValue<string>(this.Prop_Cc, nameof(Prop_Cc), sd);
                var subject = GetPropertyValue<string>(this.Prop_Subject, nameof(Prop_Subject), sd);
                var body = GetPropertyValue<string>(this.Prop_Body, nameof(Prop_Body), sd);
                var attachments = GetPropertyValue<string>(this.Prop_Attachments, nameof(Prop_Attachments), sd);
                var tlsMode = GetPropertyValue<string>(this.Prop_TlsMode, nameof(Prop_TlsMode), sd);
                var timeout = StringHelper.TryParseInt(
                    GetPropertyValue<string>(this.Prop_TimeoutSeconds, nameof(Prop_TimeoutSeconds), sd)) ?? 60;

                var settings = new SmtpConnectionSettings
                {
                    Host = host,
                    Port = StringHelper.TryParseInt(
                        GetPropertyValue<string>(this.Prop_Port, nameof(Prop_Port), sd)) ?? 587,
                    UseImplicitTls = string.Equals(tlsMode, "ImplicitTls", StringComparison.OrdinalIgnoreCase),
                    UseStartTls = !string.Equals(tlsMode, "None", StringComparison.OrdinalIgnoreCase)
                                  && !string.Equals(tlsMode, "ImplicitTls", StringComparison.OrdinalIgnoreCase),
                    UserName = userName,
                    Password = password,
                    TimeoutSeconds = timeout <= 0 ? 60 : timeout
                };

                logic.ValidateSettings(settings, from);
                var toList = logic.ParseAddresses(to);
                var ccList = logic.ParseAddresses(cc);
                var attachmentPaths = logic.ParseAttachments(attachments);
                var message = logic.BuildMessage(from, toList, ccList, subject, body, this.Prop_BodyIsHtml, attachmentPaths);

                // Сетевая отправка (MailKit)
                Send(settings, message, settings.TimeoutSeconds);

                var attachmentsInfo = attachmentPaths.Count > 0 ? $", вложений: {attachmentPaths.Count}" : string.Empty;
                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Почта] Отправлено «{subject}»: {toList.Count} получателю(ей){attachmentsInfo}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка SMTP: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Сетевая отправка (MailKit, sync-over-async — SyncContext в роботе нет).
        /// </summary>
        private void Send(SmtpConnectionSettings settings, MimeKit.MimeMessage message, int timeoutSeconds)
        {
            using (var client = new SmtpClient())
            {
                client.Timeout = timeoutSeconds * 1000;
                var options = new SmtpLogic().ResolveSocketOptions(settings);

                // MailKit 4.x имеет синхронные перегрузки
                client.Connect(settings.Host, settings.Port, options);

                try
                {
                    if (!string.IsNullOrWhiteSpace(settings.UserName))
                        client.Authenticate(settings.UserName, settings.Password);

                    client.Send(message);
                }
                finally
                {
                    SafeDispose.Quiet(
                        () => client.Disconnect(true), "SMTP disconnect");
                }
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Host, ActivityStrings.Field_MailHost, ActivityStrings.Error_MailHostRequired);
            ret.ValidateRequired(this.Prop_From, ActivityStrings.Field_MailFrom, ActivityStrings.Error_MailFromRequired);
            ret.ValidateRequired(this.Prop_To, ActivityStrings.Field_MailTo, ActivityStrings.Error_MailToRequired);
            return ret;
        }
    }
}
