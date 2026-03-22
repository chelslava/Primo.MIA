using FluentAssertions;
using LTools.Common.Model;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    public class ValidationHelperTests
    {
        [Fact(DisplayName = "ValidateRequired: non-empty value does not add an error")]
        public void ValidateRequired_NonEmptyValue_DoesNotAddError()
        {
            var result = new ValidationResult();

            result.ValidateRequired("value", "FieldName", "Field is required");

            result.Items.Should().BeEmpty();
        }

        [Fact(DisplayName = "ValidateRequired: empty value adds an error")]
        public void ValidateRequired_EmptyValue_AddsError()
        {
            var result = new ValidationResult();

            result.ValidateRequired("  ", "FieldName", "Field is required");

            result.Items.Should().ContainSingle();
            result.Items[0].PropertyName.Should().Be("FieldName");
            result.Items[0].Error.Should().Be("Field is required");
        }

        [Fact(DisplayName = "ValidateCondition: false condition does not add an error")]
        public void ValidateCondition_False_DoesNotAddError()
        {
            var result = new ValidationResult();

            result.ValidateCondition(false, "FieldName", "Unexpected value");

            result.Items.Should().BeEmpty();
        }

        [Fact(DisplayName = "ValidateCondition: true condition adds an error")]
        public void ValidateCondition_True_AddsError()
        {
            var result = new ValidationResult();

            result.ValidateCondition(true, "FieldName", "Unexpected value");

            result.Items.Should().ContainSingle();
            result.Items[0].PropertyName.Should().Be("FieldName");
            result.Items[0].Error.Should().Be("Unexpected value");
        }
    }
}
