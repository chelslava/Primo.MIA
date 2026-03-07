using System;
using System.Collections.Generic;
using FluentAssertions;
using Primo.MIA.Tests.Logic;
using Xunit;

namespace Primo.MIA.Tests.List
{
    public class ListAggregateLogicTests
    {
        private readonly ListAggregateLogic _logic;

        public ListAggregateLogicTests()
        {
            _logic = new ListAggregateLogic();
        }

        #region Count Tests

        [Fact]
        public void Aggregate_Count_ReturnsTotal()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "", null };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.Count);

            // Assert
            result.Should().Be(5);
        }

        [Fact]
        public void Aggregate_CountDistinct_ReturnsUniqueNonEmpty()
        {
            // Arrange
            var list = new List<string> { "apple", "banana", "apple", "", "banana", null, "cherry" };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.CountDistinct);

            // Assert
            result.Should().Be(3); // apple, banana, cherry
        }

        [Fact]
        public void Aggregate_CountNonEmpty_ExcludesEmptyAndNull()
        {
            // Arrange
            var list = new List<string> { "apple", "", "banana", null, "  ", "cherry" };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.CountNonEmpty);

            // Assert
            result.Should().Be(3); // apple, banana, cherry
        }

        #endregion

        #region Numeric Aggregation Tests

        [Fact]
        public void Aggregate_Sum_AddsNumericValues()
        {
            // Arrange
            var list = new List<string> { "10", "20", "30", "abc" };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.Sum);

            // Assert
            result.Should().Be(60.0);
        }

        [Fact]
        public void Aggregate_Sum_HandlesDecimals()
        {
            // Arrange
            var list = new List<string> { "10.5", "20.3", "5.2" };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.Sum);

            // Assert
            ((double)result).Should().BeApproximately(36.0, 0.01);
        }

        [Fact]
        public void Aggregate_Min_ReturnsSmallest()
        {
            // Arrange
            var list = new List<string> { "100", "5", "50", "abc", "10" };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.Min);

            // Assert
            result.Should().Be(5.0);
        }

        [Fact]
        public void Aggregate_Max_ReturnsLargest()
        {
            // Arrange
            var list = new List<string> { "100", "5", "50", "abc", "10" };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.Max);

            // Assert
            result.Should().Be(100.0);
        }

        [Fact]
        public void Aggregate_Average_ReturnsAverage()
        {
            // Arrange
            var list = new List<string> { "10", "20", "30" };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.Average);

            // Assert
            result.Should().Be(20.0);
        }

        [Fact]
        public void Aggregate_Sum_NoNumericValues_ReturnsZero()
        {
            // Arrange
            var list = new List<string> { "abc", "def", "xyz" };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.Sum);

            // Assert
            result.Should().Be(0.0);
        }

        #endregion

        #region String Length Tests

        [Fact]
        public void Aggregate_ShortestString_ReturnsShortestNonEmpty()
        {
            // Arrange
            var list = new List<string> { "apple", "a", "banana", "", "ab" };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.ShortestString);

            // Assert
            result.Should().Be("a");
        }

        [Fact]
        public void Aggregate_LongestString_ReturnsLongestNonEmpty()
        {
            // Arrange
            var list = new List<string> { "apple", "a", "banana", "", "ab" };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.LongestString);

            // Assert
            result.Should().Be("banana");
        }

        [Fact]
        public void Aggregate_ShortestString_EmptyList_ReturnsEmpty()
        {
            // Arrange
            var list = new List<string> { "", null, "  " };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.ShortestString);

            // Assert
            result.Should().Be(string.Empty);
        }

        #endregion

        #region Join Tests

        [Fact]
        public void Aggregate_Join_DefaultSeparator()
        {
            // Arrange
            var list = new List<string> { "apple", "banana", "cherry" };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.Join);

            // Assert
            result.Should().Be("apple, banana, cherry");
        }

        [Fact]
        public void Aggregate_Join_CustomSeparator()
        {
            // Arrange
            var list = new List<string> { "apple", "banana", "cherry" };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.Join, separator: " | ");

            // Assert
            result.Should().Be("apple | banana | cherry");
        }

        [Fact]
        public void Aggregate_Join_SkipsNulls()
        {
            // Arrange
            var list = new List<string> { "apple", null, "banana", null, "cherry" };

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.Join);

            // Assert
            result.Should().Be("apple, banana, cherry");
        }

        [Fact]
        public void Aggregate_Join_EmptyList_ReturnsEmpty()
        {
            // Arrange
            var list = new List<string>();

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.Join);

            // Assert
            result.Should().Be(string.Empty);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Aggregate_NullList_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.Aggregate(null, ListAggregateMode.Count);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Aggregate_EmptyList_Count_ReturnsZero()
        {
            // Arrange
            var list = new List<string>();

            // Act
            var result = _logic.Aggregate(list, ListAggregateMode.Count);

            // Assert
            result.Should().Be(0);
        }

        #endregion
    }
}
