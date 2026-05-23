// =============================================================================
// TupleCreateDestructure.cs — активности «Кортеж: Создать» и «Кортеж: Деструктуризация».
//
// TupleCreateBack      — создаёт Tuple из 1–7 элементов произвольных типов.
// TupleDestructureBack — раскладывает все элементы кортежа в отдельные переменные.
//
// Совместимость: .NET Framework 4.7.2
// Тип кортежа:   System.Tuple<T1..T7> — иммутабельный, доступ через .Item1–.Item7.
//
// ВАЖНО: ValueTuple (C# 7 синтаксис (a, b)) НЕ используется — он требует NuGet
// пакет System.ValueTuple в .NET Framework. Используется классический Tuple.
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
    /// Создаёт System.Tuple или System.ValueTuple из 1–7 элементов произвольных типов.
    ///
    /// Тип кортежа задаётся через свойство «Тип кортежа»:
    ///   ClassicTuple — System.Tuple  (по умолчанию, не требует NuGet)
    ///   ValueTuple   — System.ValueTuple (требует NuGet System.ValueTuple 4.5.0)
    ///
    /// Арность определяется автоматически по последнему заполненному полю.
    /// Все остальные активности группы принимают оба типа прозрачно.
    /// </summary>
    public class TupleCreateBack : PrimoComponentTO<TupleCreate>
    {
        public override string GroupName { get => ActivityCategories.Tuples; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // ── INPUT: тип кортежа ─────────────────────────────────────────────────

        private TupleKind _kind = TupleKind.ClassicTuple;
        /// <summary>
        /// Тип создаваемого кортежа.
        /// ClassicTuple — System.Tuple (по умолчанию, не требует NuGet).
        /// ValueTuple   — System.ValueTuple (требует NuGet System.ValueTuple 4.5.0).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_TupleKind)]
        public TupleKind Kind
        {
            get => _kind;
            set { _kind = value; InvokePropertyChanged(this, nameof(Kind)); }
        }

        // ── INPUT: элементы ────────────────────────────────────────────────────

        private string _propItem1;
        /// <summary>Item1 — первый элемент (обязателен). Принимает любой тип.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Elements)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Item1)]
        public string Prop_Item1 { get => _propItem1; set { _propItem1 = value; InvokePropertyChanged(this, nameof(Prop_Item1)); } }

        private string _propItem2;
        /// <summary>Item2 — второй элемент.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Elements)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Item2)]
        public string Prop_Item2 { get => _propItem2; set { _propItem2 = value; InvokePropertyChanged(this, nameof(Prop_Item2)); } }

        private string _propItem3;
        /// <summary>Item3 — третий элемент.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Elements)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Item3)]
        public string Prop_Item3 { get => _propItem3; set { _propItem3 = value; InvokePropertyChanged(this, nameof(Prop_Item3)); } }

        private string _propItem4;
        /// <summary>Item4 — четвёртый элемент.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Elements)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Item4)]
        public string Prop_Item4 { get => _propItem4; set { _propItem4 = value; InvokePropertyChanged(this, nameof(Prop_Item4)); } }

        private string _propItem5;
        /// <summary>Item5 — пятый элемент.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Elements)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Item5)]
        public string Prop_Item5 { get => _propItem5; set { _propItem5 = value; InvokePropertyChanged(this, nameof(Prop_Item5)); } }

        private string _propItem6;
        /// <summary>Item6 — шестой элемент.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Elements)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Item6)]
        public string Prop_Item6 { get => _propItem6; set { _propItem6 = value; InvokePropertyChanged(this, nameof(Prop_Item6)); } }

        private string _propItem7;
        /// <summary>Item7 — седьмой элемент. Максимально допустимый.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Elements)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Item7)]
        public string Prop_Item7 { get => _propItem7; set { _propItem7 = value; InvokePropertyChanged(this, nameof(Prop_Item7)); } }

        // ── OUTPUT ─────────────────────────────────────────────────────────────

        private string _propResult;
        /// <summary>Созданный кортеж. Тип зависит от «Тип кортежа» и арности.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Result)]
        public string Prop_Result { get => _propResult; set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); } }

        private string _propArity;
        /// <summary>Арность созданного кортежа (1–7).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Arity)]
        public string Prop_Arity { get => _propArity; set { _propArity = value; InvokePropertyChanged(this, nameof(Prop_Arity)); } }

        private string _propKindName;
        /// <summary>Имя типа созданного кортежа: "Tuple`2" или "ValueTuple`2". Для диагностики.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_KindName)]
        public string Prop_KindName { get => _propKindName; set { _propKindName = value; InvokePropertyChanged(this, nameof(Prop_KindName)); } }

        // ── КОНСТРУКТОР ────────────────────────────────────────────────────────

        public TupleCreateBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Кортеж: Создать";
            sdkComponentHelp =
                "Создаёт System.Tuple или System.ValueTuple из 1–7 элементов.\n\n" +
                "── Тип кортежа ──────────────────────────────────────────\n" +
                "ClassicTuple — System.Tuple, ссылочный тип (по умолчанию).\n" +
                "               Не требует дополнительных NuGet-пакетов.\n" +
                "ValueTuple   — System.ValueTuple, значимый тип (struct).\n" +
                "               Выгоден для больших коллекций кортежей (меньше GC).\n" +
                "               Требует NuGet: System.ValueTuple 4.5.0.\n\n" +
                "── Правила ──────────────────────────────────────────────\n" +
                "Item1 обязателен. Заполняйте поля без пропусков по порядку.\n" +
                "Арность = последний заполненный ItemN.\n\n" +
                "Все прочие активности группы прозрачно работают\n" +
                "как с Tuple, так и с ValueTuple.";

            sdkComponentIcon = ActivityIcons.Tuple;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<TupleKind>("Kind", "ClassicTuple (System.Tuple) или ValueTuple (System.ValueTuple)"),
                PropertyBuilder.Script<object>("Prop_Item1", "Item1 — первый элемент (обязателен)"),
                PropertyBuilder.Script<object>("Prop_Item2", "Item2 — второй элемент"),
                PropertyBuilder.Script<object>("Prop_Item3", "Item3 — третий элемент"),
                PropertyBuilder.Script<object>("Prop_Item4", "Item4 — четвёртый элемент"),
                PropertyBuilder.Script<object>("Prop_Item5", "Item5 — пятый элемент"),
                PropertyBuilder.Script<object>("Prop_Item6", "Item6 — шестой элемент"),
                PropertyBuilder.Script<object>("Prop_Item7", "Item7 — седьмой элемент (максимум)"),
                PropertyBuilder.Variable<object>("Prop_Result", "Созданный кортеж"),
                PropertyBuilder.Variable<int>("Prop_Arity", "Арность (1–7)"),
                PropertyBuilder.Variable<string>("Prop_KindName", "Имя типа: Tuple`N или ValueTuple`N")
            };

            InitClass(container);
        }

        // ── ДЕЙСТВИЕ ───────────────────────────────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                object v1 = GetPropertyValue<object>(this.Prop_Item1, "Prop_Item1", sd);
                object v2 = string.IsNullOrWhiteSpace(this.Prop_Item2) ? null : GetPropertyValue<object>(this.Prop_Item2, "Prop_Item2", sd);
                object v3 = string.IsNullOrWhiteSpace(this.Prop_Item3) ? null : GetPropertyValue<object>(this.Prop_Item3, "Prop_Item3", sd);
                object v4 = string.IsNullOrWhiteSpace(this.Prop_Item4) ? null : GetPropertyValue<object>(this.Prop_Item4, "Prop_Item4", sd);
                object v5 = string.IsNullOrWhiteSpace(this.Prop_Item5) ? null : GetPropertyValue<object>(this.Prop_Item5, "Prop_Item5", sd);
                object v6 = string.IsNullOrWhiteSpace(this.Prop_Item6) ? null : GetPropertyValue<object>(this.Prop_Item6, "Prop_Item6", sd);
                object v7 = string.IsNullOrWhiteSpace(this.Prop_Item7) ? null : GetPropertyValue<object>(this.Prop_Item7, "Prop_Item7", sd);

                // Определяем арность по последнему непустому элементу
                int arity;
                if (v7 != null) arity = 7;
                else if (v6 != null) arity = 6;
                else if (v5 != null) arity = 5;
                else if (v4 != null) arity = 4;
                else if (v3 != null) arity = 3;
                else if (v2 != null) arity = 2;
                else arity = 1;

                bool useVT = this.Kind == TupleKind.ValueTuple;
                object tuple = TupleHelper.CreateFromArray(new object[] { v1, v2, v3, v4, v5, v6, v7 }, arity, useVT);

                SetVariableValue(this.Prop_Result, tuple, sd);
                SetVariableValue(this.Prop_Arity, arity, sd);
                SetVariableValue(this.Prop_KindName, tuple.GetType().Name, sd);

                string label = useVT ? "ValueTuple" : "Tuple";
                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Создан {label} с {arity} элементами" };
            }
            catch (ArgumentException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Неверный аргумент: {ex.Message}" };
            }
            catch (InvalidOperationException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Недопустимая операция: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка создания кортежа: {ex.Message}" };
            }
        }

        // ── ВАЛИДАЦИЯ ──────────────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Item1, ActivityStrings.Field_Item1, "Item1 обязателен — это первый элемент кортежа");
            ret.ValidateRequired(this.Prop_Result, ActivityStrings.Field_Result, ActivityStrings.Error_TupleRequired);
            return ret;
        }
    }
}