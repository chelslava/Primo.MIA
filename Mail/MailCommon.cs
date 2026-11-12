// =============================================================================
// MailCommon.cs — общая чистая логика Mail-модуля (issue #37, фазы 2–3).
//
// Используется ImapLogic и Pop3Logic: разбор даты «С даты», извлечение тела,
// сохранение вложений. Сетевые вызовы (MailKit) остаются в активностях —
// unit-тесты покрывают логику без сети.
// =============================================================================

using MimeKit;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>Общие чистые операции Mail-модуля (IMAP/POP3).</summary>
    public static class MailCommon
    {
        /// <summary>
        /// Разбирает строку даты «С даты»: ГГГГ-ММ-ДД, ДД.ММ.ГГГГ (+ время).
        /// null/пустая — без ограничения (null).
        /// </summary>
        public static DateTime? ParseDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = raw.Trim().Trim('"');
            var formats = new[]
            {
                "yyyy-MM-dd", "dd.MM.yyyy", "dd/MM/yyyy",
                "yyyy-MM-dd HH:mm", "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy HH:mm"
            };
            if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var exact))
                return exact;
            if (DateTime.TryParse(raw, CultureInfo.GetCultureInfo("ru-RU"),
                    DateTimeStyles.None, out var parsed))
                return parsed;

            throw new FormatException(
                $"Не удалось распознать дату «{raw}». Ожидается ГГГГ-ММ-ДД или ДД.ММ.ГГГГ");
        }

        /// <summary>
        /// Извлекает текст тела: приоритет у plain-части, при её отсутствии — HTML.
        /// Хвостовые переводы строк (стандартный хвост MIME-частей) отсекаются.
        /// </summary>
        public static string GetBodyText(MimeMessage message)
        {
            Guard.NotNull(message, nameof(message));

            if (!string.IsNullOrEmpty(message.TextBody))
                return message.TextBody.TrimEnd();
            if (!string.IsNullOrEmpty(message.HtmlBody))
                return message.HtmlBody.TrimEnd();
            return string.Empty;
        }

        /// <summary>
        /// Сохраняет вложения письма в папку (создаётся при отсутствии),
        /// возвращает полные пути. Имя файла дезинфицируется (без каталогов).
        /// </summary>
        public static List<string> SaveAttachments(MimeMessage message, string directory)
        {
            Guard.NotNull(message, nameof(message));
            Guard.NotNullOrWhiteSpace(directory, nameof(directory));

            Directory.CreateDirectory(directory);
            var saved = new List<string>();
            foreach (var part in message.Attachments.OfType<MimePart>())
            {
                var fileName = Path.GetFileName(part.FileName);
                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = $"attachment-{saved.Count + 1}";

                var path = Path.Combine(directory, fileName);
                using (var stream = File.Create(path))
                    part.Content.DecodeTo(stream);
                saved.Add(path);
            }
            return saved;
        }
    }
}
