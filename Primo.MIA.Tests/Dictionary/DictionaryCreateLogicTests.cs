using System;
using System.Collections.Generic;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Dictionary
{
    /// <summary>
    /// Тесты для бизнес-логики создания словарей.
    /// </summary>
    public class DictionaryCreateLogicTests
    {
        private readonly DictionaryCreateLogic _logic;

        public DictionaryCreateLogicTests()
        {
            _logic = new DictionaryCreateLogic();
        }

        #region CreateEmpty Tests

        [Fact]
        public void CreateEmpty_ReturnsEmptyDictionary()
        {
            // Act
            var result = _logic.CreateEmpty();

            // Assert
            result.Dictionary.Should().NotBeNull();
            result.Dictionary.Should().BeEmpty();
            result.Count.Should().Be(0);
            result.DuplicatesSkipped.Should().Be(0);
        }

        [Fact]
        public void CreateEmpty_ReturnsNewInstanceEachTime()
        {
            // Act
            var result1 = _logic.CreateEmpty();
            var result2 = _logic.CreateEmpty();

            // Assert
            result1.Dictionary.Should().NotBeSameAs(result2.Dictionary);
        }

        #endregion

        #region FromLists Tests

        [Fact]
        public void FromLists_ValidLists_ReturnsDictionary()
        {
            // Arrange
            var keys = new List<string> { "a", "b", "c" };
            var values = new List<string> { "1", "2", "3" };

            // Act
            var result = _logic.FromLists(keys, values);

            // Assert
            result.Dictionary.Should().HaveCount(3);
            result.Dictionary["a"].Should().Be("1");
            result.Dictionary["b"].Should().Be("2");
            result.Dictionary["c"].Should().Be("3");
            result.Count.Should().Be(3);
        }

        [Fact]
        public void FromLists_EmptyLists_ReturnsEmptyDictionary()
        {
            // Arrange
            var keys = new List<string>();
            var values = new List<string>();

            // Act
            var result = _logic.FromLists(keys, values);

            // Assert
            result.Dictionary.Should().BeEmpty();
            result.Count.Should().Be(0);
        }

        [Fact]
        public void FromLists_DuplicateKeys_LastValueWins()
        {
            // Arrange
            var keys = new List<string> { "a", "a", "b" };
            var values = new List<string> { "1", "2", "3" };

            // Act
            var result = _logic.FromLists(keys, values);

            // Assert
            result.Dictionary.Should().HaveCount(2);
            result.Dictionary["a"].Should().Be("2"); // Последнее значение
            result.Dictionary["b"].Should().Be("3");
        }

        [Fact]
        public void FromLists_DifferentLengths_ThrowsArgumentException()
        {
            // Arrange
            var keys = new List<string> { "a", "b" };
            var values = new List<string> { "1", "2", "3" };

            // Act & Assert
            Action act = () => _logic.FromLists(keys, values);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*длина*не совпадает*");
        }

        [Fact]
        public void FromLists_NullKeys_ThrowsArgumentNullException()
        {
            // Arrange
            var values = new List<string> { "1", "2" };

            // Act & Assert
            Action act = () => _logic.FromLists(null, values);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("keysList");
        }

        [Fact]
        public void FromLists_NullValues_ThrowsArgumentNullException()
        {
            // Arrange
            var keys = new List<string> { "a", "b" };

            // Act & Assert
            Action act = () => _logic.FromLists(keys, null);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("valuesList");
        }

        [Fact]
        public void FromLists_WithNullKeys_ThrowsArgumentNullException()
        {
            // Arrange
            var keys = new List<string> { "a", null, "c" };
            var values = new List<string> { "1", "2", "3" };

            // Act & Assert
            // Dictionary<string, string> не может содержать null как ключ
            Action act = () => _logic.FromLists(keys, values);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void FromLists_WithNullValues_HandlesNullValues()
        {
            // Arrange
            var keys = new List<string> { "a", "b", "c" };
            var values = new List<string> { "1", null, "3" };

            // Act
            var result = _logic.FromLists(keys, values);

            // Assert
            result.Dictionary.Should().HaveCount(3);
            result.Dictionary["b"].Should().BeNull();
        }

        #endregion

        #region Clone Tests

        [Fact]
        public void Clone_ValidDictionary_ReturnsIndependentCopy()
        {
            // Arrange
            var original = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            };

            // Act
            var result = _logic.Clone(original);

            // Assert
            result.Dictionary.Should().NotBeSameAs(original);
            result.Dictionary.Should().HaveCount(2);
            result.Dictionary["a"].Should().Be("1");
            result.Dictionary["b"].Should().Be("2");
        }

        [Fact]
        public void Clone_ModifyOriginal_DoesNotAffectClone()
        {
            // Arrange
            var original = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            };
            var result = _logic.Clone(original);

            // Act
            original["a"] = "modified";
            original.Add("c", "3");

            // Assert
            result.Dictionary["a"].Should().Be("1"); // Не изменился
            result.Dictionary.Should().HaveCount(2); // Не добавился "c"
        }

        [Fact]
        public void Clone_ModifyClone_DoesNotAffectOriginal()
        {
            // Arrange
            var original = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            };
            var result = _logic.Clone(original);

            // Act
            result.Dictionary["a"] = "modified";
            result.Dictionary.Add("c", "3");

            // Assert
            original["a"].Should().Be("1"); // Не изменился
            original.Should().HaveCount(2); // Не добавился "c"
        }

        [Fact]
        public void Clone_EmptyDictionary_ReturnsEmptyCopy()
        {
            // Arrange
            var original = new Dictionary<string, string>();

            // Act
            var result = _logic.Clone(original);

            // Assert
            result.Dictionary.Should().BeEmpty();
            result.Count.Should().Be(0);
        }

        [Fact]
        public void Clone_NullDictionary_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.Clone(null);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("source");
        }

        #endregion

        #region Invert Tests

        [Fact]
        public void Invert_UniqueValues_ReturnsInvertedDictionary()
        {
            // Arrange
            var original = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" },
                { "c", "3" }
            };

            // Act
            var result = _logic.Invert(original, throwOnDuplicates: false);

            // Assert
            result.Dictionary.Should().HaveCount(3);
            result.Dictionary["1"].Should().Be("a");
            result.Dictionary["2"].Should().Be("b");
            result.Dictionary["3"].Should().Be("c");
            result.DuplicatesSkipped.Should().Be(0);
        }

        [Fact]
        public void Invert_DuplicateValues_ThrowOnDuplicates_ThrowsInvalidOperationException()
        {
            // Arrange
            var original = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "1" },
                { "c", "2" }
            };

            // Act & Assert
            Action act = () => _logic.Invert(original, throwOnDuplicates: true);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*дублирующиеся значения*");
        }

        [Fact]
        public void Invert_DuplicateValues_NoThrow_KeepsFirstOccurrence()
        {
            // Arrange
            var original = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "1" },
                { "c", "2" }
            };

            // Act
            var result = _logic.Invert(original, throwOnDuplicates: false);

            // Assert
            result.Dictionary.Should().HaveCount(2);
            result.Dictionary["1"].Should().Be("a"); // Первое вхождение
            result.Dictionary["2"].Should().Be("c");
            result.DuplicatesSkipped.Should().Be(1);
        }

        [Fact]
        public void Invert_EmptyDictionary_ReturnsEmpty()
        {
            // Arrange
            var original = new Dictionary<string, string>();

            // Act
            var result = _logic.Invert(original);

            // Assert
            result.Dictionary.Should().BeEmpty();
            result.Count.Should().Be(0);
            result.DuplicatesSkipped.Should().Be(0);
        }

        [Fact]
        public void Invert_NullDictionary_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.Invert(null);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("source");
        }

        [Fact]
        public void Invert_MultipleDuplicates_TracksSkippedCount()
        {
            // Arrange
            var original = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "1" },
                { "c", "1" },
                { "d", "2" },
                { "e", "2" }
            };

            // Act
            var result = _logic.Invert(original, throwOnDuplicates: false);

            // Assert
            result.Dictionary.Should().HaveCount(2);
            result.DuplicatesSkipped.Should().Be(3); // 2 дубля "1" + 1 дубль "2"
        }

        [Fact]
        public void Invert_WithNullValues_ThrowsArgumentException()
        {
            // Arrange
            var original = new Dictionary<string, string>
            {
                { "a", null },
                { "b", "1" }
            };

            // Act & Assert
            // Dictionary<string, string> не может содержать null как ключ,
            // поэтому инверсия с null-значениями невозможна
            Action act = () => _logic.Invert(original, throwOnDuplicates: false);
            act.Should().Throw<ArgumentException>();
        }

        #endregion
    }
}
