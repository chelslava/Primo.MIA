using FluentAssertions;
using System;
using System.Data;
using System.Collections.Generic;
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
                false,
                DatabaseBulkPreloadMode.None);

            result.RowsWritten.Should().Be(0);
            result.Mode.Should().Be("NoColumns");
        }

        [Fact(DisplayName = "TableExists: пустое имя таблицы -> ошибка валидации")]
        public void TableExists_EmptyTableName_Throws()
        {
            Action action = () => DatabaseHelper.TableExists(
                DatabaseHelper.DefaultProviderInvariantName,
                "fake",
                " ");

            action.Should().Throw<ArgumentException>()
                .WithMessage("*Имя таблицы не может быть пустым*");
        }

        [Fact(DisplayName = "GetColumnsSchema: пустое имя таблицы -> ошибка валидации")]
        public void GetColumnsSchema_EmptyTableName_Throws()
        {
            Action action = () => DatabaseHelper.GetColumnsSchema(
                DatabaseHelper.DefaultProviderInvariantName,
                "fake",
                null);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*Имя таблицы не может быть пустым*");
        }

        [Fact(DisplayName = "ConvertScalarToBoolean: понимает 0 и 1")]
        public void ConvertScalarToBoolean_ParsesZeroAndOne()
        {
            DatabaseHelper.ConvertScalarToBoolean("1").Should().BeTrue();
            DatabaseHelper.ConvertScalarToBoolean("0").Should().BeFalse();
        }

        [Fact(DisplayName = "ConvertScalarToDateTime: roundtrip ISO дата парсится")]
        public void ConvertScalarToDateTime_IsoString_Parses()
        {
            var value = DatabaseHelper.ConvertScalarToDateTime("2026-03-21T10:15:30.0000000Z");
            value.Should().HaveValue();
            value.Value.ToUniversalTime().Should().Be(new DateTime(2026, 3, 21, 10, 15, 30, DateTimeKind.Utc));
        }

        [Fact(DisplayName = "BuildPreloadCommandText: формирует команду очистки")]
        public void BuildPreloadCommandText_ReturnsExpectedSql()
        {
            DatabaseHelper.BuildPreloadCommandText(DatabaseHelper.DefaultProviderInvariantName, "dbo.Users", DatabaseBulkPreloadMode.DeleteAll)
                .Should().Be("DELETE FROM [dbo].[Users]");
            DatabaseHelper.BuildPreloadCommandText(DatabaseHelper.DefaultProviderInvariantName, "dbo.Users", DatabaseBulkPreloadMode.Truncate)
                .Should().Be("TRUNCATE TABLE [dbo].[Users]");
            DatabaseHelper.BuildPreloadCommandText(DatabaseHelper.DefaultProviderInvariantName, "dbo.Users", DatabaseBulkPreloadMode.None)
                .Should().BeNull();
        }

        [Fact(DisplayName = "QuoteQualifiedIdentifier: экранирует составные имена под провайдер")]
        public void QuoteQualifiedIdentifier_UsesProviderSpecificQuotes()
        {
            DatabaseHelper.QuoteQualifiedIdentifier(DatabaseHelper.DefaultProviderInvariantName, "dbo.Order Details")
                .Should().Be("[dbo].[Order Details]");
            DatabaseHelper.QuoteQualifiedIdentifier("Npgsql", "public.Order")
                .Should().Be("\"public\".\"Order\"");
            DatabaseHelper.QuoteQualifiedIdentifier("MySql.Data.MySqlClient", "sales.Order")
                .Should().Be("`sales`.`Order`");
        }

        [Fact(DisplayName = "QuoteQualifiedIdentifier: не переэкранирует уже quoted имя")]
        public void QuoteQualifiedIdentifier_DoesNotDoubleQuote()
        {
            DatabaseHelper.QuoteQualifiedIdentifier(DatabaseHelper.DefaultProviderInvariantName, "[dbo].[Users]")
                .Should().Be("[dbo].[Users]");
        }

        [Fact(DisplayName = "NormalizeOutputParameterName: использует префикс провайдера")]
        public void NormalizeOutputParameterName_UsesProviderPrefix()
        {
            DatabaseHelper.NormalizeOutputParameterName(DatabaseHelper.DefaultProviderInvariantName, "StatusCode")
                .Should().Be("@StatusCode");
            DatabaseHelper.NormalizeOutputParameterName("Oracle.ManagedDataAccess.Client", "StatusCode")
                .Should().Be(":StatusCode");
        }

        [Fact(DisplayName = "GetIdentityQuery: выбирает SQL по провайдеру")]
        public void GetIdentityQuery_ReturnsProviderSpecificSql()
        {
            DatabaseHelper.GetIdentityQuery(DatabaseHelper.DefaultProviderInvariantName)
                .Should().Be("SELECT SCOPE_IDENTITY()");
            DatabaseHelper.GetIdentityQuery("Npgsql")
                .Should().Be("SELECT LASTVAL()");
            DatabaseHelper.GetIdentityQuery("MySql.Data.MySqlClient")
                .Should().Be("SELECT LAST_INSERT_ID()");
            DatabaseHelper.GetIdentityQuery("System.Data.SQLite")
                .Should().Be("SELECT last_insert_rowid()");
        }

        [Fact(DisplayName = "GetIdentityQuery: неизвестный провайдер -> null")]
        public void GetIdentityQuery_UnknownProvider_ReturnsNull()
        {
            DatabaseHelper.GetIdentityQuery("Any.Provider").Should().BeNull();
        }

        [Fact(DisplayName = "NormalizeOutputParameterName: сохраняет существующий префикс")]
        public void NormalizeOutputParameterName_KeepsExistingPrefix()
        {
            DatabaseHelper.NormalizeOutputParameterName(DatabaseHelper.DefaultProviderInvariantName, "@Status")
                .Should().Be("@Status");
            DatabaseHelper.NormalizeOutputParameterName("Oracle.ManagedDataAccess.Client", ":Status")
                .Should().Be(":Status");
        }

        [Fact(DisplayName = "SplitSqlBatches: делит SQL по строкам GO")]
        public void SplitSqlBatches_SplitsByGo()
        {
            var sql = "create table t1(id int)\r\nGO\r\ninsert into t1 values(1)\r\n GO \r\nselect * from t1";

            var batches = DatabaseHelper.SplitSqlBatches(sql);

            batches.Should().HaveCount(3);
            batches[0].Should().Be("create table t1(id int)");
            batches[1].Should().Be("insert into t1 values(1)");
            batches[2].Should().Be("select * from t1");
        }

        [Fact(DisplayName = "SplitSqlBatches: пустой SQL -> пустой список")]
        public void SplitSqlBatches_EmptySql_ReturnsEmptyList()
        {
            DatabaseHelper.SplitSqlBatches(" ").Should().BeEmpty();
        }
    }
}
