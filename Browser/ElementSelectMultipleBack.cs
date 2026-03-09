// =============================================================================
// ElementSelectMultipleBack.cs — активность «Работа с multiple select».
//
// Управляет множественным выбором в select[multiple] элементах.
// Поддерживает выбор и снятие выбора нескольких опций.
//
// Используется для:
//   - Выбора нескольких опций в списке
//   - Снятия выбора с опций
//   - Получения списка всех/выбранных опций
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для работы с multiple select элементами.
    /// </summary>
    public class ElementSelectMultipleBack : PrimoComponentTO<ElementSelectMultiple>
    {
        public override string GroupName
        {
            get => ActivityCategories.Browser;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 30000;
            set { }
        }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propSessionId;
        /// <summary>ID сессии браузера.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_SessionId)]
        public string Prop_SessionId
        {
            get => _propSessionId;
            set { _propSessionId = value; InvokePropertyChanged(this, "Prop_SessionId"); }
        }

        private string _propElementId;
        /// <summary>ID select элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ElementId)]
        public string Prop_ElementId
        {
            get => _propElementId;
            set { _propElementId = value; InvokePropertyChanged(this, "Prop_ElementId"); }
        }

        private ElementLocatorType _propLocatorType;
        /// <summary>Тип локатора для поиска элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorType)]
        public ElementLocatorType Prop_LocatorType
        {
            get => _propLocatorType;
            set { _propLocatorType = value; InvokePropertyChanged(this, "Prop_LocatorType"); }
        }

        private string _propLocatorValue;
        /// <summary>Значение локатора для поиска элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorValue)]
        public string Prop_LocatorValue
        {
            get => _propLocatorValue;
            set { _propLocatorValue = value; InvokePropertyChanged(this, "Prop_LocatorValue"); }
        }

        private MultiSelectOperation _propOperation;
        /// <summary>Операция с select.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Операция")]
        public MultiSelectOperation Prop_Operation
        {
            get => _propOperation;
            set { _propOperation = value; InvokePropertyChanged(this, "Prop_Operation"); }
        }

        private string _propValue;
        /// <summary>Значение для выбора/снятия выбора.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Значение")]
        public string Prop_Value
        {
            get => _propValue;
            set { _propValue = value; InvokePropertyChanged(this, "Prop_Value"); }
        }

        private string _propWaitTimeout;
        /// <summary>Таймаут ожидания элемента (сек).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Wait),
         System.ComponentModel.DisplayName(ActivityStrings.Field_WaitTimeout)]
        public string Prop_WaitTimeout
        {
            get => _propWaitTimeout;
            set { _propWaitTimeout = value; InvokePropertyChanged(this, "Prop_WaitTimeout"); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propOutOptions;
        /// <summary>Список опций.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Список опций")]
        public string Prop_OutOptions
        {
            get => _propOutOptions;
            set { _propOutOptions = value; InvokePropertyChanged(this, "Prop_OutOptions"); }
        }

        private string _propOutSelectedOptions;
        /// <summary>Список выбранных опций.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Выбранные опции")]
        public string Prop_OutSelectedOptions
        {
            get => _propOutSelectedOptions;
            set { _propOutSelectedOptions = value; InvokePropertyChanged(this, "Prop_OutSelectedOptions"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementSelectMultipleBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Элемент: Multiple Select";
            sdkComponentHelp =
                "Работа с множественным выбором в select[multiple].\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "ID элемента — идентификатор select элемента\n" +
                "Операция — тип операции\n" +
                "Значение — текст/value/индекс для выбора\n" +
                "\n" +
                "── Операции ──────────────────────────────────\n" +
                "SelectByText — выбрать по тексту\n" +
                "SelectByValue — выбрать по value\n" +
                "SelectByIndex — выбрать по индексу\n" +
                "DeselectByText — снять выбор по тексту\n" +
                "DeselectByValue — снять выбор по value\n" +
                "DeselectByIndex — снять выбор по индексу\n" +
                "DeselectAll — снять все выборы\n" +
                "GetAllOptions — получить все опции\n" +
                "GetSelectedOptions — получить выбранные\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение локатора — конкретное значение\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Элемент должен быть select[multiple].";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID элемента"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.Enum<MultiSelectOperation>("Prop_Operation", "Операция"),
                PropertyBuilder.String("Prop_Value", "Значение"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания (сек)"),
                PropertyBuilder.Variable<List<string>>("Prop_OutOptions", "Список опций"),
                PropertyBuilder.Variable<List<string>>("Prop_OutSelectedOptions", "Выбранные опции")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_Operation = MultiSelectOperation.SelectByText;
            this.Prop_Value = "\"\"";
            this.Prop_WaitTimeout = "10";
            this.Prop_OutOptions = "";
            this.Prop_OutSelectedOptions = "";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
                string elementId = GetPropertyValue<string>(this.Prop_ElementId, "Prop_ElementId", sd);
                string locatorValue = GetPropertyValue<string>(this.Prop_LocatorValue, "Prop_LocatorValue", sd);

                if (string.IsNullOrWhiteSpace(sessionId))
                    throw new ArgumentException("ID сессии не может быть пустым");

                var driver = SeleniumHelper.GetDriver(sessionId);

                // Получение элемента
                IWebElement element;
                if (!string.IsNullOrWhiteSpace(locatorValue))
                {
                    string timeoutStr = GetPropertyValue<string>(this.Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                    int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                    timeout = SeleniumHelper.ValidateTimeout(timeout, 10);

                    var locator = SeleniumHelper.CreateLocator(this.Prop_LocatorType, locatorValue);
                    element = SeleniumHelper.WaitForElement(driver, locator, timeout);
                }
                else if (!string.IsNullOrWhiteSpace(elementId))
                {
                    element = SeleniumHelper.GetElement(elementId);
                }
                else
                {
                    throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор для поиска");
                }

                string resultMsg;

                switch (this.Prop_Operation)
                {
                    case MultiSelectOperation.SelectByText:
                        {
                            string value = GetPropertyValue<string>(this.Prop_Value, "Prop_Value", sd);
                            if (string.IsNullOrWhiteSpace(value))
                                throw new ArgumentException("Значение не может быть пустым для операции SelectByText");

                            SeleniumHelper.SelectMultipleByText(element, value);
                            resultMsg = $"[Multiple Select] Выбрано по тексту: {value}";
                        }
                        break;

                    case MultiSelectOperation.SelectByValue:
                        {
                            string value = GetPropertyValue<string>(this.Prop_Value, "Prop_Value", sd);
                            if (string.IsNullOrWhiteSpace(value))
                                throw new ArgumentException("Значение не может быть пустым для операции SelectByValue");

                            SeleniumHelper.SelectMultipleByValue(element, value);
                            resultMsg = $"[Multiple Select] Выбрано по value: {value}";
                        }
                        break;

                    case MultiSelectOperation.SelectByIndex:
                        {
                            string indexStr = GetPropertyValue<string>(this.Prop_Value, "Prop_Value", sd) ?? "0";
                            int index = int.TryParse(indexStr, out int idx) ? idx : 0;

                            SeleniumHelper.SelectMultipleByIndex(element, index);
                            resultMsg = $"[Multiple Select] Выбрано по индексу: {index}";
                        }
                        break;

                    case MultiSelectOperation.DeselectByText:
                        {
                            string value = GetPropertyValue<string>(this.Prop_Value, "Prop_Value", sd);
                            if (string.IsNullOrWhiteSpace(value))
                                throw new ArgumentException("Значение не может быть пустым для операции DeselectByText");

                            SeleniumHelper.DeselectByText(element, value);
                            resultMsg = $"[Multiple Select] Снят выбор по тексту: {value}";
                        }
                        break;

                    case MultiSelectOperation.DeselectByValue:
                        {
                            string value = GetPropertyValue<string>(this.Prop_Value, "Prop_Value", sd);
                            if (string.IsNullOrWhiteSpace(value))
                                throw new ArgumentException("Значение не может быть пустым для операции DeselectByValue");

                            SeleniumHelper.DeselectByValue(element, value);
                            resultMsg = $"[Multiple Select] Снят выбор по value: {value}";
                        }
                        break;

                    case MultiSelectOperation.DeselectByIndex:
                        {
                            string indexStr = GetPropertyValue<string>(this.Prop_Value, "Prop_Value", sd) ?? "0";
                            int index = int.TryParse(indexStr, out int idx) ? idx : 0;

                            SeleniumHelper.DeselectByIndex(element, index);
                            resultMsg = $"[Multiple Select] Снят выбор по индексу: {index}";
                        }
                        break;

                    case MultiSelectOperation.DeselectAll:
                        SeleniumHelper.DeselectAll(element);
                        resultMsg = "[Multiple Select] Сняты все выборы";
                        break;

                    case MultiSelectOperation.GetAllOptions:
                        {
                            var options = SeleniumHelper.GetAllSelectOptions(element);
                            SetVariableValue(this.Prop_OutOptions, options, sd);
                            resultMsg = $"[Multiple Select] Получено опций: {options.Count}";
                        }
                        break;

                    case MultiSelectOperation.GetSelectedOptions:
                        {
                            var selected = SeleniumHelper.GetSelectedSelectOptions(element);
                            SetVariableValue(this.Prop_OutSelectedOptions, selected, sd);
                            resultMsg = $"[Multiple Select] Выбрано опций: {selected.Count}";
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Операция {this.Prop_Operation} не поддерживается");
                }

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
                    ErrorMessage = $"Ошибка [Multiple Select]: {ex.Message}"
                };
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_SessionId, ActivityStrings.Field_SessionId, "ID сессии обязателен");

            bool hasElementId = !string.IsNullOrWhiteSpace(this.Prop_ElementId);
            bool hasLocator = !string.IsNullOrWhiteSpace(this.Prop_LocatorValue);

            if (!hasElementId && !hasLocator)
            {
                ret.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = "ElementId/LocatorValue",
                    Error = "Необходимо указать либо ID элемента, либо локатор для поиска"
                });
            }

            // Валидация в зависимости от операции
            switch (this.Prop_Operation)
            {
                case MultiSelectOperation.SelectByText:
                case MultiSelectOperation.SelectByValue:
                case MultiSelectOperation.SelectByIndex:
                case MultiSelectOperation.DeselectByText:
                case MultiSelectOperation.DeselectByValue:
                case MultiSelectOperation.DeselectByIndex:
                    ret.ValidateRequired(this.Prop_Value, "Значение", "Значение обязательно для данной операции");
                    break;

                case MultiSelectOperation.GetAllOptions:
                    ret.ValidateRequired(this.Prop_OutOptions, "Список опций", "Переменная для списка опций обязательна");
                    break;

                case MultiSelectOperation.GetSelectedOptions:
                    ret.ValidateRequired(this.Prop_OutSelectedOptions, "Выбранные опции", "Переменная для выбранных опций обязательна");
                    break;
            }

            return ret;
        }
    }
}
