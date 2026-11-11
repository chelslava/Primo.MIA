using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using MailKit.Security;
using Xunit;

namespace Primo.MIA.Tests.Mail
{
    /// <summary>
    /// Тесты SmtpLogic (issue #37, фаза 1): разбор адресов/вложений,
    /// построение письма (без сети), TLS-опции, Guard.
    /// Сетевая отправка — кандидат на Trait(Integration).
    /// </summary>
    [Trait("Category", "Fast")]
    public class SmtpLogicTests : IDisposable
    {
        private readonly SmtpLogic _logic = new SmtpLogic();
        private readonly string _workDir = Path.Combine(
            Path.GetTempPath(), "mia-smtp-tests-" + Guid.NewGuid().ToString("N"));

        public SmtpLogicTests()
        {
            Directory.CreateDirectory(_workDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_workDir))
                    Directory.Delete(_workDir, recursive: true);
            }
            catch
            {
                // временная папка может быть занята — не роняем уборкой
            }
        }

        // ── ParseAddresses ─────────────────────────────────────────────

        [Fact(DisplayName = "Адреса: разделители ; и , дублируются, пустые отсекаются")]
        public void ParseAddresses_Mixed()
        {
            var result = _logic.ParseAddresses("a@mail.ru; b@corp.ru, c@org.net ,  ;");
            result.Should().BeEquivalentTo("a@mail.ru", "b@corp.ru", "c@org.net");
        }

        [Fact(DisplayName = "Адреса: null/пустая строка — пустой список")]
        public void ParseAddresses_Empty()
        {
            _logic.ParseAddresses(null).Should().BeEmpty();
            _logic.ParseAddresses("").Should().BeEmpty();
        }

        // ── ParseAttachments ───────────────────────────────────────────

        [Fact(DisplayName = "Вложения: существующие пути проходят")]
        public void ParseAttachments_Existing()
        {
            var path = Path.Combine(_workDir, "attach.xlsx");
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });

            var result = _logic.ParseAttachments($"{path} ; ");
            result.Should().ContainSingle(p => p == path);
        }

        [Fact(DisplayName = "Вложения: несуществующий файл — FileNotFoundException")]
        public void ParseAttachments_Missing_Throws()
        {
            var missing = Path.Combine(_workDir, "missing.pdf");
            Action act = () => _logic.ParseAttachments($"{missing}");
            act.Should().Throw<FileNotFoundException>().WithMessage("*вложения*");
        }

        // ── BuildMessage ───────────────────────────────────────────────

        [Fact(DisplayName = "Письмо: From/To/тема/plain-тело")]
        public void BuildMessage_Plain()
        {
            var message = _logic.BuildMessage("robot@corp.ru", new System.Collections.Generic.List<string> { "a@mail.ru", "b@mail.ru" },
                null, "Итоги дня", "Привет из робота", bodyIsHtml: false, attachments: null);

            message.Subject.Should().Be("Итоги дня");
            message.To.Count.Should().Be(2);
            message.From.Count.Should().Be(1);
            message.Body.Should().NotBeNull();
        }

        [Fact(DisplayName = "Письмо: HTML-тело + Cc")]
        public void BuildMessage_Html_WithCc()
        {
            var message = _logic.BuildMessage("robot@corp.ru", new System.Collections.Generic.List<string> { "a@mail.ru" },
                new System.Collections.Generic.List<string> { "boss@corp.ru" },
                "Отчёт", "<b>Жирный</b>", bodyIsHtml: true, attachments: null);

            message.Cc.Count.Should().Be(1);
            message.HtmlBody.Should().Contain("<b>Жирный</b>");
            message.TextBody.Should().BeNull("HTML-режим — plain не задаётся");
        }

        [Fact(DisplayName = "Письмо: вложения попадают в сообщение")]
        public void BuildMessage_WithAttachments()
        {
            var path = Path.Combine(_workDir, "doc.pdf");
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });

            var message = _logic.BuildMessage("robot@corp.ru", new System.Collections.Generic.List<string> { "a@mail.ru" },
                null, "С вложением", "тело", false, new System.Collections.Generic.List<string> { path });

            message.Attachments.Count().Should().Be(1);
        }

        [Fact(DisplayName = "Письмо: без получателей — InvalidOperationException")]
        public void BuildMessage_NoRecipients_Throws()
        {
            Action act = () => _logic.BuildMessage("robot@corp.ru", new System.Collections.Generic.List<string>(),
                null, "тема", "тело", false, null);
            act.Should().Throw<ArgumentException>().WithMessage("*получатель*");
        }

        [Fact(DisplayName = "Письмо: пустой From — ArgumentException (Guard)")]
        public void BuildMessage_EmptyFrom_Throws()
        {
            Action act = () => _logic.BuildMessage("", new System.Collections.Generic.List<string> { "a@mail.ru" },
                null, "тема", "тело", false, null);
            act.Should().Throw<ArgumentException>();
        }

        // ── TLS ────────────────────────────────────────────────────────

        [Theory(DisplayName = "TLS: маппинг режимов в SecureSocketOptions")]
        [InlineData(true, false, SecureSocketOptions.SslOnConnect)]
        [InlineData(false, true, SecureSocketOptions.StartTls)]
        [InlineData(false, false, SecureSocketOptions.None)]
        public void ResolveSocketOptions_Mapping(bool implicitTls, bool startTls, SecureSocketOptions expected)
        {
            var result = _logic.ResolveSocketOptions(new SmtpConnectionSettings
            {
                UseImplicitTls = implicitTls,
                UseStartTls = startTls
            });
            result.Should().Be(expected);
        }

        // ── ValidateSettings ───────────────────────────────────────────

        [Fact(DisplayName = "Настройки: хост/From обязательны, порт положительный")]
        public void ValidateSettings_Guard()
        {
            Action noHost = () => _logic.ValidateSettings(new SmtpConnectionSettings { Host = "" }, "a@b.ru");
            Action noFrom = () => _logic.ValidateSettings(new SmtpConnectionSettings { Host = "smtp.ru" }, "");
            Action badPort = () => _logic.ValidateSettings(new SmtpConnectionSettings { Host = "smtp.ru", Port = -1 }, "a@b.ru");

            noHost.Should().Throw<ArgumentException>();
            noFrom.Should().Throw<ArgumentException>();
            badPort.Should().Throw<ArgumentException>();
        }
    }
}
