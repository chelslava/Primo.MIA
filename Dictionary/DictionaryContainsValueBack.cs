using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Словарь: Проверить значение».
    /// Проверяет наличие точного совпадения значения в Dictionary&lt;string, string&gt;.
    /// Поддерживает регистрозависимый и регистронезависимый поиск.
    /// Не изменяет словарь.
    /// </summary>
    public class DictionaryContainsValueBack : PrimoComponentTO<DictionaryContainsValue>
    {
        private const string CGroupName = "MIA" + WFPublishedElementBase.TREE_SEPARATOR + "Словари";
        public override string GroupName { get => CGroupName; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        private string _propDictionary;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Словарь")]
        public string Prop_Dictionary
        {
            get => _propDictionary;
            set { _propDictionary = value; InvokePropertyChanged(this, "Prop_Dictionary"); }
        }

        private string _propSearchValue;
        /// <summary>Значение которое нужно найти в словаре (точное совпадение)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Искомое значение")]
        public string Prop_SearchValue
        {
            get => _propSearchValue;
            set { _propSearchValue = value; InvokePropertyChanged(this, "Prop_SearchValue"); }
        }

        private bool _caseSensitive = false;
        /// <summary>
        /// Учитывать регистр при сравнении значений.
        /// По умолчанию false — "Hello" == "hello".
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Учитывать регистр")]
        public bool Prop_CaseSensitive
        {
            get => _caseSensitive;
            set { _caseSensitive = value; InvokePropertyChanged(this, "Prop_CaseSensitive"); }
        }

        private string _propResult;
        /// <summary>True если значение найдено в словаре, False если нет</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Значение найдено")]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); }
        }

        private string _propFoundKeys;
        /// <summary>
        /// Список ключей у которых значение совпало с искомым.
        /// Удобно когда одно значение встречается в нескольких ключах.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Ключи с таким значением")]
        public string Prop_FoundKeys
        {
            get => _propFoundKeys;
            set { _propFoundKeys = value; InvokePropertyChanged(this, "Prop_FoundKeys"); }
        }

        public DictionaryContainsValueBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Словарь: Проверить значение";
            sdkComponentHelp =
                "Проверяет наличие значения в Dictionary<string, string> (точное совпадение).\n\n" +
                "Входные параметры:\n" +
                "  Словарь*        — Dictionary<string, string>\n" +
                "  Искомое значение* — строка для поиска\n" +
                "  Учитывать регистр — false (по умолчанию) = регистронезависимый поиск\n\n" +
                "Выходные параметры:\n" +
                "  Значение найдено      — true если есть хотя бы одно совпадение\n" +
                "  Ключи с таким значением — List<string> ключей с совпавшим значением";

            sdkComponentIcon = "pack://application:,,,/Primo.MIA;component/images/dict.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Dictionary", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(Dictionary<string, string>),
                    ToolTip = "Входной словарь", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_SearchValue", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Значение для поиска (точное совпадение)", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Result", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool),
                    ToolTip = "True если значение найдено хотя бы у одного ключа", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_FoundKeys", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>),
                    ToolTip = "Список ключей у которых значение совпало", IsReadOnly = false
                }
            };

            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var    dict      = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary,  "Prop_Dictionary",  sd);
                string searchVal = GetPropertyValue<string>(this.Prop_SearchValue, "Prop_SearchValue", sd);

                if (dict == null) throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null");
                if (searchVal == null) throw new ArgumentNullException("Prop_SearchValue", "Искомое значение не может быть null");

                StringComparison comparison = this.Prop_CaseSensitive
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

                // Через LINQ Where собираем все ключи с совпавшим значением
                List<string> foundKeys = dict
                    .Where(p => string.Equals(p.Value, searchVal, comparison))
                    .Select(p => p.Key)
                    .OrderBy(k => k)
                    .ToList();

                SetVariableValue(this.Prop_Result,     foundKeys.Any(), sd);
                SetVariableValue(this.Prop_FoundKeys,  foundKeys,       sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Значение '{searchVal}': найдено у {foundKeys.Count} ключей" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ValidateField(ret, this.Prop_Dictionary,  "Словарь",         "Словарь обязателен");
            ValidateField(ret, this.Prop_SearchValue, "Искомое значение", "Искомое значение обязательно");
            return ret;
        }

        private void ValidateField(ValidationResult result, string value, string fieldName, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
                result.Items.Add(new ValidationResult.ValidationItem() { PropertyName = fieldName, Error = errorMessage });
        }
    }
}
