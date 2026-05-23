// =============================================================================
// JsonDataTableBack.cs — активность «JSON: DataTable конвертер».
//
// Конвертирует DataTable в JSON и обратно без сторонних библиотек.
// Использует только System.Text.Json (встроен в .NET 5+) и LINQ.
//
// Направления (JsonConvertDirection):
//   DataTableToJson — DataTable → JSON-строка
//   JsonToDataTable — JSON-строка → DataTable
//
// Форматы JSON (JsonTableFormat, только для DataTableToJson):
//   ArrayOfObjects — [{"Id":1,"Name":"Иван"},...]   — совместим с REST API
//   ArrayOfArrays  — [[1,"Иван"],...]               — компактный (Chart.js и др.)
//   WithHeaders    — {"columns":[...],"rows":[...]} — с явной структурой
//
// NULL-значения (JsonNullMode):
//   JsonNull      → null
//   EmptyString   → ""
//   Zero          → "0"
//   Skip          → поле пропускается
//
// Особенности:
//   - Prop_Columns — выборочная сериализация/десериализация колонок
//   - Prop_DateFormat — формат дат (по умолч. yyyy-MM-dd)
//   - Prop_Indent — форматированный JSON с отступами
//   - Prop_TypeInference — при JsonToDataTable определять типы колонок автоматически
// =============================================================================

