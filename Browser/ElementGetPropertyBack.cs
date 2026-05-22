// =============================================================================
// ElementGetPropertyBack.cs — активность «Получить свойство элемента».
//
// Извлекает свойства элемента: текст, атрибуты, CSS свойства.
// Используется для чтения данных со страницы.
//
// Типы свойств:
//   - Text: видимый текст элемента
//   - Attribute: значение HTML атрибута
//   - CssValue: значение CSS свойства
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для получения свойств элемента на странице.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class ElementGetPropertyBack : BrowserActivityBase<ElementGetProperty>
    {
        public override string GroupName
        {
            get => ActivityCategories.Browser;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 10000;
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
        /// <summary>ID элемента.</summary>
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

        private string _propWaitMode;
        /// <summary>Режим ожидания элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Wait),
         System.ComponentModel.DisplayName("Режим ожидания")]
        public ElementWaitMode Prop_WaitMode
        {
            get => (ElementWaitMode)Enum.Parse(typeof(ElementWaitMode), _propWaitMode ?? "Present");
            set { _propWaitMode = value.ToString(); InvokePropertyChanged(this, "Prop_WaitMode"); }
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

        private string _propPropertyName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_PropertyName)]
        public string Prop_PropertyName
        {
            get => _propPropertyName;
            set { _propPropertyName = value; InvokePropertyChanged(this, "Prop_PropertyName"); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propValue;
        /// <summary>Значение свойства.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ElementValue)]
        public string Prop_Value
        {
            get => _propValue;
            set { _propValue = value; InvokePropertyChanged(this, "Prop_Value"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementGetPropertyBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ElementGetProperty;
            sdkComponentHelp =
                "Получает свойство элемента на странице.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии    — идентификатор сессии браузера\n" +
                "ID элемента  — идентификатор элемента (если уже найден)\n" +
                "Имя свойства — text, attribute:name, css:property\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение локатора — конкретное значение для поиска\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Режим ожидания — стратегия ожидания элемента\n" +
                "Таймаут (сек) — время ожидания элемента\n" +
                "\n" +
                "Примеры:\n" +
                "  text              — видимый текст элемента\n" +
                "  attribute:href    — значение атрибута href\n" +
                "  attribute:value   — значение поля ввода\n" +
                "  css:color         — CSS свойство color\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Значение — полученное значение свойства";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.Enum<ElementWaitMode>("Prop_WaitMode", "Режим ожидания"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.String("Prop_PropertyName", "Имя свойства (text/attribute:name/css:property)"),
                PropertyBuilder.Variable<string>("Prop_Value", "Значение свойства")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_WaitMode = ElementWaitMode.Present;
            this.Prop_WaitTimeout = "10";
            this.Prop_PropertyName = "\"text\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return SafeExecute(() =>
            {
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                string elementId = GetPropertyValue<string>(Prop_ElementId, nameof(Prop_ElementId), sd);
                string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), sd);
                string propertyName = GetPropertyValue<string>(Prop_PropertyName, nameof(Prop_PropertyName), sd);

                Logger.LogInfo(sdkComponentName, "Получение свойства элемента для сессии: {0}", sessionId);

                ValidateNotEmpty(propertyName, nameof(propertyName));

                IWebDriver driver = GetDriverFromContext(sessionId);

                IWebElement element;
                if (!string.IsNullOrWhiteSpace(locatorValue))
                {
                    string timeoutStr = GetPropertyValue<string>(Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                    int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                    ValidatePositive(timeout, "Prop_WaitTimeout");

                    var elementLocator = new ElementLocator();
                    var locatorType = ConvertLocatorType(Prop_LocatorType);
                    
                    Logger.LogDebug(sdkComponentName, 
                        "Ожидание элемента для получения свойства: {0}='{1}', режим: {2}", 
                        locatorType, locatorValue, Prop_WaitMode);

                    element = elementLocator.FindElementWithWaitMode(
                        driver, locatorType, locatorValue, timeout, Prop_WaitMode);
                }
                else if (!string.IsNullOrWhiteSpace(elementId))
                {
                    element = ElementRepository.GetElement(elementId);
                    if (element == null)
                        throw new ArgumentException($"Элемент с ID '{elementId}' не найден в репозитории");
                }
                else
                {
                    throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор для поиска");
                }

                string value = GetPropertyValue(element, propertyName);

                if (!string.IsNullOrWhiteSpace(Prop_Value))
                    SetVariableValue(Prop_Value, value, sd);

                Logger.LogInfo(sdkComponentName, "Свойство получено: {0} = {1}", propertyName, value);
                return CreateSuccessResult($"[Получить свойство] {propertyName} = {value}");

            }, "Получить свойство");
        }

        // ── Приватные методы ───────────────────────────────────────────

        /// <summary>
        /// Извлекает значение свойства элемента по имени.
        /// Поддерживает text, attribute:name, css:property.
        /// </summary>
        private string GetPropertyValue(IWebElement element, string propertyName)
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

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            // Проверяем, что указан хотя бы один из способов идентификации элемента
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

            ret.ValidateRequired(Prop_PropertyName, ActivityStrings.Field_PropertyName, "Имя свойства обязательно");
            return ret;
        }
    }
}
