using System;
using System.Data;
using System.Linq;
using FluentAssertions;
using MailKit.Security;
using MimeKit;
using Xunit;

namespace Primo.MIA.Tests.Mail
{
    /// <summary>
    /// Тесты Pop3Logic (issue #37, фаза 3): TLS-опции, разбор даты,
    /// клиентский фильтр, DataTable с UID, валидация настроек.
    /// Сетевая часть (Pop3Client) — кандидат на Trait(Integration).
    /// </summary>
    [Trait("Category", "Fast")]
    public class Pop3LogicTests
    {
        private readonly Pop3Logic _logic = new Pop3Logic();

        private Pop3ConnectionSettings Settings(
            bool implicitTls = true, bool startTls = false)
            => new Pop3ConnectionSettings
            {
                Host = "pop3.test.ru",
                UseImplicitTls = implicitTls,
                UseStartTls = startTls
            };

        private MimeMessage Message(string from, string subject, DateTime? date = null)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            if (subject != null)
                message.Subject = subject;
            if (date.HasValue)
                message.Date = new DateTimeOffset(date.Value, TimeSpan.Zero);
            return message;
        }

        #region ValidateSettings

        [Fact(DisplayName = "Настройки: пустой хост — ArgumentException")]
        public void ValidateSettings_EmptyHost()
        {
            Action act = () => _logic.ValidateSettings(new Pop3ConnectionSettings());
            act.Should().Throw<ArgumentException>();
        }

        [Fact(DisplayName = "Настройки: корректные — без исключений")]
        public void ValidateSettings_Valid()
        {
            Action act = () => _logic.ValidateSettings(Settings());
            act.Should().NotThrow();
        }

        [Fact(DisplayName = "Настройки: порт по умолчанию 995")]
        public void Settings_DefaultPort()
        {
            new Pop3ConnectionSettings().Port.Should().Be(995);
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
        }

        [Fact(DisplayName = "Дата «С даты»: мусор — FormatException")]
        public void ParseSince_Invalid()
        {
            Action act = () => _logic.ParseSince("вчера");
            act.Should().Throw<FormatException>();
        }

        #endregion

        #region MatchesFilter

        [Fact(DisplayName = "Фильтр: пустой — письмо проходит")]
        public void MatchesFilter_EmptyFilter()
        {
            var message = Message("boss@corp.ru", "Отчёт", new DateTime(2026, 11, 10));
            _logic.MatchesFilter(message, new Pop3Filter()).Should().BeTrue();
        }

        [Fact(DisplayName = "Фильтр: подстрока темы, без учёта регистра")]
        public void MatchesFilter_Subject()
        {
            var message = Message("boss@corp.ru", "Акт сверки за ноябрь");
            _logic.MatchesFilter(message, new Pop3Filter { SubjectContains = "СВЕРКИ" })
                .Should().BeTrue();
            _logic.MatchesFilter(message, new Pop3Filter { SubjectContains = "отчёт" })
                .Should().BeFalse();
        }

        [Fact(DisplayName = "Фильтр: тема без темы (null) — не проходит")]
        public void MatchesFilter_NullSubject()
        {
            var message = Message("boss@corp.ru", null);
            _logic.MatchesFilter(message, new Pop3Filter { SubjectContains = "что-то" })
                .Should().BeFalse();
        }

        [Fact(DisplayName = "Фильтр: подстрока отправителя, без учёта регистра")]
        public void MatchesFilter_From()
        {
            var message = Message("Alice <alice@corp.ru>", "Привет");
            _logic.MatchesFilter(message, new Pop3Filter { FromContains = "ALICE@" })
                .Should().BeTrue();
            _logic.MatchesFilter(message, new Pop3Filter { FromContains = "bob" })
                .Should().BeFalse();
        }

        [Fact(DisplayName = "Фильтр: дата «с» включительно")]
        public void MatchesFilter_Since()
        {
            var message = Message("a@b.ru", "тема", new DateTime(2026, 11, 10, 9, 0, 0));
            var filter = new Pop3Filter { Since = new DateTime(2026, 11, 10) };
            _logic.MatchesFilter(message, filter).Should().BeTrue();

            filter = new Pop3Filter { Since = new DateTime(2026, 11, 10, 9, 0, 1) };
            _logic.MatchesFilter(message, filter).Should().BeFalse();
        }

