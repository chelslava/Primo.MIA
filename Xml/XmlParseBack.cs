// =============================================================================
// XmlParseBack.cs — активность «XML: Парсинг».
//
// Парсит XML-строку в структуру данных (Dictionary/List).
// Поддерживает режимы: в структуру, валидация, форматирование.
//
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для парсинга XML.
    /// </summary>
    public class XmlParseBack : PrimoComponentTO<XmlParse>
    {
        // ── Свойства SDK ──────────────────────────────────────────────────

        /// <summary>
        /// Категория активности в дизайнере Primo.
        /// </summary>
        public override string GroupName
        {
            get => ActivityCategories.HttpWeb;
            protected set { }
        }

        /// <summary>
        /// Максимальное время выполнения активности (мс).
        /// </summary>
        protected override int sdkTimeOut
        {
            get => 30000;
            set { }
        }

        // ── Входные параметры ─────────────────────────────────────────────

        #region Prop_XmlString

        private string _propXmlString;

        /// <summary>
        /// XML-строка для парсинга.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("XML-строка")]
        public string Prop_XmlString
        {
            get => _propXmlString;
            set { _propXmlString = value; InvokePropertyChanged(this, nameof(Prop_XmlString)); }
        }

        #endregion

        #region Prop_ParseMode

        private XmlParseMode _propParseMode = XmlParseMode.ToStructure;

        /// <summary>
        /// Режим парсинга.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Режим")]
        public XmlParseMode Prop_ParseMode
        {
            get => _propParseMode;
            set { _propParseMode = value; InvokePropertyChanged(this, nameof(Prop_ParseMode)); }
        }

        #endregion

        #region Prop_IndentChars

        private string _propIndentChars = "  ";

        /// <summary>
        /// Символы отступа для форматирования.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Символы отступа")]
        public string Prop_IndentChars
        {
            get => _propIndentChars;
            set { _propIndentChars = value; InvokePropertyChanged(this, nameof(Prop_IndentChars)); }
        }

        #endregion

        // ── Выходные параметры ────────────────────────────────────────────

        #region Prop_Result

        private string _propResult;

        /// <summary>
        /// Результат парсинга (структура или отформатированный XML).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Результат")]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
        }

        #endregion

        #region Prop_IsValid

        private string _propIsValid;

        /// <summary>
        /// Признак валидности XML (для режима валидации).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Валиден")]
        public string Prop_IsValid
        {
            get => _propIsValid;
            set { _propIsValid = value; InvokePropertyChanged(this, nameof(Prop_IsValid)); }
        }

        #endregion

        #region Prop_ParseError

        private string _propParseError;

        /// <summary>
        /// Сообщение об ошибке парсинга.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Ошибка парсинга")]
        public string Prop_ParseError
        {
            get => _propParseError;
            set { _propParseError = value; InvokePropertyChanged(this, nameof(Prop_ParseError)); }
        }

        #endregion

        // ── Конструктор ───────────────────────────────────────────────────

        /// <summary>
        /// Конструктор активности «XML: Парсинг».
        /// </summary>
        public XmlParseBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "XML: Парсинг";
            sdkComponentHelp =
                "Парсит XML-строку в структуру данных.\n" +
                "\n" +
                "── Режимы ──────────────────────────────────────\n" +
                "В структуру   — преобразует в Dictionary/List\n" +
                "Валидация     — проверяет корректность XML\n" +
                "Форматирование — форматирует с отступами\n" +
                "\n" +
                "── Параметры ───────────────────────────────────\n" +
                "XML-строка     — исходный XML для обработки\n" +
                "Символы отступа — отступ для форматирования";

            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<string>("Prop_XmlString", "XML-строка"),
                PropertyBuilder.Enum<XmlParseMode>("Prop_ParseMode", "Режим"),
                PropertyBuilder.Script<string>("Prop_IndentChars", "Символы отступа"),
                PropertyBuilder.Variable<object>("Prop_Result", "Результат"),
                PropertyBuilder.Variable<bool>("Prop_IsValid", "Валиден"),
                PropertyBuilder.Variable<string>("Prop_ParseError", "Ошибка парсинга")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_XmlString = "\"<root><item>value</item></root>\"";
            this.Prop_IndentChars = "\"  \"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────────

        /// <summary>
        /// Основной метод парсинга XML.
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Чтение входных параметров ──────────────────────────────
                string xmlString = GetPropertyValue<string>(this.Prop_XmlString, nameof(Prop_XmlString), sd);
                if (string.IsNullOrWhiteSpace(xmlString))
                    throw new ArgumentException("XML-строка не может быть пустой");

                string indentChars = GetPropertyValue<string>(this.Prop_IndentChars, nameof(Prop_IndentChars), sd) ?? "  ";

                // ── Выполнение в зависимости от режима ─────────────────────
                string resultMsg = ExecuteParseMode(sd, xmlString, indentChars);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = resultMsg
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [XML: Парсинг]: {ex.Message}"
                };
            }
        }

        // ── Приватные методы ──────────────────────────────────────────────

        /// <summary>
        /// Выполняет парсинг в зависимости от режима.
        /// </summary>
        private string ExecuteParseMode(ScriptingData sd, string xmlString, string indentChars)
        {
            switch (this.Prop_ParseMode)
            {
                case XmlParseMode.ToStructure:
                    return ParseToStructure(sd, xmlString);

                case XmlParseMode.Validate:
                    return ValidateXml(sd, xmlString);

                case XmlParseMode.Format:
                    return FormatXml(sd, xmlString, indentChars);

                default:
                    throw new ArgumentOutOfRangeException(nameof(Prop_ParseMode),
                        $"Неизвестный режим: {this.Prop_ParseMode}");
            }
        }

        /// <summary>
        /// Парсит XML в структуру Dictionary.
        /// </summary>
        private string ParseToStructure(ScriptingData sd, string xmlString)
        {
            var doc = XDocument.Parse(xmlString);
            var result = ElementToDictionary(doc.Root);

            if (!string.IsNullOrWhiteSpace(this.Prop_Result))
                SetVariableValue(this.Prop_Result, result, sd);

            if (!string.IsNullOrWhiteSpace(this.Prop_IsValid))
                SetVariableValue(this.Prop_IsValid, true, sd);

            if (!string.IsNullOrWhiteSpace(this.Prop_ParseError))
                SetVariableValue(this.Prop_ParseError, "", sd);

            return $"[В структуру] XML преобразован в структуру, корневой элемент: {doc.Root.Name.LocalName}";
        }

        /// <summary>
        /// Валидирует XML.
        /// </summary>
        private string ValidateXml(ScriptingData sd, string xmlString)
        {
            try
            {
                var doc = XDocument.Parse(xmlString);

                if (!string.IsNullOrWhiteSpace(this.Prop_IsValid))
                    SetVariableValue(this.Prop_IsValid, true, sd);

                if (!string.IsNullOrWhiteSpace(this.Prop_ParseError))
                    SetVariableValue(this.Prop_ParseError, "", sd);

                return $"[Валидация] XML валиден, корневой элемент: {doc.Root.Name.LocalName}";
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(this.Prop_IsValid))
                    SetVariableValue(this.Prop_IsValid, false, sd);

                if (!string.IsNullOrWhiteSpace(this.Prop_ParseError))
                    SetVariableValue(this.Prop_ParseError, ex.Message, sd);

                return $"[Валидация] XML невалиден: {ex.Message}";
            }
        }

        /// <summary>
        /// Форматирует XML с отступами.
        /// </summary>
        private string FormatXml(ScriptingData sd, string xmlString, string indentChars)
        {
            var doc = XDocument.Parse(xmlString);
            string formatted = doc.ToString(SaveOptions.None);

            if (!string.IsNullOrWhiteSpace(this.Prop_Result))
                SetVariableValue(this.Prop_Result, formatted, sd);

            return $"[Форматирование] XML отформатирован, длина: {formatted.Length}";
        }

        /// <summary>
        /// Преобразует XElement в Dictionary.
        /// </summary>
        private Dictionary<string, object> ElementToDictionary(XElement element)
        {
            var result = new Dictionary<string, object>();

            // Атрибуты
            foreach (var attr in element.Attributes())
            {
                result[$"@{attr.Name.LocalName}"] = attr.Value;
            }

            // Дочерние элементы
            var childGroups = new Dictionary<string, List<object>>();

            foreach (var child in element.Elements())
            {
                string name = child.Name.LocalName;

                if (!childGroups.ContainsKey(name))
                    childGroups[name] = new List<object>();

                // Если элемент имеет только текст — берём текст
                if (!child.HasElements && !child.HasAttributes)
                {
                    childGroups[name].Add(child.Value);
                }
                else
                {
                    childGroups[name].Add(ElementToDictionary(child));
                }
            }

            // Добавляем группы в результат
            foreach (var group in childGroups)
            {
                if (group.Value.Count == 1)
                    result[group.Key] = group.Value[0];
                else
                    result[group.Key] = group.Value;
            }

            // Текстовое содержимое
            if (!string.IsNullOrWhiteSpace(element.Value) && !element.HasElements)
            {
                result["#text"] = element.Value.Trim();
            }

            return result;
        }

        // ── Валидация ─────────────────────────────────────────────────────

        /// <summary>
        /// Валидация параметров активности.
        /// </summary>
        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_XmlString))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = "Prop_XmlString",
                    Error = "XML-строка не может быть пустой"
                });
            }

            return ret;
        }
    }
}
