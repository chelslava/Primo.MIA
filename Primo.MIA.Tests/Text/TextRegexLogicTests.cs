using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.Text
{
    /// <summary>
    /// Тесты TextRegexLogic (issue #30): Match/Replace, именованные группы,
    /// опциональные группы, опции, ReDoS-таймаут, Guard.
    /// </summary>
    [Trait("Category", "Fast")]
    public class TextRegexLogicTests
    {
        private readonly TextRegexLogic _logic = new TextRegexLogic();

        // ── Опции и компиляция ─────────────────────────────────────────

        [Fact(DisplayName = "BuildOptions: комбинация флагов")]
        public void BuildOptions_CombinesFlags()
        {
            _logic.BuildOptions(false, false, false).Should().Be(RegexOptions.None);
            _logic.BuildOptions(true, false, false).Should().Be(RegexOptions.IgnoreCase);
            _logic.BuildOptions(true, true, true)
                .Should().Be(RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline);
        }

        [Fact(DisplayName = "BuildRegex: нулевой таймаут → значение по умолчанию")]
        public void BuildRegex_ZeroTimeout_UsesDefault()
        {
            var regex = _logic.BuildRegex("a", RegexOptions.None, 0);
            regex.MatchTimeout.Should().Be(TimeSpan.FromMilliseconds(TextRegexLogic.DefaultTimeoutMs));
        }

        [Fact(DisplayName = "BuildRegex: таймаут сверх максимума ограничивается")]
        public void BuildRegex_OverMaxTimeout_Clamped()
        {
            var regex = _logic.BuildRegex("a", RegexOptions.None, 999_999);
            regex.MatchTimeout.Should().Be(TimeSpan.FromMilliseconds(TextRegexLogic.MaxTimeoutMs));
        }

        [Fact(DisplayName = "BuildRegex: некорректный паттерн → InvalidOperationException")]
        public void BuildRegex_InvalidPattern_Throws()
        {
            Action act = () => _logic.BuildRegex("(не-закрыта", RegexOptions.None, 1000);
            act.Should().Throw<InvalidOperationException>().WithMessage("*выражение*");
        }

        [Fact(DisplayName = "BuildRegex: пустой паттерн — ArgumentException (Guard)")]
        public void BuildRegex_EmptyPattern_Throws()
        {
            Action act = () => _logic.BuildRegex("", RegexOptions.None, 1000);
            act.Should().Throw<ArgumentException>();
        }

        // ── Match → DataTable ──────────────────────────────────────────

        [Fact(DisplayName = "FindMatches: все совпадения с позициями")]
        public void FindMatches_Basic()
        {
            var regex = _logic.BuildRegex(@"\d+", RegexOptions.None, 1000);
            var table = _logic.FindMatches("цены: 100, 200 и 300", regex);

            table.Rows.Count.Should().Be(3);
            table.Columns["Match"].DataType.Should().Be(typeof(string));
            table.Columns["Index"].DataType.Should().Be(typeof(int));
            ((string)table.Rows[0]["Match"]).Should().Be("100");
            ((int)table.Rows[1]["Index"]).Should().Be(11);
        }

        [Fact(DisplayName = "FindMatches: нумерованные группы в колонках")]
        public void FindMatches_NumberedGroups()
        {
            var regex = _logic.BuildRegex(@"(\w+)@(\w+)\.(\w+)", RegexOptions.None, 1000);
            var table = _logic.FindMatches("иван@mail.ru и maria@yandex.ru", regex);

            table.Rows.Count.Should().Be(2);
            ((string)table.Rows[0]["1"]).Should().Be("иван");
            ((string)table.Rows[0]["2"]).Should().Be("mail");
            ((string)table.Rows[1]["3"]).Should().Be("ru");
        }

        [Fact(DisplayName = "FindMatches: именованные группы — колонки по имени")]
        public void FindMatches_NamedGroups()
        {
            var regex = _logic.BuildRegex(@"(?<year>\d{4})-(?<month>\d{2})", RegexOptions.None, 1000);
            var table = _logic.FindMatches("даты: 2024-01 и 2023-12", regex);

            ((string)table.Rows[0]["year"]).Should().Be("2024");
            ((string)table.Rows[1]["month"]).Should().Be("12");
            table.Columns.Contains("0").Should().BeFalse("группа 0 представлена колонкой Match");
        }

        [Fact(DisplayName = "FindMatches: несовпавшая опциональная группа — пустая строка")]
        public void FindMatches_OptionalGroupEmptyString()
        {
            var regex = _logic.BuildRegex(@"(\d+)(?:-(?<suffix>[a-z]+))?", RegexOptions.None, 1000);
            var table = _logic.FindMatches("123", regex);

            table.Rows.Count.Should().Be(1);
            ((string)table.Rows[0]["suffix"]).Should().Be("", "опциональная группа не совпала");
        }

        [Fact(DisplayName = "FindMatches: без совпадений — пустая таблица с колонками")]
        public void FindMatches_NoMatches_EmptyTable()
        {
            var regex = _logic.BuildRegex("xyz", RegexOptions.None, 1000);
            var table = _logic.FindMatches("abc", regex);

            table.Rows.Count.Should().Be(0);
            table.Columns["Match"].Should().NotBeNull("структура сохраняется для привязок");
        }

        [Fact(DisplayName = "FindMatches: IgnoreCase меняет результат")]
        public void FindMatches_IgnoreCase()
        {
            var input = "Иван ИВАН иван";

            var sensitive = _logic.FindMatches(input, _logic.BuildRegex("иван", RegexOptions.None, 1000));
            var insensitive = _logic.FindMatches(input, _logic.BuildRegex("иван", RegexOptions.IgnoreCase, 1000));

            sensitive.Rows.Count.Should().Be(1);
            insensitive.Rows.Count.Should().Be(3);
        }

        // ── Replace ────────────────────────────────────────────────────

        [Fact(DisplayName = "ReplaceMatches: подстановки $1 и ${name}")]
        public void ReplaceMatches_Substitutions()
        {
            var regex = _logic.BuildRegex(@"(\w+)@(\w+)\.(\w+)", RegexOptions.None, 1000);

            _logic.ReplaceMatches("иван@mail.ru", regex, "$2.$1")
                .Should().Be("mail.иван");

            var named = _logic.BuildRegex(@"(?<user>\w+)@(?<host>\w+)", RegexOptions.None, 1000);
            _logic.ReplaceMatches("иван@mail", named, "${host}: ${user}")
                .Should().Be("mail: иван");
        }

        [Fact(DisplayName = "ReplaceMatches: null замена → пустая строка")]
        public void ReplaceMatches_NullReplacement_Empty()
        {
            var regex = _logic.BuildRegex(@"\d+", RegexOptions.None, 1000);
            _logic.ReplaceMatches("a1b2", regex, null).Should().Be("ab");
        }

        // ── ReDoS-таймаут ──────────────────────────────────────────────

        [Fact(DisplayName = "ReDoS: катастрофический бэктрекинг прерывается таймаутом")]
        public void Match_CatastrophicBacktracking_TimeoutException()
        {
            var regex = _logic.BuildRegex("(a+)+$", RegexOptions.None, 200);
            var longInput = new string('a', 10_000) + "!";

            Action act = () => regex.Match(longInput);
            act.Should().Throw<RegexMatchTimeoutException>(
                "кровожадный паттерн обязан прерваться, а не подвесить робота");
        }

        // ── Guard ──────────────────────────────────────────────────────

        [Fact(DisplayName = "Guard: null входных аргументов — ArgumentException")]
        public void Guard_NullArguments_Throw()
        {
            var regex = _logic.BuildRegex("a", RegexOptions.None, 1000);

            Action noInput = () => _logic.FindMatches(null, regex);
            Action noRegex = () => _logic.FindMatches("abc", null);
            Action noCount = () => _logic.CountMatches(null, regex);

            noInput.Should().Throw<ArgumentException>();
            noRegex.Should().Throw<ArgumentException>();
            noCount.Should().Throw<ArgumentException>();
        }

        [Fact(DisplayName = "CountMatches: счётчик совпадений")]
        public void CountMatches_Basic()
        {
            var regex = _logic.BuildRegex(@"\d+", RegexOptions.None, 1000);
            _logic.CountMatches("1, 22 и 333", regex).Should().Be(3);
        }
    }
}