using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «JSON: DataTable конвертер».
    /// Конвертирует DataTable ↔ JSON без сторонних зависимостей.
    /// </summary>
    public class JsonDataTableBack : PrimoComponentTO<JsonDataTable>
    {
        // =====================================================================
        // Служебные свойства
        // =====================================================================

        public override string GroupName
        {
            get => ActivityCategories.Utilities;
            protected set { }
        }

        protected override int sdkTimeOut { get => 60000; set { } }

        // =====================================================================
        // Свойства
        // =====================================================================

        #region Prop_Direction
        private JsonConvertDirection _propDirection = JsonConvertDirection.DataTableToJson;
        /// <summary>Направление конвертации.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Направление")]
        public JsonConvertDirection Prop_Direction
        {
            get => _propDirection;
            set { _propDirection = value; InvokePropertyChanged(this, nameof(Prop_Direction)); }
        }
        #endregion

        #region Prop_DataTable
        private string _propDataTable;
        /// <summary>
        /// Входная DataTable (при DataTableToJson).
        /// Тип поля string — содержит имя переменной скрипта.
        /// Значение читается через GetPropertyValue&lt;object&gt; и кастуется к DataTable.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("DataTable (входная)")]
        public string Prop_DataTable
        {
            get => _propDataTable;
            set { _propDataTable = value; InvokePropertyChanged(this, nameof(Prop_DataTable)); }
        }
        #endregion

        #region Prop_Json
        private string _propJson;
        /// <summary>JSON-строка для конвертации (при JsonToDataTable).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("JSON (входной)")]
        public string Prop_Json
        {
            get => _propJson;
            set { _propJson = value; InvokePropertyChanged(this, nameof(Prop_Json)); }
        }
        #endregion

        #region Prop_Format
        private JsonTableFormat _propFormat = JsonTableFormat.ArrayOfObjects;
        /// <summary>
        /// Формат JSON при DataTableToJson.
        /// ArrayOfObjects — наиболее совместимый формат.
        /// ArrayOfArrays  — компактный.
        /// WithHeaders    — с явной структурой колонок.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Формат JSON")]
        public JsonTableFormat Prop_Format
        {
            get => _propFormat;
            set { _propFormat = value; InvokePropertyChanged(this, nameof(Prop_Format)); }
        }
        #endregion

        #region Prop_Columns
        private string _propColumns;
        /// <summary>
        /// Список колонок через запятую.
        /// Пусто = все колонки.
        /// Пример: "Id,Name,Amount"
        /// При JsonToDataTable — читаются только указанные колонки из JSON.
        /// При DataTableToJson — сериализуются только указанные колонки.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Колонки (пусто = все)")]
        public string Prop_Columns
        {
            get => _propColumns;
            set { _propColumns = value; InvokePropertyChanged(this, nameof(Prop_Columns)); }
        }
        #endregion

        #region Prop_DateFormat
        private string _propDateFormat;
        /// <summary>
        /// Формат дат при сериализации DateTime-колонок.
        /// По умолчанию: yyyy-MM-dd.
        /// Примеры: dd.MM.yyyy, yyyy-MM-ddTHH:mm:ss, dd/MM/yyyy HH:mm
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Формат дат")]
        public string Prop_DateFormat
        {
            get => _propDateFormat;
            set { _propDateFormat = value; InvokePropertyChanged(this, nameof(Prop_DateFormat)); }
        }
        #endregion

        #region Prop_Indent
        private bool _propIndent = false;
        /// <summary>Форматировать JSON с отступами (human-readable). По умолч. false.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Форматировать (отступы)")]
        public bool Prop_Indent
        {
            get => _propIndent;
            set { _propIndent = value; InvokePropertyChanged(this, nameof(Prop_Indent)); }
        }
        #endregion

        #region Prop_NullMode
        private JsonNullMode _propNullMode = JsonNullMode.JsonNull;
        /// <summary>Как представлять NULL-значения при DataTableToJson.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("NULL как")]
        public JsonNullMode Prop_NullMode
        {
            get => _propNullMode;
            set { _propNullMode = value; InvokePropertyChanged(this, nameof(Prop_NullMode)); }
        }
        #endregion

        #region Prop_TypeInference
        private bool _propTypeInference = true;
        /// <summary>
        /// При JsonToDataTable — автоматически определять типы колонок
        /// (int, double, bool, DateTime, string) по первой строке данных.
        /// false = всё читается как string.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Определять типы колонок")]
        public bool Prop_TypeInference
        {
            get => _propTypeInference;
            set { _propTypeInference = value; InvokePropertyChanged(this, nameof(Prop_TypeInference)); }
        }
        #endregion

        // ── Выходные параметры ─────────────────────────────────────────────

        #region Prop_ResultJson (Выходной)
        private string _propResultJson;
        /// <summary>Результирующий JSON при DataTableToJson.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Результат (JSON)")]
        public string Prop_ResultJson
        {
            get => _propResultJson;
            set { _propResultJson = value; InvokePropertyChanged(this, nameof(Prop_ResultJson)); }
        }
        #endregion

        #region Prop_ResultTable (Выходной)
        private string _propResultTable;
        /// <summary>Результирующая DataTable при JsonToDataTable.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Результат (DataTable)")]
        public string Prop_ResultTable
        {
            get => _propResultTable;
            set { _propResultTable = value; InvokePropertyChanged(this, nameof(Prop_ResultTable)); }
        }
        #endregion

        #region Prop_RowCount (Выходной)
        private string _propRowCount;
        /// <summary>Количество строк в результате.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Строк")]
        public string Prop_RowCount
        {
            get => _propRowCount;
            set { _propRowCount = value; InvokePropertyChanged(this, nameof(Prop_RowCount)); }
        }
        #endregion

        // =====================================================================
        // Конструктор
        // =====================================================================

        public JsonDataTableBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "JSON: DataTable конвертер";
            sdkComponentHelp =
                "Конвертирует DataTable ↔ JSON без сторонних библиотек.\n" +
                "\n" +
                "── Форматы DataTable → JSON ──────────────────────\n" +
                "ArrayOfObjects — [{\"Id\":1,\"Name\":\"Иван\"},...]\n" +
                "ArrayOfArrays  — [[1,\"Иван\"],...]\n" +
                "WithHeaders    — {\"columns\":[...],\"rows\":[[...]]}\n" +
                "\n" +
                "── Настройки ─────────────────────────────────────\n" +
                "Колонки      — список через запятую (пусто = все)\n" +
                "Формат дат   — yyyy-MM-dd, dd.MM.yyyy и т.п.\n" +
                "NULL как     — null / \"\" / \"0\" / пропуск\n" +
                "Типы колонок — автоопределение int/double/bool/DateTime\n" +
                "\n" +
                "── Выходные данные ───────────────────────────────\n" +
                "ResultJson  (string)    при DataTableToJson\n" +
                "ResultTable (DataTable) при JsonToDataTable\n" +
                "RowCount    (int)";

            sdkComponentIcon = ActivityIcons.Table;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Основные
                PropertyBuilder.Enum<JsonConvertDirection>("Prop_Direction",  "Направление конвертации"),
                PropertyBuilder.Script<DataTable>("Prop_DataTable", "DataTable для конвертации в JSON (при DataTableToJson)"),
                PropertyBuilder.Script<string>("Prop_Json",     "JSON-строка (при JsonToDataTable)"),
                // Настройки
                PropertyBuilder.Enum<JsonTableFormat>("Prop_Format",  "Формат JSON (для DataTableToJson)"),
                PropertyBuilder.Script<string>("Prop_Columns",        "Колонки через запятую (пусто = все)"),
                PropertyBuilder.Script<string>("Prop_DateFormat",     "Формат дат (yyyy-MM-dd)"),
                PropertyBuilder.BooleanObject("Prop_Indent",          "Форматировать JSON с отступами"),
                PropertyBuilder.Enum<JsonNullMode>("Prop_NullMode",   "NULL как (null / пустая строка / 0 / пропуск)"),
                PropertyBuilder.BooleanObject("Prop_TypeInference",   "Автоопределять типы колонок при JsonToDataTable"),
                // Выходные
                PropertyBuilder.Variable<string>("Prop_ResultJson",    "Результирующий JSON"),
                PropertyBuilder.Variable<DataTable>("Prop_ResultTable","Результирующая DataTable"),
                PropertyBuilder.Variable<int>("Prop_RowCount",         "Количество строк"),
            };

            InitClass(container);

            this.Prop_Direction = JsonConvertDirection.DataTableToJson;
            this.Prop_Format = JsonTableFormat.ArrayOfObjects;
            this.Prop_NullMode = JsonNullMode.JsonNull;
            this.Prop_Indent = false;
            this.Prop_TypeInference = true;
            this.Prop_DateFormat = this.IsNoCode(nameof(Prop_DateFormat))
                ? "yyyy-MM-dd" : "\"yyyy-MM-dd\"";
        }

        // =====================================================================
        // Выполнение
        // =====================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string dateFormat = GetPropertyValue<string>(
                    this.Prop_DateFormat, nameof(Prop_DateFormat), sd) ?? "yyyy-MM-dd";

                // Разбираем список нужных колонок через LINQ
                string colsRaw = GetPropertyValue<string>(
                    this.Prop_Columns, nameof(Prop_Columns), sd) ?? string.Empty;
                HashSet<string> selectedCols = new HashSet<string>(
                    colsRaw
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrEmpty(c)),
                    StringComparer.OrdinalIgnoreCase);

                if (this.Prop_Direction == JsonConvertDirection.DataTableToJson)
                    return ExecuteToJson(sd, dateFormat, selectedCols);
                else
                    return ExecuteToDataTable(sd, dateFormat, selectedCols);
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                return Fail($"Ошибка JSON: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                return Fail($"Неверный аргумент: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка конвертации: {ex.Message}");
            }
        }

        // =====================================================================
        // DataTable → JSON
        // =====================================================================

        private ExecutionResult ExecuteToJson(
            ScriptingData sd, string dateFormat, HashSet<string> selectedCols)
        {
            DataTable dt = (DataTable)GetPropertyValue<object>(
                this.Prop_DataTable, nameof(Prop_DataTable), sd);
            if (dt == null)
                return Fail("DataTable не задана");

            // Определяем рабочий набор колонок через LINQ
            List<DataColumn> columns = dt.Columns.Cast<DataColumn>()
                .Where(c => selectedCols.Count == 0
                    || selectedCols.Contains(c.ColumnName))
                .ToList();

            if (columns.Count == 0)
                return Fail($"Ни одна из указанных колонок не найдена в DataTable. " +
                            $"Доступные: {string.Join(", ", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");

            var options = new JsonWriterOptions { Indented = this.Prop_Indent };
            string json;

            switch (this.Prop_Format)
            {
                case JsonTableFormat.ArrayOfObjects:
                    json = SerializeAsArrayOfObjects(dt, columns, options, dateFormat);
                    break;

                case JsonTableFormat.ArrayOfArrays:
                    json = SerializeAsArrayOfArrays(dt, columns, options, dateFormat);
                    break;

                case JsonTableFormat.WithHeaders:
                    json = SerializeWithHeaders(dt, columns, options, dateFormat);
                    break;

                default:
                    json = SerializeAsArrayOfObjects(dt, columns, options, dateFormat);
                    break;
            }

            SetVariableValue(this.Prop_ResultJson, json, sd);
            SetVariableValue(this.Prop_RowCount, dt.Rows.Count, sd);

            return new ExecutionResult
            {
                IsSuccess = true,
                SuccessMessage = $"DataTable → JSON: {dt.Rows.Count} строк, " +
                                 $"{columns.Count} колонок, {json.Length} символов"
            };
        }

        // ── Три формата сериализации ───────────────────────────────────────

        /// <summary>
        /// Сериализует как массив объектов: [{"Col1":val,"Col2":val},...]
        /// Использует Utf8JsonWriter для эффективной записи без промежуточных строк.
        /// </summary>
        private string SerializeAsArrayOfObjects(
            DataTable dt, List<DataColumn> columns,
            JsonWriterOptions opts, string dateFormat)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var writer = new Utf8JsonWriter(ms, opts))
            {
                writer.WriteStartArray();
                foreach (DataRow row in dt.Rows)
                {
                    writer.WriteStartObject();
                    foreach (DataColumn col in columns)
                    {
                        writer.WritePropertyName(col.ColumnName);
                        WriteValue(writer, row[col], col.DataType, dateFormat);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.Flush();

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// Сериализует как массив массивов: [[val1,val2],[val1,val2],...]
        /// Компактный формат — имена колонок не включаются в каждую строку.
        /// </summary>
        private string SerializeAsArrayOfArrays(
            DataTable dt, List<DataColumn> columns,
            JsonWriterOptions opts, string dateFormat)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var writer = new Utf8JsonWriter(ms, opts))
            {
                writer.WriteStartArray();
                foreach (DataRow row in dt.Rows)
                {
                    writer.WriteStartArray();
                    foreach (DataColumn col in columns)
                        WriteValue(writer, row[col], col.DataType, dateFormat);
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                writer.Flush();

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// Сериализует как объект с ключами "columns" и "rows":
        /// {"columns":["Id","Name"],"rows":[[1,"Иван"],[2,"Пётр"]]}
        /// Содержит явную структуру — удобно для десериализации.
        /// </summary>
        private string SerializeWithHeaders(
            DataTable dt, List<DataColumn> columns,
            JsonWriterOptions opts, string dateFormat)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var writer = new Utf8JsonWriter(ms, opts))
            {
                writer.WriteStartObject();

                // "columns": ["Id", "Name", ...]
                writer.WritePropertyName("columns");
                writer.WriteStartArray();
                columns.ForEach(c => writer.WriteStringValue(c.ColumnName));
                writer.WriteEndArray();

                // "rows": [[...],[...],...]
                writer.WritePropertyName("rows");
                writer.WriteStartArray();
                foreach (DataRow row in dt.Rows)
                {
                    writer.WriteStartArray();
                    foreach (DataColumn col in columns)
                        WriteValue(writer, row[col], col.DataType, dateFormat);
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();

                writer.WriteEndObject();
                writer.Flush();

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// Записывает одно значение в Utf8JsonWriter с учётом типа колонки и NullMode.
        /// Поддерживает: int, long, double, decimal, bool, DateTime, byte[], string.
        /// </summary>
        private void WriteValue(
            Utf8JsonWriter writer, object value, Type colType, string dateFormat)
        {
            // NULL-значения
            if (value == null || value == DBNull.Value)
            {
                switch (this.Prop_NullMode)
                {
                    case JsonNullMode.JsonNull: writer.WriteNullValue(); return;
                    case JsonNullMode.EmptyString: writer.WriteStringValue(""); return;
                    case JsonNullMode.Zero: writer.WriteStringValue("0"); return;
                    case JsonNullMode.Skip: writer.WriteNullValue(); return;
                }
                writer.WriteNullValue();
                return;
            }

            // Числовые типы через словарь Action<> вместо цепочки if-else
            // Словарь строится один раз через статическое поле
            if (colType == typeof(int) || colType == typeof(short))
                writer.WriteNumberValue(Convert.ToInt32(value));
            else if (colType == typeof(long))
                writer.WriteNumberValue(Convert.ToInt64(value));
            else if (colType == typeof(double) || colType == typeof(float))
                writer.WriteNumberValue(Convert.ToDouble(value));
            else if (colType == typeof(decimal))
                writer.WriteNumberValue(Convert.ToDecimal(value));
            else if (colType == typeof(bool))
                writer.WriteBooleanValue(Convert.ToBoolean(value));
            else if (colType == typeof(DateTime))
                writer.WriteStringValue(Convert.ToDateTime(value).ToString(dateFormat));
            else if (colType == typeof(byte[]))
                writer.WriteBase64StringValue((byte[])value);
            else
                writer.WriteStringValue(value.ToString());
        }

        // =====================================================================
        // JSON → DataTable
        // =====================================================================

        private ExecutionResult ExecuteToDataTable(
            ScriptingData sd, string dateFormat, HashSet<string> selectedCols)
        {
            string json = GetPropertyValue<string>(
                this.Prop_Json, nameof(Prop_Json), sd);

            if (string.IsNullOrWhiteSpace(json))
                return Fail("JSON не задан");

            DataTable dt;
            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    dt = this.Prop_Format == JsonTableFormat.WithHeaders
                        ? ParseWithHeaders(doc.RootElement, selectedCols, dateFormat)
                        : ParseArrayFormat(doc.RootElement, selectedCols, dateFormat);
                }
            }
            catch (JsonException ex)
            {
                return Fail($"Некорректный JSON: {ex.Message}");
            }

            SetVariableValue(this.Prop_ResultTable, dt, sd);
            SetVariableValue(this.Prop_RowCount, dt.Rows.Count, sd);

            return new ExecutionResult
            {
                IsSuccess = true,
                SuccessMessage = $"JSON → DataTable: {dt.Rows.Count} строк, " +
                                 $"{dt.Columns.Count} колонок"
            };
        }

        /// <summary>
        /// Парсит массив объектов или массив массивов.
        /// Автоматически определяет формат по первому элементу.
        /// </summary>
        private DataTable ParseArrayFormat(
            JsonElement root, HashSet<string> selectedCols, string dateFormat)
        {
            var dt = new DataTable();

            if (root.ValueKind != JsonValueKind.Array)
                throw new JsonException("Ожидался JSON-массив ([...])");

            var elements = root.EnumerateArray().ToList();
            if (elements.Count == 0)
                return dt; // Пустой массив — пустая таблица

            // Определяем формат по первому элементу
            bool isArrayOfObjects = elements[0].ValueKind == JsonValueKind.Object;

            if (isArrayOfObjects)
            {
                // ── Массив объектов: [{"Col":val},...] ──────────────────────

                // Определяем колонки из первого объекта через LINQ
                var allCols = elements[0].EnumerateObject()
                    .Select(p => p.Name)
                    .Where(n => selectedCols.Count == 0 || selectedCols.Contains(n))
                    .ToList();

                // Создаём колонки с типами если включено TypeInference
                foreach (string colName in allCols)
                {
                    Type colType = this.Prop_TypeInference
                        ? InferType(elements[0].GetProperty(colName))
                        : typeof(string);
                    dt.Columns.Add(colName, colType);
                }

                // Заполняем строки через LINQ — конвертируем каждый объект в DataRow
                foreach (JsonElement elem in elements)
                {
                    DataRow row = dt.NewRow();
                    foreach (string colName in allCols)
                    {
                        if (elem.TryGetProperty(colName, out JsonElement val))
                            row[colName] = ParseJsonValue(val, dt.Columns[colName].DataType, dateFormat);
                        else
                            row[colName] = DBNull.Value;
                    }
                    dt.Rows.Add(row);
                }
            }
            else
            {
                // ── Массив массивов: [[val1,val2],...] ──────────────────────

                int colCount = elements[0].GetArrayLength();

                // Создаём колонки Col0, Col1, Col2...
                Enumerable.Range(0, colCount)
                    .ToList()
                    .ForEach(i =>
                    {
                        string name = $"Col{i}";
                        Type type = this.Prop_TypeInference
                            ? InferType(elements[0][i])
                            : typeof(string);
                        dt.Columns.Add(name, type);
                    });

                // Заполняем строки
                foreach (JsonElement elem in elements)
                {
                    DataRow row = dt.NewRow();
                    var vals = elem.EnumerateArray().ToList();
                    for (int i = 0; i < Math.Min(vals.Count, dt.Columns.Count); i++)
                        row[i] = ParseJsonValue(vals[i], dt.Columns[i].DataType, dateFormat);
                    dt.Rows.Add(row);
                }
            }

            return dt;
        }

        /// <summary>
        /// Парсит формат WithHeaders:
        /// {"columns":["Id","Name"],"rows":[[1,"Иван"],...]}
        /// </summary>
        private DataTable ParseWithHeaders(
            JsonElement root, HashSet<string> selectedCols, string dateFormat)
        {
            var dt = new DataTable();

            if (root.ValueKind != JsonValueKind.Object)
                throw new JsonException("Ожидался JSON-объект {\"columns\":[...],\"rows\":[...]}");

            // Читаем колонки
            var colNames = root.GetProperty("columns")
                .EnumerateArray()
                .Select(e => e.GetString())
                .Where(n => n != null && (selectedCols.Count == 0 || selectedCols.Contains(n)))
                .ToList();

            var rows = root.GetProperty("rows").EnumerateArray().ToList();

            // Определяем типы по первой строке если есть данные
            if (rows.Count > 0 && this.Prop_TypeInference)
            {
                var firstRow = rows[0].EnumerateArray().ToList();
                colNames.ForEach((colName) =>
                {
                    // Получаем индекс оригинальной колонки (без фильтра selectedCols)
                    var origCols = root.GetProperty("columns")
                        .EnumerateArray()
                        .Select(e => e.GetString())
                        .ToList();
                    int idx = origCols.IndexOf(colName);
                    Type type = idx >= 0 && idx < firstRow.Count
                        ? InferType(firstRow[idx])
                        : typeof(string);
                    dt.Columns.Add(colName, type);
                });
            }
            else
                colNames.ForEach(n => dt.Columns.Add(n, typeof(string)));

            // Индексы оригинальных колонок → отфильтрованных
            var origColsList = root.GetProperty("columns")
                .EnumerateArray()
                .Select(e => e.GetString())
                .ToList();

            var colIndexMap = colNames
                .ToDictionary(
                    n => n,
                    n => origColsList.IndexOf(n)
                );

            // Заполняем строки
            foreach (JsonElement rowElem in rows)
            {
                var vals = rowElem.EnumerateArray().ToList();
                DataRow row = dt.NewRow();
                foreach (string colName in colNames)
                {
                    int idx = colIndexMap[colName];
                    if (idx >= 0 && idx < vals.Count)
                        row[colName] = ParseJsonValue(vals[idx], dt.Columns[colName].DataType, dateFormat);
                    else
                        row[colName] = DBNull.Value;
                }
                dt.Rows.Add(row);
            }

            return dt;
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>
        /// Определяет тип C# по значению JsonElement.
        /// Приоритет: int → long → double → bool → DateTime → string.
        /// </summary>
        private static Type InferType(JsonElement elem)
        {
            switch (elem.ValueKind)
            {
                case JsonValueKind.Number:
                    if (elem.TryGetInt32(out _)) return typeof(int);
                    if (elem.TryGetInt64(out _)) return typeof(long);
                    if (elem.TryGetDouble(out _)) return typeof(double);
                    return typeof(decimal);

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return typeof(bool);

                case JsonValueKind.String:
                    string s = elem.GetString() ?? string.Empty;
                    if (DateTime.TryParse(s, out _)) return typeof(DateTime);
                    return typeof(string);

                default:
                    return typeof(string);
            }
        }

        /// <summary>
        /// Конвертирует JsonElement в значение указанного типа C#.
        /// DBNull при JsonValueKind.Null.
        /// </summary>
        private static object ParseJsonValue(
            JsonElement elem, Type targetType, string dateFormat)
        {
            if (elem.ValueKind == JsonValueKind.Null)
                return DBNull.Value;

            try
            {
                if (targetType == typeof(int)) return elem.GetInt32();
                if (targetType == typeof(long)) return elem.GetInt64();
                if (targetType == typeof(double)) return elem.GetDouble();
                if (targetType == typeof(decimal)) return elem.GetDecimal();
                if (targetType == typeof(bool))
                    return elem.ValueKind == JsonValueKind.True ||
                           (elem.ValueKind == JsonValueKind.String &&
                            bool.TryParse(elem.GetString(), out bool b) && b);
                if (targetType == typeof(DateTime))
                {
                    string sv = elem.ValueKind == JsonValueKind.String
                        ? elem.GetString()
                        : elem.ToString();
                    return DateTime.TryParse(sv, out DateTime dt) ? dt : (object)DBNull.Value;
                }
                // string и всё остальное
                return elem.ValueKind == JsonValueKind.String
                    ? elem.GetString()
                    : elem.ToString();
            }
            catch
            {
                // Fallback — возвращаем как строку
                return elem.ValueKind == JsonValueKind.String
                    ? elem.GetString()
                    : elem.ToString();
            }
        }

        // =====================================================================
        // Валидация
        // =====================================================================

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (this.Prop_Direction == JsonConvertDirection.DataTableToJson
                && string.IsNullOrWhiteSpace(this.Prop_DataTable))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_DataTable),
                    Error = "DataTable обязательна при конвертации DataTable → JSON"
                });

            if (this.Prop_Direction == JsonConvertDirection.JsonToDataTable
                && string.IsNullOrWhiteSpace(this.Prop_Json))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_Json),
                    Error = "JSON обязателен при конвертации JSON → DataTable"
                });

            return ret;
        }

        private static ExecutionResult Fail(string msg) =>
            new ExecutionResult { IsSuccess = false, ErrorMessage = msg };
    }
}