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
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public override string GroupName { get => ActivityCategories.Dictionaries; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        private string _propDictionary;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Dictionary)]
        public string Prop_Dictionary
        {
            get => _propDictionary;
            set { _propDictionary = value; InvokePropertyChanged(this, "Prop_Dictionary"); }
        }

        private string _propCount;
        /// <summary>Количество пар ключ-значение в словаре</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Count)]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        private string _propKeys;
        /// <summary>Все ключи словаря в алфавитном порядке</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Keys)]
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
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Values)]
        public string Prop_Values
        {
            get => _propValues;
            set { _propValues = value; InvokePropertyChanged(this, "Prop_Values"); }
        }

        private string _propIsEmpty;
        /// <summary>True если словарь не содержит ни одного элемента</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_IsEmpty)]
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

            sdkComponentIcon = ActivityIcons.Dictionary;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<Dictionary<string, string>>("Prop_Dictionary", "Входной словарь Dictionary<string, string>"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество элементов в словаре"),
                PropertyBuilder.Variable<List<string>>("Prop_Keys", "Все ключи словаря (алфавитный порядок)"),
                PropertyBuilder.Variable<List<string>>("Prop_Values", "Все значения в порядке ключей"),
                PropertyBuilder.Variable<bool>("Prop_IsEmpty", "True если словарь пустой")
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

                List<string> keys = sorted.Select(p => p.Key).ToList();
                List<string> values = sorted.Select(p => p.Value).ToList();

                SetVariableValue(this.Prop_Count, dict.Count, sd);
                SetVariableValue(this.Prop_Keys, keys, sd);
                SetVariableValue(this.Prop_Values, values, sd);
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
            ret.ValidateRequired(this.Prop_Dictionary, ActivityStrings.Field_Dictionary, ActivityStrings.Error_DictionaryRequired);
            return ret;
        }
    }
}
