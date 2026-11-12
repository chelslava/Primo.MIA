// =============================================================================
// Pop3Logic.cs — чистая логика выборки писем по POP3 (issue #37, фаза 3).
//
// Чистые части: валидация настроек, TLS-опции, клиентская фильтрация,
// DataTable-сборка. Сетевые вызовы (MailKit Pop3Client) остаются в активности.
// POP3 не поддерживает серверный поиск и флаги «прочитано» — фильтры
// применяются на клиенте, уникальность писем отслеживается по UIDL.
// =============================================================================

using MailKit.Security;
using MimeKit;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;

namespace Primo.MIA
{
    /// <summary>Настройки POP3-сервера.</summary>
    public class Pop3ConnectionSettings
    {
        public string Host { get; set; }
        public int Port { get; set; } = 995;

        /// <summary>Неявный TLS (pop3s, 995) — приоритетнее StartTls.</summary>
        public bool UseImplicitTls { get; set; } = true;

        /// <summary>STARTTLS (обычный 110).</summary>
        public bool UseStartTls { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }
        public int TimeoutSeconds { get; set; } = 60;
    }

    /// <summary>
    /// Фильтры выборки писем. Обрабатываются клиентски: POP3 умеет отдать
    /// только всё содержимое ящика (серверный поиск отсутствует).
    /// </summary>
    public class Pop3Filter
    {
        /// <summary>Подстрока в теме (без учёта регистра).</summary>
        public string SubjectContains { get; set; }

        /// <summary>Подстрока в отправителе (без учёта регистра).</summary>
        public string FromContains { get; set; }

        /// <summary>Письма, доставленные с указанной даты (включительно); null — без ограничения.</summary>
        public DateTime? Since { get; set; }

        /// <summary>Максимум писем в выдаче (0 — без ограничения).</summary>
        public int MaxCount { get; set; }
    }

    /// <summary>
    /// Логика POP3-выборки: клиентский фильтр, тело письма, DataTable.
    /// Тело и вложения — общие операции MailCommon.
    /// </summary>
    public class Pop3Logic
    {
        /// <summary>
        /// Валидирует настройки: хост обязателен.
        /// </summary>
        public void ValidateSettings(Pop3ConnectionSettings settings)
        {
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNullOrWhiteSpace(settings.Host, nameof(settings.Host));
            Guard.Positive(settings.Port, nameof(settings.Port));
        }

        /// <summary>
        /// Требуемый SecureSocketOptions из настроек (для Pop3Client.Connect).
        /// </summary>
        public SecureSocketOptions ResolveSocketOptions(Pop3ConnectionSettings settings)
        {
            if (settings.UseImplicitTls)
                return SecureSocketOptions.SslOnConnect;
            if (settings.UseStartTls)
                return SecureSocketOptions.StartTls;
            return SecureSocketOptions.None;
        }

        /// <summary>
        /// Разбирает строку даты «С даты» — общая логика MailCommon (IMAP/POP3).
        /// </summary>
        public DateTime? ParseSince(string raw)
        {
            return MailCommon.ParseDate(raw);
        }

        /// <summary>
        /// Клиентский фильтр письма: подстроки темы/отправителя (без учёта
        /// регистра) и дата «с» включительно. Пустые фильтры — письмо проходит.
        /// Письмо без заголовка Date трактуется как свежее: MimeKit подставляет
        /// текущее время (Date == DateTime.Now), и оно проходит фильтр «с».
        /// </summary>
        public bool MatchesFilter(MimeMessage message, Pop3Filter filter)
        {
            Guard.NotNull(message, nameof(message));
            Guard.NotNull(filter, nameof(filter));

            if (!string.IsNullOrWhiteSpace(filter.SubjectContains))
            {
                var subject = message.Subject ?? string.Empty;
                if (subject.IndexOf(filter.SubjectContains.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(filter.FromContains)
                && message.From.ToString().IndexOf(filter.FromContains.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            if (filter.Since.HasValue && message.Date.DateTime < filter.Since.Value)
                return false;

            return true;
        }

        /// <summary>
        /// Собирает DataTable со сводкой писем: От, Тема, Дата, UID, Тело.
        /// UID — уникальный идентификатор письма (UIDL), остаётся постоянным
        /// между сессиями — база для дедупликации при периодическом опросе
        /// (флагов «прочитано» в POP3 нет). Число UID обязано совпадать
        /// с числом писем (индексное соответствие). Дата всегда заполнена:
        /// при отсутствии заголовка Date MimeKit подставляет текущее время.
        /// </summary>
        public DataTable BuildMessagesTable(
            IList<MimeMessage> messages,
            IList<string> uids,
            Func<MimeMessage, string> bodyOf)
        {
            Guard.NotNull(messages, nameof(messages));
            Guard.NotNull(uids, nameof(uids));
            Guard.NotNull(bodyOf, nameof(bodyOf));
            Guard.That(uids.Count == messages.Count,
                "Число UID не совпадает с числом писем", nameof(uids));

            var table = new DataTable("Сообщения");
            table.Columns.Add("От", typeof(string));
            table.Columns.Add("Тема", typeof(string));
            table.Columns.Add("Дата", typeof(DateTime));
            table.Columns.Add("UID", typeof(string));
            table.Columns.Add("Тело", typeof(string));

            for (var i = 0; i < messages.Count; i++)
            {
                var message = messages[i];
                table.Rows.Add(
                    message.From.ToString(),
                    message.Subject ?? string.Empty,
                    message.Date.DateTime,
                    uids[i] ?? string.Empty,
                    bodyOf(message));
            }
            return table;
        }
    }
}
