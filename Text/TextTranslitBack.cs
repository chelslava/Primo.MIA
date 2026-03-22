// =============================================================================
// TextTranslitBack.cs — активность «Текст: Транслитерация».
//
// Преобразует текст между кириллицей и латиницей по выбранной схеме.
//
// Поддерживаемые схемы (TranslitScheme):
//   GOST7792000  — ГОСТ 7.79-2000 сист. Б (официальный стандарт РФ)
//   Passport2013 — Загранпаспорта РФ с 2013 года (Приказ МВД №827)
//   ICAOPassport — ИКАО Doc 9303 (авиабилеты, машиносчитываемые документы)
//   ISO9         — ISO 9:1995 (однозначная обратимая транслитерация)
//   BGN_PCGN     — BGN/PCGN 1947 (английские географические названия)
//   Simplified   — Упрощённая ASCII (логины, имена файлов, URL)
//
// Направления (TranslitDirection):
//   CyrillicToLatin — Кириллица → Латиница
//   LatinToCyrillic — Латиница → Кириллица
//   AutoDetect      — Определить по большинству символов
//
// Постобработка:
//   PreserveCase      — сохранить регистр оригинала
//   PreserveNonAlpha  — оставить нетранслитерируемые символы
//   ToUpperCase       — привести к верхнему регистру
//   ToLowerCase       — привести к нижнему регистру
//   ReplaceSpaces     — заменить пробелы указанным символом
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Текст: Транслитерация».
    /// Преобразует текст между кириллицей и латиницей по выбранной схеме.
    /// </summary>
    public class TextTranslitBack : PrimoComponentTO<TextTranslit>
    {
        // =====================================================================
        // Свойства
        // =====================================================================

        #region Prop_InputText
        private string _propInputText;
        /// <summary>Входной текст для транслитерации.</summary>
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

        #region Prop_Direction
        private TranslitDirection _propDirection = TranslitDirection.CyrillicToLatin;
        /// <summary>Направление: CyrillicToLatin / LatinToCyrillic / AutoDetect.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_TranslitDirection)]
        public TranslitDirection Prop_Direction
        {
            get => _propDirection;
            set { _propDirection = value; InvokePropertyChanged(this, nameof(Prop_Direction)); }
        }
        #endregion

        #region Prop_Scheme
        private TranslitScheme _propScheme = TranslitScheme.Simplified;
        /// <summary>Схема транслитерации.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_TranslitScheme)]
        public TranslitScheme Prop_Scheme
        {
            get => _propScheme;
            set { _propScheme = value; InvokePropertyChanged(this, nameof(Prop_Scheme)); }
        }
        #endregion

        #region Prop_PreserveCase
        private bool _propPreserveCase = true;
        /// <summary>
        /// Сохранять регистр оригинального символа в результате.
        /// При включении: "Иванов" → "Ivanov" (не "IVANOV" и не "ivanov").
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_PreserveCase)]
        public bool Prop_PreserveCase
        {
            get => _propPreserveCase;
            set { _propPreserveCase = value; InvokePropertyChanged(this, nameof(Prop_PreserveCase)); }
        }
        #endregion

        #region Prop_PreserveNonAlpha
        private bool _propPreserveNonAlpha = true;
        /// <summary>
        /// Оставлять нетранслитерируемые символы без изменений.
        /// При включении: цифры, пунктуация, пробелы — остаются.
        /// При выключении — удаляются.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_PreserveNonAlpha)]
        public bool Prop_PreserveNonAlpha
        {
            get => _propPreserveNonAlpha;
            set { _propPreserveNonAlpha = value; InvokePropertyChanged(this, nameof(Prop_PreserveNonAlpha)); }
        }
        #endregion

        #region Prop_ToUpperCase
        private bool _propToUpperCase = false;
        /// <summary>Привести весь результат к верхнему регистру после транслитерации.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ToUpperCase)]
        public bool Prop_ToUpperCase
        {
            get => _propToUpperCase;
            set { _propToUpperCase = value; InvokePropertyChanged(this, nameof(Prop_ToUpperCase)); }
        }
        #endregion

        #region Prop_ToLowerCase
        private bool _propToLowerCase = false;
        /// <summary>Привести весь результат к нижнему регистру после транслитерации.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ToLowerCase)]
        public bool Prop_ToLowerCase
        {
            get => _propToLowerCase;
            set { _propToLowerCase = value; InvokePropertyChanged(this, nameof(Prop_ToLowerCase)); }
        }
        #endregion

        #region Prop_ReplaceSpaces
        private string _propReplaceSpaces = string.Empty;
        /// <summary>
        /// Заменить пробелы в результате на указанную строку.
        /// Пусто — оставить пробелы без изменений.
        /// Примеры: "_" для имён файлов, "-" для slug, "." для логинов.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ReplaceSpaces)]
        public string Prop_ReplaceSpaces
        {
            get => _propReplaceSpaces;
            set { _propReplaceSpaces = value; InvokePropertyChanged(this, nameof(Prop_ReplaceSpaces)); }
        }
        #endregion

        #region Prop_Result (Выходной)
        private string _propResult;
        /// <summary>Имя переменной скрипта для записи транслитерированного текста.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_OutputVariable)]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
        }
        #endregion

        #region Prop_DetectedDirection (Выходной)
        private string _propDetectedDirection;
        /// <summary>
        /// Имя переменной скрипта для записи определённого направления (string).
        /// Заполняется при Direction = AutoDetect.
        /// Значение: "CyrillicToLatin" или "LatinToCyrillic".
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_DetectedDirection)]
        public string Prop_DetectedDirection
        {
            get => _propDetectedDirection;
            set { _propDetectedDirection = value; InvokePropertyChanged(this, nameof(Prop_DetectedDirection)); }
        }
        #endregion

        #region Prop_ChangedChars (Выходной)
        private string _propChangedChars;
        /// <summary>
        /// Имя переменной скрипта для записи количества изменённых символов (int).
        /// Полезно для контроля — если 0, возможно не та схема или направление.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ChangedChars)]
        public string Prop_ChangedChars
        {
            get => _propChangedChars;
            set { _propChangedChars = value; InvokePropertyChanged(this, nameof(Prop_ChangedChars)); }
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

        public TextTranslitBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_TextTranslit;
            sdkComponentHelp =
                "Преобразует текст между кириллицей и латиницей.\n" +
                "\n" +
                "── Схемы транслитерации ────────────────────────\n" +
                "Simplified   — ASCII, без диакритики (логины, файлы, URL)\n" +
                "Passport2013 — Загранпаспорта РФ с 2013 года\n" +
                "ICAOPassport — Авиабилеты и машиносчитываемые документы\n" +
                "GOST7792000  — ГОСТ 7.79-2000 система Б\n" +
                "ISO9         — ISO 9:1995, обратимая транслитерация\n" +
                "BGN_PCGN     — Английские географические названия\n" +
                "\n" +
                "── Направления ─────────────────────────────────\n" +
                "CyrillicToLatin — Кириллица → Латиница\n" +
                "LatinToCyrillic — Латиница → Кириллица (только ISO9 без потерь)\n" +
                "AutoDetect      — Определить по большинству символов\n" +
                "\n" +
                "── Постобработка ───────────────────────────────\n" +
                "Сохранять регистр — \"Иванов\" → \"Ivanov\" (не \"IVANOV\")\n" +
                "Замена пробелов   — \"_\" для файлов, \".\" для логинов\n" +
                "К верхнему / нижнему регистру\n" +
                "\n" +
                "── Примеры ─────────────────────────────────────\n" +
                "\"Иванов Иван\" + Simplified + ToLower + ReplaceSpaces=\".\" → \"ivanov.ivan\"\n" +
                "\"ЩЕРБАКОВ\" + Passport2013 → \"SHCHERBAKOV\"\n" +
                "\"г. Москва\" + BGN_PCGN → \"g. Moskva\"";

            sdkComponentIcon = ActivityIcons.TextTranslit;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Основные
                PropertyBuilder.Script<string>("Prop_InputText",      ActivityStrings.Field_InputString),
                PropertyBuilder.Enum<TranslitDirection>("Prop_Direction", ActivityStrings.Field_TranslitDirection),
                PropertyBuilder.Enum<TranslitScheme>("Prop_Scheme",   ActivityStrings.Field_TranslitScheme),
                // Постобработка
                PropertyBuilder.BooleanObject("Prop_PreserveCase",    ActivityStrings.Field_PreserveCase),
                PropertyBuilder.BooleanObject("Prop_PreserveNonAlpha",ActivityStrings.Field_PreserveNonAlpha),
                PropertyBuilder.BooleanObject("Prop_ToUpperCase",     ActivityStrings.Field_ToUpperCase),
                PropertyBuilder.BooleanObject("Prop_ToLowerCase",     ActivityStrings.Field_ToLowerCase),
                PropertyBuilder.Script<string>("Prop_ReplaceSpaces",  ActivityStrings.Field_ReplaceSpaces),
                // Выходные
                PropertyBuilder.Variable<string>("Prop_Result",       ActivityStrings.Field_OutputVariable),
                PropertyBuilder.Variable<string>("Prop_DetectedDirection", ActivityStrings.Field_DetectedDirection),
                PropertyBuilder.Variable<int>("Prop_ChangedChars",    ActivityStrings.Field_ChangedChars)
            };

            InitClass(container);

            this.Prop_Direction        = TranslitDirection.CyrillicToLatin;
            this.Prop_Scheme           = TranslitScheme.Simplified;
            this.Prop_PreserveCase     = true;
            this.Prop_PreserveNonAlpha = true;
            this.Prop_ToUpperCase      = false;
            this.Prop_ToLowerCase      = false;
            this.Prop_ReplaceSpaces    = string.Empty;
        }

        // =====================================================================
        // Выполнение
        // =====================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var logic = new TextTranslitLogic();
                string inputText = GetPropertyValue<string>(
                    this.Prop_InputText, nameof(Prop_InputText), sd);

                if (inputText == null)
                    return Fail(ActivityStrings.Error_InputStringRequired);

                var translitResult = logic.Transliterate(
                    inputText,
                    this.Prop_Scheme,
                    this.Prop_Direction,
                    this.Prop_PreserveCase,
                    this.Prop_PreserveNonAlpha);

                TranslitDirection direction = translitResult.DetectedDirection;
                SetVariableValue(this.Prop_DetectedDirection, direction.ToString(), sd);

                // ── Постобработка ──────────────────────────────────────────

                // Замена пробелов (до смены регистра — важен порядок)
                string replaceSpaces = GetPropertyValue<string>(
                    this.Prop_ReplaceSpaces, nameof(Prop_ReplaceSpaces), sd);
                string result = translitResult.Text;
                if (!string.IsNullOrEmpty(replaceSpaces))
                    result = logic.ReplaceSpaces(result, replaceSpaces);

                // Приведение регистра (взаимоисключающие флаги — ToUpper имеет приоритет)
                if (this.Prop_ToUpperCase)
                    result = logic.ToUpperCase(result);
                else if (this.Prop_ToLowerCase)
                    result = logic.ToLowerCase(result);

                // ── Записываем результаты ──────────────────────────────────

                SetVariableValue(this.Prop_Result,       result,       sd);
                SetVariableValue(this.Prop_ChangedChars, translitResult.ChangedChars, sd);

                string directionMsg = direction == TranslitDirection.CyrillicToLatin
                    ? "Кир → Лат"
                    : "Лат → Кир";

                return new ExecutionResult
                {
                    IsSuccess      = true,
                    SuccessMessage = $"Транслитерировано ({this.Prop_Scheme}, {directionMsg}): " +
                                     $"изменено {translitResult.ChangedChars} символов"
                };
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка транслитерации: {ex.Message}");
            }
        }

        private static ExecutionResult Fail(string msg) =>
            new ExecutionResult { IsSuccess = false, ErrorMessage = msg };

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

            // ToUpperCase и ToLowerCase взаимоисключающи
            if (this.Prop_ToUpperCase && this.Prop_ToLowerCase)
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_ToUpperCase),
                    Error        = "Нельзя одновременно включить «К верхнему регистру» и «К нижнему регистру»"
                });

            return ret;
        }
    }
}
