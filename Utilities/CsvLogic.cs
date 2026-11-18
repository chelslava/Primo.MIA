// =============================================================================
// CsvLogic.cs — чистая логика CSV ↔ DataTable (issue #29, расширенные
// опции диалекта — issue #48).
//
// RFC 4180: кавычки экранируются удвоением (""), кавычкованное поле может
// содержать разделители и переводы строк; разделитель настраиваемый
// (по умолчанию ','), переводы строк CRLF/LF/CR принимаются на чтении.
// Опциональная типизация колонок (int → double → bool → DateTime → string)
// по образцу JsonDataTable (Prop_TypeInference).
//
// Расширенные опции (issue #48, все опциональны — дефолты не меняют
// поведение существующих вызовов):
//   - настраиваемый символ кавычек (например, ') и экранирования (\);
//   - пропуск строк-комментариев (задаваемый символ, только вне кавычек
//     и только в начале записи);
//   - потоковое чтение файла: записи отдаются по мере разбора, весь текст
//     и список записей в память не грузятся (колонки остаются string —
//     вывод типов требует полного прохода);
//   - append-режим записи: заголовок пишется только при создании файла.
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
    /// Опции CSV-диалекта (issue #48): символы кавычек/экранирования/комментария.
    /// </summary>
    public class CsvOptions
    {
        /// <summary>Символ кавычек по умолчанию (RFC 4180).</summary>
        public const char DefaultQuoteChar = '"';

        /// <summary>Разделитель полей (сырая строка студии; пусто → ','; поддерживаются tab/semicolon/pipe).</summary>
        public string Delimiter { get; set; }

        /// <summary>Символ кавычек.</summary>
        public char QuoteChar { get; set; } = DefaultQuoteChar;

        /// <summary>Символ экранирования (null — только удвоение кавычки, RFC 4180).</summary>
        public char? EscapeChar { get; set; }

        /// <summary>Символ комментария (null — комментарии отключены).</summary>
        public char? CommentChar { get; set; }

        /// <summary>
        /// Разбирает обязательный односимвольный параметр; пусто → fallback,
        /// не-одиночный символ → понятная ошибка.
        /// </summary>
        public static char ParseCharOption(string value, char fallback, string displayName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            var trimmed = value.Trim();
            if (trimmed.Length != 1)
                throw new InvalidOperationException(
                    $"«{displayName}»: ожидается один символ, получено «{value}»");
            return trimmed[0];
        }

        /// <summary>
        /// Разбирает опциональный односимвольный параметр; пусто → null (опция выключена).
        /// </summary>
        public static char? ParseOptionalCharOption(string value, string displayName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            if (trimmed.Length != 1)
                throw new InvalidOperationException(
                    $"«{displayName}»: ожидается один символ или пусто (отключено), получено «{value}»");
            return trimmed[0];
        }

        /// <summary>
        /// Собирает опции из строковых значений студии. Экранирование, совпадающее
        /// с кавычкой, отключается (работает каноничное удвоение).
        /// </summary>
        public static CsvOptions Parse(string delimiter, string quoteChar, string escapeChar, string commentChar)
        {
            var quote = ParseCharOption(quoteChar, DefaultQuoteChar, ActivityStrings.Field_CsvQuoteChar);
            var escape = ParseOptionalCharOption(escapeChar, ActivityStrings.Field_CsvEscapeChar);
            if (escape == quote)
                escape = null;

            return new CsvOptions
            {
                Delimiter = delimiter,
                QuoteChar = quote,
                EscapeChar = escape,
                CommentChar = ParseOptionalCharOption(commentChar, ActivityStrings.Field_CsvCommentChar)
            };
        }
    }

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
            return ParseToDataTable(csv, new CsvOptions { Delimiter = delimiter }, hasHeaderRow, typeInference);
        }

        /// <summary>
        /// Разбирает CSV-текст в DataTable с настройками диалекта (issue #48).
        /// </summary>
        public DataTable ParseToDataTable(
            string csv,
            CsvOptions options,
            bool hasHeaderRow,
            bool typeInference)
        {
            Guard.NotNull(csv, nameof(csv));
            Guard.NotNull(options, nameof(options));

            var splitter = new RecordSplitter(options);
            splitter.Append(csv);
            splitter.Finish();
            var rows = splitter.TakeAll();

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
            return ParseFileToDataTable(
                filePath, new CsvOptions { Delimiter = delimiter }, hasHeaderRow, typeInference, encodingName);
        }

        /// <summary>
        /// Читает CSV из файла с настройками диалекта (issue #48).
        /// </summary>
        public DataTable ParseFileToDataTable(
            string filePath,
            CsvOptions options,
            bool hasHeaderRow,
            bool typeInference,
            string encodingName)
        {
            Guard.NotNullOrWhiteSpace(filePath, nameof(filePath));

            var encoding = ResolveEncoding(encodingName);
            string csv;
            using (var reader = new StreamReader(filePath, encoding, detectEncodingFromByteOrderMarks: true))
                csv = reader.ReadToEnd();

            return ParseToDataTable(csv, options, hasHeaderRow, typeInference);
        }

        /// <summary>
        /// Потоковое чтение CSV из файла (issue #48): записи отдаются по мере
        /// разбора — весь текст и список записей в память не грузятся.
        /// Колонки остаются string: вывод типов требует полного прохода.
        /// Число колонок задаёт заголовок (или первая запись без него);
        /// «лишние» поля последующих записей игнорируются.
        /// </summary>
        public DataTable ParseFileStreaming(
            string filePath,
            CsvOptions options,
            bool hasHeaderRow,
            string encodingName)
        {
            Guard.NotNullOrWhiteSpace(filePath, nameof(filePath));
            Guard.NotNull(options, nameof(options));

            var encoding = ResolveEncoding(encodingName);
            var table = new DataTable(DefaultTableName);
            var columnsReady = false;

            using (var reader = new StreamReader(filePath, encoding, detectEncodingFromByteOrderMarks: true))
            {
                ReadRecords(reader, options, record =>
                {
                    if (!columnsReady)
                    {
                        // Первая запись задаёт колонки: с заголовком — из него,
                        // без — из первой записи данных (имена Column1..N)
                        var source = hasHeaderRow ? record : null;
                        for (int c = 0; c < record.Count; c++)
                        {
                            var name = source != null && c < source.Count
                                && !string.IsNullOrWhiteSpace(source[c])
                                ? source[c].Trim()
                                : $"Column{c + 1}";
                            table.Columns.Add(name, typeof(string));
                        }
                        columnsReady = true;

                        if (hasHeaderRow)
                            return; // запись-заголовок в данные не попадает
                    }

                    var row = table.NewRow();
                    for (int c = 0; c < table.Columns.Count; c++)
                        row[c] = c < record.Count ? record[c] : string.Empty;
                    table.Rows.Add(row);
                });
            }

            return table;
        }

        /// <summary>
        /// Потоковое чтение записей CSV из произвольного TextReader (issue #48):
        /// каждая завершённая запись отдаётся через <paramref name="onRecord"/>
        /// сразу после разбора. Поддерживает переводы строк внутри
        /// кавычкованных полей и комментарии (в начале записи, вне кавычек).
        /// </summary>
        public void ReadRecords(TextReader reader, CsvOptions options, Action<List<string>> onRecord)
        {
            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(onRecord, nameof(onRecord));

            var splitter = new RecordSplitter(options);
            var buffer = new char[8192];
            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                splitter.Append(buffer, read);
                foreach (var record in splitter.TakeFinished())
                    onRecord(record);
            }
            splitter.Finish();
            foreach (var record in splitter.TakeFinished())
                onRecord(record);
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
            return BuildCsv(table, new CsvOptions { Delimiter = delimiter }, includeHeader, convertDBNullToEmpty);
        }

        /// <summary>
        /// Собирает CSV-текст из DataTable с настройками диалекта (issue #48).
        /// Экранирование кавычки: удвоение (по умолчанию) или префикс
        /// <see cref="CsvOptions.EscapeChar"/>.
        /// </summary>
        public string BuildCsv(
            DataTable table,
            CsvOptions options,
            bool includeHeader,
            bool convertDBNullToEmpty)
        {
            Guard.NotNull(table, nameof(table));
            Guard.NotNull(options, nameof(options));

            var delim = ResolveDelimiter(options.Delimiter);
            var sb = new StringBuilder();

            if (includeHeader)
            {
                var headers = table.Columns.Cast<DataColumn>().Select(c => QuoteField(c.ColumnName, delim, options));
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
                    fields.Add(QuoteField(text, delim, options));
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
            WriteCsvToFile(table, filePath, new CsvOptions { Delimiter = delimiter },
                includeHeader, convertDBNullToEmpty, encodingName, append: false);
        }

        /// <summary>
        /// Записывает CSV в файл с настройками диалекта (issue #48).
        /// При <paramref name="append"/> = true дописывает в конец; заголовок
        /// пишется только при создании нового (пустого) файла. Запись идёт
        /// построчно — большой DataTable не собирается в одну строку.
        /// </summary>
        public void WriteCsvToFile(
            DataTable table,
            string filePath,
            CsvOptions options,
            bool includeHeader,
            bool convertDBNullToEmpty,
            string encodingName,
            bool append)
        {
            Guard.NotNull(table, nameof(table));
            Guard.NotNullOrWhiteSpace(filePath, nameof(filePath));
            Guard.NotNull(options, nameof(options));

            var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var delim = ResolveDelimiter(options.Delimiter);
            var fileExists = File.Exists(filePath) && new FileInfo(filePath).Length > 0;
            var writeHeader = includeHeader && (!append || !fileExists);

            using (var writer = new StreamWriter(filePath, append, ResolveEncoding(encodingName, bomForUtf8: false)))
            {
                if (writeHeader)
                    writer.WriteLine(string.Join(delim,
                        table.Columns.Cast<DataColumn>().Select(c => QuoteField(c.ColumnName, delim, options))));

                foreach (DataRow row in table.Rows)
                {
                    var fields = new List<string>(table.Columns.Count);
                    foreach (var column in table.Columns.Cast<DataColumn>())
                    {
                        var value = row[column];
                        var text = convertDBNullToEmpty && (value == null || value == DBNull.Value)
                            ? string.Empty
                            : value?.ToString() ?? string.Empty;
                        fields.Add(QuoteField(text, delim, options));
                    }
                    writer.WriteLine(string.Join(delim, fields));
                }
            }
        }

        // ── RFC 4180: разбор записей ───────────────────────────────────

        /// <summary>
        /// Разбивает CSV на записи (список полей). Состоянийная машина:
        /// в кавычках разделители и переводы строк — часть данных, "" — кавычка.
        /// </summary>
        private static List<List<string>> SplitRecords(string csv, CsvOptions options)
        {
            var splitter = new RecordSplitter(options);
            splitter.Append(csv);
            splitter.Finish();
            return splitter.TakeAll();
        }

        /// <summary>
        /// Инкрементальный разборщик записей CSV (issue #48): один и тот же
        /// автомат обслуживает и разбор строки целиком, и потоковое чтение
        /// по кускам — поведение идентично. Разделитель многосимвольный:
        /// нерешённые хвостовые символы держатся в окне до набора длины.
        /// </summary>
        private sealed class RecordSplitter
        {
            private readonly string _delimiter;
            private readonly char _quote;
            private readonly char? _escape;
            private readonly char? _comment;

            // списки заменяются новыми (не Clear) — ссылки уходят в результаты
            private List<List<string>> _finished = new List<List<string>>();
            private List<string> _fields = new List<string>();
            private readonly StringBuilder _field = new StringBuilder();
            private readonly StringBuilder _pending = new StringBuilder(); // окно разделителя

            private bool _inQuotes;
            private bool _fieldStarted;
            private bool _atRecordStart = true;
            private bool _skipLine;      // строка комментария — пропускается до \n
            private bool _skipLf;        // \n после \r — та же граница записи (CRLF)
            private bool _escapePending; // ждём экранированный символ
            private bool _quotePending;  // ждём: удвоение кавычки или закрытие поля
            private char? _replay;       // символ, закрывший кавычки — подаётся повторно «вне кавычек»

            public RecordSplitter(CsvOptions options)
            {
                _delimiter = ResolveDelimiter(options?.Delimiter);
                _quote = options?.QuoteChar ?? CsvOptions.DefaultQuoteChar;
                _escape = options?.EscapeChar;
                _comment = options?.CommentChar;
            }

            public void Append(string text)
            {
                if (text == null || text.Length == 0)
                    return;
                for (int i = 0; i < text.Length; i++)
                    Step(text[i]);
            }

            public void Append(char[] buffer, int count)
            {
                for (int i = 0; i < count; i++)
                    Step(buffer[i]);
            }

            /// <summary>Завершает разбор: последняя запись без перевода строки тоже отдаётся.</summary>
            public void Finish()
            {
                // символ в реплее мог остаться без следующего Append
                if (_replay != null)
                {
                    var replayed = _replay.Value;
                    _replay = null;
                    FeedOutside(replayed);
                }

                // окно разделителя могло держать хвостовые символы
                while (_pending.Length > 0)
                {
                    if (MatchDelimiter(_pending.ToString(), 0))
                    {
                        EndField();
                        _pending.Remove(0, _delimiter.Length);
                        continue;
                    }
                    var ch = _pending[0];
                    _pending.Remove(0, 1);
                    StepCore(ch);
                }

                if (_field.Length > 0 || _fieldStarted || _fields.Count > 0)
                    EndRecord();
            }

            /// <summary>Забирает все накопленные записи (строковый API).</summary>
            public List<List<string>> TakeAll()
            {
                var result = _finished;
                _finished = new List<List<string>>();
                return result;
            }

            /// <summary>Забирает записи, завершённые с прошлого вызова (потоковый API).</summary>
            public List<List<string>> TakeFinished()
            {
                return TakeAll();
            }

            private void Step(char ch)
            {
                // символ, на котором завершились кавычки, переподдаётся
                // по правилам «вне кавычек» (разделитель/граница записи)
                if (_replay != null)
                {
                    var replayed = _replay.Value;
                    _replay = null;
                    FeedOutside(replayed);
                }

                if (_inQuotes || _skipLine)
                    StepCore(ch);
                else
                    FeedOutside(ch);
            }

            /// <summary>
            /// Вне кавычек многосимвольный разделитель ищется через окно:
            /// символ держится, пока не наберётся длина разделителя.
            /// </summary>
            private void FeedOutside(char ch)
            {
                if (_inQuotes || _skipLine)
                {
                    StepCore(ch);
                    return;
                }

                _pending.Append(ch);
                while (_pending.Length >= _delimiter.Length)
                {
                    if (MatchDelimiter(_pending.ToString(), 0))
                    {
                        EndField();
                        _pending.Remove(0, _delimiter.Length);
                        continue;
                    }
                    var head = _pending[0];
                    _pending.Remove(0, 1);
                    StepCore(head);
                }
            }

            /// <summary>Обработка одного символа без оконной логики разделителя.</summary>
            private void StepCore(char ch)
            {
                if (_skipLine)
                {
                    if (ch == '\n')
                        _skipLine = false; // конец строки комментария; \r пропускается
                    return;
                }

                if (_inQuotes)
                {
                    if (_escapePending)
                    {
                        // символ после экранирования — всегда литерал
                        _field.Append(ch);
                        _escapePending = false;
                        return;
                    }

                    if (_escape != null && ch == _escape.Value)
                    {
                        _escapePending = true;
                        return;
                    }

                    if (ch == _quote)
                    {
                        if (!_quotePending)
                        {
                            _quotePending = true; // кавычка: закрытие поля или удвоение
                            return;
                        }

                        // удвоение → литеральная кавычка, режим кавычек сохраняется
                        _quotePending = false;
                        _field.Append(_quote);
                        return;
                    }

                    if (_quotePending)
                    {
                        // не удвоение → кавычка закрыла поле; символ обрабатывается
                        // заново по правилам «вне кавычек» (может быть \n или разделитель)
                        _quotePending = false;
                        _inQuotes = false;
                        _replay = ch;
                        return;
                    }

                    _field.Append(ch);
                    return;
                }

                if (_skipLf)
                {
                    // CRLF: \n после \r — не новая граница, а часть прошлой
                    _skipLf = false;
                    if (ch == '\n')
                        return;
                }

                if (_comment != null && _atRecordStart && ch == _comment.Value)
                {
                    _skipLine = true;
                    return;
                }

                if (ch == _quote && _field.Length == 0 && !_fieldStarted)
                {
                    // Кавычка в начале поля → кавычкованный режим
                    _inQuotes = true;
                    _fieldStarted = true;
                    _atRecordStart = false;
                    return;
                }

                if (ch == '\r')
                {
                    EndRecord();
                    _skipLf = true;
                    return;
                }

                if (ch == '\n')
                {
                    EndRecord();
                    return;
                }

                _field.Append(ch);
                _fieldStarted = true;
                _atRecordStart = false;
            }

            private void EndField()
            {
                _fields.Add(_field.ToString());
                _field.Clear();
                _fieldStarted = false;
                _atRecordStart = false;
            }

            private void EndRecord()
            {
                EndField();
                _finished.Add(_fields);
                _fields = new List<string>(); // новая запись — новый список (ссылка ушла в результат)
                _atRecordStart = true;
            }

            private bool MatchDelimiter(string text, int position)
            {
                if (position + _delimiter.Length > text.Length)
                    return false;
                return string.CompareOrdinal(text, position, _delimiter, 0, _delimiter.Length) == 0;
            }
        }

        private static string QuoteField(string value, string delimiter, CsvOptions options)
        {
            var quoteChar = options?.QuoteChar ?? CsvOptions.DefaultQuoteChar;
            var quoteText = quoteChar.ToString();

            var needsQuoting = value != null && (
                value.Contains(delimiter) ||
                value.Contains(quoteText) ||
                value.Contains('\r') ||
                value.Contains('\n') ||
                value.StartsWith(" ") ||
                value.StartsWith("\t") ||
                value.EndsWith(" ") ||
                value.EndsWith("\t"));

            if (!needsQuoting)
                return value ?? string.Empty;

            var escape = options?.EscapeChar;
            var escaped = escape != null
                // диалект с экранированием: сам экранирующий символ тоже удваивается
                ? value
                    .Replace(escape.Value.ToString(), escape.Value.ToString() + escape.Value.ToString())
                    .Replace(quoteText, escape.Value.ToString() + quoteText)
                : value.Replace(quoteText, quoteText + quoteText); // RFC 4180: удвоение

            return quoteText + escaped + quoteText;
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
