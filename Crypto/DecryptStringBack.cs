// =============================================================================
// DecryptStringBack.cs — активность «Крипто: Дешифрование строки (AES)».
// Расшифровка вывода EncryptStringBack тем же паролем.
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность дешифрования строки паролем.</summary>
    public class DecryptStringBack : PrimoComponentTO<DecryptString>
    {
        public override string GroupName { get => ActivityCategories.Crypto; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        private string _propEncryptedString;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Input), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoEncryptResult)]
        /// <summary>Зашифрованная строка Base64 (вывод активности шифрования).</summary>
        public string Prop_EncryptedString { get => _propEncryptedString; set { _propEncryptedString = value; InvokePropertyChanged(this, nameof(Prop_EncryptedString)); } }

        private string _propPassword;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoPassword)]
        /// <summary>Пароль, использованный при шифровании.</summary>
        public string Prop_Password { get => _propPassword; set { _propPassword = value; InvokePropertyChanged(this, nameof(Prop_Password)); } }

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoDecryptResult)]
        /// <summary>Расшифрованный открытый текст.</summary>
        public string Prop_Result { get => _propResult; set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); } }

        public DecryptStringBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DecryptString;
            sdkComponentHelp = @"Компонент ""Крипто: Дешифрование строки (AES)""
Расшифровывает строку, зашифрованную активностью ""Крипто: Шифрование строки (AES)"", тем же паролем (AES-256-GCM + PBKDF2-SHA256; старый формат CBC читается для совместимости).

Входные данные:
Зашифрованная строка*: [String] MIA2.<Base64([соль][nonce][шифртекст+тег])> или старый формат Base64([соль][IV][шифртекст]).

Настройки:
Пароль*: [String] Пароль шифрования.

Выходные данные:
Расшифрованная строка: [String] Открытый текст.";
            sdkComponentIcon = ActivityIcons.Crypto;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_EncryptedString", "Зашифрованная строка (Base64)"),
                PropertyBuilder.String("Prop_Password", "Пароль шифрования"),
                PropertyBuilder.Variable<string>("Prop_Result", "Расшифрованная строка")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new CryptoLogic();
            try
            {
                var encrypted = GetPropertyValue<string>(this.Prop_EncryptedString, nameof(Prop_EncryptedString), sd);
                var password = GetPropertyValue<string>(this.Prop_Password, nameof(Prop_Password), sd);

                var result = logic.DecryptString(encrypted, password);
                SetVariableValue(this.Prop_Result, result, sd);
                return new ExecutionResult { IsSuccess = true, SuccessMessage = "Строка расшифрована" };
            }
            catch (InvalidOperationException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"{ActivityStrings.Error_CryptoDecryptFailed}: {ex.Message}" };
            }
            catch (ArgumentException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Неверный аргумент: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка дешифрования: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Password, ActivityStrings.Field_CryptoPassword, ActivityStrings.Error_CryptoPasswordRequired);
            ret.ValidateRequired(this.Prop_EncryptedString, ActivityStrings.Field_CryptoEncryptResult, ActivityStrings.Error_CryptoInputRequired);
            return ret;
        }
    }
}
