using System;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using MimeKit;
using Xunit;

namespace Primo.MIA.Tests.Mail
{
    /// <summary>
    /// Тесты разбора EML (issue #37, фаза 2, активность «Почта: Разобрать EML»):
    /// полный цикл — построение MimeMessage → запись в .eml → загрузка через
    /// ImapLogic.LoadEml → поля письма и вложения.
    /// </summary>
    [Trait("Category", "Fast")]
    public class EmlParseLogicTests : IDisposable
    {
        private readonly ImapLogic _logic = new ImapLogic();
        private readonly string _workDir = Path.Combine(
            Path.GetTempPath(), "mia-eml-tests-" + Guid.NewGuid().ToString("N"));

        public EmlParseLogicTests()
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

        /// <summary>Пишет письмо в .eml-файл и возвращает путь.</summary>
        private string WriteEml(MimeMessage message)
        {
            var path = Path.Combine(_workDir, Guid.NewGuid().ToString("N") + ".eml");
            using (var stream = File.Create(path))
                message.WriteTo(stream);
            return path;
        }

        [Fact(DisplayName = "EML round-trip: тема, от, кому, дата")]
        public void LoadEml_RoundTrip_Headers()
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse("Ирина <irina@corp.ru>"));
            message.To.Add(MailboxAddress.Parse("robot@corp.ru"));
            message.To.Add(MailboxAddress.Parse("audit@corp.ru"));
            message.Subject = "Акт сверки за ноябрь";
            message.Date = new DateTimeOffset(2026, 11, 10, 8, 45, 0, TimeSpan.FromHours(3));
            message.Body = new BodyBuilder { TextBody = "Во вложении акт." }.ToMessageBody();

            var loaded = _logic.LoadEml(WriteEml(message));

            loaded.Subject.Should().Be("Акт сверки за ноябрь");
            loaded.From.ToString().Should().Contain("irina@corp.ru");
            loaded.To.Count.Should().Be(2);
            loaded.To.Select(a => a.ToString())
                .Should().Contain("robot@corp.ru").And.Contain("audit@corp.ru");
            loaded.Date.DateTime.Should().Be(new DateTime(2026, 11, 10, 8, 45, 0));
            _logic.GetBodyText(loaded).Should().Be("Во вложении акт.");
        }

        [Fact(DisplayName = "EML round-trip: вложение сохраняется на диск")]
        public void LoadEml_RoundTrip_Attachment()
        {
            var contentPath = Path.Combine(_workDir, "акт.pdf");
            File.WriteAllBytes(contentPath, new byte[] { 9, 8, 7 });

            var message = new MimeMessage { Subject = "с вложением" };
            var builder = new BodyBuilder { TextBody = "см. вложение" };
            builder.Attachments.Add(contentPath);
            message.Body = builder.ToMessageBody();

            var loaded = _logic.LoadEml(WriteEml(message));
            var outDir = Path.Combine(_workDir, "out");
            var saved = _logic.SaveAttachments(loaded, outDir);

            loaded.Attachments.Should().HaveCount(1);
            saved.Should().HaveCount(1);
            File.ReadAllBytes(saved[0]).Should().Equal(new byte[] { 9, 8, 7 });
        }

        [Fact(DisplayName = "EML: HTML-письмо без plain — тело из HTML")]
        public void LoadEml_HtmlOnly()
        {
            var message = new MimeMessage { Subject = "html" };
            message.Body = new BodyBuilder { HtmlBody = "<b>жирно</b>" }.ToMessageBody();

            var loaded = _logic.LoadEml(WriteEml(message));
            _logic.GetBodyText(loaded).Should().Be("<b>жирно</b>");
        }

        [Fact(DisplayName = "EML: тело пишется в файл (UTF-8)")]
        public void LoadEml_BodyToFile()
        {
            var message = new MimeMessage { Subject = "файл" };
            message.Body = new BodyBuilder { TextBody = "кириллица ✓" }.ToMessageBody();

            var loaded = _logic.LoadEml(WriteEml(message));
            var bodyPath = Path.Combine(_workDir, "body.txt");
            var body = _logic.GetBodyText(loaded);
            System.IO.File.WriteAllText(bodyPath, body, Encoding.UTF8);

            System.IO.File.ReadAllText(bodyPath).Should().Be("кириллица ✓");
        }
    }
}
