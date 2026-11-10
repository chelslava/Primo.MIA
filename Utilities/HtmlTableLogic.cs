// =============================================================================
// HtmlTableLogic.cs — чистая логика HtmlTableToDataTable (issue #36).
//
// Парсинг HTML-таблиц в DataTable через HtmlAgilityPack:
// rowspan/colspan обрабатываются алгоритмом «занятых слотов» сетки,
// вложенные таблицы игнорируются (берутся только строки, чей ближайший
// <table> — выбранный), заголовки из <th> первой строки (опционально).
// =============================================================================

using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    /// <summary>
    /// Логика извлечения HTML-таблиц в DataTable.
    /// </summary>
    public class HtmlTableLogic
    {
        /// <summary>
        /// Извлекает N-ю таблицу документа (0-based) в DataTable.
        /// </summary>
        /// <param name="html">HTML-документ или фрагмент.</param>
        /// <param name="tableIndex">Индекс таблицы (0 = первая).</param>
        /// <param name="firstRowIsHeader">Первая строка содержит заголовки (&lt;th&gt; или любой текст).</param>
        public DataTable ToDataTable(string html, int tableIndex, bool firstRowIsHeader)
        {
            Guard.NotNullOrWhiteSpace(html, nameof(html));

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var tables = doc.DocumentNode.SelectNodes("//table");
            if (tables == null || tables.Count == 0)
                throw new InvalidOperationException("В документе не найдено ни одной HTML-таблицы (<table>)");

            if (tableIndex < 0 || tableIndex >= tables.Count)
                throw new InvalidOperationException(
                    $"Таблица с индексом {tableIndex} не найдена (в документе {tables.Count})");

            var table = tables[tableIndex];
            var rows = GetOwnRows(table);

            if (rows.Count == 0)
                throw new InvalidOperationException("В выбранной таблице нет строк (<tr>)");

            // ── Сетка с учётом rowspan/colspan ─────────────────────────
            var grid = ParseGrid(rows);

            var columnCount = grid.Max(r => r.Count);
            if (columnCount == 0)
                throw new InvalidOperationException("В таблице нет ячеек");

            var result = new DataTable($"HtmlTable{tableIndex + 1}");

            // ── Колонки ────────────────────────────────────────────────
            var dataStartRow = 0;
            if (firstRowIsHeader)
            {
                var header = grid[0];
                var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int c = 0; c < columnCount; c++)
                {
                    var name = c < header.Count ? header[c] : string.Empty;
                    name = name.Trim();
                    if (string.IsNullOrEmpty(name))
                        name = $"Column{c + 1}";
                    var baseName = name;
                    var n = 2;
                    while (!used.Add(name))
                        name = baseName + n++;
                    result.Columns.Add(name, typeof(string));
                }
                dataStartRow = 1;
            }
            else
            {
                for (int c = 0; c < columnCount; c++)
                    result.Columns.Add($"Column{c + 1}", typeof(string));
            }

            // ── Строки ─────────────────────────────────────────────────
            for (int r = dataStartRow; r < grid.Count; r++)
            {
                var row = result.NewRow();
                for (int c = 0; c < columnCount; c++)
                    row[c] = c < grid[r].Count ? grid[r][c] : string.Empty;
                result.Rows.Add(row);
            }

            return result;
        }

        /// <summary>
        /// Строки, принадлежащие именно этой таблице: ближайший родительский
        /// &lt;table&gt; — сама таблица (строки вложенных таблиц отсекаются).
        /// </summary>
        private static List<HtmlAgilityPack.HtmlNode> GetOwnRows(HtmlAgilityPack.HtmlNode table)
        {
            var rows = table.SelectNodes(".//tr");
            if (rows == null)
                return new List<HtmlAgilityPack.HtmlNode>();

            return rows
                .Where(r => r.Ancestors("table").FirstOrDefault() == table)
                .ToList();
        }

        /// <summary>
        /// Разбирает строки в сетку значений с поддержкой rowspan/colspan:
        /// ячейка с rowspan занимает слоты последующих строк, с colspan —
        /// соседние колонки той же строки.
        /// </summary>
        private static List<List<string>> ParseGrid(List<HtmlAgilityPack.HtmlNode> rows)
        {
            var grid = new List<List<string>>();
            // занятые слоты: (строка, колонка) → значение (копия rowspan-ячейки)
            var occupied = new Dictionary<string, string>();

            for (int r = 0; r < rows.Count; r++)
            {
                var rowValues = new List<string>();
                int col = 0;

                foreach (var cell in rows[r].SelectNodes("./th|./td") ?? Enumerable.Empty<HtmlAgilityPack.HtmlNode>())
                {
                    // слоты, занятые rowspan-ячейками из предыдущих строк
                    while (occupied.ContainsKey(Key(r, col)))
                    {
                        rowValues.Add(occupied[Key(r, col)]);
                        occupied.Remove(Key(r, col));
                        col++;
                    }

                    var value = CleanText(cell.InnerText);
                    var colspan = GetSpan(cell, "colspan", 1);
                    var rowspan = GetSpan(cell, "rowspan", 1);

                    rowValues.Add(value);
                    for (int cc = 1; cc < colspan; cc++)
                        rowValues.Add(value); // colspan: та же строка, соседние колонки

                    // rowspan: будущие строки, ВСЕ колонки ячейки (включая colspan)
                    for (int rr = 1; rr < rowspan; rr++)
                        for (int cc = 0; cc < colspan; cc++)
                            occupied[Key(r + rr, col + cc)] = value;

                    col += colspan;
                }

                // хвостовые rowspan-слоты после последней ячейки строки
                while (occupied.ContainsKey(Key(r, col)))
                {
                    rowValues.Add(occupied[Key(r, col)]);
                    occupied.Remove(Key(r, col));
                    col++;
                }

                grid.Add(rowValues);
            }

            return grid;
        }

        private static string Key(int row, int col) => row.ToString(CultureInfo.InvariantCulture) + ":" + col.ToString(CultureInfo.InvariantCulture);

        private static int GetSpan(HtmlAgilityPack.HtmlNode cell, string attribute, int fallback)
        {
            var raw = cell.GetAttributeValue(attribute, null);
            if (int.TryParse(raw, out var value) && value > 0)
                return value;
            return fallback;
        }

        private static string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            // декодирование HTML-сущностей (&nbsp;, &amp;, &lt;...), затем
            // неразрывные пробелы → обычные, схлопывание пробельных последовательностей
            var decoded = System.Net.WebUtility.HtmlDecode(text);
            var normalized = decoded.Replace('\u00A0', ' ');
            return Regex.Replace(normalized, @"\s+", " ").Trim();
        }
    }
}
