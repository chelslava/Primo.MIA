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
    /// Тесты расширенных CSV-опций (issue #48): настраиваемый символ кавычек
    /// и экранирования, строки-комментарии, потоковое чтение, append-запись.
    /// Все опции опциональны — дефолтные вызовы покрыты CsvLogicTests.
    /// </summary>
    [Trait("Category", "Fast")]
    public class CsvOptionsLogicTests : IDisposable
    {
        private readonly CsvLogic _logic = new CsvLogic();

        private readonly string _workDir = Path.Combine(
            Path.GetTempPath(), "mia-csvopt-tests-" + Guid.NewGuid().ToString("N"));

        public CsvOptionsLogicTests()
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

        private string TempPath(string name) => Path.Combine(_workDir, name);

        // ── CsvOptions.Parse: валидация символов ───────────────────────

        [Fact(DisplayName = "ParseCharOption: пусто → дефолт, 1 символ → он")]
        public void ParseCharOption_Basics()
        {
            CsvOptions.ParseCharOption(null, '"', "Кавычка").Should().Be('"');
            CsvOptions.ParseCharOption("", '"', "Кавычка").Should().Be('"');
            CsvOptions.ParseCharOption("  ", '"', "Кавычка").Should().Be('"');
            CsvOptions.ParseCharOption("'", '"', "Кавычка").Should().Be('\'');
        }

        [Fact(DisplayName = "ParseCharOption: многосимвольное значение → понятная ошибка")]
        public void ParseCharOption_MultiChar_Throws()
        {
            Action act = () => CsvOptions.ParseCharOption("ab", '"', "Кавычка");
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Кавычка*ab*");
        }

        [Fact(DisplayName = "ParseOptionalCharOption: пусто → null (отключено)")]
        public void ParseOptionalCharOption_Basics()
        {
            CsvOptions.ParseOptionalCharOption(null, "Экранирование").Should().BeNull();
            CsvOptions.ParseOptionalCharOption("", "Экранирование").Should().BeNull();
            CsvOptions.ParseOptionalCharOption("\\", "Экранирование").Should().Be('\\');
            Action act = () => CsvOptions.ParseOptionalCharOption("##", "Комментарий");
            act.Should().Throw<InvalidOperationException>().WithMessage("*Комментарий*##*");
        }

        [Fact(DisplayName = "Parse: escape, совпадающий с кавычкой, отключается")]
        public void Parse_EscapeEqualsQuote_Disabled()
        {
            var options = CsvOptions.Parse(",", "'", "'", null);
            options.EscapeChar.Should().BeNull();

            var optionsDefault = CsvOptions.Parse(",", "\"", "\"", null);
            optionsDefault.EscapeChar.Should().BeNull();
        }

        // ── Настраиваемый символ кавычек ────────────────────────────────

        [Fact(DisplayName = "Чтение: одинарные кавычки как quote-символ")]
        public void Parse_CustomQuoteChar_Reads()
        {
            var options = new CsvOptions { Delimiter = ",", QuoteChar = '\'' };
            var table = _logic.ParseToDataTable("Name,Note\nИван,'Мск, Тверская'\n", options, true, false);

            table.Rows[0]["Note"].Should().Be("Мск, Тверская");
        }

        [Fact(DisplayName = "Round-trip: одинарные кавычки (чтение → запись → чтение)")]
        public void RoundTrip_CustomQuoteChar()
        {
            var options = new CsvOptions { Delimiter = ",", QuoteChar = '\'' };

            var source = _logic.ParseToDataTable("Name,Note\nИван,'Мск, Тверская'\n", options, true, false);
            var csv = _logic.BuildCsv(source, options, true, true);
            var restored = _logic.ParseToDataTable(csv, options, true, false);

            restored.Columns.Count.Should().Be(source.Columns.Count);
            restored.Rows.Count.Should().Be(1);
            restored.Rows[0]["Note"].Should().Be("Мск, Тверская");
        }

        [Fact(DisplayName = "Запись: кавычки ставятся по quote-символу, разделитель экранируется")]
        public void BuildCsv_CustomQuoteChar_Writes()
        {
            var options = new CsvOptions { Delimiter = ",", QuoteChar = '\'' };
            var table = new DataTable();
            table.Columns.Add("A");
            table.Columns.Add("B");
            table.Rows.Add("x,y", "просто текст");

            var csv = _logic.BuildCsv(table, options, true, true);

            // кавычковаться должно по ' — B без спецсимволов остаётся чистым
            csv.Should().Contain("'x,y'");
            csv.Should().Contain("просто текст");
            csv.Should().NotContain("\"x,y\"");
        }

        // ── Символ экранирования ────────────────────────────────────────

        [Fact(DisplayName = "Чтение: backslash-экранирование кавычки внутри поля")]
        public void Parse_EscapeChar_Reads()
        {
            var options = new CsvOptions { Delimiter = ",", QuoteChar = '"', EscapeChar = '\\' };
            var table = _logic.ParseToDataTable("Note\n\"Он сказал \\\"привет\\\"\"\n", options, true, false);

            table.Rows[0]["Note"].Should().Be("Он сказал \"привет\"");
        }

        [Fact(DisplayName = "Чтение: backslash-экранирование самого себя")]
        public void Parse_EscapeChar_EscapesItself()
        {
            var options = new CsvOptions { Delimiter = ",", QuoteChar = '"', EscapeChar = '\\' };
            // внутри кавычкованного поля \\ → один литеральный \
            var table = _logic.ParseToDataTable("Path\n\"C:\\\\dir\\\\file\"\n", options, true, false);

            table.Rows[0]["Path"].Should().Be("C:\\dir\\file");
        }

        [Fact(DisplayName = "Round-trip: диалект с backslash-экранированием")]
        public void RoundTrip_EscapeChar()
        {
            var options = new CsvOptions { Delimiter = ",", QuoteChar = '"', EscapeChar = '\\' };

            var table = new DataTable();
            table.Columns.Add("Note");
            table.Rows.Add("Он сказал \"привет\" и ушёл");

            var csv = _logic.BuildCsv(table, options, true, true);
            // при escape-диалекте кавычки внутри поля пишутся через \", а не ""
            csv.Should().Contain("\\\"привет\\\"");

            var restored = _logic.ParseToDataTable(csv, options, true, false);
            restored.Rows[0]["Note"].Should().Be("Он сказал \"привет\" и ушёл");
        }

        [Fact(DisplayName = "Дефолт: без escape работает каноничное удвоение кавычек")]
        public void Default_Doubling_StillWorks()
        {
            var options = CsvOptions.Parse(",", null, null, null);
            options.QuoteChar.Should().Be('"');
            options.EscapeChar.Should().BeNull();

            var table = _logic.ParseToDataTable("Note\n\"Он сказал \"\"привет\"\"\"\n", options, true, false);
            table.Rows[0]["Note"].Should().Be("Он сказал \"привет\"");
        }

        // ── Строки-комментарии ──────────────────────────────────────────

        [Fact(DisplayName = "Чтение: строки-комментарии пропускаются")]
        public void Parse_CommentChar_SkipsLines()
        {
            var options = new CsvOptions { Delimiter = ",", CommentChar = '#' };
            var csv = "# комментарий в начале\nName,Age\n# ещё комментарий\nИван,30\n";

            var table = _logic.ParseToDataTable(csv, options, true, false);

            table.Rows.Count.Should().Be(1);
            table.Rows[0]["Name"].Should().Be("Иван");
        }

        [Fact(DisplayName = "Чтение: комментарий в середине данных не ломает порядок")]
        public void Parse_CommentChar_MidData()
        {
            var options = new CsvOptions { Delimiter = ",", CommentChar = '#' };
            var csv = "A,B\n1,2\n# пауза\n3,4\n";

            var table = _logic.ParseToDataTable(csv, options, true, false);

            table.Rows.Count.Should().Be(2);
            table.Rows[1]["A"].Should().Be("3");
        }

        [Fact(DisplayName = "Чтение: comment-символ внутри кавычкованного поля — данные")]
        public void Parse_CommentChar_InsideQuotes_IsData()
        {
            var options = new CsvOptions { Delimiter = ",", CommentChar = '#' };
            var csv = "Note\n\"# не комментарий\"\n";

            var table = _logic.ParseToDataTable(csv, options, true, false);

            table.Rows.Count.Should().Be(1);
            table.Rows[0]["Note"].Should().Be("# не комментарий");
        }

        [Fact(DisplayName = "Чтение: comment-символ в середине записи — данные")]
        public void Parse_CommentChar_MidRecord_IsData()
        {
            var options = new CsvOptions { Delimiter = ",", CommentChar = '#' };
            var csv = "A,B\n1,#2\n";

            var table = _logic.ParseToDataTable(csv, options, true, false);

            table.Rows.Count.Should().Be(1);
            table.Rows[0]["B"].Should().Be("#2");
        }

        [Fact(DisplayName = "Дефолт: без comment-символа строки не пропускаются")]
        public void Default_NoCommentChar()
        {
            var options = CsvOptions.Parse(",", null, null, null);
            options.CommentChar.Should().BeNull();

            var table = _logic.ParseToDataTable("# A\n1,2\n", options, true, false);

            // '#' попадает в заголовок как обычные данные
            table.Columns[0].ColumnName.Should().Be("# A");
        }

        // ── Потоковое чтение ────────────────────────────────────────────

        [Fact(DisplayName = "Поток: результаты совпадают с обычным разбором")]
        public void Streaming_MatchesNonStreaming()
        {
            var csv = "Name,Note\nИван,\"Мск, Тверская\"\nМария,\"две\nстроки\"\n";
            var options = new CsvOptions { Delimiter = "," };

            var direct = _logic.ParseToDataTable(csv, options, true, false);

            DataTable streamed = null;
            using (var reader = new StringReader(csv))
                _logic.ReadRecords(reader, options, _ => { }); // прогрев API не нужен, но безвреден

            using (var reader = new StringReader(csv))
            {
                var table = new DataTable();
                var headerDone = false;
                _logic.ReadRecords(reader, options, record =>
                {
                    if (!headerDone)
                    {
                        for (int c = 0; c < record.Count; c++)
                            table.Columns.Add(record[c], typeof(string));
                        headerDone = true;
                        return;
                    }
                    var row = table.NewRow();
                    for (int c = 0; c < table.Columns.Count; c++)
                        row[c] = c < record.Count ? record[c] : string.Empty;
                    table.Rows.Add(row);
                });
                streamed = table;
            }

            streamed.Rows.Count.Should().Be(direct.Rows.Count);
            streamed.Rows[0]["Note"].Should().Be("Мск, Тверская");
            streamed.Rows[1]["Note"].Should().Be("две\nстроки");
        }

        [Fact(DisplayName = "Поток-файл: ParseFileStreaming == ParseFileToDataTable на 20 КБ с кавычками")]
        public void ParseFileStreaming_LargeFile_EqualsNonStreaming()
        {
            var path = TempPath("stream-large.csv");
            var sb = new StringBuilder();
            sb.Append("Id,Text,Value\n");
            for (int i = 0; i < 400; i++)
                sb.Append(i).Append(",\"текст с ; и \"\"кавычками\"\" и переводом\nвторой строки\",").Append(i * 2).Append("\n");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));

            var options = new CsvOptions { Delimiter = "," };
            var direct = _logic.ParseFileToDataTable(path, options, true, false, null);
            var streamed = _logic.ParseFileStreaming(path, options, true, null);

            streamed.Rows.Count.Should().Be(direct.Rows.Count);
            streamed.Columns.Count.Should().Be(direct.Columns.Count);
            streamed.Rows[399]["Text"].Should().Be(direct.Rows[399]["Text"]);
            ((string)streamed.Rows[399]["Text"]).Should().Contain("второй строки");
        }

        [Fact(DisplayName = "Поток-файл: учитывает опции диалекта и CRLF")]
        public void ParseFileStreaming_WithOptions_AndCrLf()
        {
            var path = TempPath("stream-dialect.csv");
            var content = "# выгрузка от 2026-11\r\nName;Note\r\n# секция данных\r\nИван;'строка\r\nвторая'\r\n";
            File.WriteAllText(path, content, Encoding.UTF8);

            var options = new CsvOptions { Delimiter = ";", QuoteChar = '\'', CommentChar = '#' };
            var table = _logic.ParseFileStreaming(path, options, true, null);

            table.Columns[0].ColumnName.Should().Be("Name");
            table.Rows.Count.Should().Be(1);
            table.Rows[0]["Note"].Should().Be("строка\r\nвторая");
        }

        [Fact(DisplayName = "Поток-файл: пустой файл → пустая таблица")]
        public void ParseFileStreaming_EmptyFile_EmptyTable()
        {
            var path = TempPath("stream-empty.csv");
            File.WriteAllText(path, string.Empty);

            var table = _logic.ParseFileStreaming(path, new CsvOptions(), true, null);

            table.Rows.Count.Should().Be(0);
            table.Columns.Count.Should().Be(0);
        }

        // ── Append-запись ───────────────────────────────────────────────

        private DataTable MakeTable(int rows)
        {
            var table = new DataTable();
            table.Columns.Add("A");
            table.Columns.Add("B");
            for (int i = 0; i < rows; i++)
                table.Rows.Add($"a{i}", $"b{i}");
            return table;
        }

        [Fact(DisplayName = "Append: заголовок только при создании файла, строки дописываются")]
        public void WriteCsvToFile_Append_HeaderOnlyOnCreate()
        {
            var path = TempPath("append.csv");

            _logic.WriteCsvToFile(MakeTable(2), path, new CsvOptions(), true, true, null, append: true);
            _logic.WriteCsvToFile(MakeTable(2), path, new CsvOptions(), true, true, null, append: true);

            var lines = File.ReadAllLines(path);
            // 1 заголовок + 4 строки данных
            lines.Length.Should().Be(5);
            lines[0].Should().Be("A,B");
            lines[1].Should().Be("a0,b0");
            lines[4].Should().Be("a1,b1");
        }

        [Fact(DisplayName = "Append: при существующем файле IncludeHeader игнорируется")]
        public void WriteCsvToFile_Append_ExistingFile_NoHeader()
        {
            var path = TempPath("append2.csv");
            File.WriteAllText(path, "A,B\nold1,old2\n", Encoding.UTF8);

            _logic.WriteCsvToFile(MakeTable(1), path, new CsvOptions(), true, true, null, append: true);

            var lines = File.ReadAllLines(path);
            lines.Length.Should().Be(3); // заголовок + старая строка + новая, без повторного заголовка
            lines[0].Should().Be("A,B");
            lines[1].Should().Be("old1,old2");
            lines[2].Should().Be("a0,b0");
        }

        [Fact(DisplayName = "Append=false (дефолт): файл перезаписывается как раньше")]
        public void WriteCsvToFile_Overwrite_Unchanged()
        {
            var path = TempPath("overwrite.csv");
            _logic.WriteCsvToFile(MakeTable(2), path, ",", true, true, null);
            _logic.WriteCsvToFile(MakeTable(1), path, new CsvOptions(), true, true, null, append: false);

            var lines = File.ReadAllLines(path);
            lines.Length.Should().Be(2); // заголовок + 1 строка — перезапись
        }

        [Fact(DisplayName = "Append: учитывает опции диалекта при дописывании")]
        public void WriteCsvToFile_Append_WithDialect()
        {
            var path = TempPath("append3.csv");
            var options = new CsvOptions { Delimiter = ";", QuoteChar = '\'' };

            _logic.WriteCsvToFile(MakeTable(1), path, options, true, true, null, append: true);
            _logic.WriteCsvToFile(MakeTable(1), path, options, true, true, null, append: true);

            var content = File.ReadAllText(path);
            content.Should().Contain("A;B");
            content.Should().NotContain(",");
        }

        // ── Обратная совместимость ──────────────────────────────────────

        [Fact(DisplayName = "Совместимость: старые сигнатуры работают как раньше")]
        public void Legacy_Signatures_Unchanged()
        {
            var table = _logic.ParseToDataTable("A,B\n1,2\n", ",", true, false);
            table.Rows[0]["A"].Should().Be("1");

            var csv = _logic.BuildCsv(table, ";", true, true);
            csv.Should().StartWith("A;B");

            var path = TempPath("legacy.csv");
            _logic.WriteCsvToFile(table, path, ",", true, true, null);
            File.ReadAllText(path).Should().Contain("1,2");

            var fromFile = _logic.ParseFileToDataTable(path, ",", true, false, null);
            fromFile.Rows.Count.Should().Be(1);
        }
    }
}
