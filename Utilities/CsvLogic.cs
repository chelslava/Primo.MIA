// =============================================================================
// CsvLogic.cs — чистая логика CSV ↔ DataTable (issue #29).
//
// RFC 4180: кавычки экранируются удвоением (""), кавычкованное поле может
// содержать разделители и переводы строк; разделитель настраиваемый
// (по умолчанию ','), переводы строк CRLF/LF/CR принимаются на чтении.
// Опциональная типизация колонок (int → double → bool → DateTime → string)
// по образцу JsonDataTable (Prop_TypeInference).
// =============================================================================

using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Primo.MIA
{
    /// <summary>
    /// Парсер и райтер CSV по RFC 4180 без внешних зависимостей.
    /// </summary>
    public class CsvLogic
    {
        private const string DefaultDelimiter = ",";
        private const string DefaultTableName = "CsvData";

        // ── Чтение: CSV → DataTable ────────────────────────────────────

        /// <summary>
        /// Разбирает CSV-текст в DataTable.
        /// </summary>
        /// <param name="csv">Текст CSV.</param>
        /// <param name="delimiter">Разделитель полей (по умолчанию ',').</param>
        /// <param name="hasHeaderRow">Первая строка — заголовки колонок.</param>
        /// <param name="typeInference">Выводить типы колонок (int/double/bool/DateTime), иначе все string.</param>
        public DataTable ParseToDataTable(
            string csv,
            string delimiter,
            bool hasHeaderRow,
            bool typeInference)
        {
            Guard.NotNull(csv, nameof(csv));

            var delim = ResolveDelimiter(delimiter);
            var rows = SplitRecords(csv, delim);

            var table = new DataTable(DefaultTableName);
            if (rows.Count == 0)
                return table;

            var headerFields = hasHeaderRow
                ? rows[0]
                : null;

            // Число колонок задаёт заголовок (лишние поля данных игнорируются);
            // без заголовка — максимум полей по записям
            var columnCount = hasHeaderRow
                ? rows[0].Count
                : rows.Max(r => r.Count);

            // Колонки
            for (int c = 0; c < columnCount; c++)
            {
                var name = headerFields != null && c < headerFields.Count
                    && !string.IsNullOrWhiteSpace(headerFields[c])
                    ? headerFields[c].Trim()
                    : $"Column{c + 1}";
                table.Columns.Add(name, typeof(string));
            }

            // Строки данных (включая «лишние» заголовочные, если hasHeaderRow=false)
            var dataRows = hasHeaderRow ? rows.Skip(1) : rows;
            foreach (var fields in dataRows)
            {
                var row = table.NewRow();
                for (int c = 0; c < table.Columns.Count; c++)
                    row[c] = c < fields.Count ? fields[c] : string.Empty;
                table.Rows.Add(row);
            }

            if (typeInference)
                ApplyTypeInference(table);

            return table;
        }

        /// <summary>
        /// Читает CSV из файла с автоопределением BOM и настраиваемой кодировкой.
        /// </summary>
        public DataTable ParseFileToDataTable(
            string filePath,
            string delimiter,
            bool hasHeaderRow,
            bool typeInference,
            string encodingName)
        {
            Guard.NotNullOrWhiteSpace(filePath, nameof(filePath));

            var encoding = ResolveEncoding(encodingName);
            string csv;
            using (var reader = new StreamReader(filePath, encoding, detectEncodingFromByteOrderMarks: true))
                csv = reader.ReadToEnd();

            return ParseToDataTable(csv, delimiter, hasHeaderRow, typeInference);
        }

        // ── Запись: DataTable → CSV ────────────────────────────────────

        /// <summary>
        /// Собирает CSV-текст из DataTable по RFC 4180: кавычки ставятся,
        /// только если поле содержит разделитель/кавычку/перевод строки.
        /// </summary>
        public string BuildCsv(
            DataTable table,
            string delimiter,
            bool includeHeader,
            bool convertDBNullToEmpty)
        {
            Guard.NotNull(table, nameof(table));

            var delim = ResolveDelimiter(delimiter);
            var sb = new StringBuilder();

            if (includeHeader)
            {
                var headers = table.Columns.Cast<DataColumn>().Select(c => QuoteField(c.ColumnName, delim));
                sb.AppendLine(string.Join(delim, headers));
            }

            foreach (DataRow row in table.Rows)
            {
                var fields = new List<string>(table.Columns.Count);
                foreach (var column in table.Columns.Cast<DataColumn>())
                {
                    var value = row[column];
                    var text = convertDBNullToEmpty && (value == null || value == DBNull.Value)
                        ? string.Empty
                        : value?.ToString() ?? string.Empty;
                    fields.Add(QuoteField(text, delim));
                }
                sb.AppendLine(string.Join(delim, fields));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Записывает CSV в файл (UTF-8 без BOM или кодировка по имени).
        /// </summary>
        public void WriteCsvToFile(
            DataTable table,
            string filePath,
            string delimiter,
            bool includeHeader,
            bool convertDBNullToEmpty,
            string encodingName)
        {
            Guard.NotNullOrWhiteSpace(filePath, nameof(filePath));

            var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var csv = BuildCsv(table, delimiter, includeHeader, convertDBNullToEmpty);
            File.WriteAllText(filePath, csv, ResolveEncoding(encodingName, bomForUtf8: false));
        }

        // ── RFC 4180: разбор записей ───────────────────────────────────

        /// <summary>
        /// Разбивает CSV на записи (список полей). Состоянийная машина:
        /// в кавычках разделители и переводы строк — часть данных, "" — кавычка.
        /// </summary>
        private static List<List<string>> SplitRecords(string csv, string delimiter)
        {
            var records = new List<List<string>>();
            var fields = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            var fieldStarted = false; // поле начато (для различения пустого поля и конца)

            int i = 0;
            int delimLen = delimiter.Length;

            void EndField()
            {
                fields.Add(field.ToString());
                field.Clear();
                fieldStarted = false;
            }

            void EndRecord()
            {
                EndField();
                records.Add(fields);
                fields = new List<string>();
            }

            while (i < csv.Length)
            {
                var ch = csv[i];

                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        // "" внутри кавычек → буквальная кавычка
                        if (i + 1 < csv.Length && csv[i + 1] == '"')
                        {
                            field.Append('"');
                            i += 2;
                            continue;
                        }
                        inQuotes = false;
                        i++;
                        continue;
                    }
                    field.Append(ch);
                    i++;
                    continue;
                }

                if (ch == '"' && field.Length == 0 && !fieldStarted)
                {
                    // Кавычка в начале поля → кавычкованный режим
                    inQuotes = true;
                    fieldStarted = true;
                    i++;
                    continue;
                }

                if (delimLen > 0 && MatchDelimiter(csv, i, delimiter))
                {
                    EndField();
                    i += delimLen;
                    continue;
                }

                if (ch == '\r' || ch == '\n')
                {
                    EndRecord();
                    // CRLF / LF / CR — одна граница записи
                    if (ch == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                        i += 2;
                    else
                        i++;
                    continue;
                }

                field.Append(ch);
                fieldStarted = true;
                i++;
            }

            // Последняя запись (если файл не заканчивается переводом строки
            // или заканчивается полем без переноса)
            if (field.Length > 0 || fieldStarted || fields.Count > 0)
                EndRecord();

            return records;
        }

        private static bool MatchDelimiter(string csv, int position, string delimiter)
        {
            if (position + delimiter.Length > csv.Length)
                return false;
            return string.CompareOrdinal(csv, position, delimiter, 0, delimiter.Length) == 0;
        }

        private static string QuoteField(string value, string delimiter)
        {
            var needsQuoting = value != null && (
                value.Contains(delimiter) ||
                value.Contains('"') ||
                value.Contains('\r') ||
                value.Contains('\n') ||
                value.StartsWith(" ") ||
                value.StartsWith("\t") ||
                value.EndsWith(" ") ||
                value.EndsWith("\t"));

            if (!needsQuoting)
                return value ?? string.Empty;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        // ── Типизация колонок ──────────────────────────────────────────

        /// <summary>
        /// Пробует поднять тип каждой колонки: int → double → bool → DateTime;
        /// если хоть одно значение не конвертируется — колонка остаётся string.
        /// DBNull/пустые значения при проверке пропускаются.
        /// </summary>
        private static void ApplyTypeInference(DataTable table)
        {
            // Снапшот: внутри цикла Columns мутируют (добавление/удаление),
            // перечисление живой коллекции бросает InvalidOperationException
            foreach (DataColumn column in table.Columns.Cast<DataColumn>().ToArray())
            {
                if (column.DataType != typeof(string))
                    continue;

                var targetType = InferColumnType(table, column);
                if (targetType == null)
                    continue;

                var newColumn = new DataColumn(column.ColumnName + "__tmp", targetType);
                table.Columns.Add(newColumn);

                foreach (DataRow row in table.Rows)
                {
                    var raw = row[column] as string;
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        row[newColumn] = DBNull.Value;
                        continue;
                    }

                    row[newColumn] = ConvertValue(raw, targetType, column.ColumnName);
                }

                var oldOrdinal = column.Ordinal;
                var oldName = column.ColumnName;
                table.Columns.Remove(column);
                newColumn.ColumnName = oldName;
                newColumn.SetOrdinal(oldOrdinal);
            }
        }

        private static Type InferColumnType(DataTable table, DataColumn column)
        {
            var candidates = new[] { typeof(int), typeof(double), typeof(bool), typeof(DateTime) };

            foreach (var candidate in candidates)
            {
                bool allConvertible = true;
                foreach (DataRow row in table.Rows)
                {
                    var raw = row[column] as string;
                    if (string.IsNullOrWhiteSpace(raw))
                        continue; // пустые не мешают выводу типа

                    if (!TryConvert(raw, candidate, out _))
                    {
                        allConvertible = false;
                        break;
                    }
                }

                if (allConvertible)
                    return candidate;
            }

            return null;
        }

        private static object ConvertValue(string raw, Type targetType, string columnName)
        {
            if (!TryConvert(raw, targetType, out var converted))
            {
                // Теоретически недостижимо после InferColumnType, но защищаемся
                throw new InvalidOperationException(
                    $"Значение '{raw}' не конвертируется в {targetType.Name} (колонка '{columnName}')");
            }
            return converted;
        }

        private static bool TryConvert(string raw, Type targetType, out object value)
        {
            value = null;
            try
            {
                switch (targetType)
                {
                    case var _ when targetType == typeof(int):
                        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                            return false;
                        value = i;
                        return true;

                    case var _ when targetType == typeof(double):
                        // InvariantCulture: точка как разделитель (CSV-стандарт)
                        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                            return false;
                        value = d;
                        return true;

                    case var _ when targetType == typeof(bool):
                        if (!bool.TryParse(raw, out var b))
                            return false;
                        value = b;
                        return true;

                    case var _ when targetType == typeof(DateTime):
                        if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                            DateTimeStyles.AllowWhiteSpaces, out var dt))
                            return false;
                        value = dt;
                        return true;

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        // ── Настройки ──────────────────────────────────────────────────

        private static string ResolveDelimiter(string delimiter)
        {
            if (string.IsNullOrEmpty(delimiter))
                return DefaultDelimiter;

            // Именованные разделители для удобства из Studio
            switch (delimiter.Trim().ToLowerInvariant())
            {
                case "tab":
                case "\\t":
                    return "\t";
                case "semicolon":
                case ";":
                    return ";";
                case "pipe":
                case "|":
                    return "|";
                default:
                    return delimiter;
            }
        }

        private static Encoding ResolveEncoding(string encodingName, bool bomForUtf8 = true)
        {
            if (string.IsNullOrWhiteSpace(encodingName)
                || encodingName.Trim().StartsWith("utf-8", StringComparison.OrdinalIgnoreCase)
                || encodingName.Trim().StartsWith("utf8", StringComparison.OrdinalIgnoreCase))
                return new UTF8Encoding(bomForUtf8);

            try
            {
                return Encoding.GetEncoding(encodingName.Trim());
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    $"Неизвестная кодировка '{encodingName}'. Примеры: utf-8, windows-1251, utf-16", ex);
            }
        }
    }
}
