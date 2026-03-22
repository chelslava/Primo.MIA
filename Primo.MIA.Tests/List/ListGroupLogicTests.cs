using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.List
{
    public class ListGroupLogicTests
    {
        private readonly ListGroupLogic _logic;

        public ListGroupLogicTests()
        {
            _logic = new ListGroupLogic();
        }

        #region ByFirstChar Tests

        [Fact]
        public void Group_ByFirstChar_GroupsCorrectly()
        {
            // Arrange
            var list = new List<string> { "apple", "apricot", "banana", "blueberry", "cherry" };

            // Act
            var result = _logic.Group(list, ListGroupMode.ByFirstChar);

            // Assert
            result.Should().HaveCount(3);
            result["A"].Should().HaveCount(2);
            result["A"].Should().Contain(new[] { "apple", "apricot" });
            result["B"].Should().HaveCount(2);
            result["C"].Should().HaveCount(1);
        }

        [Fact]
        public void Group_ByFirstChar_CaseInsensitive()
        {
            // Arrange
            var list = new List<string> { "Apple", "apple", "APPLE" };

            // Act
            var result = _logic.Group(list, ListGroupMode.ByFirstChar);

            // Assert
            result.Should().HaveCount(1);
            result["A"].Should().HaveCount(3);
        }

        [Fact]
        public void Group_ByFirstChar_SkipsEmpty()
        {
            // Arrange
            var list = new List<string> { "apple", "", null, "banana" };

            // Act
            var result = _logic.Group(list, ListGroupMode.ByFirstChar);

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainKey("A");
            result.Should().ContainKey("B");
        }

        #endregion

        #region ByLength Tests

        [Fact]
        public void Group_ByLength_GroupsByStringLength()
        {
            // Arrange
            var list = new List<string> { "a", "ab", "abc", "ab", "abcd" };

            // Act
            var result = _logic.Group(list, ListGroupMode.ByLength);

            // Assert
            result.Should().HaveCount(4);
            result["1"].Should().Equal("a");
            result["2"].Should().Equal("ab", "ab");
            result["3"].Should().Equal("abc");
            result["4"].Should().Equal("abcd");
        }

        [Fact]
        public void Group_ByLength_HandlesEmptyStrings()
        {
            // Arrange
            var list = new List<string> { "", "a", "" };

            // Act
            var result = _logic.Group(list, ListGroupMode.ByLength);

            // Assert
            result.Should().HaveCount(2);
            result["0"].Should().HaveCount(2);
            result["1"].Should().HaveCount(1);
        }

        #endregion

        #region ByPrefix Tests

        [Fact]
        public void Group_ByPrefix_Length2_GroupsCorrectly()
        {
            // Arrange
            var list = new List<string> { "apple", "apricot", "banana", "berry", "cherry" };

            // Act
            var result = _logic.Group(list, ListGroupMode.ByPrefix, prefixLength: 2);

            // Assert
            result.Should().HaveCount(4);
            result["AP"].Should().HaveCount(2);
            result["BA"].Should().HaveCount(1);
            result["BE"].Should().HaveCount(1);
            result["CH"].Should().HaveCount(1);
        }

        [Fact]
        public void Group_ByPrefix_ShorterThanPrefix_UsesFullString()
        {
            // Arrange
            var list = new List<string> { "a", "ab", "abc" };

            // Act
            var result = _logic.Group(list, ListGroupMode.ByPrefix, prefixLength: 2);

            // Assert
            result.Should().HaveCount(2);
            result["A"].Should().Equal("a");
            result["AB"].Should().Contain(new[] { "ab", "abc" });
        }

        #endregion

        #region ByRegexGroup Tests

        [Fact]
        public void Group_ByRegexGroup_ExtractsFirstGroup()
        {
            // Arrange
            var list = new List<string> { "file_2025.txt", "file_2024.txt", "doc_2025.pdf" };

            // Act
            var result = _logic.Group(list, ListGroupMode.ByRegexGroup, regexPattern: @"_(\d{4})");

            // Assert
            result.Should().HaveCount(2);
            result["2025"].Should().HaveCount(2);
            result["2024"].Should().HaveCount(1);
        }

        [Fact]
        public void Group_ByRegexGroup_NoPattern_ThrowsException()
        {
            // Arrange
            var list = new List<string> { "test" };

            // Act & Assert
            Action act = () => _logic.Group(list, ListGroupMode.ByRegexGroup);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Group_ByRegexGroup_NoMatches_ReturnsEmpty()
        {
            // Arrange
            var list = new List<string> { "test", "hello" };

            // Act
            var result = _logic.Group(list, ListGroupMode.ByRegexGroup, regexPattern: @"(\d+)");

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region TopFrequent Tests

        [Fact]
        public void Group_TopFrequent_ReturnsTopN()
        {
            // Arrange
            var list = new List<string> 
            { 
                "apple", "apple", "apple",
                "banana", "banana",
                "cherry",
                "date", "date", "date", "date"
            };

            // Act
            var result = _logic.Group(list, ListGroupMode.TopFrequent, topN: 2);

            // Assert
            result.Should().HaveCount(2);
            result.Keys.Should().Contain(k => k.StartsWith("date"));
            result.Keys.Should().Contain(k => k.StartsWith("apple"));
        }

        [Fact]
        public void Group_TopFrequent_IncludesCount()
        {
            // Arrange
            var list = new List<string> { "apple", "apple", "banana" };

            // Act
            var result = _logic.Group(list, ListGroupMode.TopFrequent, topN: 10);

            // Assert
            result.Keys.Should().Contain("apple (2)");
            result.Keys.Should().Contain("banana (1)");
        }

        [Fact]
        public void Group_TopFrequent_SkipsWhitespace()
        {
            // Arrange
            var list = new List<string> { "apple", "", "  ", null, "apple" };

            // Act
            var result = _logic.Group(list, ListGroupMode.TopFrequent);

            // Assert
            result.Should().HaveCount(1);
            result.Keys.Should().Contain("apple (2)");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Group_NullList_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.Group(null, ListGroupMode.ByFirstChar);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Group_EmptyList_ReturnsEmptyDictionary()
        {
            // Arrange
            var list = new List<string>();

            // Act
            var result = _logic.Group(list, ListGroupMode.ByFirstChar);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion
    }
}
