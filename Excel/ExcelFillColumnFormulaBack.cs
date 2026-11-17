// =============================================================================
// ExcelFillColumnFormulaBack.cs — активность «Excel: Формула на столбец»
// (issue #47). Заполняет столбец формулой по шаблону с подстановкой {row}.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace Primo.MIA
{
    /// <summary>Активность заполнения столбца формулой.</summary>
    public class ExcelFillColumnFormulaBack : PrimoComponentTO<ExcelFillColumnFormula>
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

        private string _propColumnLetter;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelColumnLetter)]
        /// <summary>Буква столбца (A…XFD), например C.</summary>
        public string Prop_ColumnLetter
        {
            get => _propColumnLetter;
            set { _propColumnLetter = value; InvokePropertyChanged(this, nameof(Prop_ColumnLetter)); }
        }

        private string _propFormula;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelFormula)]
        /// <summary>Шаблон формулы с {row}: «=A{row}*B{row}».</summary>
        public string Prop_Formula
        {
            get => _propFormula;
            set { _propFormula = value; InvokePropertyChanged(this, nameof(Prop_Formula)); }
        }

        private string _propStartRow;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelStartRow)]
        /// <summary>Первая строка диапазона (по умолчанию 2 — после заголовка).</summary>
        public string Prop_StartRow
        {
            get => _propStartRow;
            set { _propStartRow = value; InvokePropertyChanged(this, nameof(Prop_StartRow)); }
        }

        private string _propEndRow;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelEndRow)]
        /// <summary>Последняя строка диапазона (включительно).</summary>
        public string Prop_EndRow
        {
            get => _propEndRow;
            set { _propEndRow = value; InvokePropertyChanged(this, nameof(Prop_EndRow)); }
        }

        #endregion

        #region Выходные параметры

        private string _propFilledCells;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelFilledCells)]
        /// <summary>Имя переменной (Int32) для числа заполненных ячеек.</summary>
        public string Prop_FilledCells
        {
            get => _propFilledCells;
            set { _propFilledCells = value; InvokePropertyChanged(this, nameof(Prop_FilledCells)); }
        }

        #endregion

        public ExcelFillColumnFormulaBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ExcelFillFormula;
            sdkComponentHelp = @"Компонент ""Excel: Формула на столбец""
Заполняет столбец формулой по шаблону (без Interop, Excel не требуется).

Основные:
Файл*: [String] Путь к файлу .xlsx.
Лист: [String] Имя листа; пусто — первый лист.

Параметры:
Столбец*: [String] Буква столбца (например C).
Формула*: [String] Шаблон с {row}: ""=A{row}*B{row}""; без {row} — одинаковая во всех ячейках.
Первая строка: [Int32] По умолчанию 2 (после заголовка).
Последняя строка: [Int32] Включительно.

Выходные:
Заполнено ячеек [Int32].";
            sdkComponentIcon = ActivityIcons.Excel;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_FilePath", "Путь к файлу .xlsx"),
                PropertyBuilder.String("Prop_SheetName", "Имя листа; пусто — первый лист"),
                PropertyBuilder.String("Prop_ColumnLetter", "Буква столбца (A…XFD), например C"),
                PropertyBuilder.String("Prop_Formula", "Шаблон формулы с {row}: =A{row}*B{row}"),
                PropertyBuilder.Int("Prop_StartRow", "Первая строка (по умолчанию 2)"),
                PropertyBuilder.Int("Prop_EndRow", "Последняя строка (включительно)"),
                PropertyBuilder.Variable<int>("Prop_FilledCells", "Переменная (Int32) для числа заполненных ячеек"),
            };

            InitClass(container);

            this.Prop_FilePath     = "\"\"";
            this.Prop_SheetName    = "\"\"";
            this.Prop_ColumnLetter = "\"\"";
            this.Prop_Formula      = "\"\"";
            this.Prop_StartRow     = "2";
            this.Prop_EndRow       = "\"\"";
            this.Prop_FilledCells  = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string filePath = GetPropertyValue<string>(this.Prop_FilePath, nameof(Prop_FilePath), sd);
                string sheetName = GetPropertyValue<string>(this.Prop_SheetName, nameof(Prop_SheetName), sd);
                string columnLetter = GetPropertyValue<string>(this.Prop_ColumnLetter, nameof(Prop_ColumnLetter), sd);
                string formula = GetPropertyValue<string>(this.Prop_Formula, nameof(Prop_Formula), sd);

                if (string.IsNullOrWhiteSpace(columnLetter))
                    return Fail($"{ActivityStrings.Error_ExcelStartCellInvalid}: укажите букву столбца");
                if (string.IsNullOrWhiteSpace(formula))
                    return Fail(ActivityStrings.Error_ExcelFormulaRequired);

                int startRow = ParseInt(GetPropertyValue<string>(this.Prop_StartRow, nameof(Prop_StartRow), sd), 2);
                int endRow = ParseInt(GetPropertyValue<string>(this.Prop_EndRow, nameof(Prop_EndRow), sd), -1);
                if (endRow < 0)
                    return Fail("Укажите последнюю строку (включительно)");

                int filled = ExcelLogic.FillColumnFormula(
                    filePath, sheetName, columnLetter, formula, startRow, endRow);

                SetVariableValue(this.Prop_FilledCells, filled, sd);

                return new ExecutionResult
                {
                    IsSuccess      = true,
                    SuccessMessage = $"[Excel] Заполнено ячеек: {filled}"
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
            catch (ArgumentOutOfRangeException ex)
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
            ret.ValidateRequired(this.Prop_ColumnLetter, ActivityStrings.Field_ExcelColumnLetter, ActivityStrings.Error_ExcelStartCellInvalid);
            ret.ValidateRequired(this.Prop_Formula, ActivityStrings.Field_ExcelFormula, ActivityStrings.Error_ExcelFormulaRequired);
            ret.ValidateRequired(this.Prop_EndRow, ActivityStrings.Field_ExcelEndRow, "Укажите последнюю строку");
            return ret;
        }

        private static int ParseInt(string raw, int fallback)
        {
            return StringHelper.TryParseInt(raw) ?? fallback;
        }

        private static ExecutionResult Fail(string msg) =>
            new ExecutionResult { IsSuccess = false, ErrorMessage = msg };
    }
}
