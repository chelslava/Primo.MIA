// =============================================================================
// EmlParseBack.cs — активность «Почта: Разобрать EML» (issue #37, фаза 2).
//
// Разбор локального .eml (MimeKit): тема/от/кому/дата/тело в переменные,
// вложения в папку, тело в файл. Без сети — полностью детерминированно.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Primo.MIA
{
    /// <summary>Активность разбора EML-файла.</summary>
    public class EmlParseBack : PrimoComponentTO<EmlParse>
    {
        public override string GroupName { get => ActivityCategories.Email; protected set { } }

        protected override int sdkTimeOut { get => 300000; set { } }

        // ── Вход ───────────────────────────────────────────────────────

        private string _propEmlPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MailEmlPath)]
        /// <summary>Путь к EML-файлу.</summary>
        public string Prop_EmlPath
        {
            get => _propEmlPath;
            set { _propEmlPath = value; InvokePropertyChanged(this, nameof(Prop_EmlPath)); }
        }

        private string _propAttachmentsDir;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MailAttachmentsDir)]
        /// <summary>Папка для сохранения вложений (пусто — не сохранять).</summary>
        public string Prop_AttachmentsDir
        {
            get => _propAttachmentsDir;
            set { _propAttachmentsDir = value; InvokePropertyChanged(this, nameof(Prop_AttachmentsDir)); }
        }

        private string _propBodyToFile;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MailBodyToFile)]
        /// <summary>Файл для тела (пусто — не сохранять).</summary>
        public string Prop_BodyToFile
        {
            get => _propBodyToFile;
            set { _propBodyToFile = value; InvokePropertyChanged(this, nameof(Prop_BodyToFile)); }
        }

        // ── Вывод ──────────────────────────────────────────────────────

        private string _propSubjectOut;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MailSubject)]
        /// <summary>Тема письма.</summary>
        public string Prop_SubjectOut
        {
            get => _propSubjectOut;
            set { _propSubjectOut = value; InvokePropertyChanged(this, nameof(Prop_SubjectOut)); }
        }

        private string _propFromOut;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MailFrom)]
        /// <summary>Отправитель.</summary>
        public string Prop_FromOut
        {
            get => _propFromOut;
            set { _propFromOut = value; InvokePropertyChanged(this, nameof(Prop_FromOut)); }
        }

        private string _propToOut;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MailToOut)]
        /// <summary>Получатели (через ;).</summary>
        public string Prop_ToOut
        {
            get => _propToOut;
            set { _propToOut = value; InvokePropertyChanged(this, nameof(Prop_ToOut)); }
        }

        private string _propDateOut;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MailDateOut)]
        /// <summary>Дата письма.</summary>
        public string Prop_DateOut
        {
            get => _propDateOut;
            set { _propDateOut = value; InvokePropertyChanged(this, nameof(Prop_DateOut)); }
        }

        private string _propBodyOut;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MailBody)]
        /// <summary>Тело письма (plain, при отсутствии — HTML).</summary>
        public string Prop_BodyOut
        {
            get => _propBodyOut;
            set { _propBodyOut = value; InvokePropertyChanged(this, nameof(Prop_BodyOut)); }
        }

        private string _propAttachmentsOut;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MailAttachmentsOut)]
        /// <summary>Пути сохранённых вложений (через ;).</summary>
        public string Prop_AttachmentsOut
        {
            get => _propAttachmentsOut;
            set { _propAttachmentsOut = value; InvokePropertyChanged(this, nameof(Prop_AttachmentsOut)); }
        }

        public EmlParseBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_EmlParse;
            sdkComponentHelp = @"Компонент ""Почта: Разобрать EML""
Разбирает локальный EML-файл (MimeKit) в переменные.

Вход:
Путь к EML*: [String] Файл .eml.

Обработка:
Вложения → папка: [String] Пусто — не сохранять.
Тело → файл: [String] Пусто — не сохранять (UTF-8).

