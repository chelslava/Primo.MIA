using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.Database
{
    /// <summary>
    /// Тесты батч-UPSERT (issue #22): выбор провайдеров, размер порции,
    /// генерация SQL для SqlClient (MERGE), Npgsql (ON CONFLICT),
    /// MySql (ON DUPLICATE KEY), разбиение порций на 1000+ строк.
    ///
    /// DatabaseUpsertBatchHelper и BulkInsertColumnMapping — internal
    /// (без InternalsVisibleTo), поэтому вызываются через отражение —
    /// по образцу DatabaseSqlGenerationTests. Методы статические, БД не нужна.
    /// </summary>
    [Trait("Category", "Fast")]
    public class DatabaseUpsertBatchTests
    {
        private const string SqlClientProvider = "System.Data.SqlClient";
        private const string NpgsqlProvider = "Npgsql";
        private const string MySqlProvider = "MySql.Data.MySqlClient";
        private const string OracleProvider = "Oracle.ManagedDataAccess.Client";

        private readonly Type _helper = typeof(Primo.MIA.DatabaseHelper).Assembly
            .GetType("Primo.MIA.DatabaseUpsertBatchHelper", throwOnError: true);

        private readonly Type _mappingType = typeof(Primo.MIA.DatabaseHelper).Assembly
            .GetType("Primo.MIA.BulkInsertColumnMapping", throwOnError: true);

        private object Invoke(string methodName, params object[] args)
        {
            var method = _helper.GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            method.Should().NotBeNull(methodName);
            return method.Invoke(null, args);
        }

        /// <summary>List&lt;BulkInsertColumnMapping&gt; через отражение.</summary>
        private object CreateMappings(params string[] columns)
        {
            var list = Activator.CreateInstance(
                typeof(System.Collections.Generic.List<>).MakeGenericType(_mappingType));
            var add = list.GetType().GetMethod("Add");
            foreach (var column in columns)
            {
                var mapping = Activator.CreateInstance(_mappingType);
                _mappingType.GetProperty("SourceColumn").SetValue(mapping, column);
                _mappingType.GetProperty("DestinationColumn").SetValue(mapping, column);
                add.Invoke(list, new[] { mapping });
            }
            return list;
        }

        private DataTable BuildTable(int rows, params string[] columns)
        {
            var table = new DataTable();
            foreach (var c in columns)
                table.Columns.Add(c, typeof(string));
            for (var i = 0; i < rows; i++)
            {
                var row = table.NewRow();
                foreach (var c in columns)
                    row[c] = c == "Id" ? $"key-{i}" : $"value-{i}";
                table.Rows.Add(row);
            }
            return table;
        }

        private System.Data.Common.DbCommand BuildBatchCommand(
            string provider, object all, object keys, object updates,
            int rowIndex, int take)
        {
            using var connection = new FakeDbConnection();
            return (System.Data.Common.DbCommand)Invoke(
                "BuildBatchCommand", connection, null, provider,
                "target", all, keys, updates, 60, rowIndex, take);
        }

        #region IsBatchSupported

        [Theory]
        [InlineData(SqlClientProvider, true)]
        [InlineData("Microsoft.Data.SqlClient", true)]
        [InlineData(NpgsqlProvider, true)]
        [InlineData(MySqlProvider, true)]
        [InlineData(OracleProvider, false)]
        [InlineData("System.Data.Odbc", false)]
        [InlineData(null, false)]
        public void IsBatchSupported_DetectsProviders(string provider, bool expected)
        {
            Invoke("IsBatchSupported", provider).Should().Be(expected);
        }

        #endregion

        #region ResolveBatchSize

        [Fact]
        public void ResolveBatchSize_Default_500()
        {
            Invoke("ResolveBatchSize", 0, SqlClientProvider, 4).Should().Be(500);
            Invoke("ResolveBatchSize", -5, NpgsqlProvider, 4).Should().Be(500);
        }

        [Fact]
        public void ResolveBatchSize_Requested_Wins()
        {
            Invoke("ResolveBatchSize", 100, NpgsqlProvider, 4).Should().Be(100);
        }

        [Fact]
        public void ResolveBatchSize_SqlClient_CappedByParameterLimit()
        {
            // 2100 параметров / 4 колонки = 525 максимум на порцию
            Invoke("ResolveBatchSize", 1000, SqlClientProvider, 4).Should().Be(525);
            // Одна строка с 3000 колонок — минимум 1
            Invoke("ResolveBatchSize", 500, SqlClientProvider, 3000).Should().Be(1);
            // Другие провайдеры не кэпятся
            Invoke("ResolveBatchSize", 1000, NpgsqlProvider, 4).Should().Be(1000);
        }

        #endregion

        #region Генерация SQL

        [Fact]
        public void BuildBatchCommand_Npgsql_OnConflictDoUpdateWithReturning()
        {
            var command = BuildBatchCommand(
                NpgsqlProvider, CreateMappings("Id", "Name", "Amount"),
                CreateMappings("Id"), CreateMappings("Name", "Amount"), 0, 2);

            command.CommandText.Should().Contain("INSERT INTO \"target\" (\"Id\", \"Name\", \"Amount\") VALUES");
            command.CommandText.Should().Contain("(@p0, @p1, @p2), (@p3, @p4, @p5)");
            command.CommandText.Should().Contain("ON CONFLICT (\"Id\") DO UPDATE SET \"Name\" = EXCLUDED.\"Name\", \"Amount\" = EXCLUDED.\"Amount\"");
            command.CommandText.Should().Contain("RETURNING (xmax = 0)");
            command.Parameters.Count.Should().Be(6, "2 строки × 3 колонки");
        }

        [Fact]
        public void BuildBatchCommand_Npgsql_NoUpdateColumns_OnConflictDoNothing()
        {
            var command = BuildBatchCommand(
                NpgsqlProvider, CreateMappings("Id", "Name"),
                CreateMappings("Id"), CreateMappings(), 0, 1);

            command.CommandText.Should().Contain("ON CONFLICT (\"Id\") DO NOTHING");
            command.CommandText.Should().Contain("RETURNING 1");
        }

        [Fact]
        public void BuildBatchCommand_MySql_OnDuplicateKeyUpdate()
        {
            var command = BuildBatchCommand(
                MySqlProvider, CreateMappings("Id", "Name"),
                CreateMappings("Id"), CreateMappings("Name"), 0, 2);

            command.CommandText.Should().Contain("INSERT INTO `target` (`Id`, `Name`) VALUES");
            command.CommandText.Should().Contain("(@p0, @p1), (@p2, @p3)");
            command.CommandText.Should().Contain("ON DUPLICATE KEY UPDATE `Name` = VALUES(`Name`)");
            command.Parameters.Count.Should().Be(4);
        }

        [Fact]
        public void BuildBatchCommand_SqlClient_MergeWithOutputAction()
        {
            var command = BuildBatchCommand(
                SqlClientProvider, CreateMappings("Id", "Name"),
                CreateMappings("Id"), CreateMappings("Name"), 0, 2);

            command.CommandText.Should().Contain("MERGE INTO [target] WITH (HOLDLOCK) AS tgt");
            command.CommandText.Should().Contain("USING (VALUES (@p0, @p1), (@p2, @p3)) AS src ([Id], [Name])");
            command.CommandText.Should().Contain("ON tgt.[Id] = src.[Id]");
            command.CommandText.Should().Contain("WHEN MATCHED THEN UPDATE SET tgt.[Name] = src.[Name]");
            command.CommandText.Should().Contain("WHEN NOT MATCHED THEN INSERT ([Id], [Name]) VALUES (src.[Id], src.[Name])");
            command.CommandText.Should().EndWith("OUTPUT $action;");
            command.Parameters.Count.Should().Be(4);
        }

        [Fact]
        public void BuildBatchCommand_SqlClient_NoUpdateColumns_NoOpUpdate()
        {
            var command = BuildBatchCommand(
                SqlClientProvider, CreateMappings("Id", "Name"),
                CreateMappings("Id"), CreateMappings(), 0, 1);

            command.CommandText.Should().Contain("WHEN MATCHED THEN UPDATE SET tgt.[Id] = src.[Id]");
        }

        #endregion

        #region Порции (1000+ строк)

        [Fact]
        public void BuildBatchCommand_ThousandRows_PaginatedValues()
        {
            // 1000 строк, порция 500: команды по 500 строк × 2 колонки = 1000 параметров
            var table = BuildTable(1000, "Id", "Value");
            var all = CreateMappings("Id", "Value");
            var keys = CreateMappings("Id");
            var updates = CreateMappings("Value");

            var first = BuildBatchCommand(NpgsqlProvider, all, keys, updates, 0, 500);
            var second = BuildBatchCommand(NpgsqlProvider, all, keys, updates, 500, 500);

            first.Parameters.Count.Should().Be(1000);
            second.Parameters.Count.Should().Be(1000);
            // Каждая порция — отдельная команда с локальной нумерацией
            // параметров p0..p999 (500 строк × 2 колонки)
            first.CommandText.Should().Contain("(@p996, @p997), (@p998, @p999) ON CONFLICT");
            first.CommandText.Should().NotContain("@p1000");
            second.CommandText.Should().StartWith("INSERT INTO \"target\" (\"Id\", \"Value\") VALUES (@p0, @p1), (@p2, @p3),");
            second.CommandText.Should().Contain("(@p996, @p997), (@p998, @p999) ON CONFLICT");

            // Параметры первой порции наполняются строками 0..499
            Invoke("AssignBatchParameterValues", first, table, 0, 500, all);
            ((System.Data.Common.DbParameter)first.Parameters[0]).Value.Should().Be("key-0");
            ((System.Data.Common.DbParameter)first.Parameters[1]).Value.Should().Be("value-0");
            ((System.Data.Common.DbParameter)first.Parameters[999]).Value.Should().Be("value-499");

            // Вторая порция — строки 500..999
            Invoke("AssignBatchParameterValues", second, table, 500, 500, all);
            ((System.Data.Common.DbParameter)second.Parameters[0]).Value.Should().Be("key-500");
            ((System.Data.Common.DbParameter)second.Parameters[999]).Value.Should().Be("value-999");
        }

        [Fact]
        public void AssignBatchParameterValues_NullCell_BecomesDbNull()
        {
            var table = BuildTable(1, "Id", "Value");
            table.Rows[0]["Value"] = DBNull.Value;
            var all = CreateMappings("Id", "Value");

            var command = BuildBatchCommand(
                NpgsqlProvider, all, CreateMappings("Id"), CreateMappings("Value"), 0, 1);

            Invoke("AssignBatchParameterValues", command, table, 0, 1, all);
            ((System.Data.Common.DbParameter)command.Parameters[1]).Value.Should().Be(DBNull.Value);
        }

        #endregion

        /// <summary>
        /// FakeDbConnection для построения команд (ExecuteReader/NonQuery
        /// не вызываются — тестируется только генерация; Prepare глушится
        /// TryPrepareCommand).
        /// </summary>
        private class FakeDbConnection : System.Data.Common.DbConnection
        {
            public override string ConnectionString { get; set; }
            public override string Database => "fake";
            public override string DataSource => "fake";
            public override string ServerVersion => "0";
            public override ConnectionState State => ConnectionState.Open;
            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { }
            public override void Open() { }
            protected override System.Data.Common.DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel)
                => throw new NotSupportedException();
            protected override System.Data.Common.DbCommand CreateDbCommand()
                => new FakeDbCommand();
        }

        private class FakeDbCommand : System.Data.Common.DbCommand
        {
            public override string CommandText { get; set; }
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override System.Data.Common.DbConnection DbConnection { get; set; }
            protected override System.Data.Common.DbParameterCollection DbParameterCollection { get; }
                = new FakeParameterCollection();
            protected override System.Data.Common.DbTransaction DbTransaction { get; set; }
            public override void Cancel() { }
            public override int ExecuteNonQuery() => throw new NotSupportedException();
            public override object ExecuteScalar() => throw new NotSupportedException();
            public override void Prepare() { }
            protected override System.Data.Common.DbParameter CreateDbParameter()
                => new FakeDbParameter();
            protected override System.Data.Common.DbDataReader ExecuteDbDataReader(System.Data.CommandBehavior behavior)
                => throw new NotSupportedException();
        }

        private class FakeDbParameter : System.Data.Common.DbParameter
        {
            public override DbType DbType { get; set; }
            public override ParameterDirection Direction { get; set; }
            public override bool IsNullable { get; set; }
            public override string ParameterName { get; set; }
            public override int Size { get; set; }
            public override string SourceColumn { get; set; }
            public override bool SourceColumnNullMapping { get; set; }
            public override DataRowVersion SourceVersion { get; set; }
            public override object Value { get; set; }
            public override void ResetDbType() { }
        }

        private class FakeParameterCollection : System.Data.Common.DbParameterCollection
        {
            private readonly System.Collections.Generic.List<object> _items =
                new System.Collections.Generic.List<object>();

            public override int Count => _items.Count;
            public override object SyncRoot => ((ICollection)_items).SyncRoot;
            public override int Add(object value) { _items.Add(value); return _items.Count - 1; }
            public override void AddRange(Array values) { foreach (var v in values) Add(v); }
            public override void Clear() => _items.Clear();
            public override bool Contains(object value) => _items.Contains(value);
            public override bool Contains(string value) => _items.OfType<System.Data.Common.DbParameter>().Any(p => p.ParameterName == value);
            public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
            public override IEnumerator GetEnumerator() => _items.GetEnumerator();
            public override int IndexOf(object value) => _items.IndexOf(value);
            public override int IndexOf(string parameterName) => _items.FindIndex(p => ((System.Data.Common.DbParameter)p).ParameterName == parameterName);
            public override void Insert(int index, object value) => _items.Insert(index, value);
            public override void Remove(object value) => _items.Remove(value);
            public override void RemoveAt(int index) => _items.RemoveAt(index);
            public override void RemoveAt(string parameterName) => _items.RemoveAt(IndexOf(parameterName));
            protected override System.Data.Common.DbParameter GetParameter(int index) => (System.Data.Common.DbParameter)_items[index];
            protected override System.Data.Common.DbParameter GetParameter(string parameterName) => (System.Data.Common.DbParameter)_items[IndexOf(parameterName)];
            protected override void SetParameter(int index, System.Data.Common.DbParameter value) => _items[index] = value;
            protected override void SetParameter(string parameterName, System.Data.Common.DbParameter value) => _items[IndexOf(parameterName)] = value;
        }
    }
}
