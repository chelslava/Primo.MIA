// =============================================================================
// TextParseBack.cs — активность «Текст: Разбор по шаблону».
//
// Извлекает именованные данные из строки по читаемой маске.
// Обратная операция к «Текст: Шаблонизатор».
//
// Маска компилируется в именованные группы захвата:
//   "Счёт №{Номер} от {Дата}" → "Счёт\ №(?<Номер>.+?)\ от\ (?<Дата>.+?)"
//
// Поддерживаемые синтаксисы (переиспользует TemplateSyntax из Enums.cs):
//   SingleBrace — {ключ}       (по умолчанию для разбора)
//   DoubleBrace — {{ключ}}
//   Percent     — %ключ%
//
// Опции:
//   GreedyMatch  — жадный захват (.+) вместо ленивого (.+?)
//   CaseSensitive — учитывать регистр литеральных частей
//   MultiLine    — маска разбирает многострочный текст (. совпадает с \n)
//   AllMatches   — найти все вхождения маски в тексте (список словарей)
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Текст: Разбор по шаблону».
    /// Извлекает именованные значения из строки по маске с плейсхолдерами.
    /// </summary>
    public class TextParseBack : PrimoComponentTO<TextParse>
    {
        // =====================================================================
        // Свойства
        // =====================================================================

        #region Prop_InputText
        private string _propInputText;
        /// <summary>Входная строка для разбора.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_InputString)]
        public string Prop_InputText
        {
            get => _propInputText;
            set { _propInputText = value; InvokePropertyChanged(this, nameof(Prop_InputText)); }
        }
        #endregion

        #region Prop_Mask
        private string _propMask;
        /// <summary>
        /// Маска с плейсхолдерами.
        /// Пример: "Счёт №{Номер} от {Дата} на сумму {Сумма} руб."
        /// Литеральные части автоматически экранируются как regex.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ParseMask)]
        public string Prop_Mask
        {
            get => _propMask;
            set { _propMask = value; InvokePropertyChanged(this, nameof(Prop_Mask)); }
        }
        #endregion

        #region Prop_MaskSyntax
        private TemplateSyntax _propMaskSyntax = TemplateSyntax.SingleBrace;
        /// <summary>
        /// Синтаксис плейсхолдеров в маске.
        /// По умолчанию SingleBrace ({ключ}) — наиболее читаемый для коротких масок.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_TemplateSyntax)]
        public TemplateSyntax Prop_MaskSyntax
        {
            get => _propMaskSyntax;
            set { _propMaskSyntax = value; InvokePropertyChanged(this, nameof(Prop_MaskSyntax)); }
        }
        #endregion

        #region Prop_CaseSensitive
        private bool _propCaseSensitive = false;
        /// <summary>
        /// Учитывать регистр литеральных частей маски при сопоставлении.
        /// По умолчанию false — регистронезависимое сопоставление.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CaseSensitive)]
        public bool Prop_CaseSensitive
        {
            get => _propCaseSensitive;
            set { _propCaseSensitive = value; InvokePropertyChanged(this, nameof(Prop_CaseSensitive)); }
        }
        #endregion

        #region Prop_GreedyMatch
        private bool _propGreedyMatch = false;
        /// <summary>
        /// Жадный захват: плейсхолдер захватывает максимально длинную подстроку (.+).
        /// По умолчанию false — ленивый захват (.+?) — плейсхолдер берёт минимум.
        ///
        /// Ленивый (рекомендуется): каждый плейсхолдер останавливается при первом
        /// совпадении с последующим литералом.
        /// Жадный: первый плейсхолдер тянет всё, последующие подстраиваются.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_GreedyMatch)]
        public bool Prop_GreedyMatch
        {
            get => _propGreedyMatch;
            set { _propGreedyMatch = value; InvokePropertyChanged(this, nameof(Prop_GreedyMatch)); }
        }
        #endregion

        #region Prop_MultiLine
        private bool _propMultiLine = false;
        /// <summary>
        /// Разбирать многострочный текст.
        /// При включении символ "." в паттерне совпадает в том числе с переносом строки (\n).
        /// Используется когда входная строка содержит несколько строк текста.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_MultiLine)]
        public bool Prop_MultiLine
        {
            get => _propMultiLine;
            set { _propMultiLine = value; InvokePropertyChanged(this, nameof(Prop_MultiLine)); }
        }
        #endregion

        #region Prop_AllMatches
        private bool _propAllMatches = false;
        /// <summary>
        /// Найти все вхождения маски в тексте, а не только первое.
        /// Результат записывается в Prop_AllResults как List&lt;Dictionary&lt;string,string&gt;&gt;.
        /// Prop_Result содержит первое совпадение (или пустой словарь).
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_AllMatches)]
        public bool Prop_AllMatches
        {
            get => _propAllMatches;
            set { _propAllMatches = value; InvokePropertyChanged(this, nameof(Prop_AllMatches)); }
        }
        #endregion

        #region Prop_Result (Выходной)
        private string _propResult;
        /// <summary>
        /// Имя переменной скрипта для записи Dictionary&lt;string,string&gt;
        /// с извлечёнными значениями первого совпадения.
        /// Пустой словарь если маска не совпала.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ParseResult)]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
        }
        #endregion

        #region Prop_IsMatched (Выходной)
        private string _propIsMatched;
        /// <summary>
        /// Имя переменной скрипта для записи признака успешного совпадения (bool).
        /// false если маска не совпала — не является ошибкой, IsSuccess = true.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_IsMatched)]
        public string Prop_IsMatched
        {
            get => _propIsMatched;
            set { _propIsMatched = value; InvokePropertyChanged(this, nameof(Prop_IsMatched)); }
        }
        #endregion

        #region Prop_MatchedCount (Выходной)
        private string _propMatchedCount;
        /// <summary>
        /// Имя переменной скрипта для записи количества извлечённых значений (int).
        /// При AllMatches=true — общее количество вхождений маски в тексте.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_MatchCount)]
        public string Prop_MatchCount
        {
            get => _propMatchedCount;
            set { _propMatchedCount = value; InvokePropertyChanged(this, nameof(Prop_MatchCount)); }
        }
        #endregion

        #region Prop_AllResults (Выходной)
        private string _propAllResults;
        /// <summary>
        /// Имя переменной скрипта для записи всех совпадений
        /// List&lt;Dictionary&lt;string,string&gt;&gt;.
        /// Заполняется только при AllMatches = true.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<Dictionary<string, string>>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_AllResults)]
        public string Prop_AllResults
        {
            get => _propAllResults;
            set { _propAllResults = value; InvokePropertyChanged(this, nameof(Prop_AllResults)); }
        }
        #endregion

        #region Prop_CompiledPattern (Выходной)
        private string _propCompiledPattern;
        /// <summary>
        /// Имя переменной скрипта для записи скомпилированного regex-паттерна (string).
        /// Диагностический параметр — помогает отладить маску.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CompiledPattern)]
        public string Prop_CompiledPattern
        {
            get => _propCompiledPattern;
            set { _propCompiledPattern = value; InvokePropertyChanged(this, nameof(Prop_CompiledPattern)); }
        }
        #endregion

        // =====================================================================
        // Служебные свойства
        // =====================================================================

        public override string GroupName
        {
            get => ActivityCategories.Utilities;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 30000;
            set { }
        }

        // =====================================================================
        // Конструктор
        // =====================================================================

        public TextParseBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_TextParse;
            sdkComponentHelp =
                "Извлекает именованные данные из строки по читаемой маске.\n" +
                "Обратная операция к «Текст: Шаблонизатор».\n" +
                "\n" +
                "── Принцип работы ─────────────────────────────\n" +
                "Маска: \"Счёт №{Номер} от {Дата} на сумму {Сумма} руб.\"\n" +
                "Строка: \"Счёт №ЗК-2026 от 15.03.2026 на сумму 14500 руб.\"\n" +
                "Результат: Номер=\"ЗК-2026\", Дата=\"15.03.2026\", Сумма=\"14500\"\n" +
                "\n" +
                "── Синтаксисы маски ────────────────────────────\n" +
                "SingleBrace — {ключ}   (по умолчанию)\n" +
                "DoubleBrace — {{ключ}}\n" +
                "Percent     — %ключ%\n" +
                "\n" +
                "── Опции ───────────────────────────────────────\n" +
                "Жадный захват  — плейсхолдер захватывает максимум (.+)\n" +
                "                 по умолчанию ленивый (.+?)\n" +
                "Многострочный  — '.' совпадает в том числе с переносом строки\n" +
                "Все вхождения  — найти все совпадения маски в тексте\n" +
                "\n" +
                "── Выходные данные ─────────────────────────────\n" +
                "Результат        — Dictionary<string,string> первого совпадения\n" +
                "Совпало          — false если маска не совпала (не ошибка)\n" +
                "Количество       — число извлечённых ключей (или вхождений)\n" +
                "Все результаты   — List<Dict> всех вхождений (AllMatches=true)\n" +
                "Скомпилированный паттерн — regex для отладки маски";

            sdkComponentIcon = ActivityIcons.TextTemplate;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Основные
                PropertyBuilder.Script<string>("Prop_InputText",      ActivityStrings.Field_InputString),
                PropertyBuilder.Script<string>("Prop_Mask",           ActivityStrings.Field_ParseMask),
                // Настройки
                PropertyBuilder.Enum<TemplateSyntax>("Prop_MaskSyntax", ActivityStrings.Field_TemplateSyntax),
                PropertyBuilder.BooleanObject("Prop_CaseSensitive",   ActivityStrings.Field_CaseSensitive),
                PropertyBuilder.BooleanObject("Prop_GreedyMatch",     ActivityStrings.Field_GreedyMatch),
                PropertyBuilder.BooleanObject("Prop_MultiLine",       ActivityStrings.Field_MultiLine),
                PropertyBuilder.BooleanObject("Prop_AllMatches",      ActivityStrings.Field_AllMatches),
                // Выходные
                PropertyBuilder.Variable<Dictionary<string,string>>("Prop_Result",  ActivityStrings.Field_ParseResult),
                PropertyBuilder.Variable<bool>("Prop_IsMatched",      ActivityStrings.Field_IsMatched),
                PropertyBuilder.Variable<int>("Prop_MatchedCount",    ActivityStrings.Field_MatchCount),
                PropertyBuilder.Variable<List<Dictionary<string,string>>>("Prop_AllResults", ActivityStrings.Field_AllResults),
                PropertyBuilder.Variable<string>("Prop_CompiledPattern", ActivityStrings.Field_CompiledPattern)
            };

            InitClass(container);

            this.Prop_MaskSyntax   = TemplateSyntax.SingleBrace;
            this.Prop_CaseSensitive = false;
            this.Prop_GreedyMatch   = false;
            this.Prop_MultiLine     = false;
            this.Prop_AllMatches    = false;
        }

        // =====================================================================
        // Выполнение
        // =====================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Читаем входные параметры ───────────────────────────────

                string inputText = GetPropertyValue<string>(
                    this.Prop_InputText, nameof(Prop_InputText), sd);
                if (inputText == null)
                    return Fail(ActivityStrings.Error_InputStringRequired);

                string mask = GetPropertyValue<string>(
                    this.Prop_Mask, nameof(Prop_Mask), sd);
                if (string.IsNullOrEmpty(mask))
                    return Fail(ActivityStrings.Error_ParseMaskRequired);

                var parseResult = TextParseLogic.Parse(
                    inputText,
                    mask,
                    this.Prop_MaskSyntax,
                    this.Prop_CaseSensitive,
                    this.Prop_GreedyMatch,
                    this.Prop_MultiLine,
                    this.Prop_AllMatches);

                if (!parseResult.IsSuccess)
                    return Fail(NormalizeError(parseResult.ErrorMessage));

                SetVariableValue(this.Prop_CompiledPattern, parseResult.CompiledPattern, sd);

                if (this.Prop_AllMatches)
                    return ApplyAllMatchesResult(sd, parseResult);

                return ApplyFirstMatchResult(sd, parseResult);
            }
            catch (ArgumentException ex)
            {
                return Fail($"Неверный аргумент: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return Fail($"Недопустимая операция: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка разбора по шаблону: {ex.Message}");
            }
        }

        // =====================================================================
        // Режим: первое совпадение
        // =====================================================================

        /// <summary>
        /// Ищет первое совпадение маски в тексте.
        /// IsMatched = false — штатный результат (не ошибка).
        /// </summary>
        private ExecutionResult ApplyFirstMatchResult(ScriptingData sd, TextParseResult parseResult)
        {
            if (!parseResult.IsMatched)
            {
                // Маска не совпала — штатный результат, не ошибка
                SetVariableValue(this.Prop_IsMatched, false, sd);
                SetVariableValue(this.Prop_MatchCount, 0, sd);
                SetVariableValue(this.Prop_Result, new Dictionary<string, string>(), sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = "Маска не совпала со строкой"
                };
            }

            Dictionary<string, string> result = parseResult.Result;

            SetVariableValue(this.Prop_Result, result, sd);
            SetVariableValue(this.Prop_IsMatched, true, sd);
            SetVariableValue(this.Prop_MatchCount, result.Count, sd);

            // Краткое сообщение с первыми двумя извлечёнными значениями
            string preview = string.Join(", ", result.Take(2)
                .Select(kv => $"{kv.Key}=\"{kv.Value}\""));
            if (result.Count > 2)
                preview += $" (+{result.Count - 2})";

            return new ExecutionResult
            {
                IsSuccess = true,
                SuccessMessage = $"Извлечено {result.Count} значений: {preview}"
            };
        }

        // =====================================================================
        // Режим: все совпадения
        // =====================================================================

        /// <summary>
        /// Ищет все вхождения маски в тексте.
        /// Prop_Result содержит первое совпадение.
        /// Prop_AllResults содержит список всех совпадений.
        /// Prop_MatchedCount — количество вхождений (не ключей).
        /// </summary>
        private ExecutionResult ApplyAllMatchesResult(ScriptingData sd, TextParseResult parseResult)
        {
            SetVariableValue(this.Prop_AllResults, parseResult.AllResults, sd);
            SetVariableValue(this.Prop_Result, parseResult.Result, sd);
            SetVariableValue(this.Prop_IsMatched, parseResult.IsMatched, sd);
            SetVariableValue(this.Prop_MatchCount, parseResult.Count, sd);

            return new ExecutionResult
            {
                IsSuccess = true,
                SuccessMessage = parseResult.Count > 0
                    ? $"Найдено {parseResult.Count} вхождений маски"
                    : "Вхождений маски не найдено"
            };
        }

        private static ExecutionResult Fail(string msg) =>
            new ExecutionResult { IsSuccess = false, ErrorMessage = msg };

        private static string NormalizeError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return ActivityStrings.Error_ParseMaskInvalid;
            if (errorMessage.StartsWith("Ошибка компиляции маски: ", StringComparison.Ordinal) ||
                errorMessage.StartsWith("Ошибка создания regex: ", StringComparison.Ordinal))
            {
                return $"{ActivityStrings.Error_ParseMaskInvalid}: {errorMessage.Substring(errorMessage.IndexOf(':') + 2)}";
            }

            return errorMessage;
        }

        // =====================================================================
        // Валидация
        // =====================================================================

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_InputText))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_InputText),
                    Error        = ActivityStrings.Error_InputStringRequired
                });

            if (string.IsNullOrWhiteSpace(this.Prop_Mask))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_Mask),
                    Error        = ActivityStrings.Error_ParseMaskRequired
                });

            return ret;
        }
    }
}
