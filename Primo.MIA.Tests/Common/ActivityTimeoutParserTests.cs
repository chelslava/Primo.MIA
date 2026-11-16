using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    /// <summary>
    /// Тесты ActivityTimeoutParser: секунды → мс / TimeSpan,
    /// ParseSecondsOrDefault с логированием некорректных значений (issue #44).
    /// </summary>
    [Trait("Category", "Fast")]
    public class ActivityTimeoutParserTests
    {
        #region ParseSecondsToMs

        [Fact(DisplayName = "ParseSecondsToMs: положительные секунды → миллисекунды")]
        public void ParseSecondsToMs_Positive()
        {
            ActivityTimeoutParser.ParseSecondsToMs("30", 5000).Should().Be(30000);
        }

        [Fact(DisplayName = "ParseSecondsToMs: пустое/невалидное/неположное → дефолт")]
        public void ParseSecondsToMs_Defaults()
        {
            ActivityTimeoutParser.ParseSecondsToMs(null, 30000).Should().Be(30000);
            ActivityTimeoutParser.ParseSecondsToMs("", 30000).Should().Be(30000);
            ActivityTimeoutParser.ParseSecondsToMs("  ", 30000).Should().Be(30000);
            ActivityTimeoutParser.ParseSecondsToMs("десять", 30000).Should().Be(30000);
            ActivityTimeoutParser.ParseSecondsToMs("0", 30000).Should().Be(30000);
            ActivityTimeoutParser.ParseSecondsToMs("-5", 30000).Should().Be(30000);
        }

        #endregion

        #region ParseSeconds (TimeSpan)

        [Fact(DisplayName = "ParseSeconds: положительные секунды → TimeSpan")]
        public void ParseSeconds_Positive()
        {
            ActivityTimeoutParser.ParseSeconds("15", 10).Should().Be(System.TimeSpan.FromSeconds(15));
        }

        [Fact(DisplayName = "ParseSeconds: невалидное → дефолт")]
        public void ParseSeconds_Default()
        {
            ActivityTimeoutParser.ParseSeconds("abc", 10).Should().Be(System.TimeSpan.FromSeconds(10));
        }

        #endregion

        #region ParseSecondsOrDefault

        [Fact(DisplayName = "ParseSecondsOrDefault: валидное значение возвращается")]
        public void ParseSecondsOrDefault_Positive()
        {
            ActivityTimeoutParser.ParseSecondsOrDefault("25", 10).Should().Be(25);
        }

        [Fact(DisplayName = "ParseSecondsOrDefault: null/пустое → дефолт молча")]
        public void ParseSecondsOrDefault_NullOrEmpty()
        {
            ActivityTimeoutParser.ParseSecondsOrDefault(null, 10).Should().Be(10);
            ActivityTimeoutParser.ParseSecondsOrDefault("", 10).Should().Be(10);
            ActivityTimeoutParser.ParseSecondsOrDefault("  ", 10).Should().Be(10);
        }

        [Fact(DisplayName = "ParseSecondsOrDefault: мусор/ноль/отрицательное → дефолт")]
        public void ParseSecondsOrDefault_Invalid()
        {
            ActivityTimeoutParser.ParseSecondsOrDefault("10 сек", 10).Should().Be(10);
            ActivityTimeoutParser.ParseSecondsOrDefault("0", 10).Should().Be(10);
            ActivityTimeoutParser.ParseSecondsOrDefault("-3", 10).Should().Be(10);
        }

        #endregion
    }
}
