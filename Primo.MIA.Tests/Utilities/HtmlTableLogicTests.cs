using System;
using System.Data;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.Utilities
{
    /// <summary>
    /// Тесты HtmlTableLogic (issue #36): заголовки, rowspan/colspan,
    /// вложенные таблицы, несколько таблиц, схлопывание пробелов, Guard.
    /// </summary>
    [Trait("Category", "Fast")]
    public class HtmlTableLogicTests
    {
        private readonly HtmlTableLogic _logic = new HtmlTableLogic();

        [Fact(DisplayName = "Простая таблица с th-заголовками")]
        public void SimpleTable_WithHeader()
        {
            var html = "<table><tr><th>Имя</th><th>Возраст</th></tr>" +
                       "<tr><td>Иван</td><td>30</td></tr>" +
                       "<tr><td>Мария</td><td>35</td></tr></table>";

            var result = _logic.ToDataTable(html, 0, true);

            result.Columns.Cast<DataColumn>().Select(c => c.ColumnName)
                .Should().BeEquivalentTo("Имя", "Возраст");
            result.Rows.Count.Should().Be(2);
            result.Rows[0]["Имя"].Should().Be("Иван");
            result.Rows[1]["Возраст"].Should().Be("35");
        }

        [Fact(DisplayName = "Без заголовков — Column1..N, все строки данные")]
        public void NoHeader_AutoColumns()
        {
            var html = "<table><tr><td>a</td><td>b</td></tr><tr><td>c</td><td>d</td></tr></table>";

            var result = _logic.ToDataTable(html, 0, false);

            result.Columns[0].ColumnName.Should().Be("Column1");
            result.Rows.Count.Should().Be(2);
        }

        [Fact(DisplayName = "colspan: ячейка занимает соседние колонки")]
        public void Colspan_Expands()
        {
            var html = "<table><tr><th>A</th><th>B</th><th>C</th></tr>" +
                       "<tr><td colspan=\"2\">широкая</td><td>x</td></tr></table>";

            var result = _logic.ToDataTable(html, 0, true);

            result.Rows[0]["A"].Should().Be("широкая");
            result.Rows[0]["B"].Should().Be("широкая", "colspan=2 дублирует значение");
            result.Rows[0]["C"].Should().Be("x");
        }

        [Fact(DisplayName = "rowspan: ячейка занимает слоты последующих строк")]
        public void Rowspan_ExtendsToNextRows()
        {
            var html = "<table><tr><th>Регион</th><th>Месяц</th><th>Сумма</th></tr>" +
                       "<tr><td rowspan=\"2\">Москва</td><td>Январь</td><td>100</td></tr>" +
                       "<tr><td>Февраль</td><td>200</td></tr>" +
                       "<tr><td>Сочи</td><td>Март</td><td>50</td></tr></table>";

            var result = _logic.ToDataTable(html, 0, true);

            result.Rows.Count.Should().Be(3);
            result.Rows[0]["Регион"].Should().Be("Москва");
            result.Rows[1]["Регион"].Should().Be("Москва", "rowspan=2 протянут во 2-ю строку");
            result.Rows[1]["Месяц"].Should().Be("Февраль", "слот занят rowspan'ом — январь смещён");
            result.Rows[2]["Регион"].Should().Be("Сочи");
            result.Rows[2]["Месяц"].Should().Be("Март");
        }

        [Fact(DisplayName = "rowspan+colspan одновременно")]
        public void Rowspan_Colspan_Combined()
        {
            var html = "<table><tr><th>A</th><th>B</th><th>C</th></tr>" +
                       "<tr><td rowspan=\"2\" colspan=\"2\">большая</td><td>r1c</td></tr>" +
                       "<tr><td>r2c</td></tr></table>";

            var result = _logic.ToDataTable(html, 0, true);

            result.Rows.Count.Should().Be(2);
            result.Rows[0]["A"].Should().Be("большая");
            result.Rows[0]["B"].Should().Be("большая");
            result.Rows[1]["A"].Should().Be("большая");
            result.Rows[1]["B"].Should().Be("большая");
            result.Rows[0]["C"].Should().Be("r1c");
            result.Rows[1]["C"].Should().Be("r2c");
        }

        [Fact(DisplayName = "Вложенные таблицы игнорируются")]
        public void NestedTable_Ignored()
        {
            var html = "<table id=\"outer\">" +
                       "<tr><td>внешняя-1</td></tr>" +
                       "<tr><td>" +
                       "<table><tr><td>внутренняя</td></tr></table>" +
                       "</td></tr>" +
                       "<tr><td>внешняя-2</td></tr></table>";

            var result = _logic.ToDataTable(html, 0, false);

            // строки внутренней таблицы не попадают в результат
            result.Rows.Count.Should().Be(3, "только строки внешней таблицы");
            Convert.ToString(result.Rows[1]["Column1"]).Should().Contain("внутренняя");
        }

        [Fact(DisplayName = "Несколько таблиц — выбор по индексу")]
        public void MultipleTables_SelectByIndex()
        {
            var html = "<table><tr><td>первая</td></tr></table>" +
                       "<table><tr><td>вторая</td></tr></table>";

            var first = _logic.ToDataTable(html, 0, false);
            var second = _logic.ToDataTable(html, 1, false);

            first.Rows[0]["Column1"].Should().Be("первая");
            second.Rows[0]["Column1"].Should().Be("вторая");
        }

        [Fact(DisplayName = "Индекс за пределами — InvalidOperationException")]
        public void InvalidIndex_Throws()
        {
            var html = "<table><tr><td>x</td></tr></table>";

            Action act = () => _logic.ToDataTable(html, 1, false);
            act.Should().Throw<InvalidOperationException>().WithMessage("*не найдена*");
        }

        [Fact(DisplayName = "Дубликаты заголовков — суффикс счётчиком")]
        public void DuplicateHeaders_Renamed()
        {
            var html = "<table><tr><th>Data</th><th>Data</th></tr><tr><td>1</td><td>2</td></tr></table>";

            var result = _logic.ToDataTable(html, 0, true);

            var names = result.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            names[0].Should().Be("Data");
            names[1].Should().Be("Data2", "дубликат переименован");
        }

        [Fact(DisplayName = "Пробелы схлопываются, &nbsp; → пробел")]
        public void Whitespace_Collapsed()
        {
            var html = "<table><tr><td>  много&nbsp;&nbsp;пробелов  </td></tr></table>";

            var result = _logic.ToDataTable(html, 0, false);

            result.Rows[0]["Column1"].Should().Be("много пробелов");
        }

        [Fact(DisplayName = "Пустая строка заголовка → ColumnN")]
        public void EmptyHeader_AutoName()
        {
            var html = "<table><tr><th></th><th>Есть</th></tr><tr><td>1</td><td>2</td></tr></table>";

            var result = _logic.ToDataTable(html, 0, true);

            result.Columns[0].ColumnName.Should().Be("Column1");
            result.Columns[1].ColumnName.Should().Be("Есть");
        }

        [Fact(DisplayName = "Ни одной таблицы — InvalidOperationException")]
        public void NoTables_Throws()
        {
            Action act = () => _logic.ToDataTable("<div>нет таблиц</div>", 0, true);
            act.Should().Throw<InvalidOperationException>().WithMessage("*не найдено*");
        }

        [Fact(DisplayName = "Пустые строки в таблице — InvalidOperationException")]
        public void EmptyTable_Throws()
        {
            Action act = () => _logic.ToDataTable("<table></table>", 0, true);
            act.Should().Throw<InvalidOperationException>().WithMessage("*нет строк*");
        }

        [Fact(DisplayName = "Guard: пустой HTML — ArgumentException")]
        public void EmptyHtml_Throws()
        {
            Action act = () => _logic.ToDataTable("", 0, true);
            act.Should().Throw<ArgumentException>();
        }
    }
}
