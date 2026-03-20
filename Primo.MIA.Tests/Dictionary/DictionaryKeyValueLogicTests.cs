using System;
using System.Collections.Generic;
using FluentAssertions;
using Primo.MIA.Tests.Logic;
using Xunit;

namespace Primo.MIA.Tests.Dictionary
{
    /// <summary>
    /// Тесты для бизнес-логики операций с ключами и значениями словаря.
    /// </summary>
    public class DictionaryKeyValueLogicTests
    {
        private readonly DictionaryKeyValueLogic _logic;

        public DictionaryKeyValueLogicTests()
        {
            _logic = new DictionaryKeyValueLogic();
        }

        #region GetValue Tests

        [Fact]
        public void GetValue_ExistingKey_ReturnsValueAndFoundTrue()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            };

            // Act
            var result = _logic.GetValue(dict, "a");

            // Assert
            result.Value.Should().Be("1");
            result.Found.Should().BeTrue();
        }

        [Fact]
        public void GetValue_NonExistingKey_ReturnsDefaultAndFoundFalse()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "a", "1" }
            };

            // Act
            var result = _logic.GetValue(dict, "missing", defaultValue: "default");

            // Assert
            result.Value.Should().Be("default");
            result.Found.Should().BeFalse();
        }

        [Fact]
        public void GetValue_NonExistingKey_ThrowIfNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "a", "1" }
            };

            // Act & Assert
            Action act = () => _logic.GetValue(dict, "missing", throwIfNotFound: true);
            act.Should().Throw<KeyNotFoundException>()
                .WithMessage("*ключ*не найден*");
        }

        [Fact]
        public void GetValue_NullDictionary_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.GetValue(null, "key");
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("dictionary");
        }

        [Fact]
        public void GetValue_EmptyKey_ThrowsArgumentException()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act & Assert
            Action act = () => _logic.GetValue(dict, "");
            act.Should().Throw<ArgumentException>()
                .WithParameterName("key");
        }

        [Fact]
        public void GetValue_NullKey_ThrowsArgumentException()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act & Assert
            Action act = () => _logic.GetValue(dict, null);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetValue_NullDefaultValue_ReturnsEmptyString()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act
            var result = _logic.GetValue(dict, "missing", defaultValue: null);

            // Assert
            result.Value.Should().Be(string.Empty);
        }

        #endregion

        #region SetValue Tests

        [Fact]
        public void SetValue_NewKey_ReturnsDictionaryWithNewKey()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act
            var result = _logic.SetValue(dict, "b", "2");

            // Assert
            result.Dictionary.Should().HaveCount(2);
            result.Dictionary["b"].Should().Be("2");
            result.IsUpdate.Should().BeFalse();
        }

        [Fact]
        public void SetValue_ExistingKey_ReturnsDictionaryWithUpdatedValue()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act
            var result = _logic.SetValue(dict, "a", "updated");

            // Assert
            result.Dictionary["a"].Should().Be("updated");
            result.IsUpdate.Should().BeTrue();
        }

        [Fact]
        public void SetValue_DoesNotModifyOriginal()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act
            var result = _logic.SetValue(dict, "a", "updated");

            // Assert
            dict["a"].Should().Be("1"); // Оригинал не изменился
            result.Dictionary["a"].Should().Be("updated");
        }

        [Fact]
        public void SetValue_NullDictionary_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.SetValue(null, "key", "value");
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("dictionary");
        }

        [Fact]
        public void SetValue_EmptyKey_ThrowsArgumentException()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act & Assert
            Action act = () => _logic.SetValue(dict, "", "value");
            act.Should().Throw<ArgumentException>()
                .WithParameterName("key");
        }

        [Fact]
        public void SetValue_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act & Assert
            Action act = () => _logic.SetValue(dict, "key", null);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("value");
        }

        #endregion

        #region ContainsKey Tests

        [Fact]
        public void ContainsKey_ExistingKey_ReturnsFoundTrue()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act
            var result = _logic.ContainsKey(dict, "a");

            // Assert
            result.Found.Should().BeTrue();
            result.Count.Should().Be(1);
        }

        [Fact]
        public void ContainsKey_NonExistingKey_ReturnsFoundFalse()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act
            var result = _logic.ContainsKey(dict, "missing");

            // Assert
            result.Found.Should().BeFalse();
            result.Count.Should().Be(0);
        }

        [Fact]
        public void ContainsKey_NullDictionary_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.ContainsKey(null, "key");
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("dictionary");
        }

        [Fact]
        public void ContainsKey_EmptyKey_ReturnsFoundFalse()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act
            var result = _logic.ContainsKey(dict, "");

            // Assert
            result.Found.Should().BeFalse();
        }

        [Fact]
        public void ContainsKey_NullKey_ReturnsFoundFalse()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act
            var result = _logic.ContainsKey(dict, null);

            // Assert
            result.Found.Should().BeFalse();
        }

        #endregion

        #region ContainsValue Tests

        [Fact]
        public void ContainsValue_ExistingValue_ReturnsFoundTrue()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "a", "value1" },
                { "b", "value2" }
            };

            // Act
            var result = _logic.ContainsValue(dict, "value1");

            // Assert
            result.Found.Should().BeTrue();
            result.Count.Should().Be(1);
        }

        [Fact]
        public void ContainsValue_NonExistingValue_ReturnsFoundFalse()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "value1" } };

            // Act
            var result = _logic.ContainsValue(dict, "missing");

            // Assert
            result.Found.Should().BeFalse();
            result.Count.Should().Be(0);
        }

        [Fact]
        public void ContainsValue_CaseInsensitive_ReturnsFoundTrue()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "Value" } };

            // Act
            var result = _logic.ContainsValue(dict, "value", caseSensitive: false);

            // Assert
            result.Found.Should().BeTrue();
        }

        [Fact]
        public void ContainsValue_CaseSensitive_ReturnsFoundFalse()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "Value" } };

            // Act
            var result = _logic.ContainsValue(dict, "value", caseSensitive: true);

            // Assert
            result.Found.Should().BeFalse();
        }

        [Fact]
        public void ContainsValue_MultipleOccurrences_ReturnsCorrectCount()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "a", "value" },
                { "b", "value" },
                { "c", "other" }
            };

            // Act
            var result = _logic.ContainsValue(dict, "value");

            // Assert
            result.Found.Should().BeTrue();
            result.Count.Should().Be(2);
        }

        [Fact]
        public void ContainsValue_NullDictionary_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.ContainsValue(null, "value");
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("dictionary");
        }

        [Fact]
        public void ContainsValue_NullValue_ReturnsFoundFalse()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "value" } };

            // Act
            var result = _logic.ContainsValue(dict, null);

            // Assert
            result.Found.Should().BeFalse();
        }

        #endregion

        #region RemoveKey Tests

        [Fact]
        public void RemoveKey_ExistingKey_ReturnsDictionaryWithoutKey()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            };

            // Act
            var result = _logic.RemoveKey(dict, "a");

            // Assert
            result.Dictionary.Should().HaveCount(1);
            result.Dictionary.Should().NotContainKey("a");
            result.IsUpdate.Should().BeTrue(); // Был удалён
        }

        [Fact]
        public void RemoveKey_NonExistingKey_ReturnsSameDictionary()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act
            var result = _logic.RemoveKey(dict, "missing");

            // Assert
            result.Dictionary.Should().HaveCount(1);
            result.IsUpdate.Should().BeFalse(); // Не был удалён
        }

        [Fact]
        public void RemoveKey_DoesNotModifyOriginal()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act
            var result = _logic.RemoveKey(dict, "a");

            // Assert
            dict.Should().ContainKey("a"); // Оригинал не изменился
            result.Dictionary.Should().NotContainKey("a");
        }

        [Fact]
        public void RemoveKey_NullDictionary_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.RemoveKey(null, "key");
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("dictionary");
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_NonEmptyDictionary_ReturnsEmptyDictionary()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            };

            // Act
            var result = _logic.Clear(dict);

            // Assert
            result.Dictionary.Should().BeEmpty();
            result.Count.Should().Be(0);
            result.IsUpdate.Should().BeTrue(); // Были удалены элементы
        }

        [Fact]
        public void Clear_EmptyDictionary_ReturnsEmptyDictionary()
        {
            // Arrange
            var dict = new Dictionary<string, string>();

            // Act
            var result = _logic.Clear(dict);

            // Assert
            result.Dictionary.Should().BeEmpty();
            result.IsUpdate.Should().BeFalse(); // Нечего было удалять
        }

        [Fact]
        public void Clear_DoesNotModifyOriginal()
        {
            // Arrange
            var dict = new Dictionary<string, string> { { "a", "1" } };

            // Act
            var result = _logic.Clear(dict);

            // Assert
            dict.Should().HaveCount(1); // Оригинал не изменился
            result.Dictionary.Should().BeEmpty();
        }

        [Fact]
        public void Clear_NullDictionary_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _logic.Clear(null);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("dictionary");
        }

        #endregion
    }
}
