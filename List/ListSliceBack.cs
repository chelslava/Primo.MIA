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
using System;
using System.Collections.Generic;
using System.Linq;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Список: Срез».
    /// Возвращает подмножество элементов списка по различным правилам.
    /// Оригинальный список не изменяется.
    /// </summary>
    public class ListSliceBack : PrimoComponentTO<ListSlice>
    {
        private const string CGroupName = "MIA" + WFPublishedElementBase.TREE_SEPARATOR + "Списки";
        public override string GroupName { get => CGroupName; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        private string _propList;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Список")]
        public string Prop_List
        {
            get => _propList;
            set { _propList = value; InvokePropertyChanged(this, "Prop_List"); }
        }

        private ListSliceMode _mode = ListSliceMode.FirstN;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Способ среза")]
        public ListSliceMode Mode
        {
            get => _mode;
            set { _mode = value; InvokePropertyChanged(this, "Mode"); }
        }

        private string _propN;
        /// <summary>Количество элементов (для FirstN, LastN, SkipFirst, SkipLast, EveryNth)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Параметры"), System.ComponentModel.DisplayName("N (кол-во / шаг)")]
        public string Prop_N
        {
            get => _propN;
            set { _propN = value; InvokePropertyChanged(this, "Prop_N"); }
        }

        private string _propPage;
        /// <summary>Номер страницы, начиная с 1 (режим Page)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Параметры"), System.ComponentModel.DisplayName("Номер страницы")]
        public string Prop_Page
        {
            get => _propPage;
            set { _propPage = value; InvokePropertyChanged(this, "Prop_Page"); }
        }

        private string _propPageSize;
        /// <summary>Размер страницы (режим Page)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Параметры"), System.ComponentModel.DisplayName("Размер страницы")]
        public string Prop_PageSize
        {
            get => _propPageSize;
            set { _propPageSize = value; InvokePropertyChanged(this, "Prop_PageSize"); }
        }

        private string _propFromIndex;
        /// <summary>Начальный индекс включительно, нумерация с 0 (режим Range)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Параметры"), System.ComponentModel.DisplayName("Индекс от (включ.)")]
        public string Prop_FromIndex
        {
            get => _propFromIndex;
            set { _propFromIndex = value; InvokePropertyChanged(this, "Prop_FromIndex"); }
        }

        private string _propToIndex;
        /// <summary>Конечный индекс включительно, нумерация с 0 (режим Range)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Параметры"), System.ComponentModel.DisplayName("Индекс до (включ.)")]
        public string Prop_ToIndex
        {
            get => _propToIndex;
            set { _propToIndex = value; InvokePropertyChanged(this, "Prop_ToIndex"); }
        }

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Результат")]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); }
        }

        private string _propCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Количество")]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        private string _propTotalPages;
        /// <summary>Общее количество страниц (заполняется только в режиме Page)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Всего страниц")]
        public string Prop_TotalPages
        {
            get => _propTotalPages;
            set { _propTotalPages = value; InvokePropertyChanged(this, "Prop_TotalPages"); }
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
            sdkComponentIcon = "pack://application:,,,/Primo.MIA;component/images/list.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_List", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>), ToolTip = "Входной список", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Mode", PropertyType = PropertyTypes.OBJECT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(ListSliceMode), ToolTip = "Способ среза", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_N", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "N — кол-во элементов или шаг", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Page", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Номер страницы (с 1) для режима Page", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_PageSize", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Размер страницы для режима Page", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_FromIndex", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Начальный индекс (с 0) для режима Range", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_ToIndex", PropertyType = PropertyTypes.SCRIPT, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Конечный индекс (включ.) для режима Range", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Result", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>), ToolTip = "Срез списка", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_Count", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Количество элементов в результате", IsReadOnly = false },
                new LTools.Common.Helpers.WFHelper.PropertiesItem() { PropName = "Prop_TotalPages", PropertyType = PropertyTypes.VARIABLE, EditorType = ScriptEditorTypes.NONE, DataType = typeof(int), ToolTip = "Всего страниц (только для режима Page)", IsReadOnly = false }
            };

            InitClass(container);
            this.Prop_N        = "10";
            this.Prop_Page     = "1";
            this.Prop_PageSize = "10";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var list = GetPropertyValue<List<string>>(this.Prop_List, "Prop_List", sd);
                if (list == null) throw new ArgumentNullException("Prop_List", "Список не может быть null");

                int n         = int.TryParse(this.Prop_N,         out int nv)   ? nv   : 10;
                int page      = int.TryParse(this.Prop_Page,      out int pv)   ? pv   : 1;
                int pageSize  = int.TryParse(this.Prop_PageSize,  out int psv)  ? psv  : 10;
                int fromIndex = int.TryParse(this.Prop_FromIndex, out int fiv)  ? fiv  : 0;
                int toIndex   = int.TryParse(this.Prop_ToIndex,   out int tiv)  ? tiv  : list.Count - 1;

                List<string> result;
                int totalPages = 0;

                switch (this.Mode)
                {
                    case ListSliceMode.FirstN:
                        result = list.Take(n).ToList();
                        break;

                    case ListSliceMode.LastN:
                        result = list.Skip(Math.Max(0, list.Count - n)).ToList();
                        break;

                    case ListSliceMode.SkipFirst:
                        result = list.Skip(n).ToList();
                        break;

                    case ListSliceMode.SkipLast:
                        result = list.Take(Math.Max(0, list.Count - n)).ToList();
                        break;

                    case ListSliceMode.Page:
                        if (page < 1) throw new ArgumentException("Номер страницы должен быть ≥ 1");
                        if (pageSize < 1) throw new ArgumentException("Размер страницы должен быть ≥ 1");
                        totalPages = (int)Math.Ceiling((double)list.Count / pageSize);
                        result = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                        break;

                    case ListSliceMode.Range:
                        fromIndex = Math.Max(0, fromIndex);
                        toIndex   = Math.Min(list.Count - 1, toIndex);
                        if (fromIndex > toIndex) { result = new List<string>(); break; }
                        result = list.Skip(fromIndex).Take(toIndex - fromIndex + 1).ToList();
                        break;

                    case ListSliceMode.EveryNth:
                        if (n < 1) throw new ArgumentException("Шаг должен быть ≥ 1");
                        // Select с индексом, фильтруем по i % n == 0
                        result = list
                            .Select((item, idx) => new { item, idx })
                            .Where(x => x.idx % n == 0)
                            .Select(x => x.item)
                            .ToList();
                        break;

                    default:
                        throw new InvalidOperationException($"Неизвестный режим: {this.Mode}");
                }

                SetVariableValue(this.Prop_Result,     result,       sd);
                SetVariableValue(this.Prop_Count,      result.Count, sd);
                SetVariableValue(this.Prop_TotalPages, totalPages,   sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Срез: {result.Count} из {list.Count} элементов" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка среза: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            if (string.IsNullOrWhiteSpace(this.Prop_List))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Список", Error = "Список обязателен" });
            return ret;
        }
    }
}
