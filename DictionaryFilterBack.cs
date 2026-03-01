// =============================================================================
// DictionaryFilter.cs — активность «Словарь: Фильтровать».
//
// Режимы (DictionaryFilterMode):
//   ByKeys  — оставить только пары с ключами из List<string>
//   ByValue — оставить только пары где значение содержит подстроку
//
// Всегда возвращает новый словарь — оригинал не изменяется.
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
    /// <summary>
    /// Активность «Словарь: Фильтровать».
    /// Возвращает новый словарь содержащий только отфильтрованные пары.
    /// Оригинальный словарь не изменяется.
    /// </summary>
    public class DictionaryFilterBack : PrimoComponentTO<DictionaryFilter>
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

        private DictionaryFilterMode _mode = DictionaryFilterMode.ByKeys;
        /// <summary>Режим фильтрации: по ключам или по подстроке в значениях</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Режим фильтрации")]
        public DictionaryFilterMode Mode
        {
            get => _mode;
            set { _mode = value; InvokePropertyChanged(this, "Mode"); }
        }

        private string _propDictionary;
        /// <summary>Входной словарь Dictionary&lt;string, string&gt; для фильтрации</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Словарь")]
        public string Prop_Dictionary
        {
            get => _propDictionary;
            set { _propDictionary = value; InvokePropertyChanged(this, "Prop_Dictionary"); }
        }

        private string _propKeysList;
        /// <summary>
        /// Список ключей которые нужно оставить (режим ByKeys).
        /// Ключи из списка которых нет в словаре — игнорируются.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("По ключам"), System.ComponentModel.DisplayName("Список ключей")]
        public string Prop_KeysList
        {
            get => _propKeysList;
            set { _propKeysList = value; InvokePropertyChanged(this, "Prop_KeysList"); }
        }

        private string _propSubstring;
        /// <summary>
        /// Подстрока для поиска в значениях (режим ByValue).
        /// Оставляются только пары где значение содержит эту подстроку.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("По значению"), System.ComponentModel.DisplayName("Подстрока поиска")]
        public string Prop_Substring
        {
            get => _propSubstring;
            set { _propSubstring = value; InvokePropertyChanged(this, "Prop_Substring"); }
        }

        private bool _caseSensitive = false;
        /// <summary>
        /// Учитывать регистр при поиске подстроки (режим ByValue).
        /// По умолчанию false — "Hello" содержит "hello".
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("По значению"), System.ComponentModel.DisplayName("Учитывать регистр")]
        public bool Prop_CaseSensitive
        {
            get => _caseSensitive;
            set { _caseSensitive = value; InvokePropertyChanged(this, "Prop_CaseSensitive"); }
        }

        // =========================================================================
        // OUTPUT PROPERTIES
        // =========================================================================

        private string _propResultDictionary;
        /// <summary>Отфильтрованный словарь — подмножество входного</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Результирующий словарь")]
        public string Prop_ResultDictionary
        {
            get => _propResultDictionary;
            set { _propResultDictionary = value; InvokePropertyChanged(this, "Prop_ResultDictionary"); }
        }

        private string _propCount;
        /// <summary>Количество элементов прошедших фильтр</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Прошло фильтр")]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        private string _propFilteredOutCount;
        /// <summary>Количество элементов отсеянных фильтром (не прошедших условие)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Отсеяно фильтром")]
        public string Prop_FilteredOutCount
        {
            get => _propFilteredOutCount;
            set { _propFilteredOutCount = value; InvokePropertyChanged(this, "Prop_FilteredOutCount"); }
        }

        // =========================================================================
        // КОНСТРУКТОР
        // =========================================================================

        public DictionaryFilterBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Словарь: Фильтровать";
            sdkComponentHelp =
                "Фильтрует Dictionary<string, string> — возвращает новый словарь\n" +
                "содержащий только элементы прошедшие условие фильтра.\n" +
                "Оригинальный словарь не изменяется.\n\n" +
                "── Режимы ──────────────────────────────────────────────\n" +
                "ByKeys  — оставить только ключи из List<string>\n" +
                "          Ключи из списка которых нет в словаре игнорируются.\n" +
                "ByValue — оставить пары где значение содержит подстроку\n" +
                "          Поддерживает настройку регистрозависимости.\n\n" +
                "── Выходные параметры ──────────────────────────────────\n" +
                "Результирующий словарь — элементы прошедшие фильтр\n" +
                "Прошло фильтр          — количество элементов в результате\n" +
                "Отсеяно фильтром       — количество пропущенных элементов";

            sdkComponentIcon = "pack://application:,,/Primo.SDKSample;component/Images/sample.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Mode", PropertyType = PropertyTypes.OBJECT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(DictionaryFilterMode),
                    ToolTip = "Режим фильтрации: ByKeys (по ключам) / ByValue (по подстроке)", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Dictionary", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(Dictionary<string, string>),
                    ToolTip = "Входной словарь для фильтрации", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_KeysList", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>),
                    ToolTip = "Список ключей которые нужно оставить (режим ByKeys)", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Substring", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Подстрока для поиска в значениях (режим ByValue)", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_ResultDictionary", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(Dictionary<string, string>),
                    ToolTip = "Отфильтрованный словарь", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Count", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(int),
                    ToolTip = "Количество элементов прошедших фильтр", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_FilteredOutCount", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(int),
                    ToolTip = "Количество элементов отсеянных фильтром", IsReadOnly = false
                }
            };

            InitClass(container);
        }

        // =========================================================================
        // ОСНОВНОЕ ДЕЙСТВИЕ
        // =========================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var    dict      = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary, "Prop_Dictionary", sd);
                var    keysList  = GetPropertyValue<List<string>>(this.Prop_KeysList,  "Prop_KeysList",  sd);
                string substring = GetPropertyValue<string>(this.Prop_Substring, "Prop_Substring", sd);

                if (dict == null)
                    throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null");

                Dictionary<string, string> result;

                switch (this.Mode)
                {
                    case DictionaryFilterMode.ByKeys:
                        result = ExecuteFilterByKeys(dict, keysList);
                        break;
                    case DictionaryFilterMode.ByValue:
                        result = ExecuteFilterByValue(dict, substring ?? string.Empty);
                        break;
                    default:
                        throw new InvalidOperationException($"Неизвестный режим: {this.Mode}");
                }

                int filteredOut = dict.Count - result.Count;

                SetVariableValue(this.Prop_ResultDictionary, result,      sd);
                SetVariableValue(this.Prop_Count,            result.Count, sd);
                SetVariableValue(this.Prop_FilteredOutCount, filteredOut,  sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Прошло фильтр: {result.Count}, отсеяно: {filteredOut}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка фильтрации: {ex.Message}" };
            }
        }

        // =========================================================================
        // РЕАЛИЗАЦИЯ РЕЖИМОВ
        // =========================================================================

        /// <summary>
        /// ByKeys — оставить только пары с ключами из списка.
        /// HashSet обеспечивает O(1) поиск при большом списке ключей.
        /// </summary>
        private Dictionary<string, string> ExecuteFilterByKeys(
            Dictionary<string, string> dict, List<string> keysList)
        {
            if (keysList == null)
                throw new ArgumentNullException("Prop_KeysList", "Список ключей не может быть null для режима ByKeys");

            // HashSet для O(1) поиска вместо O(n) у List
            var allowedKeys = new HashSet<string>(keysList);

            return dict
                .Where(p => allowedKeys.Contains(p.Key))
                .ToDictionary(p => p.Key, p => p.Value);
        }

        /// <summary>
        /// ByValue — оставить только пары где значение содержит подстроку.
        /// StringComparison определяет регистрозависимость поиска.
        /// </summary>
        private Dictionary<string, string> ExecuteFilterByValue(
            Dictionary<string, string> dict, string substring)
        {
            StringComparison comparison = this.Prop_CaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            return dict
                .Where(p => p.Value != null
                         && p.Value.IndexOf(substring, comparison) >= 0)
                .ToDictionary(p => p.Key, p => p.Value);
        }

        // =========================================================================
        // ВАЛИДАЦИЯ
        // =========================================================================

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ValidateField(ret, this.Prop_Dictionary, "Словарь", "Словарь обязателен");

            if (this.Mode == DictionaryFilterMode.ByKeys)
                ValidateField(ret, this.Prop_KeysList, "Список ключей",
                    "Список ключей обязателен для режима ByKeys");

            if (this.Mode == DictionaryFilterMode.ByValue)
                ValidateField(ret, this.Prop_Substring, "Подстрока поиска",
                    "Подстрока обязательна для режима ByValue");

            return ret;
        }

        private void ValidateField(ValidationResult result, string value, string fieldName, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
                result.Items.Add(new ValidationResult.ValidationItem() { PropertyName = fieldName, Error = errorMessage });
        }
    }
}
