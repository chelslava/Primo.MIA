using System;
using FluentAssertions;
using Moq;
using OpenQA.Selenium;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Browser
{
    /// <summary>
    /// Тесты BrowserWaitLogic на замоканном IWebDriver:
    /// условия TitleContains/UrlContains, таймауты, валидация.
    /// </summary>
    [Trait("Category", "Fast")]
    public class BrowserWaitLogicTests
    {
        private readonly BrowserWaitLogic _logic;

        public BrowserWaitLogicTests()
        {
            _logic = new BrowserWaitLogic();
        }

        private static IWebDriver CreateDriver(string title = "", string url = "")
        {
            var mock = new Mock<IWebDriver>();
            mock.SetupGet(d => d.Title).Returns(title);
            mock.SetupGet(d => d.Url).Returns(url);
            return mock.Object;
        }

        [Fact]
        public void WaitFor_NullDriver_ThrowsArgumentNull()
        {
            Action act = () => _logic.WaitFor(WaitConditionType.TitleContains, 1000, 100, null, null, "text");
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WaitFor_TitleContains_MatchingTitle_Succeeds()
        {
            var driver = CreateDriver("Мой тестовый заголовок страницы");
            var result = _logic.WaitFor(WaitConditionType.TitleContains, 5000, 100, driver, null, "тестовый");
            result.Success.Should().BeTrue();
            result.ActualState.Should().Contain("TitleContains");
            result.Elapsed.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
        }

        [Fact]
        public void WaitFor_TitleContains_NotMatching_TimesOutGracefully()
        {
            var driver = CreateDriver("совершенно другое");
            var result = _logic.WaitFor(WaitConditionType.TitleContains, 300, 100, driver, null, "искомое");
            result.Success.Should().BeFalse();
            result.ActualState.Should().Contain("Таймаут");
            result.ActualState.Should().Contain("TitleContains");
        }

        [Fact]
        public void WaitFor_UrlContains_MatchingUrl_Succeeds()
        {
            var driver = CreateDriver(url: "https://example.com/dashboard?tab=main");
            var result = _logic.WaitFor(WaitConditionType.UrlContains, 5000, 100, driver, null, "dashboard");
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void WaitFor_UrlContains_NotMatching_ReturnsFalse()
        {
            var driver = CreateDriver(url: "https://example.com/login");
            var result = _logic.WaitFor(WaitConditionType.UrlContains, 300, 100, driver, null, "dashboard");
            result.Success.Should().BeFalse();
        }

        [Fact]
        public void WaitFor_UnknownCondition_ThrowsNotSupported()
        {
            var driver = CreateDriver();
            Action act = () => _logic.WaitFor((WaitConditionType)9999, 1000, 100, driver, By.Id("x"), null);
            act.Should().Throw<NotSupportedException>();
        }
    }
}
