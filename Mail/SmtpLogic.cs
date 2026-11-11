// =============================================================================
// SmtpLogic.cs — чистая логика SMTP-отправки (issue #37, фаза 1).
//
// Чистые части: разбор получателей/вложений, построение MimeMessage.
// Сетевая отправка (MailKit SmtpClient) вызывается из активности —
// unit-тесты покрывают построение сообщения без сети.
// =============================================================================

using MailKit;
using MailKit.Security;
using MimeKit;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>Настройки SMTP-сервера.</summary>
    public class SmtpConnectionSettings
    {
        public string Host { get; set; }
        public int Port { get; set; } = 587;

        /// <summary>TLS: true = STARTTLS (обычный 587/25); false = без шифрования.</summary>
        public bool UseStartTls { get; set; } = true;

        /// <summary>Неявный TLS (amtps, 465) — приоритетнее StartTls.</summary>
        public bool UseImplicitTls { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }
        public int TimeoutSeconds { get; set; } = 60;
    }

    /// <summary>
    /// Логика SMTP-отправки: разбор адресов/вложений, построение письма.
    /// </summary>
    public class SmtpLogic
    {
        /// <summary>
        /// Разбирает список адресов: разделители «;» и «,», трим, без пустых.
        /// </summary>
        public List<string> ParseAddresses(string raw)
        {
            return (raw ?? string.Empty)
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }

        /// <summary>
        /// Разбирает список путей вложений (через «;»), проверяет существование.
        /// </summary>
        public List<string> ParseAttachments(string raw)
        {
            var paths = (raw ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            foreach (var path in paths)
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Файл вложения не найден: {path}", path);
            }

            return paths;
        }

        /// <summary>
        /// Валидирует настройки: хост и From обязательны.
        /// </summary>
        public void ValidateSettings(SmtpConnectionSettings settings, string from)
        {
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNullOrWhiteSpace(settings.Host, nameof(settings.Host));
            Guard.Positive(settings.Port, nameof(settings.Port));
            Guard.NotNullOrWhiteSpace(from, nameof(from));
        }

        /// <summary>
        /// Строит письмо: From/To/Cc, тема, тело (plain/HTML), вложения.
        /// Body.From... для MimeMessage — без сети, полностью тестируемо.
        /// </summary>
        public MimeMessage BuildMessage(
            string from,
            List<string> to,
            List<string> cc,
            string subject,
            string body,
            bool bodyIsHtml,
            List<string> attachments)
        {
            Guard.NotNullOrWhiteSpace(from, nameof(from));
            Guard.NotNull(to, nameof(to));
            Guard.That(to.Count > 0, "Не указан ни один получатель (To)");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            foreach (var t in to)
                message.To.Add(MailboxAddress.Parse(t));
            if (cc != null)
                foreach (var c in cc.Where(s => !string.IsNullOrWhiteSpace(s)))
                    message.Cc.Add(MailboxAddress.Parse(c));

            message.Subject = subject ?? string.Empty;

            var builder = new BodyBuilder
            {
                TextBody = bodyIsHtml ? null : (body ?? string.Empty),
                HtmlBody = bodyIsHtml ? (body ?? string.Empty) : null
            };

            foreach (var path in attachments ?? new List<string>())
                builder.Attachments.Add(path);

            message.Body = builder.ToMessageBody();
            return message;
        }

        /// <summary>
        /// Требуемый SecureSocketOptions из настроек (для SmtpClient.Connect).
        /// </summary>
        public SecureSocketOptions ResolveSocketOptions(SmtpConnectionSettings settings)
        {
            if (settings.UseImplicitTls)
                return SecureSocketOptions.SslOnConnect;
            if (settings.UseStartTls)
                return SecureSocketOptions.StartTls;
            return SecureSocketOptions.None;
        }
    }
}
