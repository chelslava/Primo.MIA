using System;
using System.Globalization;
using System.Text.RegularExpressions;
using FluentAssertions;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    public class StringHelperTests
    {
        [Theory(DisplayName = "TryParseDouble: null or whitespace returns null")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TryParseDouble_NullOrWhitespace_ReturnsNull(string value)
        {
            StringHelper.TryParseDouble(value).Should().BeNull();
        }

        [Fact(DisplayName = "TryParseDouble: parses invariant format")]
        public void TryParseDouble_InvariantValue_ReturnsParsedNumber()
        {
            StringHelper.TryParseDouble("12.5").Should().Be(12.5);
        }

        [Fact(DisplayName = "TryParseDouble: falls back to current culture")]
        public void TryParseDouble_CurrentCultureValue_ReturnsParsedNumber()
        {
            WithCulture("de-DE", () =>
            {
                StringHelper.TryParseDouble("1.234,5").Should().Be(1234.5);
            });
        }

        [Fact(DisplayName = "TryParseDouble: invalid value returns null")]
        public void TryParseDouble_InvalidValue_ReturnsNull()
        {
            StringHelper.TryParseDouble("not-a-number").Should().BeNull();
        }

        [Fact(DisplayName = "TryParseInt: parses integer with spaces")]
        public void TryParseInt_ValidValue_ReturnsParsedInt()
        {
            StringHelper.TryParseInt(" 42 ").Should().Be(42);
        }

        [Theory(DisplayName = "TryParseInt: invalid value returns null")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("12.5")]
        [InlineData("abc")]
        public void TryParseInt_InvalidValue_ReturnsNull(string value)
        {
            StringHelper.TryParseInt(value).Should().BeNull();
        }

        [Theory(DisplayName = "WildcardToRegex: null or empty becomes catch-all regex")]
        [InlineData(null)]
        [InlineData("")]
        public void WildcardToRegex_NullOrEmpty_ReturnsCatchAllPattern(string pattern)
        {
            StringHelper.WildcardToRegex(pattern).Should().Be(".*");
        }

        [Fact(DisplayName = "WildcardToRegex: converts wildcards and escapes special characters")]
        public void WildcardToRegex_ConvertsWildcardsAndEscapesRegexCharacters()
        {
            StringHelper.WildcardToRegex("file[1]??.txt").Should().Be(@"^file\[1]..\.txt$");
        }

        [Fact(DisplayName = "WildcardToRegex: produced pattern matches expected strings")]
        public void WildcardToRegex_ProducesWorkingRegex()
        {
            var regex = new Regex(StringHelper.WildcardToRegex("report-*.csv"));

            regex.IsMatch("report-2026.csv").Should().BeTrue();
            regex.IsMatch("report.csv").Should().BeFalse();
        }

        private static void WithCulture(string cultureName, Action action)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;

            try
            {
                var culture = new CultureInfo(cultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                action();
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }
    }
}
