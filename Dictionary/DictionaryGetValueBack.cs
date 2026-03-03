// =============================================================================
// DictionaryGetValue.cs — активность «Словарь: Получить значение».
//
// Читает значение из Dictionary<string, string> по заданному ключу.
// При отсутствии ключа — возвращает DefaultValue или выбрасывает исключение.
// Словарь не изменяется.
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
    /// Активность «Словарь: Получить значение».
    /// Возвращает значение по ключу из Dictionary&lt;string, string&gt;.
    /// Не изменяет словарь.
    /// </summary>
    public class DictionaryGetValueBack : PrimoComponentTO<DictionaryGetValue>
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
        /// <summary>Ключ для поиска значения в словаре</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Ключ")]
        public string Prop_Key
        {
            get => _propKey;
            set { _propKey = value; InvokePropertyChanged(this, "Prop_Key"); }
        }

        private string _propDefaultValue;
        /// <summary>
        /// Значение возвращаемое если ключ не найден.
        /// Игнорируется при Prop_ThrowIfNotFound = true.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Значение по умолчанию")]
        public string Prop_DefaultValue
        {
            get => _propDefaultValue;
            set { _propDefaultValue = value; InvokePropertyChanged(this, "Prop_DefaultValue"); }
        }

        private bool _throwIfNotFound = false;
        /// <summary>
        /// Если true — выбросить KeyNotFoundException при отсутствии ключа.
        /// Если false — вернуть Prop_DefaultValue.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Ошибка если не найдено")]
        public bool Prop_ThrowIfNotFound
        {
            get => _throwIfNotFound;
            set { _throwIfNotFound = value; InvokePropertyChanged(this, "Prop_ThrowIfNotFound"); }
        }

        // =========================================================================
        // OUTPUT PROPERTIES
        // =========================================================================

        private string _propValue;
        /// <summary>Найденное значение или DefaultValue если ключ не найден</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Значение")]
        public string Prop_Value
        {
            get => _propValue;
            set { _propValue = value; InvokePropertyChanged(this, "Prop_Value"); }
        }

        private string _propFound;
        /// <summary>True если ключ найден в словаре, false если нет</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Ключ найден")]
        public string Prop_Found
        {
            get => _propFound;
            set { _propFound = value; InvokePropertyChanged(this, "Prop_Found"); }
        }

        // =========================================================================
        // КОНСТРУКТОР
        // =========================================================================

        public DictionaryGetValueBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Словарь: Получить значение";
            sdkComponentHelp =
                "Возвращает значение из Dictionary<string, string> по заданному ключу.\n" +
                "Если ключ не найден:\n" +
                "  Ошибка если не найдено = false → возвращает Значение по умолчанию\n" +
                "  Ошибка если не найдено = true  → KeyNotFoundException с перечислением ключей\n\n" +
                "Входные параметры:\n" +
                "  Словарь*            — Dictionary<string, string>\n" +
                "  Ключ*               — строка ключа для поиска\n" +
                "  Значение по умолчанию — возвращается при отсутствии ключа\n" +
                "  Ошибка если не найдено — поведение при отсутствии ключа\n\n" +
                "Выходные параметры:\n" +
                "  Значение    — найденное значение или DefaultValue\n" +
                "  Ключ найден — bool флаг результата поиска";

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
                    ToolTip = "Ключ для поиска значения", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_DefaultValue", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Значение по умолчанию если ключ не найден", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Value", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Найденное значение (или DefaultValue)", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Found", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool),
                    ToolTip = "True если ключ был найден в словаре", IsReadOnly = false
                }
            };

            InitClass(container);
            this.Prop_DefaultValue = "\"\"";
        }

        // =========================================================================
        // ОСНОВНОЕ ДЕЙСТВИЕ
        // =========================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var    dict   = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary,   "Prop_Dictionary",   sd);
                string key    = GetPropertyValue<string>(this.Prop_Key,          "Prop_Key",          sd);
                string defVal = GetPropertyValue<string>(this.Prop_DefaultValue, "Prop_DefaultValue", sd);

                if (dict == null)
                    throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null");
                if (string.IsNullOrEmpty(key))
                    throw new ArgumentException("Ключ не может быть пустым");

                bool found = dict.TryGetValue(key, out string value);

                if (!found)
                {
                    if (this.Prop_ThrowIfNotFound)
                        throw new KeyNotFoundException(
                            $"Ключ '{key}' не найден в словаре. " +
                            $"Доступные ключи: {string.Join(", ", dict.Keys.OrderBy(k => k))}");

                    value = defVal ?? string.Empty;
                }

                SetVariableValue(this.Prop_Value, value, sd);
                SetVariableValue(this.Prop_Found, found, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Ключ '{key}': {(found ? "найден" : "не найден, возвращено значение по умолчанию")}" };
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
            ValidateField(ret, this.Prop_Dictionary, "Словарь", "Словарь обязателен");
            ValidateField(ret, this.Prop_Key,        "Ключ",    "Ключ обязателен");
            return ret;
        }

        private void ValidateField(ValidationResult result, string value, string fieldName, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
                result.Items.Add(new ValidationResult.ValidationItem() { PropertyName = fieldName, Error = errorMessage });
        }
    }
}
