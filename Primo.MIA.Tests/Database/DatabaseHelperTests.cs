using FluentAssertions;
using System;
using System.Data;
using Xunit;

namespace Primo.MIA.Tests.Database
{
    public class DatabaseHelperTests
    {
        [Fact(DisplayName = "NormalizeParameterName: добавляет @ если префикса нет")]
        public void NormalizeParameterName_AddsAtPrefix()
        {
            DatabaseHelper.NormalizeParameterName("Id").Should().Be("@Id");
        }

        [Fact(DisplayName = "NormalizeParameterName: сохраняет существующий префикс")]
        public void NormalizeParameterName_KeepsExistingPrefix()
        {
            DatabaseHelper.NormalizeParameterName(":Id").Should().Be(":Id");
            DatabaseHelper.NormalizeParameterName("?").Should().Be("?");
            DatabaseHelper.NormalizeParameterName("@Name").Should().Be("@Name");
        }

        [Fact(DisplayName = "ConvertScalarToString: null и DBNull -> null")]
        public void ConvertScalarToString_NullCases()
        {
            DatabaseHelper.ConvertScalarToString(null).Should().BeNull();
            DatabaseHelper.ConvertScalarToString(DBNull.Value).Should().BeNull();
        }

        [Fact(DisplayName = "ConvertScalarToString: DateTime -> ISO 8601")]
        public void ConvertScalarToString_DateTime_UsesIsoFormat()
        {
            var value = new DateTime(2025, 12, 31, 23, 59, 58, DateTimeKind.Utc);
            DatabaseHelper.ConvertScalarToString(value).Should().Be("2025-12-31T23:59:58.0000000Z");
        }

        [Fact(DisplayName = "GetFactory: неверный provider -> понятная ошибка")]
        public void GetFactory_InvalidProvider_Throws()
        {
            Action action = () => DatabaseHelper.GetFactory("No.Such.Provider");
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*DbProviderFactory*");
        }

        [Fact(DisplayName = "ExecuteBulkInsert: пустая DataTable без колонок -> успех без записи")]
        public void ExecuteBulkInsert_EmptySchema_ReturnsNoColumnsMode()
        {
            var table = new DataTable();

            var result = DatabaseHelper.ExecuteBulkInsert(
                "Any.Provider",
                "fake",
                table,
                "TargetTable",
                1000,
                60,
                false,
                false);

            result.RowsWritten.Should().Be(0);
            result.Mode.Should().Be("NoColumns");
        }
    }
}
