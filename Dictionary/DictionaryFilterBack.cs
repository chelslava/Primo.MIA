// =============================================================================
// DictionaryFilter.cs — активность «Словарь: Фильтровать».
//
// Логика фильтрации разделена на два независимых параметра:
//
//   DictionaryFilterTarget (где искать):
//     Keys        — искать/проверять по ключам словаря
//     Values      — искать/проверять по значениям словаря
//     KeysAndValues — совпадение в ключе ИЛИ в значении
//
//   DictionaryFilterMethod (как искать):
//     Contains    — содержит подстроку
//     Exact       — точное совпадение
//     Regex       — регулярное выражение
//     Wildcard    — wildcard-паттерн (* и ?)
//
//   Prop_CaseSensitive — регистронезависимость, работает для всех методов.
//
// Всегда возвращает новый словарь — оригинал не изменяется.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.Enums;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Словарь: Фильтровать».
    /// Фильтрует Dictionary&lt;string, string&gt; по заданному условию.
    /// Параметр Target определяет где искать (ключи / значения / оба).
    /// Параметр Method определяет как искать (подстрока / точно / regex / wildcard).
    /// Оригинальный словарь не изменяется — всегда возвращается новый.
    /// </summary>
        public class DictionaryFilterBack : PrimoComponentTO<DictionaryFilter>
    {
        public override string GroupName { get => ActivityCategories.Dictionaries; protected set { } }

        protected override int sdkTimeOut { get => 10000; set { } }

        // =========================================================================
        // INPUT PROPERTIES
        // =========================================================================

        private string _propDictionary;
        /// <summary>Входной словарь Dictionary&lt;string, string&gt; для фильтрации</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Dictionary)]
        public string Prop_Dictionary
        {
            get => _propDictionary;
            set { _propDictionary = value; InvokePropertyChanged(this, "Prop_Dictionary"); }
        }

        private string _propQuery;
        /// <summary>
        /// Строка-запрос: подстрока, точная строка, regex-паттерн или wildcard-паттерн.
        /// Интерпретация зависит от выбранного метода поиска (Prop_Method).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Query)]
        public string Prop_Query
        {
            get => _propQuery;
            set { _propQuery = value; InvokePropertyChanged(this, "Prop_Query"); }
        }

        private DictionaryFilterTarget _target = DictionaryFilterTarget.Values;
        /// <summary>Где искать: Keys / Values / KeysAndValues</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Target)]
        public DictionaryFilterTarget Prop_Target
        {
            get => _target;
            set { _target = value; InvokePropertyChanged(this, "Prop_Target"); }
        }

        private DictionaryFilterMethod _method = DictionaryFilterMethod.Contains;
        /// <summary>Метод поиска: Contains / Exact / Regex / Wildcard</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Method)]
        public DictionaryFilterMethod Prop_Method
        {
            get => _method;
            set { _method = value; InvokePropertyChanged(this, "Prop_Method"); }
        }

        private bool _caseSensitive = false;
        /// <summary>
        /// Учитывать регистр при сравнении.
        /// Работает для всех методов поиска и для обоих полей (ключ и значение).
        /// По умолчанию false — поиск без учёта регистра.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CaseSensitive)]
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
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ResultDictionary)]
        public string Prop_ResultDictionary
        {
            get => _propResultDictionary;
            set { _propResultDictionary = value; InvokePropertyChanged(this, "Prop_ResultDictionary"); }
        }

        private string _propCount;
        /// <summary>Количество элементов прошедших фильтр</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_Count)]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        private string _propFilteredOutCount;
        /// <summary>Количество элементов отсеянных фильтром</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_FilteredOutCount)]
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
                "Фильтрует Dictionary<string, string>.\n" +
                "Параметры независимы — любая комбинация Target + Method допустима.\n\n" +
                "── Где искать (Target) ──────────────────────────────────────\n" +
                "Keys         — условие применяется к ключам\n" +
                "Values       — условие применяется к значениям\n" +
                "KeysAndValues — пара проходит если условие выполнено для ключа ИЛИ значения\n\n" +
                "── Метод поиска (Method) ────────────────────────────────────\n" +
                "Contains — строка содержит подстроку\n" +
                "Exact    — строка равна запросу полностью\n" +
                "Regex    — строка соответствует регулярному выражению\n" +
                "           Пример: ^\\d{4}-\\d{2}-\\d{2}$\n" +
                "Wildcard — строка соответствует wildcard-паттерну\n" +
                "           * = любое кол-во символов, ? = один символ\n" +
                "           Пример: order_*_2024 или user_?@domain.com\n\n" +
                "── Регистр ──────────────────────────────────────────────────\n" +
                "Учитывать регистр — работает для всех методов и полей.";

            sdkComponentIcon = ActivityIcons.Dictionary;

                                    sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<Dictionary<string, string>>("Prop_Dictionary", "Входной словарь для фильтрации"),
                PropertyBuilder.Script<string>("Prop_Query", "Подстрока, точная строка, regex или wildcard — зависит от Метода поиска"),
                PropertyBuilder.Enum<DictionaryFilterTarget>("Prop_Target", "Где искать: Keys / Values / KeysAndValues"),
                PropertyBuilder.Enum<DictionaryFilterMethod>("Prop_Method", "Метод поиска: Contains / Exact / Regex / Wildcard"),
                PropertyBuilder.BooleanObject("Prop_CaseSensitive", "Учитывать регистр (применяется ко всем методам)"),
                PropertyBuilder.Variable<Dictionary<string, string>>("Prop_ResultDictionary", "Отфильтрованный словарь"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество элементов прошедших фильтр"),
                PropertyBuilder.Variable<int>("Prop_FilteredOutCount", "Количество элементов отсеянных фильтром")
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
                var dict = GetPropertyValue<Dictionary<string, string>>(this.Prop_Dictionary, "Prop_Dictionary", sd);
                string query = GetPropertyValue<string>(this.Prop_Query, "Prop_Query", sd);

                if (dict == null)
                    throw new ArgumentNullException("Prop_Dictionary", "Словарь не может быть null");

                if (query == null)
                    throw new ArgumentNullException("Prop_Query", "Строка поиска не может быть null");

                // Строим предикат "как проверять одну строку" из выбранного метода
                Func<string, bool> matchFunc = BuildMatchFunction(query);

                // Применяем предикат к нужному полю пары согласно Target
                var result = dict
                    .Where(p => ApplyTarget(p, matchFunc))
                    .ToDictionary(p => p.Key, p => p.Value);

                int filteredOut = dict.Count - result.Count;

                // Записываем выходные переменные
                SetVariableValue(this.Prop_ResultDictionary, result, sd);
                SetVariableValue(this.Prop_Count, result.Count, sd);
                SetVariableValue(this.Prop_FilteredOutCount, filteredOut, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Прошло фильтр: {result.Count}, отсеяно: {filteredOut}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка фильтрации: {ex.Message}"
                };
            }
        }

        // =========================================================================
        // ПОСТРОЕНИЕ ПРЕДИКАТА МЕТОДА ПОИСКА
        // =========================================================================

        /// <summary>
        /// Фабрика предикатов: по выбранному Prop_Method возвращает функцию
        /// bool(string) которая проверяет соответствует ли переданная строка условию.
        /// Все варианты учитывают флаг Prop_CaseSensitive.
        /// </summary>
        private Func<string, bool> BuildMatchFunction(string query)
        {
            switch (this.Prop_Method)
            {
                case DictionaryFilterMethod.Contains:
                    return BuildContainsFunc(query);

                case DictionaryFilterMethod.Exact:
                    return BuildExactFunc(query);

                case DictionaryFilterMethod.Regex:
                    return BuildRegexFunc(query);

                case DictionaryFilterMethod.Wildcard:
                    return BuildWildcardFunc(query);

                default:
                    throw new InvalidOperationException($"Неизвестный метод поиска: {this.Prop_Method}");
            }
        }

        /// <summary>
        /// Contains — строка содержит подстроку query.
        /// Использует IndexOf с нужным StringComparison для учёта регистра.
        /// </summary>
        private Func<string, bool> BuildContainsFunc(string query)
        {
            StringComparison cmp = this.Prop_CaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            // Null-безопасно: если проверяемая строка null — не проходит фильтр
            return s => s != null && s.IndexOf(query, cmp) >= 0;
        }

        /// <summary>
        /// Exact — строка полностью равна query.
        /// Использует StringComparer для учёта регистра.
        /// </summary>
        private Func<string, bool> BuildExactFunc(string query)
        {
            StringComparison cmp = this.Prop_CaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            return s => s != null && string.Equals(s, query, cmp);
        }

        /// <summary>
        /// Regex — строка соответствует регулярному выражению.
        /// Паттерн компилируется один раз через RegexOptions.Compiled.
        /// Проверяет полное совпадение (^ ... $) чтобы поведение было предсказуемым.
        /// Для частичного поиска пользователь может убрать якоря из своего паттерна.
        /// </summary>
        private Func<string, bool> BuildRegexFunc(string query)
        {
            if (string.IsNullOrEmpty(query))
                throw new ArgumentException(
                    "Regex-паттерн не может быть пустым", "Prop_Query");

            var options = RegexOptions.Compiled;
            if (!this.Prop_CaseSensitive)
                options |= RegexOptions.IgnoreCase;

            Regex regex;
            try
            {
                regex = new Regex(query, options);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException(
                    $"Некорректный regex-паттерн '{query}': {ex.Message}", "Prop_Query", ex);
            }

            return s => s != null && regex.IsMatch(s);
        }

        /// <summary>
        /// Wildcard — строка соответствует wildcard-паттерну.
        /// Символы паттерна:
        ///   * — любое количество любых символов (включая пустую строку)
        ///   ? — ровно один любой символ
        /// Реализация: wildcard транслируется в regex через Select + Regex.Escape.
        /// Добавляются якоря ^ и $ — сопоставление всегда полное.
        /// </summary>
        private Func<string, bool> BuildWildcardFunc(string query)
        {
            if (string.IsNullOrEmpty(query))
                throw new ArgumentException(
                    "Wildcard-паттерн не может быть пустым", "Prop_Query");

            // Транслируем каждый символ wildcard в эквивалент regex:
            //   * → .*   (любое кол-во любых символов)
            //   ? → .    (ровно один любой символ)
            //   всё остальное → Regex.Escape (спецсимволы экранируются)
            string regexPattern = "^"
                + string.Concat(
                    query.Select(c =>
                        c == '*' ? ".*"
                      : c == '?' ? "."
                      : Regex.Escape(c.ToString())))
                + "$";

            var options = RegexOptions.Compiled;
            if (!this.Prop_CaseSensitive)
                options |= RegexOptions.IgnoreCase;

            var regex = new Regex(regexPattern, options);
            return s => s != null && regex.IsMatch(s);
        }

        // =========================================================================
        // ПРИМЕНЕНИЕ ЦЕЛИ ПОИСКА (Target)
        // =========================================================================

        /// <summary>
        /// Применяет предикат matchFunc к нужному полю пары согласно Prop_Target:
        ///   Keys         — проверяет только ключ
        ///   Values       — проверяет только значение
        ///   KeysAndValues — проверяет ключ ИЛИ значение (достаточно одного совпадения)
        /// </summary>
        private bool ApplyTarget(KeyValuePair<string, string> pair, Func<string, bool> matchFunc)
        {
            switch (this.Prop_Target)
            {
                case DictionaryFilterTarget.Keys:
                    return matchFunc(pair.Key);

                case DictionaryFilterTarget.Values:
                    return matchFunc(pair.Value);

                case DictionaryFilterTarget.KeysAndValues:
                    return matchFunc(pair.Key) || matchFunc(pair.Value);

                default:
                    throw new InvalidOperationException($"Неизвестная цель поиска: {this.Prop_Target}");
            }
        }

        // =========================================================================
        // ВАЛИДАЦИЯ
        // =========================================================================

                public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            ret.ValidateRequired(this.Prop_Dictionary, ActivityStrings.Field_Dictionary, ActivityStrings.Error_DictionaryRequired);
            ret.ValidateRequired(this.Prop_Query, ActivityStrings.Field_Query, "Строка поиска не может быть пустой");

            if (this.Prop_Method == DictionaryFilterMethod.Regex
                && !string.IsNullOrWhiteSpace(this.Prop_Query))
            {
                ValidateRegexSyntax(ret, this.Prop_Query);
            }

            return ret;
        }

                private void ValidateRegexSyntax(ValidationResult result, string pattern)
        {
            try
            {
                _ = new Regex(pattern);
            }
            catch (ArgumentException ex)
            {
                result.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = ActivityStrings.Field_Query,
                    Error = $"Некорректный regex: {ex.Message}"
                });
            }
        }
    }
}