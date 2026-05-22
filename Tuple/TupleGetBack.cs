





using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Извлекает один элемент кортежа по его номеру (Item1–Item7).
    /// Запись в выходную переменную с сохранением исходного типа (как object).
    ///
    /// Если запрошенный номер превышает арность кортежа — активность завершается ошибкой.
    /// Например, запрос Item3 из Tuple&lt;string,int&gt; (арность 2) — ошибка.
    /// </summary>




    public class TupleGetBack : PrimoComponentTO<TupleGet>
    {
        public override string GroupName { get => ActivityCategories.Tuples; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // ── INPUT ──────────────────────────────────────────────────────────────

        private string _propTuple;
        /// <summary>Входной кортеж типа Tuple&lt;...&gt;.</summary>



        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Tuple)]
        public string Prop_Tuple { get => _propTuple; set { _propTuple = value; InvokePropertyChanged(this, "Prop_Tuple"); } }

        private TupleItemIndex _index = TupleItemIndex.Item1;
        /// <summary>Номер извлекаемого элемента: Item1–Item7.</summary>


        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_ItemIndex)]
        public TupleItemIndex Index
        {
            get => _index;
            set { _index = value; InvokePropertyChanged(this, "Index"); }
        }

        // ── OUTPUT ─────────────────────────────────────────────────────────────

        private string _propValue;
        /// <summary>
        /// Значение извлечённого элемента как object.
        /// Для работы с конкретным типом приведите переменную: (string)myVar, (int)myVar.
        /// </summary>



        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Value)]
        public string Prop_Value { get => _propValue; set { _propValue = value; InvokePropertyChanged(this, "Prop_Value"); } }

        private string _propTypeName;
        /// <summary>
        /// Имя типа извлечённого элемента (например, "String", "Int32", "Boolean").
        /// Полезно для диагностики и условных переходов.
        /// </summary>



        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_ValueType)]
        public string Prop_TypeName { get => _propTypeName; set { _propTypeName = value; InvokePropertyChanged(this, "Prop_TypeName"); } }

        // ── КОНСТРУКТОР ────────────────────────────────────────────────────────

        public TupleGetBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Кортеж: Получить элемент";
            sdkComponentHelp =
                "Извлекает один элемент кортежа по номеру Item1–Item7.\n\n" +
                "Выход «Значение» имеет тип object.\n" +
                "Для приведения к конкретному типу используйте выражение:\n" +
                "  (string)myVar    (int)myVar    Convert.ToInt32(myVar)\n\n" +
                "Выход «Тип значения» содержит имя типа (String, Int32 и т.д.).\n\n" +
                "Ошибка: если Номер элемента > арности кортежа.\n" +
                "Пример: Item3 из Tuple<string,int> → ошибка (арность 2).";

            sdkComponentIcon = ActivityIcons.Tuple;







            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<object>("Prop_Tuple", "Входной кортеж"),
                PropertyBuilder.Enum<TupleItemIndex>("Index", "Номер элемента Item1–Item7"),
                PropertyBuilder.Variable<object>("Prop_Value", "Извлечённое значение (object)"),
                PropertyBuilder.Variable<string>("Prop_TypeName", "Имя типа значения (String, Int32...)")
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
                int itemNum = (int)this.Index;

                if (arity < 0)
                    throw new InvalidOperationException($"Объект не является System.Tuple или System.ValueTuple: {tupleObj.GetType().Name}");
                if (itemNum > arity)
                    throw new InvalidOperationException(
                        $"Item{itemNum} не существует в кортеже арности {arity}. " +
                        $"Доступны: Item1–Item{arity}.");

                // Читаем значение через рефлексию
                object value = TupleHelper.GetItem(tupleObj, itemNum);

                SetVariableValue(this.Prop_Value, value, sd);
                SetVariableValue(this.Prop_TypeName, value?.GetType().Name ?? "null", sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Получен Item{itemNum} = {value} (тип: {value?.GetType().Name ?? "null"})"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка получения элемента: {ex.Message}" };
            }
        }

        // ── ВАЛИДАЦИЯ ──────────────────────────────────────────────────────────









        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Tuple, ActivityStrings.Field_Tuple, ActivityStrings.Error_TupleRequired);
            ret.ValidateRequired(this.Prop_Value, ActivityStrings.Field_Value, ActivityStrings.Error_ValueRequired);
            return ret;
        }
    }
}
