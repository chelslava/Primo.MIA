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
    /// Активность «Словарь: Удалить ключ».
    /// Возвращает новую копию словаря без указанного ключа.
    /// Оригинальный словарь не изменяется.
    /// Если ключ не найден — поведение задаётся флагом Prop_ThrowIfNotFound.
    /// </summary>
        public class DictionaryRemoveKeyBack : PrimoComponentTO<DictionaryOperationsSmall>
    {
        public override string GroupName { get => ActivityCategories.Dictionaries; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        private string _propDictionary;
        /// <summary>Входной словарь Dictionary&lt;string, string&gt;</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Dictionary)]
        public string Prop_Dictionary
        {
            get => _propDictionary;
            set { _propDictionary = value; InvokePropertyChanged(this, "Prop_Dictionary"); }
        }

        private string _propKey;
        /// <summary>Ключ который нужно удалить из словаря</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Key)]
        public string Prop_Key
        {
            get => _propKey;
            set { _propKey = value; InvokePropertyChanged(this, "Prop_Key"); }
        }

        private bool _throwIfNotFound = false;
        /// <summary>
        /// Если true — выбросить KeyNotFoundException если ключ не найден.
        /// Если false — вернуть копию словаря без изменений.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_ThrowIfNotFound)]
        public bool Prop_ThrowIfNotFound
        {
            get => _throwIfNotFound;
            set { _throwIfNotFound = value; InvokePropertyChanged(this, "Prop_ThrowIfNotFound"); }
        }

        private string _propResultDictionary;
        /// <summary>Новый словарь без удалённого ключа</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ResultDictionary)]
        public string Prop_ResultDictionary
        {
            get => _propResultDictionary;
            set { _propResultDictionary = value; InvokePropertyChanged(this, "Prop_ResultDictionary"); }
        }

        private string _propRemoved;
        /// <summary>True если ключ был найден и удалён, False если ключа не было</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Removed)]
        public string Prop_Removed
        {
            get => _propRemoved;
            set { _propRemoved = value; InvokePropertyChanged(this, "Prop_Removed"); }
        }

        public DictionaryRemoveKeyBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Словарь: Удалить ключ";
            sdkComponentHelp =
                "Удаляет ключ из Dictionary<string, string>.\n" +
                "Возвращает новую копию словаря — оригинал не изменяется.\n\n" +
                "Входные параметры:\n" +
                "  Словарь*               — Dictionary<string, string>\n" +
                "  Ключ*                  — ключ для удаления\n" +
                "  Ошибка если не найдено — поведение при отсутствии ключа\n\n" +
                "Выходные параметры:\n" +
                "  Результирующий словарь — копия без удалённого ключа\n" +
                "  Ключ удалён            — true если ключ существовал и был удалён";

            sdkComponentIcon = ActivityIcons.Dictionary;

                                    sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<Dictionary<string, string>>("Prop_Dictionary", "Входной словарь"),
                PropertyBuilder.Script<string>("Prop_Key", "Ключ для удаления"),
                PropertyBuilder.Variable<Dictionary<string, string>>("Prop_ResultDictionary", "Новый словарь без удалённого ключа"),
                PropertyBuilder.Variable<bool>("Prop_Removed", "True если ключ был найден и удалён")
            };

            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var    dict = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary, "Prop_Dictionary", sd);
                string key  = GetPropertyValue<string>(this.Prop_Key, "Prop_Key", sd);

                if (dict == null) throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null");
                if (string.IsNullOrEmpty(key)) throw new ArgumentException("Ключ не может быть пустым");

                bool exists = dict.ContainsKey(key);

                if (!exists && this.Prop_ThrowIfNotFound)
                    throw new KeyNotFoundException(
                        $"Ключ '{key}' не найден в словаре. " +
                        $"Доступные ключи: {string.Join(", ", dict.Keys.OrderBy(k => k))}");

                // Создаём новый словарь через LINQ Where — исключаем удаляемый ключ
                var result = dict
                    .Where(p => p.Key != key)
                    .ToDictionary(p => p.Key, p => p.Value);

                SetVariableValue(this.Prop_ResultDictionary, result,  sd);
                SetVariableValue(this.Prop_Removed,          exists,  sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = exists ? $"Ключ '{key}' удалён" : $"Ключ '{key}' не найден" };
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
            ret.ValidateRequired(this.Prop_Key, ActivityStrings.Field_Key, ActivityStrings.Error_KeyRequired);
            return ret;
        }
    }
}
