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
    /// Разбирает List&lt;Tuple&lt;string,string&gt;&gt; или List&lt;Tuple&lt;string,string,string&gt;&gt;
    /// обратно в отдельные списки — по одному на каждый ItemN.
    ///
    /// Обратная операция к «Кортеж: Zip списков».
    ///
    /// Пример:
    ///   [(«Иванов»,«42»), («Петров»,«37»)]
    ///   → ListA = [«Иванов»,«Петров»], ListB = [«42»,«37»]
    /// </summary>
    public class TupleUnzipBack : PrimoComponentTO<TupleUnzip>
    {
        private const string CGroupName = "MIA" + WFPublishedElementBase.TREE_SEPARATOR + "Кортежи";
        public override string GroupName { get => CGroupName; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // ── INPUT ──────────────────────────────────────────────────────────────

        private string _propTupleList;
        /// <summary>
        /// Список кортежей для разбора.
        /// Поддерживаются List&lt;Tuple&lt;string,string&gt;&gt; и List&lt;Tuple&lt;string,string,string&gt;&gt;,
        /// а также любой List&lt;T&gt; где T — Tuple любой арности (через рефлексию).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Список кортежей")]
        public string Prop_TupleList { get => _propTupleList; set { _propTupleList = value; InvokePropertyChanged(this, "Prop_TupleList"); } }

        // ── OUTPUT ─────────────────────────────────────────────────────────────

        private string _propListA;
        /// <summary>Список значений Item1 из каждого кортежа.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Список A (из Item1)")]
        public string Prop_ListA { get => _propListA; set { _propListA = value; InvokePropertyChanged(this, "Prop_ListA"); } }

        private string _propListB;
        /// <summary>Список значений Item2 из каждого кортежа.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Список B (из Item2)")]
        public string Prop_ListB { get => _propListB; set { _propListB = value; InvokePropertyChanged(this, "Prop_ListB"); } }

        private string _propListC;
        /// <summary>
        /// Список значений Item3 из каждого кортежа.
        /// Пустой список если арность кортежей меньше 3.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Список C (из Item3)")]
        public string Prop_ListC { get => _propListC; set { _propListC = value; InvokePropertyChanged(this, "Prop_ListC"); } }

        private string _propCount;
        /// <summary>Количество обработанных кортежей.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Количество")]
        public string Prop_Count { get => _propCount; set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); } }

        private string _propArity;
        /// <summary>Арность кортежей в переданном списке (определяется по первому элементу).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Арность кортежей")]
        public string Prop_Arity { get => _propArity; set { _propArity = value; InvokePropertyChanged(this, "Prop_Arity"); } }

        // ── КОНСТРУКТОР ────────────────────────────────────────────────────────

        public TupleUnzipBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Кортеж: Unzip списков";
            sdkComponentHelp =
                "Разбирает список кортежей обратно в отдельные списки.\n" +
                "Обратная операция к «Кортеж: Zip списков».\n\n" +
                "── Пример ───────────────────────────────────────────────\n" +
                "Вход: [(\"Иванов\",\"42\"), (\"Петров\",\"37\")]\n" +
                "Список A (Item1): [\"Иванов\", \"Петров\"]\n" +
                "Список B (Item2): [\"42\", \"37\"]\n\n" +
                "Значения null записываются как пустая строка \"\".";

            sdkComponentIcon = "pack://application:,,,/Primo.MIA;component/images/sharp.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_TupleList", PropertyType = PropertyTypes.SCRIPT,   EditorType = ScriptEditorTypes.NONE, DataType = typeof(object),       ToolTip = "Список кортежей для разбора", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_ListA",     PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>), ToolTip = "Значения Item1 каждого кортежа", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_ListB",     PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>), ToolTip = "Значения Item2 каждого кортежа", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_ListC",     PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>), ToolTip = "Значения Item3 каждого кортежа (если арность ≥ 3)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Count",     PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int),          ToolTip = "Количество обработанных кортежей", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Arity",     PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int),          ToolTip = "Арность кортежей в списке", IsReadOnly = false }
            };

            InitClass(container);
        }

        // ── ДЕЙСТВИЕ ───────────────────────────────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Получаем список как object — тип элементов неизвестен заранее
                object rawList = GetPropertyValue<object>(this.Prop_TupleList, "Prop_TupleList", sd);
                if (rawList == null) throw new ArgumentNullException("Prop_TupleList", "Список кортежей не может быть null");

                // Приводим к IEnumerable<object> через рефлексию — работает с любым List<T>
                var enumerable = rawList as System.Collections.IEnumerable;
                if (enumerable == null)
                    throw new InvalidOperationException($"Входной объект не является списком: {rawList.GetType().Name}");

                var items = enumerable.Cast<object>().ToList();

                if (items.Count == 0)
                {
                    // Пустой список — возвращаем пустые списки
                    SetVariableValue(this.Prop_ListA, new List<string>(), sd);
                    SetVariableValue(this.Prop_ListB, new List<string>(), sd);
                    SetVariableValue(this.Prop_ListC, new List<string>(), sd);
                    SetVariableValue(this.Prop_Count, 0, sd);
                    SetVariableValue(this.Prop_Arity, 0, sd);
                    return new ExecutionResult { IsSuccess = true, SuccessMessage = "Список пуст — результаты пусты" };
                }

                // Определяем арность по первому элементу
                int arity = TupleHelper.GetArity(items[0]);
                if (arity < 0)
                    throw new InvalidOperationException(
                        $"Элементы списка не являются System.Tuple: {items[0].GetType().Name}");
                if (arity < 2)
                    throw new InvalidOperationException(
                        $"Unzip требует кортежи арности ≥ 2. Текущая арность: {arity}");

                // Разбираем каждый кортеж в отдельные списки через LINQ
                var listA = items.Select(t => TupleHelper.GetItem(t, 1)?.ToString() ?? "").ToList();
                var listB = items.Select(t => TupleHelper.GetItem(t, 2)?.ToString() ?? "").ToList();
                var listC = arity >= 3
                    ? items.Select(t => TupleHelper.GetItem(t, 3)?.ToString() ?? "").ToList()
                    : new List<string>();

                SetVariableValue(this.Prop_ListA, listA, sd);
                SetVariableValue(this.Prop_ListB, listB, sd);
                SetVariableValue(this.Prop_ListC, listC, sd);
                SetVariableValue(this.Prop_Count, items.Count, sd);
                SetVariableValue(this.Prop_Arity, arity, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Unzip: {items.Count} кортежей → {(arity >= 3 ? "3 списка" : "2 списка")}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка Unzip: {ex.Message}" };
            }
        }

        // ── ВАЛИДАЦИЯ ──────────────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            if (string.IsNullOrWhiteSpace(this.Prop_TupleList))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Список кортежей", Error = "Список кортежей обязателен" });

            bool anyOut = !string.IsNullOrWhiteSpace(this.Prop_ListA)
                       || !string.IsNullOrWhiteSpace(this.Prop_ListB);
            if (!anyOut)
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Список A", Error = "Задайте хотя бы один выходной список (A или B)" });

            return ret;
        }
    }
}