        [Fact(DisplayName = "Фильтр: письмо без заголовка Date трактуется как свежее (проходит «с»)")]
        public void MatchesFilter_NoDateHeader()
        {
            // MimeKit подставляет текущее время при отсутствии заголовка Date
            var message = Message("a@b.ru", "тема");
            var filter = new Pop3Filter { Since = new DateTime(2020, 1, 1) };
            _logic.MatchesFilter(message, filter).Should().BeTrue();

            var past = new Pop3Filter { Since = DateTime.Now.AddDays(1) };
            _logic.MatchesFilter(message, past).Should().BeFalse();
        }

        [Fact(DisplayName = "Фильтр: null-письмо или null-фильтр — Guard")]
        public void MatchesFilter_NullArguments()
        {
            Action nullMessage = () => _logic.MatchesFilter(null, new Pop3Filter());
            nullMessage.Should().Throw<ArgumentNullException>();

            Action nullFilter = () => _logic.MatchesFilter(new MimeMessage(), null);
            nullFilter.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region BuildMessagesTable

        [Fact(DisplayName = "Таблица: колонки и значения строк, UID на месте")]
        public void BuildMessagesTable_Populates()
        {
            var first = Message("Alice <alice@corp.ru>", "Отчёт за ноябрь",
                new DateTime(2026, 11, 10, 12, 0, 0));
            var second = Message("bob@corp.ru", "Re: Отчёт", new DateTime(2026, 11, 11, 8, 0, 0));

            var table = _logic.BuildMessagesTable(
                new[] { first, second },
                new[] { "uid-1", "uid-2" },
                m => MailCommon.GetBodyText(m));

            table.Rows.Count.Should().Be(2);
            table.Columns.Should().Contain(c => c.ColumnName == "От");
            table.Columns.Should().Contain(c => c.ColumnName == "Тема");
            table.Columns.Should().Contain(c => c.ColumnName == "Дата");
            table.Columns.Should().Contain(c => c.ColumnName == "UID");
            table.Columns.Should().Contain(c => c.ColumnName == "Тело");

            table.Rows[0]["Тема"].Should().Be("Отчёт за ноябрь");
            table.Rows[0]["UID"].Should().Be("uid-1");
            table.Rows[0]["Дата"].Should().Be(new DateTime(2026, 11, 10, 12, 0, 0));
            table.Rows[1]["UID"].Should().Be("uid-2");
            table.Rows[1]["Тема"].Should().Be("Re: Отчёт");
        }

        [Fact(DisplayName = "Таблица: письмо без заголовка Date — текущее время")]
        public void BuildMessagesTable_NoDateHeader()
        {
            // MimeKit подставляет текущее время при отсутствии заголовка Date
            var message = Message("a@b.ru", "без даты");
            var table = _logic.BuildMessagesTable(
                new[] { message }, new[] { "uid" }, _ => "");
            var date = table.Rows[0]["Дата"].Should().BeOfType<DateTime>().Subject;
            date.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(5));
        }

        [Fact(DisplayName = "Таблица: null-тема — пустая строка, null-UID — пустая строка")]
        public void BuildMessagesTable_NullSubjectAndUid()
        {
            var message = Message("a@b.ru", null);
            var table = _logic.BuildMessagesTable(
                new[] { message }, new string[] { null }, _ => "");
            table.Rows[0]["Тема"].Should().Be(string.Empty);
            table.Rows[0]["UID"].Should().Be(string.Empty);
        }

        [Fact(DisplayName = "Таблица: пустая выдача — колонки без строк")]
        public void BuildMessagesTable_Empty()
        {
            var table = _logic.BuildMessagesTable(
                Array.Empty<MimeMessage>(), Array.Empty<string>(), _ => "");
            table.Rows.Count.Should().Be(0);
        }

        [Fact(DisplayName = "Таблица: число UID ≠ числу писем — ArgumentException")]
        public void BuildMessagesTable_UidCountMismatch()
        {
            var message = Message("a@b.ru", "тема");
            Action act = () => _logic.BuildMessagesTable(
                new[] { message }, new[] { "uid-1", "uid-2" }, _ => "");
            act.Should().Throw<ArgumentException>();
        }

        [Fact(DisplayName = "Таблица: null-аргументы — Guard")]
        public void BuildMessagesTable_NullArguments()
        {
            var message = Message("a@b.ru", "тема");
            Action nullMessages = () => _logic.BuildMessagesTable(null, new[] { "uid" }, _ => "");
            nullMessages.Should().Throw<ArgumentNullException>();

            Action nullUids = () => _logic.BuildMessagesTable(new[] { message }, null, _ => "");
            nullUids.Should().Throw<ArgumentNullException>();

            Action nullBody = () => _logic.BuildMessagesTable(
                new[] { message }, new[] { "uid" }, null);
            nullBody.Should().Throw<ArgumentNullException>();
        }

        #endregion
    }
}
