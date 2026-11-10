// =============================================================================
// TextRegexBack.cs — активность «Текст: Regex» (issue #30).
//
// Match (совпадения + группы → DataTable) / Replace (подстановки $1, ${имя}).
// Match timeout обязателен — ReDoS-защита пользовательских паттернов.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System.Data;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность поиска/замены по регулярному выражению.</summary>
    public class TextRegexBack : PrimoComponentTO<TextRegex>
    {
        public override string GroupName { get => ActivityCategories.Text; protected set { } }

        protected override int sdkTimeOut { get => 30000; set { } }

        // ── Входные параметры ──────────────────────────────────────────

        private TextRegexMode _propMode = TextRegexMode.Match;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_RegexMode)]
        /// <summary>Режим: найти совпадения или заменить.</summary>
        public TextRegexMode Prop_Mode
        {
            get => _propMode;
            set { _propMode = value; InvokePropertyChanged(this, nameof(Prop_Mode)); }
        }

        private string _propInputText;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_RegexInput)]
        /// <summary>Входной текст.</summary>
        public string Prop_InputText
        {
            get => _propInputText;
            set { _propInputText = value; InvokePropertyChanged(this, nameof(Prop_InputText)); }
        }

        private string _propPattern;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_RegexPattern)]
        /// <summary>Регулярное выражение.</summary>
        public string Prop_Pattern
        {
            get => _propPattern;
            set { _propPattern = value; InvokePropertyChanged(this, nameof(Prop_Pattern)); }
        }

        private string _propReplacement;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RegexReplacement)]
        /// <summary>Замена (для режима Replace): $1, ${имя}.</summary>
        public string Prop_Replacement
        {
            get => _propReplacement;
            set { _propReplacement = value; InvokePropertyChanged(this, nameof(Prop_Replacement)); }
        }

        private bool _propIgnoreCase;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RegexIgnoreCase)]
        /// <summary>Игнорировать регистр (IgnoreCase).</summary>
        public bool Prop_IgnoreCase
        {
            get => _propIgnoreCase;
            set { _propIgnoreCase = value; InvokePropertyChanged(this, nameof(Prop_IgnoreCase)); }
        }

        private bool _propMultiline;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RegexMultiline)]
        /// <summary>Multiline: ^ и $ на границах строк.</summary>
        public bool Prop_Multiline
        {
            get => _propMultiline;
            set { _propMultiline = value; InvokePropertyChanged(this, nameof(Prop_Multiline)); }
        }

        private bool _propSingleline;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RegexSingleline)]
        /// <summary>Singleline: точка матчит и перевод строки.</summary>
        public bool Prop_Singleline
        {
            get => _propSingleline;
            set { _propSingleline = value; InvokePropertyChanged(this, nameof(Prop_Singleline)); }
        }

        private int _propTimeoutMs;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_RegexTimeout)]
        /// <summary>Таймаут совпадения в мс (0 = 1000; максимум 60000) — ReDoS-защита.</summary>
        public int Prop_TimeoutMs
        {
            get => _propTimeoutMs;
            set { _propTimeoutMs = value; InvokePropertyChanged(this, nameof(Prop_TimeoutMs)); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propMatchesTable;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RegexMatchesTable)]
        /// <summary>Таблица совпадений (режим Match; имя выходной переменной).</summary>
        public string Prop_MatchesTable
        {
            get => _propMatchesTable;
            set { _propMatchesTable = value; InvokePropertyChanged(this, nameof(Prop_MatchesTable)); }
        }

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RegexResult)]
        /// <summary>Результат замены (режим Replace).</summary>
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
        }

        private string _propMatchCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_MatchCount)]
        /// <summary>Число совпадений (имя выходной переменной).</summary>
        public string Prop_MatchCount
        {
            get => _propMatchCount;
            set { _propMatchCount = value; InvokePropertyChanged(this, nameof(Prop_MatchCount)); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public TextRegexBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_TextRegex;
            sdkComponentHelp = @"Компонент ""Текст: Regex""
Поиск и замена по регулярному выражению .NET (с обязательным таймаутом).

Основные:
Режим: [Enum] Найти совпадения (DataTable) / Заменить (строка).
Текст (входной)*: [String] Обрабатываемый текст.
Выражение*: [String] Регулярное выражение .NET. Именованные группы: (?<name>...).

Настройки:
Замена: [String] Только для «Заменить»: подстановки $1, ${name}.
Игнорировать регистр / Multiline / Singleline: [Bool]
Таймаут (мс): [Int] 0 = 1000, максимум 60000. Защита от «висящих» паттернов (ReDoS).

Выходные данные:
Совпадения: [DataTable] Match, Index, группы 1..N и именованные.
Результат: [String] Текст после замены.
Найдено: [Int] Число совпадений.";
            sdkComponentIcon = ActivityIcons.Regex;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<TextRegexMode>("Prop_Mode", "Режим: найти или заменить"),
                PropertyBuilder.String("Prop_InputText", "Входной текст"),
                PropertyBuilder.String("Prop_Pattern", "Регулярное выражение"),
                PropertyBuilder.String("Prop_Replacement", "Замена ($1, ${name})"),
                PropertyBuilder.BooleanObject("Prop_IgnoreCase", "Игнорировать регистр"),
                PropertyBuilder.BooleanObject("Prop_Multiline", "Multiline (^/$ по строкам)"),
                PropertyBuilder.BooleanObject("Prop_Singleline", "Singleline (. захватывает переносы)"),
                PropertyBuilder.Variable<int>("Prop_TimeoutMs", "Таймаут совпадения, мс (0 = 1000)"),
                PropertyBuilder.Variable<DataTable>("Prop_MatchesTable", "Таблица совпадений"),
                PropertyBuilder.Variable<string>("Prop_Result", "Результат замены"),
                PropertyBuilder.Variable<int>("Prop_MatchCount", "Число совпадений")
            };

            InitClass(container);

            this.Prop_InputText = "\"\"";
            this.Prop_Pattern = "\"\"";
            this.Prop_Replacement = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new TextRegexLogic();
            try
            {
                var input = GetPropertyValue<string>(this.Prop_InputText, nameof(Prop_InputText), sd);
                var pattern = GetPropertyValue<string>(this.Prop_Pattern, nameof(Prop_Pattern), sd);
                var replacement = GetPropertyValue<string>(this.Prop_Replacement, nameof(Prop_Replacement), sd);

                var options = logic.BuildOptions(this.Prop_IgnoreCase, this.Prop_Multiline, this.Prop_Singleline);
                var regex = logic.BuildRegex(pattern, options, this.Prop_TimeoutMs);
                var count = logic.CountMatches(input, regex);

                SetVariableValue(this.Prop_MatchCount, count, sd);

                if (this.Prop_Mode == TextRegexMode.Replace)
                {
                    var result = logic.ReplaceMatches(input, regex, replacement);
                    SetVariableValue(this.Prop_Result, result, sd);
                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = $"[Текст: Regex] заменено {count} совпадений"
                    };
                }

                var table = logic.FindMatches(input, regex);
                SetVariableValue(this.Prop_MatchesTable, table, sd);
                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Текст: Regex] найдено {count} совпадений"
                };
            }
            catch (InvalidOperationException ex)
            {
                // некорректный паттерн / таймаут совпадения
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка Regex: {ex.Message}"
                };
            }
            catch (RegexMatchTimeoutException)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Совпадение прервано по таймауту: паттерн слишком «тяжёлый» для этого текста. Упростите выражение или увеличьте таймаут."
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка Regex: {ex.Message}"
                };
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_InputText, ActivityStrings.Field_RegexInput, ActivityStrings.Error_TextRegexInputRequired);
            ret.ValidateRequired(this.Prop_Pattern, ActivityStrings.Field_RegexPattern, ActivityStrings.Error_TextRegexPatternRequired);
            return ret;
        }
    }
}
