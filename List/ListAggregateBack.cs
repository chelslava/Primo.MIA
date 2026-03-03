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
        private const string CGroupName = "MIA" + WFPublishedElementBase.TREE_SEPARATOR + "Списки";
        public override string GroupName 
        { 
            get => CGroupName;
            protected set { } 
        }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        private string _propList;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Список")]
        public string Prop_List
        {
            get => _propList;
            set { _propList = value; InvokePropertyChanged(this, "Prop_List"); }
        }

        private ListAggregateMode _mode = ListAggregateMode.Count;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Агрегатная функция")]
        public ListAggregateMode Mode
        {
            get => _mode;
            set { _mode = value; InvokePropertyChanged(this, "Mode"); }
        }

        private string _propSeparator;
        /// <summary>Разделитель между элементами (режим Join). По умолчанию ", ".</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Join"), System.ComponentModel.DisplayName("Разделитель")]
        public string Prop_Separator
        {
            get => _propSeparator;
            set { _propSeparator = value; InvokePropertyChanged(this, "Prop_Separator"); }
        }

        private string _propStringResult;
        /// <summary>Строковый результат — итог Join или строковых агрегатов</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Строковый результат")]
        public string Prop_StringResult
        {
            get => _propStringResult;
            set { _propStringResult = value; InvokePropertyChanged(this, "Prop_StringResult"); }
        }

        private string _propNumericResult;
        /// <summary>Числовой результат — для Count, Sum, Min, Max, Average</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(double))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Числовой результат")]
        public string Prop_NumericResult
        {
            get => _propNumericResult;
            set { _propNumericResult = value; InvokePropertyChanged(this, "Prop_NumericResult"); }
        }

        private string _propNumericCount;
        /// <summary>Количество числовых элементов (для статистических режимов)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Кол-во числовых")]
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
            sdkComponentIcon = "pack://application:,,,/Primo.MIA;component/images/list.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem() 
                { 
                    PropName = "Prop_List",
                    PropertyType = PropertyTypes.SCRIPT, 
                    EditorType = ScriptEditorTypes.NONE, 
                    DataType = typeof(List<string>), 
                    ToolTip = "Входной список", 
                    IsReadOnly = false 
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() 
                {
                    PropName = "Mode", 
                    PropertyType = PropertyTypes.OBJECT,
                    EditorType = ScriptEditorTypes.NONE, 
                    DataType = typeof(ListAggregateMode), 
                    ToolTip = "Агрегатная функция", 
                    IsReadOnly = false 
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() 
                {
                    PropName = "Prop_Separator", 
                    PropertyType = PropertyTypes.SCRIPT, 
                    EditorType = ScriptEditorTypes.NONE, 
                    DataType = typeof(string), 
                    ToolTip = "Разделитель для Join (по умолч. \", \")",
                    IsReadOnly = false 
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() 
                {
                    PropName = "Prop_StringResult",
                    PropertyType = PropertyTypes.VARIABLE, 
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(string), 
                    ToolTip = "Строковый результат (Join, ShortestString, LongestString)", 
                    IsReadOnly = false 
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() 
                {
                    PropName = "Prop_NumericResult",
                    PropertyType = PropertyTypes.VARIABLE, 
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(double), 
                    ToolTip = "Числовой результат (Count, Sum, Min, Max, Average)", 
                    IsReadOnly = false 
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() 
                {
                    PropName = "Prop_NumericCount",
                    PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(int), 
                    ToolTip = "Кол-во числовых элементов", 
                    IsReadOnly = false 
                }
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
                    .Select(s => new { Raw = s, Parsed = TryParseDouble(s) })
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

        /// <summary>Безопасный парсинг double с поддержкой точки и запятой как разделителя</summary>
        private static double? TryParseDouble(string s)
        {
            if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double d))
                return d;
            if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double d2))
                return d2;
            return null;
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            if (string.IsNullOrWhiteSpace(this.Prop_List))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Список", Error = "Список обязателен" });
            return ret;
        }
    }
}
