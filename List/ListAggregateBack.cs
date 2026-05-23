// =============================================================================
// ListSetAggregateSlice.cs — три активности:
//
//   ListSetBack       — «Список: Теория множеств»
//     Union, Intersect, Except, Distinct, SymmetricDiff
//
//   ListAggregateBack — «Список: Агрегация»
//     Count, CountDistinct, Sum, Min, Max, Average, Join (конкатенация строк)
//
//   ListSliceBack     — «Список: Срез»
//     Take, Skip, Page (пагинация), Range (от..до), First/Last N
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Список: Агрегация».
    /// Вычисляет одно агрегатное значение из всего списка.
    /// Числовые режимы игнорируют нечисловые строки.
    /// </summary>
    public class ListAggregateBack : PrimoComponentTO<ListAggregate>
    {
        public override string GroupName { get => ActivityCategories.Lists; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        private string _propList;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_List)]
        public string Prop_List
        {
            get => _propList;
            set { _propList = value; InvokePropertyChanged(this, nameof(Prop_List)); }
        }

        private ListAggregateMode _mode = ListAggregateMode.Count;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_AggregateFunction)]
        public ListAggregateMode Mode
        {
            get => _mode;
            set { _mode = value; InvokePropertyChanged(this, nameof(Mode)); }
        }

        private string _propSeparator;
        /// <summary>Разделитель между элементами (режим Join). По умолчанию ", ".</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Join), System.ComponentModel.DisplayName(ActivityStrings.Field_Separator)]
        public string Prop_Separator
        {
            get => _propSeparator;
            set { _propSeparator = value; InvokePropertyChanged(this, nameof(Prop_Separator)); }
        }

        private string _propStringResult;
        /// <summary>Строковый результат — итог Join или строковых агрегатов</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_StringResult)]
        public string Prop_StringResult
        {
            get => _propStringResult;
            set { _propStringResult = value; InvokePropertyChanged(this, nameof(Prop_StringResult)); }
        }

        private string _propNumericResult;
        /// <summary>Числовой результат — для Count, Sum, Min, Max, Average</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(double))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_NumericResult)]
        public string Prop_NumericResult
        {
            get => _propNumericResult;
            set { _propNumericResult = value; InvokePropertyChanged(this, nameof(Prop_NumericResult)); }
        }

        private string _propNumericCount;
        /// <summary>Количество числовых элементов (для статистических режимов)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_NumericCount)]
        public string Prop_NumericCount
        {
            get => _propNumericCount;
            set { _propNumericCount = value; InvokePropertyChanged(this, nameof(Prop_NumericCount)); }
        }

        public ListAggregateBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Список: Агрегация";
            sdkComponentHelp =
                "Вычисляет агрегатное значение из List<string>.\n\n" +
                "Count          — кол-во всех элементов\n" +
                "CountDistinct  — кол-во уникальных непустых\n" +
                "CountNonEmpty  — кол-во непустых\n" +
                "Sum/Min/Max/Average — числовые агрегаты (нечисловые игнорируются)\n" +
                "ShortestString — кратчайшая строка\n" +
                "LongestString  — длиннейшая строка\n" +
                "Join           — склеить все через разделитель";
            sdkComponentIcon = ActivityIcons.List;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<List<string>>("Prop_List", "Входной список"),
                PropertyBuilder.Enum<ListAggregateMode>("Mode", "Агрегатная функция"),
                PropertyBuilder.Script<string>("Prop_Separator", "Разделитель для Join (по умолч. \", \")"),
                PropertyBuilder.Variable<string>("Prop_StringResult", "Строковый результат (Join, ShortestString, LongestString)"),
                PropertyBuilder.Variable<double>("Prop_NumericResult", "Числовой результат (Count, Sum, Min, Max, Average)"),
                PropertyBuilder.Variable<int>("Prop_NumericCount", "Кол-во числовых элементов")
            };

            InitClass(container);
            this.Prop_Separator = "\", \"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var logic = new ListAggregateLogic();
                var list = GetPropertyValue<List<string>>(this.Prop_List, "Prop_List", sd);
                if (list == null) throw new ArgumentNullException("Prop_List", "Список не может быть null");

                string sep = GetPropertyValue<string>(this.Prop_Separator, "Prop_Separator", sd) ?? ", ";
                var result = logic.AggregateDetailed(list, this.Mode, sep);

                SetVariableValue(this.Prop_NumericResult, result.NumericResult, sd);
                SetVariableValue(this.Prop_StringResult, result.StringResult, sd);
                SetVariableValue(this.Prop_NumericCount, result.NumericCount, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"{this.Mode} = {(string.IsNullOrEmpty(result.StringResult) ? result.NumericResult.ToString() : result.StringResult)}" };
            }
            catch (ArgumentException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Неверный аргумент: {ex.Message}" };
            }
            catch (InvalidOperationException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Недопустимая операция: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка агрегации: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_List, ActivityStrings.Field_List, ActivityStrings.Error_ListRequired);
            return ret;
        }
    }
}
