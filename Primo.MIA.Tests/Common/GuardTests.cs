using System;
using System.Collections.Generic;
using System.IO;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    /// <summary>
    /// Unit-тесты для класса Guard.
    /// </summary>
        [Trait("Category", "Fast")]
    public class GuardTests
    {
        #region NotNull Tests

        [Fact]
        public void NotNull_WithNullValue_ThrowsArgumentNullException()
        {
            string value = null;
            Assert.Throws<ArgumentNullException>(() => Guard.NotNull(value, "testParam"));
        }

        [Fact]
        public void NotNull_WithValidValue_DoesNotThrow()
        {
            string value = "test";
            var exception = Record.Exception(() => Guard.NotNull(value, "testParam"));
            Assert.Null(exception);
        }

        [Fact]
        public void NotNull_ThrowsWithCorrectParamName()
        {
            string value = null;
            var ex = Assert.Throws<ArgumentNullException>(() => Guard.NotNull(value, "myParam"));
            Assert.Equal("myParam", ex.ParamName);
        }

        [Fact]
        public void NotNull_ForNullableStruct_WithNullValue_ThrowsArgumentNullException()
        {
            int? value = null;
            Assert.Throws<ArgumentNullException>(() => Guard.NotNull(value, "testParam"));
        }

        [Fact]
        public void NotNull_ForNullableStruct_WithValue_DoesNotThrow()
        {
            int? value = 42;
            var exception = Record.Exception(() => Guard.NotNull(value, "testParam"));
            Assert.Null(exception);
        }

        #endregion

        #region NotNullOrEmpty Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void NotNullOrEmpty_WithInvalidValue_ThrowsArgumentException(string value)
        {
            Assert.Throws<ArgumentException>(() => Guard.NotNullOrEmpty(value, "testParam"));
        }

        [Theory]
        [InlineData("test")]
        [InlineData("a")]
        [InlineData("  ")]
        public void NotNullOrEmpty_WithValidValue_DoesNotThrow(string value)
        {
            var exception = Record.Exception(() => Guard.NotNullOrEmpty(value, "testParam"));
            Assert.Null(exception);
        }

        [Fact]
        public void NotNullOrEmpty_ThrowsWithCorrectParamName()
        {
            var ex = Assert.Throws<ArgumentException>(() => Guard.NotNullOrEmpty(null, "myParam"));
            Assert.Equal("myParam", ex.ParamName);
        }

        #endregion

        #region NotNullOrWhiteSpace Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r\n")]
        [InlineData(" \t\n ")]
        public void NotNullOrWhiteSpace_WithInvalidValue_ThrowsArgumentException(string value)
        {
            Assert.Throws<ArgumentException>(() => Guard.NotNullOrWhiteSpace(value, "testParam"));
        }

        [Theory]
        [InlineData("test")]
        [InlineData("  test  ")]
        [InlineData("a")]
        [InlineData("test value")]
        public void NotNullOrWhiteSpace_WithValidValue_DoesNotThrow(string value)
        {
            var exception = Record.Exception(() => Guard.NotNullOrWhiteSpace(value, "testParam"));
            Assert.Null(exception);
        }

        [Fact]
        public void NotNullOrWhiteSpace_ThrowsWithCorrectParamName()
        {
            var ex = Assert.Throws<ArgumentException>(() => Guard.NotNullOrWhiteSpace(null, "myParam"));
            Assert.Equal("myParam", ex.ParamName);
        }

        [Fact]
        public void NotNullOrWhiteSpace_ErrorMessageContainsParamName()
        {
            var ex = Assert.Throws<ArgumentException>(() => Guard.NotNullOrWhiteSpace("", "myParam"));
            Assert.Contains("myParam", ex.Message);
        }

        #endregion

        #region Positive (int) Tests

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(int.MinValue)]
        public void Positive_WithNonPositiveValue_ThrowsArgumentOutOfRangeException(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Positive(value, "testParam"));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public void Positive_WithPositiveValue_DoesNotThrow(int value)
        {
            var exception = Record.Exception(() => Guard.Positive(value, "testParam"));
            Assert.Null(exception);
        }

        [Fact]
        public void Positive_ThrowsWithCorrectParamName()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Positive(0, "myParam"));
            Assert.Equal("myParam", ex.ParamName);
        }

        [Fact]
        public void Positive_ErrorMessageContainsActualValue()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Positive(-5, "testParam"));
            Assert.Contains("-5", ex.Message);
        }

        #endregion

        #region Positive (long) Tests

        [Theory]
        [InlineData(0L)]
        [InlineData(-1L)]
        [InlineData(-100L)]
        public void Positive_Long_WithNonPositiveValue_ThrowsArgumentOutOfRangeException(long value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Positive(value, "testParam"));
        }

        [Theory]
        [InlineData(1L)]
        [InlineData(100L)]
        [InlineData(long.MaxValue)]
        public void Positive_Long_WithPositiveValue_DoesNotThrow(long value)
        {
            var exception = Record.Exception(() => Guard.Positive(value, "testParam"));
            Assert.Null(exception);
        }

        #endregion

        #region NotNegative Tests

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(int.MinValue)]
        public void NotNegative_WithNegativeValue_ThrowsArgumentOutOfRangeException(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Guard.NotNegative(value, "testParam"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public void NotNegative_WithNonNegativeValue_DoesNotThrow(int value)
        {
            var exception = Record.Exception(() => Guard.NotNegative(value, "testParam"));
            Assert.Null(exception);
        }

        #endregion

        #region InRange Tests

        [Theory]
        [InlineData(0, 1, 10)]
        [InlineData(11, 1, 10)]
        [InlineData(-5, 0, 10)]
        [InlineData(100, 1, 10)]
        public void InRange_WithOutOfRangeValue_ThrowsArgumentOutOfRangeException(int value, int min, int max)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Guard.InRange(value, min, max, "testParam"));
        }

        [Theory]
        [InlineData(1, 1, 10)]
        [InlineData(10, 1, 10)]
        [InlineData(5, 1, 10)]
        [InlineData(1, 1, 1)]
        public void InRange_WithInRangeValue_DoesNotThrow(int value, int min, int max)
        {
            var exception = Record.Exception(() => Guard.InRange(value, min, max, "testParam"));
            Assert.Null(exception);
        }

        [Fact]
        public void InRange_ErrorMessageContainsRange()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.InRange(0, 1, 10, "testParam"));
            Assert.Contains("1", ex.Message);
            Assert.Contains("10", ex.Message);
        }

        #endregion

        #region GreaterThan Tests

        [Theory]
        [InlineData(5, 5)]
        [InlineData(4, 5)]
        [InlineData(0, 5)]
        public void GreaterThan_WithNotGreaterValue_ThrowsArgumentOutOfRangeException(int value, int minValue)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Guard.GreaterThan(value, minValue, "testParam"));
        }

        [Theory]
        [InlineData(6, 5)]
        [InlineData(100, 5)]
        public void GreaterThan_WithGreaterValue_DoesNotThrow(int value, int minValue)
        {
            var exception = Record.Exception(() => Guard.GreaterThan(value, minValue, "testParam"));
            Assert.Null(exception);
        }

        #endregion

        #region NotEmpty Collection Tests

        [Fact]
        public void NotEmpty_Collection_WithNull_ThrowsArgumentNullException()
        {
            List<string> collection = null;
            Assert.Throws<ArgumentNullException>(() => Guard.NotEmpty(collection, "testParam"));
        }

        [Fact]
        public void NotEmpty_Collection_WithEmpty_ThrowsArgumentException()
        {
            var collection = new List<string>();
            Assert.Throws<ArgumentException>(() => Guard.NotEmpty(collection, "testParam"));
        }

        [Fact]
        public void NotEmpty_Collection_WithElements_DoesNotThrow()
        {
            var collection = new List<string> { "item" };
            var exception = Record.Exception(() => Guard.NotEmpty(collection, "testParam"));
            Assert.Null(exception);
        }

        [Fact]
        public void NotEmpty_IList_WithNull_ThrowsArgumentNullException()
        {
            IList<string> list = null;
            Assert.Throws<ArgumentNullException>(() => Guard.NotEmpty(list, "testParam"));
        }

        [Fact]
        public void NotEmpty_IList_WithEmpty_ThrowsArgumentException()
        {
            IList<string> list = new List<string>();
            Assert.Throws<ArgumentException>(() => Guard.NotEmpty(list, "testParam"));
        }

        [Fact]
        public void NotEmpty_IList_WithElements_DoesNotThrow()
        {
            IList<string> list = new List<string> { "item" };
            var exception = Record.Exception(() => Guard.NotEmpty(list, "testParam"));
            Assert.Null(exception);
        }

        [Fact]
        public void NotEmpty_Dictionary_WithNull_ThrowsArgumentNullException()
        {
            Dictionary<string, string> dictionary = null;
            Assert.Throws<ArgumentNullException>(() => Guard.NotEmpty(dictionary, "testParam"));
        }

        [Fact]
        public void NotEmpty_Dictionary_WithEmpty_ThrowsArgumentException()
        {
            var dictionary = new Dictionary<string, string>();
            Assert.Throws<ArgumentException>(() => Guard.NotEmpty(dictionary, "testParam"));
        }

        [Fact]
        public void NotEmpty_Dictionary_WithElements_DoesNotThrow()
        {
            var dictionary = new Dictionary<string, string> { { "key", "value" } };
            var exception = Record.Exception(() => Guard.NotEmpty(dictionary, "testParam"));
            Assert.Null(exception);
        }

        #endregion

        #region FileExists Tests

        [Fact]
        public void FileExists_WithNull_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Guard.FileExists(null, "testParam"));
        }

        [Fact]
        public void FileExists_WithEmpty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Guard.FileExists("", "testParam"));
        }

        [Fact]
        public void FileExists_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
            Assert.Throws<FileNotFoundException>(() => Guard.FileExists(path, "testParam"));
        }

        [Fact]
        public void FileExists_WithExistingFile_DoesNotThrow()
        {
            var path = Path.GetTempFileName();
            try
            {
                var exception = Record.Exception(() => Guard.FileExists(path, "testParam"));
                Assert.Null(exception);
            }
            finally
            {
                File.Delete(path);
            }
        }

        #endregion

        #region DirectoryExists Tests

        [Fact]
        public void DirectoryExists_WithNull_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Guard.DirectoryExists(null, "testParam"));
        }

        [Fact]
        public void DirectoryExists_WithEmpty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Guard.DirectoryExists("", "testParam"));
        }

        [Fact]
        public void DirectoryExists_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Assert.Throws<DirectoryNotFoundException>(() => Guard.DirectoryExists(path, "testParam"));
        }

        [Fact]
        public void DirectoryExists_WithExistingDirectory_DoesNotThrow()
        {
            var path = Path.GetTempPath();
            var exception = Record.Exception(() => Guard.DirectoryExists(path, "testParam"));
            Assert.Null(exception);
        }

        #endregion

        #region EnumDefined Tests

        [Fact]
        public void EnumDefined_WithValidValue_DoesNotThrow()
        {
            var exception = Record.Exception(() => Guard.EnumDefined(ConsoleColor.Red, "testParam"));
            Assert.Null(exception);
        }

        [Fact]
        public void EnumDefined_WithInvalidValue_ThrowsArgumentOutOfRangeException()
        {
            var invalidValue = (ConsoleColor)999;
            Assert.Throws<ArgumentOutOfRangeException>(() => Guard.EnumDefined(invalidValue, "testParam"));
        }

        [Fact]
        public void EnumDefined_ErrorMessageContainsEnumName()
        {
            var invalidValue = (ConsoleColor)999;
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.EnumDefined(invalidValue, "testParam"));
            Assert.Contains("ConsoleColor", ex.Message);
        }

        #endregion

        #region IsType Tests

        [Fact]
        public void IsType_WithNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Guard.IsType<string>(null, "testParam"));
        }

        [Fact]
        public void IsType_WithCorrectType_DoesNotThrowAndReturnsValue()
        {
            object value = "test string";
            var result = Guard.IsType<string>(value, "testParam");
            Assert.Equal("test string", result);
        }

        [Fact]
        public void IsType_WithWrongType_ThrowsInvalidCastException()
        {
            object value = 42;
            Assert.Throws<InvalidCastException>(() => Guard.IsType<string>(value, "testParam"));
        }

        [Fact]
        public void IsType_ErrorMessageContainsTypes()
        {
            object value = 42;
            var ex = Assert.Throws<InvalidCastException>(() => Guard.IsType<string>(value, "testParam"));
            Assert.Contains("String", ex.Message);
            Assert.Contains("Int32", ex.Message);
        }

        #endregion

        #region That Tests

        [Fact]
        public void That_WithTrueCondition_DoesNotThrow()
        {
            var exception = Record.Exception(() => Guard.That(true, "Error message"));
            Assert.Null(exception);
        }

        [Fact]
        public void That_WithFalseCondition_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Guard.That(false, "Error message"));
        }

        [Fact]
        public void That_WithFalseConditionAndParamName_ThrowsArgumentExceptionWithParamName()
        {
            var ex = Assert.Throws<ArgumentException>(() => Guard.That(false, "Error message", "myParam"));
            Assert.Equal("myParam", ex.ParamName);
        }

        [Fact]
        public void That_ErrorMessageContainsMessage()
        {
            var ex = Assert.Throws<ArgumentException>(() => Guard.That(false, "Custom error message", "testParam"));
            Assert.Contains("Custom error message", ex.Message);
        }

        #endregion

        #region ValidOperation Tests

        [Fact]
        public void ValidOperation_WithTrueCondition_DoesNotThrow()
        {
            var exception = Record.Exception(() => Guard.ValidOperation(true, "Error message"));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidOperation_WithFalseCondition_ThrowsInvalidOperationException()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Guard.ValidOperation(false, "Error message"));
            Assert.Equal("Error message", ex.Message);
        }

        #endregion
    }
}
