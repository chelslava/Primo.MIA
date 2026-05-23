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
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Список: Срез».
    /// Возвращает подмножество элементов списка по различным правилам.
    /// Оригинальный список не изменяется.
    /// </summary>
    public class ListSliceBack : PrimoComponentTO<ListSlice>
    {
        public override string GroupName { get => ActivityCategories.Lists; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        private string _propList;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_List)]
        public string Prop_List
        {
            get => _propList;
            set { _propList = value; InvokePropertyChanged(this, nameof(Prop_List)); }
        }

        private ListSliceMode _mode = ListSliceMode.FirstN;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SliceMode)]
        public ListSliceMode Mode
        {
            get => _mode;
            set { _mode = value; InvokePropertyChanged(this, nameof(Mode)); }
        }

        private string _propN;
        /// <summary>Количество элементов (для FirstN, LastN, SkipFirst, SkipLast, EveryNth)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_N)]
        public string Prop_N
        {
            get => _propN;
            set { _propN = value; InvokePropertyChanged(this, nameof(Prop_N)); }
        }

        private string _propPage;
        /// <summary>Номер страницы, начиная с 1 (режим Page)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_Page)]
        public string Prop_Page
        {
            get => _propPage;
            set { _propPage = value; InvokePropertyChanged(this, nameof(Prop_Page)); }
        }

        private string _propPageSize;
        /// <summary>Размер страницы (режим Page)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_PageSize)]
        public string Prop_PageSize
        {
            get => _propPageSize;
            set { _propPageSize = value; InvokePropertyChanged(this, nameof(Prop_PageSize)); }
        }

        private string _propFromIndex;
        /// <summary>Начальный индекс включительно, нумерация с 0 (режим Range)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_FromIndex)]
        public string Prop_FromIndex
        {
            get => _propFromIndex;
            set { _propFromIndex = value; InvokePropertyChanged(this, nameof(Prop_FromIndex)); }
        }

        private string _propToIndex;
        /// <summary>Конечный индекс включительно, нумерация с 0 (режим Range)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_ToIndex)]
        public string Prop_ToIndex
        {
            get => _propToIndex;
            set { _propToIndex = value; InvokePropertyChanged(this, nameof(Prop_ToIndex)); }
        }

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Result)]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
        }

        private string _propCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Count)]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, nameof(Prop_Count)); }
        }

        private string _propTotalPages;
        /// <summary>Общее количество страниц (заполняется только в режиме Page)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_TotalPages)]
        public string Prop_TotalPages
        {
            get => _propTotalPages;
            set { _propTotalPages = value; InvokePropertyChanged(this, nameof(Prop_TotalPages)); }
        }

        public ListSliceBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Список: Срез";
            sdkComponentHelp =
                "Возвращает подмножество элементов списка.\n\n" +
                "FirstN    — первые N элементов\n" +
                "LastN     — последние N элементов\n" +
                "SkipFirst — пропустить первые N, вернуть остаток\n" +
                "SkipLast  — пропустить последние N, вернуть начало\n" +
                "Page      — пагинация: страница Page, размер PageSize\n" +
                "Range     — элементы с индекса FromIndex до ToIndex (вкл.)\n" +
                "EveryNth  — каждый N-й элемент (шаг N)";
            sdkComponentIcon = ActivityIcons.List;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<List<string>>("Prop_List", "Входной список"),
                PropertyBuilder.Enum<ListSliceMode>("Mode", "Способ среза"),
                PropertyBuilder.Script<int>("Prop_N", "N — кол-во элементов или шаг"),
                PropertyBuilder.Script<int>("Prop_Page", "Номер страницы (с 1) для режима Page"),
                PropertyBuilder.Script<int>("Prop_PageSize", "Размер страницы для режима Page"),
                PropertyBuilder.Script<int>("Prop_FromIndex", "Начальный индекс (с 0) для режима Range"),
                PropertyBuilder.Script<int>("Prop_ToIndex", "Конечный индекс (включ.) для режима Range"),
                PropertyBuilder.Variable<List<string>>("Prop_Result", "Срез списка"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество элементов в результате"),
                PropertyBuilder.Variable<int>("Prop_TotalPages", "Всего страниц (только для режима Page)")
            };

            InitClass(container);
            this.Prop_N = "10";
            this.Prop_Page = "1";
            this.Prop_PageSize = "10";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var logic = new ListSliceLogic();
                var list = GetPropertyValue<List<string>>(this.Prop_List, "Prop_List", sd);
                if (list == null) throw new ArgumentNullException("Prop_List", "Список не может быть null");

                int n = int.TryParse(GetPropertyValue<string>(this.Prop_N, "Prop_N", sd), out int nv) ? nv : 10;
                int page = int.TryParse(GetPropertyValue<string>(this.Prop_Page, "Prop_Page", sd), out int pv) ? pv : 1;
                int pageSize = int.TryParse(GetPropertyValue<string>(this.Prop_PageSize, "Prop_PageSize", sd), out int psv) ? psv : 10;
                int fromIndex = int.TryParse(GetPropertyValue<string>(this.Prop_FromIndex, "Prop_FromIndex", sd), out int fiv) ? fiv : 0;
                int toIndex = int.TryParse(GetPropertyValue<string>(this.Prop_ToIndex, "Prop_ToIndex", sd), out int tiv) ? tiv : list.Count - 1;

                var slice = logic.SliceDetailed(list, this.Mode, n, page, pageSize, fromIndex, toIndex, n);

                SetVariableValue(this.Prop_Result, slice.Result, sd);
                SetVariableValue(this.Prop_Count, slice.Result.Count, sd);
                SetVariableValue(this.Prop_TotalPages, slice.TotalPages, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Срез: {slice.Result.Count} из {list.Count} элементов" };
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
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка среза: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_List, ActivityStrings.Field_List, ActivityStrings.Error_ListRequired);
            return ret;
        }
    }
}
