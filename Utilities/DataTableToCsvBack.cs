// =============================================================================
// DataTableToCsvBack.cs — активность «DataTable → CSV» (issue #29).
//
// RFC 4180-райтер: кавычки ставятся только при необходимости,
// DBNULL → пустое поле, кодировка файла настраивается.
// Приёмник: строка (выходная переменная) или файл.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System.Data;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность сборки CSV из DataTable.</summary>
    public class DataTableToCsvBack : PrimoComponentTO<DataTableToCsv>
    {
        public override string GroupName { get => ActivityCategories.Utilities; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propTable;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvInputTable)]
        /// <summary>Входная DataTable.</summary>
        public string Prop_Table
        {
            get => _propTable;
            set { _propTable = value; InvokePropertyChanged(this, nameof(Prop_Table)); }
        }

        private CsvLocation _propTarget = CsvLocation.String;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvTarget)]
        /// <summary>Приёмник результата: строка или файл.</summary>
        public CsvLocation Prop_Target
        {
            get => _propTarget;
            set { _propTarget = value; InvokePropertyChanged(this, nameof(Prop_Target)); }
        }

        private string _propFilePath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvFilePath)]
        /// <summary>Путь к создаваемому CSV-файлу (режим «Файл»).</summary>
        public string Prop_FilePath
        {
            get => _propFilePath;
            set { _propFilePath = value; InvokePropertyChanged(this, nameof(Prop_FilePath)); }
        }

        private string _propDelimiter;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvDelimiter)]
        /// <summary>Разделитель полей; пусто = запятая.</summary>
        public string Prop_Delimiter
        {
            get => _propDelimiter;
            set { _propDelimiter = value; InvokePropertyChanged(this, nameof(Prop_Delimiter)); }
        }

        private bool _propIncludeHeader = true;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvIncludeHeader)]
        /// <summary>Включить строку заголовков колонок.</summary>
        public bool Prop_IncludeHeader
        {
            get => _propIncludeHeader;
            set { _propIncludeHeader = value; InvokePropertyChanged(this, nameof(Prop_IncludeHeader)); }
        }

        private bool _propDBNullToEmpty = true;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvDBNullToEmpty)]
        /// <summary>DBNull → пустое поле (иначе DBNull.Value.ToString() = пусто тоже, но явно).</summary>
        public bool Prop_DBNullToEmpty
        {
            get => _propDBNullToEmpty;
            set { _propDBNullToEmpty = value; InvokePropertyChanged(this, nameof(Prop_DBNullToEmpty)); }
        }

        private string _propEncoding;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvEncoding)]
        /// <summary>Кодировка файла (utf-8, windows-1251); пусто = utf-8 без BOM.</summary>
        public string Prop_Encoding
        {
            get => _propEncoding;
            set { _propEncoding = value; InvokePropertyChanged(this, nameof(Prop_Encoding)); }
        }

        private string _propQuoteChar;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvQuoteChar)]
        /// <summary>Символ кавычек (один символ); пусто = ".</summary>
        public string Prop_QuoteChar
        {
            get => _propQuoteChar;
            set { _propQuoteChar = value; InvokePropertyChanged(this, nameof(Prop_QuoteChar)); }
        }

        private string _propEscapeChar;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvEscapeChar)]
        /// <summary>Символ экранирования кавычек (один символ, например \); пусто = удвоение кавычки.</summary>
        public string Prop_EscapeChar
        {
            get => _propEscapeChar;
            set { _propEscapeChar = value; InvokePropertyChanged(this, nameof(Prop_EscapeChar)); }
        }

        private bool _propAppend;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvAppend)]
        /// <summary>Дописывать в файл: заголовок — только при создании файла (приёмник «Файл»).</summary>
        public bool Prop_Append
        {
            get => _propAppend;
            set { _propAppend = value; InvokePropertyChanged(this, nameof(Prop_Append)); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvResultText)]
        /// <summary>CSV-текст (режим «Строка»).</summary>
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public DataTableToCsvBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DataTableToCsv;
            sdkComponentHelp = @"Компонент ""DataTable → CSV""
Собирает CSV из DataTable по RFC 4180 (без внешних зависимостей).

Основные:
DataTable*: [DataTable] Входная таблица.
Приёмник: [Enum] Строка (переменная) / Файл.
Путь к файлу: [String] Создаваемый CSV-файл (режим «Файл»; папка создаётся при отсутствии).

