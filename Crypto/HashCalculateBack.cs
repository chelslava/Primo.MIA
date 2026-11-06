// =============================================================================
// HashCalculateBack.cs — активность «Крипто: Хэширование».
// Вычисляет хэш строки или файла (MD5/SHA1/SHA256/SHA384/SHA512).
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace Primo.MIA
{
    /// <summary>Активность вычисления хэша строки или файла.</summary>
    public class HashCalculateBack : PrimoComponentTO<HashCalculate>
    {
        public override string GroupName { get => ActivityCategories.Crypto; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        private CryptoHashSource _propHashSource = CryptoHashSource.String;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoHashSource)]
        /// <summary>Источник данных: строка или файл.</summary>
        public CryptoHashSource Prop_HashSource { get => _propHashSource; set { _propHashSource = value; InvokePropertyChanged(this, nameof(Prop_HashSource)); } }

        private string _propInputString;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Input), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoInputString)]
        /// <summary>Входная строка для хэширования (режим String).</summary>
        public string Prop_InputString { get => _propInputString; set { _propInputString = value; InvokePropertyChanged(this, nameof(Prop_InputString)); } }

        private string _propFilePath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Input), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoInputFile)]
        /// <summary>Путь к файлу для хэширования (режим File).</summary>
        public string Prop_FilePath { get => _propFilePath; set { _propFilePath = value; InvokePropertyChanged(this, nameof(Prop_FilePath)); } }

        private CryptoHashAlgorithm _propHashAlgorithm = CryptoHashAlgorithm.SHA256;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoHashAlgorithm)]
        /// <summary>Алгоритм хэширования.</summary>
        public CryptoHashAlgorithm Prop_HashAlgorithm { get => _propHashAlgorithm; set { _propHashAlgorithm = value; InvokePropertyChanged(this, nameof(Prop_HashAlgorithm)); } }

        private CryptoHashOutputFormat _propHashFormat = CryptoHashOutputFormat.HexLower;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoHashFormat)]
        /// <summary>Формат вывода: hex (нижний/верхний регистр) или Base64.</summary>
        public CryptoHashOutputFormat Prop_HashFormat { get => _propHashFormat; set { _propHashFormat = value; InvokePropertyChanged(this, nameof(Prop_HashFormat)); } }

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Crypto_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_CryptoHashResult)]
        /// <summary>Вычисленный хэш.</summary>
        public string Prop_Result { get => _propResult; set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); } }

        public HashCalculateBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_HashCalculate;
            sdkComponentHelp = @"Компонент ""Крипто: Хэширование""
Вычисляет хэш строки или содержимого файла. Алгоритмы: MD5, SHA-1, SHA-256, SHA-384, SHA-512. Формат вывода: hex (нижний/верхний регистр) или Base64.

Входные данные:
Строка (входная): [String] Строка для хэширования (режим ""Строка"").
Путь к файлу: [String] Путь к файлу (режим ""Файл""). Файл читается потоково — большие файлы не занимают память.

Настройки:
Источник: [Enum] Строка или Файл.
Алгоритм: [Enum] MD5, SHA1, SHA256, SHA384, SHA512. По умолчанию SHA-256.
Формат вывода: [Enum] HexLower, HexUpper, Base64.

Выходные данные:
Хэш: [String] Хэш в выбранном формате.";
            sdkComponentIcon = ActivityIcons.Crypto;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<CryptoHashSource>("Prop_HashSource", "Источник: строка или файл"),
                PropertyBuilder.String("Prop_InputString", "Входная строка"),
                PropertyBuilder.FileSelector("Prop_FilePath", "Путь к файлу"),
                PropertyBuilder.Enum<CryptoHashAlgorithm>("Prop_HashAlgorithm", "Алгоритм хэширования"),
                PropertyBuilder.Enum<CryptoHashOutputFormat>("Prop_HashFormat", "Формат вывода"),
                PropertyBuilder.Variable<string>("Prop_Result", "Вычисленный хэш")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new CryptoLogic();
            try
            {
                string result;
                if (this.Prop_HashSource == CryptoHashSource.File)
                {
                    var filePath = GetPropertyValue<string>(this.Prop_FilePath, nameof(Prop_FilePath), sd);
                    result = logic.HashFile(filePath, this.Prop_HashAlgorithm, this.Prop_HashFormat);
                }
                else
                {
                    var input = GetPropertyValue<string>(this.Prop_InputString, nameof(Prop_InputString), sd);
                    result = logic.HashString(input, this.Prop_HashAlgorithm, this.Prop_HashFormat);
                }

                SetVariableValue(this.Prop_Result, result, sd);
                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Хэш вычислен: {result}" };
            }
            catch (FileNotFoundException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"{ActivityStrings.Error_CryptoFileNotFound}: {ex.FileName}" };
            }
            catch (ArgumentException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Неверный аргумент: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка хэширования: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            if (this.Prop_HashSource == CryptoHashSource.File)
                ret.ValidateRequired(this.Prop_FilePath, ActivityStrings.Field_CryptoInputFile, ActivityStrings.Error_CryptoFileNotFound);
            else
                ret.ValidateRequired(this.Prop_InputString, ActivityStrings.Field_CryptoInputString, ActivityStrings.Error_CryptoInputRequired);
            return ret;
        }
    }
}
