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
    /// Активность «Список: Анализ».
    /// Полная диагностика List&lt;string&gt;: дубликаты, статистика длин,
    /// поиск элементов, частотность, разброс значений.
    /// Не изменяет список.
    /// </summary>
    public class ListInspectBack : PrimoComponentTO<ListInspect>
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

        private string _propSearchValue;
        /// <summary>Значение для поиска в списке (для определения индексов вхождений)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Search), System.ComponentModel.DisplayName(ActivityStrings.Field_SearchValue)]
        public string Prop_SearchValue
        {
            get => _propSearchValue;
            set { _propSearchValue = value; InvokePropertyChanged(this, "Prop_SearchValue"); }
        }

        private bool _caseSensitive = false;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Search), System.ComponentModel.DisplayName(ActivityStrings.Field_CaseSensitive)]
        public bool Prop_CaseSensitive
        {
            get => _caseSensitive;
            set { _caseSensitive = value; InvokePropertyChanged(this, "Prop_CaseSensitive"); }
        }

        // — OUTPUT: базовая статистика —

        private string _propTotalCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Statistics), System.ComponentModel.DisplayName(ActivityStrings.Field_TotalCount)]
        public string Prop_TotalCount { get => _propTotalCount; set { _propTotalCount = value; InvokePropertyChanged(this, "Prop_TotalCount"); } }

        private string _propUniqueCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Statistics), System.ComponentModel.DisplayName(ActivityStrings.Field_UniqueCount)]
        public string Prop_UniqueCount { get => _propUniqueCount; set { _propUniqueCount = value; InvokePropertyChanged(this, "Prop_UniqueCount"); } }

        private string _propEmptyCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Statistics), System.ComponentModel.DisplayName(ActivityStrings.Field_EmptyCount)]
        public string Prop_EmptyCount { get => _propEmptyCount; set { _propEmptyCount = value; InvokePropertyChanged(this, "Prop_EmptyCount"); } }

        private string _propHasDuplicates;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Statistics), System.ComponentModel.DisplayName(ActivityStrings.Field_HasDuplicates)]
        public string Prop_HasDuplicates { get => _propHasDuplicates; set { _propHasDuplicates = value; InvokePropertyChanged(this, "Prop_HasDuplicates"); } }

        private string _propDuplicates;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Statistics), System.ComponentModel.DisplayName(ActivityStrings.Field_Duplicates)]
        public string Prop_Duplicates { get => _propDuplicates; set { _propDuplicates = value; InvokePropertyChanged(this, "Prop_Duplicates"); } }

        // — OUTPUT: статистика длин —

        private string _propMinLength;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Lengths), System.ComponentModel.DisplayName(ActivityStrings.Field_MinLength)]
        public string Prop_MinLength { get => _propMinLength; set { _propMinLength = value; InvokePropertyChanged(this, "Prop_MinLength"); } }

        private string _propMaxLength;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Lengths), System.ComponentModel.DisplayName(ActivityStrings.Field_MaxLength)]
        public string Prop_MaxLength { get => _propMaxLength; set { _propMaxLength = value; InvokePropertyChanged(this, "Prop_MaxLength"); } }

        private string _propAvgLength;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(double))]
        [System.ComponentModel.Category(ActivityStrings.Category_Lengths), System.ComponentModel.DisplayName(ActivityStrings.Field_AvgLength)]
        public string Prop_AvgLength { get => _propAvgLength; set { _propAvgLength = value; InvokePropertyChanged(this, "Prop_AvgLength"); } }

        // — OUTPUT: поиск —

        private string _propContains;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Search), System.ComponentModel.DisplayName(ActivityStrings.Field_Contains)]
        public string Prop_Contains { get => _propContains; set { _propContains = value; InvokePropertyChanged(this, "Prop_Contains"); } }

        private string _propFirstIndex;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Search), System.ComponentModel.DisplayName(ActivityStrings.Field_FirstIndex)]
        public string Prop_FirstIndex { get => _propFirstIndex; set { _propFirstIndex = value; InvokePropertyChanged(this, "Prop_FirstIndex"); } }

        private string _propAllIndexes;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<int>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Search), System.ComponentModel.DisplayName(ActivityStrings.Field_AllIndexes)]
        public string Prop_AllIndexes { get => _propAllIndexes; set { _propAllIndexes = value; InvokePropertyChanged(this, "Prop_AllIndexes"); } }

        private string _propOccurrenceCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Search), System.ComponentModel.DisplayName(ActivityStrings.Field_OccurrenceCount)]
        public string Prop_OccurrenceCount { get => _propOccurrenceCount; set { _propOccurrenceCount = value; InvokePropertyChanged(this, "Prop_OccurrenceCount"); } }

        public ListInspectBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Список: Анализ";
            sdkComponentHelp =
                "Полная диагностика List<string>.\n" +
                "Не изменяет список.\n\n" +
                "── Статистика ─────────────────────────────────────────\n" +
                "Всего элементов / Уникальных / Пустых\n" +
                "Есть дубликаты + список дублирующихся значений\n\n" +
                "── Длины строк ─────────────────────────────────────────\n" +
                "Мин. / Макс. / Средняя длина\n\n" +
                "── Поиск элемента ──────────────────────────────────────\n" +
                "Задайте Искомое значение → получите:\n" +
                "  Найдено (bool)\n" +
                "  Первый индекс (-1 если не найдено)\n" +
                "  Все индексы вхождений\n" +
                "  Количество вхождений";
            sdkComponentIcon = ActivityIcons.List;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<List<string>>("Prop_List", "Входной список"),
                PropertyBuilder.Script<string>("Prop_SearchValue", "Значение для поиска (необязательно)"),
                PropertyBuilder.BooleanObject("Prop_CaseSensitive", "Учитывать регистр при поиске"),
                PropertyBuilder.Variable<int>("Prop_TotalCount", "Всего элементов"),
                PropertyBuilder.Variable<int>("Prop_UniqueCount", "Уникальных элементов"),
                PropertyBuilder.Variable<int>("Prop_EmptyCount", "Пустых элементов"),
                PropertyBuilder.Variable<bool>("Prop_HasDuplicates", "Есть ли дублирующиеся значения"),
                PropertyBuilder.Variable<List<string>>("Prop_Duplicates", "Список дублирующихся значений"),
                PropertyBuilder.Variable<int>("Prop_MinLength", "Минимальная длина строки"),
                PropertyBuilder.Variable<int>("Prop_MaxLength", "Максимальная длина строки"),
                PropertyBuilder.Variable<double>("Prop_AvgLength", "Средняя длина строки"),
                PropertyBuilder.Variable<bool>("Prop_Contains", "Найден ли искомый элемент"),
                PropertyBuilder.Variable<int>("Prop_FirstIndex", "Первый индекс вхождения (-1 если не найден)"),
                PropertyBuilder.Variable<List<int>>("Prop_AllIndexes", "Все индексы вхождений искомого значения"),
                PropertyBuilder.Variable<int>("Prop_OccurrenceCount", "Количество вхождений искомого значения")
            };

            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var list = GetPropertyValue<List<string>>(this.Prop_List, "Prop_List", sd);
                if (list == null) throw new ArgumentNullException("Prop_List", "Список не может быть null");

                string searchVal = GetPropertyValue<string>(this.Prop_SearchValue, "Prop_SearchValue", sd);

                StringComparison sc = this.Prop_CaseSensitive
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

                // ── Базовая статистика ──────────────────────────────────────────

                int totalCount = list.Count;
                int emptyCount = list.Count(s => string.IsNullOrWhiteSpace(s));
                StringComparer comparer = this.Prop_CaseSensitive
                    ? StringComparer.Ordinal
                    : StringComparer.OrdinalIgnoreCase;

                int uniqueCount = list
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(comparer)
                    .Count();

                // Дубликаты: элементы встречающиеся более одного раза
                var duplicates = list
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .GroupBy(s => s, comparer)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .OrderBy(s => s)
                    .ToList();

                // ── Статистика длин ─────────────────────────────────────────────

                var lengths = list.Select(s => s?.Length ?? 0).ToList();
                int minLen = lengths.Any() ? lengths.Min() : 0;
                int maxLen = lengths.Any() ? lengths.Max() : 0;
                double avgLen = lengths.Any() ? lengths.Average() : 0.0;

                // ── Поиск элемента ──────────────────────────────────────────────

                bool contains = false;
                int firstIndex = -1;
                var allIndexes = new List<int>();
                int occurrences = 0;

                if (!string.IsNullOrEmpty(searchVal))
                {
                    // Select с индексом → фильтруем по совпадению
                    allIndexes = list
                        .Select((item, idx) => new { item, idx })
                        .Where(x => string.Equals(x.item, searchVal, sc))
                        .Select(x => x.idx)
                        .ToList();

                    occurrences = allIndexes.Count;
                    contains = occurrences > 0;
                    firstIndex = allIndexes.Count > 0 ? allIndexes[0] : -1;
                }

                // ── Запись результатов ──────────────────────────────────────────

                SetVariableValue(this.Prop_TotalCount, totalCount, sd);
                SetVariableValue(this.Prop_UniqueCount, uniqueCount, sd);
                SetVariableValue(this.Prop_EmptyCount, emptyCount, sd);
                SetVariableValue(this.Prop_HasDuplicates, duplicates.Any(), sd);
                SetVariableValue(this.Prop_Duplicates, duplicates, sd);
                SetVariableValue(this.Prop_MinLength, minLen, sd);
                SetVariableValue(this.Prop_MaxLength, maxLen, sd);
                SetVariableValue(this.Prop_AvgLength, avgLen, sd);
                SetVariableValue(this.Prop_Contains, contains, sd);
                SetVariableValue(this.Prop_FirstIndex, firstIndex, sd);
                SetVariableValue(this.Prop_AllIndexes, allIndexes, sd);
                SetVariableValue(this.Prop_OccurrenceCount, occurrences, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Анализ: всего {totalCount}, уникальных {uniqueCount}, дублей {duplicates.Count}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка анализа: {ex.Message}" };
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
