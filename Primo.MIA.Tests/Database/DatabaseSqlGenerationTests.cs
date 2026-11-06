using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using Xunit;

namespace Primo.MIA.Tests.Database
{
    /// <summary>
    /// Тесты генерации SQL и параметров Database-хелперов (issue #16):
    /// только чистые построители без живого соединения:
    ///   - DatabaseCommandHelper (публичный): префиксы/плейсхолдеры параметров;
    ///   - DatabaseBulkInsertHelper (internal): BuildMappings / BuildInsertCommandText /
    ///     EnsureColumnExists / CreateInsertParameters / AssignInsertParameterValues;
    ///   - DatabaseUpsertHelper (internal): ResolveMappings и построение UPDATE/EXISTS;
    ///   - DatabaseHelper.QuoteIdentifier: экранирование и защита от инъекций.
    ///
    /// Вспомогательные классы DatabaseBulkInsertHelper/DatabaseUpsertHelper и тип
    /// BulkInsertColumnMapping — internal (без InternalsVisibleTo), поэтому вызываются
    /// через отражение. Методы статические и не требуют соединения с БД.
    /// Сетевые пути (Execute*) здесь не тестируются.
    /// </summary>
        [Trait("Category", "Fast")]
    public class DatabaseSqlGenerationTests
    {
        private const string SqlClientProvider = "System.Data.SqlClient";
        private const string OracleProvider = "Oracle.ManagedDataAccess.Client";
        private const string OdbcProvider = "System.Data.Odbc";

        // =====================================================================
        // DatabaseCommandHelper — префиксы и плейсхолдеры параметров
        // =====================================================================

        #region ParameterPlaceholders

        [Theory]
        [InlineData(null, "@")]
        [InlineData("", "@")]
        [InlineData("System.Data.SqlClient", "@")]
        [InlineData("Npgsql", "@")]
        [InlineData("Oracle.ManagedDataAccess.Client", ":")]
        public void GetParameterPrefix_ReturnsProviderSpecificPrefix(string provider, string expected)
        {
            DatabaseCommandHelper.GetParameterPrefix(provider).Should().Be(expected);
        }

        [Theory]
        [InlineData("System.Data.Odbc", true)]
        [InlineData("System.Data.OleDb", true)]
        [InlineData("System.Data.SqlClient", false)]
        [InlineData("Npgsql", false)]
        [InlineData(null, false)]
        public void IsPositionalProvider_DetectsOdbcAndOleDb(string provider, bool expected)
        {
            DatabaseCommandHelper.IsPositionalProvider(provider).Should().Be(expected);
        }

        [Fact]
        public void GetParameterPlaceholder_NamedProviders_UsePrefixWithIndex()
        {
            DatabaseCommandHelper.GetParameterPlaceholder(SqlClientProvider, 0).Should().Be("@p0");
            DatabaseCommandHelper.GetParameterPlaceholder(SqlClientProvider, 2).Should().Be("@p2");
            DatabaseCommandHelper.GetParameterPlaceholder(OracleProvider, 1).Should().Be(":p1");
        }

        [Fact]
        public void GetParameterPlaceholder_PositionalProviders_UseQuestionMark()
        {
            // ODBC/OleDb не поддерживают именованные параметры — только ?
            DatabaseCommandHelper.GetParameterPlaceholder(OdbcProvider, 0).Should().Be("?");
            DatabaseCommandHelper.GetParameterPlaceholder("System.Data.OleDb", 3).Should().Be("?");
        }

        [Fact]
        public void GetParameterName_PositionalProviders_OmitPrefix()
        {
            DatabaseCommandHelper.GetParameterName(SqlClientProvider, 0).Should().Be("@p0");
            DatabaseCommandHelper.GetParameterName(OdbcProvider, 0).Should().Be("p0");
        }

        [Fact]
        public void NormalizeParameterName_TrimsWhitespaceBeforePrefixing()
        {
            DatabaseCommandHelper.NormalizeParameterName("  Id  ").Should().Be("@Id");
        }

        #endregion

        // =====================================================================
        // DatabaseBulkInsertHelper — маппинги колонок
        // =====================================================================

        #region BuildMappings

        [Fact]
        public void BuildMappings_WithoutExplicitMappings_MapsAllColumnsIdentity()
        {
            var table = CreateTable("Id", "Name");

            var mappings = InvokeBulkList("BuildMappings", table, null);

            mappings.Count.Should().Be(2);
            GetMappingField(mappings[0], "SourceColumn").Should().Be("Id");
            GetMappingField(mappings[0], "DestinationColumn").Should().Be("Id");
            GetMappingField(mappings[1], "SourceColumn").Should().Be("Name");
            GetMappingField(mappings[1], "DestinationColumn").Should().Be("Name");
        }

