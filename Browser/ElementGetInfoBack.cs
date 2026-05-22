// =============================================================================
// ElementGetInfoBack.cs — активность «Получить информацию об элементе».
//
// Объединяет три режима получения информации:
//   - Property: свойства элемента (текст, атрибуты, CSS)
//   - ComputedStyle: вычисленные CSS стили
//   - Rectangle: размеры и позиция элемента
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
    /// Активность для получения информации об элементе на странице.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class ElementGetInfoBack : BrowserActivityBase<ElementGetInfo>
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

        private ElementInfoMode _propMode;
        /// <summary>Режим получения информации.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Режим")]
        public ElementInfoMode Prop_Mode
        {
            get => _propMode;
            set { _propMode = value; InvokePropertyChanged(this, "Prop_Mode"); }
        }

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

        private string _propPropertyName;
        /// <summary>Имя свойства (для режима Property и ComputedStyle).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Имя свойства")]
        public string Prop_PropertyName
        {
            get => _propPropertyName;
            set { _propPropertyName = value; InvokePropertyChanged(this, "Prop_PropertyName"); }
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

        private string _propValue;
        /// <summary>Значение свойства (для Property и ComputedStyle).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Значение")]
        public string Prop_Value
        {
            get => _propValue;
            set { _propValue = value; InvokePropertyChanged(this, "Prop_Value"); }
        }

        private string _propX;
        /// <summary>Координата X (для Rectangle).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("X")]
        public string Prop_X
        {
            get => _propX;
            set { _propX = value; InvokePropertyChanged(this, "Prop_X"); }
        }

        private string _propY;
        /// <summary>Координата Y (для Rectangle).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Y")]
        public string Prop_Y
        {
            get => _propY;
            set { _propY = value; InvokePropertyChanged(this, "Prop_Y"); }
        }

        private string _propWidth;
        /// <summary>Ширина (для Rectangle).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Ширина")]
        public string Prop_Width
        {
            get => _propWidth;
            set { _propWidth = value; InvokePropertyChanged(this, "Prop_Width"); }
        }

        private string _propHeight;
        /// <summary>Высота (для Rectangle).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Высота")]
        public string Prop_Height
        {
            get => _propHeight;
            set { _propHeight = value; InvokePropertyChanged(this, "Prop_Height"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

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

            this.Prop_Mode = ElementInfoMode.Property;
            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_PropertyName = "\"text\"";
            this.Prop_WaitTimeout = "10";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return SafeExecute(() =>
            {
                // Чтение параметров
                string sessionId = GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd);
                string elementId = GetPropertyValue<string>(this.Prop_ElementId, nameof(Prop_ElementId), sd);
                string locatorValue = GetPropertyValue<string>(this.Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

                Logger.LogInfo(sdkComponentName, "Начинается получение информации об элементе для сессии: {0}", sessionId);

                // Получение драйвера
                var driver = GetDriverFromContext(sessionId);

                // Получение элемента
                IWebElement element = GetElement(driver, elementId, locatorValue, sd);

                Logger.LogDebug(sdkComponentName, "Режим получения информации: {0}", this.Prop_Mode);

                // Выполнение действия в зависимости от режима
                ExecutionResult result;
                switch (this.Prop_Mode)
                {
                    case ElementInfoMode.Property:
                        result = ExecutePropertyMode(element, sd);
                        break;

                    case ElementInfoMode.ComputedStyle:
                        result = ExecuteComputedStyleMode(driver, element, sd);
                        break;

                    case ElementInfoMode.Rectangle:
                        result = ExecuteRectangleMode(element, sd);
                        break;

                    default:
                        throw new ArgumentException($"Неизвестный режим: {this.Prop_Mode}");
                }

                Logger.LogInfo(sdkComponentName, "Получение информации об элементе завершено успешно");
                return result;
            }, "Получить информацию");
        }

        // ── Приватные методы ───────────────────────────────────────────

        /// <summary>
        /// Получает элемент по ID или локатору.
        /// </summary>
        private IWebElement GetElement(IWebDriver driver, string elementId, string locatorValue, ScriptingData sd)
        {
            if (!string.IsNullOrWhiteSpace(locatorValue))
            {
                string timeoutStr = GetPropertyValue<string>(this.Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                ValidatePositive(timeout, "Prop_WaitTimeout");

                var locatorType = ConvertLocatorType(this.Prop_LocatorType);
                var element = ElementLocator.FindElement(driver, locatorType, locatorValue, timeout);
                
                Logger.LogDebug(sdkComponentName, "Элемент найден по локатору: {0}={1}", this.Prop_LocatorType, locatorValue);
                return element;
            }
            else if (!string.IsNullOrWhiteSpace(elementId))
            {
                var element = ElementRepository.GetElement(elementId);
                if (element == null)
                    throw new ArgumentException($"Элемент с ID '{elementId}' не найден в репозитории");
                
                Logger.LogDebug(sdkComponentName, "Использован сохраненный элемент: {0}", elementId);
                return element;
            }
            else
            {
                throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор для поиска");
            }
        }

        /// <summary>
        /// Режим Property: получение свойства элемента.
        /// </summary>
        private ExecutionResult ExecutePropertyMode(IWebElement element, ScriptingData sd)
        {
            string propertyName = GetPropertyValue<string>(this.Prop_PropertyName, "Prop_PropertyName", sd);

            Guard.NotNullOrWhiteSpace(propertyName, nameof(propertyName));

            string value = GetPropertyValue(element, propertyName);

            if (!string.IsNullOrWhiteSpace(this.Prop_Value))
                SetVariableValue(this.Prop_Value, value, sd);

            return new ExecutionResult
            {
                IsSuccess = true,
                SuccessMessage = $"[Получить свойство] {propertyName} = {value}"
            };
        }

        /// <summary>
        /// Режим ComputedStyle: получение вычисленного CSS стиля.
        /// </summary>
        private ExecutionResult ExecuteComputedStyleMode(IWebDriver driver, IWebElement element, ScriptingData sd)
        {
            string cssProperty = GetPropertyValue<string>(this.Prop_PropertyName, "Prop_PropertyName", sd);

            Guard.NotNullOrWhiteSpace(cssProperty, nameof(cssProperty));

            string value = SeleniumHelper.GetComputedStyle(driver, element, cssProperty);

            if (!string.IsNullOrWhiteSpace(this.Prop_Value))
                SetVariableValue(this.Prop_Value, value, sd);

            return new ExecutionResult
            {
                IsSuccess = true,
                SuccessMessage = $"[Получить CSS стиль] {cssProperty} = {value}"
            };
        }

        /// <summary>
        /// Режим Rectangle: получение размеров и позиции элемента.
        /// </summary>
        private ExecutionResult ExecuteRectangleMode(IWebElement element, ScriptingData sd)
        {
            var (x, y, width, height) = SeleniumHelper.GetElementRect(element);

            if (!string.IsNullOrWhiteSpace(this.Prop_X))
                SetVariableValue(this.Prop_X, x, sd);
            if (!string.IsNullOrWhiteSpace(this.Prop_Y))
                SetVariableValue(this.Prop_Y, y, sd);
            if (!string.IsNullOrWhiteSpace(this.Prop_Width))
                SetVariableValue(this.Prop_Width, width, sd);
            if (!string.IsNullOrWhiteSpace(this.Prop_Height))
                SetVariableValue(this.Prop_Height, height, sd);

            return new ExecutionResult
            {
                IsSuccess = true,
                SuccessMessage = $"[Получить размер] X={x}, Y={y}, Ширина={width}, Высота={height}"
            };
        }

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

            // Валидация в зависимости от режима
            switch (this.Prop_Mode)
            {
                case ElementInfoMode.Property:
                case ElementInfoMode.ComputedStyle:
                    ret.ValidateRequired(this.Prop_PropertyName, "Имя свойства", "Имя свойства обязательно");
                    ret.ValidateRequired(this.Prop_Value, "Значение", "Переменная для значения обязательна");
                    break;

                case ElementInfoMode.Rectangle:
                    ret.ValidateRequired(this.Prop_X, "X", "Переменная для X обязательна");
                    ret.ValidateRequired(this.Prop_Y, "Y", "Переменная для Y обязательна");
                    ret.ValidateRequired(this.Prop_Width, "Ширина", "Переменная для ширины обязательна");
                    ret.ValidateRequired(this.Prop_Height, "Высота", "Переменная для высоты обязательна");
                    break;
            }

            return ret;
        }
    }
}
