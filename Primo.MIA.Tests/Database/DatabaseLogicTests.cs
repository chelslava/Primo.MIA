using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FluentAssertions;
using Moq;
using Xunit;

namespace Primo.MIA.Tests.Database
{
    /// <summary>
    /// Тесты Database*-Logic с моком IDatabaseExecutor (issue #10):
    /// выбор пути (транзакция/подключение), маппинг результатов,
    /// прокидка параметров. БД не нужна.
    /// </summary>
    [Trait("Category", "Fast")]
    public class DatabaseLogicTests
    {
        private readonly Mock<IDatabaseExecutor> _executor = new Mock<IDatabaseExecutor>();

        private DataTable BuildTable(params string[] columns)
        {
            var table = new DataTable();
            foreach (var c in columns)
                table.Columns.Add(c);
            var row = table.NewRow();
            foreach (var c in columns)
                row[c] = c + "-value";
            table.Rows.Add(row);
            return table;
        }

        #region DatabaseQueryLogic

        [Fact(DisplayName = "QueryLogic: без транзакции — connection-путь, маппинг результата")]
        public void QueryLogic_NoTransaction_UsesConnectionPath()
        {
            var table = BuildTable("Id", "Name");
            _executor.Setup(e => e.ExecuteQuery("Npgsql", "cs", "SELECT 1",
                    DatabaseCommandType.Text, 30, It.IsAny<Dictionary<string, string>>()))
                .Returns(table);

            var logic = new DatabaseQueryLogic(_executor.Object);
            var result = logic.Execute("Npgsql", "cs", DatabaseCommandType.Text,
                "SELECT 1", new Dictionary<string, string>(), 30, transactionId: null);

            result.Table.Should().BeSameAs(table);
            result.RowCount.Should().Be(1);
            result.ColumnCount.Should().Be(2);
            result.ColumnNames.Should().BeEquivalentTo("Id", "Name");
            result.HasRows.Should().BeTrue();

            _executor.Verify(e => e.ExecuteQuery("Npgsql", "cs", "SELECT 1",
                DatabaseCommandType.Text, 30, It.IsAny<Dictionary<string, string>>()), Times.Once);
            _executor.Verify(e => e.ExecuteQuery(
                It.IsAny<DatabaseTransactionHandle>(), It.IsAny<string>(),
                It.IsAny<DatabaseCommandType>(), It.IsAny<int>(),
                It.IsAny<Dictionary<string, string>>()), Times.Never,
                "пустой transactionId — транзакционный путь не используется");
        }

        [Fact(DisplayName = "QueryLogic: пустая таблица — HasRows=false")]
        public void QueryLogic_EmptyTable()
        {
            _executor.Setup(e => e.ExecuteQuery(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DatabaseCommandType>(),
                    It.IsAny<int>(), It.IsAny<Dictionary<string, string>>()))
                .Returns(new DataTable());

            var result = new DatabaseQueryLogic(_executor.Object).Execute(
                "Npgsql", "cs", DatabaseCommandType.Text, "SELECT 1", null, 30, null);
            result.HasRows.Should().BeFalse();
            result.RowCount.Should().Be(0);
        }

        #endregion

        #region DatabaseCommandLogic

        [Fact(DisplayName = "CommandLogic: ExecuteNonQuery — маппинг результата")]
        public void CommandLogic_ExecuteNonQuery()
        {
            _executor.Setup(e => e.ExecuteNonQuery("Npgsql", "cs", "UPDATE t SET x=1",
                    DatabaseCommandType.Text, 30, true, true, It.IsAny<Dictionary<string, string>>()))
                .Returns(new NonQueryExecutionResult { AffectedRows = 5, BatchCount = 2, IdentityValue = "42" });

            var result = new DatabaseCommandLogic(_executor.Object).ExecuteNonQuery(
                "Npgsql", "cs", DatabaseCommandType.Text, "UPDATE t SET x=1",
                new Dictionary<string, string>(), 30, transactionId: null,
                splitByGoBatches: true, returnIdentity: true);

            result.RowsAffected.Should().Be(5);
            result.BatchCount.Should().Be(2);
            result.IdentityValue.Should().Be("42");
            result.ScalarValue.Should().Be("42");
            result.Success.Should().BeTrue();
        }

        [Fact(DisplayName = "CommandLogic: ExecuteScalar — значение пробрасывается")]
        public void CommandLogic_ExecuteScalar()
        {
            _executor.Setup(e => e.ExecuteScalar("Npgsql", "cs", "SELECT 7",
                    DatabaseCommandType.Text, 30, It.IsAny<Dictionary<string, string>>()))
                .Returns(7);

            var result = new DatabaseCommandLogic(_executor.Object).ExecuteScalar(
                "Npgsql", "cs", DatabaseCommandType.Text, "SELECT 7",
                new Dictionary<string, string>(), 30, null);

            result.ScalarValue.Should().Be(7);
            result.Success.Should().BeTrue();
        }

        #endregion

        #region DatabaseBulkLogic

