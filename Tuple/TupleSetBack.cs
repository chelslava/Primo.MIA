using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{

    /// <summary>
    /// Заменяет один элемент кортежа, возвращая НОВЫЙ кортеж той же арности.
    ///
    /// Поскольку System.Tuple иммутабелен, активность:
    ///   1. Читает все текущие значения оригинального кортежа.
    ///   2. Заменяет указанный элемент новым значением.
    ///   3. Создаёт новый Tuple с теми же элементами кроме заменённого.
    ///   4. Оригинальный кортеж не изменяется.
    ///
    /// Новый кортеж можно записать в ту же переменную — эффект «изменения».
    /// </summary>
        public class TupleSetBack : PrimoComponentTO<TupleSet>
    {
        public override string GroupName { get => ActivityCategories.Tuples; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // ── INPUT ──────────────────────────────────────────────────────────────

        private string _propTuple;
        /// <summary>Исходный кортеж. Не изменяется — создаётся новый.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Tuple)]
        public string Prop_Tuple { get => _propTuple; set { _propTuple = value; InvokePropertyChanged(this, "Prop_Tuple"); } }

        private TupleItemIndex _index = TupleItemIndex.Item1;
        /// <summary>Номер заменяемого элемента: Item1–Item7. Должен быть ≤ арности кортежа.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_ItemIndex)]
        public TupleItemIndex Index
        {
            get => _index;
            set { _index = value; InvokePropertyChanged(this, "Index"); }
        }

        private string _propNewValue;
        /// <summary>Новое значение для указанного элемента. Принимает любой тип.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_NewValue)]
        public string Prop_NewValue { get => _propNewValue; set { _propNewValue = value; InvokePropertyChanged(this, "Prop_NewValue"); } }

        // ── OUTPUT ─────────────────────────────────────────────────────────────

        private string _propResult;
        /// <summary>
        /// Новый кортеж с заменённым элементом. Имеет ту же арность что и оригинал.
        /// Можно записать в ту же переменную что и исходный кортеж.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_NewTuple)]
        public string Prop_Result { get => _propResult; set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); } }

        // ── КОНСТРУКТОР ────────────────────────────────────────────────────────

        public TupleSetBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Кортеж: Заменить элемент";
            sdkComponentHelp =
                "Заменяет элемент кортежа, возвращая новый Tuple той же арности.\n\n" +
                "Tuple иммутабелен — активность создаёт новый кортеж.\n" +
                "Оригинальная переменная не изменяется, пока не перезапишете её.\n\n" +
                "Пример: Tuple(\"Иванов\", 42, true)\n" +
                "  Номер = Item2, Новое значение = 99\n" +
                "  → Новый кортеж: Tuple(\"Иванов\", 99, true)\n\n" +
                "Ошибка: Номер элемента > арности кортежа.";
            sdkComponentIcon = ActivityIcons.Tuple;

                        sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<object>("Prop_Tuple", "Исходный кортеж (не изменяется)"),
                PropertyBuilder.Enum<TupleItemIndex>("Index", "Номер заменяемого элемента"),
                PropertyBuilder.Script<object>("Prop_NewValue", "Новое значение элемента"),
                PropertyBuilder.Variable<object>("Prop_Result", "Новый кортеж с заменённым элементом")
            };

            InitClass(container);
        }

        // ── ДЕЙСТВИЕ ───────────────────────────────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                object tupleObj = GetPropertyValue<object>(this.Prop_Tuple, "Prop_Tuple", sd);
                object newValue = GetPropertyValue<object>(this.Prop_NewValue, "Prop_NewValue", sd);

                if (tupleObj == null) throw new ArgumentNullException("Prop_Tuple", "Кортеж не может быть null");

                int itemNum = (int)this.Index;

                // WithItem проверяет арность и выбрасывает InvalidOperationException при нарушении
                object newTuple = TupleHelper.WithItem(tupleObj, itemNum, newValue);

                SetVariableValue(this.Prop_Result, newTuple, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Item{itemNum} заменён на {newValue}. Создан новый кортеж."
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка замены элемента: {ex.Message}" };
            }
        }

        // ── ВАЛИДАЦИЯ ──────────────────────────────────────────────────────────

                public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Tuple, ActivityStrings.Field_Tuple, ActivityStrings.Error_TupleRequired);
            ret.ValidateRequired(this.Prop_NewValue, ActivityStrings.Field_NewValue, ActivityStrings.Error_NewValueRequired);
            ret.ValidateRequired(this.Prop_Result, ActivityStrings.Field_NewTuple, "Выходная переменная «Новый кортеж» обязательна");
            return ret;
        }
    }
}