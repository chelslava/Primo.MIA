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
using LTools.Enums;
using LTools.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

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
        private const string CGroupName = "MIA" + WFPublishedElementBase.TREE_SEPARATOR + "Кортежи";
        public override string GroupName { get => CGroupName; protected set { } }

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
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Тип кортежа")]
        public TupleKind Kind
        {
            get => _kind;
            set { _kind = value; InvokePropertyChanged(this, "Kind"); }
        }

        // ── INPUT: элементы ────────────────────────────────────────────────────

        private string _propItem1;
        /// <summary>Item1 — первый элемент (обязателен). Принимает любой тип.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Элементы"), System.ComponentModel.DisplayName("Item1 (обязателен)")]
        public string Prop_Item1 { get => _propItem1; set { _propItem1 = value; InvokePropertyChanged(this, "Prop_Item1"); } }

        private string _propItem2;
        /// <summary>Item2 — второй элемент.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Элементы"), System.ComponentModel.DisplayName("Item2")]
        public string Prop_Item2 { get => _propItem2; set { _propItem2 = value; InvokePropertyChanged(this, "Prop_Item2"); } }

        private string _propItem3;
        /// <summary>Item3 — третий элемент.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Элементы"), System.ComponentModel.DisplayName("Item3")]
        public string Prop_Item3 { get => _propItem3; set { _propItem3 = value; InvokePropertyChanged(this, "Prop_Item3"); } }

        private string _propItem4;
        /// <summary>Item4 — четвёртый элемент.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Элементы"), System.ComponentModel.DisplayName("Item4")]
        public string Prop_Item4 { get => _propItem4; set { _propItem4 = value; InvokePropertyChanged(this, "Prop_Item4"); } }

        private string _propItem5;
        /// <summary>Item5 — пятый элемент.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Элементы"), System.ComponentModel.DisplayName("Item5")]
        public string Prop_Item5 { get => _propItem5; set { _propItem5 = value; InvokePropertyChanged(this, "Prop_Item5"); } }

        private string _propItem6;
        /// <summary>Item6 — шестой элемент.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Элементы"), System.ComponentModel.DisplayName("Item6")]
        public string Prop_Item6 { get => _propItem6; set { _propItem6 = value; InvokePropertyChanged(this, "Prop_Item6"); } }

        private string _propItem7;
        /// <summary>Item7 — седьмой элемент. Максимально допустимый.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Элементы"), System.ComponentModel.DisplayName("Item7")]
        public string Prop_Item7 { get => _propItem7; set { _propItem7 = value; InvokePropertyChanged(this, "Prop_Item7"); } }

        // ── OUTPUT ─────────────────────────────────────────────────────────────

        private string _propResult;
        /// <summary>Созданный кортеж. Тип зависит от «Тип кортежа» и арности.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Кортеж")]
        public string Prop_Result { get => _propResult; set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); } }

        private string _propArity;
        /// <summary>Арность созданного кортежа (1–7).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Арность")]
        public string Prop_Arity { get => _propArity; set { _propArity = value; InvokePropertyChanged(this, "Prop_Arity"); } }

        private string _propKindName;
        /// <summary>Имя типа созданного кортежа: "Tuple`2" или "ValueTuple`2". Для диагностики.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Тип (имя)")]
        public string Prop_KindName { get => _propKindName; set { _propKindName = value; InvokePropertyChanged(this, "Prop_KindName"); } }

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

            sdkComponentIcon = "pack://application:,,,/Primo.MIA;component/images/sharp.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Kind",          PropertyType = PropertyTypes.OBJECT,   EditorType = ScriptEditorTypes.NONE, DataType = typeof(TupleKind), ToolTip = "ClassicTuple (System.Tuple) или ValueTuple (System.ValueTuple)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Item1",    PropertyType = PropertyTypes.SCRIPT,   EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Item1 — первый элемент (обязателен)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Item2",    PropertyType = PropertyTypes.SCRIPT,   EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Item2 — второй элемент", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Item3",    PropertyType = PropertyTypes.SCRIPT,   EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Item3 — третий элемент", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Item4",    PropertyType = PropertyTypes.SCRIPT,   EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Item4 — четвёртый элемент", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Item5",    PropertyType = PropertyTypes.SCRIPT,   EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Item5 — пятый элемент", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Item6",    PropertyType = PropertyTypes.SCRIPT,   EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Item6 — шестой элемент", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Item7",    PropertyType = PropertyTypes.SCRIPT,   EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Item7 — седьмой элемент (максимум)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Result",   PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Созданный кортеж", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Arity",    PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int),    ToolTip = "Арность (1–7)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_KindName", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(string), ToolTip = "Имя типа: Tuple`N или ValueTuple`N", IsReadOnly = false }
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
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка создания кортежа: {ex.Message}" };
            }
        }

        // ── ВАЛИДАЦИЯ ──────────────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            if (string.IsNullOrWhiteSpace(this.Prop_Item1))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Item1", Error = "Item1 обязателен — это первый элемент кортежа" });
            if (string.IsNullOrWhiteSpace(this.Prop_Result))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Кортеж", Error = "Выходная переменная «Кортеж» обязательна" });
            return ret;
        }
    }
}