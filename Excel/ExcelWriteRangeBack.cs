// =============================================================================
// ExcelWriteRangeBack.cs — активность «Excel: Записать диапазон» (issue #47).
// Пишет DataTable в .xlsx без Interop (ClosedXML): перезапись от начальной
// ячейки или дописывание после последней занятой строки.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace Primo.MIA
{
    /// <summary>Активность записи DataTable в .xlsx.</summary>
    public class ExcelWriteRangeBack : PrimoComponentTO<ExcelWriteRange>
    {
        public override string GroupName { get => ActivityCategories.Excel; protected set { } }

        protected override int sdkTimeOut { get => 60000; set { } }

        #region Входные параметры

        private string _propFilePath;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelFilePath)]
        /// <summary>Путь к файлу .xlsx (создаётся при отсутствии, если включено).</summary>
        public string Prop_FilePath
        {
            get => _propFilePath;
            set { _propFilePath = value; InvokePropertyChanged(this, nameof(Prop_FilePath)); }
        }

        private string _propSheetName;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelSheetName)]
        /// <summary>Имя листа; пусто — первый лист (для нового файла — Sheet1); создаётся при отсутствии.</summary>
        public string Prop_SheetName
        {
            get => _propSheetName;
            set { _propSheetName = value; InvokePropertyChanged(this, nameof(Prop_SheetName)); }
        }

        private string _propTable;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelTable)]
        /// <summary>Исходная таблица DataTable.</summary>
        public string Prop_Table
        {
            get => _propTable;
            set { _propTable = value; InvokePropertyChanged(this, nameof(Prop_Table)); }
        }

        private ExcelWriteMode _propMode = ExcelWriteMode.Overwrite;
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelWriteMode)]
        /// <summary>Режим записи: перезапись от начальной ячейки или дописывание.</summary>
        public ExcelWriteMode Prop_Mode
        {
            get => _propMode;
            set { _propMode = value; InvokePropertyChanged(this, nameof(Prop_Mode)); }
        }

        private string _propStartCell;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelStartCell)]
        /// <summary>Начальная ячейка («A1») для режима перезаписи.</summary>
        public string Prop_StartCell
        {
            get => _propStartCell;
            set { _propStartCell = value; InvokePropertyChanged(this, nameof(Prop_StartCell)); }
        }

        private bool _propWriteHeaders = true;
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelWriteHeaders)]
        /// <summary>Записать имена колонок первой строкой.</summary>
        public bool Prop_WriteHeaders
        {
            get => _propWriteHeaders;
            set { _propWriteHeaders = value; InvokePropertyChanged(this, nameof(Prop_WriteHeaders)); }
        }

        private bool _propCreateIfMissing = true;
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelCreateIfMissing)]
        /// <summary>Создавать файл/лист при отсутствии.</summary>
        public bool Prop_CreateIfMissing
        {
            get => _propCreateIfMissing;
            set { _propCreateIfMissing = value; InvokePropertyChanged(this, nameof(Prop_CreateIfMissing)); }
        }

        #endregion

        #region Выходные параметры

        private string _propWrittenRows;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelWrittenRows)]
        /// <summary>Имя переменной (Int32) для числа записанных строк данных.</summary>
        public string Prop_WrittenRows
        {
            get => _propWrittenRows;
            set { _propWrittenRows = value; InvokePropertyChanged(this, nameof(Prop_WrittenRows)); }
        }

        #endregion

        public ExcelWriteRangeBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ExcelWriteRange;
            sdkComponentHelp = @"Компонент ""Excel: Записать диапазон""
Записывает DataTable в .xlsx без Interop (не требует установленного Excel).

Основные:
Файл*: [String] Путь к .xlsx (создаётся при отсутствии).
Лист: [String] Пусто — первый лист; создаётся при отсутствии.
Таблица*: [DataTable] Записываемая таблица.

Параметры:
Режим записи: Перезаписать (от начальной ячейки) / Дописать после последней строки.
Начальная ячейка: [String] Для перезаписи, по умолчанию A1.
Записать заголовки: [Boolean] Имена колонок первой строкой.

Выходные:
Записано строк (Int32).";
            sdkComponentIcon = ActivityIcons.Excel;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_FilePath", "Путь к файлу .xlsx"),
                PropertyBuilder.String("Prop_SheetName", "Имя листа; пусто — первый лист; создаётся при отсутствии"),
                PropertyBuilder.String("Prop_Table", "Переменная (DataTable) для записи"),
                PropertyBuilder.Enum<ExcelWriteMode>("Prop_Mode", "Перезапись от начальной ячейки / дописывание после последней строки"),
                PropertyBuilder.String("Prop_StartCell", "Начальная ячейка для перезаписи (по умолчанию A1)"),
                PropertyBuilder.BooleanObject("Prop_WriteHeaders", "Записать имена колонок первой строкой"),
                PropertyBuilder.BooleanObject("Prop_CreateIfMissing", "Создавать файл/лист при отсутствии"),
                PropertyBuilder.Variable<int>("Prop_WrittenRows", "Переменная (Int32) для числа записанных строк"),
            };

            InitClass(container);

            this.Prop_FilePath  = "\"\"";
            this.Prop_SheetName = "\"\"";
            this.Prop_Table     = "\"\"";
            this.Prop_Mode      = ExcelWriteMode.Overwrite;
            this.Prop_StartCell = "\"A1\"";
            this.Prop_WriteHeaders   = true;
            this.Prop_CreateIfMissing = true;
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string filePath = GetPropertyValue<string>(this.Prop_FilePath, nameof(Prop_FilePath), sd);
                string sheetName = GetPropertyValue<string>(this.Prop_SheetName, nameof(Prop_SheetName), sd);
                string startCell = GetPropertyValue<string>(this.Prop_StartCell, nameof(Prop_StartCell), sd);
                var table = (DataTable)GetPropertyValue<object>(this.Prop_Table, nameof(Prop_Table), sd);

                if (table == null)
                    return Fail(ActivityStrings.Error_ExcelTableRequired);

                int written = ExcelLogic.WriteRange(
                    filePath, sheetName, startCell,
                    this.Prop_Mode, this.Prop_CreateIfMissing, this.Prop_WriteHeaders, table);

                SetVariableValue(this.Prop_WrittenRows, written, sd);

                string modeStr = this.Prop_Mode == ExcelWriteMode.Append ? "дописано" : "записано";
                return new ExecutionResult
                {
                    IsSuccess      = true,
                    SuccessMessage = $"[Excel] {modeStr} строк: {written}"
                };
            }
            catch (FileNotFoundException ex)
            {
                return Fail($"Файл не найден: {ex.FileName ?? ex.Message}");
            }
            catch (FormatException ex)
            {
                return Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка записи Excel: {ex.Message}");
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_FilePath, ActivityStrings.Field_ExcelFilePath, ActivityStrings.Error_ExcelFileRequired);
            ret.ValidateRequired(this.Prop_Table, ActivityStrings.Field_ExcelTable, ActivityStrings.Error_ExcelTableRequired);
            return ret;
        }

        private static ExecutionResult Fail(string msg) =>
            new ExecutionResult { IsSuccess = false, ErrorMessage = msg };
    }
}
