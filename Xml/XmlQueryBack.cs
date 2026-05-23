// =============================================================================
// XmlQueryBack.cs — активность «XML: XPath запрос».
//
// Выполняет XPath запросы к XML-документу.
// Поддерживает режимы: одно значение, список значений, количество.
//
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Xml.XPath;
using System.Xml.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для выполнения XPath запросов к XML.
    /// </summary>
    public class XmlQueryBack : PrimoComponentTO<XmlQuery>
    {
        private readonly XmlQueryLogic _logic = new XmlQueryLogic();

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
        /// XML-строка для запроса.
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

        #region Prop_XPath

        private string _propXPath;

        /// <summary>
        /// XPath выражение.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("XPath")]
        public string Prop_XPath
        {
            get => _propXPath;
            set { _propXPath = value; InvokePropertyChanged(this, nameof(Prop_XPath)); }
        }

        #endregion

        #region Prop_QueryMode

        private XmlQueryMode _propQueryMode = XmlQueryMode.SingleValue;

        /// <summary>
        /// Режим запроса.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Режим")]
        public XmlQueryMode Prop_QueryMode
        {
            get => _propQueryMode;
            set { _propQueryMode = value; InvokePropertyChanged(this, nameof(Prop_QueryMode)); }
        }

        #endregion

        #region Prop_NamespacePrefix

        private string _propNamespacePrefix;

        /// <summary>
        /// Префикс пространства имён.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Префикс namespace")]
        public string Prop_NamespacePrefix
        {
            get => _propNamespacePrefix;
            set { _propNamespacePrefix = value; InvokePropertyChanged(this, nameof(Prop_NamespacePrefix)); }
        }

        #endregion

        #region Prop_NamespaceUri

        private string _propNamespaceUri;

        /// <summary>
        /// URI пространства имён.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("URI namespace")]
        public string Prop_NamespaceUri
        {
            get => _propNamespaceUri;
            set { _propNamespaceUri = value; InvokePropertyChanged(this, nameof(Prop_NamespaceUri)); }
        }

        #endregion

        // ── Выходные параметры ────────────────────────────────────────────

        #region Prop_Result

        private string _propResult;

        /// <summary>
        /// Результат запроса.
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

        #region Prop_Count

        private string _propCount;

        /// <summary>
        /// Количество найденных элементов.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Количество")]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, nameof(Prop_Count)); }
        }

        #endregion

        // ── Конструктор ───────────────────────────────────────────────────

        /// <summary>
        /// Конструктор активности «XML: XPath запрос».
        /// </summary>
        public XmlQueryBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "XML: XPath запрос";
            sdkComponentHelp =
                "Выполняет XPath запросы к XML-документу.\n" +
                "\n" +
                "── Режимы ──────────────────────────────────────\n" +
                "Одно значение — возвращает первое совпадение\n" +
                "Список       — возвращает все совпадения\n" +
                "Количество   — возвращает число совпадений\n" +
                "\n" +
                "── Параметры ───────────────────────────────────\n" +
                "XML-строка — исходный XML\n" +
                "XPath      — выражение для поиска\n" +
                "Namespace  — пространство имён (опционально)";

            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<string>("Prop_XmlString", "XML-строка"),
                PropertyBuilder.Script<string>("Prop_XPath", "XPath"),
                PropertyBuilder.Enum<XmlQueryMode>("Prop_QueryMode", "Режим"),
                PropertyBuilder.Script<string>("Prop_NamespacePrefix", "Префикс namespace"),
                PropertyBuilder.Script<string>("Prop_NamespaceUri", "URI namespace"),
                PropertyBuilder.Variable<object>("Prop_Result", "Результат"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_XmlString = "\"<root><item>value</item></root>\"";
            this.Prop_XPath = "\"//item\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────────

        /// <summary>
        /// Основной метод выполнения XPath запроса.
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Чтение входных параметров ──────────────────────────────
                string xmlString = GetPropertyValue<string>(this.Prop_XmlString, nameof(Prop_XmlString), sd);
                Guard.NotNullOrWhiteSpace(xmlString, nameof(Prop_XmlString));

                string xpath = GetPropertyValue<string>(this.Prop_XPath, nameof(Prop_XPath), sd);
                Guard.NotNullOrWhiteSpace(xpath, nameof(Prop_XPath));

                string nsPrefix = GetPropertyValue<string>(this.Prop_NamespacePrefix, nameof(Prop_NamespacePrefix), sd);
                string nsUri = GetPropertyValue<string>(this.Prop_NamespaceUri, nameof(Prop_NamespaceUri), sd);

                // ── Выполнение в зависимости от режима ────────────────────
                string resultMsg = ExecuteQueryMode(sd, xmlString, xpath, nsPrefix, nsUri);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = resultMsg
                };
            }
            catch (System.Xml.XmlException ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка XML: {ex.Message}"
                };
            }
            catch (ArgumentException ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Неверный аргумент: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [XML: XPath запрос]: {ex.Message}"
                };
            }
        }

        // ── Приватные методы ──────────────────────────────────────────────

        private string ExecuteQueryMode(ScriptingData sd, string xmlString, string xpath, string nsPrefix, string nsUri)
        {
            switch (this.Prop_QueryMode)
            {
                case XmlQueryMode.SingleValue:
                    return QuerySingleValue(sd, xmlString, xpath, nsPrefix, nsUri);

                case XmlQueryMode.List:
                    return QueryList(sd, xmlString, xpath, nsPrefix, nsUri);

                case XmlQueryMode.Count:
                    return QueryCount(sd, xmlString, xpath, nsPrefix, nsUri);

                default:
                    throw new ArgumentOutOfRangeException(nameof(Prop_QueryMode),
                        $"Неизвестный режим: {this.Prop_QueryMode}");
            }
        }

        private string QuerySingleValue(ScriptingData sd, string xmlString, string xpath, string nsPrefix, string nsUri)
        {
            string value = _logic.QuerySingleValue(xmlString, xpath, nsPrefix, nsUri);

            if (value == null)
            {
                if (!string.IsNullOrWhiteSpace(this.Prop_Result))
                    SetVariableValue<string>(this.Prop_Result, null, sd);

                if (!string.IsNullOrWhiteSpace(this.Prop_Count))
                    SetVariableValue(this.Prop_Count, 0, sd);

                return $"[Одно значение] Элемент не найден: {xpath}";
            }

            if (!string.IsNullOrWhiteSpace(this.Prop_Result))
                SetVariableValue(this.Prop_Result, value, sd);

            if (!string.IsNullOrWhiteSpace(this.Prop_Count))
                SetVariableValue(this.Prop_Count, 1, sd);

            return $"[Одно значение] Найдено: {value}";
        }

        private string QueryList(ScriptingData sd, string xmlString, string xpath, string nsPrefix, string nsUri)
        {
            var results = _logic.QueryList(xmlString, xpath, nsPrefix, nsUri);

            if (!string.IsNullOrWhiteSpace(this.Prop_Result))
                SetVariableValue(this.Prop_Result, results, sd);

            if (!string.IsNullOrWhiteSpace(this.Prop_Count))
                SetVariableValue(this.Prop_Count, results.Count, sd);

            return $"[Список] Найдено элементов: {results.Count}";
        }

        private string QueryCount(ScriptingData sd, string xmlString, string xpath, string nsPrefix, string nsUri)
        {
            int count = _logic.QueryCount(xmlString, xpath, nsPrefix, nsUri);

            if (!string.IsNullOrWhiteSpace(this.Prop_Result))
                SetVariableValue(this.Prop_Result, count, sd);

            if (!string.IsNullOrWhiteSpace(this.Prop_Count))
                SetVariableValue(this.Prop_Count, count, sd);

            return $"[Количество] Найдено: {count}";
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

            if (string.IsNullOrWhiteSpace(this.Prop_XPath))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = "Prop_XPath",
                    Error = "XPath не может быть пустым"
                });
            }

            return ret;
        }
    }
}
