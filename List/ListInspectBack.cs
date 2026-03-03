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
    /// Активность «Список: Анализ».
    /// Полная диагностика List&lt;string&gt;: дубликаты, статистика длин,
    /// поиск элементов, частотность, разброс значений.
    /// Не изменяет список.
    /// </summary>
    public class ListInspectBack : PrimoComponentTO<ListInspect>
    {
        private const string CGroupName = "MIA" + WFPublishedElementBase.TREE_SEPARATOR + "Списки";
        public override string GroupName { get => CGroupName; protected set { } }

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

        private string _propSearchValue;
        /// <summary>Значение для поиска в списке (для определения индексов вхождений)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Поиск"), System.ComponentModel.DisplayName("Искомое значение")]
        public string Prop_SearchValue
        {
            get => _propSearchValue;
            set { _propSearchValue = value; InvokePropertyChanged(this, "Prop_SearchValue"); }
        }

        private bool _caseSensitive = false;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("Поиск"), System.ComponentModel.DisplayName("Учитывать регистр")]
        public bool Prop_CaseSensitive
        {
            get => _caseSensitive;
            set { _caseSensitive = value; InvokePropertyChanged(this, "Prop_CaseSensitive"); }
        }

        // — OUTPUT: базовая статистика —

        private string _propTotalCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Статистика"), System.ComponentModel.DisplayName("Всего элементов")]
        public string Prop_TotalCount { get => _propTotalCount; set { _propTotalCount = value; InvokePropertyChanged(this, "Prop_TotalCount"); } }

        private string _propUniqueCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Статистика"), System.ComponentModel.DisplayName("Уникальных")]
        public string Prop_UniqueCount { get => _propUniqueCount; set { _propUniqueCount = value; InvokePropertyChanged(this, "Prop_UniqueCount"); } }

        private string _propEmptyCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Статистика"), System.ComponentModel.DisplayName("Пустых")]
        public string Prop_EmptyCount { get => _propEmptyCount; set { _propEmptyCount = value; InvokePropertyChanged(this, "Prop_EmptyCount"); } }

        private string _propHasDuplicates;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Статистика"), System.ComponentModel.DisplayName("Есть дубликаты")]
        public string Prop_HasDuplicates { get => _propHasDuplicates; set { _propHasDuplicates = value; InvokePropertyChanged(this, "Prop_HasDuplicates"); } }

        private string _propDuplicates;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Статистика"), System.ComponentModel.DisplayName("Дублирующиеся значения")]
        public string Prop_Duplicates { get => _propDuplicates; set { _propDuplicates = value; InvokePropertyChanged(this, "Prop_Duplicates"); } }

        // — OUTPUT: статистика длин —

        private string _propMinLength;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Длины"), System.ComponentModel.DisplayName("Мин. длина")]
        public string Prop_MinLength { get => _propMinLength; set { _propMinLength = value; InvokePropertyChanged(this, "Prop_MinLength"); } }

        private string _propMaxLength;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Длины"), System.ComponentModel.DisplayName("Макс. длина")]
        public string Prop_MaxLength { get => _propMaxLength; set { _propMaxLength = value; InvokePropertyChanged(this, "Prop_MaxLength"); } }

        private string _propAvgLength;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(double))]
        [System.ComponentModel.Category("Длины"), System.ComponentModel.DisplayName("Средняя длина")]
        public string Prop_AvgLength { get => _propAvgLength; set { _propAvgLength = value; InvokePropertyChanged(this, "Prop_AvgLength"); } }

        // — OUTPUT: поиск —

        private string _propContains;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Поиск"), System.ComponentModel.DisplayName("Найдено")]
        public string Prop_Contains { get => _propContains; set { _propContains = value; InvokePropertyChanged(this, "Prop_Contains"); } }

        private string _propFirstIndex;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Поиск"), System.ComponentModel.DisplayName("Первый индекс")]
        public string Prop_FirstIndex { get => _propFirstIndex; set { _propFirstIndex = value; InvokePropertyChanged(this, "Prop_FirstIndex"); } }

        private string _propAllIndexes;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<int>))]
        [System.ComponentModel.Category("Поиск"), System.ComponentModel.DisplayName("Все индексы")]
        public string Prop_AllIndexes { get => _propAllIndexes; set { _propAllIndexes = value; InvokePropertyChanged(this, "Prop_AllIndexes"); } }

        private string _propOccurrenceCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Поиск"), System.ComponentModel.DisplayName("Кол-во вхождений")]
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
            sdkComponentIcon = "pack://application:,,,/Primo.MIA;component/images/list.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_List", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>), ToolTip = "Входной список", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_SearchValue", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(string), ToolTip = "Значение для поиска (необязательно)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_TotalCount", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Всего элементов", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_UniqueCount", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Уникальных элементов", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_EmptyCount", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Пустых элементов", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_HasDuplicates", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool), ToolTip = "Есть ли дублирующиеся значения", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Duplicates", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>), ToolTip = "Список дублирующихся значений", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_MinLength", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Минимальная длина строки", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_MaxLength", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Максимальная длина строки", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_AvgLength", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(double), ToolTip = "Средняя длина строки", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Contains", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool), ToolTip = "Найден ли искомый элемент", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_FirstIndex", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Первый индекс вхождения (-1 если не найден)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_AllIndexes", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<int>), ToolTip = "Все индексы вхождений искомого значения", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_OccurrenceCount", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Количество вхождений искомого значения", IsReadOnly = false }
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

                bool contains    = false;
                int  firstIndex  = -1;
                var  allIndexes  = new List<int>();
                int  occurrences = 0;

                if (!string.IsNullOrEmpty(searchVal))
                {
                    // Select с индексом → фильтруем по совпадению
                    allIndexes = list
                        .Select((item, idx) => new { item, idx })
                        .Where(x => string.Equals(x.item, searchVal, sc))
                        .Select(x => x.idx)
                        .ToList();

                    occurrences = allIndexes.Count;
                    contains    = occurrences > 0;
                    firstIndex = allIndexes.Count > 0 ? allIndexes[0] : -1;
                }

                // ── Запись результатов ──────────────────────────────────────────

                SetVariableValue(this.Prop_TotalCount,      totalCount,       sd);
                SetVariableValue(this.Prop_UniqueCount,     uniqueCount,      sd);
                SetVariableValue(this.Prop_EmptyCount,      emptyCount,       sd);
                SetVariableValue(this.Prop_HasDuplicates,   duplicates.Any(), sd);
                SetVariableValue(this.Prop_Duplicates,      duplicates,       sd);
                SetVariableValue(this.Prop_MinLength,       minLen,           sd);
                SetVariableValue(this.Prop_MaxLength,       maxLen,           sd);
                SetVariableValue(this.Prop_AvgLength,       avgLen,           sd);
                SetVariableValue(this.Prop_Contains,        contains,         sd);
                SetVariableValue(this.Prop_FirstIndex,      firstIndex,       sd);
                SetVariableValue(this.Prop_AllIndexes,      allIndexes,       sd);
                SetVariableValue(this.Prop_OccurrenceCount, occurrences,      sd);

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
            if (string.IsNullOrWhiteSpace(this.Prop_List))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Список", Error = "Список обязателен" });
            return ret;
        }
    }
}
