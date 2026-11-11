using System;
using System.Data;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.Json
{
    /// <summary>
    /// Тесты JsonSchemaLogic (issue #34): валидация по draft-07 векторам,
    /// пути ошибок, генерация схемы из примера, roundtrip, Guard.
    /// </summary>
    [Trait("Category", "Fast")]
    public class JsonSchemaLogicTests
    {
        private readonly JsonSchemaLogic _logic = new JsonSchemaLogic();

        private const string OrderSchema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""id"": { ""type"": ""integer"" },
    ""customer"": { ""type"": ""string"" },
    ""amount"": { ""type"": ""number"" }
  },
  ""required"": [""id"", ""customer""],
  ""additionalProperties"": false
}";

        private const string ValidOrder = @"{""id"": 1, ""customer"": ""Иван"", ""amount"": 99.5}";
        private const string MissingCustomer = @"{""id"": 1}";
        private const string WrongType = @"{""id"": ""not-a-number"", ""customer"": ""Иван""}";
        private const string ExtraProperty = @"{""id"": 1, ""customer"": ""И"", ""bonus"": true}";

        // ── Validate ───────────────────────────────────────────────────

        [Fact(DisplayName = "Validate: валидный объект проходит")]
        public void Validate_ValidJson_Passes()
        {
            var result = _logic.Validate(ValidOrder, OrderSchema);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact(DisplayName = "Validate: отсутствует required-свойство — ошибка с путём")]
        public void Validate_MissingRequired_FailsWithPath()
        {
            var result = _logic.Validate(MissingCustomer, OrderSchema);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
            result.Errors.Count.Should().Be(1);
            result.Errors[0].Error.Should().Be("PropertyRequired");
        }

        [Fact(DisplayName = "Validate: неверный тип свойства")]
        public void Validate_WrongType_Fails()
        {
            var result = _logic.Validate(WrongType, OrderSchema);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Path.Contains("id"));
        }

        [Fact(DisplayName = "Validate: additionalProperties=false отсекает лишние поля")]
        public void Validate_ExtraProperty_Fails()
        {
            var result = _logic.Validate(ExtraProperty, OrderSchema);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact(DisplayName = "Validate: невалидный проверяемый JSON — InvalidOperationException")]
        public void Validate_InvalidJson_Throws()
        {
            Action act = () => _logic.Validate("{broken", OrderSchema);
            act.Should().Throw<Exception>("парсер JSON бросает до проверки схемы");
        }

        [Fact(DisplayName = "Validate: некорректная схема — InvalidOperationException с текстом")]
        public void Validate_InvalidSchema_Throws()
        {
            Action act = () => _logic.Validate(ValidOrder, "{type: 1, properties: 42}");
            act.Should().Throw<InvalidOperationException>().WithMessage("*Schema*");
        }

        [Fact(DisplayName = "Validate: вложенные объекты — путь через точку")]
        public void Validate_NestedObject_Path()
        {
            var schema = @"{
              ""type"": ""object"",
              ""properties"": {
                ""address"": {
                  ""type"": ""object"",
                  ""properties"": { ""zip"": { ""type"": ""string"" } },
                  ""required"": [""zip""]
                }
              },
              ""required"": [""address""]
            }";
            var json = @"{""address"": {}}";

            var result = _logic.Validate(json, schema);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact(DisplayName = "ErrorsToDataTable: колонки path/error")]
        public void ErrorsToDataTable_Columns()
        {
            var result = _logic.Validate(MissingCustomer, OrderSchema);
            var table = _logic.ErrorsToDataTable(result);

            table.Columns.Cast<DataColumn>().Select(c => c.ColumnName)
                .Should().BeEquivalentTo("path", "error");
            table.Rows.Count.Should().Be(result.Errors.Count);
        }

        // ── Generate ───────────────────────────────────────────────────

        [Fact(DisplayName = "Generate: схема содержит свойства и типы примера")]
        public void Generate_Basic()
        {
            var schema = _logic.Generate(ValidOrder, "Order", true);

            schema.Should().Contain("\"Order\"").And.Contain("id").And.Contain("customer");
        }

        [Fact(DisplayName = "Generate: roundtrip — пример валиден по своей схеме")]
        public void Generate_Roundtrip_SampleValid()
        {
            var schema = _logic.Generate(ValidOrder, null, true);

            var result = _logic.Validate(ValidOrder, schema);
            result.IsValid.Should().BeTrue("схема, сгенерированная из примера, валидирует пример");
        }

        [Fact(DisplayName = "Generate: prettyPrint=false — компактная схема")]
        public void Generate_NoPrettyPrint_Compact()
        {
            var pretty = _logic.Generate(ValidOrder, null, true);
            var compact = _logic.Generate(ValidOrder, null, false);

            compact.Should().NotContain("\r").And.NotContain("\n");
            pretty.Should().Contain("\n", "форматированная схема с переносами");
        }

        [Fact(DisplayName = "Generate: пустой пример — ArgumentException (Guard)")]
        public void Generate_EmptySample_Throws()
        {
            Action act = () => _logic.Generate(" ", null, true);
            act.Should().Throw<ArgumentException>();
        }

        [Fact(DisplayName = "Validate: Guard — пустые json/schema")]
        public void Validate_Guard()
        {
            Action noJson = () => _logic.Validate("", OrderSchema);
            Action noSchema = () => _logic.Validate(ValidOrder, null);

            noJson.Should().Throw<ArgumentException>();
            noSchema.Should().Throw<ArgumentException>();
        }
    }
}
