using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.List
{
    public class ListConvertLogicTests
    {
        private readonly ListConvertLogic _logic;

        public ListConvertLogicTests()
        {
            _logic = new ListConvertLogic();
        }

        #region ToDict Tests

        [Fact]
        public void Convert_ToDict_ParsesKeyValuePairs()
        {
            // Arrange
            var list = new List<string> { "name=John", "age=30", "city=Moscow" };

            // Act
            var result = _logic.Convert(list, ListConvertMode.ToDict) as Dictionary<string, string>;

            // Assert
            result.Should().HaveCount(3);
            result["name"].Should().Be("John");
            result["age"].Should().Be("30");
            result["city"].Should().Be("Moscow");
        }

        [Fact]
        public void Convert_ToDict_CustomSeparator()
        {
            // Arrange
            var list = new List<string> { "name:John", "age:30" };

            // Act
            var result = _logic.Convert(list, ListConvertMode.ToDict, separator: ":") as Dictionary<string, string>;

            // Assert
            result.Should().HaveCount(2);
            result["name"].Should().Be("John");
        }

        [Fact]
        public void Convert_ToDict_TrimsWhitespace()
        {
            // Arrange
            var list = new List<string> { "  name  =  John  " };

            // Act
            var result = _logic.Convert(list, ListConvertMode.ToDict) as Dictionary<string, string>;

            // Assert
            result["name"].Should().Be("John");
        }

        [Fact]
        public void Convert_ToDict_SkipsInvalidLines()
        {
            // Arrange
            var list = new List<string> { "name=John", "invalid", "age=30" };

            // Act
            var result = _logic.Convert(list, ListConvertMode.ToDict) as Dictionary<string, string>;

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainKey("name");
            result.Should().ContainKey("age");
        }

        #endregion

        #region ToDictIndexed Tests

        [Fact]
        public void Convert_ToDictIndexed_CreatesIndexedDictionary()
        {
            // Arrange
            var list = new List<string> { "apple", "banana", "cherry" };

            // Act
            var result = _logic.Convert(list, ListConvertMode.ToDictIndexed) as Dictionary<string, string>;

            // Assert
            result.Should().HaveCount(3);
            result["0"].Should().Be("apple");
            result["1"].Should().Be("banana");
            result["2"].Should().Be("cherry");
        }

        [Fact]
        public void Convert_ToDictIndexed_HandlesNulls()
        {
            // Arrange
            var list = new List<string> { "apple", null, "cherry" };

            // Act
            var result = _logic.Convert(list, ListConvertMode.ToDictIndexed) as Dictionary<string, string>;

            // Assert
            result.Should().HaveCount(3);
            result["1"].Should().Be(string.Empty);
        }

        #endregion

        #region ToCSVRow Tests

        [Fact]
        public void Convert_ToCSVRow_CreatesCSVString()
        {
            // Arrange
            var list = new List<string> { "apple", "banana", "cherry" };

            // Act
            var result = _logic.Convert(list, ListConvertMode.ToCSVRow) as string;

            // Assert
            result.Should().Be("\"apple\",\"banana\",\"cherry\"");
        }

        [Fact]
        public void Convert_ToCSVRow_EscapesQuotes()
        {
            // Arrange
            var list = new List<string> { "say \"hello\"", "world" };

            // Act
            var result = _logic.Convert(list, ListConvertMode.ToCSVRow) as string;

            // Assert
            result.Should().Be("\"say \"\"hello\"\"\",\"world\"");
        }

        [Fact]
        public void Convert_ToCSVRow_CustomSeparator()
        {
            // Arrange
            var list = new List<string> { "apple", "banana" };

            // Act
            var result = _logic.Convert(list, ListConvertMode.ToCSVRow, csvSeparator: ";") as string;

            // Assert
            result.Should().Be("\"apple\";\"banana\"");
        }

        #endregion

        #region FromCSVRow Tests

        [Fact]
        public void Convert_FromCSVRow_ParsesCSVString()
        {
            // Arrange
            var list = new List<string> { "\"apple\",\"banana\",\"cherry\"" };

            // Act
            var result = _logic.Convert(list, ListConvertMode.FromCSVRow) as List<string>;

            // Assert
            result.Should().HaveCount(3);
            result.Should().Equal("apple", "banana", "cherry");
        }

        [Fact]
        public void Convert_FromCSVRow_HandlesEscapedQuotes()
        {
            // Arrange
            var list = new List<string> { "\"say \"\"hello\"\"\",\"world\"" };

            // Act
            var result = _logic.Convert(list, ListConvertMode.FromCSVRow) as List<string>;

            // Assert
            result.Should().HaveCount(2);
            result[0].Should().Be("say \"hello\"");
            result[1].Should().Be("world");
        }

        [Fact]
        public void Convert_FromCSVRow_EmptyList_ReturnsEmpty()
        {
            // Arrange
            var list = new List<string>();

            // Act
            var result = _logic.Convert(list, ListConvertMode.FromCSVRow) as List<string>;

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region ZipToDict Tests

        [Fact]
        public void Convert_ZipToDict_CombinesTwoLists()
        {
            // Arrange
            var keys = new List<string> { "name", "age", "city" };
            var values = new List<string> { "John", "30", "Moscow" };

            // Act
            var result = _logic.Convert(keys, ListConvertMode.ZipToDict, secondList: values) as Dictionary<string, string>;

            // Assert
            result.Should().HaveCount(3);
            result["name"].Should().Be("John");
            result["age"].Should().Be("30");
            result["city"].Should().Be("Moscow");
        }

        [Fact]
        public void Convert_ZipToDict_DifferentLengths_UsesMinimum()
        {
            // Arrange
            var keys = new List<string> { "a", "b", "c" };
            var values = new List<string> { "1", "2" };

            // Act
            var result = _logic.Convert(keys, ListConvertMode.ZipToDict, secondList: values) as Dictionary<string, string>;

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public void Convert_ZipToDict_NullSecondList_ThrowsException()
        {
            // Arrange
            var list = new List<string> { "a", "b" };

            // Act & Assert
            Action act = () => _logic.Convert(list, ListConvertMode.ZipToDict);
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region Flatten Tests

        [Fact]
        public void Convert_Flatten_SplitsAndFlattens()
        {
            // Arrange
            var list = new List<string> { "a,b,c", "d,e", "f" };

            // Act
            var result = _logic.Convert(list, ListConvertMode.Flatten, separator: ",") as List<string>;

            // Assert
            result.Should().HaveCount(6);
            result.Should().Equal("a", "b", "c", "d", "e", "f");
        }

        [Fact]
        public void Convert_Flatten_TrimsElements()
        {
            // Arrange
            var list = new List<string> { " a , b , c " };

            // Act
            var result = _logic.Convert(list, ListConvertMode.Flatten, separator: ",") as List<string>;

            // Assert
            result.Should().Equal("a", "b", "c");
        }

        [Fact]
        public void Convert_Flatten_CustomSeparator()
        {
            // Arrange
            var list = new List<string> { "a|b|c" };

            // Act
            var result = _logic.Convert(list, ListConvertMode.Flatten, separator: "|") as List<string>;

            // Assert
            result.Should().Equal("a", "b", "c");
        }

        #endregion

        #region Chunk Tests

        [Fact]
        public void Convert_Chunk_SplitsIntoChunks()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g" };

            // Act
            var result = _logic.Convert(list, ListConvertMode.Chunk, chunkSize: 3) as List<List<string>>;

            // Assert
            result.Should().HaveCount(3);
            result[0].Should().Equal("a", "b", "c");
            result[1].Should().Equal("d", "e", "f");
            result[2].Should().Equal("g");
        }

        [Fact]
        public void Convert_Chunk_ExactDivision()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c", "d" };

            // Act
            var result = _logic.Convert(list, ListConvertMode.Chunk, chunkSize: 2) as List<List<string>>;

            // Assert
            result.Should().HaveCount(2);
            result[0].Should().Equal("a", "b");
            result[1].Should().Equal("c", "d");
        }

        [Fact]
        public void Convert_Chunk_InvalidSize_ThrowsException()
        {
            // Arrange
            var list = new List<string> { "a", "b" };

            // Act & Assert
            Action act = () => _logic.Convert(list, ListConvertMode.Chunk, chunkSize: 0);
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Convert_NullList_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.Convert(null, ListConvertMode.ToDict);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Convert_EmptyList_ReturnsAppropriateEmpty()
        {
            // Arrange
            var list = new List<string>();

            // Act
            var dictResult = _logic.Convert(list, ListConvertMode.ToDict) as Dictionary<string, string>;
            var csvResult = _logic.Convert(list, ListConvertMode.ToCSVRow) as string;

            // Assert
            dictResult.Should().BeEmpty();
            csvResult.Should().Be(string.Empty);
        }

        #endregion
    }
}
