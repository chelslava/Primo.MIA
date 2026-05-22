using System;
using System.Collections.Generic;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Dictionary
{
    public class DictionaryOperationsLogicTests
    {
        private readonly DictionaryOperationsLogic _logic;

        public DictionaryOperationsLogicTests()
        {
            _logic = new DictionaryOperationsLogic();
        }

        #region Merge Tests

        [Fact]
        public void Merge_KeepFirst_PreservesFirstDictionaryValues()
        {
            // Arrange
            var first = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            };
            var second = new Dictionary<string, string>
            {
                { "b", "20" },
                { "c", "3" }
            };

            // Act
            var result = _logic.Merge(first, second, DictionaryMergeStrategy.KeepFirst);

            // Assert
            result.Should().HaveCount(3);
            result["a"].Should().Be("1");
            result["b"].Should().Be("2"); // Сохранено из first
            result["c"].Should().Be("3");
        }

        [Fact]
        public void Merge_KeepSecond_OverwritesWithSecondDictionary()
        {
            // Arrange
            var first = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            };
            var second = new Dictionary<string, string>
            {
                { "b", "20" },
                { "c", "3" }
            };

            // Act
            var result = _logic.Merge(first, second, DictionaryMergeStrategy.KeepSecond);

            // Assert
            result.Should().HaveCount(3);
            result["a"].Should().Be("1");
            result["b"].Should().Be("20"); // Перезаписано из second
            result["c"].Should().Be("3");
        }

        [Fact]
        public void Merge_ThrowOnDuplicate_WithDuplicates_ThrowsException()
        {
            // Arrange
            var first = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            };
            var second = new Dictionary<string, string>
            {
                { "b", "20" },
                { "c", "3" }
            };

            // Act & Assert
            Action act = () => _logic.Merge(first, second, DictionaryMergeStrategy.ThrowOnDuplicate);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*дублирующиеся ключи*");
        }

        [Fact]
        public void Merge_ThrowOnDuplicate_NoDuplicates_Success()
        {
            // Arrange
            var first = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            };
            var second = new Dictionary<string, string>
            {
                { "c", "3" },
                { "d", "4" }
            };

            // Act
            var result = _logic.Merge(first, second, DictionaryMergeStrategy.ThrowOnDuplicate);

            // Assert
            result.Should().HaveCount(4);
        }

        [Fact]
        public void Merge_NullFirstDictionary_ThrowsArgumentNullException()
        {
            // Arrange
            var second = new Dictionary<string, string> { { "a", "1" } };

            // Act & Assert
            Action act = () => _logic.Merge(null, second, DictionaryMergeStrategy.KeepFirst);
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region Filter Tests

        [Fact]
        public void Filter_Keys_Contains_ReturnsMatchingKeys()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "apple", "red" },
                { "banana", "yellow" },
                { "apricot", "orange" }
            };

            // Act
            var result = _logic.Filter(dict, "ap", DictionaryFilterTarget.Keys, 
                DictionaryFilterMethod.Contains, caseSensitive: false);

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainKey("apple");
            result.Should().ContainKey("apricot");
        }

        [Fact]
        public void Filter_Values_Exact_ReturnsMatchingValues()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "apple", "red" },
                { "banana", "yellow" },
                { "cherry", "red" }
            };

            // Act
            var result = _logic.Filter(dict, "red", DictionaryFilterTarget.Values,
                DictionaryFilterMethod.Exact, caseSensitive: false);

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainKey("apple");
            result.Should().ContainKey("cherry");
        }

        [Fact]
        public void Filter_KeysAndValues_Contains_ReturnsMatchingEither()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "apple", "red" },
                { "banana", "yellow" },
                { "grape", "red" }
            };

            // Act
            var result = _logic.Filter(dict, "ap", DictionaryFilterTarget.KeysAndValues,
                DictionaryFilterMethod.Contains, caseSensitive: false);

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainKey("apple");
            result.Should().ContainKey("grape"); // "grape" содержит "ap" в значении? Нет, но "apple" содержит
        }

        [Fact]
        public void Filter_Wildcard_MatchesPattern()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "test1", "value1" },
                { "test2", "value2" },
                { "other", "value3" }
            };

            // Act
            var result = _logic.Filter(dict, "test*", DictionaryFilterTarget.Keys,
                DictionaryFilterMethod.Wildcard, caseSensitive: false);

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainKey("test1");
            result.Should().ContainKey("test2");
        }

        [Fact]
        public void Filter_Regex_MatchesPattern()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "test123", "value1" },
                { "test456", "value2" },
                { "test", "value3" }
            };

            // Act
            var result = _logic.Filter(dict, @"^test\d+$", DictionaryFilterTarget.Keys,
                DictionaryFilterMethod.Regex, caseSensitive: false);

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainKey("test123");
            result.Should().ContainKey("test456");
        }

        #endregion

        #region Invert Tests

        [Fact]
        public void Invert_UniqueValues_Success()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" },
                { "c", "3" }
            };

            // Act
            var result = _logic.Invert(dict, throwOnDuplicates: false);

            // Assert
            result.Should().HaveCount(3);
            result["1"].Should().Be("a");
            result["2"].Should().Be("b");
            result["3"].Should().Be("c");
        }

        [Fact]
        public void Invert_DuplicateValues_ThrowOnDuplicates_ThrowsException()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "1" },
                { "c", "2" }
            };

            // Act & Assert
            Action act = () => _logic.Invert(dict, throwOnDuplicates: true);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*дублирующиеся значения*");
        }

        [Fact]
        public void Invert_DuplicateValues_NoThrow_KeepsFirst()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "1" },
                { "c", "2" }
            };

            // Act
            var result = _logic.Invert(dict, throwOnDuplicates: false);

            // Assert
            result.Should().HaveCount(2);
            result["1"].Should().Be("a"); // Первое вхождение
            result["2"].Should().Be("c");
        }

        [Fact]
        public void Invert_EmptyDictionary_ReturnsEmpty()
        {
            // Arrange
            var dict = new Dictionary<string, string>();

            // Act
            var result = _logic.Invert(dict);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion
    }
}
