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
    /// Активность «Список: Группировка».
    /// Группирует List&lt;string&gt; по выбранному ключу.
    /// Возвращает Dictionary&lt;string, List&lt;string&gt;&gt;.
    /// </summary>
    public class ListGroupBack : PrimoComponentTO<ListGroup>
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

        private ListGroupMode _mode = ListGroupMode.ByFirstChar;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_GroupingMode)]
        public ListGroupMode Mode
        {
            get => _mode;
            set { _mode = value; InvokePropertyChanged(this, "Mode"); }
        }

        private string _propPrefixLength;
        /// <summary>Длина префикса для режима ByPrefix</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_PrefixLength)]
        public string Prop_PrefixLength
        {
            get => _propPrefixLength;
            set { _propPrefixLength = value; InvokePropertyChanged(this, "Prop_PrefixLength"); }
        }

        private string _propRegexPattern;
        /// <summary>Regex с capture-группой для режима ByRegexGroup. Группа 1 → ключ.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_RegexPattern)]
        public string Prop_RegexPattern
        {
            get => _propRegexPattern;
            set { _propRegexPattern = value; InvokePropertyChanged(this, "Prop_RegexPattern"); }
        }

        private string _propTopN;
        /// <summary>Количество топ-элементов (режим TopFrequent)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_TopN)]
        public string Prop_TopN
        {
            get => _propTopN;
            set { _propTopN = value; InvokePropertyChanged(this, "Prop_TopN"); }
        }

        private string _propGroupedResult;
        /// <summary>Словарь: ключ группы → список элементов</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, List<string>>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Groups)]
        public string Prop_GroupedResult
        {
            get => _propGroupedResult;
            set { _propGroupedResult = value; InvokePropertyChanged(this, "Prop_GroupedResult"); }
        }

        private string _propGroupCount;
        /// <summary>Количество групп в результате</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_GroupCount)]
        public string Prop_GroupCount
        {
            get => _propGroupCount;
            set { _propGroupCount = value; InvokePropertyChanged(this, "Prop_GroupCount"); }
        }

        private string _propFrequencyMap;
        /// <summary>Словарь: элемент → количество вхождений (заполняется всегда)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, int>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_FrequencyMap)]
        public string Prop_FrequencyMap
        {
            get => _propFrequencyMap;
            set { _propFrequencyMap = value; InvokePropertyChanged(this, "Prop_FrequencyMap"); }
        }

        public ListGroupBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Список: Группировка";
            sdkComponentHelp =
                "Группирует List<string> по выбранному ключу.\n" +
                "Результат: Dictionary<string, List<string>>\n\n" +
                "ByFirstChar  — по первому символу (A, B, ...)\n" +
                "ByLength     — по длине строки (1, 2, 3, ...)\n" +
                "ByPrefix     — по первым N символам\n" +
                "ByRegexGroup — по первой capture-группе regex\n" +
                "TopFrequent  — топ-N самых частых значений";
            sdkComponentIcon = ActivityIcons.List;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<List<string>>("Prop_List", "Входной список"),
                PropertyBuilder.Enum<ListGroupMode>("Mode", "Способ группировки"),
                PropertyBuilder.Script<int>("Prop_PrefixLength", "Длина префикса (ByPrefix)"),
                PropertyBuilder.Script<string>("Prop_RegexPattern", "Regex с capture-группой (ByRegexGroup)"),
                PropertyBuilder.Script<int>("Prop_TopN", "Топ N элементов (TopFrequent)"),
                PropertyBuilder.Variable<Dictionary<string, List<string>>>("Prop_GroupedResult", "Словарь группы → элементы"),
                PropertyBuilder.Variable<int>("Prop_GroupCount", "Количество групп"),
                PropertyBuilder.Variable<Dictionary<string, int>>("Prop_FrequencyMap", "Словарь элемент → частота")
            };

            InitClass(container);
            this.Prop_PrefixLength = "3";
            this.Prop_TopN = "10";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var logic = new ListGroupLogic();
                var list = GetPropertyValue<List<string>>(this.Prop_List, "Prop_List", sd);
                if (list == null) throw new ArgumentNullException("Prop_List", "Список не может быть null");

                int prefixLen = int.TryParse(GetPropertyValue<string>(this.Prop_PrefixLength, "Prop_PrefixLength", sd), out int pl) ? pl : 3;
                int topN = int.TryParse(GetPropertyValue<string>(this.Prop_TopN, "Prop_TopN", sd), out int tn) ? tn : 10;
                string regex = GetPropertyValue<string>(this.Prop_RegexPattern, "Prop_RegexPattern", sd) ?? string.Empty;

                // Словарь частот нужен для всех режимов — вычисляем один раз
                var freqMap = list
                    .Where(s => s != null)
                    .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

                var grouped = logic.Group(list, this.Mode, prefixLen, regex, topN);

                SetVariableValue(this.Prop_GroupedResult, grouped, sd);
                SetVariableValue(this.Prop_GroupCount, grouped.Count, sd);
                SetVariableValue(this.Prop_FrequencyMap, freqMap, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Сгруппировано в {grouped.Count} групп" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка группировки: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_List, ActivityStrings.Field_List, ActivityStrings.Error_ListRequired);
            if (this.Mode == ListGroupMode.ByRegexGroup)
                ret.ValidateRequired(this.Prop_RegexPattern, ActivityStrings.Field_RegexPattern, ActivityStrings.Error_RegexPatternRequired);
            return ret;
        }
    }

}
