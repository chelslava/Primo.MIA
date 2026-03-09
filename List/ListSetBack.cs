// =============================================================================
// ListSetAggregateSlice.cs — три активности:
//
//   ListSetBack       — «Список: Теория множеств»
//     Union, Intersect, Except, Distinct, SymmetricDiff
//
//   ListAggregateBack — «Список: Агрегация»
//     Count, CountDistinct, Sum, Min, Max, Average, Join (конкатенация строк)
//
//   ListSliceBack     — «Список: Срез»
//     Take, Skip, Page (пагинация), Range (от..до), First/Last N
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.Enums;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Список: Теория множеств».
    /// Выполняет операции над двумя списками как над множествами.
    /// Оба входных списка не изменяются.
    /// </summary>
    public class ListSetBack : PrimoComponentTO<ListSet>
    {
                public override string GroupName { get => ActivityCategories.Lists; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        private string _propListA;
        /// <summary>Первый список (множество A). Обязателен для всех операций.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_ListA)]
        public string Prop_ListA
        {
            get => _propListA;
            set { _propListA = value; InvokePropertyChanged(this, "Prop_ListA"); }
        }

        private string _propListB;
        /// <summary>Второй список (множество B). Не нужен для операции Distinct.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_ListB)]
        public string Prop_ListB
        {
            get => _propListB;
            set { _propListB = value; InvokePropertyChanged(this, "Prop_ListB"); }
        }

        private ListSetOperation _operation = ListSetOperation.Union;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Operation)]
        public ListSetOperation Operation
        {
            get => _operation;
            set { _operation = value; InvokePropertyChanged(this, "Operation"); }
        }

        private bool _caseSensitive = false;
        /// <summary>Учитывать регистр при сравнении элементов</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CaseSensitive)]
        public bool Prop_CaseSensitive
        {
            get => _caseSensitive;
            set { _caseSensitive = value; InvokePropertyChanged(this, "Prop_CaseSensitive"); }
        }

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Result)]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); }
        }

        private string _propCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Count)]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        public ListSetBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Список: Теория множеств";
            sdkComponentHelp =
                "Операции над двумя списками как над математическими множествами.\n\n" +
                "Union         — A∪B: все уникальные элементы\n" +
                "Intersect     — A∩B: только общие элементы\n" +
                "Except        — A∖B: в A но не в B\n" +
                "ExceptReverse — B∖A: в B но не в A\n" +
                "SymmetricDiff — только в одном из списков (не в обоих)\n" +
                "Distinct      — уникальные элементы A (дубли удаляются)";
            sdkComponentIcon = ActivityIcons.List;

                        sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<List<string>>("Prop_ListA", "Список A"),
                PropertyBuilder.Script<List<string>>("Prop_ListB", "Список B (не нужен для Distinct)"),
                PropertyBuilder.Enum<ListSetOperation>("Operation", "Операция над множествами"),
                PropertyBuilder.BooleanObject("Prop_CaseSensitive", "Учитывать регистр при сравнении"),
                PropertyBuilder.Variable<List<string>>("Prop_Result", "Результирующий список"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество элементов в результате")
            };

            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var listA = GetPropertyValue<List<string>>(this.Prop_ListA, "Prop_ListA", sd);
                var listB = GetPropertyValue<List<string>>(this.Prop_ListB, "Prop_ListB", sd);

                if (listA == null) throw new ArgumentNullException("Prop_ListA", "Список A не может быть null");

                var comparer = this.Prop_CaseSensitive
                    ? StringComparer.Ordinal
                    : StringComparer.OrdinalIgnoreCase;

                List<string> result;

                switch (this.Operation)
                {
                    case ListSetOperation.Union:
                        if (listB == null) throw new ArgumentNullException("Prop_ListB", "Список B обязателен для Union");
                        result = listA.Union(listB, comparer).ToList();
                        break;

                    case ListSetOperation.Intersect:
                        if (listB == null) throw new ArgumentNullException("Prop_ListB", "Список B обязателен для Intersect");
                        result = listA.Intersect(listB, comparer).ToList();
                        break;

                    case ListSetOperation.Except:
                        if (listB == null) throw new ArgumentNullException("Prop_ListB", "Список B обязателен для Except");
                        result = listA.Except(listB, comparer).ToList();
                        break;

                    case ListSetOperation.ExceptReverse:
                        if (listB == null) throw new ArgumentNullException("Prop_ListB", "Список B обязателен для ExceptReverse");
                        result = listB.Except(listA, comparer).ToList();
                        break;

                    case ListSetOperation.SymmetricDiff:
                        if (listB == null) throw new ArgumentNullException("Prop_ListB", "Список B обязателен для SymmetricDiff");
                        // (A∪B) ∖ (A∩B)
                        var union     = listA.Union(listB, comparer);
                        var intersect = new HashSet<string>(listA.Intersect(listB, comparer), comparer);
                        result = union.Where(x => !intersect.Contains(x)).ToList();
                        break;

                    case ListSetOperation.Distinct:
                        result = listA.Distinct(comparer).ToList();
                        break;

                    default:
                        throw new InvalidOperationException($"Неизвестная операция: {this.Operation}");
                }

                SetVariableValue(this.Prop_Result, result,       sd);
                SetVariableValue(this.Prop_Count,  result.Count, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"{this.Operation}: {result.Count} элементов" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
                        var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_ListA, ActivityStrings.Field_ListA, ActivityStrings.Error_ListRequired);
            if (this.Operation != ListSetOperation.Distinct)
                ret.ValidateRequired(this.Prop_ListB, ActivityStrings.Field_ListB, ActivityStrings.Error_ListBRequired);
            return ret;
        }
    }

}