Настройки:
Разделитель: [String] ',', ';', '|' или таб; пусто = ','.
Заголовки: [Bool] первая строка с именами колонок (по умолчанию да).
DBNull как пустое: [Bool] NULL → пустое поле.
Кодировка: [String] utf-8 (без BOM), windows-1251; пусто = utf-8.
Символ кавычек: [String] один символ; пусто = "" (RFC 4180).
Символ экранирования: [String] один символ (например \); пусто = удвоение кавычки.
Append: [Bool] дописывать в конец файла; заголовок — только при создании файла (приёмник «Файл»).

Выходные данные:
CSV: [String] Текст CSV (режим «Строка»).";
            sdkComponentIcon = ActivityIcons.Table;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<DataTable>("Prop_Table", "Входная DataTable"),
                PropertyBuilder.Enum<CsvLocation>("Prop_Target", "Приёмник: строка или файл"),
                PropertyBuilder.FileSelector("Prop_FilePath", "Путь к CSV-файлу (режим «Файл»)"),
                PropertyBuilder.String("Prop_Delimiter", "Разделитель полей, пусто = ','"),
                PropertyBuilder.BooleanObject("Prop_IncludeHeader", "Включить строку заголовков"),
                PropertyBuilder.BooleanObject("Prop_DBNullToEmpty", "DBNull → пустое поле"),
                PropertyBuilder.String("Prop_Encoding", "Кодировка файла, пусто = utf-8"),
                PropertyBuilder.String("Prop_QuoteChar", "Символ кавычек, пусто = \""),
                PropertyBuilder.String("Prop_EscapeChar", "Символ экранирования, пусто = удвоение"),
                PropertyBuilder.BooleanObject("Prop_Append", "Дописывать в файл (append)"),
                PropertyBuilder.Variable<string>("Prop_Result", "CSV-текст (режим «Строка»)")
            };

            InitClass(container);

            this.Prop_Table = "\"\"";
            this.Prop_FilePath = "\"\"";
            this.Prop_Delimiter = "\"\"";
            this.Prop_Encoding = "\"\"";
            this.Prop_QuoteChar = "\"\"";
            this.Prop_EscapeChar = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new CsvLogic();
            try
            {
                // DataTable приходит как object (по образцу JsonDataTableBack)
                var table = (System.Data.DataTable)GetPropertyValue<object>(
                    this.Prop_Table, nameof(Prop_Table), sd);
                var delimiter = GetPropertyValue<string>(this.Prop_Delimiter, nameof(Prop_Delimiter), sd);
                var encodingName = GetPropertyValue<string>(this.Prop_Encoding, nameof(Prop_Encoding), sd);
                var options = CsvOptions.Parse(
                    delimiter,
                    GetPropertyValue<string>(this.Prop_QuoteChar, nameof(Prop_QuoteChar), sd),
                    GetPropertyValue<string>(this.Prop_EscapeChar, nameof(Prop_EscapeChar), sd),
                    commentChar: null);

                if (this.Prop_Target == CsvLocation.File)
                {
                    var filePath = GetPropertyValue<string>(this.Prop_FilePath, nameof(Prop_FilePath), sd);
                    logic.WriteCsvToFile(table, filePath, options, this.Prop_IncludeHeader, this.Prop_DBNullToEmpty, encodingName, this.Prop_Append);

                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = this.Prop_Append
                            ? $"[DataTable → CSV] дописано {table.Rows.Count} строк → {filePath}"
                            : $"[DataTable → CSV] {table.Rows.Count} строк → {filePath}"
                    };
                }

                if (this.Prop_Append)
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = ActivityStrings.Error_CsvAppendRequiresFile };

                var csv = logic.BuildCsv(table, options, this.Prop_IncludeHeader, this.Prop_DBNullToEmpty);
                SetVariableValue(this.Prop_Result, csv, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[DataTable → CSV] {table.Rows.Count} строк"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка сборки CSV: {ex.Message}"
                };
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Table, ActivityStrings.Field_CsvInputTable, ActivityStrings.Error_CsvTableRequired);
            if (this.Prop_Target == CsvLocation.File)
                ret.ValidateRequired(this.Prop_FilePath, ActivityStrings.Field_CsvFilePath, ActivityStrings.Error_CsvFileRequired);
            return ret;
        }
    }
}