Вывод:
Тема: [String] Отправитель: [String] Кому: [String] (через ;)
Дата: [DateTime] Тело: [String] (plain, при отсутствии — HTML)
Вложения (пути): [String] (через ;)";
            sdkComponentIcon = ActivityIcons.Mail;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_EmlPath", "Путь к EML-файлу"),
                PropertyBuilder.String("Prop_AttachmentsDir", "Папка для сохранения вложений"),
                PropertyBuilder.String("Prop_BodyToFile", "Файл для тела письма"),
                PropertyBuilder.Variable<string>("Prop_SubjectOut", "Тема письма"),
                PropertyBuilder.Variable<string>("Prop_FromOut", "Отправитель"),
                PropertyBuilder.Variable<string>("Prop_ToOut", "Получатели, через ;"),
                PropertyBuilder.Variable<string>("Prop_DateOut", "Дата письма"),
                PropertyBuilder.Variable<string>("Prop_BodyOut", "Тело письма"),
                PropertyBuilder.Variable<string>("Prop_AttachmentsOut", "Пути сохранённых вложений, через ;")
            };

            InitClass(container);

            this.Prop_EmlPath = "\"\"";
            this.Prop_AttachmentsDir = "\"\"";
            this.Prop_BodyToFile = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new ImapLogic();
            try
            {
                var emlPath = GetPropertyValue<string>(this.Prop_EmlPath, nameof(Prop_EmlPath), sd);
                var attachmentsDir = GetPropertyValue<string>(this.Prop_AttachmentsDir, nameof(Prop_AttachmentsDir), sd);
                var bodyToFile = GetPropertyValue<string>(this.Prop_BodyToFile, nameof(Prop_BodyToFile), sd);

                Guard.That(!string.IsNullOrWhiteSpace(emlPath),
                    ActivityStrings.Error_MailEmlRequired, nameof(Prop_EmlPath));

                var message = logic.LoadEml(emlPath);
                var body = logic.GetBodyText(message);

                var saved = string.IsNullOrWhiteSpace(attachmentsDir)
                    ? new List<string>()
                    : logic.SaveAttachments(message, attachmentsDir);

                if (!string.IsNullOrWhiteSpace(this.Prop_SubjectOut))
                    SetVariableValue(this.Prop_SubjectOut, message.Subject ?? string.Empty, sd);
                if (!string.IsNullOrWhiteSpace(this.Prop_FromOut))
                    SetVariableValue(this.Prop_FromOut, message.From.ToString(), sd);
                if (!string.IsNullOrWhiteSpace(this.Prop_ToOut))
                    SetVariableValue(this.Prop_ToOut, string.Join(";",
                        message.To.Select(a => a.ToString())), sd);
                if (!string.IsNullOrWhiteSpace(this.Prop_DateOut))
                    SetVariableValue(this.Prop_DateOut,
                        message.Date.DateTime == DateTimeOffset.MinValue
                            ? (object)string.Empty
                            : message.Date.DateTime, sd);
                if (!string.IsNullOrWhiteSpace(this.Prop_BodyOut))
                    SetVariableValue(this.Prop_BodyOut, body, sd);
                if (!string.IsNullOrWhiteSpace(this.Prop_AttachmentsOut) && saved.Count > 0)
                    SetVariableValue(this.Prop_AttachmentsOut, string.Join(";", saved), sd);

                if (!string.IsNullOrWhiteSpace(bodyToFile))
                    File.WriteAllText(bodyToFile, body, Encoding.UTF8);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Почта] EML разобран: «{message.Subject}»"
                        + (saved.Count > 0 ? $", вложений: {saved.Count}" : string.Empty)
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка разбора EML: {ex.Message}"
                };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_EmlPath, ActivityStrings.Field_MailEmlPath, ActivityStrings.Error_MailEmlRequired);
            return ret;
        }
    }
}
