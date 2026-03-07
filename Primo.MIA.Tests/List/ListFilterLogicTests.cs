using System;
using System.Collections.Generic;
using FluentAssertions;
using Primo.MIA.Tests.Logic;
using Xunit;

namespace Primo.MIA.Tests.List
{
    public class ListFilterLogicTests
    {
        private readonly ListFilterLogic _logic;

        public ListFilterLogicTests()
        {
            _logic = new ListFilterLogic();
        }

        #region Contains Tests

        [Fact]
        public void Filter_Contains_CaseInsensitive_ReturnsMatchingElements()
        {
            // Arrange
            var list = new List<string> { "apple", "banana", "APRICOT", "cherry" };

            // Act
            var result = _logic.Filter(list, "ap", ListFilterMode.Contains, caseSensitive: false);

            // Assert
            result.Matched.Should().HaveCount(2);
            result.Matched.Should().Contain("apple");
            result.Matched.Should().Contain("APRICOT");
            result.Rejected.Should().HaveCount(2);
            result.Rejected.Should().Contain("banana");
            result.Rejected.Should().Contain("cherry");
        }

        [Fact]
        public void Filter_Contains_CaseSensitive_ReturnsOnlyExactCase()
        {
            // Arrange
            var list = new List<string> { "apple", "Apple", "APPLE" };

            // Act
            var result = _logic.Filter(list, "app", ListFilterMode.Contains, caseSensitive: true);

            // Assert
            result.Matched.Should().HaveCount(1);
            result.Matched.Should().Contain("apple");
            result.Rejected.Should().HaveCount(2);
        }

        [Fact]
        public void Filter_Contains_EmptyPattern_MatchesAll()
        {
            // Arrange
            var list = new List<string> { "apple", "banana", "cherry" };

            // Act
            var result = _logic.Filter(list, "", ListFilterMode.Contains, caseSensitive: false);

            // Assert
            result.Matched.Should().HaveCount(3);
            result.Rejected.Should().BeEmpty();
        }

        #endregion

        #region NotContains Tests

        [Fact]
        public void Filter_NotContains_ReturnsNonMatchingElements()
        {
            // Arrange
            var list = new List<string> { "apple", "banana", "apricot", "cherry" };

            // Act
            var result = _logic.Filter(list, "ap", ListFilterMode.NotContains, caseSensitive: false);

            // Assert
            result.Matched.Should().HaveCount(2);
            result.Matched.Should().Contain("banana");
            result.Matched.Should().Contain("cherry");
            result.Rejected.Should().HaveCount(2);
        }

        #endregion

        #region StartsWith Tests

        [Fact]
        public void Filter_StartsWith_ReturnsElementsStartingWithPattern()
        {
            // Arrange
            var list = new List<string> { "apple", "apricot", "banana", "application" };

            // Act
            var result = _logic.Filter(list, "app", ListFilterMode.StartsWith, caseSensitive: false);

            // Assert
            result.Matched.Should().HaveCount(2);
            result.Matched.Should().Contain("apple");
            result.Matched.Should().Contain("application");
        }

        #endregion

        #region EndsWith Tests

        [Fact]
        public void Filter_EndsWith_ReturnsElementsEndingWithPattern()
        {
            // Arrange
            var list = new List<string> { "apple", "pineapple", "banana", "grape" };

            // Act
            var result = _logic.Filter(list, "ple", ListFilterMode.EndsWith, caseSensitive: false);

            // Assert
            result.Matched.Should().HaveCount(2);
            result.Matched.Should().Contain("apple");
            result.Matched.Should().Contain("pineapple");
        }

        #endregion

        #region ExactMatch Tests

        [Theory]
        [InlineData("test", true)]
        [InlineData("TEST", true)]
        [InlineData("Test", true)]
        [InlineData("testing", false)]
        [InlineData("tes", false)]
        public void Filter_ExactMatch_CaseInsensitive(string input, bool shouldMatch)
        {
            // Arrange
            var list = new List<string> { input };

            // Act
            var result = _logic.Filter(list, "test", ListFilterMode.ExactMatch, caseSensitive: false);

            // Assert
            if (shouldMatch)
            {
                result.Matched.Should().HaveCount(1);
                result.Rejected.Should().BeEmpty();
            }
            else
            {
                result.Matched.Should().BeEmpty();
                result.Rejected.Should().HaveCount(1);
            }
        }

        [Fact]
        public void Filter_ExactMatch_CaseSensitive_OnlyExactCase()
        {
            // Arrange
            var list = new List<string> { "test", "TEST", "Test" };

            // Act
            var result = _logic.Filter(list, "test", ListFilterMode.ExactMatch, caseSensitive: true);

            // Assert
            result.Matched.Should().HaveCount(1);
            result.Matched.Should().Contain("test");
            result.Rejected.Should().HaveCount(2);
        }

