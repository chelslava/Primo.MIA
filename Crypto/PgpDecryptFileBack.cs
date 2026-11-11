// =============================================================================
// PgpDecryptFileBack.cs — активность «Крипто: PGP Расшифровать файл»
// (issue #35). Приватный ключ + passphrase (парольная фраза, не пароль
// от файла — файл расшифровывается сессионным ключом конверта).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность PGP-расшифровки файла.</summary>
    public class PgpDecryptFileBack : PrimoComponentTO<PgpDecryptFile>
    {
        public override string GroupName { get => ActivityCategories.Crypto; protected set { } }

        protected override int sdkTimeOut { get => 120000; set { } }

        private string _propInputPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PgpInputFile)]
        /// <summary>Зашифрованный файл (.pgp/.asc).</summary>
        public string Prop_InputPath
        {
            get => _propInputPath;
            set { _propInputPath = value; InvokePropertyChanged(this, nameof(Prop_InputPath)); }
        }

        private string _propOutputPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PgpOutputFile)]
        /// <summary>Путь расшифрованного файла.</summary>
        public string Prop_OutputPath
        {
            get => _propOutputPath;
            set { _propOutputPath = value; InvokePropertyChanged(this, nameof(Prop_OutputPath)); }
        }

        private string _propPrivateKeyPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PgpPrivateKey)]
        /// <summary>Приватный ключ (.asc/.gpg).</summary>
        public string Prop_PrivateKeyPath
        {
            get => _propPrivateKeyPath;
            set { _propPrivateKeyPath = value; InvokePropertyChanged(this, nameof(Prop_PrivateKeyPath)); }
        }

        private string _propPassphrase;
        /// <summary>Парольная фраза приватного ключа.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PgpPassphrase)]
        public string Prop_Passphrase
        {
            get => _propPassphrase;
            set { _propPassphrase = value; InvokePropertyChanged(this, nameof(Prop_Passphrase)); }
        }

        private string _propResultPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_PgpResultPath)]
        /// <summary>Путь расшифрованного файла (имя выходной переменной).</summary>
        public string Prop_ResultPath
        {
            get => _propResultPath;
            set { _propResultPath = value; InvokePropertyChanged(this, nameof(Prop_ResultPath)); }
        }

        public PgpDecryptFileBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_PgpDecryptFile;
            sdkComponentHelp = @"Компонент ""Крипто: PGP Расшифровать файл""
Расшифровывает файл приватным ключом (OpenPGP).

Основные:
Зашифрованный файл*: [String] .pgp или .asc.
Файл результата*: [String] Путь расшифрованного файла (пересоздаётся).
Приватный ключ*: [String] .asc/.gpg владельца.
Парольная фраза: [String] От приватного ключа (если установлена).

Выходные данные:
Путь результата: [String]";
            sdkComponentIcon = ActivityIcons.Crypto;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.FileSelector("Prop_InputPath", "Зашифрованный файл (.pgp/.asc)"),
                PropertyBuilder.FileSelector("Prop_OutputPath", "Расшифрованный файл (результат)"),
                PropertyBuilder.FileSelector("Prop_PrivateKeyPath", "Приватный ключ (.asc/.gpg)"),
                PropertyBuilder.String("Prop_Passphrase", "Парольная фраза приватного ключа"),
                PropertyBuilder.Variable<string>("Prop_ResultPath", "Путь расшифрованного файла")
            };

            InitClass(container);

            this.Prop_InputPath = "\"\"";
            this.Prop_OutputPath = "\"\"";
            this.Prop_PrivateKeyPath = "\"\"";
            this.Prop_Passphrase = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new PgpLogic();
            try
            {
                var input = GetPropertyValue<string>(this.Prop_InputPath, nameof(Prop_InputPath), sd);
                var output = GetPropertyValue<string>(this.Prop_OutputPath, nameof(Prop_OutputPath), sd);
                var privateKey = GetPropertyValue<string>(this.Prop_PrivateKeyPath, nameof(Prop_PrivateKeyPath), sd);
                var passphrase = GetPropertyValue<string>(this.Prop_Passphrase, nameof(Prop_Passphrase), sd);

                var bytes = logic.DecryptFile(input, output, privateKey, passphrase);

                SetVariableValue(this.Prop_ResultPath, System.IO.Path.GetFullPath(output), sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[PGP] Файл расшифрован: {output} ({bytes} байт)"
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
                    ErrorMessage = $"Ошибка PGP-расшифровки: {ex.Message}"
                };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_InputPath, ActivityStrings.Field_PgpInputFile, ActivityStrings.Error_PgpInputRequired);
            ret.ValidateRequired(this.Prop_OutputPath, ActivityStrings.Field_PgpOutputFile, ActivityStrings.Error_PgpOutputRequired);
            ret.ValidateRequired(this.Prop_PrivateKeyPath, ActivityStrings.Field_PgpPrivateKey, ActivityStrings.Error_PgpKeyRequired);
            return ret;
        }
    }
}