        [Fact]
        public void BuildMappings_WithExplicitMappings_RenamesColumns()
        {
            var table = CreateTable("Id", "Name");
            var columnMappings = new Dictionary<string, string> { { "Id", "user_id" } };

            var mappings = InvokeBulkList("BuildMappings", table, columnMappings);

            mappings.Count.Should().Be(1);
            GetMappingField(mappings[0], "SourceColumn").Should().Be("Id");
            GetMappingField(mappings[0], "DestinationColumn").Should().Be("user_id");
        }

        [Fact]
        public void BuildMappings_EmptyDataTable_ReturnsEmptyMappingList()
        {
            var table = new DataTable();

            var mappings = InvokeBulkList("BuildMappings", table, null);

            mappings.Count.Should().Be(0);
        }

        [Fact]
        public void BuildMappings_UnknownSourceColumn_ThrowsInvalidOperationException()
        {
            var table = CreateTable("Id");
            var columnMappings = new Dictionary<string, string> { { "Missing", "x" } };

            // Отражение оборачивает исключение в TargetInvocationException
            Action act = () => InvokeBulkList("BuildMappings", table, columnMappings);

            var inner = act.Should().Throw<TargetInvocationException>().Which.InnerException;
            inner.Should().BeOfType<InvalidOperationException>();
            inner.Message.Should().Contain("Колонка 'Missing' отсутствует");
        }

        [Fact]
        public void EnsureColumnExists_MissingColumn_ThrowsInvalidOperationException()
        {
            var table = CreateTable("Id");

            Action act = () => InvokeBulk("EnsureColumnExists", table, "Nope");

            var inner = act.Should().Throw<TargetInvocationException>().Which.InnerException;
            inner.Should().BeOfType<InvalidOperationException>();
            inner.Message.Should().Contain("Колонка 'Nope' отсутствует");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void EnsureColumnExists_EmptyColumnName_ThrowsArgumentException(string columnName)
        {
            var table = CreateTable("Id");

            Action act = () => InvokeBulk("EnsureColumnExists", table, columnName);

            var inner = act.Should().Throw<TargetInvocationException>().Which.InnerException;
            inner.Should().BeOfType<ArgumentException>();
            inner.Message.Should().Contain("Имя колонки не может быть пустым");
        }

        #endregion

        // =====================================================================
        // DatabaseBulkInsertHelper — текст INSERT-команды
        // =====================================================================

        #region BuildInsertCommandText

        [Fact]
        public void BuildInsertCommandText_SqlClient_QuotesIdentifiersAndUsesNamedPlaceholders()
        {
            var mappings = NewMappings(("Id", "Id"), ("Name", "Name"));

            var sql = InvokeInsertText(SqlClientProvider, "dbo.Users", mappings);

            sql.Should().Be("INSERT INTO [dbo].[Users] ([Id], [Name]) VALUES (@p0, @p1)");
        }

        [Fact]
        public void BuildInsertCommandText_OdbcProvider_UsesPositionalPlaceholders()
        {
            var mappings = NewMappings(("Id", "id"), ("Order", "order"));

            var sql = InvokeInsertText(OdbcProvider, "users", mappings);

            sql.Should().Be("INSERT INTO [users] ([id], [order]) VALUES (?, ?)");
        }

        [Fact]
        public void BuildInsertCommandText_ColumnNameWithSpace_IsQuotedAsSingleIdentifier()
        {
            var mappings = NewMappings(("Order Details", "Order Details"));

            var sql = InvokeInsertText(SqlClientProvider, "dbo.Order Details", mappings);

            sql.Should().Be("INSERT INTO [dbo].[Order Details] ([Order Details]) VALUES (@p0)");
        }

        [Fact]
        public void BuildInsertCommandText_ColumnNameWithSqlInjection_StaysSingleQuotedToken()
        {
            // Инъекция в имени колонки: ']' экранируется удвоением,
            // всё имя остаётся одним закавыченным идентификатором —
            // разбить statement (DROP TABLE) невозможно.
            var mappings = NewMappings(("x", "Id]; DROP TABLE Users;--"));

            var sql = InvokeInsertText(SqlClientProvider, "dbo.Users", mappings);

            sql.Should().Be("INSERT INTO [dbo].[Users] ([Id]]; DROP TABLE Users;--]) VALUES (@p0)");
        }

        [Fact]
        public void BuildInsertCommandText_AlreadyQuotedIdentifier_IsNotRequoted()
        {
            var mappings = NewMappings(("x", "[Name]"));

            var sql = InvokeInsertText(SqlClientProvider, "dbo.Users", mappings);

            sql.Should().Be("INSERT INTO [dbo].[Users] ([Name]) VALUES (@p0)");
        }

        [Fact]
        public void BuildBulkInsertModeName_AppendsPreloadMode()
        {
            InvokeBulk<string>("BuildBulkInsertModeName", "BatchedInsert", DatabaseBulkPreloadMode.None)
                .Should().Be("BatchedInsert");
            InvokeBulk<string>("BuildBulkInsertModeName", "SqlBulkCopy", DatabaseBulkPreloadMode.Truncate)
                .Should().Be("SqlBulkCopy+Truncate");
        }

        #endregion

        // =====================================================================
        // DatabaseBulkInsertHelper — параметры команды
        // =====================================================================

        #region InsertParameters

        [Fact]
        public void CreateInsertParameters_CreatesNamedParametersForEachMapping()
        {
            var command = new SqlCommand();
            var mappings = NewMappings(("Id", "Id"), ("Name", "Name"));

            InvokeBulk("CreateInsertParameters", command, SqlClientProvider, mappings);

            command.Parameters.Count.Should().Be(2);
            command.Parameters[0].ParameterName.Should().Be("@p0");
            command.Parameters[1].ParameterName.Should().Be("@p1");
        }

        [Fact]
        public void AssignInsertParameterValues_PreservesValueTypesAndQuotes()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Price", typeof(decimal));
            table.Columns.Add("Created", typeof(DateTime));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Note", typeof(string));
            var created = new DateTime(2026, 2, 15, 10, 30, 0);
            table.Rows.Add(42, 19.99m, created, "O'Brien", DBNull.Value);

            var command = new SqlCommand();
            var mappings = NewMappings(
                ("Id", "Id"), ("Price", "Price"), ("Created", "Created"),
                ("Name", "Name"), ("Note", "Note"));
            InvokeBulk("CreateInsertParameters", command, SqlClientProvider, mappings);

            InvokeBulk("AssignInsertParameterValues", command, table.Rows[0], mappings);

            // Значения уходят параметрами: кавычки в строках не требуют экранирования,
            // DBNull передаётся как есть
            command.Parameters[0].Value.Should().Be(42);
            command.Parameters[1].Value.Should().Be(19.99m);
            command.Parameters[2].Value.Should().Be(created);
            command.Parameters[3].Value.Should().Be("O'Brien");
            command.Parameters[4].Value.Should().Be(DBNull.Value);
        }

