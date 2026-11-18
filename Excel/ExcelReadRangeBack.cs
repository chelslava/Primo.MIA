// =============================================================================
// ExcelReadRangeBack.cs — активность «Excel: Прочитать диапазон» (issue #47).
// Читает .xlsx в DataTable без Interop (ClosedXML).
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
using System.Linq;

namespace Primo.MIA
{
    /// <summary>Активность чтения диапазона из .xlsx в DataTable.</summary>
    public class ExcelReadRangeBack : PrimoComponentTO<ExcelReadRange>
    {
        public override string GroupName { get => ActivityCategories.Excel; protected set { } }

        protected override int sdkTimeOut { get => 60000; set { } }

        #region Входные параметры

        private string _propFilePath;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelFilePath)]
        /// <summary>Путь к файлу .xlsx.</summary>
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
        /// <summary>Имя листа; пусто — первый лист книги.</summary>
        public string Prop_SheetName
        {
            get => _propSheetName;
            set { _propSheetName = value; InvokePropertyChanged(this, nameof(Prop_SheetName)); }
        }

        private string _propRange;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelRange)]
        /// <summary>Диапазон «A1:F100»; пусто — используемый диапазон листа.</summary>
        public string Prop_Range
        {
            get => _propRange;
            set { _propRange = value; InvokePropertyChanged(this, nameof(Prop_Range)); }
        }

        private bool _propHeaderRow = true;
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelHeaderRow)]
        /// <summary>Первая строка диапазона — имена колонок DataTable.</summary>
        public bool Prop_HeaderRow
        {
            get => _propHeaderRow;
            set { _propHeaderRow = value; InvokePropertyChanged(this, nameof(Prop_HeaderRow)); }
        }

        #endregion

        #region Выходные параметры

        private string _propResultTable;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelResultTable)]
        /// <summary>Имя переменной (DataTable) для результата.</summary>
        public string Prop_ResultTable
        {
            get => _propResultTable;
            set { _propResultTable = value; InvokePropertyChanged(this, nameof(Prop_ResultTable)); }
        }

        private string _propRowCount;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelRowCount)]
        /// <summary>Имя переменной (Int32) для числа прочитанных строк.</summary>
        public string Prop_RowCount
        {
            get => _propRowCount;
            set { _propRowCount = value; InvokePropertyChanged(this, nameof(Prop_RowCount)); }
        }

        private string _propColumnNames;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelColumnNames)]
        /// <summary>Имя переменной (List&lt;string&gt;) для имён колонок.</summary>
        public string Prop_ColumnNames
        {
            get => _propColumnNames;
            set { _propColumnNames = value; InvokePropertyChanged(this, nameof(Prop_ColumnNames)); }
        }

        #endregion

        public ExcelReadRangeBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ExcelReadRange;
            sdkComponentHelp = @"Компонент ""Excel: Прочитать диапазон""
Читает .xlsx в DataTable без Interop (не требует установленного Excel).

Основные:
Файл*: [String] Путь к файлу .xlsx.
Лист: [String] Имя листа; пусто — первый лист.
Диапазон: [String] Например A1:F100; пусто — используемый диапазон.
Первая строка — заголовки: [Boolean] Имена колонок из первой строки.

Выходные:
Результат (DataTable)*, Кол-во строк, Имена колонок.";
            sdkComponentIcon = ActivityIcons.Excel;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.FileSelector("Prop_FilePath", "Путь к файлу .xlsx"),
                PropertyBuilder.String("Prop_SheetName", "Имя листа; пусто — первый лист"),
                PropertyBuilder.String("Prop_Range", "Диапазон A1:F100; пусто — используемый диапазон"),
                PropertyBuilder.BooleanObject("Prop_HeaderRow", "Первая строка диапазона — имена колонок"),
                PropertyBuilder.Variable<DataTable>("Prop_ResultTable", "Переменная (DataTable) для результата"),
                PropertyBuilder.Variable<int>("Prop_RowCount", "Переменная (Int32) для числа строк"),
                PropertyBuilder.Variable<List<string>>("Prop_ColumnNames", "Переменная (List<string>) для имён колонок"),
            };

            InitClass(container);

            this.Prop_FilePath  = "\"\"";
            this.Prop_SheetName = "\"\"";
            this.Prop_Range     = "\"\"";
            this.Prop_HeaderRow = true;
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string filePath = GetPropertyValue<string>(this.Prop_FilePath, nameof(Prop_FilePath), sd);
                string sheetName = GetPropertyValue<string>(this.Prop_SheetName, nameof(Prop_SheetName), sd);
                string range = GetPropertyValue<string>(this.Prop_Range, nameof(Prop_Range), sd);

                var table = ExcelLogic.ReadRange(filePath, sheetName, range, this.Prop_HeaderRow);

                SetVariableValue(this.Prop_ResultTable, table, sd);
                SetVariableValue(this.Prop_RowCount, table.Rows.Count, sd);
                SetVariableValue(this.Prop_ColumnNames,
                    table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList(), sd);

                return new ExecutionResult
                {
                    IsSuccess      = true,
                    SuccessMessage = $"[Excel] Прочитано строк: {table.Rows.Count}, колонок: {table.Columns.Count}"
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
            catch (InvalidOperationException ex)
            {
                return Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка чтения Excel: {ex.Message}");
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_FilePath, ActivityStrings.Field_ExcelFilePath, ActivityStrings.Error_ExcelFileRequired);
            return ret;
        }

        private static ExecutionResult Fail(string msg) =>
            new ExecutionResult { IsSuccess = false, ErrorMessage = msg };
    }
}
