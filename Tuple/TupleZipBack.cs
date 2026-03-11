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
    /// Объединяет два или три List&lt;string&gt; в List&lt;Tuple&lt;string,string&gt;&gt;
    /// или List&lt;Tuple&lt;string,string,string&gt;&gt; — попарно по индексу.
    ///
    /// Результат имеет длину минимального из входных списков.
    ///
    /// Типичное применение:
    ///   Список имён + список значений → список пар для обработки в цикле.
    ///   ["Иванов","Петров"] + ["42","37"] → [("Иванов","42"), ("Петров","37")]
    /// </summary>
    public class TupleZipBack : PrimoComponentTO<TupleZip>
    {
        public override string GroupName { get => ActivityCategories.Tuples; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // ── INPUT ──────────────────────────────────────────────────────────────

        private string _propListA;
        /// <summary>Первый список — значения становятся Item1 каждого кортежа.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_ListA_Item1)]
        public string Prop_ListA { get => _propListA; set { _propListA = value; InvokePropertyChanged(this, "Prop_ListA"); } }

        private string _propListB;
        /// <summary>Второй список — значения становятся Item2 каждого кортежа.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_ListB_Item2)]
        public string Prop_ListB { get => _propListB; set { _propListB = value; InvokePropertyChanged(this, "Prop_ListB"); } }

        private string _propListC;
        /// <summary>
        /// Третий список — необязателен. Если задан, создаются трёхэлементные кортежи Tuple&lt;string,string,string&gt;.
        /// Если не задан — создаются двухэлементные Tuple&lt;string,string&gt;.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Optional)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_ListC_Item3)]
        public string Prop_ListC { get => _propListC; set { _propListC = value; InvokePropertyChanged(this, "Prop_ListC"); } }

        // ── INPUT: вид кортежей ────────────────────────────────────────────────

        private TupleKind _kind = TupleKind.ClassicTuple;
        /// <summary>
        /// Вид кортежей в создаваемом списке.
        ///   ClassicTuple — List&lt;Tuple&lt;string,string&gt;&gt; (по умолчанию, без NuGet).
        ///   ValueTuple   — List&lt;ValueTuple&lt;string,string&gt;&gt; (требует System.ValueTuple).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_TupleKind)]
        public TupleKind Kind
        {
            get => _kind;
            set { _kind = value; InvokePropertyChanged(this, "Kind"); }
        }

        // ── OUTPUT ─────────────────────────────────────────────────────────────

        private string _propResult;
        /// <summary>
        /// Список кортежей. Тип элемента:
        ///   List&lt;Tuple&lt;string,string&gt;&gt; если ListC не задан,
        ///   List&lt;Tuple&lt;string,string,string&gt;&gt; если ListC задан.
        /// Используйте тип object или приводите при обходе цикла.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_TupleList)]
        public string Prop_Result { get => _propResult; set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); } }

        private string _propCount;
        /// <summary>Количество кортежей в результате (длина минимального из входных списков).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Count)]
        public string Prop_Count { get => _propCount; set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); } }

        private string _propArity;
        /// <summary>Арность каждого кортежа в результате (2 или 3).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_TupleArity)]
        public string Prop_Arity { get => _propArity; set { _propArity = value; InvokePropertyChanged(this, "Prop_Arity"); } }

        // ── КОНСТРУКТОР ────────────────────────────────────────────────────────

        public TupleZipBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Кортеж: Zip списков";
            sdkComponentHelp =
                "Объединяет 2–3 списка в список кортежей попарно по индексу.\n\n" +
                "Длина результата = длина наименьшего из входных списков.\n\n" +
                "── Примеры ──────────────────────────────────────────────\n" +
                "A=[\"Иванов\",\"Петров\"]  B=[\"42\",\"37\"]\n" +
                "→ [(\"Иванов\",\"42\"), (\"Петров\",\"37\")]  Tuple<string,string>\n\n" +
                "A=[\"a\",\"b\"]  B=[\"1\",\"2\"]  C=[\"X\",\"Y\"]\n" +
                "→ [(\"a\",\"1\",\"X\"), (\"b\",\"2\",\"Y\")]  Tuple<string,string,string>\n\n" +
                "Обратная операция — «Кортеж: Unzip списков».";
            sdkComponentIcon = ActivityIcons.Tuple;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<List<string>>("Prop_ListA", "Список A — значения для Item1"),
                PropertyBuilder.Script<List<string>>("Prop_ListB", "Список B — значения для Item2"),
                PropertyBuilder.Script<List<string>>("Prop_ListC", "Список C — значения для Item3 (необязателен)"),
                PropertyBuilder.Variable<object>("Prop_Result", "Результирующий список кортежей"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество кортежей"),
                PropertyBuilder.Variable<int>("Prop_Arity", "Арность каждого кортежа (2 или 3)")
            };

            InitClass(container);
        }

        // ── ДЕЙСТВИЕ ───────────────────────────────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var listA = GetPropertyValue<List<string>>(this.Prop_ListA, "Prop_ListA", sd);
                var listB = GetPropertyValue<List<string>>(this.Prop_ListB, "Prop_ListB", sd);

                if (listA == null) throw new ArgumentNullException("Prop_ListA", "Список A не может быть null");
                if (listB == null) throw new ArgumentNullException("Prop_ListB", "Список B не может быть null");

                // Список C — необязателен
                List<string> listC = null;
                if (!string.IsNullOrWhiteSpace(this.Prop_ListC))
                    listC = GetPropertyValue<List<string>>(this.Prop_ListC, "Prop_ListC", sd);

                int arity;
                object result;
                int count;

                if (listC != null)
                {
                    // Трёхэлементные кортежи — длина по минимуму из трёх списков
                    count = Math.Min(listA.Count, Math.Min(listB.Count, listC.Count));
                    arity = 3;
                    bool useVT3 = this.Kind == TupleKind.ValueTuple;
                    result = Enumerable.Range(0, count)
                                       .Select(i => TupleHelper.CreateFromArray(
                                                        new object[] { listA[i], listB[i], listC[i] }, 3, useVT3))
                                       .ToList();
                }
                else
                {
                    // Двухэлементные кортежи — длина по минимуму из двух списков
                    count = Math.Min(listA.Count, listB.Count);
                    arity = 2;
                    bool useVT2 = this.Kind == TupleKind.ValueTuple;
                    result = Enumerable.Range(0, count)
                                       .Select(i => TupleHelper.CreateFromArray(
                                                        new object[] { listA[i], listB[i] }, 2, useVT2))
                                       .ToList();
                }

                SetVariableValue(this.Prop_Result, result, sd);
                SetVariableValue(this.Prop_Count, count, sd);
                SetVariableValue(this.Prop_Arity, arity, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Zip: создано {count} кортежей Tuple<string,string{(arity == 3 ? ",string" : "")}>"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка Zip: {ex.Message}" };
            }
        }

        // ── ВАЛИДАЦИЯ ──────────────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_ListA, ActivityStrings.Field_ListA_Item1, "Список A обязателен");
            ret.ValidateRequired(this.Prop_ListB, ActivityStrings.Field_ListB_Item2, ActivityStrings.Error_ListBRequired);
            ret.ValidateRequired(this.Prop_Result, ActivityStrings.Field_TupleList, "Выходная переменная обязательна");
            return ret;
        }
    }
}