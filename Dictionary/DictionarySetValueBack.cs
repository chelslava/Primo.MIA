// =============================================================================
// DictionarySetValue.cs — активность «Словарь: Установить значение».
//
// Добавляет новый ключ или обновляет значение существующего.
// Возвращает новый словарь — оригинал не изменяется.
// Prop_IsUpdate = true если ключ уже существовал (обновление), false — новый ключ.
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
    /// Активность «Словарь: Установить значение».
    /// Добавляет новый ключ или перезаписывает существующий.
    /// Возвращает изменённую копию — оригинальный словарь не мутируется.
    /// </summary>
    public class DictionarySetValueBack : PrimoComponentTO<DictionarySetValue>
    {
        public override string GroupName { get => ActivityCategories.Dictionaries; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // =========================================================================
        // INPUT PROPERTIES
        // =========================================================================

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
        /// <summary>Ключ для добавления или обновления</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Key)]
        public string Prop_Key
        {
            get => _propKey;
            set { _propKey = value; InvokePropertyChanged(this, "Prop_Key"); }
        }

        private string _propValue;
        /// <summary>Значение которое будет установлено для ключа</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Value)]
        public string Prop_Value
        {
            get => _propValue;
            set { _propValue = value; InvokePropertyChanged(this, "Prop_Value"); }
        }

        // =========================================================================
        // OUTPUT PROPERTIES
        // =========================================================================

        private string _propResultDictionary;
        /// <summary>
        /// Новый словарь с добавленным/обновлённым ключом.
        /// Оригинальный Prop_Dictionary не изменяется.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ResultDictionary)]
        public string Prop_ResultDictionary
        {
            get => _propResultDictionary;
            set { _propResultDictionary = value; InvokePropertyChanged(this, "Prop_ResultDictionary"); }
        }

        private string _propIsUpdate;
        /// <summary>
        /// True если ключ уже существовал (обновление значения).
        /// False если ключ был добавлен впервые.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_IsUpdate)]
        public string Prop_IsUpdate
        {
            get => _propIsUpdate;
            set { _propIsUpdate = value; InvokePropertyChanged(this, "Prop_IsUpdate"); }
        }

        // =========================================================================
        // КОНСТРУКТОР
        // =========================================================================

        public DictionarySetValueBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Словарь: Установить значение";
            sdkComponentHelp =
                "Добавляет новый ключ или обновляет значение существующего в словаре.\n" +
                "Возвращает новую копию словаря — оригинал не изменяется.\n\n" +
                "Входные параметры:\n" +
                "  Словарь*  — Dictionary<string, string>\n" +
                "  Ключ*     — ключ для добавления или обновления\n" +
                "  Значение* — значение для установки\n\n" +
                "Выходные параметры:\n" +
                "  Результирующий словарь    — копия с изменённым ключом\n" +
                "  Обновление (не добавление) — true если ключ уже существовал";

            sdkComponentIcon = ActivityIcons.Dictionary;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<Dictionary<string, string>>("Prop_Dictionary", "Входной словарь Dictionary<string, string>"),
                PropertyBuilder.Script<string>("Prop_Key", "Ключ для добавления или обновления"),
                PropertyBuilder.Script<string>("Prop_Value", "Значение которое будет установлено"),
                PropertyBuilder.Variable<Dictionary<string, string>>("Prop_ResultDictionary", "Новый словарь с изменённым ключом"),
                PropertyBuilder.Variable<bool>("Prop_IsUpdate", "True = обновление существующего, False = новый ключ")
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
                var logic = new DictionaryKeyValueLogic();
                var dict = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary, "Prop_Dictionary", sd);
                string key = GetPropertyValue<string>(this.Prop_Key, "Prop_Key", sd);
                string value = GetPropertyValue<string>(this.Prop_Value, "Prop_Value", sd);

                if (dict == null)
                    throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null");
                if (string.IsNullOrEmpty(key))
                    throw new ArgumentException("Ключ не может быть пустым");
                if (value == null)
                    throw new ArgumentNullException("Prop_Value", "Значение не может быть null");

                var result = logic.SetValue(dict, key, value);

                SetVariableValue(this.Prop_ResultDictionary, result.Dictionary, sd);
                SetVariableValue(this.Prop_IsUpdate, result.IsUpdate, sd);

                string action = result.IsUpdate ? "обновлён" : "добавлен";
                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Ключ '{key}' {action}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка: {ex.Message}" };
            }
        }

        // =========================================================================
        // ВАЛИДАЦИЯ
        // =========================================================================

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Dictionary, ActivityStrings.Field_Dictionary, ActivityStrings.Error_DictionaryRequired);
            ret.ValidateRequired(this.Prop_Key, ActivityStrings.Field_Key, ActivityStrings.Error_KeyRequired);
            ret.ValidateRequired(this.Prop_Value, ActivityStrings.Field_Value, ActivityStrings.Error_ValueRequired);
            return ret;
        }
    }
}
