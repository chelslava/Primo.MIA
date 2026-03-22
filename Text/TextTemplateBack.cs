// =============================================================================
// TextTemplateBack.cs — активность «Текст: Шаблонизатор».
//
// Подставляет значения из Dictionary<string,string> в текстовый шаблон
// с плейсхолдерами. Поддерживает три синтаксиса плейсхолдеров, три режима
// обработки пропущенных ключей, чтение шаблона из файла и форматирование
// числовых и датовых значений через суффикс формата ({{Сумма:N2}}).
//
// Синтаксисы:
//   DoubleBrace — {{ключ}}     (по умолчанию, не конфликтует с JSON)
//   SingleBrace — {ключ}
//   Percent     — %ключ%       (стиль Windows-переменных окружения)
//
// Форматирование значений (суффикс через двоеточие):
//   {{Сумма:N2}}          → "14 500,00"
//   {{Дата:dd.MM.yyyy}}   → "15.03.2026"
//   {{Процент:P1}}        → "12,5 %"
//   Работает для любого значения, которое можно распарсить как число или дату.
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Текст: Шаблонизатор».
    /// Подставляет значения словаря в шаблон с плейсхолдерами.
    /// </summary>
    public class TextTemplateBack : PrimoComponentTO<TextTemplate>
    {
        // =====================================================================
        // Свойства
        // =====================================================================

        #region Prop_Template
        private string _propTemplate;
        /// <summary>
        /// Шаблон с плейсхолдерами.
        /// Если указан Prop_TemplateFile — это свойство игнорируется.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_Template)]
        public string Prop_Template
        {
            get => _propTemplate;
            set { _propTemplate = value; InvokePropertyChanged(this, nameof(Prop_Template)); }
        }
        #endregion

        #region Prop_Variables
        private string _propVariables;
        /// <summary>Словарь переменных для подстановки Dictionary&lt;string,string&gt;.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_Variables)]
        public string Prop_Variables
        {
            get => _propVariables;
            set { _propVariables = value; InvokePropertyChanged(this, nameof(Prop_Variables)); }
        }
        #endregion

        #region Prop_Syntax
        private TemplateSyntax _propSyntax = TemplateSyntax.DoubleBrace;
        /// <summary>Синтаксис плейсхолдеров: DoubleBrace / SingleBrace / Percent.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_TemplateSyntax)]
        public TemplateSyntax Prop_Syntax
        {
            get => _propSyntax;
            set { _propSyntax = value; InvokePropertyChanged(this, nameof(Prop_Syntax)); }
        }
        #endregion

        #region Prop_MissingKeyBehavior
        private MissingKeyBehavior _propMissingKeyBehavior = MissingKeyBehavior.LeaveAsIs;
        /// <summary>
        /// Поведение при отсутствии ключа в словаре:
        /// LeaveAsIs / ReplaceWithEmpty / ThrowError.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_MissingKeyBehavior)]
        public MissingKeyBehavior Prop_MissingKeyBehavior
        {
            get => _propMissingKeyBehavior;
            set { _propMissingKeyBehavior = value; InvokePropertyChanged(this, nameof(Prop_MissingKeyBehavior)); }
        }
        #endregion

        #region Prop_CaseSensitive
        private bool _propCaseSensitive = false;
        /// <summary>
        /// Учитывать регистр ключей при поиске в словаре.
        /// По умолчанию false — поиск регистронезависимый.
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

        #region Prop_TemplateFile
        private string _propTemplateFile;
        /// <summary>
        /// Путь к файлу шаблона. Если указан — имеет приоритет над Prop_Template.
        /// Удобно для больших шаблонов (письма, SQL-запросы, HTML-страницы).
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_FilePath)]
        public string Prop_TemplateFile
        {
            get => _propTemplateFile;
            set { _propTemplateFile = value; InvokePropertyChanged(this, nameof(Prop_TemplateFile)); }
        }
        #endregion

        #region Prop_Encoding
        private string _propEncoding = "UTF-8";
        /// <summary>
        /// Кодировка файла шаблона (по умолчанию UTF-8).
        /// Используется только если указан Prop_TemplateFile.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_Encoding)]
        public string Prop_Encoding
        {
            get => _propEncoding;
            set { _propEncoding = value; InvokePropertyChanged(this, nameof(Prop_Encoding)); }
        }
        #endregion

        #region Prop_Result (Выходной)
        private string _propResult;
        /// <summary>Имя переменной скрипта для записи результирующей строки.</summary>
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

        #region Prop_ReplacedCount (Выходной)
        private string _propReplacedCount;
        /// <summary>Имя переменной скрипта для записи количества выполненных замен (int).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_TemplateReplacedCount)]
        public string Prop_ReplacedCount
        {
            get => _propReplacedCount;
            set { _propReplacedCount = value; InvokePropertyChanged(this, nameof(Prop_ReplacedCount)); }
        }
        #endregion

        #region Prop_MissingKeys (Выходной)
        private string _propMissingKeys;
        /// <summary>
        /// Имя переменной скрипта для записи списка незаполненных ключей List&lt;string&gt;.
        /// Пустой список если все плейсхолдеры заполнены.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_TemplateMissingKeys)]
        public string Prop_MissingKeys
        {
            get => _propMissingKeys;
            set { _propMissingKeys = value; InvokePropertyChanged(this, nameof(Prop_MissingKeys)); }
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

        public TextTemplateBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_TextTemplate;
            sdkComponentHelp =
                "Подставляет значения словаря в текстовый шаблон с плейсхолдерами.\n" +
                "\n" +
                "── Синтаксисы плейсхолдеров ───────────────────\n" +
                "DoubleBrace — {{ключ}}  (по умолчанию, не конфликтует с JSON)\n" +
                "SingleBrace — {ключ}\n" +
                "Percent     — %ключ%   (стиль Windows-переменных окружения)\n" +
                "\n" +
                "── Форматирование значений ─────────────────────\n" +
                "Суффикс через двоеточие применяет .NET-формат к значению:\n" +
                "  {{Сумма:N2}}         → \"14 500,00\"\n" +
                "  {{Дата:dd.MM.yyyy}}  → \"15.03.2026\"\n" +
                "  {{Процент:P1}}       → \"12,5 %\"\n" +
                "Работает если значение распознаётся как число или DateTime.\n" +
                "\n" +
                "── При отсутствии ключа ────────────────────────\n" +
                "LeaveAsIs      — оставить плейсхолдер (удобно для отладки)\n" +
                "ReplaceWithEmpty — заменить пустой строкой\n" +
                "ThrowError     — ошибка со списком незаполненных ключей\n" +
                "\n" +
                "── Шаблон из файла ─────────────────────────────\n" +
                "Если указан «Файл шаблона» — он имеет приоритет над полем «Шаблон».\n" +
                "Удобно для больших шаблонов: письма, SQL-запросы, HTML-страницы.";

            sdkComponentIcon = ActivityIcons.TextTemplate;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Основные
                PropertyBuilder.Script<string>("Prop_Template",       ActivityStrings.Field_Template),
                PropertyBuilder.Script<Dictionary<string,string>>("Prop_Variables", ActivityStrings.Field_Variables),
                // Настройки
                PropertyBuilder.Enum<TemplateSyntax>("Prop_Syntax",   ActivityStrings.Field_TemplateSyntax),
                PropertyBuilder.Enum<MissingKeyBehavior>("Prop_MissingKeyBehavior", ActivityStrings.Field_MissingKeyBehavior),
                PropertyBuilder.BooleanObject("Prop_CaseSensitive",   ActivityStrings.Field_CaseSensitive),
                PropertyBuilder.Script<string>("Prop_TemplateFile",   ActivityStrings.Field_FilePath),
                PropertyBuilder.Script<string>("Prop_Encoding",       ActivityStrings.Field_Encoding),
                // Выходные
                PropertyBuilder.Variable<string>("Prop_Result",       ActivityStrings.Field_OutputVariable),
                PropertyBuilder.Variable<int>("Prop_ReplacedCount",   ActivityStrings.Field_TemplateReplacedCount),
                PropertyBuilder.Variable<List<string>>("Prop_MissingKeys", ActivityStrings.Field_TemplateMissingKeys)
            };

            InitClass(container);

            this.Prop_Syntax              = TemplateSyntax.DoubleBrace;
            this.Prop_MissingKeyBehavior  = MissingKeyBehavior.LeaveAsIs;
            this.Prop_CaseSensitive       = false;
            this.Prop_Encoding            = "UTF-8";
        }

        // =====================================================================
        // Выполнение
        // =====================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Читаем словарь переменных ──────────────────────────────

                var variables = GetPropertyValue<Dictionary<string, string>>(
                    this.Prop_Variables, nameof(Prop_Variables), sd);

                if (variables == null)
                    return Fail(ActivityStrings.Error_TemplateVariablesRequired);

                // ── Читаем шаблон (файл имеет приоритет над полем) ─────────

                string template;
                string templateFile = GetPropertyValue<string>(
                    this.Prop_TemplateFile, nameof(Prop_TemplateFile), sd);

                if (!string.IsNullOrWhiteSpace(templateFile))
                {
                    // Читаем шаблон из файла
                    if (!File.Exists(templateFile))
                        return Fail($"{ActivityStrings.Error_TemplateFileNotFound}: {templateFile}");

                    string encodingName = GetPropertyValue<string>(
                        this.Prop_Encoding, nameof(Prop_Encoding), sd);
                    Encoding encoding = TextTemplateLogic.ParseEncoding(encodingName);
                    template = File.ReadAllText(templateFile, encoding);
                }
                else
                {
                    // Читаем шаблон из свойства
                    template = GetPropertyValue<string>(
                        this.Prop_Template, nameof(Prop_Template), sd);

                    if (template == null)
                        return Fail(ActivityStrings.Error_TemplateRequired);
                }

                // ── Выполняем подстановку ──────────────────────────────────

                var renderResult = TextTemplateLogic.Render(
                    template,
                    variables,
                    this.Prop_Syntax,
                    this.Prop_MissingKeyBehavior,
                    this.Prop_CaseSensitive);

                if (!renderResult.IsSuccess)
                    return Fail(renderResult.ErrorMessage);

                // ── Записываем выходные параметры ──────────────────────────

                SetVariableValue(this.Prop_Result,         renderResult.Result,        sd);
                SetVariableValue(this.Prop_ReplacedCount,  renderResult.ReplacedCount, sd);
                SetVariableValue(this.Prop_MissingKeys,    renderResult.MissingKeys,   sd);

                // ── Формируем сообщение ────────────────────────────────────

                string successMsg = renderResult.MissingKeys.Count == 0
                    ? $"Выполнено {renderResult.ReplacedCount} замен"
                    : $"Выполнено {renderResult.ReplacedCount} замен, незаполненных ключей: {renderResult.MissingKeys.Count}";

                return new ExecutionResult
                {
                    IsSuccess      = true,
                    SuccessMessage = successMsg
                };
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка шаблонизатора: {ex.Message}");
            }
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        private static ExecutionResult Fail(string msg) =>
            new ExecutionResult { IsSuccess = false, ErrorMessage = msg };

        // =====================================================================
        // Валидация
        // =====================================================================

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            // Шаблон нужен если не указан файл
            bool hasFile     = !string.IsNullOrWhiteSpace(this.Prop_TemplateFile);
            bool hasTemplate = !string.IsNullOrWhiteSpace(this.Prop_Template);

            if (!hasFile && !hasTemplate)
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_Template),
                    Error        = ActivityStrings.Error_TemplateRequired
                });

            // Словарь обязателен всегда
            if (string.IsNullOrWhiteSpace(this.Prop_Variables))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_Variables),
                    Error        = ActivityStrings.Error_TemplateVariablesRequired
                });

            return ret;
        }
    }
}
