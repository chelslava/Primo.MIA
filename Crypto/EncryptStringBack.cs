// =============================================================================
// EncryptStringBack.cs — активность «Крипто: Шифрование строки (AES)».
// AES-256-CBC, ключ из пароля через PBKDF2-SHA256.
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность шифрования строки паролем.</summary>
    public class EncryptStringBack : PrimoComponentTO<EncryptString>
    {
        public override string GroupName { get => ActivityCategories.Crypto; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        private string _propInputString;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Input), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoInputString)]
        /// <summary>Открытый текст для шифрования.</summary>
        public string Prop_InputString { get => _propInputString; set { _propInputString = value; InvokePropertyChanged(this, nameof(Prop_InputString)); } }

        private string _propPassword;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoPassword)]
        /// <summary>Пароль шифрования (ключ AES-256 выводится через PBKDF2).</summary>
        public string Prop_Password { get => _propPassword; set { _propPassword = value; InvokePropertyChanged(this, nameof(Prop_Password)); } }

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoEncryptResult)]
        /// <summary>Зашифрованная строка в Base64 ([соль][IV][шифртекст]).</summary>
        public string Prop_Result { get => _propResult; set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); } }

        public EncryptStringBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_EncryptString;
            sdkComponentHelp = @"Компонент ""Крипто: Шифрование строки (AES)""
Шифрует строку паролем: AES-256-CBC, ключ и IV выводятся из пароля через PBKDF2-SHA256 (100 000 итераций), случайная соль при каждом шифровании. Результат — Base64([соль 16 байт][IV 16 байт][шифртекст]).

Входные данные:
Строка (входная): [String] Открытый текст.

Настройки:
Пароль*: [String] Пароль шифрования. Расшифровка возможна только тем же паролем.

Выходные данные:
Зашифрованная строка: [String] Base64-строка, совместимая с активностью ""Крипто: Дешифрование строки (AES)"".";
            sdkComponentIcon = ActivityIcons.Crypto;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_InputString", "Открытый текст"),
                PropertyBuilder.String("Prop_Password", "Пароль шифрования"),
                PropertyBuilder.Variable<string>("Prop_Result", "Зашифрованная строка (Base64)")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new CryptoLogic();
            try
            {
                var input = GetPropertyValue<string>(this.Prop_InputString, nameof(Prop_InputString), sd);
                var password = GetPropertyValue<string>(this.Prop_Password, nameof(Prop_Password), sd);

                var result = logic.EncryptString(input, password);
                SetVariableValue(this.Prop_Result, result, sd);
                return new ExecutionResult { IsSuccess = true, SuccessMessage = "Строка зашифрована" };
            }
            catch (ArgumentException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Неверный аргумент: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка шифрования: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Password, ActivityStrings.Field_CryptoPassword, ActivityStrings.Error_CryptoPasswordRequired);
            ret.ValidateRequired(this.Prop_InputString, ActivityStrings.Field_CryptoInputString, ActivityStrings.Error_CryptoInputRequired);
            return ret;
        }
    }
}
