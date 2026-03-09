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
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

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
            set { _propList = value; InvokePropertyChanged(this, "Prop_List"); }
        }

        private ListAggregateMode _mode = ListAggregateMode.Count;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_AggregateFunction)]
        public ListAggregateMode Mode
        {
            get => _mode;
            set { _mode = value; InvokePropertyChanged(this, "Mode"); }
        }

        private string _propSeparator;
        /// <summary>Разделитель между элементами (режим Join). По умолчанию ", ".</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Join), System.ComponentModel.DisplayName(ActivityStrings.Field_Separator)]
        public string Prop_Separator
        {
            get => _propSeparator;
            set { _propSeparator = value; InvokePropertyChanged(this, "Prop_Separator"); }
        }

        private string _propStringResult;
        /// <summary>Строковый результат — итог Join или строковых агрегатов</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_StringResult)]
        public string Prop_StringResult
        {
            get => _propStringResult;
            set { _propStringResult = value; InvokePropertyChanged(this, "Prop_StringResult"); }
        }

        private string _propNumericResult;
        /// <summary>Числовой результат — для Count, Sum, Min, Max, Average</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(double))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_NumericResult)]
        public string Prop_NumericResult
        {
            get => _propNumericResult;
            set { _propNumericResult = value; InvokePropertyChanged(this, "Prop_NumericResult"); }
        }

        private string _propNumericCount;
        /// <summary>Количество числовых элементов (для статистических режимов)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_NumericCount)]
        public string Prop_NumericCount
        {
            get => _propNumericCount;
            set { _propNumericCount = value; InvokePropertyChanged(this, "Prop_NumericCount"); }
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
                var list = GetPropertyValue<List<string>>(this.Prop_List, "Prop_List", sd);
                if (list == null) throw new ArgumentNullException("Prop_List", "Список не может быть null");

                string sep = GetPropertyValue<string>(this.Prop_Separator, "Prop_Separator", sd) ?? ", ";

                                // Числа из списка — парсим один раз через LINQ для числовых режимов
                var numbers = list
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => new { Raw = s, Parsed = StringHelper.TryParseDouble(s) })
                    .Where(x => x.Parsed.HasValue)
                    .Select(x => x.Parsed.Value)
                    .ToList();

                double numResult  = 0;
                string strResult  = string.Empty;
                int    numCount   = numbers.Count;

                switch (this.Mode)
                {
                    case ListAggregateMode.Count:
                        numResult = list.Count;
                        break;

                    case ListAggregateMode.CountDistinct:
                        numResult = list
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Count();
                        break;

                    case ListAggregateMode.CountNonEmpty:
                        numResult = list.Count(s => !string.IsNullOrWhiteSpace(s));
                        break;

                    case ListAggregateMode.Sum:
                        numResult = numbers.Any() ? numbers.Sum() : 0;
                        break;

                    case ListAggregateMode.Min:
                        if (!numbers.Any()) throw new InvalidOperationException("Нет числовых элементов для вычисления Min");
                        numResult = numbers.Min();
                        break;

                    case ListAggregateMode.Max:
                        if (!numbers.Any()) throw new InvalidOperationException("Нет числовых элементов для вычисления Max");
                        numResult = numbers.Max();
                        break;

                    case ListAggregateMode.Average:
                        if (!numbers.Any()) throw new InvalidOperationException("Нет числовых элементов для вычисления Average");
                        numResult = numbers.Average();
                        break;

                    case ListAggregateMode.ShortestString:
                        strResult = list
                            .Where(s => s != null)
                            .OrderBy(s => s.Length)
                            .ThenBy(s => s)
                            .FirstOrDefault() ?? string.Empty;
                        break;

                    case ListAggregateMode.LongestString:
                        strResult = list
                            .Where(s => s != null)
                            .OrderByDescending(s => s.Length)
                            .ThenBy(s => s)
                            .FirstOrDefault() ?? string.Empty;
                        break;

                    case ListAggregateMode.Join:
                        strResult = string.Join(sep, list.Where(s => s != null));
                        numResult = list.Count;
                        break;

                    default:
                        throw new InvalidOperationException($"Неизвестный режим: {this.Mode}");
                }

                SetVariableValue(this.Prop_NumericResult, numResult, sd);
                SetVariableValue(this.Prop_StringResult,  strResult, sd);
                SetVariableValue(this.Prop_NumericCount,  numCount,  sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"{this.Mode} = {(string.IsNullOrEmpty(strResult) ? numResult.ToString() : strResult)}" };
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
