using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

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
                var logic = new ExcelCellLogic();
                string startCell = GetPropertyValue<string>(this.Prop_StartCell, "Prop_StartCell", sd);
                if (string.IsNullOrWhiteSpace(startCell))
                    throw new ArgumentNullException("Prop_StartCell", "Начальная ячейка обязательна");

                // Получаем смещения
                int rowOffset = int.TryParse(GetPropertyValue<string>(this.Prop_RowOffset, "Prop_RowOffset", sd), out int ro) ? ro : 0;
                int colOffset = int.TryParse(GetPropertyValue<string>(this.Prop_ColumnOffset, "Prop_ColumnOffset", sd), out int co) ? co : 0;

                string targetCell = logic.RecalculateCell(startCell, rowOffset, colOffset);

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
