// =============================================================================
// PgpEncryptFileBack.cs — активность «Крипто: PGP Шифровать файл» (issue #35).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность PGP-шифрования файла.</summary>
    public class PgpEncryptFileBack : PrimoComponentTO<PgpEncryptFile>
    {
        public override string GroupName { get => ActivityCategories.Crypto; protected set { } }

        protected override int sdkTimeOut { get => 120000; set { } }

        private string _propInputPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PgpInputFile)]
        /// <summary>Исходный файл.</summary>
        public string Prop_InputPath
        {
            get => _propInputPath;
            set { _propInputPath = value; InvokePropertyChanged(this, nameof(Prop_InputPath)); }
        }

        private string _propOutputPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PgpOutputFile)]
        /// <summary>Путь зашифрованного файла.</summary>
        public string Prop_OutputPath
        {
            get => _propOutputPath;
            set { _propOutputPath = value; InvokePropertyChanged(this, nameof(Prop_OutputPath)); }
        }

        private string _propPublicKeyPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PgpPublicKey)]
        /// <summary>Публичный ключ получателя (.asc/.gpg).</summary>
        public string Prop_PublicKeyPath
        {
            get => _propPublicKeyPath;
            set { _propPublicKeyPath = value; InvokePropertyChanged(this, nameof(Prop_PublicKeyPath)); }
        }

        private bool _propArmor;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_PgpArmor)]
        /// <summary>ASCII-armored выход (.asc) — для почты и текстовых каналов.</summary>
        public bool Prop_Armor
        {
            get => _propArmor;
            set { _propArmor = value; InvokePropertyChanged(this, nameof(Prop_Armor)); }
        }

        private string _propResultPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_PgpResultPath)]
        /// <summary>Путь зашифрованного файла (имя выходной переменной).</summary>
        public string Prop_ResultPath
        {
            get => _propResultPath;
            set { _propResultPath = value; InvokePropertyChanged(this, nameof(Prop_ResultPath)); }
        }

        public PgpEncryptFileBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_PgpEncryptFile;
            sdkComponentHelp = @"Компонент ""Крипто: PGP Шифровать файл""
Шифрует файл публичным ключом получателя (OpenPGP, AES-256).

Основные:
Исходный файл*: [String] Что шифруем.
Файл результата*: [String] Путь зашифрованного файла (пересоздаётся).
Публичный ключ*: [String] .asc/.gpg получателя.

Настройки:
ASCII-armored: [Boolean] Текстовый выход .asc (для почты); иначе бинарный .pgp.

Выходные данные:
Путь результата: [String]";
            sdkComponentIcon = ActivityIcons.Crypto;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.FileSelector("Prop_InputPath", "Исходный файл"),
                PropertyBuilder.FileSelector("Prop_OutputPath", "Зашифрованный файл (результат)"),
                PropertyBuilder.FileSelector("Prop_PublicKeyPath", "Публичный ключ получателя (.asc/.gpg)"),
                PropertyBuilder.BooleanObject("Prop_Armor", "ASCII-armored выход (.asc)"),
                PropertyBuilder.Variable<string>("Prop_ResultPath", "Путь зашифрованного файла")
            };

            InitClass(container);

            this.Prop_InputPath = "\"\"";
            this.Prop_OutputPath = "\"\"";
            this.Prop_PublicKeyPath = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new PgpLogic();
            try
            {
                var input = GetPropertyValue<string>(this.Prop_InputPath, nameof(Prop_InputPath), sd);
                var output = GetPropertyValue<string>(this.Prop_OutputPath, nameof(Prop_OutputPath), sd);
                var publicKey = GetPropertyValue<string>(this.Prop_PublicKeyPath, nameof(Prop_PublicKeyPath), sd);

                var bytes = logic.EncryptFile(input, output, publicKey, this.Prop_Armor);

                SetVariableValue(this.Prop_ResultPath, System.IO.Path.GetFullPath(output), sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[PGP] Файл зашифрован: {output} ({bytes} байт)"
                };
            }
            catch (InvalidOperationException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка PGP-шифрования: {ex.Message}"
                };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_InputPath, ActivityStrings.Field_PgpInputFile, ActivityStrings.Error_PgpInputRequired);
            ret.ValidateRequired(this.Prop_OutputPath, ActivityStrings.Field_PgpOutputFile, ActivityStrings.Error_PgpOutputRequired);
            ret.ValidateRequired(this.Prop_PublicKeyPath, ActivityStrings.Field_PgpPublicKey, ActivityStrings.Error_PgpKeyRequired);
            return ret;
        }
    }
}