        #endregion

        #region Regex Tests

        [Fact]
        public void Filter_Regex_MatchesPattern()
        {
            // Arrange
            var list = new List<string> { "test123", "test", "123test", "test456test" };

            // Act
            var result = _logic.Filter(list, @"^test\d+$", ListFilterMode.Regex, caseSensitive: false);

            // Assert
            result.Matched.Should().HaveCount(1);
            result.Matched.Should().Contain("test123");
        }

        [Fact]
        public void Filter_Regex_InvalidPattern_ThrowsException()
        {
            // Arrange
            var list = new List<string> { "test" };

            // Act & Assert
            Action act = () => _logic.Filter(list, "[invalid(", ListFilterMode.Regex, caseSensitive: false);
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region NotEmpty Tests

        [Fact]
        public void Filter_NotEmpty_RemovesEmptyAndWhitespace()
        {
            // Arrange
            var list = new List<string> { "apple", "", "  ", null, "banana", "\t\n" };

            // Act
            var result = _logic.Filter(list, "", ListFilterMode.NotEmpty, caseSensitive: false);

            // Assert
            result.Matched.Should().HaveCount(2);
            result.Matched.Should().Contain("apple");
            result.Matched.Should().Contain("banana");
            result.Rejected.Should().HaveCount(4);
        }

        #endregion

        #region EmptyOnly Tests

        [Fact]
        public void Filter_EmptyOnly_ReturnsOnlyEmptyElements()
        {
            // Arrange
            var list = new List<string> { "apple", "", "  ", null, "banana" };

            // Act
            var result = _logic.Filter(list, "", ListFilterMode.EmptyOnly, caseSensitive: false);

            // Assert
            result.Matched.Should().HaveCount(3);
            result.Rejected.Should().HaveCount(2);
        }

        #endregion

        #region LengthRange Tests

        [Fact]
        public void Filter_LengthRange_ReturnsElementsInRange()
        {
            // Arrange
            var list = new List<string> { "a", "ab", "abc", "abcd", "abcde" };

            // Act
            var result = _logic.Filter(list, "", ListFilterMode.LengthRange, 
                caseSensitive: false, minLength: 2, maxLength: 4);

            // Assert
            result.Matched.Should().HaveCount(3);
            result.Matched.Should().Contain("ab");
            result.Matched.Should().Contain("abc");
            result.Matched.Should().Contain("abcd");
            result.Rejected.Should().HaveCount(2);
        }

        [Fact]
        public void Filter_LengthRange_MinOnly()
        {
            // Arrange
            var list = new List<string> { "a", "ab", "abc", "abcd" };

            // Act
            var result = _logic.Filter(list, "", ListFilterMode.LengthRange, 
                caseSensitive: false, minLength: 3, maxLength: int.MaxValue);

            // Assert
            result.Matched.Should().HaveCount(2);
            result.Matched.Should().Contain("abc");
            result.Matched.Should().Contain("abcd");
        }

        #endregion

        #region NumericOnly Tests

        [Theory]
        [InlineData("123", true)]
        [InlineData("123.45", true)]
        [InlineData("-123", true)]
        [InlineData("1.23e10", true)]
        [InlineData("abc", false)]
        [InlineData("12abc", false)]
        [InlineData("", false)]
        [InlineData("  ", false)]
        public void Filter_NumericOnly_ValidatesNumbers(string input, bool shouldMatch)
        {
            // Arrange
            var list = new List<string> { input };

            // Act
            var result = _logic.Filter(list, "", ListFilterMode.NumericOnly, caseSensitive: false);

            // Assert
            if (shouldMatch)
            {
                result.Matched.Should().HaveCount(1);
                result.Rejected.Should().BeEmpty();
            }
            else
            {
                result.Matched.Should().BeEmpty();
                result.Rejected.Should().HaveCount(1);
            }
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Filter_NullList_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.Filter(null, "test", ListFilterMode.Contains);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Filter_EmptyList_ReturnsEmptyResults()
        {
            // Arrange
            var list = new List<string>();

            // Act
            var result = _logic.Filter(list, "test", ListFilterMode.Contains);

            // Assert
            result.Matched.Should().BeEmpty();
            result.Rejected.Should().BeEmpty();
        }

        [Fact]
        public void Filter_ListWithNulls_HandlesGracefully()
        {
            // Arrange
            var list = new List<string> { "apple", null, "banana", null };

            // Act
            var result = _logic.Filter(list, "a", ListFilterMode.Contains);

            // Assert
            result.Matched.Should().HaveCount(2);
            result.Rejected.Should().HaveCount(2);
        }

        #endregion
    }
}
