// =============================================================================
// ExcelLazyReader.cs — потоковое (ленивое) чтение строк .xlsx через
// OpenXML SAX (issue: быстрые Excel-циклы).
//
// В отличие от ReadRange (ClosedXML грузит книгу целиком + строит
// DataTable), здесь строки читаются из XML листа по одной: память O(строка),
// старт мгновенный даже для файлов на сотни тысяч строк.
//
// Особенности SAX-чтения:
//   - значения: SharedStrings — по таблице общих строк (читается один раз,
//     лениво при первой строковой ячейке); числа — какInvariant-строка;
//   - даты хранятся в Excel как числа с датовым стилем: без парсинга
//     стилей возвращается серийное число (документированное ограничение
//     ленивого режима; полная типизация — через ExcelReadRange);
//   - пустые хвостовые ячейки строки дополняются пустыми строками до
//     ширины первой непустой строки листа.
// =============================================================================

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Primo.MIA
{
    /// <summary>
    /// Ленивое построчное чтение .xlsx (OpenXML SAX). Реализует IEnumerable —
    /// foreach потребляет строки по одной; параллельное чтение одного
    /// экземпляра не поддерживается (одиночный проход).
    /// </summary>
    public sealed class ExcelLazyReader : IEnumerable<IReadOnlyList<string>>, IDisposable
    {
        private readonly string _filePath;
        private readonly string _sheetName;
        private readonly bool _hasHeader;
        private readonly int _skipRows;
        private readonly int _maxRows;

        private SpreadsheetDocument _doc;
        private SharedStringItem[] _sharedStrings;

        /// <summary>Имена колонок из строки заголовков (пусто до первой итерации при hasHeader).</summary>
        public IReadOnlyList<string> Columns { get; private set; }

        public ExcelLazyReader(string filePath, string sheetName, bool hasHeader, int skipRows, int maxRows)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу Excel обязателен", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл Excel не найден: {filePath}", filePath);

            _filePath = filePath;
            _sheetName = sheetName;
            _hasHeader = hasHeader;
            _skipRows = Math.Max(0, skipRows);
            _maxRows = Math.Max(0, maxRows);
        }

        public IEnumerator<IReadOnlyList<string>> GetEnumerator()
        {
            _doc = SpreadsheetDocument.Open(_filePath, isEditable: false);
            try
            {
                var workbookPart = _doc.WorkbookPart
                    ?? throw new InvalidOperationException("Файл не содержит книгу (WorkbookPart отсутствует)");

                var sheet = FindSheet(workbookPart, _sheetName)
                    ?? throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(_sheetName)
                            ? "Лист не найден"
                            : $"Лист «{_sheetName}» не найден");

                var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);

                // таблица общих строк — лениво при первом обращении
                SharedStringTable shared = null;
                var sstPart = workbookPart.SharedStringTablePart;

                int width = 0;
                int yielded = 0;
                int skipped = 0;
                bool headerPending = _hasHeader;

                using (var reader = OpenXmlReader.Create(worksheetPart))
                {
                    while (reader.Read())
                    {
                        if (reader.ElementType != typeof(Row) || !reader.IsStartElement)
                            continue;

                        var row = (Row)reader.LoadCurrentElement();
                        var cells = ReadRowCells(row, sstPart, ref shared);

                        if (cells.Count > width)
                            width = cells.Count;

                        // пустые строки листа пропускаем бесшумно
                        if (IsEmptyRow(cells))
                            continue;

                        if (skipped < _skipRows)
                        {
                            skipped++;
                            continue;
                        }

                        // заголовок: первая строка данных задаёт имена колонок
                        if (headerPending)
                        {
                            Columns = new List<string>(cells);
                            headerPending = false;
                            continue;
                        }

                        yield return NormalizeWidth(cells, width);
                        yielded++;

                        if (_maxRows > 0 && yielded >= _maxRows)
                            yield break;
                    }
                }
            }
            finally
            {
                Dispose();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => GetEnumerator();

        private static Sheet FindSheet(WorkbookPart workbookPart, string sheetName)
        {
            foreach (var s in workbookPart.Workbook.Sheets.Elements<Sheet>())
            {
                if (string.IsNullOrWhiteSpace(sheetName)
                    || string.Equals(s.Name?.Value?.Trim(), sheetName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return s;
            }
            return null;
        }

        /// <summary>Ячейки строки в порядке следования (пропуская разрывы — пустые вставки).</summary>
        private List<string> ReadRowCells(Row row, SharedStringTablePart sstPart, ref SharedStringTable shared)
        {
            var values = new List<string>();
            uint expected = 1;

            foreach (var cell in row.Elements<Cell>())
            {
                // разреженные строки: пропущенные индексы — пустые ячейки
                var index = GetColumnIndex(cell.CellReference?.Value);
                if (index == 0)
                    index = expected; // без ссылки — текущая позиция
                while (expected < index)
                {
                    values.Add(string.Empty);
                    expected++;
                }
                expected = index + 1;

                values.Add(ReadCellValue(cell, sstPart, ref shared));
            }
            return values;
        }

        private string ReadCellValue(Cell cell, SharedStringTablePart sstPart, ref SharedStringTable shared)
        {
            var type = cell.DataType?.Value;
            if (type == CellValues.SharedString)
            {
                if (shared == null)
                {
                    shared = sstPart?.SharedStringTable;
                    if (shared == null)
                        return string.Empty;
                    // материализуем для O(1) доступа по индексу
                    _sharedStrings = shared.Elements<SharedStringItem>().ToArray();
                }

                if (int.TryParse(cell.CellValue?.InnerText, out var idx)
                    && idx >= 0 && idx < _sharedStrings.Length)
                    return _sharedStrings[idx].InnerText ?? string.Empty;
                return string.Empty;
            }

            // числа/даты/прочее: сырой текст; формула — кэшированное значение
            return cell.CellValue?.InnerText ?? string.Empty;
        }

        private static bool IsEmptyRow(IReadOnlyList<string> cells)
        {
            foreach (var v in cells)
                if (!string.IsNullOrEmpty(v))
                    return false;
            return true;
        }

        private static List<string> NormalizeWidth(List<string> cells, int width)
        {
            if (cells.Count >= width)
                return cells;
            var padded = new List<string>(width);
            padded.AddRange(cells);
            while (padded.Count < width)
                padded.Add(string.Empty);
            return padded;
        }

        /// <summary>Индекс колонки из ссылки A1 (например C5 → 3, 1-based); без ссылки — 0.</summary>
        private static uint GetColumnIndex(string cellRef)
        {
            if (string.IsNullOrEmpty(cellRef))
                return 0;
            uint col = 0;
            foreach (var ch in cellRef)
            {
                if (ch >= 'A' && ch <= 'Z')
                    col = col * 26 + (uint)(ch - 'A' + 1);
                else if (ch >= 'a' && ch <= 'z')
                    col = col * 26 + (uint)(ch - 'a' + 1);
                else
                    break; // цифры — конец буквенной части
            }
            return col; // 1-based
        }

        public void Dispose()
        {
            _doc?.Dispose();
            _doc = null;
        }
    }
}
