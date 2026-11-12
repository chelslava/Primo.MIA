using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using FluentAssertions;
using MailKit;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using Xunit;

namespace Primo.MIA.Tests.Mail
{
    /// <summary>
    /// Тесты ImapLogic (issue #37, фаза 2): поисковый запрос, разбор даты,
    /// TLS-опции, тело письма, сохранение вложений, DataTable, загрузка EML.
    /// Сетевая часть (ImapClient) — кандидат на Trait(Integration).
    /// </summary>
    [Trait("Category", "Fast")]
    public class ImapLogicTests : IDisposable
    {
        private readonly ImapLogic _logic = new ImapLogic();
        private readonly string _workDir = Path.Combine(
            Path.GetTempPath(), "mia-imap-tests-" + Guid.NewGuid().ToString("N"));

        public ImapLogicTests()
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

        private ImapConnectionSettings Settings(
            bool implicitTls = true, bool startTls = false)
            => new ImapConnectionSettings
            {
                Host = "imap.test.ru",
                UseImplicitTls = implicitTls,
                UseStartTls = startTls
            };

        #region BuildSearchQuery

        /// <summary>
        /// Обходит дерево поискового запроса MailKit (AND-узлы Left/Right,
        /// NOT-узлы Operand) и собирает все узлы плоским списком.
        /// </summary>
        private static void CollectNodes(SearchQuery query, System.Collections.Generic.List<SearchQuery> nodes)
        {
            nodes.Add(query);
            switch (query)
            {
                case BinarySearchQuery binary:
                    CollectNodes(binary.Left, nodes);
                    CollectNodes(binary.Right, nodes);
                    break;
                case UnarySearchQuery unary:
                    CollectNodes(unary.Operand, nodes);
                    break;
            }
        }

        private static System.Collections.Generic.List<SearchQuery> NodesOf(SearchQuery query)
        {
            var nodes = new System.Collections.Generic.List<SearchQuery>();
            CollectNodes(query, nodes);
            return nodes;
        }

        [Fact(DisplayName = "Запрос: пустой фильтр — ALL")]
        public void BuildSearchQuery_EmptyFilter()
        {
            var query = _logic.BuildSearchQuery(new ImapFilter());
            query.Should().BeSameAs(SearchQuery.All);
        }

        [Fact(DisplayName = "Запрос: тема/отправитель/дата/непрочитанные попадают в запрос")]
        public void BuildSearchQuery_Components()
        {
            // SubjectContains/FromContents в MailKit — TextSearchQuery с Term,
            // дата — DateSearchQuery, непрочитанные — узел SearchTerm.NotSeen
            var nodes = NodesOf(_logic.BuildSearchQuery(new ImapFilter
            {
                SubjectContains = "акт сверки"
            }));
            nodes.OfType<TextSearchQuery>().Should().Contain(t =>
                t.Text == "акт сверки" && t.Term == SearchTerm.SubjectContains);

            nodes = NodesOf(_logic.BuildSearchQuery(new ImapFilter
            {
                FromContains = "billing@corp.ru"
            }));
            nodes.OfType<TextSearchQuery>().Should().Contain(t =>
                t.Text == "billing@corp.ru" && t.Term == SearchTerm.FromContains);

            nodes = NodesOf(_logic.BuildSearchQuery(new ImapFilter
            {
                Since = new DateTime(2026, 11, 1)
            }));
            nodes.OfType<DateSearchQuery>().Should().Contain(d => d.Date == new DateTime(2026, 11, 1));

            nodes = NodesOf(_logic.BuildSearchQuery(new ImapFilter { OnlyUnseen = true }));
            nodes.Should().Contain(n => n.Term == SearchTerm.NotSeen);
        }

        [Fact(DisplayName = "Запрос: комбинация фильтров объединяется через AND")]
        public void BuildSearchQuery_Combined()
        {
            var query = _logic.BuildSearchQuery(new ImapFilter
            {
                SubjectContains = "отчёт",
                FromContains = "boss@corp.ru",
                Since = new DateTime(2026, 11, 1),
                OnlyUnseen = true
            });

            query.Should().BeOfType<BinarySearchQuery>("4 условия объединяются AND-узлами");
            var nodes = NodesOf(query);
            nodes.OfType<TextSearchQuery>().Should().Contain(t =>
                t.Text == "отчёт" && t.Term == SearchTerm.SubjectContains);
            nodes.OfType<TextSearchQuery>().Should().Contain(t =>
                t.Text == "boss@corp.ru" && t.Term == SearchTerm.FromContains);
            nodes.OfType<DateSearchQuery>().Should().Contain(d => d.Date == new DateTime(2026, 11, 1));
            nodes.Should().Contain(n => n.Term == SearchTerm.NotSeen);
        }

