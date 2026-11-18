// =============================================================================
// ExcelLazyReader.cs — потоковое (ленивое) чтение строк .xlsx на чистом BCL
// (ZipArchive + XmlReader) — БЕЗ DocumentFormat.OpenXml.
//
// Почему BCL: у конечных установок Studio может не оказаться нужной версии
// DocumentFormat.OpenXml (конфликт версий) → FileNotFoundException при
// запуске цикла. .xlsx — это zip + xml: чтение листа потоково через
// XmlReader даёт тот же SAX-эффект (память O(строки)) без внешних сборок.
//
// Формат: xl/workbook.xml (лист → r:id), xl/_rels/workbook.xml.rels
// (r:id → файл листа), xl/sharedStrings.xml (общие строки, лениво при
// первом строковом значении), xl/worksheets/sheetN.xml (строки/ячейки).
//
// Ограничения ленивого режима: даты хранятся в Excel как числа с датовым
// стилем — без парсинга стилей возвращается серийное число (полная
// типизация — через ExcelReadRange); формулы — кэшированное значение.
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Primo.MIA
{
    /// <summary>
    /// Ленивое построчное чтение .xlsx (Zip+XmlReader, одиночный проход).
    /// </summary>
    public sealed class ExcelLazyReader : IEnumerable<IReadOnlyList<string>>, IDisposable
    {
        private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        private readonly string _filePath;
        private readonly string _sheetName;
        private readonly bool _hasHeader;
        private readonly int _skipRows;
        private readonly int _maxRows;

        /// <summary>Имена колонок из строки заголовков (заполняется при чтении первой записи).</summary>
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
            using var zip = ZipFile.OpenRead(_filePath);
            var sheetPath = ResolveSheetPath(zip, _sheetName);
            var sharedStrings = LoadSharedStrings(zip); // пустой массив — если таблицы нет

            int width = 0;
            int yielded = 0;
            int skipped = 0;
            bool headerPending = _hasHeader;

            var entry = zip.GetEntry(sheetPath)
                ?? throw new InvalidOperationException($"Лист не найден в файле ({sheetPath})");
            using var sheetStream = entry.Open();
            using var reader = XmlReader.Create(sheetStream);

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "row")
                    continue;

                var cells = ReadRowCells(reader, sharedStrings);

                if (cells.Count > width)
                    width = cells.Count;

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

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => GetEnumerator();

        /// <summary>Путь в zip к XML листа: workbook.xml → r:id → workbook.xml.rels → Target.</summary>
        private static string ResolveSheetPath(ZipArchive zip, string sheetName)
        {
            string rid = null;
            var workbookEntry = zip.GetEntry("xl/workbook.xml")
                ?? throw new InvalidOperationException("Файл не содержит книгу (xl/workbook.xml отсутствует)");
            using (var s = workbookEntry.Open())
            using (var r = XmlReader.Create(s))
            {
                while (r.Read())
                {
                    if (r.NodeType == XmlNodeType.Element && r.LocalName == "sheet")
                    {
                        var name = r.GetAttribute("name");
                        if (string.IsNullOrWhiteSpace(sheetName)
                            || string.Equals(name?.Trim(), sheetName.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            rid = r.GetAttribute("id", RelNs);
                            break;
                        }
                    }
                }
            }

            if (rid == null)
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(sheetName)
                        ? "Лист не найден"
                        : $"Лист «{sheetName}» не найден");

            var relsEntry = zip.GetEntry("xl/_rels/workbook.xml.rels")
                ?? throw new InvalidOperationException("Не найден xl/_rels/workbook.xml.rels");
            using (var s = relsEntry.Open())
            using (var r = XmlReader.Create(s))
            {
                while (r.Read())
                {
                    if (r.NodeType == XmlNodeType.Element && r.LocalName == "Relationship"
                        && string.Equals(r.GetAttribute("Id"), rid, StringComparison.Ordinal))
                    {
                        var target = r.GetAttribute("Target");
                        if (string.IsNullOrWhiteSpace(target))
                            break;
                        // Target относительный: "worksheets/sheet1.xml" → "xl/..."; абсолютный: "/xl/..."
                        target = target.TrimStart('/');
                        return target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)
                            ? target
                            : "xl/" + target;
                    }
                }
            }
            throw new InvalidOperationException($"Не найдена цель отношения {rid} в workbook.xml.rels");
        }

        /// <summary>
        /// Общие строки: индекс → текст. Лениво при первом проходе (таблица
        /// может занимать мегабайты — читается один раз, только если есть).
        /// </summary>
        private static string[] LoadSharedStrings(ZipArchive zip)
        {
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return Array.Empty<string>();

            var list = new List<string>();
            using (var s = entry.Open())
            using (var r = XmlReader.Create(s))
            {
                string current = null;
                while (r.Read())
                {
                    if (r.NodeType == XmlNodeType.Element && r.LocalName == "si")
                    {
                        current = string.Empty;
                    }
                    else if (r.NodeType == XmlNodeType.EndElement && r.LocalName == "si")
                    {
                        list.Add(current ?? string.Empty);
                        current = null;
                    }
                    else if (current != null && r.NodeType == XmlNodeType.Text)
                    {
                        current += r.Value;
                    }
                }
            }
            return list.ToArray();
        }

        /// <summary>Ячейки строки в порядке колонок (пропуски — пустые вставки).</summary>
        private List<string> ReadRowCells(XmlReader rowReader, string[] sharedStrings)
        {
            var values = new List<string>();
            uint expected = 1;

            using var row = rowReader.ReadSubtree();
            while (row.Read())
            {
                if (row.NodeType != XmlNodeType.Element || row.LocalName != "c")
                    continue;

                var cellRef = row.GetAttribute("r");
                var cellType = row.GetAttribute("t");
                var index = GetColumnIndex(cellRef);
                if (index == 0)
                    index = expected; // без ссылки — текущая позиция

                while (expected < index)
                {
                    values.Add(string.Empty);
                    expected++;
                }
                expected = index + 1;

                values.Add(ReadCellValue(row, cellType, sharedStrings));
            }
            return values;
        }

        private string ReadCellValue(XmlReader row, string cellType, string[] sharedStrings)
        {
            // <v> — значение; для inlineStr текст в <is><t>…</t></is>
            string value = null;
            string inline = null;
            using var cell = row.ReadSubtree();
            while (cell.Read())
            {
                if (cell.NodeType != XmlNodeType.Element)
                    continue;
                if (cell.LocalName == "v")
                {
                    value = cell.ReadElementContentAsString();
                    break;
                }
                if (cell.LocalName == "is")
                {
                    using var isReader = cell.ReadSubtree();
                    while (isReader.Read())
                        if (isReader.NodeType == XmlNodeType.Text)
                            inline += isReader.Value;
                    break;
                }
            }

            if (cellType == "inlineStr")
                return inline ?? string.Empty;
            if (cellType == "s")
            {
                // индекс таблицы общих строк
                if (int.TryParse(value, out var idx) && idx >= 0 && idx < sharedStrings.Length)
                    return sharedStrings[idx];
                return string.Empty;
            }
            return value ?? string.Empty;
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
            // все ресурсы освобождаются в GetEnumerator (using по zip/stream)
        }
    }
}
