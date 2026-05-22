using System;
using System.Collections.Generic;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.List
{
    public class ListTransformLogicTests
    {
        private readonly ListTransformLogic _logic;

        public ListTransformLogicTests()
        {
            _logic = new ListTransformLogic();
        }

        [Fact]
        public void Transform_ToUpper_ConvertsToUpperCase()
        {
            // Arrange
            var list = new List<string> { "hello", "World", "TEST" };

            // Act
            var result = _logic.Transform(list, ListTransformMode.ToUpper);

            // Assert
            result.Should().Equal("HELLO", "WORLD", "TEST");
        }

        [Fact]
        public void Transform_ToLower_ConvertsToLowerCase()
        {
            // Arrange
            var list = new List<string> { "HELLO", "World", "test" };

            // Act
            var result = _logic.Transform(list, ListTransformMode.ToLower);

            // Assert
            result.Should().Equal("hello", "world", "test");
        }

        [Fact]
        public void Transform_Trim_RemovesWhitespace()
        {
            // Arrange
            var list = new List<string> { "  hello  ", "\tworld\t", "  test" };

            // Act
            var result = _logic.Transform(list, ListTransformMode.Trim);

            // Assert
            result.Should().Equal("hello", "world", "test");
        }

        [Fact]
        public void Transform_Replace_ReplacesSubstring()
        {
            // Arrange
            var list = new List<string> { "hello world", "hello there" };

            // Act
            var result = _logic.Transform(list, ListTransformMode.Replace, 
                find: "hello", replacement: "hi");

            // Assert
            result.Should().Equal("hi world", "hi there");
        }

        [Fact]
        public void Transform_Prefix_AddsPrefix()
        {
            // Arrange
            var list = new List<string> { "apple", "banana" };

            // Act
            var result = _logic.Transform(list, ListTransformMode.Prefix, prefix: "fruit_");

            // Assert
            result.Should().Equal("fruit_apple", "fruit_banana");
        }

        [Fact]
        public void Transform_Suffix_AddsSuffix()
        {
            // Arrange
            var list = new List<string> { "file1", "file2" };

            // Act
            var result = _logic.Transform(list, ListTransformMode.Suffix, suffix: ".txt");

            // Assert
            result.Should().Equal("file1.txt", "file2.txt");
        }

        [Fact]
        public void Transform_Wrap_AddsPreAndSuffix()
        {
            // Arrange
            var list = new List<string> { "test" };

            // Act
            var result = _logic.Transform(list, ListTransformMode.Wrap, 
                prefix: "[", suffix: "]");

            // Assert
            result.Should().Equal("[test]");
        }

        [Fact]
        public void Transform_PadLeft_PadsCorrectly()
        {
            // Arrange
            var list = new List<string> { "1", "22", "333" };

            // Act
            var result = _logic.Transform(list, ListTransformMode.PadLeft, 
                padWidth: 5, padChar: '0');

            // Assert
            result.Should().Equal("00001", "00022", "00333");
        }

        [Fact]
        public void Transform_Truncate_LimitsLength()
        {
            // Arrange
            var list = new List<string> { "short", "verylongstring" };

            // Act
            var result = _logic.Transform(list, ListTransformMode.Truncate, maxLength: 5);

            // Assert
            result.Should().Equal("short", "veryl");
        }

        [Fact]
        public void Transform_RemoveNumbers_RemovesDigits()
        {
            // Arrange
            var list = new List<string> { "test123", "abc456def" };

            // Act
            var result = _logic.Transform(list, ListTransformMode.RemoveNumbers);

            // Assert
            result.Should().Equal("test", "abcdef");
        }

        [Fact]
        public void Transform_NullList_ThrowsException()
        {
            // Act & Assert
            Action act = () => _logic.Transform(null, ListTransformMode.ToUpper);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Transform_ListWithNulls_HandlesGracefully()
        {
            // Arrange
            var list = new List<string> { "test", null, "hello" };

            // Act
            var result = _logic.Transform(list, ListTransformMode.ToUpper);

            // Assert
            result.Should().HaveCount(3);
            result[0].Should().Be("TEST");
            result[1].Should().BeNull();
            result[2].Should().Be("HELLO");
        }
    }
}
