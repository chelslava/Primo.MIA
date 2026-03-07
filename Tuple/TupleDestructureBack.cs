using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using System;
using System.Collections.Generic;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Раскладывает все элементы кортежа в отдельные переменные за один шаг.
    /// Аналог C# 7: var (a, b, c) = tuple — для .NET Framework.
    ///
    /// Прозрачно работает с System.Tuple и System.ValueTuple:
    ///   Tuple      → читает через PropertyInfo (свойство)
    ///   ValueTuple → читает через FieldInfo (публичное поле)
    ///
    /// Незаполненные Out-переменные пропускаются.
    /// Номер > арности кортежа — пропускается без ошибки.
    /// </summary>
    public class TupleDestructureBack : PrimoComponentTO<TupleDestructure>
    {
        private const string CGroupName = "MIA" + WFPublishedElementBase.TREE_SEPARATOR + "Кортежи";
        public override string GroupName { get => CGroupName; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // ── INPUT ──────────────────────────────────────────────────────────────

        private string _propTuple;
        /// <summary>Входной кортеж — System.Tuple или System.ValueTuple любой арности.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Кортеж")]
        public string Prop_Tuple { get => _propTuple; set { _propTuple = value; InvokePropertyChanged(this, "Prop_Tuple"); } }

        // ── OUTPUT: переменные для каждого элемента ────────────────────────────

        private string _propOut1;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Элементы"), System.ComponentModel.DisplayName("Out: Item1")]
        public string Prop_Out1 { get => _propOut1; set { _propOut1 = value; InvokePropertyChanged(this, "Prop_Out1"); } }

        private string _propOut2;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Элементы"), System.ComponentModel.DisplayName("Out: Item2")]
        public string Prop_Out2 { get => _propOut2; set { _propOut2 = value; InvokePropertyChanged(this, "Prop_Out2"); } }

        private string _propOut3;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Элементы"), System.ComponentModel.DisplayName("Out: Item3")]
        public string Prop_Out3 { get => _propOut3; set { _propOut3 = value; InvokePropertyChanged(this, "Prop_Out3"); } }

        private string _propOut4;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Элементы"), System.ComponentModel.DisplayName("Out: Item4")]
        public string Prop_Out4 { get => _propOut4; set { _propOut4 = value; InvokePropertyChanged(this, "Prop_Out4"); } }

        private string _propOut5;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Элементы"), System.ComponentModel.DisplayName("Out: Item5")]
        public string Prop_Out5 { get => _propOut5; set { _propOut5 = value; InvokePropertyChanged(this, "Prop_Out5"); } }

        private string _propOut6;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Элементы"), System.ComponentModel.DisplayName("Out: Item6")]
        public string Prop_Out6 { get => _propOut6; set { _propOut6 = value; InvokePropertyChanged(this, "Prop_Out6"); } }

        private string _propOut7;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category("Элементы"), System.ComponentModel.DisplayName("Out: Item7")]
        public string Prop_Out7 { get => _propOut7; set { _propOut7 = value; InvokePropertyChanged(this, "Prop_Out7"); } }

        private string _propArity;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Арность")]
        public string Prop_Arity { get => _propArity; set { _propArity = value; InvokePropertyChanged(this, "Prop_Arity"); } }

        private string _propKindName;
        /// <summary>Имя типа кортежа: "Tuple`3" или "ValueTuple`3". Для диагностики.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Тип (имя)")]
        public string Prop_KindName { get => _propKindName; set { _propKindName = value; InvokePropertyChanged(this, "Prop_KindName"); } }

        // ── КОНСТРУКТОР ────────────────────────────────────────────────────────

        public TupleDestructureBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Кортеж: Деструктуризация";
            sdkComponentHelp =
                "Раскладывает все элементы кортежа в отдельные переменные.\n" +
                "Аналог C# 7: var (a, b, c) = tuple — для .NET Framework.\n\n" +
                "Работает прозрачно с System.Tuple и System.ValueTuple.\n\n" +
                "Правила:\n" +
                "  • Задайте только нужные Out-переменные.\n" +
                "  • Номер Out > арности кортежа — пропускается без ошибки.\n" +
                "  • «Тип (имя)»: \"Tuple`3\" или \"ValueTuple`3\".";

            sdkComponentIcon = "pack://application:,,,/Primo.MIA;component/images/sharp.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Tuple",    PropertyType = PropertyTypes.SCRIPT,   EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Входной кортеж (Tuple или ValueTuple)", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Out1",     PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Переменная для Item1", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Out2",     PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Переменная для Item2", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Out3",     PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Переменная для Item3", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Out4",     PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Переменная для Item4", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Out5",     PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Переменная для Item5", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Out6",     PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Переменная для Item6", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Out7",     PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(object), ToolTip = "Переменная для Item7", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Arity",    PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int),    ToolTip = "Арность кортежа", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_KindName", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(string), ToolTip = "Имя типа кортежа (Tuple`N или ValueTuple`N)", IsReadOnly = false }
            };

            InitClass(container);
        }

        // ── ДЕЙСТВИЕ ───────────────────────────────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                object tupleObj = GetPropertyValue<object>(this.Prop_Tuple, "Prop_Tuple", sd);
                if (tupleObj == null) throw new ArgumentNullException("Prop_Tuple", "Кортеж не может быть null");

                int arity = TupleHelper.GetArity(tupleObj);
                if (arity < 0)
                    throw new InvalidOperationException(
                        $"Объект не является System.Tuple или System.ValueTuple: {tupleObj.GetType().Name}");

                string[] outProps = new string[] { null, this.Prop_Out1, this.Prop_Out2, this.Prop_Out3,
                                                         this.Prop_Out4, this.Prop_Out5, this.Prop_Out6, this.Prop_Out7 };
                int extracted = 0;

                for (int i = 1; i <= arity; i++)
                {
                    string outProp = outProps[i];
                    if (string.IsNullOrWhiteSpace(outProp)) continue;

                    // TupleHelper.GetItem прозрачно использует Property или Field
                    object value = TupleHelper.GetItem(tupleObj, i);
                    SetVariableValue(outProp, value, sd);
                    extracted++;
                }

                SetVariableValue(this.Prop_Arity, arity, sd);
                SetVariableValue(this.Prop_KindName, tupleObj.GetType().Name, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Деструктуризован {tupleObj.GetType().Name} ({arity} эл.), извлечено: {extracted}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка деструктуризации: {ex.Message}" };
            }
        }

        // ── ВАЛИДАЦИЯ ──────────────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            if (string.IsNullOrWhiteSpace(this.Prop_Tuple))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Кортеж", Error = "Кортеж обязателен" });

            bool anyOut = !string.IsNullOrWhiteSpace(this.Prop_Out1) || !string.IsNullOrWhiteSpace(this.Prop_Out2)
                       || !string.IsNullOrWhiteSpace(this.Prop_Out3) || !string.IsNullOrWhiteSpace(this.Prop_Out4)
                       || !string.IsNullOrWhiteSpace(this.Prop_Out5) || !string.IsNullOrWhiteSpace(this.Prop_Out6)
                       || !string.IsNullOrWhiteSpace(this.Prop_Out7);
            if (!anyOut)
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Out:Item1", Error = "Задайте хотя бы одну выходную переменную (Out:Item1 … Out:Item7)" });

            return ret;
        }
    }
}