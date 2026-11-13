using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    /// <summary>
    /// Тесты RegexCache (issue #24, ReDoS): кэш экземпляров, диапазон
    /// таймаута, прерывание катастрофического бэктрекинга, поведение
    /// подключённой логики (ListFilter/ListTransform/ListGroup).
    /// </summary>
    [Trait("Category", "Fast")]
    public class RegexCacheTests
    {
        private const string EvilPattern = "(a+)+$";

        private static string EvilInput(int length = 5000)
            => new string('a', length) + "!";

        #region Кэш

        [Fact(DisplayName = "Кэш: одинаковый паттерн+опции — тот же экземпляр")]
        public void Get_SamePatternAndOptions_ReturnsSameInstance()
        {
            var first = RegexCache.Get("mia-test-pattern", RegexOptions.Compiled);
            var second = RegexCache.Get("mia-test-pattern", RegexOptions.Compiled);
            second.Should().BeSameAs(first);
        }

        [Fact(DisplayName = "Кэш: разные опции — разные экземпляры")]
        public void Get_DifferentOptions_ReturnsDifferentInstances()
        {
            var plain = RegexCache.Get("mia-test-options", RegexOptions.Compiled);
            var ignoreCase = RegexCache.Get("mia-test-options", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            ignoreCase.Should().NotBeSameAs(plain);
        }

        [Fact(DisplayName = "Кэш: разные таймауты — разные экземпляры")]
        public void Get_DifferentTimeouts_ReturnsDifferentInstances()
        {
            var fast = RegexCache.Get("mia-test-timeout", RegexOptions.Compiled, 500);
            var slow = RegexCache.Get("mia-test-timeout", RegexOptions.Compiled, 2000);
            slow.Should().NotBeSameAs(fast);
        }

        [Fact(DisplayName = "Кэш: пустой паттерн — Guard")]
        public void Get_EmptyPattern_Throws()
        {
            Action act = () => RegexCache.Get("  ");
            act.Should().Throw<ArgumentException>();
        }

        [Fact(DisplayName = "Кэш: параллельные запросы — один экземпляр")]
        public void Get_Concurrent_SingleInstance()
        {
            var instances = Enumerable.Range(0, 8)
                .Select(_ => Task.Factory.StartNew(
                    () => RegexCache.Get("mia-test-concurrent", RegexOptions.Compiled)))
                .Select(t => t.Result)
                .ToList();
            instances.Distinct().Should().HaveCount(1);
        }

        #endregion

        #region Таймаут

        [Fact(DisplayName = "Таймаут: null/<=0 — 1 с, >60 с — 60 с")]
        public void ResolveTimeout_Clamps()
        {
            RegexCache.ResolveTimeout(null).Should().Be(TimeSpan.FromSeconds(1));
            RegexCache.ResolveTimeout(0).Should().Be(TimeSpan.FromSeconds(1));
            RegexCache.ResolveTimeout(-5).Should().Be(TimeSpan.FromSeconds(1));
            RegexCache.ResolveTimeout(3000).Should().Be(TimeSpan.FromSeconds(3));
            RegexCache.ResolveTimeout(61000).Should().Be(TimeSpan.FromSeconds(60));
        }

        [Fact(DisplayName = "ReDoS: (a+)+$ на «aaaa…!» — RegexMatchTimeoutException, не зависание")]
        public void Get_EvilPattern_ThrowsTimeout()
        {
            var regex = RegexCache.Get(EvilPattern);
            Action match = () => regex.IsMatch(EvilInput());
            match.Should().Throw<RegexMatchTimeoutException>()
                .And.MatchTimeout.Should().Be(TimeSpan.FromSeconds(1),
                    "таймаут по умолчанию 1 с обязателен для пользовательских паттернов");
        }

        #endregion

        #region Подключённая логика (List/issue #24)

        [Fact(DisplayName = "ListFilter Regex: катастрофический паттерн — таймаут")]
        public void ListFilter_EvilPattern_ThrowsTimeout()
        {
            var logic = new ListFilterLogic();
            var source = new List<string> { EvilInput() };
            Action act = () => logic.Filter(source, EvilPattern, ListFilterMode.Regex);
            act.Should().Throw<RegexMatchTimeoutException>();
        }

        [Fact(DisplayName = "ListFilter Regex: нормальный паттерн работает и кэшируется")]
        public void ListFilter_NormalPattern_Works()
        {
            var logic = new ListFilterLogic();
            var source = new List<string> { "отчёт-2026.csv", "акт.xlsx", "отчёт-2025.csv" };
            var result = logic.Filter(source, "^отчёт", ListFilterMode.Regex);
            result.Matched.Should().HaveCount(2);
            result.Rejected.Should().Contain("акт.xlsx");
        }

        [Fact(DisplayName = "ListTransform RegexReplace: катастрофический паттерн — таймаут")]
        public void ListTransform_EvilPattern_ThrowsTimeout()
        {
            var logic = new ListTransformLogic();
            var source = new List<string> { EvilInput() };
            Action act = () => logic.Transform(
                source, ListTransformMode.RegexReplace, EvilPattern, "X");
            act.Should().Throw<RegexMatchTimeoutException>();
        }

        [Fact(DisplayName = "ListGroup ByRegexGroup: катастрофический паттерн — таймаут")]
        public void ListGroup_EvilPattern_ThrowsTimeout()
        {
            var logic = new ListGroupLogic();
            var source = new List<string> { EvilInput() };
            Action act = () => logic.Group(source, ListGroupMode.ByRegexGroup, regexPattern: EvilPattern);
            act.Should().Throw<RegexMatchTimeoutException>();
        }

        [Fact(DisplayName = "ListGroup ByRegexGroup: нормальный паттерн группирует")]
        public void ListGroup_NormalPattern_Groups()
        {
            var logic = new ListGroupLogic();
            var source = new List<string> { "А-1", "А-2", "Б-1" };
            var groups = logic.Group(source, ListGroupMode.ByRegexGroup, regexPattern: "^([АБ])-");
            groups.Should().ContainKey("А");
            groups["А"].Should().HaveCount(2);
            groups.Should().ContainKey("Б");
        }

        #endregion
    }
}
