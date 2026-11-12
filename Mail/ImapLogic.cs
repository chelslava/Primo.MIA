// =============================================================================
// ImapLogic.cs — чистая логика выборки писем по IMAP (issue #37, фаза 2).
//
// Чистые части: построение поискового запроса, разбор даты, извлечение тела,
// сохранение вложений, DataTable-сборка. Сетевые вызовы (MailKit ImapClient)
// остаются в активности — unit-тесты покрывают логику без сети.
// =============================================================================

using MailKit;
using MailKit.Search;
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
    /// <summary>Настройки IMAP-сервера.</summary>
    public class ImapConnectionSettings
    {
        public string Host { get; set; }
        public int Port { get; set; } = 993;

        /// <summary>Неявный TLS (imaps, 993) — приоритетнее StartTls.</summary>
        public bool UseImplicitTls { get; set; } = true;

        /// <summary>STARTTLS (обычный 143).</summary>
        public bool UseStartTls { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }
        public int TimeoutSeconds { get; set; } = 60;
    }

    /// <summary>Фильтры выборки писем.</summary>
    public class ImapFilter
    {
        /// <summary>Подстрока в теме (без учёта регистра).</summary>
        public string SubjectContains { get; set; }

        /// <summary>Подстрока в отправителе (без учёта регистра).</summary>
        public string FromContains { get; set; }

        /// <summary>Письма, доставленные с указанной даты (включительно); null — без ограничения.</summary>
        public DateTime? Since { get; set; }

        /// <summary>Только непрочитанные.</summary>
        public bool OnlyUnseen { get; set; }

        /// <summary>Максимум писем в выдаче (0 — без ограничения).</summary>
        public int MaxCount { get; set; }
    }

    /// <summary>
    /// Логика IMAP-выборки: поисковый запрос, тело письма, вложения, DataTable.
    /// </summary>
    public class ImapLogic
    {
        /// <summary>
        /// Валидирует настройки: хост обязателен.
        /// </summary>
        public void ValidateSettings(ImapConnectionSettings settings)
        {
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNullOrWhiteSpace(settings.Host, nameof(settings.Host));
            Guard.Positive(settings.Port, nameof(settings.Port));
        }

        /// <summary>
        /// Строит серверный поисковый запрос MailKit из фильтров.
        /// Тема/отправитель — серверный substring-поиск (HEADER), дата — SINCE,
        /// непрочитанные — UNSEEN.
        /// </summary>
        public SearchQuery BuildSearchQuery(ImapFilter filter)
        {
            Guard.NotNull(filter, nameof(filter));

            var query = SearchQuery.All;
            if (!string.IsNullOrWhiteSpace(filter.SubjectContains))
                query = query.And(SearchQuery.SubjectContains(filter.SubjectContains));
            if (!string.IsNullOrWhiteSpace(filter.FromContains))
                query = query.And(SearchQuery.FromContains(filter.FromContains));
            if (filter.Since.HasValue)
                query = query.And(SearchQuery.DeliveredAfter(filter.Since.Value));
            if (filter.OnlyUnseen)
                query = query.And(SearchQuery.NotSeen);
            return query;
        }

        /// <summary>
        /// Разбирает строку даты «С даты» — общая логика MailCommon (IMAP/POP3).
        /// </summary>
        public DateTime? ParseSince(string raw)
        {
            return MailCommon.ParseDate(raw);
        }

        /// <summary>
        /// Требуемый SecureSocketOptions из настроек (для ImapClient.Connect).
        /// </summary>
        public SecureSocketOptions ResolveSocketOptions(ImapConnectionSettings settings)
        {
            if (settings.UseImplicitTls)
                return SecureSocketOptions.SslOnConnect;
            if (settings.UseStartTls)
                return SecureSocketOptions.StartTls;
            return SecureSocketOptions.None;
        }

        /// <summary>Тело письма — общая логика MailCommon (IMAP/POP3).</summary>
        public string GetBodyText(MimeMessage message)
        {
            return MailCommon.GetBodyText(message);
        }

        /// <summary>Сохранение вложений — общая логика MailCommon (IMAP/POP3).</summary>
        public List<string> SaveAttachments(MimeMessage message, string directory)
        {
            return MailCommon.SaveAttachments(message, directory);
        }

        /// <summary>
        /// Собирает DataTable со сводкой писем: От, Тема, Дата, Непрочитано,
        /// Вложения, Тело. unseenOf/bodyOf/attachmentsOf — колбэки активности
        /// (флаг IMAP приходит через MessageSummary, тело — из MimeMessage).
        /// </summary>
        public DataTable BuildMessagesTable(
            IList<MimeMessage> messages,
            Func<MimeMessage, bool> unseenOf,
            Func<MimeMessage, string> bodyOf,
            Func<MimeMessage, string> attachmentsOf)
        {
            Guard.NotNull(messages, nameof(messages));
            Guard.NotNull(unseenOf, nameof(unseenOf));
            Guard.NotNull(bodyOf, nameof(bodyOf));
            Guard.NotNull(attachmentsOf, nameof(attachmentsOf));

            var table = new DataTable("Сообщения");
            table.Columns.Add("От", typeof(string));
            table.Columns.Add("Тема", typeof(string));
            table.Columns.Add("Дата", typeof(DateTime));
            table.Columns.Add("Непрочитано", typeof(bool));
            table.Columns.Add("Вложения", typeof(string));
            table.Columns.Add("Тело", typeof(string));

            foreach (var message in messages)
            {
                table.Rows.Add(
                    message.From.ToString(),
                    message.Subject ?? string.Empty,
                    message.Date.DateTime == DateTime.MinValue
                        ? (object)DBNull.Value
                        : message.Date.DateTime,
                    unseenOf(message),
                    attachmentsOf(message),
                    bodyOf(message));
            }
            return table;
        }

        /// <summary>
        /// Загружает EML-файл (или поток) в MimeMessage — общий для
        /// «Почта: Разобрать EML» и отладки.
        /// </summary>
        public MimeMessage LoadEml(string filePath)
        {
            Guard.NotNullOrWhiteSpace(filePath, nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"EML-файл не найден: {filePath}", filePath);
            return LoadEml(File.OpenRead(filePath), closeStream: true);
        }

        /// <summary>Загружает EML из потока.</summary>
        public MimeMessage LoadEml(Stream stream, bool closeStream = false)
        {
            Guard.NotNull(stream, nameof(stream));
            try
            {
                return MimeMessage.Load(stream);
            }
            finally
            {
                if (closeStream)
                    SafeDispose.Quiet(() => stream.Dispose(), "EML stream");
            }
        }
    }
}
