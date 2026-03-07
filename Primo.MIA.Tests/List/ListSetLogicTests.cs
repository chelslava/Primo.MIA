using System;
using System.Collections.Generic;
using FluentAssertions;
using Primo.MIA.Tests.Logic;
using Xunit;

namespace Primo.MIA.Tests.List
{
    public class ListSetLogicTests
    {
        private readonly ListSetLogic _logic;

        public ListSetLogicTests()
        {
            _logic = new ListSetLogic();
        }

        #region Union Tests

        [Fact]
        public void SetOperation_Union_CombinesUniquely()
        {
            // Arrange
            var listA = new List<string> { "a", "b", "c" };
            var listB = new List<string> { "c", "d", "e" };

            // Act
            var result = _logic.PerformSetOperation(listA, listB, ListSetOperation.Union);

            // Assert
            result.Should().HaveCount(5);
            result.Should().Contain(new[] { "a", "b", "c", "d", "e" });
        }

        [Fact]
        public void SetOperation_Union_RemovesDuplicates()
        {
            // Arrange
            var listA = new List<string> { "a", "b", "a" };
            var listB = new List<string> { "b", "c", "b" };

            // Act
            var result = _logic.PerformSetOperation(listA, listB, ListSetOperation.Union);

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(new[] { "a", "b", "c" });
        }

        #endregion

        #region Intersect Tests

        [Fact]
        public void SetOperation_Intersect_ReturnsCommonElements()
        {
            // Arrange
            var listA = new List<string> { "a", "b", "c", "d" };
            var listB = new List<string> { "c", "d", "e", "f" };

            // Act
            var result = _logic.PerformSetOperation(listA, listB, ListSetOperation.Intersect);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(new[] { "c", "d" });
        }

        [Fact]
        public void SetOperation_Intersect_NoCommon_ReturnsEmpty()
        {
            // Arrange
            var listA = new List<string> { "a", "b" };
            var listB = new List<string> { "c", "d" };

            // Act
            var result = _logic.PerformSetOperation(listA, listB, ListSetOperation.Intersect);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region Except Tests

        [Fact]
        public void SetOperation_Except_ReturnsAMinusB()
        {
            // Arrange
            var listA = new List<string> { "a", "b", "c", "d" };
            var listB = new List<string> { "c", "d", "e" };

            // Act
            var result = _logic.PerformSetOperation(listA, listB, ListSetOperation.Except);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(new[] { "a", "b" });
        }

        [Fact]
        public void SetOperation_ExceptReverse_ReturnsBMinusA()
        {
            // Arrange
            var listA = new List<string> { "a", "b", "c" };
            var listB = new List<string> { "c", "d", "e" };

            // Act
            var result = _logic.PerformSetOperation(listA, listB, ListSetOperation.ExceptReverse);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(new[] { "d", "e" });
        }

        #endregion

        #region SymmetricDiff Tests

        [Fact]
        public void SetOperation_SymmetricDiff_ReturnsElementsInOnlyOneList()
        {
            // Arrange
            var listA = new List<string> { "a", "b", "c" };
            var listB = new List<string> { "c", "d", "e" };

            // Act
            var result = _logic.PerformSetOperation(listA, listB, ListSetOperation.SymmetricDiff);

            // Assert
            result.Should().HaveCount(4);
            result.Should().Contain(new[] { "a", "b", "d", "e" });
            result.Should().NotContain("c");
        }

        [Fact]
        public void SetOperation_SymmetricDiff_NoCommon_ReturnsAll()
        {
            // Arrange
            var listA = new List<string> { "a", "b" };
            var listB = new List<string> { "c", "d" };

            // Act
            var result = _logic.PerformSetOperation(listA, listB, ListSetOperation.SymmetricDiff);

            // Assert
            result.Should().HaveCount(4);
            result.Should().Contain(new[] { "a", "b", "c", "d" });
        }

        #endregion

        #region Distinct Tests

        [Fact]
        public void SetOperation_Distinct_RemovesDuplicates()
        {
            // Arrange
            var listA = new List<string> { "a", "b", "a", "c", "b", "a" };

            // Act
            var result = _logic.PerformSetOperation(listA, null, ListSetOperation.Distinct);

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(new[] { "a", "b", "c" });
        }

        [Fact]
        public void SetOperation_Distinct_AlreadyUnique_ReturnsSame()
        {
            // Arrange
            var listA = new List<string> { "a", "b", "c" };

            // Act
            var result = _logic.PerformSetOperation(listA, null, ListSetOperation.Distinct);

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(new[] { "a", "b", "c" });
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void SetOperation_NullListA_ThrowsArgumentNullException()
        {
            // Arrange
            var listB = new List<string> { "a", "b" };

            // Act & Assert
            Action act = () => _logic.PerformSetOperation(null, listB, ListSetOperation.Union);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void SetOperation_NullListB_Union_ReturnsDistinctA()
        {
            // Arrange
            var listA = new List<string> { "a", "b", "a" };

            // Act
            var result = _logic.PerformSetOperation(listA, null, ListSetOperation.Union);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(new[] { "a", "b" });
        }

        [Fact]
        public void SetOperation_NullListB_Intersect_ReturnsEmpty()
        {
            // Arrange
            var listA = new List<string> { "a", "b" };

            // Act
            var result = _logic.PerformSetOperation(listA, null, ListSetOperation.Intersect);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void SetOperation_EmptyLists_ReturnsEmpty()
        {
            // Arrange
            var listA = new List<string>();
            var listB = new List<string>();

            // Act
            var result = _logic.PerformSetOperation(listA, listB, ListSetOperation.Union);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion
    }
}
