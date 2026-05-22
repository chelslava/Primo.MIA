// =============================================================================
// ListSort.cs — активность «Список: Сортировка».
//
// Сортирует List<string> различными способами:
//   Alphabetical        — алфавитная (A→Z)
//   AlphabeticalDesc    — алфавитная обратная (Z→A)
//   ByLength            — по длине строки (короткие первые)
//   ByLengthDesc        — по длине строки (длинные первые)
//   Natural             — натуральная (file1, file2, file10 вместо file1, file10, file2)
//   Reverse             — обратный порядок текущего списка (без сортировки)
//   Random              — случайное перемешивание
//   CaseInsensitive     — алфавитная без учёта регистра
//
// Все операции возвращают новый список — оригинал не изменяется.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Список: Сортировка».
    /// Сортирует List&lt;string&gt; выбранным способом.
    /// Возвращает новый список — оригинал не изменяется.
    /// </summary>
    public class ListSortBack : PrimoComponentTO<ListSort>
    {
        public override string GroupName { get => ActivityCategories.Lists; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // ── INPUT ──────────────────────────────────────────────────────────────

        private string _propList;
        /// <summary>Входной список List&lt;string&gt; для сортировки</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_List)]
        public string Prop_List
        {
            get => _propList;
            set { _propList = value; InvokePropertyChanged(this, "Prop_List"); }
        }

        private ListSortMode _mode = ListSortMode.Alphabetical;
        /// <summary>Способ сортировки</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SortMode)]
        public ListSortMode Mode
        {
            get => _mode;
            set { _mode = value; InvokePropertyChanged(this, "Mode"); }
        }

        private string _propRandomSeed;
        /// <summary>
        /// Зерно генератора случайных чисел для режима Random.
        /// Одинаковое зерно → одинаковый результат. Пусто → случайное зерно.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Random), System.ComponentModel.DisplayName(ActivityStrings.Field_RandomSeed)]
        public string Prop_RandomSeed
        {
            get => _propRandomSeed;
            set { _propRandomSeed = value; InvokePropertyChanged(this, "Prop_RandomSeed"); }
        }

        // ── OUTPUT ─────────────────────────────────────────────────────────────

        private string _propResult;
        /// <summary>Отсортированный список</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Result)]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); }
        }

        private string _propCount;
        /// <summary>Количество элементов в результате</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Count)]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        // ── КОНСТРУКТОР ────────────────────────────────────────────────────────

        public ListSortBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Список: Сортировка";
            sdkComponentHelp =
                "Сортирует List<string> выбранным способом.\n" +
                "Возвращает новый список — оригинал не изменяется.\n\n" +
                "── Способы ─────────────────────────────────────────────\n" +
                "Alphabetical        — A→Z (с учётом регистра)\n" +
                "AlphabeticalDesc    — Z→A (с учётом регистра)\n" +
                "CaseInsensitive     — A→Z без учёта регистра\n" +
                "CaseInsensitiveDesc — Z→A без учёта регистра\n" +
                "ByLength            — по длине, короткие первые\n" +
                "ByLengthDesc        — по длине, длинные первые\n" +
                "Natural             — file1, file2, file10 (не file10 после file1)\n" +
                "Reverse             — обратный порядок (не сортирует)\n" +
                "Random              — перемешивание Fisher–Yates";

            sdkComponentIcon = ActivityIcons.List;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<List<string>>("Prop_List", "Входной список"),
                PropertyBuilder.Enum<ListSortMode>("Mode", "Способ сортировки"),
                PropertyBuilder.Script<int>("Prop_RandomSeed", "Зерно для Random (необязательно)"),
                PropertyBuilder.Variable<List<string>>("Prop_Result", "Отсортированный список"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество элементов")
            };

            InitClass(container);
        }

        // ── ДЕЙСТВИЕ ───────────────────────────────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var logic = new ListSortLogic();
                var list = GetPropertyValue<List<string>>(this.Prop_List, "Prop_List", sd);
                if (list == null) throw new ArgumentNullException("Prop_List", "Список не может быть null");

                int? randomSeed = TryGetRandomSeed(sd);
                List<string> result = logic.Sort(list, this.Mode, randomSeed);

                SetVariableValue(this.Prop_Result, result, sd);
                SetVariableValue(this.Prop_Count, result.Count, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Отсортировано {result.Count} элементов ({this.Mode})" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка сортировки: {ex.Message}" };
            }
        }

        private int? TryGetRandomSeed(ScriptingData sd)
        {
            string randomSeed = GetPropertyValue<string>(this.Prop_RandomSeed, nameof(Prop_RandomSeed), sd);
            if (!string.IsNullOrWhiteSpace(randomSeed)
                && int.TryParse(randomSeed, out int seed))
                return seed;

            return null;
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_List, ActivityStrings.Field_List, ActivityStrings.Error_ListRequired);
            return ret;
        }
    }

}
