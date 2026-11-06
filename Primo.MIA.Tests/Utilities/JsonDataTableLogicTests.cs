// =============================================================================
// JsonDataTableLogicTests.cs — тесты чистой логики JSON → DataTable.
//
// Покрывает:
//   - InferType: вывод типа колонки по JsonElement
//     (int → long → double → decimal → bool → DateTime → string)
//   - ParseJsonValue: приведение значения к целевому типу,
//     DBNull для null, фолбэк на строку при несовместимости
// =============================================================================

using System;
using System.Data;
using System.Text.Json;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Utilities
{
    /// <summary>Тесты вывода типов и конвертации значений JSON → DataTable.</summary>
    [Trait("Category", "Fast")]
    public class JsonDataTableLogicTests
    {
        /// <summary>Разбирает JSON-фрагмент и возвращает RootElement.</summary>
        private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

        // =====================================================================
        // InferType
        // =====================================================================

        [Theory(DisplayName = "InferType: числа → int / long / double / decimal")]
        [InlineData("42", "System.Int32")]
        [InlineData("-7", "System.Int32")]
        [InlineData("3000000000", "System.Int64")]
        [InlineData("3.14", "System.Double")]
        public void InferType_Numbers_ReturnsNumericType(string json, string expectedTypeName)
        {
            // Arrange
            var elem = Parse(json);

            // Act + Assert
            JsonDataTableLogic.InferType(elem).Should().Be(Type.GetType(expectedTypeName));
        }

        [Fact(DisplayName = "InferType: огромное число → decimal")]
        public void InferType_HugeNumber_ReturnsDecimal()
        {
            // Arrange — за пределами double
            var elem = Parse("1e400");

            // Act + Assert
            JsonDataTableLogic.InferType(elem).Should().Be(typeof(decimal));
        }

        [Fact(DisplayName = "InferType: true/false → bool")]
        public void InferType_Booleans_ReturnsBool()
        {
            // Act + Assert
            JsonDataTableLogic.InferType(Parse("true")).Should().Be(typeof(bool));
            JsonDataTableLogic.InferType(Parse("false")).Should().Be(typeof(bool));
        }

        [Theory(DisplayName = "InferType: строки (даты — инвариантный формат ISO)")]
        [InlineData("\"2024-01-15\"", "System.DateTime")]
        [InlineData("\"Иван\"", "System.String")]
        [InlineData("\"42\"", "System.String")]
        [InlineData("\"\"", "System.String")]
        [InlineData("\"15.03.2024\"", "System.String")]
        public void InferType_Strings_ReturnsDateTimeOrString(string json, string expectedTypeName)
        {
            // Arrange
            var elem = Parse(json);

            // Act + Assert
            JsonDataTableLogic.InferType(elem).Should().Be(Type.GetType(expectedTypeName));
        }

        [Theory(DisplayName = "InferType: объекты, массивы, null → string")]
        [InlineData("{\"a\": 1}")]
        [InlineData("[1, 2]")]
        [InlineData("null")]
        public void InferType_ComplexValues_ReturnsString(string json)
        {
            // Arrange
            var elem = Parse(json);

            // Act + Assert
            JsonDataTableLogic.InferType(elem).Should().Be(typeof(string));
        }

        // =====================================================================
        // ParseJsonValue
        // =====================================================================

        [Fact(DisplayName = "ParseJsonValue: null → DBNull.Value")]
        public void ParseJsonValue_Null_ReturnsDbNull()
        {
            // Act
            object result = JsonDataTableLogic.ParseJsonValue(Parse("null"), typeof(string), null);

            // Assert
            result.Should().Be(DBNull.Value);
        }

        [Fact(DisplayName = "ParseJsonValue: число в целевой тип")]
        public void ParseJsonValue_Number_ConvertsToTargetType()
        {
            // Act + Assert
            JsonDataTableLogic.ParseJsonValue(Parse("42"), typeof(int), null).Should().Be(42);
            JsonDataTableLogic.ParseJsonValue(Parse("3000000000"), typeof(long), null).Should().Be(3000000000L);
            JsonDataTableLogic.ParseJsonValue(Parse("3.14"), typeof(double), null).Should().Be(3.14);
            JsonDataTableLogic.ParseJsonValue(Parse("1.5"), typeof(decimal), null).Should().Be(1.5m);
        }

        [Fact(DisplayName = "ParseJsonValue: bool из литерала и из строки")]
        public void ParseJsonValue_Booleans_FromLiteralAndString()
        {
            // Act + Assert
            JsonDataTableLogic.ParseJsonValue(Parse("true"), typeof(bool), null).Should().Be(true);
            JsonDataTableLogic.ParseJsonValue(Parse("\"true\""), typeof(bool), null).Should().Be(true);
            JsonDataTableLogic.ParseJsonValue(Parse("\"nope\""), typeof(bool), null).Should().Be(false);
        }

        [Fact(DisplayName = "ParseJsonValue: строка-дата → DateTime")]
        public void ParseJsonValue_DateString_ReturnsDateTime()
        {
            // Act
            object result = JsonDataTableLogic.ParseJsonValue(Parse("\"2024-01-15\""), typeof(DateTime), null);

            // Assert
            result.Should().BeOfType<DateTime>();
            ((DateTime)result).Should().Be(new DateTime(2024, 1, 15));
        }

        [Fact(DisplayName = "ParseJsonValue: не-дата в DateTime → DBNull")]
        public void ParseJsonValue_InvalidDate_ReturnsDbNull()
        {
            // Act
            object result = JsonDataTableLogic.ParseJsonValue(Parse("\"не дата\""), typeof(DateTime), null);

            // Assert
            result.Should().Be(DBNull.Value);
        }

        [Fact(DisplayName = "ParseJsonValue: строка остаётся строкой")]
        public void ParseJsonValue_String_StaysString()
        {
            // Act
            object result = JsonDataTableLogic.ParseJsonValue(Parse("\"Иван\""), typeof(string), null);

            // Assert
            result.Should().Be("Иван");
        }

        [Fact(DisplayName = "ParseJsonValue: нестроковое значение в string-колонку → строковое представление")]
        public void ParseJsonValue_NumberToStringTarget_ReturnsStringForm()
        {
            // Act
            object result = JsonDataTableLogic.ParseJsonValue(Parse("42"), typeof(string), null);

            // Assert
            result.Should().BeOfType<string>();
            ((string)result).Should().Contain("42");
        }

        [Fact(DisplayName = "ParseJsonValue: объект в string-колонку → JSON-представление")]
        public void ParseJsonValue_ObjectToStringTarget_ReturnsJsonText()
        {
            // Act
            object result = JsonDataTableLogic.ParseJsonValue(Parse("{\"a\": 1}"), typeof(string), null);

            // Assert
            result.Should().BeOfType<string>();
            ((string)result).Should().Contain("a");
        }
    }
}
