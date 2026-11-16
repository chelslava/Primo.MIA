// =============================================================================
// ExcelLogic.cs — чистая логика Excel-активностей (issue #47).
// ClosedXML без Interop: .xlsx читается/пишется без установленного Excel.
//
// Правила:
//   • чтение — типизированные значения (число → double, дата → DateTime,
//     логическое → bool, остальное → string) — сценарий получает «как в Excel»;
//   • запись — строки DataTable с типами; DBNull → пустая ячейка;
//     текст пишется SetText (значение «=…» не интерпретируется формулой);
//   • Append — дописывание после последней занятой строки листа;
//   • дубликаты имён колонок обезличиваются («Количество», «Количество2»).
// =============================================================================

using ClosedXML.Excel;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>Чистая логика чтения/записи диапазонов .xlsx (без Interop).</summary>
    public static class ExcelLogic
    {
        /// <summary>Имя листа по умолчанию для нового файла.</summary>
        public const string DefaultSheetName = "Sheet1";

        // =====================================================================
        // Чтение
        // =====================================================================

        /// <summary>
        /// Читает диапазон .xlsx в DataTable.
        /// Внимание: без явного <paramref name="range"/> читается используемый
        /// диапазон листа — пустые ведущие колонки/строки обрезаются ClosedXML;
        /// для стабильных позиций колонок при пустых данных задавайте диапазон явно.
        /// </summary>
        /// <param name="filePath">Путь к .xlsx.</param>
        /// <param name="sheetName">Имя листа; пусто — первый лист книги.</param>
        /// <param name="range">Диапазон «A1:F100»; пусто — используемый диапазон листа.</param>
        /// <param name="headerRow">Первая строка диапазона — имена колонок.</param>
        /// <returns>DataTable: типизированные значения, имена колонок из заголовков.</returns>
        public static DataTable ReadRange(string filePath, string sheetName, string range, bool headerRow)
        {
            Guard.NotNullOrWhiteSpace(filePath, nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл Excel не найден: {filePath}", filePath);

            using var workbook = new XLWorkbook(filePath);
            IXLWorksheet worksheet = ResolveWorksheet(workbook, sheetName);

            var table = new DataTable($"{worksheet.Name}");

            IXLRange xlRange;
            if (string.IsNullOrWhiteSpace(range))
            {
                xlRange = worksheet.RangeUsed();
                if (xlRange == null)
                {
                    // Лист без данных — валидный результат для RPA-сценария:
                    // пустой DataTable (RowCount=0) вместо ошибки
                    return table;
                }
            }
            else
            {
                try
                {
                    xlRange = worksheet.Range(range.Trim());
                }
                catch (Exception ex)
                {
                    throw new FormatException(
                        $"{ActivityStrings.Error_ExcelRangeInvalid}: «{range.Trim()}» ({ex.Message})", ex);
                }
            }

            int columnCount = xlRange.ColumnCount();

            // ── Колонки: из первой строки или обезличенные «КолонкаN» ────
            var headerRowCells = headerRow ? xlRange.FirstRow().Cells() : null;
            var names = new List<string>(columnCount);
            for (int i = 1; i <= columnCount; i++)
            {
                string name = null;
                if (headerRow)
                {
                    var value = headerRowCells.ElementAt(i - 1).Value;
                    name = value.Type == XLDataType.Text ? value.GetText()?.Trim() : value.ToString();
                }

                if (string.IsNullOrEmpty(name))
                    name = "Колонка" + i;

                // DataTable не допускает дубликатов имён колонок.
                // typeof(object) ОБЯЗАТЕЛЕН: колонка без типа по умолчанию
                // строковая — все значения конвертировались бы в String.
                string unique = name;
                int suffix = 2;
                while (table.Columns.Contains(unique))
                    unique = name + suffix++;

                table.Columns.Add(unique, typeof(object));
            }

            // ── Данные (первая строка пропускается при headerRow) ─────────
            var dataRows = headerRow ? xlRange.Rows().Skip(1) : xlRange.Rows();
            foreach (var row in dataRows)
            {
                var cells = row.Cells();
                var values = new object[columnCount];
                for (int i = 1; i <= columnCount; i++)
                    values[i - 1] = GetCellValue(cells.ElementAt(i - 1));
                table.Rows.Add(values);
            }

            return table;
        }

        /// <summary>
        /// Возвращает имена листов книги (в порядке следования).
        /// </summary>
        public static IReadOnlyList<string> GetSheetNames(string filePath)
        {
            Guard.NotNullOrWhiteSpace(filePath, nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл Excel не найден: {filePath}", filePath);

            using var workbook = new XLWorkbook(filePath);
            return workbook.Worksheets.Select(w => w.Name).ToList();
        }

        // =====================================================================
        // Запись
        // =====================================================================

        /// <summary>
        /// Записывает DataTable в .xlsx: перезапись от начальной ячейки или
        /// дописывание после последней занятой строки.
        /// </summary>
        /// <param name="filePath">Путь к .xlsx (создаётся при отсутствии, если createIfMissing).</param>
        /// <param name="sheetName">Имя листа; пусто — первый лист (для нового файла — DefaultSheetName).</param>
        /// <param name="startCell">Начальная ячейка («A1») — только для Overwrite.</param>
        /// <param name="mode">Перезапись / дописывание.</param>
        /// <param name="createIfMissing">Создать файл при отсутствии.</param>
        /// <param name="writeHeaders">Записать имена колонок первой строкой.</param>
        /// <param name="table">Исходная таблица.</param>
        /// <returns>Число записанных строк данных (без строки заголовков).</returns>
        public static int WriteRange(
            string filePath, string sheetName, string startCell,
            ExcelWriteMode mode, bool createIfMissing, bool writeHeaders, DataTable table)
        {
            Guard.NotNullOrWhiteSpace(filePath, nameof(filePath));
            Guard.NotNull(table, nameof(table));

            bool fileExists = File.Exists(filePath);
            if (!fileExists && !createIfMissing)
                throw new FileNotFoundException($"Файл Excel не найден: {filePath}", filePath);

            using (var workbook = fileExists ? new XLWorkbook(filePath) : new XLWorkbook())
            {
                IXLWorksheet worksheet = fileExists
                    ? ResolveOrCreateWorksheet(workbook, sheetName)
                    : workbook.AddWorksheet(string.IsNullOrWhiteSpace(sheetName) ? DefaultSheetName : sheetName.Trim());

                int startRow;
                if (mode == ExcelWriteMode.Append)
                {
                    startRow = (worksheet.LastRowUsed()?.RowNumber() ?? 0) + 1;
                }
                else
                {
                    try
                    {
                        startRow = worksheet.Cell(string.IsNullOrWhiteSpace(startCell) ? "A1" : startCell.Trim()).Address.RowNumber;
                    }
                    catch (Exception ex)
                    {
                        throw new FormatException(
                            $"{ActivityStrings.Error_ExcelStartCellInvalid}: «{startCell?.Trim()}» ({ex.Message})", ex);
                    }
                }

                int rowNumber = startRow;

                if (writeHeaders && table.Columns.Count > 0)
                {
                    for (int c = 0; c < table.Columns.Count; c++)
                        worksheet.Cell(rowNumber, c + 1).SetValue(table.Columns[c].ColumnName);
                    rowNumber++;
                }

                foreach (DataRow row in table.Rows)
                {
                    for (int c = 0; c < table.Columns.Count; c++)
                        SetCellValue(worksheet.Cell(rowNumber, c + 1), row[c]);
                    rowNumber++;
                }

                if (fileExists)
                    workbook.Save();
                else
                    workbook.SaveAs(filePath);
            }

            return table.Rows.Count;
        }

        // =====================================================================
        // Внутреннее
        // =====================================================================

        private static IXLWorksheet ResolveWorksheet(XLWorkbook workbook, string sheetName)
        {
            if (string.IsNullOrWhiteSpace(sheetName))
                return workbook.Worksheet(1);

            if (!workbook.TryGetWorksheet(sheetName.Trim(), out var worksheet))
                throw new InvalidOperationException(
                    $"Лист «{sheetName.Trim()}» не найден. Доступны: {string.Join(", ", workbook.Worksheets.Select(w => w.Name))}");
            return worksheet;
        }

        private static IXLWorksheet ResolveOrCreateWorksheet(XLWorkbook workbook, string sheetName)
        {
            if (string.IsNullOrWhiteSpace(sheetName))
                return workbook.Worksheet(1);

            sheetName = sheetName.Trim();
            if (workbook.TryGetWorksheet(sheetName, out var worksheet))
                return worksheet;

            return workbook.AddWorksheet(sheetName);
        }

        /// <summary>Типизированное значение ячейки для DataTable.</summary>
        private static object GetCellValue(IXLCell cell)
        {
            return cell.Value.Type switch
            {
                XLDataType.Boolean  => cell.Value.GetBoolean(),
                XLDataType.DateTime => cell.Value.GetDateTime(),
                XLDataType.TimeSpan => cell.Value.GetTimeSpan(),
                XLDataType.Number   => cell.Value.GetNumber(),
                XLDataType.Blank    => string.Empty,
                XLDataType.Error    => cell.Value.GetError().ToString(),
                _                   => cell.Value.GetText()
            };
        }

        /// <summary>Запись значения DataRow в ячейку (DBNull → пустая строка:
        /// ячейка помечается использованной — строки из одних пустых значений
        /// не «исчезают» из используемого диапазона листа).</summary>
        private static void SetCellValue(IXLCell cell, object value)
        {
            if (value == null || value == DBNull.Value)
            {
                cell.SetValue(string.Empty);
                return;
            }

            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.Boolean:  cell.SetValue(Convert.ToBoolean(value)); break;
                case TypeCode.DateTime: cell.SetValue(Convert.ToDateTime(value)); break;
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    cell.SetValue(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                    break;
                default:
                    // SetText: строка «=…» не становится формулой
                    cell.SetValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
            }
        }
    }
}
