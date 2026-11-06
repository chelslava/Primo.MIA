// =============================================================================
// Base64EncodeBack.cs — активность «Крипто: Base64 Кодировать».
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность кодирования строки в Base64.</summary>
    public class Base64EncodeBack : PrimoComponentTO<Base64Encode>
    {
        public override string GroupName { get => ActivityCategories.Crypto; protected set { } }

        protected override int sdkTimeOut { get => 10000; set { } }

        private string _propInputString;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Input), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoInputString)]
        /// <summary>Строка для кодирования.</summary>
        public string Prop_InputString { get => _propInputString; set { _propInputString = value; InvokePropertyChanged(this, nameof(Prop_InputString)); } }

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoBase64Result)]
        /// <summary>Base64-строка.</summary>
        public string Prop_Result { get => _propResult; set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); } }

        public Base64EncodeBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_Base64Encode;
            sdkComponentHelp = @"Компонент ""Крипто: Base64 Кодировать""
Кодирует строку в Base64 (UTF-8).

Входные данные:
Строка (входная): [String] Строка для кодирования.

Выходные данные:
Результат: [String] Base64-строка.";
            sdkComponentIcon = ActivityIcons.Crypto;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_InputString", "Строка для кодирования"),
                PropertyBuilder.Variable<string>("Prop_Result", "Base64-строка")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new CryptoLogic();
            try
            {
                var input = GetPropertyValue<string>(this.Prop_InputString, nameof(Prop_InputString), sd);
                var result = logic.Base64Encode(input);
                SetVariableValue(this.Prop_Result, result, sd);
                return new ExecutionResult { IsSuccess = true, SuccessMessage = "Строка закодирована" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка кодирования: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_InputString, ActivityStrings.Field_CryptoInputString, ActivityStrings.Error_CryptoInputRequired);
            return ret;
        }
    }
}