        [Fact(DisplayName = "BulkLogic: Upsert — batchSize прокидывается в исполнитель")]
        public void BulkLogic_Upsert_ProxiesBatchSize()
        {
            var table = BuildTable("Id");
            UpsertResult captured = null;
            _executor.Setup(e => e.ExecuteUpsert("Npgsql", "cs", table, "target",
                    It.IsAny<List<string>>(), It.IsAny<List<string>>(),
                    It.IsAny<Dictionary<string, string>>(), 60, 250))
                .Callback<string, string, DataTable, string, List<string>, List<string>,
                    Dictionary<string, string>, int, int>(
                    (p, c, t, d, k, u, m, to, bs) =>
                        captured = new UpsertResult { InsertedCount = 3, UpdatedCount = 4 })
                .Returns(new UpsertResult { InsertedCount = 3, UpdatedCount = 4 });

            var result = new DatabaseBulkLogic(_executor.Object).Upsert(
                "Npgsql", "cs", transactionId: null, table, "target",
                new List<string> { "Id" }, new List<string> { "Name" },
                columnMappings: null, timeoutSeconds: 60, batchSize: 250);

            result.InsertedCount.Should().Be(3);
            result.UpdatedCount.Should().Be(4);
            result.RowsAffected.Should().Be(7);
            result.Success.Should().BeTrue();
            captured.Should().NotBeNull();

            _executor.Verify(e => e.ExecuteUpsert(It.IsAny<DatabaseTransactionHandle>(),
                It.IsAny<DataTable>(), It.IsAny<string>(), It.IsAny<List<string>>(),
                It.IsAny<List<string>>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact(DisplayName = "BulkLogic: BulkInsert — флаги и маппинги прокидываются")]
        public void BulkLogic_BulkInsert_ProxiesFlags()
        {
            var table = BuildTable("Id");
            _executor.Setup(e => e.ExecuteBulkInsert("Npgsql", "cs", table, "target",
                    100, 60, true, true, DatabaseBulkPreloadMode.Truncate,
                    It.IsAny<Dictionary<string, string>>()))
                .Returns(new BulkInsertResult { RowsWritten = 100, Mode = "SqlBulkCopy" });

            var result = new DatabaseBulkLogic(_executor.Object).BulkInsert(
                "Npgsql", "cs", transactionId: null, table, "target",
                columnMappings: null, batchSize: 100, timeoutSeconds: 60,
                useTableLock: true, keepIdentity: true,
                preloadMode: DatabaseBulkPreloadMode.Truncate);

            result.RowsAffected.Should().Be(100);
            result.WriteMode.Should().Be("SqlBulkCopy");
        }

        #endregion

        #region DatabaseStoredProcLogic

        [Fact(DisplayName = "StoredProcLogic: выходные параметры и ReturnValue маппятся")]
        public void StoredProcLogic_MapsOutputs()
        {
            _executor.Setup(e => e.ExecuteStoredProcedure("Npgsql", "cs", "proc",
                    45, It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<List<string>>(), 4000, true))
                .Returns(new StoredProcedureExecutionResult
                {
                    AffectedRows = 1,
                    OutputParameters = new Dictionary<string, string> { ["out1"] = "v1" },
                    ReturnValue = "0"
                });

            var result = new DatabaseStoredProcLogic(_executor.Object).Execute(
                "Npgsql", "cs", transactionId: null, "proc", 45,
                new Dictionary<string, string>(), new Dictionary<string, string>(),
                new List<string> { "out1" }, 4000, includeReturnValue: true);

            result.RowCount.Should().Be(1);
            result.OutputParams["out1"].Should().Be("v1");
            result.OutputParams["__ReturnValue"].Should().Be("0");
        }

        [Fact(DisplayName = "StoredProcLogic: пустой ReturnValue — не добавляется")]
        public void StoredProcLogic_EmptyReturnValue_Skipped()
        {
            _executor.Setup(e => e.ExecuteStoredProcedure(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<List<string>>(),
                    It.IsAny<int>(), It.IsAny<bool>()))
                .Returns(new StoredProcedureExecutionResult
                {
                    AffectedRows = 0,
                    OutputParameters = new Dictionary<string, string>(),
                    ReturnValue = string.Empty
                });

            var result = new DatabaseStoredProcLogic(_executor.Object).Execute(
                "Npgsql", "cs", null, "proc", 30, null, null, null, 0, true);
            result.OutputParams.ContainsKey("__ReturnValue").Should().BeFalse();
        }

        #endregion

        #region Конструктор

        [Fact(DisplayName = "Конструктор без аргументов — DatabaseExecutor по умолчанию")]
        public void DefaultConstructor_UsesDefaultExecutor()
        {
            Action create = () =>
            {
                var logic = new DatabaseQueryLogic();
                var command = new DatabaseCommandLogic();
                var bulk = new DatabaseBulkLogic();
                var proc = new DatabaseStoredProcLogic();
            };
            create.Should().NotThrow();
        }

        [Fact(DisplayName = "null-исполнитель — подставляется дефолт, не падает")]
        public void NullExecutor_FallsBack()
        {
            Action create = () => new DatabaseQueryLogic(null);
            create.Should().NotThrow();
        }

        #endregion
    }
}
