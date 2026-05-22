using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Словарь: Из строки».
    /// Десериализует строку вида "key1=val1;key2=val2" в Dictionary&lt;string, string&gt;.
    /// Разделители настраиваются.
    /// Пустые и некорректные пары пропускаются с подсчётом.
    /// При дублирующихся ключах — побеждает последнее значение.
    /// Значение может содержать разделитель "=" — Split выполняется с лимитом 2 части.
    /// </summary>
    public class DictionaryFromStringBack : PrimoComponentTO<DictionaryFromString>
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

        private string _propInputString;
        /// <summary>
        /// Входная строка для десериализации.
        /// Формат по умолчанию: "key1=val1;key2=val2".
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_InputString)]
        public string Prop_InputString
        {
            get => _propInputString;
            set { _propInputString = value; InvokePropertyChanged(this, "Prop_InputString"); }
        }

        private string _propPairSeparator;
        /// <summary>
        /// Разделитель между парами.
        /// По умолчанию ";" → строка разбивается по точке с запятой.
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
        /// Разделитель между ключом и значением.
        /// По умолчанию "=" → "key=value".
        /// Важно: Split выполняется с лимитом 2 части — значение может содержать "=".
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Separators), System.ComponentModel.DisplayName(ActivityStrings.Field_KeyValueSeparator)]
        public string Prop_KeyValueSeparator
        {
            get => _propKeyValueSeparator;
            set { _propKeyValueSeparator = value; InvokePropertyChanged(this, "Prop_KeyValueSeparator"); }
        }

        private bool _trimWhitespace = true;
        /// <summary>
        /// Удалять пробелы в начале/конце ключей и значений.
        /// По умолчанию true — "  key  =  value  " → ключ "key", значение "value".
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_TrimWhitespace)]
        public bool Prop_TrimWhitespace
        {
            get => _trimWhitespace;
            set { _trimWhitespace = value; InvokePropertyChanged(this, "Prop_TrimWhitespace"); }
        }

        // =========================================================================
        // OUTPUT PROPERTIES
        // =========================================================================

        private string _propResultDictionary;
        /// <summary>Десериализованный словарь Dictionary&lt;string, string&gt;</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ResultDictionary)]
        public string Prop_ResultDictionary
        {
            get => _propResultDictionary;
            set { _propResultDictionary = value; InvokePropertyChanged(this, "Prop_ResultDictionary"); }
        }

        private string _propCount;
        /// <summary>Количество успешно разобранных пар ключ-значение</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ParsedPairs)]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        private string _propSkippedCount;
        /// <summary>
        /// Количество пропущенных пар (пустые строки, нет разделителя, пустой ключ).
        /// Удобно для диагностики качества входной строки.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_SkippedCount)]
        public string Prop_SkippedCount
        {
            get => _propSkippedCount;
            set { _propSkippedCount = value; InvokePropertyChanged(this, "Prop_SkippedCount"); }
        }

        // =========================================================================
        // КОНСТРУКТОР
        // =========================================================================

        public DictionaryFromStringBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Словарь: Из строки";
            sdkComponentHelp =
                "Десериализует строку в Dictionary<string, string>.\n" +
                "Формат по умолчанию: \"key1=val1;key2=val2\"\n\n" +
                "── Особенности ─────────────────────────────────────────\n" +
                "Значение может содержать разделитель ключ-значение:\n" +
                "  \"url=https://example.com?a=1\" → ключ \"url\", значение \"https://example.com?a=1\"\n" +
                "Пустые строки и пары без разделителя — пропускаются\n" +
                "При дублирующихся ключах — побеждает последнее значение\n\n" +
                "── Параметры ────────────────────────────────────────────\n" +
                "Входная строка*           — строка для десериализации\n" +
                "Разделитель пар           — между парами (по умолчанию \";\")\n" +
                "Разделитель ключ-значение — внутри пары (по умолчанию \"=\")\n" +
                "Обрезать пробелы          — удалять пробелы у ключей и значений\n\n" +
                "── Выходные параметры ──────────────────────────────────\n" +
                "Результирующий словарь — десериализованный Dictionary\n" +
                "Разобрано пар          — успешно обработанных пар\n" +
                "Пропущено пар          — пар с ошибками/пустых";

            sdkComponentIcon = ActivityIcons.Dictionary;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<string>("Prop_InputString", "Входная строка в формате key1=val1;key2=val2"),
                PropertyBuilder.Script<string>("Prop_PairSeparator", "Разделитель между парами (по умолчанию \";\")"),
                PropertyBuilder.Script<string>("Prop_KeyValueSeparator", "Разделитель ключ-значение (по умолчанию \"=\")"),
                PropertyBuilder.Variable<Dictionary<string, string>>("Prop_ResultDictionary", "Десериализованный словарь Dictionary<string, string>"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество успешно разобранных пар"),
                PropertyBuilder.Variable<int>("Prop_SkippedCount", "Количество пропущенных (некорректных или пустых) пар")
            };

            InitClass(container);
            this.Prop_PairSeparator = "\";\"";
            this.Prop_KeyValueSeparator = "\"=\"";
        }

        // =========================================================================
        // ОСНОВНОЕ ДЕЙСТВИЕ
        // =========================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string input = GetPropertyValue<string>(this.Prop_InputString, "Prop_InputString", sd);
                string pairSep = GetPropertyValue<string>(this.Prop_PairSeparator, "Prop_PairSeparator", sd);
                string kvSep = GetPropertyValue<string>(this.Prop_KeyValueSeparator, "Prop_KeyValueSeparator", sd);

                if (string.IsNullOrEmpty(input))
                    throw new ArgumentException("Входная строка не может быть пустой");

                // Нормализация разделителей
                if (string.IsNullOrEmpty(pairSep)) pairSep = ";";
                if (string.IsNullOrEmpty(kvSep)) kvSep = "=";

                // Шаг 1: разбиваем строку на сырые пары
                string[] rawPairs = input.Split(
                    new[] { pairSep }, StringSplitOptions.RemoveEmptyEntries);

                int totalRaw = rawPairs.Length;

                // Шаг 2: разбираем каждую пару на ключ-значение
                // Split с лимитом 2 — значение может содержать разделитель "="
                // GroupBy обрабатывает дублирующиеся ключи (побеждает последнее)
                var result = rawPairs
                    .Select(pair => pair.Split(new[] { kvSep }, 2, StringSplitOptions.None))
                    .Where(parts =>
                        parts.Length == 2 &&
                        !string.IsNullOrWhiteSpace(parts[0]))          // пропускаем пустые ключи
                    .Select(parts => new
                    {
                        Key = this.Prop_TrimWhitespace ? parts[0].Trim() : parts[0],
                        Value = this.Prop_TrimWhitespace ? parts[1].Trim() : parts[1]
                    })
                    .Where(pair => !string.IsNullOrEmpty(pair.Key))    // повторная проверка после Trim
                    .GroupBy(pair => pair.Key)                          // группируем по ключу для дублей
                    .ToDictionary(
                        group => group.Key,
                        group => group.Last().Value                    // дубль — побеждает последнее
                    );

                int skipped = totalRaw - result.Count;

                SetVariableValue(this.Prop_ResultDictionary, result, sd);
                SetVariableValue(this.Prop_Count, result.Count, sd);
                SetVariableValue(this.Prop_SkippedCount, skipped, sd);

                string msg = skipped > 0
                    ? $"Разобрано: {result.Count} пар, пропущено: {skipped}"
                    : $"Разобрано: {result.Count} пар";

                return new ExecutionResult { IsSuccess = true, SuccessMessage = msg };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка десериализации: {ex.Message}" };
            }
        }

        // =========================================================================
        // ВАЛИДАЦИЯ
        // =========================================================================

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_InputString, ActivityStrings.Field_InputString, ActivityStrings.Error_InputStringRequired);
            return ret;
        }
    }
}
