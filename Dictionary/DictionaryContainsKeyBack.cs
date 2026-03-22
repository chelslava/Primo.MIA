using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Словарь: Проверить ключ».
    /// Проверяет наличие ключа в Dictionary&lt;string, string&gt; → bool.
    /// Не изменяет словарь.
    /// </summary>
    public class DictionaryContainsKeyBack : PrimoComponentTO<DictionaryContainsKey>
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

        private string _propKey;
        /// <summary>Ключ для проверки существования в словаре</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Key)]
        public string Prop_Key
        {
            get => _propKey;
            set { _propKey = value; InvokePropertyChanged(this, "Prop_Key"); }
        }

        private string _propResult;
        /// <summary>True если ключ присутствует в словаре, False если нет</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Result)]
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

            sdkComponentIcon = ActivityIcons.Dictionary;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<Dictionary<string, string>>("Prop_Dictionary", "Входной словарь"),
                PropertyBuilder.Script<string>("Prop_Key", "Ключ для проверки"),
                PropertyBuilder.Variable<bool>("Prop_Result", "True если ключ найден в словаре")
            };

            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var logic = new DictionaryKeyValueLogic();
                var dict = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary, "Prop_Dictionary", sd);
                string key = GetPropertyValue<string>(this.Prop_Key, "Prop_Key", sd);

                if (dict == null) throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null");
                if (string.IsNullOrEmpty(key)) throw new ArgumentException("Ключ не может быть пустым");

                var result = logic.ContainsKey(dict, key);
                SetVariableValue(this.Prop_Result, result.Found, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Ключ '{key}': {(result.Found ? "найден" : "не найден")}" };
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
