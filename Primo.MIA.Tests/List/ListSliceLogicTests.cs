using System;
using System.Collections.Generic;
using FluentAssertions;
using Primo.MIA.Tests.Logic;
using Xunit;

namespace Primo.MIA.Tests.List
{
    public class ListSliceLogicTests
    {
        private readonly ListSliceLogic _logic;

        public ListSliceLogicTests()
        {
            _logic = new ListSliceLogic();
        }

        #region FirstN / LastN Tests

        [Fact]
        public void Slice_FirstN_ReturnsFirstElements()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d", "e" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.FirstN, count: 3);

            // Assert
            result.Should().Equal("a", "b", "c");
        }

        [Fact]
        public void Slice_FirstN_CountExceedsLength_ReturnsAll()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.FirstN, count: 10);

            // Assert
            result.Should().Equal("a", "b", "c");
        }

        [Fact]
        public void Slice_LastN_ReturnsLastElements()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d", "e" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.LastN, count: 3);

            // Assert
            result.Should().Equal("c", "d", "e");
        }

        #endregion

        #region SkipFirst / SkipLast Tests

        [Fact]
        public void Slice_SkipFirst_SkipsFirstElements()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d", "e" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.SkipFirst, count: 2);

            // Assert
            result.Should().Equal("c", "d", "e");
        }

        [Fact]
        public void Slice_SkipLast_SkipsLastElements()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d", "e" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.SkipLast, count: 2);

            // Assert
            result.Should().Equal("a", "b", "c");
        }

        #endregion

        #region Page Tests

        [Fact]
        public void Slice_Page_FirstPage()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.Page, page: 1, pageSize: 3);

            // Assert
            result.Should().Equal("a", "b", "c");
        }

        [Fact]
        public void Slice_Page_SecondPage()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.Page, page: 2, pageSize: 3);

            // Assert
            result.Should().Equal("d", "e", "f");
        }

        [Fact]
        public void Slice_Page_LastPagePartial()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d", "e" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.Page, page: 2, pageSize: 3);

            // Assert
            result.Should().Equal("d", "e");
        }

        [Fact]
        public void Slice_Page_BeyondEnd_ReturnsEmpty()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.Page, page: 5, pageSize: 3);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region Range Tests

        [Fact]
        public void Slice_Range_ReturnsSublist()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d", "e" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.Range, fromIndex: 1, toIndex: 4);

            // Assert
            result.Should().Equal("b", "c", "d");
        }

        [Fact]
        public void Slice_Range_FromZero()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d", "e" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.Range, fromIndex: 0, toIndex: 3);

            // Assert
            result.Should().Equal("a", "b", "c");
        }

        [Fact]
        public void Slice_Range_ToEnd()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d", "e" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.Range, fromIndex: 2, toIndex: 5);

            // Assert
            result.Should().Equal("c", "d", "e");
        }

        [Fact]
        public void Slice_Range_InvalidRange_ReturnsEmpty()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.Range, fromIndex: 3, toIndex: 2);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region EveryNth Tests

        [Fact]
        public void Slice_EveryNth_Step2_ReturnsEvenIndices()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d", "e", "f" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.EveryNth, step: 2);

            // Assert
            result.Should().Equal("a", "c", "e");
        }

        [Fact]
        public void Slice_EveryNth_Step3()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h", "i" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.EveryNth, step: 3);

            // Assert
            result.Should().Equal("a", "d", "g");
        }

        [Fact]
        public void Slice_EveryNth_Step1_ReturnsAll()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.EveryNth, step: 1);

            // Assert
            result.Should().Equal("a", "b", "c");
        }

        [Fact]
        public void Slice_EveryNth_InvalidStep_ThrowsException()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c" };

            // Act & Assert
            Action act = () => _logic.Slice(list, ListSliceMode.EveryNth, step: 0);
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Slice_NullList_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.Slice(null, ListSliceMode.FirstN, count: 5);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Slice_EmptyList_ReturnsEmpty()
        {
            // Arrange
            var list = new List<string>();

            // Act
            var result = _logic.Slice(list, ListSliceMode.FirstN, count: 5);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Slice_NegativeCount_ReturnsEmpty()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c" };

            // Act
            var result = _logic.Slice(list, ListSliceMode.FirstN, count: -5);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion
    }
}