        #endregion

        // =====================================================================
        // DatabaseUpsertHelper — маппинги и текст команд
        // =====================================================================

        #region Upsert

        [Fact]
        public void ResolveMappings_MatchesCaseInsensitiveBySourceAndDestination()
        {
            var mappings = NewMappings(("Id", "Id"), ("Name", "NAME"));

            var resolved = InvokeUpsertList(mappings, new List<string> { "id", "name" });

            resolved.Count.Should().Be(2);
            GetMappingField(resolved[0], "SourceColumn").Should().Be("Id");
            GetMappingField(resolved[1], "SourceColumn").Should().Be("Name");
        }

        [Fact]
        public void ResolveMappings_SkipsWhitespaceNames()
        {
            var mappings = NewMappings(("Id", "Id"));

            var resolved = InvokeUpsertList(mappings, new List<string> { "   ", "Id" });

            resolved.Count.Should().Be(1);
        }

        [Fact]
        public void ResolveMappings_UnknownName_ThrowsInvalidOperationException()
        {
            var mappings = NewMappings(("Id", "Id"));

            Action act = () => InvokeUpsertList(mappings, new List<string> { "Nope" });

            var inner = act.Should().Throw<TargetInvocationException>().Which.InnerException;
            inner.Should().BeOfType<InvalidOperationException>();
            inner.Message.Should().Contain("Колонка 'Nope' не найдена");
        }

        [Fact]
        public void BuildUpdateCommandText_SqlClient_GeneratesSetAndWhereClauses()
        {
            var keyMappings = NewMappings(("Id", "Id"));
            var updateMappings = NewMappings(("Name", "Name"), ("Age", "Age"));

            var sql = InvokeUpsertText("BuildUpdateCommandText", SqlClientProvider, "dbo.Users", keyMappings, updateMappings);

            sql.Should().Be("UPDATE [dbo].[Users] SET [Name] = @p0, [Age] = @p1 WHERE [Id] = @p2");
        }

