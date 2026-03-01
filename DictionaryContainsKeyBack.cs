using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using System;
using System.Collections.Generic;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Словарь: Проверить ключ».
    /// Проверяет наличие ключа в Dictionary&lt;string, string&gt; → bool.
    /// Не изменяет словарь.
    /// </summary>
    public class DictionaryContainsKeyBack : PrimoComponentTO<DictionaryContainsKey>
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

        private string _propKey;
        /// <summary>Ключ для проверки существования в словаре</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Ключ")]
        public string Prop_Key
        {
            get => _propKey;
            set { _propKey = value; InvokePropertyChanged(this, "Prop_Key"); }
        }

        private string _propResult;
        /// <summary>True если ключ присутствует в словаре, False если нет</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Ключ существует")]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); }
        }

        public DictionaryContainsKeyBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Словарь: Проверить ключ";
            sdkComponentHelp =
                "Проверяет наличие ключа в Dictionary<string, string>.\n\n" +
                "Входные параметры:\n" +
                "  Словарь* — Dictionary<string, string>\n" +
                "  Ключ*    — проверяемый ключ\n\n" +
                "Выходные параметры:\n" +
                "  Ключ существует — true если ключ есть в словаре";

            sdkComponentIcon = "pack://application:,,,/Primo.MIA;component/images/dict.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Dictionary", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(Dictionary<string, string>),
                    ToolTip = "Входной словарь", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Key", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Ключ для проверки", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Result", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool),
                    ToolTip = "True если ключ найден в словаре", IsReadOnly = false
                }
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

                bool result = dict.ContainsKey(key);
                SetVariableValue(this.Prop_Result, result, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Ключ '{key}': {(result ? "найден" : "не найден")}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка: {ex.Message}" };
            }
        }

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
