// =============================================================================
// DictionaryCreate.cs — активность «Словарь: Создать».
//
// Режимы (DictionaryCreateMode):
//   CreateEmpty — новый пустой Dictionary<string, string>
//   FromLists   — словарь из двух List<string> (ключи + значения)
//   Clone       — полная независимая копия существующего словаря
//   Invert      — инверсия: значения → ключи, ключи → значения
//
// Все режимы возвращают новый словарь в Prop_ResultDictionary.
// Входной словарь не изменяется.
// =============================================================================

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
    /// Активность «Словарь: Создать».
    /// Создаёт Dictionary&lt;string, string&gt; четырьмя способами:
    /// пустой, из списков, копия или инверсия существующего.
    /// </summary>
        public class DictionaryCreateBack : PrimoComponentTO<DictionaryCreate>
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

        private DictionaryCreateMode _mode = DictionaryCreateMode.CreateEmpty;
        /// <summary>Режим создания словаря</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Mode)]
        public DictionaryCreateMode Mode
        {
            get => _mode;
            set { _mode = value; InvokePropertyChanged(this, "Mode"); }
        }

        private string _propDictionary;
        /// <summary>
        /// Входной словарь для режимов Clone и Invert.
        /// В режимах CreateEmpty и FromLists — не используется.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Dictionary)]
        public string Prop_Dictionary
        {
            get => _propDictionary;
            set { _propDictionary = value; InvokePropertyChanged(this, "Prop_Dictionary"); }
        }

        private string _propKeysList;
        /// <summary>
        /// Список ключей для режима FromLists.
        /// Должен быть той же длины что и Prop_ValuesList.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_FromLists), System.ComponentModel.DisplayName(ActivityStrings.Field_KeysList)]
        public string Prop_KeysList
        {
            get => _propKeysList;
            set { _propKeysList = value; InvokePropertyChanged(this, "Prop_KeysList"); }
        }

        private string _propValuesList;
        /// <summary>
        /// Список значений для режима FromLists.
        /// Должен быть той же длины что и Prop_KeysList.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_FromLists), System.ComponentModel.DisplayName(ActivityStrings.Field_ValuesList)]
        public string Prop_ValuesList
        {
            get => _propValuesList;
            set { _propValuesList = value; InvokePropertyChanged(this, "Prop_ValuesList"); }
        }

        private bool _throwOnDuplicates = false;
        /// <summary>
        /// Поведение при дублирующихся значениях в режиме Invert.
        /// true  → выбросить исключение (значения должны быть уникальны).
        /// false → сохранить первое вхождение, остальные игнорировать.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Invert), System.ComponentModel.DisplayName(ActivityStrings.Field_ThrowOnDuplicates)]
        public bool Prop_ThrowOnDuplicates
        {
            get => _throwOnDuplicates;
            set { _throwOnDuplicates = value; InvokePropertyChanged(this, "Prop_ThrowOnDuplicates"); }
        }

        // =========================================================================
        // OUTPUT PROPERTIES
        // =========================================================================

        private string _propResultDictionary;
        /// <summary>Созданный словарь — результат операции</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ResultDictionary)]
        public string Prop_ResultDictionary
        {
            get => _propResultDictionary;
            set { _propResultDictionary = value; InvokePropertyChanged(this, "Prop_ResultDictionary"); }
        }

        private string _propCount;
        /// <summary>Количество элементов в созданном словаре</summary>
                [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Count)]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        private string _propDuplicatesCount;
        /// <summary>
        /// Количество значений-дублей пропущенных при инверсии (режим Invert).
        /// Заполняется только при Prop_ThrowOnDuplicates = false.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_DuplicatesCount)]
        public string Prop_DuplicatesCount
        {
            get => _propDuplicatesCount;
            set { _propDuplicatesCount = value; InvokePropertyChanged(this, "Prop_DuplicatesCount"); }
        }

        // =========================================================================
        // КОНСТРУКТОР
        // =========================================================================

        public DictionaryCreateBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Словарь: Создать";
            sdkComponentHelp =
                "Создаёт Dictionary<string, string> четырьмя способами.\n\n" +
                "── Режимы ──────────────────────────────────────────────\n" +
                "CreateEmpty — новый пустой словарь\n" +
                "FromLists   — из двух List<string> (ключи + значения)\n" +
                "Clone       — полная независимая копия словаря\n" +
                "Invert      — инверсия: значения ↔ ключи\n\n" +
                "── FromLists ───────────────────────────────────────────\n" +
                "Список ключей и список значений должны быть одинаковой длины.\n" +
                "При дублирующихся ключах — побеждает последнее значение.\n\n" +
                "── Invert ──────────────────────────────────────────────\n" +
                "Ошибка при дублях = true  → исключение если значения не уникальны\n" +
                "Ошибка при дублях = false → первое вхождение сохраняется\n\n" +
                "── Выходные параметры ──────────────────────────────────\n" +
                "Результирующий словарь — созданный Dictionary<string, string>\n" +
                "Количество элементов   — размер результата\n" +
                "Пропущено дублей       — кол-во пропущенных дублей (только Invert)";

            sdkComponentIcon = ActivityIcons.Dictionary;

                                    sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<DictionaryCreateMode>("Mode", "Режим: CreateEmpty / FromLists / Clone / Invert"),
                PropertyBuilder.Script<Dictionary<string, string>>("Prop_Dictionary", "Входной словарь (только для Clone и Invert)"),
                PropertyBuilder.Script<List<string>>("Prop_KeysList", "Список ключей (только для FromLists)"),
                PropertyBuilder.Script<List<string>>("Prop_ValuesList", "Список значений (только для FromLists)"),
                PropertyBuilder.BooleanObject("Prop_ThrowOnDuplicates", "Ошибка при дублях"),
                PropertyBuilder.Variable<Dictionary<string, string>>("Prop_ResultDictionary", "Созданный словарь"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество элементов в результирующем словаре"),
                PropertyBuilder.Variable<int>("Prop_DuplicatesCount", "Количество пропущенных дублей при инверсии (только Invert)")
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
                var dict       = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary, "Prop_Dictionary", sd);
                var keysList   = GetPropertyValue<List<string>>(this.Prop_KeysList,   "Prop_KeysList",   sd);
                var valuesList = GetPropertyValue<List<string>>(this.Prop_ValuesList, "Prop_ValuesList", sd);

                switch (this.Mode)
                {
                    case DictionaryCreateMode.CreateEmpty:
                        ExecuteCreateEmpty(sd);
                        break;
                    case DictionaryCreateMode.FromLists:
                        ExecuteFromLists(sd, keysList, valuesList);
                        break;
                    case DictionaryCreateMode.Clone:
                        ExecuteClone(sd, dict);
                        break;
                    case DictionaryCreateMode.Invert:
                        ExecuteInvert(sd, dict);
                        break;
                    default:
                        throw new InvalidOperationException($"Неизвестный режим: {this.Mode}");
                }

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Режим '{this.Mode}' выполнен" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка создания словаря: {ex.Message}" };
            }
        }

        // =========================================================================
        // РЕАЛИЗАЦИЯ РЕЖИМОВ
        // =========================================================================

        /// <summary>CreateEmpty — новый пустой словарь</summary>
        private void ExecuteCreateEmpty(ScriptingData sd)
        {
            var result = new Dictionary<string, string>();
            SetVariableValue(this.Prop_ResultDictionary, result, sd);
            SetVariableValue(this.Prop_Count,            0,      sd);
        }

        /// <summary>
        /// FromLists — создать словарь из двух List&lt;string&gt; одинаковой длины.
        /// LINQ Zip объединяет два списка попарно по индексу.
        /// При дублирующихся ключах побеждает последнее значение.
        /// </summary>
        private void ExecuteFromLists(ScriptingData sd, List<string> keysList, List<string> valuesList)
        {
            if (keysList == null)
                throw new ArgumentNullException("Prop_KeysList", "Список ключей не может быть null");
            if (valuesList == null)
                throw new ArgumentNullException("Prop_ValuesList", "Список значений не может быть null");
            if (keysList.Count != valuesList.Count)
                throw new ArgumentException(
                    $"Длина списка ключей ({keysList.Count}) " +
                    $"не совпадает с длиной списка значений ({valuesList.Count})");

            // Zip попарно объединяет списки, GroupBy обрабатывает дубли ключей
            var result = keysList
                .Zip(valuesList, (k, v) => new { Key = k, Value = v })
                .GroupBy(pair => pair.Key)
                .ToDictionary(
                    group => group.Key,
                    group => group.Last().Value   // при дубле побеждает последний
                );

            SetVariableValue(this.Prop_ResultDictionary, result,       sd);
            SetVariableValue(this.Prop_Count,            result.Count, sd);
        }

        /// <summary>
        /// Clone — создать полную независимую копию словаря.
        /// ToDictionary создаёт новый экземпляр — изменения в копии
        /// не затронут оригинал.
        /// </summary>
        private void ExecuteClone(ScriptingData sd, Dictionary<string, string> dict)
        {
            if (dict == null)
                throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null для режима Clone");

            var result = dict.ToDictionary(p => p.Key, p => p.Value);
            SetVariableValue(this.Prop_ResultDictionary, result,       sd);
            SetVariableValue(this.Prop_Count,            result.Count, sd);
        }

        /// <summary>
        /// Invert — инвертировать словарь: значения → ключи, ключи → значения.
        /// При дублирующихся значениях (будущих ключах):
        ///   ThrowOnDuplicates=true  → исключение со списком дублей
        ///   ThrowOnDuplicates=false → сохраняем первое вхождение через GroupBy + First
        /// </summary>
        private void ExecuteInvert(ScriptingData sd, Dictionary<string, string> dict)
        {
            if (dict == null)
                throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null для режима Invert");

            // Ищем значения которые встречаются более одного раза
            var duplicates = dict.Values
                .GroupBy(v => v)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any() && this.Prop_ThrowOnDuplicates)
                throw new InvalidOperationException(
                    $"Инверсия невозможна — дублирующиеся значения: {string.Join(", ", duplicates)}. " +
                    "Отключите 'Ошибка при дублях' чтобы сохранить первое вхождение.");

            // GroupBy по значению, берём первый ключ при дублях
            var result = dict
                .GroupBy(p => p.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Key
                );

            // Количество пропущенных = исходное кол-во - результирующее
            int skipped = dict.Count - result.Count;

            SetVariableValue(this.Prop_ResultDictionary, result,  sd);
            SetVariableValue(this.Prop_Count,            result.Count, sd);
            SetVariableValue(this.Prop_DuplicatesCount,  skipped, sd);
        }

        // =========================================================================
        // ВАЛИДАЦИЯ
        // =========================================================================

                        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            ret.ValidateRequired(this.Prop_ResultDictionary, ActivityStrings.Field_ResultDictionary, "Выходная переменная для результата обязательна");

            if (this.Mode == DictionaryCreateMode.Clone || this.Mode == DictionaryCreateMode.Invert)
                ret.ValidateRequired(this.Prop_Dictionary, ActivityStrings.Field_Dictionary,
                    $"Словарь обязателен для режима {this.Mode}");

            if (this.Mode == DictionaryCreateMode.FromLists)
            {
                ret.ValidateRequired(this.Prop_KeysList, ActivityStrings.Field_KeysList, "Список ключей обязателен для режима FromLists");
                ret.ValidateRequired(this.Prop_ValuesList, ActivityStrings.Field_ValuesList, "Список значений обязателен для режима FromLists");
            }

            return ret;
        }
    }
}
