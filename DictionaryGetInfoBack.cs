// =============================================================================
// DictionaryOperationsSmall.cs — четыре компактные активности:
//
//   DictionaryRemoveKeyBack    — Словарь: Удалить ключ
//   DictionaryContainsKeyBack  — Словарь: Проверить ключ
//   DictionaryContainsValueBack — Словарь: Проверить значение
//   DictionaryGetInfoBack      — Словарь: Информация (Count + Keys + Values)
//
// Каждая активность делает ровно одно действие и не изменяет входной словарь
// (кроме RemoveKey — возвращает новую копию без ключа).
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
    /// Активность «Словарь: Информация».
    /// За один шаг возвращает полную информацию о словаре:
    /// количество элементов, список ключей и список значений.
    /// Не изменяет словарь.
    /// </summary>
    public class DictionaryGetInfoBack : PrimoComponentTO<DictionaryGetInfo>
    {
        private const string CGroupName = "MIA" + WFPublishedElementBase.TREE_SEPARATOR + "Словари";
        public override string GroupName { get => CGroupName; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        private string _propDictionary;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Словарь")]
        public string Prop_Dictionary
        {
            get => _propDictionary;
            set { _propDictionary = value; InvokePropertyChanged(this, "Prop_Dictionary"); }
        }

        private string _propCount;
        /// <summary>Количество пар ключ-значение в словаре</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Количество элементов")]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        private string _propKeys;
        /// <summary>Все ключи словаря в алфавитном порядке</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Список ключей")]
        public string Prop_Keys
        {
            get => _propKeys;
            set { _propKeys = value; InvokePropertyChanged(this, "Prop_Keys"); }
        }

        private string _propValues;
        /// <summary>
        /// Все значения словаря в порядке алфавитной сортировки ключей.
        /// Порядок значений соответствует порядку ключей в Prop_Keys.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Список значений")]
        public string Prop_Values
        {
            get => _propValues;
            set { _propValues = value; InvokePropertyChanged(this, "Prop_Values"); }
        }

        private string _propIsEmpty;
        /// <summary>True если словарь не содержит ни одного элемента</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Словарь пуст")]
        public string Prop_IsEmpty
        {
            get => _propIsEmpty;
            set { _propIsEmpty = value; InvokePropertyChanged(this, "Prop_IsEmpty"); }
        }

        public DictionaryGetInfoBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Словарь: Информация";
            sdkComponentHelp =
                "Возвращает полную информацию о Dictionary<string, string>:\n" +
                "количество элементов, список ключей и список значений.\n" +
                "Ключи и значения возвращаются в алфавитном порядке ключей.\n" +
                "Не изменяет словарь.\n\n" +
                "Входные параметры:\n" +
                "  Словарь* — Dictionary<string, string>\n\n" +
                "Выходные параметры:\n" +
                "  Количество элементов — int, размер словаря\n" +
                "  Список ключей        — List<string> в алфавитном порядке\n" +
                "  Список значений      — List<string> в том же порядке что и ключи\n" +
                "  Словарь пуст         — true если Count == 0";

            sdkComponentIcon = "pack://application:,,,/Primo.MIA;component/images/dict.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Dictionary", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(Dictionary<string, string>),
                    ToolTip = "Входной словарь Dictionary<string, string>", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Count", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(int),
                    ToolTip = "Количество элементов в словаре", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Keys", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>),
                    ToolTip = "Все ключи словаря (алфавитный порядок)", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Values", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>),
                    ToolTip = "Все значения в порядке ключей", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_IsEmpty", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool),
                    ToolTip = "True если словарь пустой", IsReadOnly = false
                }
            };

            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var dict = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary, "Prop_Dictionary", sd);

                if (dict == null)
                    throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null");

                // Сортируем один раз — и ключи и значения в одном порядке
                var sorted = dict.OrderBy(p => p.Key).ToList();

                List<string> keys   = sorted.Select(p => p.Key).ToList();
                List<string> values = sorted.Select(p => p.Value).ToList();

                SetVariableValue(this.Prop_Count,   dict.Count,      sd);
                SetVariableValue(this.Prop_Keys,    keys,            sd);
                SetVariableValue(this.Prop_Values,  values,          sd);
                SetVariableValue(this.Prop_IsEmpty, dict.Count == 0, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Словарь содержит {dict.Count} элементов" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            if (string.IsNullOrWhiteSpace(this.Prop_Dictionary))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Словарь", Error = "Словарь обязателен" });
            return ret;
        }
    }
}