        [Fact]
        public void BuildExistsCommandText_SqlClient_GeneratesCountWithWhereClause()
        {
            var keyMappings = NewMappings(("Id", "Id"), ("Email", "Email"));

            var sql = InvokeUpsertText("BuildExistsCommandText", SqlClientProvider, "dbo.Users", keyMappings);

            sql.Should().Be("SELECT COUNT(1) FROM [dbo].[Users] WHERE [Id] = @p0 AND [Email] = @p1");
        }

        #endregion

        // =====================================================================
        // DatabaseHelper.QuoteIdentifier — экранирование/инъекции
        // (QuoteQualifiedIdentifier для составных имён уже покрыт DatabaseHelperTests)
        // =====================================================================

        #region QuoteIdentifier

        [Fact]
        public void QuoteIdentifier_TrimsSurroundingWhitespace()
        {
            DatabaseHelper.QuoteIdentifier(SqlClientProvider, "  Id  ").Should().Be("[Id]");
        }

        [Fact]
        public void QuoteIdentifier_SingleQuoteInsideName_IsAllowedByBrackets()
        {
            DatabaseHelper.QuoteIdentifier(SqlClientProvider, "Name's").Should().Be("[Name's]");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void QuoteIdentifier_EmptyIdentifier_ThrowsArgumentException(string identifier)
        {
            Action act = () => DatabaseHelper.QuoteIdentifier(SqlClientProvider, identifier);

            act.Should().Throw<ArgumentException>()
                .WithMessage("*Идентификатор не может быть пустым*");
        }

        #endregion

        // =====================================================================
        // Вспомогательные методы (отражение по internal-хелперам)
        // =====================================================================

        private static DataTable CreateTable(params string[] columnNames)
        {
            var table = new DataTable();
            foreach (var name in columnNames)
                table.Columns.Add(name);
            return table;
        }

        private static readonly Type BulkHelperType =
            typeof(DatabaseHelper).Assembly.GetType("Primo.MIA.DatabaseBulkInsertHelper");

        private static readonly Type UpsertHelperType =
            typeof(DatabaseHelper).Assembly.GetType("Primo.MIA.DatabaseUpsertHelper");

        private static readonly Type MappingType =
            typeof(DatabaseHelper).Assembly.GetType("Primo.MIA.BulkInsertColumnMapping");

        private static readonly Type MappingListType =
            typeof(List<>).MakeGenericType(MappingType);

        /// <summary>Создаёт internal List&lt;BulkInsertColumnMapping&gt; из пар (источник → приёмник).</summary>
        private static object NewMappings(params (string Source, string Destination)[] pairs)
        {
            var list = Activator.CreateInstance(MappingListType);
            var add = MappingListType.GetMethod("Add");
            foreach (var (source, destination) in pairs)
            {
                var mapping = Activator.CreateInstance(MappingType);
                MappingType.GetProperty("SourceColumn").SetValue(mapping, source);
                MappingType.GetProperty("DestinationColumn").SetValue(mapping, destination);
                add.Invoke(list, new[] { mapping });
            }
            return list;
        }

        private static object InvokeBulk(string methodName, params object[] args)
        {
            return BulkHelperType
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, args);
        }

        private static T InvokeBulk<T>(string methodName, params object[] args)
        {
            return (T)InvokeBulk(methodName, args);
        }

        private static System.Collections.IList InvokeBulkList(string methodName, params object[] args)
        {
            return (System.Collections.IList)InvokeBulk(methodName, args);
        }

        private static object InvokeUpsert(string methodName, params object[] args)
        {
            return UpsertHelperType
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, args);
        }

        private static string InvokeInsertText(string provider, string table, object mappings)
        {
            return (string)InvokeBulk("BuildInsertCommandText", provider, table, mappings);
        }

        private static System.Collections.IList InvokeUpsertList(object mappings, List<string> names)
        {
            return (System.Collections.IList)InvokeUpsert("ResolveMappings", mappings, names);
        }

        private static string InvokeUpsertText(string methodName, string provider, string table,
            object keyMappings, object updateMappings = null)
        {
            return updateMappings == null
                ? (string)InvokeUpsert(methodName, provider, table, keyMappings)
                : (string)InvokeUpsert(methodName, provider, table, keyMappings, updateMappings);
        }

        private static object GetMappingField(object mapping, string propertyName)
        {
            return MappingType.GetProperty(propertyName).GetValue(mapping);
        }
    }
}
