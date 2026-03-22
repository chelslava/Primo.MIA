using System;
using FluentAssertions;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    public class ComparisonHelperTests
    {
        [Fact(DisplayName = "GetStringComparison: case sensitive returns Ordinal")]
        public void GetStringComparison_CaseSensitiveTrue_ReturnsOrdinal()
        {
            ComparisonHelper.GetStringComparison(true).Should().Be(StringComparison.Ordinal);
        }

        [Fact(DisplayName = "GetStringComparison: case insensitive returns OrdinalIgnoreCase")]
        public void GetStringComparison_CaseSensitiveFalse_ReturnsOrdinalIgnoreCase()
        {
            ComparisonHelper.GetStringComparison(false).Should().Be(StringComparison.OrdinalIgnoreCase);
        }

        [Fact(DisplayName = "GetStringComparer: case sensitive comparer keeps casing")]
        public void GetStringComparer_CaseSensitiveTrue_UsesOrdinalComparison()
        {
            var comparer = ComparisonHelper.GetStringComparer(true);

            comparer.Equals("abc", "ABC").Should().BeFalse();
            comparer.Equals("same", "same").Should().BeTrue();
        }

        [Fact(DisplayName = "GetStringComparer: case insensitive comparer ignores casing")]
        public void GetStringComparer_CaseSensitiveFalse_UsesIgnoreCaseComparison()
        {
            var comparer = ComparisonHelper.GetStringComparer(false);

            comparer.Equals("abc", "ABC").Should().BeTrue();
            comparer.Equals("same", "same").Should().BeTrue();
        }
    }
}
