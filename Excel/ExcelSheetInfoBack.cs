// =============================================================================
// ExcelSheetInfoBack.cs — активность «Excel: Сведения о книге» (issue #47).
// Список листов книги без Interop (ClosedXML).
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
    /// <summary>Активность чтения сведений о книге Excel.</summary>
    public class ExcelSheetInfoBack : PrimoComponentTO<ExcelSheetInfo>
    {
        public override string GroupName { get => ActivityCategories.Excel; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

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

        private string _propSheetNames;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelSheetNames)]
        /// <summary>Имя переменной (List&lt;string&gt;) для списка листов.</summary>
        public string Prop_SheetNames
        {
            get => _propSheetNames;
            set { _propSheetNames = value; InvokePropertyChanged(this, nameof(Prop_SheetNames)); }
        }

        private string _propSheetCount;
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ExcelSheetCount)]
        /// <summary>Имя переменной (Int32) для числа листов.</summary>
        public string Prop_SheetCount
        {
            get => _propSheetCount;
            set { _propSheetCount = value; InvokePropertyChanged(this, nameof(Prop_SheetCount)); }
        }

        public ExcelSheetInfoBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ExcelSheetInfo;
            sdkComponentHelp = @"Компонент ""Excel: Сведения о книге""
Возвращает список листов файла .xlsx (без Interop, Excel не требуется).

Основные:
Файл*: [String] Путь к файлу .xlsx.

Выходные:
Листы* [List<string>], Кол-во листов [Int32].";
            sdkComponentIcon = ActivityIcons.Excel;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.FileSelector("Prop_FilePath", "Путь к файлу .xlsx"),
                PropertyBuilder.Variable<List<string>>("Prop_SheetNames", "Переменная (List<string>) для списка листов"),
                PropertyBuilder.Variable<int>("Prop_SheetCount", "Переменная (Int32) для числа листов"),
            };

            InitClass(container);

            this.Prop_FilePath = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string filePath = GetPropertyValue<string>(this.Prop_FilePath, nameof(Prop_FilePath), sd);

                var sheets = ExcelLogic.GetSheetNames(filePath);

                SetVariableValue(this.Prop_SheetNames, sheets, sd);
                SetVariableValue(this.Prop_SheetCount, sheets.Count, sd);

                return new ExecutionResult
                {
                    IsSuccess      = true,
                    SuccessMessage = $"[Excel] Листов: {sheets.Count} ({string.Join(", ", sheets)})"
                };
            }
            catch (FileNotFoundException ex)
            {
                return Fail($"Файл не найден: {ex.FileName ?? ex.Message}");
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
