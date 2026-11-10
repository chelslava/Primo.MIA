using System;
using System.Data;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.Utilities
{
    /// <summary>
    /// Тесты DataTableJoinLogic (issue #36): все виды соединений, NULL-ключи
    /// (семантика SQL), конфликты имён с суффиксом, Pivot/Unpivot, агрегаты.
    /// </summary>
    [Trait("Category", "Fast")]
    public class DataTableJoinLogicTests
    {
        private readonly DataTableJoinLogic _logic = new DataTableJoinLogic();

        private DataTable LeftTable()
        {
            var t = new DataTable("Left");
            t.Columns.Add("Id", typeof(int));
            t.Columns.Add("Name", typeof(string));
            t.Rows.Add(1, "Иван");
            t.Rows.Add(2, "Мария");
            t.Rows.Add(3, "Пётр");
            return t;
        }

        private DataTable RightTable()
        {
            var t = new DataTable("Right");
            t.Columns.Add("Id", typeof(int));
            t.Columns.Add("City", typeof(string));
            t.Rows.Add(1, "Москва");
            t.Rows.Add(2, "Сочи");
            t.Rows.Add(4, "Казань");
            return t;
        }

        private static string Col(DataTable t, int row, string name) =>
            Convert.ToString(t.Rows[row][name]);

        // ── Join ───────────────────────────────────────────────────────

        [Fact(DisplayName = "Inner: только совпавшие ключи")]
        public void Join_Inner()
        {
            var result = _logic.Join(LeftTable(), RightTable(), new[] { "Id" }, new[] { "Id" },
                DataTableJoinKind.Inner);

            result.Rows.Count.Should().Be(2);
            Col(result, 0, "Name").Should().Be("Иван");
            Col(result, 0, "City").Should().Be("Москва");
            result.Columns.Contains("Id").Should().BeTrue("ключ один — не дублируется");
            result.Columns.Contains("Id_right").Should().BeFalse();
        }

        [Fact(DisplayName = "Left: все левые, без пары — DBNull справа")]
        public void Join_Left()
        {
            var result = _logic.Join(LeftTable(), RightTable(), new[] { "Id" }, new[] { "Id" },
                DataTableJoinKind.Left);

            result.Rows.Count.Should().Be(3);
            Col(result, 2, "Name").Should().Be("Пётр");
            result.Rows[2]["City"].Should().Be(DBNull.Value, "Id=3 не нашёл пару");
        }

        [Fact(DisplayName = "Right: все правые, без пары — DBNull слева")]
        public void Join_Right()
        {
            var result = _logic.Join(LeftTable(), RightTable(), new[] { "Id" }, new[] { "Id" },
                DataTableJoinKind.Right);

            result.Rows.Count.Should().Be(3);
            Col(result, 2, "City").Should().Be("Казань");
            result.Rows[2]["Name"].Should().Be(DBNull.Value, "Id=4 не нашёл пару");
        }

        [Fact(DisplayName = "Full: объединение, все без пары присутствуют")]
        public void Join_Full()
        {
            var result = _logic.Join(LeftTable(), RightTable(), new[] { "Id" }, new[] { "Id" },
                DataTableJoinKind.Full);

            result.Rows.Count.Should().Be(4);
            var names = Enumerable.Range(0, 4).Select(r => Col(result, r, "Name"));
            names.Should().Contain(new[] { "Иван", "Мария", "Пётр" });
            var cities = Enumerable.Range(0, 4).Select(r => Col(result, r, "City"));
            cities.Should().Contain("Казань");
        }

        [Fact(DisplayName = "Join: составной ключ из двух колонок")]
        public void Join_CompositeKey()
        {
            var left = new DataTable();
            left.Columns.Add("A", typeof(int));
            left.Columns.Add("B", typeof(string));
            left.Columns.Add("V", typeof(string));
            left.Rows.Add(1, "x", "L1");
            left.Rows.Add(1, "y", "L2");

            var right = new DataTable();
            right.Columns.Add("A", typeof(int));
            right.Columns.Add("B", typeof(string));
            right.Columns.Add("W", typeof(string));
            right.Rows.Add(1, "x", "R1");
            right.Rows.Add(2, "y", "R2");

            var result = _logic.Join(left, right, new[] { "A", "B" }, new[] { "A", "B" },
                DataTableJoinKind.Inner);

            result.Rows.Count.Should().Be(1, "совпадает только пара (1,x)");
            Col(result, 0, "V").Should().Be("L1");
            Col(result, 0, "W").Should().Be("R1");
        }

        [Fact(DisplayName = "Join: NULL-ключи не совпадают (семантика SQL)")]
        public void Join_NullKeys_DoNotMatch()
        {
            var left = new DataTable();
            left.Columns.Add("Id", typeof(string));
            left.Columns.Add("V", typeof(string));
            left.Rows.Add((object)DBNull.Value, "L-null");

            var right = new DataTable();
            right.Columns.Add("Id", typeof(string));
            right.Columns.Add("W", typeof(string));
            right.Rows.Add((object)DBNull.Value, "R-null");

            var result = _logic.Join(left, right, new[] { "Id" }, new[] { "Id" },
                DataTableJoinKind.Inner);

            result.Rows.Count.Should().Be(0, "NULL = NULL не даёт совпадения в SQL");
        }

        [Fact(DisplayName = "Join: конфликт имён колонок — суффикс _right")]
        public void Join_NameConflict_Suffix()
        {
            var left = new DataTable();
            left.Columns.Add("Id", typeof(int));
            left.Columns.Add("Value", typeof(string));
            left.Rows.Add(1, "left");

            var right = new DataTable();
            right.Columns.Add("Id", typeof(int));
            right.Columns.Add("Value", typeof(string));
            right.Rows.Add(1, "right");

            var result = _logic.Join(left, right, new[] { "Id" }, new[] { "Id" },
                DataTableJoinKind.Inner);

            result.Columns.Contains("Value").Should().BeTrue();
            result.Columns.Contains("Value_right").Should().BeTrue();
            Col(result, 0, "Value").Should().Be("left");
            Col(result, 0, "Value_right").Should().Be("right");
        }

        [Fact(DisplayName = "Join: дубликаты ключей справа — декартово по совпавшим")]
        public void Join_DuplicateKeys_Cartesian()
        {
            var right = new DataTable();
            right.Columns.Add("Id", typeof(int));
            right.Columns.Add("City", typeof(string));
            right.Rows.Add(1, "Москва-1");
            right.Rows.Add(1, "Москва-2");

            var result = _logic.Join(LeftTable(), right, new[] { "Id" }, new[] { "Id" },
                DataTableJoinKind.Inner);

            result.Rows.Count.Should().Be(2, "правая таблица содержит только Id=1: Иван × 2 строки");
            Enumerable.Range(0, 2).Select(r => Col(result, r, "City"))
                .Should().BeEquivalentTo("Москва-1", "Москва-2");
        }

        [Fact(DisplayName = "Join: разные числа ключей / отсутствующая колонка — InvalidOperationException")]
        public void Join_Validation_Throws()
        {
            Action mismatched = () => _logic.Join(LeftTable(), RightTable(),
                new[] { "Id" }, new[] { "Id", "City" }, DataTableJoinKind.Inner);
            Action missingColumn = () => _logic.Join(LeftTable(), RightTable(),
                new[] { "Nope" }, new[] { "Id" }, DataTableJoinKind.Inner);
            Action noKeys = () => _logic.Join(LeftTable(), RightTable(),
                new string[0], new string[0], DataTableJoinKind.Inner);

            mismatched.Should().Throw<InvalidOperationException>().WithMessage("*совпадать*");
            missingColumn.Should().Throw<InvalidOperationException>().WithMessage("*не найдена*");
            noKeys.Should().Throw<InvalidOperationException>().WithMessage("*ключевые колонки*");
        }

        [Fact(DisplayName = "Join: регистр строковых ключей")]
        public void Join_CaseSensitivity()
        {
            var left = new DataTable();
            left.Columns.Add("Code", typeof(string));
            left.Rows.Add("ABC");

            var right = new DataTable();
            right.Columns.Add("Code", typeof(string));
            right.Columns.Add("W", typeof(string));
            right.Rows.Add("abc", "matched");

            var insensitive = _logic.Join(left, right, new[] { "Code" }, new[] { "Code" },
                DataTableJoinKind.Inner, caseSensitive: false);
            var sensitive = _logic.Join(left, right, new[] { "Code" }, new[] { "Code" },
                DataTableJoinKind.Inner, caseSensitive: true);

            insensitive.Rows.Count.Should().Be(1);
            sensitive.Rows.Count.Should().Be(0);
        }

        // ── Pivot ──────────────────────────────────────────────────────

        private DataTable SalesTable()
        {
            var t = new DataTable("Sales");
            t.Columns.Add("Region", typeof(string));
            t.Columns.Add("Month", typeof(string));
            t.Columns.Add("Amount", typeof(int));
            t.Rows.Add("Москва", "Январь", 100);
            t.Rows.Add("Москва", "Февраль", 200);
            t.Rows.Add("Сочи", "Январь", 50);
            t.Rows.Add("Сочи", "Январь", 70); // дубликат для агрегата
            return t;
        }

        [Fact(DisplayName = "Pivot: суммы по строкам × месяцам")]
        public void Pivot_Sum()
        {
            var result = _logic.Pivot(SalesTable(), "Region", "Month", "Amount", DataTableAggregation.Sum);

            result.Columns.Cast<DataColumn>().Select(c => c.ColumnName)
                .Should().BeEquivalentTo("Region", "Январь", "Февраль");
            result.Rows.Count.Should().Be(2);

            var msk = result.Select("Region = 'Москва'")[0];
            msk["Январь"].Should().Be(100);
            msk["Февраль"].Should().Be(200);

            var sochi = result.Select("Region = 'Сочи'")[0];
            sochi["Январь"].Should().Be(120, "50 + 70 агрегируются");
            sochi["Февраль"].Should().Be(DBNull.Value, "данных нет");
        }

        [Fact(DisplayName = "Pivot: агрегаты Count/Min/Max/First")]
        public void Pivot_OtherAggregations()
        {
            var count = _logic.Pivot(SalesTable(), "Region", "Month", "Amount", DataTableAggregation.Count);
            count.Select("Region = 'Сочи'")[0]["Январь"].Should().Be(2);

            var min = _logic.Pivot(SalesTable(), "Region", "Month", "Amount", DataTableAggregation.Min);
            min.Select("Region = 'Сочи'")[0]["Январь"].Should().Be(50);

            var max = _logic.Pivot(SalesTable(), "Region", "Month", "Amount", DataTableAggregation.Max);
            max.Select("Region = 'Сочи'")[0]["Январь"].Should().Be(70);

            var first = _logic.Pivot(SalesTable(), "Region", "Month", "Amount", DataTableAggregation.First);
            first.Select("Region = 'Сочи'")[0]["Январь"].Should().Be(50);
        }

        [Fact(DisplayName = "Pivot: нечисловые значения для Sum → DBNull")]
        public void Pivot_Sum_NonNumeric_Null()
        {
            var t = new DataTable();
            t.Columns.Add("K", typeof(string));
            t.Columns.Add("P", typeof(string));
            t.Columns.Add("V", typeof(string));
            t.Rows.Add("r1", "p1", "текст");

            var result = _logic.Pivot(t, "K", "P", "V", DataTableAggregation.Sum);
            result.Rows[0]["p1"].Should().Be(DBNull.Value);
        }

        // ── Unpivot ────────────────────────────────────────────────────

        [Fact(DisplayName = "Unpivot: широкая форма → длинная")]
        public void Unpivot_Basic()
        {
            var wide = _logic.Pivot(SalesTable(), "Region", "Month", "Amount", DataTableAggregation.Sum);

            var longForm = _logic.Unpivot(wide, new[] { "Region" },
                new[] { "Январь", "Февраль" }, "Месяц", "Сумма");

            longForm.Columns.Cast<DataColumn>().Select(c => c.ColumnName)
                .Should().BeEquivalentTo("Region", "Месяц", "Сумма");
            longForm.Rows.Count.Should().Be(4, "2 региона × 2 месяца");

            var sochiFeb = longForm.Select("Region = 'Сочи' AND Месяц = 'Февраль'")[0];
            sochiFeb["Сумма"].Should().Be(DBNull.Value);
        }

        [Fact(DisplayName = "Unpivot: пустые списки колонок — InvalidOperationException")]
        public void Unpivot_EmptyColumns_Throws()
        {
            Action noKeys = () => _logic.Unpivot(SalesTable(), new string[0], new[] { "Amount" }, "V", "N");
            Action noValues = () => _logic.Unpivot(SalesTable(), new[] { "Region" }, new string[0], "V", "N");

            noKeys.Should().Throw<InvalidOperationException>();
            noValues.Should().Throw<InvalidOperationException>();
        }
    }
}
