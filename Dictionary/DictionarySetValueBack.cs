// =============================================================================
// DictionarySetValue.cs — активность «Словарь: Установить значение».
//
// Добавляет новый ключ или обновляет значение существующего.
// Возвращает новый словарь — оригинал не изменяется.
// Prop_IsUpdate = true если ключ уже существовал (обновление), false — новый ключ.
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
    /// Активность «Словарь: Установить значение».
    /// Добавляет новый ключ или перезаписывает существующий.
    /// Возвращает изменённую копию — оригинальный словарь не мутируется.
    /// </summary>
    public class DictionarySetValueBack : PrimoComponentTO<DictionarySetValue>
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

        private string _propDictionary;
        /// <summary>Входной словарь Dictionary&lt;string, string&gt;</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Словарь")]
        public string Prop_Dictionary
        {
            get => _propDictionary;
            set { _propDictionary = value; InvokePropertyChanged(this, "Prop_Dictionary"); }
        }

        private string _propKey;
        /// <summary>Ключ для добавления или обновления</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Ключ")]
        public string Prop_Key
        {
            get => _propKey;
            set { _propKey = value; InvokePropertyChanged(this, "Prop_Key"); }
        }

        private string _propValue;
        /// <summary>Значение которое будет установлено для ключа</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Значение")]
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
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Результирующий словарь")]
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
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Обновление (не добавление)")]
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
                    PropName = "Prop_Key", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Ключ для добавления или обновления", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Value", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Значение которое будет установлено", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_ResultDictionary", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(Dictionary<string, string>),
                    ToolTip = "Новый словарь с изменённым ключом", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_IsUpdate", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool),
                    ToolTip = "True = обновление существующего, False = новый ключ", IsReadOnly = false
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
                var    dict  = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary, "Prop_Dictionary", sd);
                string key   = GetPropertyValue<string>(this.Prop_Key,   "Prop_Key",   sd);
                string value = GetPropertyValue<string>(this.Prop_Value, "Prop_Value", sd);

                if (dict == null)
                    throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null");
                if (string.IsNullOrEmpty(key))
                    throw new ArgumentException("Ключ не может быть пустым");
                if (value == null)
                    throw new ArgumentNullException("Prop_Value", "Значение не может быть null");

                // Фиксируем: был ли ключ до установки
                bool isUpdate = dict.ContainsKey(key);

                // Создаём копию и устанавливаем значение — оригинал не трогаем
                var result = dict.ToDictionary(p => p.Key, p => p.Value);
                result[key] = value;

                SetVariableValue(this.Prop_ResultDictionary, result,    sd);
                SetVariableValue(this.Prop_IsUpdate,         isUpdate,  sd);

                string action = isUpdate ? "обновлён" : "добавлен";
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
            ValidateField(ret, this.Prop_Dictionary, "Словарь",  "Словарь обязателен");
            ValidateField(ret, this.Prop_Key,        "Ключ",     "Ключ обязателен");
            ValidateField(ret, this.Prop_Value,      "Значение", "Значение обязательно");
            return ret;
        }

        private void ValidateField(ValidationResult result, string value, string fieldName, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
                result.Items.Add(new ValidationResult.ValidationItem() { PropertyName = fieldName, Error = errorMessage });
        }
    }
}
