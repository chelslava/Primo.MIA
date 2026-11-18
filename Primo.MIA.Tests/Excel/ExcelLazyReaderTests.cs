using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.Excel
{
    /// <summary>
    /// Тесты ленивого SAX-чтения .xlsx (ExcelLazyReader): заголовки, skip/max,
    /// общие строки, разреженные ячейки, пустые строки, именованный лист.
    /// </summary>
    [Trait("Category", "Fast")]
    public class ExcelLazyReaderTests : IDisposable
    {
        private readonly string _workDir = Path.Combine(
            Path.GetTempPath(), "mia-lazyexcel-tests-" + Guid.NewGuid().ToString("N"));

        public ExcelLazyReaderTests()
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

        private string CreateFile(string sheetName, Action<IXLWorksheet> fill)
        {
            var path = Path.Combine(_workDir, Guid.NewGuid().ToString("N") + ".xlsx");
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add(sheetName);
                fill(ws);
                wb.SaveAs(path);
            }
            return path;
        }

        private List<IReadOnlyList<string>> ReadAll(string path, string sheet = null, bool header = false, int skip = 0, int max = 0)
        {
            using (var reader = new ExcelLazyReader(path, sheet, header, skip, max))
                return reader.Select(r => r).ToList();
        }

        [Fact(DisplayName = "Чтение: строки по одной, значения соответствуют записанным")]
        public void Read_RoundTrip()
        {
            var path = CreateFile("Data", ws =>
            {
                ws.Cell(1, 1).Value = "Имя";
                ws.Cell(1, 2).Value = "Сумма";
                ws.Cell(2, 1).Value = "Иван";
                ws.Cell(2, 2).Value = 42;
                ws.Cell(3, 1).Value = "Мария";
                ws.Cell(3, 2).Value = 3.14;
            });

            var rows = ReadAll(path, "Data");

            rows.Count.Should().Be(3);
            rows[0][0].Should().Be("Имя");
            rows[1][0].Should().Be("Иван");
            // числа — сырой Invariant-текст из XML (точка-разделитель)
            rows[1][1].Should().Be("42");
            rows[2][1].Should().Be("3.14");
        }

        [Fact(DisplayName = "Заголовок: первая строка уходит в Columns, не в данные")]
        public void Header_ConsumedIntoColumns()
        {
            var path = CreateFile("Data", ws =>
            {
                ws.Cell(1, 1).Value = "Col1";
                ws.Cell(1, 2).Value = "Col2";
                ws.Cell(2, 1).Value = "a";
                ws.Cell(2, 2).Value = "b";
            });

            using (var reader = new ExcelLazyReader(path, null, hasHeader: true, skipRows: 0, maxRows: 0))
            {
                var rows = reader.Select(r => r).ToList();

                rows.Count.Should().Be(1, "строка заголовков не попадает в тело");
                rows[0][0].Should().Be("a");
                reader.Columns.Should().ContainInOrder("Col1", "Col2");
            }
        }

        [Fact(DisplayName = "Skip и Max: окно чтения")]
        public void Skip_Max_Window()
        {
            var path = CreateFile("Data", ws =>
            {
                for (int i = 1; i <= 6; i++)
                    ws.Cell(i, 1).Value = $"r{i}";
            });

            var rows = ReadAll(path, null, header: false, skip: 2, max: 3);

            rows.Count.Should().Be(3);
            rows[0][0].Should().Be("r3");
            rows[2][0].Should().Be("r5");
        }

        [Fact(DisplayName = "Разреженные ячейки: пропуски дополняются пустыми")]
        public void SparseCells_Padded()
        {
            var path = CreateFile("Data", ws =>
            {
                ws.Cell(1, 1).Value = "a";
                ws.Cell(1, 3).Value = "c"; // B пустая
            });

            var rows = ReadAll(path, null, header: false);

            rows[0].Count.Should().BeGreaterOrEqualTo(3);
            rows[0][0].Should().Be("a");
            rows[0][1].Should().Be(string.Empty);
            rows[0][2].Should().Be("c");
        }

        [Fact(DisplayName = "Пустые строки листа пропускаются")]
        public void EmptyRows_Skipped()
        {
            var path = CreateFile("Data", ws =>
            {
                ws.Cell(1, 1).Value = "first";
                ws.Cell(3, 1).Value = "third"; // строка 2 пустая
            });

            var rows = ReadAll(path, null, header: false);

            rows.Count.Should().Be(2);
            rows[1][0].Should().Be("third");
        }

        [Fact(DisplayName = "Именованный лист: чтение с указанного листа")]
        public void NamedSheet_Used()
        {
            var path = CreateFile("Data", ws =>
            {
                ws.Cell(1, 1).Value = "data-sheet";
            });
            using (var wb = new XLWorkbook(path))
            {
                wb.Worksheets.Add("Other").Cell(1, 1).Value = "other-sheet";
                wb.Save();
            }

            var rows = ReadAll(path, "Other");

            rows.Count.Should().Be(1);
            rows[0][0].Should().Be("other-sheet");
        }

        [Fact(DisplayName = "Несуществующий лист → InvalidOperationException")]
        public void MissingSheet_Throws()
        {
            var path = CreateFile("Data", ws => ws.Cell(1, 1).Value = "x");

            Action act = () => ReadAll(path, "НетТакого");

            act.Should().Throw<InvalidOperationException>().WithMessage("*НетТакого*");
        }

        [Fact(DisplayName = "Несуществующий файл → FileNotFoundException")]
        public void MissingFile_Throws()
        {
            Action act = () => ReadAll(Path.Combine(_workDir, "no-such.xlsx"));

            act.Should().Throw<FileNotFoundException>();
        }

        [Fact(DisplayName = "Большие данные: 50 000 строк читаются быстро и полностью")]
        public void LargeFile_Streams()
        {
            var path = CreateFile("Data", ws =>
            {
                for (int i = 1; i <= 50000; i++)
                {
                    ws.Cell(i, 1).Value = i;
                    ws.Cell(i, 2).Value = $"строка-{i}";
                }
            });

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var rows = ReadAll(path, null, header: false);
            sw.Stop();

            rows.Count.Should().Be(50000);
            rows[49999][1].Should().Be("строка-50000");
            // потоковое чтение не должно занимать минуты (ClosedXML-загрузка + DataTable были бы заметно тяжелее)
            sw.ElapsedMilliseconds.Should().BeLessThan(60000);
        }
    }
}
