// =============================================================================
// DictionaryMerge.cs — активность «Словарь: Объединить».
//
// Объединяет два Dictionary<string, string> в один новый словарь.
// Стратегия при конфликте ключей задаётся через DictionaryMergeStrategy:
//   KeepFirst        — приоритет у первого словаря
//   KeepSecond       — приоритет у второго словаря
//   ThrowOnDuplicate — исключение при любом конфликте ключей
//
// Оба входных словаря не изменяются — возвращается новый экземпляр.
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
    /// Активность «Словарь: Объединить».
    /// Объединяет два Dictionary&lt;string, string&gt; в один новый словарь.
    /// Оба входных словаря остаются без изменений.
    /// </summary>
    public class DictionaryMergeBack : PrimoComponentTO<DictionaryMerge>
    {
        private const string CGroupName = "MIA" + WFPublishedElementBase.TREE_SEPARATOR + "Словари";
        public override string GroupName { get => CGroupName; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // =========================================================================
        // INPUT PROPERTIES
        // =========================================================================

        private string _propFirstDictionary;
        /// <summary>Первый словарь для объединения (базовый)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Первый словарь")]
        public string Prop_FirstDictionary
        {
            get => _propFirstDictionary;
            set { _propFirstDictionary = value; InvokePropertyChanged(this, "Prop_FirstDictionary"); }
        }

        private string _propSecondDictionary;
        /// <summary>Второй словарь для объединения</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Второй словарь")]
        public string Prop_SecondDictionary
        {
            get => _propSecondDictionary;
            set { _propSecondDictionary = value; InvokePropertyChanged(this, "Prop_SecondDictionary"); }
        }

        private DictionaryMergeStrategy _strategy = DictionaryMergeStrategy.KeepSecond;
        /// <summary>
        /// Стратегия при конфликте ключей.
        /// KeepFirst        — приоритет у первого словаря.
        /// KeepSecond       — приоритет у второго (значение перезаписывается).
        /// ThrowOnDuplicate — исключение при любом совпадении ключей.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Стратегия при конфликте")]
        public DictionaryMergeStrategy Strategy
        {
            get => _strategy;
            set { _strategy = value; InvokePropertyChanged(this, "Strategy"); }
        }

        // =========================================================================
        // OUTPUT PROPERTIES
        // =========================================================================

        private string _propResultDictionary;
        /// <summary>Объединённый словарь — результат слияния двух входных</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Результирующий словарь")]
        public string Prop_ResultDictionary
        {
            get => _propResultDictionary;
            set { _propResultDictionary = value; InvokePropertyChanged(this, "Prop_ResultDictionary"); }
        }

        private string _propCount;
        /// <summary>Количество элементов в объединённом словаре</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Количество элементов")]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        private string _propHadConflicts;
        /// <summary>True если при слиянии были обнаружены конфликтующие ключи</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Были конфликты ключей")]
        public string Prop_HadConflicts
        {
            get => _propHadConflicts;
            set { _propHadConflicts = value; InvokePropertyChanged(this, "Prop_HadConflicts"); }
        }

        private string _propConflictKeys;
        /// <summary>
        /// Список ключей которые присутствовали в обоих словарях.
        /// Заполняется всегда — удобно для диагностики слияния.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Конфликтующие ключи")]
        public string Prop_ConflictKeys
        {
            get => _propConflictKeys;
            set { _propConflictKeys = value; InvokePropertyChanged(this, "Prop_ConflictKeys"); }
        }

        // =========================================================================
        // КОНСТРУКТОР
        // =========================================================================

        public DictionaryMergeBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Словарь: Объединить";
            sdkComponentHelp =
                "Объединяет два Dictionary<string, string> в один новый словарь.\n" +
                "Оба входных словаря остаются без изменений.\n\n" +
                "── Стратегии при конфликте ключей ──────────────────────\n" +
                "KeepFirst        — приоритет у первого словаря\n" +
                "KeepSecond       — приоритет у второго (по умолчанию)\n" +
                "ThrowOnDuplicate — исключение при любом конфликте ключей\n\n" +
                "── Выходные параметры ──────────────────────────────────\n" +
                "Результирующий словарь — объединённый Dictionary<string, string>\n" +
                "Количество элементов   — размер результата\n" +
                "Были конфликты ключей  — bool: были ли совпадающие ключи\n" +
                "Конфликтующие ключи    — List<string> с именами конфликтных ключей";

            sdkComponentIcon = "pack://application:,,,/Primo.MIA;component/images/dict.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_FirstDictionary", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(Dictionary<string, string>),
                    ToolTip = "Первый (базовый) словарь для слияния", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_SecondDictionary", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(Dictionary<string, string>),
                    ToolTip = "Второй словарь для слияния", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Strategy", PropertyType = PropertyTypes.OBJECT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(DictionaryMergeStrategy),
                    ToolTip = "Стратегия при конфликте ключей: KeepFirst / KeepSecond / ThrowOnDuplicate",
                    IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_ResultDictionary", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(Dictionary<string, string>),
                    ToolTip = "Объединённый словарь", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Count", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(int),
                    ToolTip = "Количество элементов в результирующем словаре", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_HadConflicts", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool),
                    ToolTip = "True если при слиянии были конфликтующие ключи", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_ConflictKeys", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>),
                    ToolTip = "Список ключей присутствовавших в обоих словарях", IsReadOnly = false
                }
            };

            InitClass(container);
        }

        // =========================================================================
        // ОСНОВНОЕ ДЕЙСТВИЕ
        // =========================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var first  = GetPropertyValue<Dictionary<string, string>>(this.Prop_FirstDictionary,  "Prop_FirstDictionary",  sd);
                var second = GetPropertyValue<Dictionary<string, string>>(this.Prop_SecondDictionary, "Prop_SecondDictionary", sd);

                if (first  == null) throw new ArgumentNullException("Prop_FirstDictionary",  "Первый словарь не может быть null");
                if (second == null) throw new ArgumentNullException("Prop_SecondDictionary", "Второй словарь не может быть null");

                // Находим конфликтующие ключи через LINQ Intersect
                List<string> conflictKeys = first.Keys
                    .Intersect(second.Keys)
                    .OrderBy(k => k)
                    .ToList();

                if (conflictKeys.Any() && this.Strategy == DictionaryMergeStrategy.ThrowOnDuplicate)
                    throw new InvalidOperationException(
                        $"Конфликт при слиянии — дублирующиеся ключи: {string.Join(", ", conflictKeys)}. " +
                        "Смените стратегию на KeepFirst или KeepSecond.");

                Dictionary<string, string> result;

                if (this.Strategy == DictionaryMergeStrategy.KeepFirst)
                {
                    // Начинаем с копии первого, из второго добавляем только отсутствующие
                    result = first.ToDictionary(p => p.Key, p => p.Value);
                    second
                        .Where(p => !result.ContainsKey(p.Key))
                        .ToList()
                        .ForEach(p => result[p.Key] = p.Value);
                }
                else // KeepSecond — второй перекрывает первый
                {
                    // Начинаем с копии первого, второй накладываем поверх (все ключи)
                    result = first.ToDictionary(p => p.Key, p => p.Value);
                    second
                        .ToList()
                        .ForEach(p => result[p.Key] = p.Value);
                }

                SetVariableValue(this.Prop_ResultDictionary, result,              sd);
                SetVariableValue(this.Prop_Count,            result.Count,        sd);
                SetVariableValue(this.Prop_HadConflicts,     conflictKeys.Any(),  sd);
                SetVariableValue(this.Prop_ConflictKeys,     conflictKeys,        sd);

                string msg = conflictKeys.Any()
                    ? $"Слияние выполнено. Конфликтов: {conflictKeys.Count}, стратегия: {this.Strategy}"
                    : "Слияние выполнено без конфликтов";

                return new ExecutionResult { IsSuccess = true, SuccessMessage = msg };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка слияния: {ex.Message}" };
            }
        }

        // =========================================================================
        // ВАЛИДАЦИЯ
        // =========================================================================

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ValidateField(ret, this.Prop_FirstDictionary,  "Первый словарь",  "Первый словарь обязателен");
            ValidateField(ret, this.Prop_SecondDictionary, "Второй словарь", "Второй словарь обязателен");
            return ret;
        }

        private void ValidateField(ValidationResult result, string value, string fieldName, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
                result.Items.Add(new ValidationResult.ValidationItem() { PropertyName = fieldName, Error = errorMessage });
        }
    }
}