        [Fact(DisplayName = "Запрос: null-фильтр — Guard")]
        public void BuildSearchQuery_NullFilter()
        {
            Action act = () => _logic.BuildSearchQuery(null);
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region ParseSince

        [Fact(DisplayName = "Дата «С даты»: null/пустая — без ограничения")]
        public void ParseSince_Empty()
        {
            _logic.ParseSince(null).Should().BeNull();
            _logic.ParseSince("").Should().BeNull();
            _logic.ParseSince("   ").Should().BeNull();
        }

        [Fact(DisplayName = "Дата «С даты»: форматы ГГГГ-ММ-ДД и ДД.ММ.ГГГГ")]
        public void ParseSince_Formats()
        {
            _logic.ParseSince("2026-11-01").Should().Be(new DateTime(2026, 11, 1));
            _logic.ParseSince("01.11.2026").Should().Be(new DateTime(2026, 11, 1));
            _logic.ParseSince("2026-11-01 09:30").Should().Be(new DateTime(2026, 11, 1, 9, 30, 0));
        }

        [Fact(DisplayName = "Дата «С даты»: мусор — FormatException")]
        public void ParseSince_Invalid()
        {
            Action act = () => _logic.ParseSince("вчера");
            act.Should().Throw<FormatException>();
        }

        #endregion

        #region ResolveSocketOptions

        [Fact(DisplayName = "TLS: ImplicitTls → SslOnConnect (по умолчанию)")]
        public void ResolveSocketOptions_Implicit()
        {
            _logic.ResolveSocketOptions(Settings()).Should().Be(SecureSocketOptions.SslOnConnect);
        }

        [Fact(DisplayName = "TLS: StartTls → StartTls")]
        public void ResolveSocketOptions_StartTls()
        {
            _logic.ResolveSocketOptions(Settings(implicitTls: false, startTls: true))
                .Should().Be(SecureSocketOptions.StartTls);
        }

        [Fact(DisplayName = "TLS: None → без шифрования")]
        public void ResolveSocketOptions_None()
        {
            _logic.ResolveSocketOptions(Settings(implicitTls: false, startTls: false))
                .Should().Be(SecureSocketOptions.None);
        }

        #endregion

        #region GetBodyText

        [Fact(DisplayName = "Тело: plain-часть приоритетнее")]
        public void GetBodyText_PlainPreferred()
        {
            var message = new MimeMessage
            {
                Body = new BodyBuilder
                {
                    TextBody = "Привет, это текст",
                    HtmlBody = "<p>Привет, это HTML</p>"
                }.ToMessageBody()
            };
            _logic.GetBodyText(message).Should().Be("Привет, это текст");
        }

        [Fact(DisplayName = "Тело: только HTML — возвращается HTML")]
        public void GetBodyText_HtmlFallback()
        {
            var message = new MimeMessage
            {
                Body = new BodyBuilder { HtmlBody = "<p>только HTML</p>" }.ToMessageBody()
            };
            _logic.GetBodyText(message).Should().Be("<p>только HTML</p>");
        }

        [Fact(DisplayName = "Тело: письмо без тела — пустая строка")]
        public void GetBodyText_NoBody()
        {
            _logic.GetBodyText(new MimeMessage()).Should().BeEmpty();
        }

        [Fact(DisplayName = "Тело: null — Guard")]
        public void GetBodyText_Null()
        {
            Action act = () => _logic.GetBodyText(null);
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region SaveAttachments

        [Fact(DisplayName = "Вложения: сохраняются в папку с исходным именем")]
        public void SaveAttachments_SavesFiles()
        {
            var contentPath = Path.Combine(_workDir, "source.bin");
            File.WriteAllBytes(contentPath, new byte[] { 1, 2, 3 });

            var targetDir = Path.Combine(_workDir, "out");
            var message = new MimeMessage();
            var builder = new BodyBuilder();
            builder.Attachments.Add(contentPath);
            message.Body = builder.ToMessageBody();

            var saved = _logic.SaveAttachments(message, targetDir);

            saved.Should().HaveCount(1);
            File.Exists(saved[0]).Should().BeTrue();
            Path.GetFileName(saved[0]).Should().Be("source.bin");
            File.ReadAllBytes(saved[0]).Should().Equal(new byte[] { 1, 2, 3 });
        }

        [Fact(DisplayName = "Вложения: имя без каталогов (дезинфекция)")]
        public void SaveAttachments_SanitizesName()
        {
            var contentPath = Path.Combine(_workDir, "safe.txt");
            File.WriteAllText(contentPath, "x");

            var message = new MimeMessage();
            var builder = new BodyBuilder();
            builder.Attachments.Add(contentPath);
            message.Body = builder.ToMessageBody();

            // Внутри EML имя части может содержать каталоги — вырезаем до имени
            var part = (MimePart)message.Attachments.First();
            part.FileName = "..\\..\\evil.txt";

            var targetDir = Path.Combine(_workDir, "out2");
            var saved = _logic.SaveAttachments(message, targetDir);

            saved.Should().HaveCount(1);
            Path.GetFileName(saved[0]).Should().Be("evil.txt");
            Path.GetDirectoryName(saved[0]).Should().Be(targetDir);
        }

        [Fact(DisplayName = "Вложения: пустая директория — Guard")]
        public void SaveAttachments_EmptyDir()
        {
            Action act = () => _logic.SaveAttachments(new MimeMessage(), "  ");
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region BuildMessagesTable

        [Fact(DisplayName = "Таблица: колонки и значения строк")]
        public void BuildMessagesTable_Populates()
        {
            var unseen = new MimeMessage();
            unseen.From.Add(MailboxAddress.Parse("Alice <alice@corp.ru>"));
            unseen.Subject = "Отчёт за ноябрь";
            unseen.Date = new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);
            unseen.Body = new BodyBuilder { TextBody = "текст" }.ToMessageBody();

            var seen = new MimeMessage();
            seen.From.Add(MailboxAddress.Parse("bob@corp.ru"));
            seen.Subject = "Re: Отчёт";

            // Флаг «прочитано» приходит из IMAP-сводок — колбэком
            var unseenBySubject = new[] { "Отчёт за ноябрь" };
            var table = _logic.BuildMessagesTable(
                new[] { unseen, seen },
                m => unseenBySubject.Contains(m.Subject),
                m => m.Subject == "Отчёт за ноябрь" ? "текст" : string.Empty,
                _ => "");

            table.Rows.Count.Should().Be(2);
            table.Columns.Should().Contain(c => c.ColumnName == "От");
            table.Columns.Should().Contain(c => c.ColumnName == "Тема");
            table.Columns.Should().Contain(c => c.ColumnName == "Дата");
            table.Columns.Should().Contain(c => c.ColumnName == "Непрочитано");
            table.Columns.Should().Contain(c => c.ColumnName == "Тело");

            table.Rows[0]["Тема"].Should().Be("Отчёт за ноябрь");
            table.Rows[0]["Дата"].Should().Be(new DateTime(2026, 11, 10, 12, 0, 0));
            table.Rows[0]["Непрочитано"].Should().Be(true);
            table.Rows[0]["Тело"].Should().Be("текст");
            table.Rows[1]["Непрочитано"].Should().Be(false);
            table.Rows[1]["Тема"].Should().Be("Re: Отчёт");
        }

        [Fact(DisplayName = "Таблица: пустая выдача — колонки без строк")]
        public void BuildMessagesTable_Empty()
        {
            var table = _logic.BuildMessagesTable(
                Array.Empty<MimeMessage>(), _ => true, _ => "", _ => "");
            table.Rows.Count.Should().Be(0);
        }

        #endregion

        #region ValidateSettings

        [Fact(DisplayName = "Настройки: пустой хост — ArgumentException")]
        public void ValidateSettings_EmptyHost()
        {
            Action act = () => _logic.ValidateSettings(new ImapConnectionSettings());
            act.Should().Throw<ArgumentException>();
        }

        [Fact(DisplayName = "Настройки: корректные — без исключений")]
        public void ValidateSettings_Valid()
        {
            Action act = () => _logic.ValidateSettings(Settings());
            act.Should().NotThrow();
        }

        #endregion

        #region LoadEml

        [Fact(DisplayName = "EML: несуществующий файл — FileNotFoundException")]
        public void LoadEml_MissingFile()
        {
            Action act = () => _logic.LoadEml(Path.Combine(_workDir, "no-such.eml"));
            act.Should().Throw<FileNotFoundException>();
        }

        [Fact(DisplayName = "EML: загрузка из потока (без закрытия)")]
        public void LoadEml_FromStream()
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse("x@y.ru"));
            message.Subject = "поток";
            var stream = new MemoryStream();
            message.WriteTo(stream);
            stream.Position = 0;

            var loaded = _logic.LoadEml(stream);
            loaded.Subject.Should().Be("поток");
        }

        #endregion
    }
}
