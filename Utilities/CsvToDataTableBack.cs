// =============================================================================
// CsvToDataTableBack.cs — активность «CSV → DataTable» (issue #29).
//
// Парсер RFC 4180 без внешних зависимостей: кавычки, вложенные
// разделители/переводы строк, настраиваемый разделитель, типизация колонок.
// Источник: строка или файл (кодировка настраивается, BOM определяется).
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
    /// <summary>Активность разбора CSV в DataTable.</summary>
    public class CsvToDataTableBack : PrimoComponentTO<CsvToDataTable>
    {
        public override string GroupName { get => ActivityCategories.Utilities; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        // ── Входные параметры ──────────────────────────────────────────

        private CsvLocation _propSource = CsvLocation.String;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvSource)]
        /// <summary>Источник данных: строка или файл.</summary>
        public CsvLocation Prop_Source
        {
            get => _propSource;
            set { _propSource = value; InvokePropertyChanged(this, nameof(Prop_Source)); }
        }

        private string _propCsv;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvInputText)]
        /// <summary>CSV-текст (режим «Строка»).</summary>
        public string Prop_Csv
        {
            get => _propCsv;
            set { _propCsv = value; InvokePropertyChanged(this, nameof(Prop_Csv)); }
        }

        private string _propFilePath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvFilePath)]
        /// <summary>Путь к CSV-файлу (режим «Файл»).</summary>
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

        private bool _propHasHeader = true;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvHasHeader)]
        /// <summary>Первая строка содержит заголовки колонок.</summary>
        public bool Prop_HasHeader
        {
            get => _propHasHeader;
            set { _propHasHeader = value; InvokePropertyChanged(this, nameof(Prop_HasHeader)); }
        }

        private bool _propTypeInference;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvTypeInference)]
        /// <summary>Выводить типы колонок (int/double/bool/DateTime).</summary>
        public bool Prop_TypeInference
        {
            get => _propTypeInference;
            set { _propTypeInference = value; InvokePropertyChanged(this, nameof(Prop_TypeInference)); }
        }

        private string _propEncoding;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvEncoding)]
        /// <summary>Кодировка файла (utf-8, windows-1251, utf-16); пусто = utf-8.</summary>
        public string Prop_Encoding
        {
            get => _propEncoding;
            set { _propEncoding = value; InvokePropertyChanged(this, nameof(Prop_Encoding)); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propResultTable;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_CsvResultTable)]
        /// <summary>Результирующая DataTable (имя выходной переменной).</summary>
        public string Prop_ResultTable
        {
            get => _propResultTable;
            set { _propResultTable = value; InvokePropertyChanged(this, nameof(Prop_ResultTable)); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public CsvToDataTableBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_CsvToDataTable;
            sdkComponentHelp = @"Компонент ""CSV → DataTable""
Разбирает CSV в DataTable по RFC 4180 (без внешних зависимостей).

Основные:
Источник: [Enum] Строка / Файл.
CSV (входной): [String] CSV-текст (для режима «Строка»).
Путь к файлу: [String] CSV-файл (для режима «Файл», кодировка настраивается, BOM определяется).

Настройки:
Разделитель: [String] ',', ';', '|' или таб; пусто = ','.
Первая строка — заголовки: [Bool] имена колонок из файла (по умолчанию да).
Определять типы колонок: [Bool] int/double/bool/DateTime (иначе все string).
Кодировка: [String] utf-8, windows-1251, utf-16; пусто = utf-8.

Выходные данные:
DataTable: [DataTable] Результат разбора.";
            sdkComponentIcon = ActivityIcons.Table;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<CsvLocation>("Prop_Source", "Источник: строка или файл"),
                PropertyBuilder.String("Prop_Csv", "CSV-текст (режим «Строка»)"),
                PropertyBuilder.FileSelector("Prop_FilePath", "Путь к CSV-файлу (режим «Файл»)"),
                PropertyBuilder.String("Prop_Delimiter", "Разделитель полей, пусто = ','"),
                PropertyBuilder.BooleanObject("Prop_HasHeader", "Первая строка — заголовки колонок"),
                PropertyBuilder.BooleanObject("Prop_TypeInference", "Определять типы колонок"),
                PropertyBuilder.String("Prop_Encoding", "Кодировка файла, пусто = utf-8"),
                PropertyBuilder.Variable<DataTable>("Prop_ResultTable", "Результирующая DataTable")
            };

            InitClass(container);

            this.Prop_Csv = "\"\"";
            this.Prop_FilePath = "\"\"";
            this.Prop_Delimiter = "\"\"";
            this.Prop_Encoding = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new CsvLogic();
            try
            {
                var delimiter = GetPropertyValue<string>(this.Prop_Delimiter, nameof(Prop_Delimiter), sd);
                var encodingName = GetPropertyValue<string>(this.Prop_Encoding, nameof(Prop_Encoding), sd);

                DataTable table;
                if (this.Prop_Source == CsvLocation.File)
                {
                    var filePath = GetPropertyValue<string>(this.Prop_FilePath, nameof(Prop_FilePath), sd);
                    table = logic.ParseFileToDataTable(filePath, delimiter, this.Prop_HasHeader, this.Prop_TypeInference, encodingName);
                }
                else
                {
                    var csv = GetPropertyValue<string>(this.Prop_Csv, nameof(Prop_Csv), sd);
                    table = logic.ParseToDataTable(csv, delimiter, this.Prop_HasHeader, this.Prop_TypeInference);
                }

                SetVariableValue(this.Prop_ResultTable, table, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[CSV → DataTable] строк: {table.Rows.Count}, колонок: {table.Columns.Count}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка разбора CSV: {ex.Message}"
                };
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            if (this.Prop_Source == CsvLocation.File)
                ret.ValidateRequired(this.Prop_FilePath, ActivityStrings.Field_CsvFilePath, ActivityStrings.Error_CsvFileRequired);
            else
                ret.ValidateRequired(this.Prop_Csv, ActivityStrings.Field_CsvInputText, ActivityStrings.Error_CsvTextRequired);
            return ret;
        }
    }
}
