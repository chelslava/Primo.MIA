using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.List
{
    public class ListSortLogicTests
    {
        private readonly ListSortLogic _logic;

        public ListSortLogicTests()
        {
            _logic = new ListSortLogic();
        }

        #region Alphabetical Tests

        [Fact]
        public void Sort_Alphabetical_SortsCorrectly()
        {
            // Arrange
            var list = new List<string> { "banana", "apple", "cherry", "Apple" };

            // Act
            var result = _logic.Sort(list, ListSortMode.Alphabetical);

            // Assert
            result.Should().Equal("Apple", "apple", "banana", "cherry");
        }

        [Fact]
        public void Sort_AlphabeticalDesc_SortsInReverse()
        {
            // Arrange
            var list = new List<string> { "banana", "apple", "cherry" };

            // Act
            var result = _logic.Sort(list, ListSortMode.AlphabeticalDesc);

            // Assert
            result.Should().Equal("cherry", "banana", "apple");
        }

        #endregion

        #region Case Insensitive Tests

        [Fact]
        public void Sort_CaseInsensitive_IgnoresCase()
        {
            // Arrange
            var list = new List<string> { "banana", "Apple", "CHERRY", "apple" };

            // Act
            var result = _logic.Sort(list, ListSortMode.CaseInsensitive);

            // Assert
            // Apple и apple должны быть рядом
            result[0].ToLower().Should().Be("apple");
            result[1].ToLower().Should().Be("apple");
            result[2].ToLower().Should().Be("banana");
            result[3].ToLower().Should().Be("cherry");
        }

        [Fact]
        public void Sort_CaseInsensitiveDesc_IgnoresCaseAndReverse()
        {
            // Arrange
            var list = new List<string> { "banana", "Apple", "cherry" };

            // Act
            var result = _logic.Sort(list, ListSortMode.CaseInsensitiveDesc);

            // Assert
            result[0].ToLower().Should().Be("cherry");
            result[1].ToLower().Should().Be("banana");
            result[2].ToLower().Should().Be("apple");
        }

        #endregion

        #region By Length Tests

        [Fact]
        public void Sort_ByLength_SortsByStringLength()
        {
            // Arrange
            var list = new List<string> { "banana", "a", "cherry", "ab" };

            // Act
            var result = _logic.Sort(list, ListSortMode.ByLength);

            // Assert
            result.Should().Equal("a", "ab", "banana", "cherry");
        }

        [Fact]
        public void Sort_ByLengthDesc_SortsByLengthDescending()
        {
            // Arrange
            var list = new List<string> { "a", "banana", "ab", "cherry" };

            // Act
            var result = _logic.Sort(list, ListSortMode.ByLengthDesc);

            // Assert
            result.Should().Equal("banana", "cherry", "ab", "a");
        }

        [Fact]
        public void Sort_ByLength_WithNulls_HandlesGracefully()
        {
            // Arrange
            var list = new List<string> { "banana", null, "a", null };

            // Act
            var result = _logic.Sort(list, ListSortMode.ByLength);

            // Assert
            result.Should().HaveCount(4);
            result[0].Should().BeNull();
            result[1].Should().BeNull();
        }

        #endregion

        #region Natural Sort Tests

        [Fact]
        public void Sort_Natural_SortsNumbersNaturally()
        {
            // Arrange
            var list = new List<string> { "file10.txt", "file2.txt", "file1.txt", "file20.txt" };

            // Act
            var result = _logic.Sort(list, ListSortMode.Natural);

            // Assert
            result.Should().Equal("file1.txt", "file2.txt", "file10.txt", "file20.txt");
        }

        [Fact]
        public void Sort_Natural_HandlesMultipleNumbers()
        {
            // Arrange
            var list = new List<string> { "v1.10.2", "v1.2.1", "v1.10.1", "v1.2.10" };

            // Act
            var result = _logic.Sort(list, ListSortMode.Natural);

            // Assert
            result.Should().Equal("v1.2.1", "v1.2.10", "v1.10.1", "v1.10.2");
        }

        [Fact]
        public void Sort_Natural_MixedContent()
        {
            // Arrange
            var list = new List<string> { "test", "test10", "test2", "test1" };

            // Act
            var result = _logic.Sort(list, ListSortMode.Natural);

            // Assert
            result.Should().Equal("test", "test1", "test2", "test10");
        }

        #endregion

        #region Reverse Tests

        [Fact]
        public void Sort_Reverse_ReversesOrder()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d" };

            // Act
            var result = _logic.Sort(list, ListSortMode.Reverse);

            // Assert
            result.Should().Equal("d", "c", "b", "a");
        }

        [Fact]
        public void Sort_Reverse_EmptyList_ReturnsEmpty()
        {
            // Arrange
            var list = new List<string>();

            // Act
            var result = _logic.Sort(list, ListSortMode.Reverse);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region Random Tests

        [Fact]
        public void Sort_Random_ChangesOrder()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" };
            var original = new List<string>(list);

            // Act
            var result = _logic.Sort(list, ListSortMode.Random);

            // Assert
            result.Should().HaveCount(original.Count);
            result.Should().Contain(original); // Все элементы присутствуют
            
            // С высокой вероятностью порядок изменится
            // (вероятность что останется тот же = 1/8! ≈ 0.0025%)
            bool orderChanged = !result.SequenceEqual(original);
            orderChanged.Should().BeTrue("порядок должен измениться при случайной сортировке");
        }

        [Fact]
        public void Sort_Random_PreservesAllElements()
        {
            // Arrange
            var list = new List<string> { "apple", "banana", "cherry", "date" };

            // Act
            var result = _logic.Sort(list, ListSortMode.Random);

            // Assert
            result.Should().HaveCount(4);
            result.Should().Contain("apple");
            result.Should().Contain("banana");
            result.Should().Contain("cherry");
            result.Should().Contain("date");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Sort_NullList_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.Sort(null, ListSortMode.Alphabetical);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Sort_EmptyList_ReturnsEmpty()
        {
            // Arrange
            var list = new List<string>();

            // Act
            var result = _logic.Sort(list, ListSortMode.Alphabetical);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Sort_SingleElement_ReturnsSameElement()
        {
            // Arrange
            var list = new List<string> { "only" };

            // Act
            var result = _logic.Sort(list, ListSortMode.Alphabetical);

            // Assert
            result.Should().Equal("only");
        }

        [Fact]
        public void Sort_DoesNotModifyOriginal()
        {
            // Arrange
            var list = new List<string> { "c", "a", "b" };
            var original = new List<string>(list);

            // Act
            var result = _logic.Sort(list, ListSortMode.Alphabetical);

            // Assert
            list.Should().Equal(original, "оригинальный список не должен изменяться");
            result.Should().NotEqual(original, "результат должен быть отсортирован");
        }

        #endregion
    }
}
