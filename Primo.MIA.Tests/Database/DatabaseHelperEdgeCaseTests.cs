using FluentAssertions;
using System;
using Xunit;

namespace Primo.MIA.Tests.Database
{
    public class DatabaseHelperEdgeCaseTests
    {
        [Fact(DisplayName = "ConvertScalarToInt32: неконвертируемое значение -> null")]
        public void ConvertScalarToInt32_InvalidValue_ReturnsNull()
        {
            DatabaseHelper.ConvertScalarToInt32("not-a-number").Should().BeNull();
        }

        [Fact(DisplayName = "ConvertScalarToInt32: строковое число парсится")]
        public void ConvertScalarToInt32_StringNumber_Parses()
        {
            DatabaseHelper.ConvertScalarToInt32("42").Should().Be(42);
        }

        [Fact(DisplayName = "ConvertScalarToBoolean: невалидная строка -> null")]
        public void ConvertScalarToBoolean_InvalidString_ReturnsNull()
        {
            DatabaseHelper.ConvertScalarToBoolean("maybe").Should().BeNull();
        }

        [Fact(DisplayName = "ConvertScalarToDateTime: DateTimeOffset переводится в UTC DateTime")]
        public void ConvertScalarToDateTime_DateTimeOffset_ReturnsUtcDateTime()
        {
            var value = new DateTimeOffset(2026, 3, 21, 15, 45, 30, TimeSpan.FromHours(3));

            var result = DatabaseHelper.ConvertScalarToDateTime(value);

            result.Should().HaveValue();
            result.Value.Should().Be(new DateTime(2026, 3, 21, 12, 45, 30, DateTimeKind.Utc));
        }

        [Fact(DisplayName = "BuildPagedQuery: некорректные page number и size заменяются значениями по умолчанию")]
        public void BuildPagedQuery_InvalidPaging_UsesDefaults()
        {
            var sql = DatabaseHelper.BuildPagedQuery(
                "Npgsql",
                "select * from public.users",
                "id",
                0,
                -5);

            sql.Should().Be("SELECT * FROM (select * from public.users) AS src ORDER BY id LIMIT 100 OFFSET 0");
        }

        [Fact(DisplayName = "BuildCountQuery: null provider использует alias AS src")]
        public void BuildCountQuery_NullProvider_UsesDefaultAlias()
        {
            var sql = DatabaseHelper.BuildCountQuery(
                null,
                "select * from dbo.Users");

            sql.Should().Be("SELECT COUNT(1) FROM (select * from dbo.Users) AS src");
        }

        [Fact(DisplayName = "NormalizeOutputParameterName: null provider использует @")]
        public void NormalizeOutputParameterName_NullProvider_UsesAtPrefix()
        {
            DatabaseHelper.NormalizeOutputParameterName(null, "ResultCode")
                .Should().Be("@ResultCode");
        }

        [Fact(DisplayName = "SplitSqlBatches: поддерживает GO с количеством и комментарием")]
        public void SplitSqlBatches_GoWithCountAndComment_SplitsCorrectly()
        {
            var sql = "select 1\r\nGO 2 -- repeat\r\nselect 2";

            var batches = DatabaseHelper.SplitSqlBatches(sql);

            batches.Should().Equal("select 1", "select 2");
        }

        [Fact(DisplayName = "BuildPagedQuery: пустой source query -> ошибка")]
        public void BuildPagedQuery_EmptySourceQuery_Throws()
        {
            Action action = () => DatabaseHelper.BuildPagedQuery(
                DatabaseHelper.DefaultProviderInvariantName,
                " ",
                "Id",
                1,
                10);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*Исходный SQL-запрос не может быть пустым*");
        }

        [Fact(DisplayName = "BuildPagedQuery: пустой order by -> ошибка")]
        public void BuildPagedQuery_EmptyOrderBy_Throws()
        {
            Action action = () => DatabaseHelper.BuildPagedQuery(
                DatabaseHelper.DefaultProviderInvariantName,
                "select * from dbo.Users",
                " ",
                1,
                10);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*Order by выражение не может быть пустым*");
        }
    }
}
