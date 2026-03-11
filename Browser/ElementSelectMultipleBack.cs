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
using OpenQA.Selenium.Support.UI;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для работы с multiple select элементами.
    /// </summary>
    public class ElementSelectMultipleBack : BrowserActivityBase<ElementSelectMultiple>
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

            Prop_SessionId = "\"\"";
            Prop_ElementId = "\"\"";
            Prop_LocatorType = ElementLocatorType.Id;
            Prop_LocatorValue = "\"\"";
            Prop_Operation = MultiSelectOperation.SelectByText;
            Prop_Value = "\"\"";
            Prop_WaitTimeout = "10";
            Prop_OutOptions = "";
            Prop_OutSelectedOptions = "";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return SafeExecute(() =>
            {
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                string elementId = GetPropertyValue<string>(Prop_ElementId, nameof(Prop_ElementId), sd);
                string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

                Logger.LogInfo(sdkComponentName, "Начинается операция множественного выбора для сессии: {0}", sessionId);

                var driver = GetDriverFromContext(sessionId);

                IWebElement element;
                if (!string.IsNullOrWhiteSpace(locatorValue))
                {
                    string timeoutStr = GetPropertyValue<string>(Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                    int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                    ValidatePositive(timeout, "Prop_WaitTimeout");

                    var locatorType = ConvertLocatorType(Prop_LocatorType);
                    element = ElementLocator.FindElement(driver, locatorType, locatorValue, timeout);
                    
                    Logger.LogDebug(sdkComponentName, "Элемент найден по локатору: {0}={1}", Prop_LocatorType, locatorValue);
                }
                else if (!string.IsNullOrWhiteSpace(elementId))
                {
                    element = ElementRepository.GetElement(elementId);
                    if (element == null)
                        throw new ArgumentException($"Элемент с ID '{elementId}' не найден в репозитории");
                    
                    Logger.LogDebug(sdkComponentName, "Использован сохраненный элемент: {0}", elementId);
                }
                else
                {
                    throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор для поиска");
                }

                var select = new SelectElement(element);
                string resultMsg;

                Logger.LogDebug(sdkComponentName, "Выполняется операция: {0}", Prop_Operation);

                switch (Prop_Operation)
                {
                    case MultiSelectOperation.SelectByText:
                        {
                            string value = GetPropertyValue<string>(Prop_Value, "Prop_Value", sd);
                            ValidateNotEmpty(value, "Prop_Value");

                            select.SelectByText(value);
                            resultMsg = $"[Multiple Select] Выбрано по тексту: {value}";
                            Logger.LogDebug(sdkComponentName, "Выбрано по тексту: {0}", value);
                        }
                        break;

                    case MultiSelectOperation.SelectByValue:
                        {
                            string value = GetPropertyValue<string>(Prop_Value, "Prop_Value", sd);
                            ValidateNotEmpty(value, "Prop_Value");

                            select.SelectByValue(value);
                            resultMsg = $"[Multiple Select] Выбрано по value: {value}";
                            Logger.LogDebug(sdkComponentName, "Выбрано по значению: {0}", value);
                        }
                        break;

                    case MultiSelectOperation.SelectByIndex:
                        {
                            string indexStr = GetPropertyValue<string>(Prop_Value, "Prop_Value", sd) ?? "0";
                            int index = int.TryParse(indexStr, out int idx) ? idx : 0;

                            select.SelectByIndex(index);
                            resultMsg = $"[Multiple Select] Выбрано по индексу: {index}";
                            Logger.LogDebug(sdkComponentName, "Выбрано по индексу: {0}", index);
                        }
                        break;

                    case MultiSelectOperation.DeselectByText:
                        {
                            string value = GetPropertyValue<string>(Prop_Value, "Prop_Value", sd);
                            ValidateNotEmpty(value, "Prop_Value");

                            select.DeselectByText(value);
                            resultMsg = $"[Multiple Select] Снят выбор по тексту: {value}";
                            Logger.LogDebug(sdkComponentName, "Снят выбор по тексту: {0}", value);
                        }
                        break;

                    case MultiSelectOperation.DeselectByValue:
                        {
                            string value = GetPropertyValue<string>(Prop_Value, "Prop_Value", sd);
                            ValidateNotEmpty(value, "Prop_Value");

                            select.DeselectByValue(value);
                            resultMsg = $"[Multiple Select] Снят выбор по value: {value}";
                            Logger.LogDebug(sdkComponentName, "Снят выбор по значению: {0}", value);
                        }
                        break;

                    case MultiSelectOperation.DeselectByIndex:
                        {
                            string indexStr = GetPropertyValue<string>(Prop_Value, "Prop_Value", sd) ?? "0";
                            int index = int.TryParse(indexStr, out int idx) ? idx : 0;

                            select.DeselectByIndex(index);
                            resultMsg = $"[Multiple Select] Снят выбор по индексу: {index}";
                            Logger.LogDebug(sdkComponentName, "Снят выбор по индексу: {0}", index);
                        }
                        break;

                    case MultiSelectOperation.DeselectAll:
                        select.DeselectAll();
                        resultMsg = "[Multiple Select] Сняты все выборы";
                        Logger.LogDebug(sdkComponentName, "Сняты все выборы");
                        break;

                    case MultiSelectOperation.GetAllOptions:
                        {
                            var options = select.Options.Select(o => o.Text).ToList();
                            SetVariableValue(Prop_OutOptions, options, sd);
                            resultMsg = $"[Multiple Select] Получено опций: {options.Count}";
                            Logger.LogDebug(sdkComponentName, "Получено опций: {0}", options.Count);
                        }
                        break;

                    case MultiSelectOperation.GetSelectedOptions:
                        {
                            var selected = select.AllSelectedOptions.Select(o => o.Text).ToList();
                            SetVariableValue(Prop_OutSelectedOptions, selected, sd);
                            resultMsg = $"[Multiple Select] Выбрано опций: {selected.Count}";
                            Logger.LogDebug(sdkComponentName, "Выбрано опций: {0}", selected.Count);
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Операция {Prop_Operation} не поддерживается");
                }

                Logger.LogInfo(sdkComponentName, "Операция множественного выбора завершена успешно");
                return CreateSuccessResult(resultMsg);
            }, "Multiple Select");
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();


            bool hasElementId = !string.IsNullOrWhiteSpace(Prop_ElementId);
            bool hasLocator = !string.IsNullOrWhiteSpace(Prop_LocatorValue);

            if (!hasElementId && !hasLocator)
            {
                ret.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = "ElementId/LocatorValue",
                    Error = "Необходимо указать либо ID элемента, либо локатор для поиска"
                });
            }

            switch (Prop_Operation)
            {
                case MultiSelectOperation.SelectByText:
                case MultiSelectOperation.SelectByValue:
                case MultiSelectOperation.SelectByIndex:
                case MultiSelectOperation.DeselectByText:
                case MultiSelectOperation.DeselectByValue:
                case MultiSelectOperation.DeselectByIndex:
                    ret.ValidateRequired(Prop_Value, "Значение", "Значение обязательно для данной операции");
                    break;

                case MultiSelectOperation.GetAllOptions:
                    ret.ValidateRequired(Prop_OutOptions, "Список опций", "Переменная для списка опций обязательна");
                    break;

                case MultiSelectOperation.GetSelectedOptions:
                    ret.ValidateRequired(Prop_OutSelectedOptions, "Выбранные опции", "Переменная для выбранных опций обязательна");
                    break;
            }

            return ret;
        }
    }
}
