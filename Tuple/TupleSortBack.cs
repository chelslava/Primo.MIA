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
    /// Сортирует List&lt;Tuple&gt; по значению указанного элемента ItemN.
    ///
    /// Работает с кортежами любой арности через рефлексию — тип кортежей
    /// определяется автоматически по первому элементу списка.
    ///
    /// Поддерживает три типа сортировки:
    ///   Alphabetical — строковая без учёта регистра (по умолчанию)
    ///   Numeric      — числовая (нечисловые элементы уходят в конец)
    ///   Natural      — натуральная (file2 &lt; file10)
    ///
    /// Возвращает новый список — оригинал не изменяется.
    /// </summary>
        public class TupleSortBack : PrimoComponentTO<TupleSort>
    {
        public override string GroupName { get => ActivityCategories.Tuples; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // ── INPUT ──────────────────────────────────────────────────────────────

        private string _propTupleList;
        /// <summary>Список кортежей для сортировки. Тип элемента — любой Tuple арности 1–7.</summary>
                [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_TupleList)]
        public string Prop_TupleList { get => _propTupleList; set { _propTupleList = value; InvokePropertyChanged(this, "Prop_TupleList"); } }

        private TupleItemIndex _sortKey = TupleItemIndex.Item1;
        /// <summary>По значению какого элемента сортировать (Item1–Item7).</summary>
                [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_SortKey)]
        public TupleItemIndex SortKey
        {
            get => _sortKey;
            set { _sortKey = value; InvokePropertyChanged(this, "SortKey"); }
        }

        private TupleSortDirection _direction = TupleSortDirection.Ascending;
        /// <summary>Направление сортировки: по возрастанию или убыванию.</summary>
                [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Direction)]
        public TupleSortDirection Direction
        {
            get => _direction;
            set { _direction = value; InvokePropertyChanged(this, "Direction"); }
        }

        private TupleSortType _sortType = TupleSortType.Alphabetical;
        /// <summary>Тип сортировки: алфавитная, числовая или натуральная.</summary>
                [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_SortType)]
        public TupleSortType SortType
        {
            get => _sortType;
            set { _sortType = value; InvokePropertyChanged(this, "SortType"); }
        }

        // ── OUTPUT ─────────────────────────────────────────────────────────────

        private string _propResult;
        /// <summary>Отсортированный список кортежей. Имеет тот же тип что и входной список.</summary>
                [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_OutputVariable)]
        public string Prop_Result { get => _propResult; set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); } }

        private string _propCount;
        /// <summary>Количество кортежей в результате.</summary>
                [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Count)]
        public string Prop_Count { get => _propCount; set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); } }

        // ── КОНСТРУКТОР ────────────────────────────────────────────────────────

        public TupleSortBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Кортеж: Сортировка списка";
            sdkComponentHelp =
                "Сортирует List<Tuple<...>> по значению выбранного элемента.\n\n" +
                "── Параметры ────────────────────────────────────────────\n" +
                "Ключ сортировки — Item1, Item2... по значению которого сортируем.\n" +
                "Направление     — Ascending (A→Z) или Descending (Z→A).\n" +
                "Тип сортировки:\n" +
                "  Alphabetical — без учёта регистра (по умолчанию)\n" +
                "  Numeric      — числовая; нечисловые уходят в конец\n" +
                "  Natural      — file2 < file10 (числа внутри строки)\n\n" +
                "Оригинальный список не изменяется — возвращается новый.";
            sdkComponentIcon = ActivityIcons.Tuple;

                        sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<object>("Prop_TupleList", "Список кортежей для сортировки"),
                PropertyBuilder.Enum<TupleItemIndex>("SortKey", "По какому элементу сортировать (Item1–Item7)"),
                PropertyBuilder.Enum<TupleSortDirection>("Direction", "Направление: Ascending или Descending"),
                PropertyBuilder.Enum<TupleSortType>("SortType", "Тип: Alphabetical, Numeric, Natural"),
                PropertyBuilder.Variable<object>("Prop_Result", "Отсортированный список"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество кортежей")
            };

            InitClass(container);
        }

        // ── ДЕЙСТВИЕ ───────────────────────────────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                object rawList = GetPropertyValue<object>(this.Prop_TupleList, "Prop_TupleList", sd);
                if (rawList == null) throw new ArgumentNullException("Prop_TupleList", "Список кортежей не может быть null");

                var enumerable = rawList as System.Collections.IEnumerable;
                if (enumerable == null)
                    throw new InvalidOperationException($"Входной объект не является списком: {rawList.GetType().Name}");

                var items = enumerable.Cast<object>().ToList();
                if (items.Count == 0)
                {
                    SetVariableValue(this.Prop_Result, rawList, sd);
                    SetVariableValue(this.Prop_Count, 0, sd);
                    return new ExecutionResult { IsSuccess = true, SuccessMessage = "Список пуст — сортировка не выполнялась" };
                }

                int keyNum = (int)this.SortKey;

                // Получаем строковое значение ключа из каждого кортежа для сравнения
                Func<object, string> getKey = t => TupleHelper.GetItem(t, keyNum)?.ToString() ?? "";

                // Выбираем компаратор в зависимости от типа сортировки
                IOrderedEnumerable<object> sorted;

                switch (this.SortType)
                {
                    case TupleSortType.Numeric:
                        // Числовые — парсим как double, нечисловые → MaxValue (уходят в конец)
                        Func<object, double> numKey = t =>
                        {
                            double d;
                            return double.TryParse(getKey(t).Replace(',', '.'),
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out d) ? d : double.MaxValue;
                        };
                        sorted = this.Direction == TupleSortDirection.Ascending
                            ? items.OrderBy(numKey)
                            : items.OrderByDescending(numKey);
                        break;

                                        case TupleSortType.Natural:
                        sorted = this.Direction == TupleSortDirection.Ascending
                            ? items.OrderBy(getKey, NaturalComparer.Instance)
                            : items.OrderByDescending(getKey, NaturalComparer.Instance);
                        break;

                    default: // Alphabetical
                        sorted = this.Direction == TupleSortDirection.Ascending
                            ? items.OrderBy(getKey, StringComparer.OrdinalIgnoreCase)
                            : items.OrderByDescending(getKey, StringComparer.OrdinalIgnoreCase);
                        break;
                }

                // Собираем результат обратно в List<object>
                // Тип элементов сохраняется — кортежи не изменяются, только порядок
                var resultList = sorted.ToList();

                SetVariableValue(this.Prop_Result, resultList, sd);
                SetVariableValue(this.Prop_Count, resultList.Count, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Отсортировано {resultList.Count} кортежей по Item{keyNum} ({this.SortType}, {this.Direction})"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка сортировки: {ex.Message}" };
            }
        }

        // ── ВАЛИДАЦИЯ ──────────────────────────────────────────────────────────

                public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_TupleList, ActivityStrings.Field_TupleList, "Список кортежей обязателен");
            ret.ValidateRequired(this.Prop_Result, ActivityStrings.Field_OutputVariable, "Выходная переменная «Результат» обязательна");
            return ret;
        }
    }
}
