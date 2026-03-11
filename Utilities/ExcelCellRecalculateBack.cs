using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Excel: Пересчёт ячейки».
    /// Пересчитывает имя ячейки Excel на основе смещения от начальной ячейки.
    /// Поддерживает формат A1, B2, Z10, AA5 и т.д.
    /// </summary>
    public class ExcelCellRecalculateBack : PrimoComponentTO<ExcelCellRecalculate>
    {
        public override string GroupName { get => ActivityCategories.Utilities; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        private string _propStartCell;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_StartCell)]
        public string Prop_StartCell
        {
            get => _propStartCell;
            set { _propStartCell = value; InvokePropertyChanged(this, "Prop_StartCell"); }
        }

        private string _propRowOffset;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Offset), System.ComponentModel.DisplayName(ActivityStrings.Field_RowOffset)]
        public string Prop_RowOffset
        {
            get => _propRowOffset;
            set { _propRowOffset = value; InvokePropertyChanged(this, "Prop_RowOffset"); }
        }

        private string _propColumnOffset;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Offset), System.ComponentModel.DisplayName(ActivityStrings.Field_ColumnOffset)]
        public string Prop_ColumnOffset
        {
            get => _propColumnOffset;
            set { _propColumnOffset = value; InvokePropertyChanged(this, "Prop_ColumnOffset"); }
        }

        private string _propTargetCell;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_TargetCell)]
        public string Prop_TargetCell
        {
            get => _propTargetCell;
            set { _propTargetCell = value; InvokePropertyChanged(this, "Prop_TargetCell"); }
        }

        public ExcelCellRecalculateBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Excel: Пересчёт ячейки";
            sdkComponentHelp =
                "Пересчитывает имя ячейки Excel на основе смещения от начальной ячейки.\n" +
                "Пример: A1 + (ΔR=2, ΔC=3) → D3\n" +
                "Поддерживает формат A1, B2, Z10, AA5 и т.д.";
            sdkComponentIcon = ActivityIcons.Excel;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<string>("Prop_StartCell", "Имя начальной ячейки (например, A1)"),
                PropertyBuilder.Script<int>("Prop_RowOffset", "Смещение по строкам (ΔR)"),
                PropertyBuilder.Script<int>("Prop_ColumnOffset", "Смещение по столбцам (ΔC)"),
                PropertyBuilder.Variable<string>("Prop_TargetCell", "Вычисленное имя целевой ячейки")
            };

            InitClass(container);
            this.Prop_RowOffset = "0";
            this.Prop_ColumnOffset = "0";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string startCell = GetPropertyValue<string>(this.Prop_StartCell, "Prop_StartCell", sd);
                if (string.IsNullOrWhiteSpace(startCell))
                    throw new ArgumentNullException("Prop_StartCell", "Начальная ячейка обязательна");

                // Парсим начальную ячейку
                var cellInfo = ParseExcelCell(startCell);
                if (!cellInfo.IsValid)
                    throw new ArgumentException($"Неверный формат начальной ячейки: {startCell}");

                // Получаем смещения
                int rowOffset = int.TryParse(this.Prop_RowOffset, out int ro) ? ro : 0;
                int colOffset = int.TryParse(this.Prop_ColumnOffset, out int co) ? co : 0;

                // Вычисляем новые координаты
                int newRow = Math.Max(1, cellInfo.Row + rowOffset); // Минимум 1
                int newCol = Math.Max(1, cellInfo.Column + colOffset); // Минимум 1

                // Формируем новое имя ячейки
                string targetCell = ConvertToExcelColumn(newCol) + newRow.ToString();

                // Устанавливаем результат
                SetVariableValue(this.Prop_TargetCell, targetCell, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Ячейка {startCell} + ({rowOffset}, {colOffset}) = {targetCell}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка пересчёта ячейки: {ex.Message}"
                };
            }
        }

        #region Вспомогательные методы

        private struct CellCoordinates
        {
            public bool IsValid;
            public int Row;
            public int Column;

            public CellCoordinates(bool valid, int row, int col)
            {
                IsValid = valid;
                Row = row;
                Column = col;
            }
        }

        private CellCoordinates ParseExcelCell(string cellName)
        {
            if (string.IsNullOrWhiteSpace(cellName)) return new CellCoordinates(false, 0, 0);

            // Регулярное выражение для парсинга A1, B2, Z10, AA5 и т.д.
            var match = Regex.Match(cellName.Trim(), @"^([A-Z]+)(\d+)$", RegexOptions.IgnoreCase);
            if (!match.Success) return new CellCoordinates(false, 0, 0);

            string colPart = match.Groups[1].Value.ToUpper();
            string rowPart = match.Groups[2].Value;

            int row;
            if (!int.TryParse(rowPart, out row) || row < 1) return new CellCoordinates(false, 0, 0);

            int column = ConvertFromExcelColumn(colPart);

            return new CellCoordinates(true, row, column);
        }

        private int ConvertFromExcelColumn(string columnName)
        {
            int result = 0;
            foreach (char c in columnName)
            {
                result *= 26;
                result += c - 'A' + 1;
            }
            return result;
        }

        private string ConvertToExcelColumn(int columnNumber)
        {
            if (columnNumber <= 0) return string.Empty;

            string result = string.Empty;
            while (columnNumber > 0)
            {
                columnNumber--; // Сдвигаем на 1, потому что A=1, а не A=0
                result = (char)('A' + (columnNumber % 26)) + result;
                columnNumber /= 26;
            }
            return result;
        }

        #endregion

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_StartCell, ActivityStrings.Field_StartCell, ActivityStrings.Error_StartCellRequired);
            return ret;
        }
    }

    // Вспомогательный класс для режимов, если понадобятся в будущем
    public enum ExcelCellRecalculationMode
    {
        Relative = 0,
        Absolute = 1
    }
}