// =============================================================================
// DictionaryOperationsSmall.cs — четыре компактные активности:
//
//   DictionaryRemoveKeyBack    — Словарь: Удалить ключ
//   DictionaryContainsKeyBack  — Словарь: Проверить ключ
//   DictionaryContainsValueBack — Словарь: Проверить значение
//   DictionaryGetInfoBack      — Словарь: Информация (Count + Keys + Values)
//
// Каждая активность делает ровно одно действие и не изменяет входной словарь
// (кроме RemoveKey — возвращает новую копию без ключа).
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
    // АКТИВНОСТЬ 1: DictionaryRemoveKey — Словарь: Удалить ключ
    // =========================================================================

    /// <summary>
    /// Активность «Словарь: Удалить ключ».
    /// Возвращает новую копию словаря без указанного ключа.
    /// Оригинальный словарь не изменяется.
    /// Если ключ не найден — поведение задаётся флагом Prop_ThrowIfNotFound.
    /// </summary>
    public class DictionaryRemoveKeyBack : PrimoComponentTO<DictionaryOperationsSmall>
    {
        private const string CGroupName = "MIA" + WFPublishedElementBase.TREE_SEPARATOR + "Словари";
        public override string GroupName { get => CGroupName; protected set { } }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        private string _propDictionary;
        /// <summary>Входной словарь Dictionary&lt;string, string&gt;</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Словарь")]
        public string Prop_Dictionary
        {
            get => _propDictionary;
            set { _propDictionary = value; InvokePropertyChanged(this, "Prop_Dictionary"); }
        }

        private string _propKey;
        /// <summary>Ключ который нужно удалить из словаря</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Ключ")]
        public string Prop_Key
        {
            get => _propKey;
            set { _propKey = value; InvokePropertyChanged(this, "Prop_Key"); }
        }

        private bool _throwIfNotFound = false;
        /// <summary>
        /// Если true — выбросить KeyNotFoundException если ключ не найден.
        /// Если false — вернуть копию словаря без изменений.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Ошибка если не найдено")]
        public bool Prop_ThrowIfNotFound
        {
            get => _throwIfNotFound;
            set { _throwIfNotFound = value; InvokePropertyChanged(this, "Prop_ThrowIfNotFound"); }
        }

        private string _propResultDictionary;
        /// <summary>Новый словарь без удалённого ключа</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Результирующий словарь")]
        public string Prop_ResultDictionary
        {
            get => _propResultDictionary;
            set { _propResultDictionary = value; InvokePropertyChanged(this, "Prop_ResultDictionary"); }
        }

        private string _propRemoved;
        /// <summary>True если ключ был найден и удалён, False если ключа не было</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Ключ удалён")]
        public string Prop_Removed
        {
            get => _propRemoved;
            set { _propRemoved = value; InvokePropertyChanged(this, "Prop_Removed"); }
        }

        public DictionaryRemoveKeyBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Словарь: Удалить ключ";
            sdkComponentHelp =
                "Удаляет ключ из Dictionary<string, string>.\n" +
                "Возвращает новую копию словаря — оригинал не изменяется.\n\n" +
                "Входные параметры:\n" +
                "  Словарь*               — Dictionary<string, string>\n" +
                "  Ключ*                  — ключ для удаления\n" +
                "  Ошибка если не найдено — поведение при отсутствии ключа\n\n" +
                "Выходные параметры:\n" +
                "  Результирующий словарь — копия без удалённого ключа\n" +
                "  Ключ удалён            — true если ключ существовал и был удалён";

            sdkComponentIcon = "pack://application:,,/Primo.SDKSample;component/Images/sample.png";

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
                    PropName = "Prop_Key", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Ключ для удаления", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_ResultDictionary", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(Dictionary<string, string>),
                    ToolTip = "Новый словарь без удалённого ключа", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Removed", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool),
                    ToolTip = "True если ключ был найден и удалён", IsReadOnly = false
                }
            };

            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var    dict = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary, "Prop_Dictionary", sd);
                string key  = GetPropertyValue<string>(this.Prop_Key, "Prop_Key", sd);

                if (dict == null) throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null");
                if (string.IsNullOrEmpty(key)) throw new ArgumentException("Ключ не может быть пустым");

                bool exists = dict.ContainsKey(key);

                if (!exists && this.Prop_ThrowIfNotFound)
                    throw new KeyNotFoundException(
                        $"Ключ '{key}' не найден в словаре. " +
                        $"Доступные ключи: {string.Join(", ", dict.Keys.OrderBy(k => k))}");

                // Создаём новый словарь через LINQ Where — исключаем удаляемый ключ
                var result = dict
                    .Where(p => p.Key != key)
                    .ToDictionary(p => p.Key, p => p.Value);

                SetVariableValue(this.Prop_ResultDictionary, result,  sd);
                SetVariableValue(this.Prop_Removed,          exists,  sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = exists ? $"Ключ '{key}' удалён" : $"Ключ '{key}' не найден" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ValidateField(ret, this.Prop_Dictionary, "Словарь", "Словарь обязателен");
            ValidateField(ret, this.Prop_Key,        "Ключ",    "Ключ обязателен");
            return ret;
        }

        private void ValidateField(ValidationResult result, string value, string fieldName, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
                result.Items.Add(new ValidationResult.ValidationItem() { PropertyName = fieldName, Error = errorMessage });
        }
    }

    // =========================================================================
    // АКТИВНОСТЬ 2: DictionaryContainsKey — Словарь: Проверить ключ
    // =========================================================================

    /// <summary>
    /// Активность «Словарь: Проверить ключ».
    /// Проверяет наличие ключа в Dictionary&lt;string, string&gt; → bool.
    /// Не изменяет словарь.
    /// </summary>
    public class DictionaryContainsKeyBack : PrimoComponentSimple<LogMessage>
    {
        private const string CGroupName = "MIA";
        public override string GroupName { get => CGroupName; protected set { } }

        private string _propDictionary;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Словарь")]
        public string Prop_Dictionary
        {
            get => _propDictionary;
            set { _propDictionary = value; InvokePropertyChanged(this, "Prop_Dictionary"); }
        }

        private string _propKey;
        /// <summary>Ключ для проверки существования в словаре</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Ключ")]
        public string Prop_Key
        {
            get => _propKey;
            set { _propKey = value; InvokePropertyChanged(this, "Prop_Key"); }
        }

        private string _propResult;
        /// <summary>True если ключ присутствует в словаре, False если нет</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Ключ существует")]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); }
        }

        public DictionaryContainsKeyBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Словарь: Проверить ключ";
            sdkComponentHelp =
                "Проверяет наличие ключа в Dictionary<string, string>.\n\n" +
                "Входные параметры:\n" +
                "  Словарь* — Dictionary<string, string>\n" +
                "  Ключ*    — проверяемый ключ\n\n" +
                "Выходные параметры:\n" +
                "  Ключ существует — true если ключ есть в словаре";

            sdkComponentIcon = "pack://application:,,/Primo.SDKSample;component/Images/sample.png";

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
                    PropName = "Prop_Key", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(string),
                    ToolTip = "Ключ для проверки", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Result", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool),
                    ToolTip = "True если ключ найден в словаре", IsReadOnly = false
                }
            };

            InitClass(container);
        }

        public override ExecutionResult SimpleAction(ScriptingData sd)
        {
            try
            {
                var    dict = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary, "Prop_Dictionary", sd);
                string key  = GetPropertyValue<string>(this.Prop_Key, "Prop_Key", sd);

                if (dict == null) throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null");
                if (string.IsNullOrEmpty(key)) throw new ArgumentException("Ключ не может быть пустым");

                bool result = dict.ContainsKey(key);
                SetVariableValue(this.Prop_Result, result, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Ключ '{key}': {(result ? "найден" : "не найден")}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ValidateField(ret, this.Prop_Dictionary, "Словарь", "Словарь обязателен");
            ValidateField(ret, this.Prop_Key,        "Ключ",    "Ключ обязателен");
            return ret;
        }

        private void ValidateField(ValidationResult result, string value, string fieldName, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
                result.Items.Add(new ValidationResult.ValidationItem() { PropertyName = fieldName, Error = errorMessage });
        }
    }

    // =========================================================================
    // АКТИВНОСТЬ 3: DictionaryContainsValue — Словарь: Проверить значение
    // =========================================================================

    /// <summary>
    /// Активность «Словарь: Проверить значение».
    /// Проверяет наличие точного совпадения значения в Dictionary&lt;string, string&gt;.
    /// Поддерживает регистрозависимый и регистронезависимый поиск.
    /// Не изменяет словарь.
    /// </summary>
    public class DictionaryContainsValueBack : PrimoComponentSimple<LogMessage>
    {
        private const string CGroupName = "MIA";
        public override string GroupName { get => CGroupName; protected set { } }

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

            sdkComponentIcon = "pack://application:,,/Primo.SDKSample;component/Images/sample.png";

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

        public override ExecutionResult SimpleAction(ScriptingData sd)
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

    // =========================================================================
    // АКТИВНОСТЬ 4: DictionaryGetInfo — Словарь: Информация
    // =========================================================================

    /// <summary>
    /// Активность «Словарь: Информация».
    /// За один шаг возвращает полную информацию о словаре:
    /// количество элементов, список ключей и список значений.
    /// Не изменяет словарь.
    /// </summary>
    public class DictionaryGetInfoBack : PrimoComponentTO<LogMessage>
    {
        private const string CGroupName = "MIA";
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

        private string _propCount;
        /// <summary>Количество пар ключ-значение в словаре</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Количество элементов")]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        private string _propKeys;
        /// <summary>Все ключи словаря в алфавитном порядке</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Список ключей")]
        public string Prop_Keys
        {
            get => _propKeys;
            set { _propKeys = value; InvokePropertyChanged(this, "Prop_Keys"); }
        }

        private string _propValues;
        /// <summary>
        /// Все значения словаря в порядке алфавитной сортировки ключей.
        /// Порядок значений соответствует порядку ключей в Prop_Keys.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Список значений")]
        public string Prop_Values
        {
            get => _propValues;
            set { _propValues = value; InvokePropertyChanged(this, "Prop_Values"); }
        }

        private string _propIsEmpty;
        /// <summary>True если словарь не содержит ни одного элемента</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Выходные данные"), System.ComponentModel.DisplayName("Словарь пуст")]
        public string Prop_IsEmpty
        {
            get => _propIsEmpty;
            set { _propIsEmpty = value; InvokePropertyChanged(this, "Prop_IsEmpty"); }
        }

        public DictionaryGetInfoBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Словарь: Информация";
            sdkComponentHelp =
                "Возвращает полную информацию о Dictionary<string, string>:\n" +
                "количество элементов, список ключей и список значений.\n" +
                "Ключи и значения возвращаются в алфавитном порядке ключей.\n" +
                "Не изменяет словарь.\n\n" +
                "Входные параметры:\n" +
                "  Словарь* — Dictionary<string, string>\n\n" +
                "Выходные параметры:\n" +
                "  Количество элементов — int, размер словаря\n" +
                "  Список ключей        — List<string> в алфавитном порядке\n" +
                "  Список значений      — List<string> в том же порядке что и ключи\n" +
                "  Словарь пуст         — true если Count == 0";

            sdkComponentIcon = "pack://application:,,/Primo.SDKSample;component/Images/sample.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Dictionary", PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(Dictionary<string, string>),
                    ToolTip = "Входной словарь Dictionary<string, string>", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Count", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(int),
                    ToolTip = "Количество элементов в словаре", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Keys", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>),
                    ToolTip = "Все ключи словаря (алфавитный порядок)", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Values", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(List<string>),
                    ToolTip = "Все значения в порядке ключей", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_IsEmpty", PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE, DataType = typeof(bool),
                    ToolTip = "True если словарь пустой", IsReadOnly = false
                }
            };

            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var dict = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary, "Prop_Dictionary", sd);

                if (dict == null)
                    throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null");

                // Сортируем один раз — и ключи и значения в одном порядке
                var sorted = dict.OrderBy(p => p.Key).ToList();

                List<string> keys   = sorted.Select(p => p.Key).ToList();
                List<string> values = sorted.Select(p => p.Value).ToList();

                SetVariableValue(this.Prop_Count,   dict.Count,      sd);
                SetVariableValue(this.Prop_Keys,    keys,            sd);
                SetVariableValue(this.Prop_Values,  values,          sd);
                SetVariableValue(this.Prop_IsEmpty, dict.Count == 0, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Словарь содержит {dict.Count} элементов" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            if (string.IsNullOrWhiteSpace(this.Prop_Dictionary))
                ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Словарь", Error = "Словарь обязателен" });
            return ret;
        }
    }
}
