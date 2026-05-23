using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

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
        public override string GroupName { get => ActivityCategories.Tuples; protected set { } }

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
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Tuple)]
        public string Prop_Tuple { get => _propTuple; set { _propTuple = value; InvokePropertyChanged(this, nameof(Prop_Tuple)); } }

        // ── OUTPUT: переменные для каждого элемента ────────────────────────────

        private string _propOut1;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Elements)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_OutItem1)]
        public string Prop_Out1 { get => _propOut1; set { _propOut1 = value; InvokePropertyChanged(this, nameof(Prop_Out1)); } }

        private string _propOut2;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Elements)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_OutItem2)]
        public string Prop_Out2 { get => _propOut2; set { _propOut2 = value; InvokePropertyChanged(this, nameof(Prop_Out2)); } }

        private string _propOut3;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Elements)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_OutItem3)]
        public string Prop_Out3 { get => _propOut3; set { _propOut3 = value; InvokePropertyChanged(this, nameof(Prop_Out3)); } }

        private string _propOut4;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Elements)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_OutItem4)]
        public string Prop_Out4 { get => _propOut4; set { _propOut4 = value; InvokePropertyChanged(this, nameof(Prop_Out4)); } }

        private string _propOut5;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Elements)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_OutItem5)]
        public string Prop_Out5 { get => _propOut5; set { _propOut5 = value; InvokePropertyChanged(this, nameof(Prop_Out5)); } }

        private string _propOut6;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Elements)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_OutItem6)]
        public string Prop_Out6 { get => _propOut6; set { _propOut6 = value; InvokePropertyChanged(this, nameof(Prop_Out6)); } }

        private string _propOut7;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Elements)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_OutItem7)]
        public string Prop_Out7 { get => _propOut7; set { _propOut7 = value; InvokePropertyChanged(this, nameof(Prop_Out7)); } }

        private string _propArity;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Arity)]
        public string Prop_Arity { get => _propArity; set { _propArity = value; InvokePropertyChanged(this, nameof(Prop_Arity)); } }

        private string _propKindName;
        /// <summary>Имя типа кортежа: "Tuple`3" или "ValueTuple`3". Для диагностики.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_KindName)]
        public string Prop_KindName { get => _propKindName; set { _propKindName = value; InvokePropertyChanged(this, nameof(Prop_KindName)); } }

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

            sdkComponentIcon = ActivityIcons.Tuple;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<object>("Prop_Tuple", "Входной кортеж (Tuple или ValueTuple)"),
                PropertyBuilder.Variable<object>("Prop_Out1", "Переменная для Item1"),
                PropertyBuilder.Variable<object>("Prop_Out2", "Переменная для Item2"),
                PropertyBuilder.Variable<object>("Prop_Out3", "Переменная для Item3"),
                PropertyBuilder.Variable<object>("Prop_Out4", "Переменная для Item4"),
                PropertyBuilder.Variable<object>("Prop_Out5", "Переменная для Item5"),
                PropertyBuilder.Variable<object>("Prop_Out6", "Переменная для Item6"),
                PropertyBuilder.Variable<object>("Prop_Out7", "Переменная для Item7"),
                PropertyBuilder.Variable<int>("Prop_Arity", "Арность кортежа"),
                PropertyBuilder.Variable<string>("Prop_KindName", "Имя типа кортежа (Tuple`N или ValueTuple`N)")
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
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка деструктуризации: {ex.Message}" };
            }
        }

        // ── ВАЛИДАЦИЯ ──────────────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Tuple, ActivityStrings.Field_Tuple, ActivityStrings.Error_TupleRequired);

            bool anyOut = !string.IsNullOrWhiteSpace(this.Prop_Out1) || !string.IsNullOrWhiteSpace(this.Prop_Out2)
                       || !string.IsNullOrWhiteSpace(this.Prop_Out3) || !string.IsNullOrWhiteSpace(this.Prop_Out4)
                       || !string.IsNullOrWhiteSpace(this.Prop_Out5) || !string.IsNullOrWhiteSpace(this.Prop_Out6)
                       || !string.IsNullOrWhiteSpace(this.Prop_Out7);
            if (!anyOut)
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = ActivityStrings.Field_OutItem1, Error = "Задайте хотя бы одну выходную переменную (Out:Item1 … Out:Item7)" });

            return ret;
        }
    }
}