// =============================================================================
// Pop3ReceiveBack.cs — активность «Почта: Получить письма (POP3)»
// (issue #37, фаза 3).
//
// MailKit Pop3Client: простая выборка для legacy-ящиков. POP3 не умеет
// серверные поиск/флаги — фильтры применяются клиентски, письма сортируются
// «от новых к старым» по дате, уникальность — по UIDL (колонка UID таблицы).
// Опционально: удаление загруженных писем (POP3-идиома обработки ящика).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using MailKit.Net.Pop3;
using MailKit.Security;
using MimeKit;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>Активность выборки писем по POP3.</summary>
    public class Pop3ReceiveBack : PrimoComponentTO<Pop3Receive>
    {
        public override string GroupName { get => ActivityCategories.Email; protected set { } }

        protected override int sdkTimeOut { get => 300000; set { } }

        // ── Подключение ────────────────────────────────────────────────

        private string _propHost;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MailHost)]
        /// <summary>Хост POP3-сервера.</summary>
        public string Prop_Host
        {
            get => _propHost;
            set { _propHost = value; InvokePropertyChanged(this, nameof(Prop_Host)); }
        }

        private string _propPort = "995";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MailPort)]
        /// <summary>Порт (995 TLS / 110 STARTTLS / plain).</summary>
        public string Prop_Port
        {
            get => _propPort;
            set { _propPort = value; InvokePropertyChanged(this, nameof(Prop_Port)); }
        }

        private string _propUserName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MailUserName)]
        /// <summary>Имя пользователя.</summary>
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

        // ── Фильтры (клиентская обработка) ─────────────────────────────

        private string _propSubjectContains;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Filter), System.ComponentModel.DisplayName(ActivityStrings.Field_MailSubjectFilter)]
        /// <summary>Подстрока в теме (пусто — без фильтра).</summary>
        public string Prop_SubjectContains
        {
            get => _propSubjectContains;
            set { _propSubjectContains = value; InvokePropertyChanged(this, nameof(Prop_SubjectContains)); }
        }

        private string _propFromContains;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Filter), System.ComponentModel.DisplayName(ActivityStrings.Field_MailFromFilter)]
        /// <summary>Подстрока в отправителе (пусто — без фильтра).</summary>
        public string Prop_FromContains
        {
            get => _propFromContains;
            set { _propFromContains = value; InvokePropertyChanged(this, nameof(Prop_FromContains)); }
        }

        private string _propSince;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Filter), System.ComponentModel.DisplayName(ActivityStrings.Field_MailSince)]
        /// <summary>Дата «с» (ГГГГ-ММ-ДД или ДД.ММ.ГГГГ; пусто — без ограничения).</summary>
        public string Prop_Since
        {
            get => _propSince;
            set { _propSince = value; InvokePropertyChanged(this, nameof(Prop_Since)); }
        }

        private string _propMaxCount = "0";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Filter), System.ComponentModel.DisplayName(ActivityStrings.Field_MailMaxCount)]
        /// <summary>Максимум писем (0 — без ограничения).</summary>
        public string Prop_MaxCount
        {
            get => _propMaxCount;
            set { _propMaxCount = value; InvokePropertyChanged(this, nameof(Prop_MaxCount)); }
        }

        // ── Обработка ──────────────────────────────────────────────────

        private string _propTlsMode = "ImplicitTls";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MailTlsMode)]
        /// <summary>Режим TLS: ImplicitTls / StartTls / None.</summary>
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

        private bool _propDeleteAfterDownload;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_MailDeleteAfterDownload)]
        /// <summary>Удалить загруженные письма с сервера (коммит при отключении).</summary>
        public bool Prop_DeleteAfterDownload
        {
            get => _propDeleteAfterDownload;
            set { _propDeleteAfterDownload = value; InvokePropertyChanged(this, nameof(Prop_DeleteAfterDownload)); }
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
        /// <summary>Файл для тела последнего письма (пусто — не сохранять).</summary>
        public string Prop_BodyToFile
        {
            get => _propBodyToFile;
            set { _propBodyToFile = value; InvokePropertyChanged(this, nameof(Prop_BodyToFile)); }
        }

        // ── Вывод ──────────────────────────────────────────────────────

        private string _propMessages;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MailMessages)]
        /// <summary>Таблица писем (От, Тема, Дата, UID, Тело).</summary>
        public string Prop_Messages
        {
            get => _propMessages;
            set { _propMessages = value; InvokePropertyChanged(this, nameof(Prop_Messages)); }
        }

        private string _propCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MailCount)]
        /// <summary>Количество выбранных писем.</summary>
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, nameof(Prop_Count)); }
        }

        private string _propBodyOut;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Mail_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MailBodyOut)]
        /// <summary>Тело последнего письма (plain, при отсутствии — HTML).</summary>
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

        public Pop3ReceiveBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_Pop3Receive;
            sdkComponentHelp = @"Компонент ""Почта: Получить письма (POP3)""
