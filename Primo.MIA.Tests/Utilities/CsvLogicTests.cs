using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.Utilities
{
    /// <summary>
    /// Тесты CSV ↔ DataTable по RFC 4180 (issue #29): кавычки, вложенные
    /// разделители/переводы строк, разделители, типизация, round-trip, кодировки.
    /// </summary>
    [Trait("Category", "Fast")]
    public class CsvLogicTests : IDisposable
    {
        private readonly CsvLogic _logic = new CsvLogic();

        private readonly string _workDir = Path.Combine(
            Path.GetTempPath(), "mia-csv-tests-" + Guid.NewGuid().ToString("N"));

        public CsvLogicTests()
        {
            Directory.CreateDirectory(_workDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_workDir))
                    Directory.Delete(_workDir, recursive: true);
            }
            catch
            {
                // временная папка может быть занята — не роняем тесты уборкой
            }
        }

        // ── Парсер: RFC 4180 векторы ───────────────────────────────────

        [Fact(DisplayName = "Parse: простая таблица с заголовками")]
        public void Parse_SimpleTable_WithHeader()
        {
            var table = _logic.ParseToDataTable("Name,Age\nИван,30\nМария,35\n", ",", true, false);

            table.Columns.Count.Should().Be(2);
            table.Columns[0].ColumnName.Should().Be("Name");
            table.Columns[1].ColumnName.Should().Be("Age");
            table.Rows.Count.Should().Be(2);
            table.Rows[0]["Name"].Should().Be("Иван");
            table.Rows[1]["Age"].Should().Be("35");
        }

        [Fact(DisplayName = "Parse: без заголовков — колонки Column1..N")]
        public void Parse_NoHeader_AutoColumnNames()
        {
            var table = _logic.ParseToDataTable("a,b\nc,d\n", ",", false, false);

            table.Columns[0].ColumnName.Should().Be("Column1");
            table.Columns[1].ColumnName.Should().Be("Column2");
            table.Rows.Count.Should().Be(2, "первая строка — данные, не заголовок");
        }

        [Fact(DisplayName = "RFC 4180: кавычкованное поле с разделителем внутри")]
        public void Parse_QuotedField_WithDelimiterInside()
        {
            var table = _logic.ParseToDataTable("Name,Note\nИван,\"Мск, Тверская\"\n", ",", true, false);

            table.Rows[0]["Note"].Should().Be("Мск, Тверская");
        }

        [Fact(DisplayName = "RFC 4180: удвоенная кавычка — буквальная кавычка в данных")]
        public void Parse_DoubledQuotes_LiteralQuote()
        {
            var table = _logic.ParseToDataTable("Note\n\"Он сказал \"\"привет\"\"\"\n", ",", true, false);

            table.Rows[0]["Note"].Should().Be("Он сказал \"привет\"");
        }

        [Fact(DisplayName = "RFC 4180: перевод строки внутри кавычкованного поля")]
        public void Parse_NewlineInsideQuotedField()
        {
            var table = _logic.ParseToDataTable("Note\n\"строка 1\nстрока 2\"\n", ",", true, false);

            table.Rows.Count.Should().Be(1, "перевод строки внутри кавычек — часть данных");
            table.Rows[0]["Note"].Should().Be("строка 1\nстрока 2");
        }

        [Fact(DisplayName = "RFC 4180: CRLF как граница записи, LF тоже")]
        public void Parse_CrlfAndLf_RecordBoundaries()
        {
            var crlf = _logic.ParseToDataTable("A\r\n1\r\n2\r\n", ",", true, false);
            var lf = _logic.ParseToDataTable("A\n1\n2\n", ",", true, false);
            var mixed = _logic.ParseToDataTable("A\r\n1\n2\r\n", ",", true, false);

            crlf.Rows.Count.Should().Be(2);
            lf.Rows.Count.Should().Be(2);
            mixed.Rows.Count.Should().Be(2);
        }

        [Fact(DisplayName = "Parse: строки короче заголовка — пустые поля, длиннее — обрезаются?")]
        public void Parse_RaggedRows_Padded()
        {
            // Строка с меньшим числом полей дополняется пустыми
            var table = _logic.ParseToDataTable("A,B,C\n1\n1,2,3,4\n", ",", true, false);

            table.Columns.Count.Should().Be(3, "число колонок = максимуму по записям");
            table.Rows[0]["B"].Should().Be("", "недостающие поля пустые");
            // Лишнее поле в строке 4 игнорируется (колонок 3)
            table.Rows[1]["C"].Should().Be("3");
        }

        [Fact(DisplayName = "Parse: пустой CSV — пустая таблица")]
        public void Parse_EmptyInput_EmptyTable()
        {
            var table = _logic.ParseToDataTable("", ",", true, false);
            table.Rows.Count.Should().Be(0);
            table.Columns.Count.Should().Be(0);
        }

        [Fact(DisplayName = "Parse: пустые поля между разделителями — пустые строки")]
        public void Parse_EmptyFields_Preserved()
        {
            var table = _logic.ParseToDataTable("A,B\n,2\n", ",", true, false);

            table.Rows[0]["A"].Should().Be("");
            table.Rows[0]["B"].Should().Be("2");
        }

        // ── Разделители ────────────────────────────────────────────────

        [Theory(DisplayName = "Parse/Build: именованные и кастомные разделители")]
        [InlineData(";", "a;b")]
        [InlineData("|", "a|b")]
        [InlineData("\t", "a\tb")]
        [InlineData("tab", "a\tb")]
        public void Parse_CustomDelimiters(string delimiter, string input)
        {
            var table = _logic.ParseToDataTable(input + "\n1;2\n".Replace(";", delimiter == "tab" ? "\t" : delimiter == "|" ? "|" : ";"),
                delimiter, true, false);

            table.Columns.Count.Should().Be(2);
        }

        [Fact(DisplayName = "Parse: пустой разделитель → запятая по умолчанию")]
        public void Parse_EmptyDelimiter_DefaultsToComma()
        {
            var table = _logic.ParseToDataTable("A,B\n1,2\n", null, true, false);
            table.Columns.Count.Should().Be(2);
        }

        // ── Типизация ──────────────────────────────────────────────────

        [Fact(DisplayName = "TypeInference: int/double/bool/DateTime по InvariantCulture")]
        public void Parse_TypeInference_ColumnTypes()
        {
            var csv = "Count,Price,Active,Born,Free\n1,1.5,true,2024-01-15,x\n2,2.5,false,2024-06-01,y\n";
            var table = _logic.ParseToDataTable(csv, ",", true, true);

            table.Columns["Count"].DataType.Should().Be(typeof(int));
            table.Columns["Price"].DataType.Should().Be(typeof(double));
            table.Columns["Active"].DataType.Should().Be(typeof(bool));
            table.Columns["Born"].DataType.Should().Be(typeof(DateTime));
            table.Columns["Free"].DataType.Should().Be(typeof(string), "не конвертируется ни в один тип");
            ((int)table.Rows[0]["Count"]).Should().Be(1);
            ((double)table.Rows[1]["Price"]).Should().Be(2.5);
        }

        [Fact(DisplayName = "TypeInference: mixed-колонка остаётся string")]
        public void Parse_TypeInference_MixedColumn_StaysString()
        {
            var csv = "Val\n1\nabc\n";
            var table = _logic.ParseToDataTable(csv, ",", true, true);

            table.Columns["Val"].DataType.Should().Be(typeof(string));
        }

        [Fact(DisplayName = "TypeInference: выключена — все string")]
        public void Parse_NoInference_AllStrings()
        {
            var csv = "Count\n1\n2\n";
            var table = _logic.ParseToDataTable(csv, ",", true, false);
            table.Columns["Count"].DataType.Should().Be(typeof(string));
        }

        // ── Запись ─────────────────────────────────────────────────────

        [Fact(DisplayName = "Build: кавычки только при необходимости")]
        public void Build_QuotesOnlyWhenNeeded()
        {
            var table = new DataTable();
            table.Columns.Add("A");
            table.Columns.Add("B");
            table.Rows.Add("plain", "с запятой, внутри");
            table.Rows.Add("с кавычкой \"", "ok");

            var csv = _logic.BuildCsv(table, ",", true, true);

            // RFC 4180 §2.1: записи разделяются CRLF
            csv.Should().StartWith("A,B\r\n");
            csv.Should().Contain("plain,\"с запятой, внутри\"");
            csv.Should().Contain("\"с кавычкой \"\"\"");
        }

        [Fact(DisplayName = "Build: без заголовков и DBNULL → пусто")]
        public void Build_NoHeader_DBNullEmpty()
        {
            var table = new DataTable();
            table.Columns.Add("A");
            table.Columns.Add("B");
            table.Rows.Add("x", DBNull.Value);

            var csv = _logic.BuildCsv(table, ",", false, true);

            // RFC 4180 §2.1: записи разделяются CRLF
            csv.Should().Be("x,\r\n");
            csv.Should().NotContain("A,B", "заголовки выключены");
        }

        [Fact(DisplayName = "Round-trip: DataTable → CSV → DataTable сохраняет данные")]
        public void RoundTrip_PreservesData()
        {
            var table = new DataTable();
            table.Columns.Add("Name");
            table.Columns.Add("Note");
            table.Rows.Add("Иван", "Мск, Тверская");
            table.Rows.Add("Мария", "с \"кавычками\" и\nпереносом");
            table.Rows.Add("Пётр", "обычный");

            var csv = _logic.BuildCsv(table, ",", true, true);
            var restored = _logic.ParseToDataTable(csv, ",", true, false);

            restored.Rows.Count.Should().Be(3);
            restored.Rows[0]["Note"].Should().Be("Мск, Тверская");
            restored.Rows[1]["Note"].Should().Be("с \"кавычками\" и\nпереносом");
            restored.Rows[2]["Name"].Should().Be("Пётр");
        }

        // ── Файлы и кодировки ──────────────────────────────────────────

        [Fact(DisplayName = "Файл: round-trip UTF-8 с кириллицей")]
        public void FileRoundTrip_Utf8()
        {
            var table = new DataTable();
            table.Columns.Add("Имя");
            table.Rows.Add("Иван");
            var path = Path.Combine(_workDir, "out-utf8.csv");

            _logic.WriteCsvToFile(table, path, ",", true, true, null);
            var restored = _logic.ParseFileToDataTable(path, ",", true, false, null);

            restored.Rows[0]["Имя"].Should().Be("Иван");
        }

        [Fact(DisplayName = "Файл: windows-1251 round-trip")]
        public void FileRoundTrip_Windows1251()
        {
            var path = Path.Combine(_workDir, "win1251.csv");
            File.WriteAllText(path, "Имя\nСергей\n", Encoding.GetEncoding("windows-1251"));

            var restored = _logic.ParseFileToDataTable(path, ",", true, false, "windows-1251");

            restored.Rows[0]["Имя"].Should().Be("Сергей");
        }

        [Fact(DisplayName = "Файл: BOM UTF-8 определяется автоматически")]
        public void FileRead_DetectsBom()
        {
            var path = Path.Combine(_workDir, "bom.csv");
            File.WriteAllText(path, "A\n1\n", new UTF8Encoding(true));

            // Читаем с явной windows-1251 — BOM перекроет: StreamReader с
            // detectEncodingFromByteOrderMarks использует BOM поверх кодировки
            var restored = _logic.ParseFileToDataTable(path, ",", true, false, "windows-1251");
            restored.Columns[0].ColumnName.Should().Be("A");
        }

        [Fact(DisplayName = "Файл: неизвестная кодировка — InvalidOperationException")]
        public void FileRead_UnknownEncoding_Throws()
        {
            var path = Path.Combine(_workDir, "any.csv");
            File.WriteAllText(path, "A\n1\n");

            Action act = () => _logic.ParseFileToDataTable(path, ",", true, false, "не-кодировка-xyz");

            act.Should().Throw<InvalidOperationException>().WithMessage("*кодировка*");
        }

        [Fact(DisplayName = "Файл: несуществующий файл — FileNotFoundException")]
        public void FileRead_MissingFile_Throws()
        {
            Action act = () => _logic.ParseFileToDataTable(
                Path.Combine(_workDir, "missing.csv"), ",", true, false, null);

            act.Should().Throw<FileNotFoundException>();
        }

        [Fact(DisplayName = "Файл: папка создаётся при записи")]
        public void FileWrite_CreatesDirectory()
        {
            var table = new DataTable();
            table.Columns.Add("A");
            table.Rows.Add("1");
            var path = Path.Combine(_workDir, "deep", "nested", "out.csv");

            _logic.WriteCsvToFile(table, path, ",", true, true, null);

            File.Exists(path).Should().BeTrue();
        }

        // ── Guard ──────────────────────────────────────────────────────

        [Fact(DisplayName = "Guard: null CSV — ArgumentException")]
        public void Parse_NullCsv_Throws()
        {
            Action act = () => _logic.ParseToDataTable(null, ",", true, false);
            act.Should().Throw<ArgumentException>();
        }

        [Fact(DisplayName = "Guard: null DataTable / пустой путь — ArgumentException")]
        public void Build_NullTable_Throws()
        {
            Action noTable = () => _logic.BuildCsv(null, ",", true, true);
            Action noPath = () => _logic.WriteCsvToFile(new DataTable(), " ", ",", true, true, null);

            noTable.Should().Throw<ArgumentException>();
            noPath.Should().Throw<ArgumentException>();
        }
    }
}
