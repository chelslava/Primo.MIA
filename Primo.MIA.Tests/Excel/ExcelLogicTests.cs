using ClosedXML.Excel;
using FluentAssertions;
using System;
using System.Data;
using System.IO;
using Xunit;

namespace Primo.MIA.Tests.Excel
{
    /// <summary>
    /// Тесты ExcelLogic (issue #47): round-trip чтение/запись .xlsx через
    /// ClosedXML, режимы Overwrite/Append, заголовки, типизация значений.
    /// Только файловая система — сети и Excel не нужны.
    /// </summary>
    [Trait("Category", "Fast")]
    public class ExcelLogicTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _filePath;

        public ExcelLogicTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "primo-mia-excel-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _filePath = Path.Combine(_tempDir, "test.xlsx");
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // файл может быть заблокирован параллельным прогоном — не критично
            }
        }

        #region Round-trip

        [Fact(DisplayName = "Round-trip: типизированные значения и заголовки сохраняются")]
        public void RoundTrip_TypedValuesWithHeaders()
        {
            var table = new DataTable("Отчёт");
            table.Columns.Add("Имя", typeof(string));
            table.Columns.Add("Количество", typeof(int));
            table.Columns.Add("Сумма", typeof(double));
            table.Columns.Add("Активен", typeof(bool));
            table.Columns.Add("Дата", typeof(DateTime));
            table.Rows.Add("Иванов", 5, 123.45, true, new DateTime(2026, 11, 17));
            table.Rows.Add("Петров", 3, 0.5, false, new DateTime(2026, 1, 1));

            int written = ExcelLogic.WriteRange(_filePath, "Данные", "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: true, table: table);
            written.Should().Be(2);

            var read = ExcelLogic.ReadRange(_filePath, "Данные", null, headerRow: true);

            read.Columns.Count.Should().Be(5);
            read.Columns["Имя"].Should().NotBeNull();
            read.Columns["Количество"].Should().NotBeNull();
            read.Rows.Count.Should().Be(2);
            read.Rows[0]["Имя"].Should().Be("Иванов");
            read.Rows[0]["Количество"].Should().Be(5d);          // число читается как Double
            read.Rows[0]["Сумма"].Should().Be(123.45d);
            read.Rows[0]["Активен"].Should().Be(true);
            read.Rows[0]["Дата"].Should().Be(new DateTime(2026, 11, 17));
            read.Rows[1]["Имя"].Should().Be("Петров");
        }

        [Fact(DisplayName = "Чтение без заголовков — обезличенные колонки Колонка1..N")]
        public void ReadRange_NoHeaderRow_GenericColumnNames()
        {
            var table = new DataTable();
            table.Columns.Add("x");
            table.Rows.Add("a");
            table.Rows.Add("b");
            ExcelLogic.WriteRange(_filePath, null, "B2",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);

            var read = ExcelLogic.ReadRange(_filePath, null, null, headerRow: false);

            read.Columns.Count.Should().Be(1);
            read.Columns[0].ColumnName.Should().Be("Колонка1");
            read.Rows[0][0].Should().Be("a");
        }

        [Fact(DisplayName = "Чтение заданного диапазона — только указанные ячейки")]
        public void ReadRange_ExplicitRange()
        {
            var table = new DataTable();
            table.Columns.Add("A");
            table.Columns.Add("B");
            table.Rows.Add("a1", "b1");
            table.Rows.Add("a2", "b2");
            table.Rows.Add("a3", "b3");
            ExcelLogic.WriteRange(_filePath, null, "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);

            var read = ExcelLogic.ReadRange(_filePath, null, "A2:B3", headerRow: false);

            read.Rows.Count.Should().Be(2);
            read.Rows[0][0].Should().Be("a2");
            read.Rows[1][1].Should().Be("b3");
        }

        [Fact(DisplayName = "Дубликаты имён колонок обезличиваются")]
        public void ReadRange_DuplicateHeaders()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Лист1");
            ws.Cell("A1").Value = "Имя";
            ws.Cell("B1").Value = "Имя";
            ws.Cell("A2").Value = "значение";
            workbook.SaveAs(_filePath);

            var read = ExcelLogic.ReadRange(_filePath, null, null, headerRow: true);

            read.Columns.Count.Should().Be(2);
            read.Columns[0].ColumnName.Should().Be("Имя");
            read.Columns[1].ColumnName.Should().Be("Имя2");
        }

        [Fact(DisplayName = "DBNull в середине строки → пустая ячейка на своей позиции")]
        public void WriteRange_NullValueStaysEmpty()
        {
            var table = new DataTable();
            table.Columns.Add("x");
            table.Columns.Add("y");
            table.Columns.Add("z");
            table.Rows.Add("лево", DBNull.Value, "право");
            ExcelLogic.WriteRange(_filePath, null, "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);

            var read = ExcelLogic.ReadRange(_filePath, null, null, headerRow: false);
            read.Rows.Count.Should().Be(1);
            read.Rows[0][0].Should().Be("лево");
            read.Rows[0][1].Should().Be(string.Empty);
            read.Rows[0][2].Should().Be("право");
        }

        [Fact(DisplayName = "Строка из одних пустых значений — лист читается пустым (ClosedXML)")]
        public void WriteRange_AllEmptyRow()
        {
            var table = new DataTable();
            table.Columns.Add("x");
            table.Rows.Add(DBNull.Value);
            ExcelLogic.WriteRange(_filePath, null, "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);

            // ClosedXML не считает пустые строки «использованным диапазоном» —
            // честная семантика: на листе ничего не видно → чтение возвращает пустую таблицу
            var read = ExcelLogic.ReadRange(_filePath, null, null, headerRow: false);
            read.Rows.Count.Should().Be(0);
        }

        [Fact(DisplayName = "Текст «=…» не становится формулой")]
        public void WriteRange_StringFormulaStaysText()
        {
            var table = new DataTable();
            table.Columns.Add("x");
            table.Rows.Add("=1+1");
            ExcelLogic.WriteRange(_filePath, null, "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);

            using var wb = new XLWorkbook(_filePath);
            wb.Worksheet(1).Cell("A1").DataType.Should().Be(XLDataType.Text);
            wb.Worksheet(1).Cell("A1").GetText().Should().Be("=1+1");
        }

        #endregion

        #region Режимы записи

        [Fact(DisplayName = "Append: дописывание после последней занятой строки")]
        public void WriteRange_Append()
        {
            var first = new DataTable();
            first.Columns.Add("x");
            first.Rows.Add("строка1");
            first.Rows.Add("строка2");
            ExcelLogic.WriteRange(_filePath, null, "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: first);

            var second = new DataTable();
            second.Columns.Add("x");
            second.Rows.Add("строка3");
            ExcelLogic.WriteRange(_filePath, null, "A1",
                ExcelWriteMode.Append, createIfMissing: false, writeHeaders: false, table: second);

            var read = ExcelLogic.ReadRange(_filePath, null, null, headerRow: false);
            read.Rows.Count.Should().Be(3);
            read.Rows[2][0].Should().Be("строка3");
        }

        [Fact(DisplayName = "Отсутствующий файл: createIfMissing=false → ошибка, true → создаётся")]
        public void WriteRange_CreateIfMissing()
        {
            var table = new DataTable();
            table.Columns.Add("x");
            table.Rows.Add("v");

            Action act = () => ExcelLogic.WriteRange(_filePath, null, "A1",
                ExcelWriteMode.Overwrite, createIfMissing: false, writeHeaders: false, table: table);
            act.Should().Throw<FileNotFoundException>();

            ExcelLogic.WriteRange(_filePath, "ЛистX", "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);
            File.Exists(_filePath).Should().BeTrue();
        }

        [Fact(DisplayName = "Новый файл без имени листа → DefaultSheetName")]
        public void WriteRange_NewFileDefaultSheet()
        {
            var table = new DataTable();
            table.Columns.Add("x");
            table.Rows.Add("v");
            ExcelLogic.WriteRange(_filePath, "", "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);

            ExcelLogic.GetSheetNames(_filePath).Should().Contain(ExcelLogic.DefaultSheetName);
        }

        [Fact(DisplayName = "Новый лист создаётся в существующем файле")]
        public void WriteRange_NewSheetInExistingFile()
        {
            var table = new DataTable();
            table.Columns.Add("x");
            table.Rows.Add("v");
            ExcelLogic.WriteRange(_filePath, "Первый", "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);
            ExcelLogic.WriteRange(_filePath, "Второй", "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);

            ExcelLogic.GetSheetNames(_filePath).Should().Contain("Первый").And.Contain("Второй");
        }

        #endregion

        #region Формула на столбец (issue #47)

        [Fact(DisplayName = "FillColumnFormula: {row} подставляется по строкам")]
        public void FillColumnFormula_RowPlaceholder()
        {
            var table = new DataTable();
            table.Columns.Add("a", typeof(double));
            table.Columns.Add("b", typeof(double));
            table.Rows.Add(2d, 3d);
            table.Rows.Add(4d, 5d);
            ExcelLogic.WriteRange(_filePath, null, "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);

            int filled = ExcelLogic.FillColumnFormula(_filePath, null, "C", "=A{row}*B{row}", 1, 2);
            filled.Should().Be(2);

            using var wb = new XLWorkbook(_filePath);
            wb.Worksheet(1).Cell("C1").FormulaA1.Should().Be("A1*B1");
            wb.Worksheet(1).Cell("C2").FormulaA1.Should().Be("A2*B2");
        }

        [Fact(DisplayName = "FillColumnFormula: без {row} формула одинакова; без «=» добавляется")]
        public void FillColumnFormula_NoPlaceholder_AutoEquals()
        {
            var table = new DataTable();
            table.Columns.Add("x");
            table.Rows.Add("v");
            table.Rows.Add("v");
            ExcelLogic.WriteRange(_filePath, null, "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);

            ExcelLogic.FillColumnFormula(_filePath, null, "B", "СУММА", 1, 2);

            using var wb = new XLWorkbook(_filePath);
            wb.Worksheet(1).Cell("B1").FormulaA1.Should().Be("СУММА");
            wb.Worksheet(1).Cell("B2").FormulaA1.Should().Be("СУММА");
        }

        [Fact(DisplayName = "FillColumnFormula: нижний регистр столбца нормализуется")]
        public void FillColumnFormula_LowercaseColumn()
        {
            var table = new DataTable();
            table.Columns.Add("x");
            table.Rows.Add("v");
            ExcelLogic.WriteRange(_filePath, null, "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);

            int filled = ExcelLogic.FillColumnFormula(_filePath, null, "b", "=1", 1, 1);
            filled.Should().Be(1);

            using var wb = new XLWorkbook(_filePath);
            wb.Worksheet(1).Cell("B1").HasFormula.Should().BeTrue();
        }

        [Fact(DisplayName = "FillColumnFormula: ошибки — некорректный столбец/строки/файл")]
        public void FillColumnFormula_Errors()
        {
            var table = new DataTable();
            table.Columns.Add("x");
            table.Rows.Add("v");
            ExcelLogic.WriteRange(_filePath, null, "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);

            Action badColumn = () => ExcelLogic.FillColumnFormula(_filePath, null, "12C", "=1", 1, 2);
            badColumn.Should().Throw<FormatException>();

            Action badRows = () => ExcelLogic.FillColumnFormula(_filePath, null, "A", "=1", 5, 2);
            badRows.Should().Throw<ArgumentOutOfRangeException>();

            Action badStart = () => ExcelLogic.FillColumnFormula(_filePath, null, "A", "=1", 0, 2);
            badStart.Should().Throw<ArgumentOutOfRangeException>();

            Action missing = () => ExcelLogic.FillColumnFormula(
                Path.Combine(_tempDir, "нет-файла.xlsx"), null, "A", "=1", 1, 2);
            missing.Should().Throw<FileNotFoundException>();
        }

        [Fact(DisplayName = "Round-trip 10 000 строк — линейное время, целостность данных")]
        public void RoundTrip_10kRows()
        {
            var table = new DataTable("Большой");
            table.Columns.Add("Ид", typeof(int));
            table.Columns.Add("Значение", typeof(double));
            for (int i = 1; i <= 10_000; i++)
                table.Rows.Add(i, i * 0.5);

            int written = ExcelLogic.WriteRange(_filePath, "Большой", "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);
            written.Should().Be(10_000);

            var read = ExcelLogic.ReadRange(_filePath, "Большой", null, headerRow: false);
            read.Rows.Count.Should().Be(10_000);
            read.Rows[0][0].Should().Be(1d);
            read.Rows[9_999][0].Should().Be(10_000d);
            read.Rows[9_999][1].Should().Be(5_000d);
        }

        #endregion

        #region Ошибки

        [Fact(DisplayName = "Чтение: файл отсутствует → FileNotFoundException")]
        public void ReadRange_MissingFile()
        {
            Action act = () => ExcelLogic.ReadRange(_filePath, null, null, headerRow: true);
            act.Should().Throw<FileNotFoundException>();
        }

        [Fact(DisplayName = "Чтение: несуществующий лист → понятное исключение")]
        public void ReadRange_MissingSheet()
        {
            var table = new DataTable();
            table.Columns.Add("x");
            ExcelLogic.WriteRange(_filePath, "Есть", "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);

            Action act = () => ExcelLogic.ReadRange(_filePath, "Нету", null, headerRow: true);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Нету*");
        }

        [Fact(DisplayName = "Чтение: пустой лист → пустой DataTable")]
        public void ReadRange_EmptySheet()
        {
            using (var wb = new XLWorkbook())
            {
                wb.Worksheets.Add("Пустой");
                wb.SaveAs(_filePath);
            }

            var read = ExcelLogic.ReadRange(_filePath, null, null, headerRow: true);
            read.Rows.Count.Should().Be(0);
            read.Columns.Count.Should().Be(0);
        }

        [Fact(DisplayName = "Чтение: некорректный диапазон → FormatException")]
        public void ReadRange_InvalidRange()
        {
            var table = new DataTable();
            table.Columns.Add("x");
            table.Rows.Add("v");
            ExcelLogic.WriteRange(_filePath, null, "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);

            Action act = () => ExcelLogic.ReadRange(_filePath, null, "НЕ_ДИАПАЗОН", headerRow: false);
            act.Should().Throw<FormatException>();
        }

        [Fact(DisplayName = "Запись: некорректная начальная ячейка → FormatException")]
        public void WriteRange_InvalidStartCell()
        {
            var table = new DataTable();
            table.Columns.Add("x");
            table.Rows.Add("v");
            ExcelLogic.WriteRange(_filePath, null, "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: table);

            Action act = () => ExcelLogic.WriteRange(_filePath, null, "123XYZ",
                ExcelWriteMode.Overwrite, createIfMissing: false, writeHeaders: false, table: table);
            act.Should().Throw<FormatException>();
        }

        [Fact(DisplayName = "Чтение: пустой путь → ArgumentException (Guard)")]
        public void ReadRange_EmptyPath()
        {
            Action act = () => ExcelLogic.ReadRange("", null, null, headerRow: true);
            act.Should().Throw<ArgumentException>();
        }

        [Fact(DisplayName = "Запись: таблица null → ArgumentNullException")]
        public void WriteRange_NullTable()
        {
            Action act = () => ExcelLogic.WriteRange(_filePath, null, "A1",
                ExcelWriteMode.Overwrite, createIfMissing: true, writeHeaders: false, table: null);
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion
    }
}
