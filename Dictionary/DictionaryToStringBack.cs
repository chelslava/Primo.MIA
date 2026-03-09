using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.Enums;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Словарь: В строку».
    /// Сериализует Dictionary&lt;string, string&gt; в строку вида "key1=val1;key2=val2".
    /// Разделители настраиваются. Ключи сортируются алфавитно для стабильного вывода.
    /// </summary>
    public class DictionaryToStringBack : PrimoComponentTO<DictionaryToString>
    {
                public override string GroupName { get => ActivityCategories.Dictionaries; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // =========================================================================
        // INPUT PROPERTIES
        // =========================================================================

        private string _propDictionary;
        /// <summary>Словарь Dictionary&lt;string, string&gt; для сериализации в строку</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Dictionary)]
        public string Prop_Dictionary
        {
            get => _propDictionary;
            set { _propDictionary = value; InvokePropertyChanged(this, "Prop_Dictionary"); }
        }

        private string _propPairSeparator;
        /// <summary>
        /// Разделитель между парами ключ-значение.
        /// По умолчанию ";" → "key1=val1;key2=val2".
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Separators), System.ComponentModel.DisplayName(ActivityStrings.Field_PairSeparator)]
        public string Prop_PairSeparator
        {
            get => _propPairSeparator;
            set { _propPairSeparator = value; InvokePropertyChanged(this, "Prop_PairSeparator"); }
        }

        private string _propKeyValueSeparator;
        /// <summary>
        /// Разделитель между ключом и значением внутри пары.
        /// По умолчанию "=" → "key=value".
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Separators), System.ComponentModel.DisplayName(ActivityStrings.Field_KeyValueSeparator)]
        public string Prop_KeyValueSeparator
        {
            get => _propKeyValueSeparator;
            set { _propKeyValueSeparator = value; InvokePropertyChanged(this, "Prop_KeyValueSeparator"); }
        }

        // =========================================================================
        // OUTPUT PROPERTIES
        // =========================================================================

        private string _propResult;
        /// <summary>
        /// Результирующая строка.
        /// Формат: "key1=val1;key2=val2" (с настроенными разделителями).
        /// Ключи отсортированы алфавитно для стабильного и предсказуемого вывода.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ResultString)]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); }
        }

        private string _propCount;
        /// <summary>Количество пар вошедших в строку (равно размеру словаря)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_PairCount)]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        // =========================================================================
        // КОНСТРУКТОР
        // =========================================================================

        public DictionaryToStringBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Словарь: В строку";
            sdkComponentHelp =
                "Сериализует Dictionary<string, string> в строку.\n" +
                "Формат по умолчанию: \"key1=val1;key2=val2\"\n" +
                "Ключи сортируются алфавитно для стабильного вывода.\n\n" +
                "── Параметры ────────────────────────────────────────────\n" +
                "Словарь*                  — входной Dictionary<string, string>\n" +
                "Разделитель пар           — между парами (по умолчанию \";\")\n" +
                "Разделитель ключ-значение — внутри пары (по умолчанию \"=\")\n\n" +
                "── Выходные параметры ──────────────────────────────────\n" +
                "Результирующая строка — сериализованный словарь\n" +
                "Количество пар        — размер словаря\n\n" +
                "── Примеры форматов ─────────────────────────────────────\n" +
                "Пары=\";\", КЗ=\"=\"  → \"host=localhost;port=5432\"\n" +
                "Пары=\"|\", КЗ=\":\" → \"host:localhost|port:5432\"\n" +
                "Пары=\"\\n\", КЗ=\"=\" → многострочный формат";

            sdkComponentIcon = ActivityIcons.Dictionary;

                        sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<Dictionary<string, string>>("Prop_Dictionary", "Словарь Dictionary<string, string> для сериализации"),
                PropertyBuilder.Script<string>("Prop_PairSeparator", "Разделитель между парами ключ-значение (по умолчанию \";\")"),
                PropertyBuilder.Script<string>("Prop_KeyValueSeparator", "Разделитель между ключом и значением (по умолчанию \"=\")"),
                PropertyBuilder.Variable<string>("Prop_Result", "Результирующая строка в формате key1=val1;key2=val2"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество пар в строке")
            };

            InitClass(container);
            this.Prop_PairSeparator      = "\";\"";
            this.Prop_KeyValueSeparator  = "\"=\"";
        }

        // =========================================================================
        // ОСНОВНОЕ ДЕЙСТВИЕ
        // =========================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var    dict    = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary,      "Prop_Dictionary",      sd);
                string pairSep = GetPropertyValue<string>(this.Prop_PairSeparator,      "Prop_PairSeparator",      sd);
                string kvSep   = GetPropertyValue<string>(this.Prop_KeyValueSeparator,  "Prop_KeyValueSeparator",  sd);

                if (dict == null)
                    throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null");

                // Нормализация разделителей
                if (string.IsNullOrEmpty(pairSep)) pairSep = ";";
                if (string.IsNullOrEmpty(kvSep))   kvSep   = "=";

                // Сортируем по ключу для стабильного воспроизводимого вывода
                // Каждую пару форматируем через Select, объединяем через string.Join
                string result = string.Join(
                    pairSep,
                    dict.OrderBy(p => p.Key)
                        .Select(p => $"{p.Key}{kvSep}{p.Value}")
                );

                SetVariableValue(this.Prop_Result, result,      sd);
                SetVariableValue(this.Prop_Count,  dict.Count,  sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Словарь сериализован: {dict.Count} пар" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка сериализации: {ex.Message}" };
            }
        }

        // =========================================================================
        // ВАЛИДАЦИЯ
        // =========================================================================

        public override ValidationResult Validate()
        {
                        var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Dictionary, ActivityStrings.Field_Dictionary, ActivityStrings.Error_DictionaryRequired);
            return ret;
        }
    }
}