Выбирает письма по POP3 (MailKit) — простая выборка для legacy-ящиков.
POP3 не поддерживает серверный поиск и флаги «прочитано»: фильтры
применяются на клиенте, выдача сортируется «от новых к старым» по дате,
уникальность писем отслеживается по UIDL (колонка UID таблицы).

Подключение:
Хост*: [String] Порт: [Int32] 995 (TLS) / 110 (STARTTLS).
Пользователь: [String] Пароль: [String]

Фильтры (клиентские):
Тема содержит: [String] Отправитель содержит: [String]
С даты: [String] ГГГГ-ММ-ДД или ДД.ММ.ГГГГ.
Макс. писем: [Int32] 0 = без ограничения.

Обработка:
TLS: [String] ImplicitTls (995) / StartTls (110) / None. Таймаут: [Int32] сек.
Удалить после загрузки: [Boolean] — письма удаляются с сервера.
Вложения → папка: [String] (по подпапке на письмо).
Тело → файл: [String] Тело последнего письма.

Вывод:
Письма (DataTable): От, Тема, Дата, UID, Тело.
Кол-во писем: [Int32] Тело (последнее): [String] Вложения (пути): [String]";
            sdkComponentIcon = ActivityIcons.Mail;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Host", "Хост POP3-сервера"),
                PropertyBuilder.Int("Prop_Port", "Порт (995/110)"),
                PropertyBuilder.String("Prop_UserName", "Пользователь"),
                PropertyBuilder.String("Prop_Password", "Пароль"),
                PropertyBuilder.String("Prop_TlsMode", "TLS: ImplicitTls / StartTls / None"),
                PropertyBuilder.Int("Prop_TimeoutSeconds", "Таймаут, сек"),
                PropertyBuilder.String("Prop_SubjectContains", "Подстрока в теме"),
                PropertyBuilder.String("Prop_FromContains", "Подстрока в отправителе"),
                PropertyBuilder.String("Prop_Since", "С даты (ГГГГ-ММ-ДД / ДД.ММ.ГГГГ)"),
                PropertyBuilder.Int("Prop_MaxCount", "Макс. писем (0 = все)"),
                PropertyBuilder.BooleanObject("Prop_DeleteAfterDownload", "Удалить после загрузки"),
                PropertyBuilder.String("Prop_AttachmentsDir", "Папка для вложений"),
                PropertyBuilder.String("Prop_BodyToFile", "Файл для тела последнего письма"),
                PropertyBuilder.Variable<DataTable>("Prop_Messages", "Письма в виде DataTable"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество писем"),
                PropertyBuilder.Variable<string>("Prop_BodyOut", "Тело последнего письма"),
                PropertyBuilder.Variable<string>("Prop_AttachmentsOut", "Пути сохранённых вложений, через ;")
            };

            InitClass(container);

            this.Prop_Host = "\"\"";
            this.Prop_UserName = "\"\"";
            this.Prop_Password = "\"\"";
            this.Prop_SubjectContains = "\"\"";
            this.Prop_FromContains = "\"\"";
            this.Prop_Since = "\"\"";
            this.Prop_AttachmentsDir = "\"\"";
            this.Prop_BodyToFile = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new Pop3Logic();
            try
            {
                var host = GetPropertyValue<string>(this.Prop_Host, nameof(Prop_Host), sd);
                var userName = GetPropertyValue<string>(this.Prop_UserName, nameof(Prop_UserName), sd);
                var password = GetPropertyValue<string>(this.Prop_Password, nameof(Prop_Password), sd);
                var subjectContains = GetPropertyValue<string>(this.Prop_SubjectContains, nameof(Prop_SubjectContains), sd);
                var fromContains = GetPropertyValue<string>(this.Prop_FromContains, nameof(Prop_FromContains), sd);
                var since = GetPropertyValue<string>(this.Prop_Since, nameof(Prop_Since), sd);
                var attachmentsDir = GetPropertyValue<string>(this.Prop_AttachmentsDir, nameof(Prop_AttachmentsDir), sd);
                var bodyToFile = GetPropertyValue<string>(this.Prop_BodyToFile, nameof(Prop_BodyToFile), sd);
                var tlsMode = GetPropertyValue<string>(this.Prop_TlsMode, nameof(Prop_TlsMode), sd);
                var timeout = StringHelper.TryParseInt(
                    GetPropertyValue<string>(this.Prop_TimeoutSeconds, nameof(Prop_TimeoutSeconds), sd)) ?? 60;
                var maxCount = StringHelper.TryParseInt(
                    GetPropertyValue<string>(this.Prop_MaxCount, nameof(Prop_MaxCount), sd)) ?? 0;

                var settings = new Pop3ConnectionSettings
                {
                    Host = host,
                    Port = StringHelper.TryParseInt(
                        GetPropertyValue<string>(this.Prop_Port, nameof(Prop_Port), sd)) ?? 995,
                    UseImplicitTls = !string.Equals(tlsMode, "StartTls", StringComparison.OrdinalIgnoreCase)
                                     && !string.Equals(tlsMode, "None", StringComparison.OrdinalIgnoreCase),
                    UseStartTls = string.Equals(tlsMode, "StartTls", StringComparison.OrdinalIgnoreCase),
                    UserName = userName,
                    Password = password,
                    TimeoutSeconds = timeout <= 0 ? 60 : timeout
                };

                var filter = new Pop3Filter
                {
                    SubjectContains = subjectContains,
                    FromContains = fromContains,
                    Since = logic.ParseSince(since),
                    MaxCount = maxCount < 0 ? 0 : maxCount
                };

                logic.ValidateSettings(settings);

                // Сетевая выборка (MailKit)
                var fetch = Fetch(settings, filter, this.Prop_DeleteAfterDownload, attachmentsDir, logic);

                if (!string.IsNullOrWhiteSpace(this.Prop_Messages) && fetch.Table != null)
                    SetVariableValue(this.Prop_Messages, fetch.Table, sd);
                if (!string.IsNullOrWhiteSpace(this.Prop_Count))
                    SetVariableValue(this.Prop_Count, fetch.Messages.Count, sd);
                if (!string.IsNullOrWhiteSpace(this.Prop_BodyOut) && fetch.LastBody != null)
                    SetVariableValue(this.Prop_BodyOut, fetch.LastBody, sd);
                if (!string.IsNullOrWhiteSpace(this.Prop_AttachmentsOut) && fetch.SavedAttachments.Count > 0)
                    SetVariableValue(this.Prop_AttachmentsOut, string.Join(";", fetch.SavedAttachments), sd);

                // Тело последнего письма в файл (опционально)
                if (!string.IsNullOrWhiteSpace(bodyToFile) && fetch.LastBody != null)
                    File.WriteAllText(bodyToFile, fetch.LastBody, System.Text.Encoding.UTF8);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Почта] Получено писем (POP3): {fetch.Messages.Count}"
                        + (this.Prop_DeleteAfterDownload ? ", удалено с сервера" : string.Empty)
                        + (fetch.SavedAttachments.Count > 0 ? $", вложений: {fetch.SavedAttachments.Count}" : string.Empty)
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка POP3: {ex.Message}"
                };
            }
        }

        /// <summary>Результат выборки для выводов активности.</summary>
        private class FetchResult
        {
            public IList<MimeMessage> Messages = new List<MimeMessage>();
            public DataTable Table;
            public string LastBody = string.Empty;
            public List<string> SavedAttachments = new List<string>();
        }

        /// <summary>Письмо с индексом и UIDL-идентификатором.</summary>
        private class Pop3Item
        {
            public int Index;
            public MimeMessage Message;
            public string Uid;
        }

        /// <summary>
        /// Сетевая выборка (MailKit). POP3 отдаёт только содержимое ящика
        /// целиком: загрузка всех писем → клиентская фильтрация → сортировка
        /// «от новых к старым» → MaxCount. Вложения — по подпапке на письмо.
        /// Удаление коммитится при Disconnect(true) после успешной загрузки.
        /// </summary>
        private FetchResult Fetch(
            Pop3ConnectionSettings settings,
            Pop3Filter filter,
            bool deleteAfter,
            string attachmentsDir,
            Pop3Logic logic)
        {
            var result = new FetchResult();

            using (var client = new Pop3Client())
            {
                client.Timeout = settings.TimeoutSeconds * 1000;
                var options = logic.ResolveSocketOptions(settings);
                client.Connect(settings.Host, settings.Port, options);

                try
                {
                    if (!string.IsNullOrWhiteSpace(settings.UserName))
                        client.Authenticate(settings.UserName, settings.Password);

                    var uids = client.GetMessageUids();
                    var items = new List<Pop3Item>();
                    for (var i = 0; i < client.Count; i++)
                    {
                        items.Add(new Pop3Item
                        {
                            Index = i,
                            Message = client.GetMessage(i),
                            Uid = i < uids.Count ? uids[i] : string.Empty
                        });
                    }

                    // Клиентская фильтрация + сортировка «от новых к старым»
                    items = items
                        .Where(x => logic.MatchesFilter(x.Message, filter))
                        .OrderByDescending(x => x.Message.Date.DateTime)
                        .ToList();
                    if (filter.MaxCount > 0)
                        items = items.Take(filter.MaxCount).ToList();

                    var messages = items.Select(x => x.Message).ToList();
                    var itemUids = items.Select(x => x.Uid).ToList();
                    result.Messages = messages;

                    // Вложения: подпапка на письмо «Письмо_N» внутри папки назначения
                    if (!string.IsNullOrWhiteSpace(attachmentsDir))
                    {
                        for (var i = 0; i < items.Count; i++)
                        {
                            var saved = MailCommon.SaveAttachments(
                                items[i].Message, Path.Combine(attachmentsDir, $"Письмо_{i + 1}"));
                            result.SavedAttachments.AddRange(saved);
                        }
                    }

                    result.Table = logic.BuildMessagesTable(
                        messages, itemUids, m => MailCommon.GetBodyText(m));

                    result.LastBody = messages.Count > 0
                        ? MailCommon.GetBodyText(messages[messages.Count - 1])
                        : string.Empty;

                    if (deleteAfter)
                    {
                        foreach (var item in items)
                            client.DeleteMessage(item.Index);
                    }
                }
                finally
                {
                    // Quit=true коммитит удаления; без deleteAfter — не коммитим
                    var quit = deleteAfter;
                    SafeDispose.Quiet(
                        () => client.Disconnect(quit), "POP3 disconnect");
                }
            }

            return result;
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Host, ActivityStrings.Field_MailHost, ActivityStrings.Error_MailPop3HostRequired);
            return ret;
        }
    }
}
