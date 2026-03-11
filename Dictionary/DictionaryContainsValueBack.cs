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
    /// Активность «Словарь: Проверить значение».
    /// Проверяет наличие точного совпадения значения в Dictionary&lt;string, string&gt;.
    /// Поддерживает регистрозависимый и регистронезависимый поиск.
    /// Не изменяет словарь.
    /// </summary>
    public class DictionaryContainsValueBack : PrimoComponentTO<DictionaryContainsValue>
    {
        public override string GroupName { get => ActivityCategories.Dictionaries; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        private string _propDictionary;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Dictionary)]
        public string Prop_Dictionary
        {
            get => _propDictionary;
            set { _propDictionary = value; InvokePropertyChanged(this, "Prop_Dictionary"); }
        }

        private string _propSearchValue;
        /// <summary>Значение которое нужно найти в словаре (точное совпадение)</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SearchValue)]
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
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CaseSensitive)]
        public bool Prop_CaseSensitive
        {
            get => _caseSensitive;
            set { _caseSensitive = value; InvokePropertyChanged(this, "Prop_CaseSensitive"); }
        }

        private string _propResult;
        /// <summary>True если значение найдено в словаре, False если нет</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Result)]
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
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_FoundKeys)]
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

            sdkComponentIcon = ActivityIcons.Dictionary;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<Dictionary<string, string>>("Prop_Dictionary", "Входной словарь"),
                PropertyBuilder.Script<string>("Prop_SearchValue", "Значение для поиска (точное совпадение)"),
                PropertyBuilder.Variable<bool>("Prop_Result", "True если значение найдено хотя бы у одного ключа"),
                PropertyBuilder.Variable<List<string>>("Prop_FoundKeys", "Список ключей у которых значение совпало")
            };

            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var dict = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary, "Prop_Dictionary", sd);
                string searchVal = GetPropertyValue<string>(this.Prop_SearchValue, "Prop_SearchValue", sd);

                if (dict == null) throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null");
                if (searchVal == null) throw new ArgumentNullException("Prop_SearchValue", "Искомое значение не может быть null");

                StringComparison comparison = ComparisonHelper.GetStringComparison(this.Prop_CaseSensitive);

                // Через LINQ Where собираем все ключи с совпавшим значением
                List<string> foundKeys = dict
                    .Where(p => string.Equals(p.Value, searchVal, comparison))
                    .Select(p => p.Key)
                    .OrderBy(k => k)
                    .ToList();

                SetVariableValue(this.Prop_Result, foundKeys.Any(), sd);
                SetVariableValue(this.Prop_FoundKeys, foundKeys, sd);

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
            ret.ValidateRequired(this.Prop_Dictionary, ActivityStrings.Field_Dictionary, ActivityStrings.Error_DictionaryRequired);
            ret.ValidateRequired(this.Prop_SearchValue, ActivityStrings.Field_SearchValue, "Искомое значение обязательно");
            return ret;
        }
    }
}
