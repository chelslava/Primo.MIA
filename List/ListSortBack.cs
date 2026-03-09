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
using LTools.Enums;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

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
                var list = GetPropertyValue<List<string>>(this.Prop_List, "Prop_List", sd);
                if (list == null) throw new ArgumentNullException("Prop_List", "Список не может быть null");

                List<string> result = Sort(list);

                SetVariableValue(this.Prop_Result, result,       sd);
                SetVariableValue(this.Prop_Count,  result.Count, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Отсортировано {result.Count} элементов ({this.Mode})" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка сортировки: {ex.Message}" };
            }
        }

        private List<string> Sort(List<string> list)
        {
            switch (this.Mode)
            {
                case ListSortMode.Alphabetical:
                    return list.OrderBy(x => x, StringComparer.Ordinal).ToList();

                case ListSortMode.AlphabeticalDesc:
                    return list.OrderByDescending(x => x, StringComparer.Ordinal).ToList();

                case ListSortMode.CaseInsensitive:
                    return list.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

                case ListSortMode.CaseInsensitiveDesc:
                    return list.OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase).ToList();

                case ListSortMode.ByLength:
                    // При одинаковой длине — вторичная сортировка по алфавиту для стабильности
                    return list.OrderBy(x => x?.Length ?? 0).ThenBy(x => x).ToList();

                case ListSortMode.ByLengthDesc:
                    return list.OrderByDescending(x => x?.Length ?? 0).ThenBy(x => x).ToList();

                case ListSortMode.Natural:
                    return list.OrderBy(x => x, NaturalStringComparer.Instance).ToList();

                case ListSortMode.Reverse:
                    // Копируем и переворачиваем — без пересортировки
                    var reversed = new List<string>(list);
                    reversed.Reverse();
                    return reversed;

                case ListSortMode.Random:
                    return ShuffleList(list);

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {this.Mode}");
            }
        }

        /// <summary>
        /// Перемешивание Fisher–Yates — равномерно случайный порядок.
        /// При заданном Prop_RandomSeed результат воспроизводим.
        /// </summary>
        private List<string> ShuffleList(List<string> list)
        {
            var result = new List<string>(list);
            Random rng;

            // Пробуем прочитать зерно из свойства
            if (!string.IsNullOrWhiteSpace(this.Prop_RandomSeed)
                && int.TryParse(this.Prop_RandomSeed, out int seed))
                rng = new Random(seed);
            else
                rng = new Random();

            // Fisher–Yates shuffle
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                string tmp = result[i];
                result[i]  = result[j];
                result[j]  = tmp;
            }

            return result;
        }

        public override ValidationResult Validate()
        {
                        var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_List, ActivityStrings.Field_List, ActivityStrings.Error_ListRequired);
            return ret;
        }
    }

    // =========================================================================
    // ВСПОМОГАТЕЛЬНЫЙ КОМПАРАТОР — натуральная сортировка
    // =========================================================================

    /// <summary>
    /// Компаратор натуральной сортировки строк.
    /// "file2" &lt; "file10" — числовые части сравниваются как числа, не как строки.
    /// Используется LINQ-совместимый паттерн через IComparer&lt;string&gt;.
    /// </summary>
    internal sealed class NaturalStringComparer : IComparer<string>
    {
        // Синглтон — один экземпляр на всё приложение
        public static readonly NaturalStringComparer Instance = new NaturalStringComparer();

        private static readonly Regex _tokenizer =
            new Regex(@"(\d+)", RegexOptions.Compiled);

        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            // Разбиваем строки на токены: текст и числовые части чередуются
            string[] xTokens = _tokenizer.Split(x);
            string[] yTokens = _tokenizer.Split(y);

            // Сравниваем попарно
            int minLen = Math.Min(xTokens.Length, yTokens.Length);

            for (int i = 0; i < minLen; i++)
            {
                string xt = xTokens[i];
                string yt = yTokens[i];

                int cmp;

                // Если оба токена — числа, сравниваем как long
                if (long.TryParse(xt, out long xn) && long.TryParse(yt, out long yn))
                    cmp = xn.CompareTo(yn);
                else
                    cmp = string.Compare(xt, yt, StringComparison.OrdinalIgnoreCase);

                if (cmp != 0) return cmp;
            }

            // Если все общие токены равны — более короткая строка идёт первой
            return xTokens.Length.CompareTo(yTokens.Length);
        }
    }
}
