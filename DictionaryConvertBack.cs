// =============================================================================
// DictionaryConvert.cs — две активности конвертации словарей:
//
//   DictionaryToStringBack   — «Словарь: В строку»
//     Сериализует Dictionary<string, string> в строку вида "key1=val1;key2=val2".
//     Разделители пар и разделитель ключ-значение настраиваются.
//
//   DictionaryFromStringBack — «Словарь: Из строки»
//     Десериализует строку вида "key1=val1;key2=val2" в Dictionary<string, string>.
//     Разделители пар и разделитель ключ-значение настраиваются.
//     Пустые и некорректные пары пропускаются.
//     При дублирующихся ключах — побеждает последнее значение.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.Enums;
using LTools.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    // =========================================================================
    // АКТИВНОСТЬ 1: DictionaryToString — Словарь: В строку
    // =========================================================================

    /// <summary>
    /// Активность «Словарь: В строку».
    /// Сериализует Dictionary&lt;string, string&gt; в строку вида "key1=val1;key2=val2".
    /// Разделители настраиваются. Ключи сортируются алфавитно для стабильного вывода.
    /// </summary>
    public class DictionaryToStringBack : PrimoComponentTO<DictionaryConvert>
    {
        private const string CGroupName = "MIA" + WFPublishedElementBase.TREE_SEPARATOR + "Словари";
        public override string GroupName { get => CGroupName; protected set { } }

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
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Словарь")]
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
        [System.ComponentModel.Category("Разделители"), System.ComponentModel.DisplayName("Разделитель пар")]
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
        [System.ComponentModel.Category("Разделители"), System.ComponentModel.DisplayName("Разделитель ключ-значение")]
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
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Результирующая строка")]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); }
        }

        private string _propCount;
        /// <summary>Количество пар вошедших в строку (равно размеру словаря)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Количество пар")]
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

            sdkComponentIcon = "pack://application:,,/Primo.SDKSample;component/Images/sample.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Dictionary", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(Dictionary<string, string>),
                    ToolTip = "Словарь Dictionary<string, string> для сериализации", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_PairSeparator", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Разделитель между парами ключ-значение (по умолчанию \";\")", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_KeyValueSeparator", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Разделитель между ключом и значением (по умолчанию \"=\")", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Result", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Результирующая строка в формате key1=val1;key2=val2", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Count", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(int),
                    ToolTip = "Количество пар в строке", IsReadOnly = false
                }
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
            if (string.IsNullOrWhiteSpace(this.Prop_Dictionary))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Словарь", Error = "Словарь обязателен" });
            return ret;
        }
    }

    // =========================================================================
    // АКТИВНОСТЬ 2: DictionaryFromString — Словарь: Из строки
    // =========================================================================

    /// <summary>
    /// Активность «Словарь: Из строки».
    /// Десериализует строку вида "key1=val1;key2=val2" в Dictionary&lt;string, string&gt;.
    /// Разделители настраиваются.
    /// Пустые и некорректные пары пропускаются с подсчётом.
    /// При дублирующихся ключах — побеждает последнее значение.
    /// Значение может содержать разделитель "=" — Split выполняется с лимитом 2 части.
    /// </summary>
    public class DictionaryFromStringBack : PrimoComponentSimple<LogMessage>
    {
        private const string CGroupName = "MIA";
        public override string GroupName { get => CGroupName; protected set { } }

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
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Входная строка")]
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
        [System.ComponentModel.Category("Разделители"), System.ComponentModel.DisplayName("Разделитель пар")]
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
        [System.ComponentModel.Category("Разделители"), System.ComponentModel.DisplayName("Разделитель ключ-значение")]
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
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Обрезать пробелы")]
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
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Результирующий словарь")]
        public string Prop_ResultDictionary
        {
            get => _propResultDictionary;
            set { _propResultDictionary = value; InvokePropertyChanged(this, "Prop_ResultDictionary"); }
        }

        private string _propCount;
        /// <summary>Количество успешно разобранных пар ключ-значение</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Разобрано пар")]
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
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Пропущено пар")]
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

            sdkComponentIcon = "pack://application:,,/Primo.SDKSample;component/Images/sample.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_InputString", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Входная строка в формате key1=val1;key2=val2", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_PairSeparator", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Разделитель между парами (по умолчанию \";\")", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_KeyValueSeparator", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Разделитель ключ-значение (по умолчанию \"=\")", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_ResultDictionary", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(Dictionary<string, string>),
                    ToolTip = "Десериализованный словарь Dictionary<string, string>", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Count", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(int),
                    ToolTip = "Количество успешно разобранных пар", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_SkippedCount", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(int),
                    ToolTip = "Количество пропущенных (некорректных или пустых) пар", IsReadOnly = false
                }
            };

            InitClass(container);
            this.Prop_PairSeparator     = "\";\"";
            this.Prop_KeyValueSeparator = "\"=\"";
        }

        // =========================================================================
        // ОСНОВНОЕ ДЕЙСТВИЕ
        // =========================================================================

        public override ExecutionResult SimpleAction(ScriptingData sd)
        {
            try
            {
                string input   = GetPropertyValue<string>(this.Prop_InputString,       "Prop_InputString",       sd);
                string pairSep = GetPropertyValue<string>(this.Prop_PairSeparator,     "Prop_PairSeparator",     sd);
                string kvSep   = GetPropertyValue<string>(this.Prop_KeyValueSeparator, "Prop_KeyValueSeparator", sd);

                if (string.IsNullOrEmpty(input))
                    throw new ArgumentException("Входная строка не может быть пустой");

                // Нормализация разделителей
                if (string.IsNullOrEmpty(pairSep)) pairSep = ";";
                if (string.IsNullOrEmpty(kvSep))   kvSep   = "=";

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
                        Key   = this.Prop_TrimWhitespace ? parts[0].Trim() : parts[0],
                        Value = this.Prop_TrimWhitespace ? parts[1].Trim() : parts[1]
                    })
                    .Where(pair => !string.IsNullOrEmpty(pair.Key))    // повторная проверка после Trim
                    .GroupBy(pair => pair.Key)                          // группируем по ключу для дублей
                    .ToDictionary(
                        group => group.Key,
                        group => group.Last().Value                    // дубль — побеждает последнее
                    );

                int skipped = totalRaw - result.Count;

                SetVariableValue(this.Prop_ResultDictionary, result,       sd);
                SetVariableValue(this.Prop_Count,            result.Count, sd);
                SetVariableValue(this.Prop_SkippedCount,     skipped,      sd);

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
            if (string.IsNullOrWhiteSpace(this.Prop_InputString))
                ret.Items.Add(new ValidationResult.ValidationItem()
                    { PropertyName = "Входная строка", Error = "Входная строка обязательна" });
            return ret;
        }
    }
}
