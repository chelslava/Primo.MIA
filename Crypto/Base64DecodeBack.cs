// =============================================================================
// Base64DecodeBack.cs — активность «Крипто: Base64 Декодировать».
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность декодирования Base64-строки.</summary>
    public class Base64DecodeBack : PrimoComponentTO<Base64Decode>
    {
        public override string GroupName { get => ActivityCategories.Crypto; protected set { } }

        protected override int sdkTimeOut { get => 10000; set { } }

        private string _propInputString;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Input), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoInputString)]
        /// <summary>Base64-строка для декодирования.</summary>
        public string Prop_InputString { get => _propInputString; set { _propInputString = value; InvokePropertyChanged(this, nameof(Prop_InputString)); } }

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoBase64Result)]
        /// <summary>Декодированная строка (UTF-8).</summary>
        public string Prop_Result { get => _propResult; set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); } }

        public Base64DecodeBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_Base64Decode;
            sdkComponentHelp = @"Компонент ""Крипто: Base64 Декодировать""
Декодирует Base64-строку в текст (UTF-8).

Входные данные:
Строка (входная): [String] Base64-строка.

Выходные данные:
Результат: [String] Декодированная строка.";
            sdkComponentIcon = ActivityIcons.Crypto;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_InputString", "Base64-строка"),
                PropertyBuilder.Variable<string>("Prop_Result", "Декодированная строка")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new CryptoLogic();
            try
            {
                var input = GetPropertyValue<string>(this.Prop_InputString, nameof(Prop_InputString), sd);
                var result = logic.Base64Decode(input);
                SetVariableValue(this.Prop_Result, result, sd);
                return new ExecutionResult { IsSuccess = true, SuccessMessage = "Строка декодирована" };
            }
            catch (InvalidOperationException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"{ActivityStrings.Error_CryptoBase64Invalid}: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка декодирования: {ex.Message}" };
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
