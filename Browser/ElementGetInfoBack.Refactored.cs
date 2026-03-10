// =============================================================================
// ElementGetInfoBack.cs — активность «Получить информацию об элементе».
//
// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using Primo.MIA.Browser;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для получения информации об элементе на странице.
    /// </summary>
    public class ElementGetInfoBack : BrowserActivityBase<ElementGetInfo>
    {
        #region Properties and Configuration

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

        #endregion

        #region Input Properties

        private ElementInfoMode _propMode;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName("Режим")]
        public ElementInfoMode Prop_Mode
        {
            get => _propMode;
            set { _propMode = value; InvokePropertyChanged(this, nameof(Prop_Mode)); }
        }

        private string _propSessionId;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_SessionId)]
        public string Prop_SessionId
        {
            get => _propSessionId;
            set { _propSessionId = value; InvokePropertyChanged(this, nameof(Prop_SessionId)); }
        }

        private string _propElementId;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_ElementId)]
        public string Prop_ElementId
        {
            get => _propElementId;
            set { _propElementId = value; InvokePropertyChanged(this, nameof(Prop_ElementId)); }
        }

        private ElementLocatorType _propLocatorType;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorType)]
        public ElementLocatorType Prop_LocatorType
        {
            get => _propLocatorType;
            set { _propLocatorType = value; InvokePropertyChanged(this, nameof(Prop_LocatorType)); }
        }

        private string _propLocatorValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorValue)]
        public string Prop_LocatorValue
        {
            get => _propLocatorValue;
            set { _propLocatorValue = value; InvokePropertyChanged(this, nameof(Prop_LocatorValue)); }
        }

        private string _propPropertyName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName("Имя свойства")]
        public string Prop_PropertyName
        {
            get => _propPropertyName;
            set { _propPropertyName = value; InvokePropertyChanged(this, nameof(Prop_PropertyName)); }
        }

        private string _propWaitTimeout;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Wait)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_WaitTimeout)]
        public string Prop_WaitTimeout
        {
            get => _propWaitTimeout;
            set { _propWaitTimeout = value; InvokePropertyChanged(this, nameof(Prop_WaitTimeout)); }
        }

        // Output properties
        private string _propValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName("Значение")]
        public string Prop_Value
        {
            get => _propValue;
            set { _propValue = value; InvokePropertyChanged(this, nameof(Prop_Value)); }
        }

        private string _propX;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName("X")]
        public string Prop_X
        {
            get => _propX;
            set { _propX = value; InvokePropertyChanged(this, nameof(Prop_X)); }
        }

        private string _propY;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName("Y")]
        public string Prop_Y
        {
            get => _propY;
            set { _propY = value; InvokePropertyChanged(this, nameof(Prop_Y)); }
        }

        private string _propWidth;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName("Ширина")]
        public string Prop_Width
        {
            get => _propWidth;
            set { _propWidth = value; InvokePropertyChanged(this, nameof(Prop_Width)); }
        }

        private string _propHeight;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output)]
        [System.ComponentModel.DisplayName("Высота")]
        public string Prop_Height
        {
            get => _propHeight;
            set { _propHeight = value; InvokePropertyChanged(this, nameof(Prop_Height)); }
        }

        #endregion

        #region Constructor

        public ElementGetInfoBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Элемент: Получить информацию";
            sdkComponentHelp =
                "Получает информацию об элементе на странице.\n" +
                "\n" +
                "── Режимы работы ──────────────────────────────\n" +
                "Property       — свойство элемента (text, attribute:name, css:property)\n" +
                "ComputedStyle  — вычисленный CSS стиль\n" +
                "Rectangle      — размеры и позиция элемента\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "Режим         — тип информации для получения\n" +
                "ID сессии     — идентификатор сессии браузера\n" +
                "ID элемента   — идентификатор элемента (если уже найден)\n" +
                "Имя свойства  — для Property и ComputedStyle\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора     — способ поиска элемента\n" +
                "Значение локатора — конкретное значение для поиска\n" +
                "\n" +
                "── Выходные данные ───────────────────────────\n" +
                "Значение — для Property и ComputedStyle\n" +
                "X, Y, Ширина, Высота — для Rectangle";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<ElementInfoMode>("Prop_Mode", "Режим получения информации"),
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.String("Prop_PropertyName", "Имя свойства (для Property/ComputedStyle)"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.Variable<string>("Prop_Value", "Значение свойства"),
                PropertyBuilder.Variable<int>("Prop_X", "X"),
                PropertyBuilder.Variable<int>("Prop_Y", "Y"),
                PropertyBuilder.Variable<int>("Prop_Width", "Ширина"),
                PropertyBuilder.Variable<int>("Prop_Height", "Высота")
            };

            InitClass(container);

            // Defaults
            Prop_Mode = ElementInfoMode.Property;
            Prop_SessionId = "\"\"";
            Prop_ElementId = "\"\"";
            Prop_LocatorType = ElementLocatorType.Id;
            Prop_LocatorValue = "\"\"";
            Prop_PropertyName = "\"text\"";
            Prop_WaitTimeout = "10";
        }

        #endregion

        #region Main Execution

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return SafeExecute(() =>
            {
                // Получение драйвера через базовый класс
                IWebDriver driver = GetDriverFromContext(sd, nameof(Prop_SessionId));

                // Поиск элемента
                IWebElement element = ResolveElement(sd, driver);

                // Выполнение действия в зависимости от режима
                return PerformInfoRetrieval(sd, driver, element);
            },
            $"Получить информацию — {Prop_Mode}");
        }

        #endregion

        #region Element Resolution

        private IWebElement ResolveElement(ScriptingData sd, IWebDriver driver)
        {
            string elementId = GetPropertyValue<string>(Prop_ElementId, nameof(Prop_ElementId), sd);
            string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

            // Guard clause
            if (string.IsNullOrWhiteSpace(locatorValue) && string.IsNullOrWhiteSpace(elementId))
                throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор для поиска");

            // Приоритет: локатор > elementId
            if (!string.IsNullOrWhiteSpace(locatorValue))
            {
                int timeout = ParseTimeout(sd);
                return FindElement(driver, Prop_LocatorType, locatorValue, timeout);
            }

            return SeleniumHelper.GetElement(elementId);
        }

        private int ParseTimeout(ScriptingData sd)
        {
            string timeoutStr = GetPropertyValue<string>(Prop_WaitTimeout, nameof(Prop_WaitTimeout), sd) ?? "10";
            int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
            return SeleniumHelper.ValidateTimeout(timeout, 10);
        }

        #endregion

        #region Info Retrieval

        private ExecutionResult PerformInfoRetrieval(ScriptingData sd, IWebDriver driver, IWebElement element)
        {
            switch (Prop_Mode)
            {
                case ElementInfoMode.Property:
                    return GetPropertyInfo(sd, element);

                case ElementInfoMode.ComputedStyle:
                    return GetComputedStyleInfo(sd, driver, element);

                case ElementInfoMode.Rectangle:
                    return GetRectangleInfo(sd, element);

                default:
                    throw new ArgumentOutOfRangeException(nameof(Prop_Mode),
                        $"Неизвестный режим: {Prop_Mode}");
            }
        }

        private ExecutionResult GetPropertyInfo(ScriptingData sd, IWebElement element)
        {
            string propertyName = GetPropertyValue<string>(Prop_PropertyName, nameof(Prop_PropertyName), sd);
            
            // Guard clause
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("Имя свойства не может быть пустым");

            string value = ExtractPropertyValue(element, propertyName);

            if (!string.IsNullOrWhiteSpace(Prop_Value))
                SetVariableValue(Prop_Value, value, sd);

            return CreateSuccessResult($"[Получить свойство] {propertyName} = {value}");
        }

        private ExecutionResult GetComputedStyleInfo(ScriptingData sd, IWebDriver driver, IWebElement element)
        {
            string cssProperty = GetPropertyValue<string>(Prop_PropertyName, nameof(Prop_PropertyName), sd);
            
            // Guard clause
            if (string.IsNullOrWhiteSpace(cssProperty))
                throw new ArgumentException("CSS свойство не может быть пустым");

            string value = SeleniumHelper.GetComputedStyle(driver, element, cssProperty);

            if (!string.IsNullOrWhiteSpace(Prop_Value))
                SetVariableValue(Prop_Value, value, sd);

            return CreateSuccessResult($"[Получить CSS стиль] {cssProperty} = {value}");
        }

        private ExecutionResult GetRectangleInfo(ScriptingData sd, IWebElement element)
        {
            var (x, y, width, height) = SeleniumHelper.GetElementRect(element);

            if (!string.IsNullOrWhiteSpace(Prop_X))
                SetVariableValue(Prop_X, x, sd);
            if (!string.IsNullOrWhiteSpace(Prop_Y))
                SetVariableValue(Prop_Y, y, sd);
            if (!string.IsNullOrWhiteSpace(Prop_Width))
                SetVariableValue(Prop_Width, width, sd);
            if (!string.IsNullOrWhiteSpace(Prop_Height))
                SetVariableValue(Prop_Height, height, sd);

            return CreateSuccessResult($"[Получить размер] X={x}, Y={y}, Ширина={width}, Высота={height}");
        }

        #endregion

        #region Property Extraction

        /// <summary>
        /// Извлекает значение свойства элемента по имени.
        /// Поддерживает text, attribute:name, css:property.
        /// </summary>
        private string ExtractPropertyValue(IWebElement element, string propertyName)
        {
            propertyName = propertyName.ToLower().Trim();

            // Текст элемента
            if (propertyName == "text")
                return element.Text ?? string.Empty;

            // HTML атрибут
            if (propertyName.StartsWith("attribute:"))
            {
                string attrName = propertyName.Substring("attribute:".Length).Trim();
                return element.GetAttribute(attrName) ?? string.Empty;
            }

            // CSS свойство
            if (propertyName.StartsWith("css:"))
            {
                string cssName = propertyName.Substring("css:".Length).Trim();
                return element.GetCssValue(cssName) ?? string.Empty;
            }

            // По умолчанию пытаемся получить как атрибут
            return element.GetAttribute(propertyName) ?? string.Empty;
        }

        #endregion

        #region Validation

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            bool hasElementId = !string.IsNullOrWhiteSpace(Prop_ElementId);
            bool hasLocator = !string.IsNullOrWhiteSpace(Prop_LocatorValue);

            if (!hasElementId && !hasLocator)
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = "ElementId/LocatorValue",
                    Error = "Необходимо указать либо ID элемента, либо локатор для поиска"
                });
            }

            // Валидация в зависимости от режима
            switch (Prop_Mode)
            {
                case ElementInfoMode.Property:
                case ElementInfoMode.ComputedStyle:
                    ret.ValidateRequired(Prop_PropertyName, "Имя свойства", "Имя свойства обязательно");
                    ret.ValidateRequired(Prop_Value, "Значение", "Переменная для значения обязательна");
                    break;

                case ElementInfoMode.Rectangle:
                    ret.ValidateRequired(Prop_X, "X", "Переменная для X обязательна");
                    ret.ValidateRequired(Prop_Y, "Y", "Переменная для Y обязательна");
                    ret.ValidateRequired(Prop_Width, "Ширина", "Переменная для ширины обязательна");
                    ret.ValidateRequired(Prop_Height, "Высота", "Переменная для высоты обязательна");
                    break;
            }

            return ret;
        }

        #endregion
    }
}
