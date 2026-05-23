// =============================================================================
// ElementSelectBack.cs — активность «Выбрать из списка».
//
// Выбирает опцию из выпадающего списка (select).
// Поддерживает выбор по тексту, значению или индексу.
//
// Режимы выбора:
//   ByText  — по видимому тексту опции
//   ByValue — по значению атрибута value
//   ByIndex — по порядковому номеру (начиная с 0)
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для выбора опции из выпадающего списка.
    /// </summary>
    public class ElementSelectBack : BrowserActivityBase<ElementSelect>
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
            set { _propSessionId = value; InvokePropertyChanged(this, nameof(Prop_SessionId)); }
        }

        private string _propElementId;
        /// <summary>ID элемента select (если элемент уже найден).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ElementId)]
        public string Prop_ElementId
        {
            get => _propElementId;
            set { _propElementId = value; InvokePropertyChanged(this, nameof(Prop_ElementId)); }
        }

        private ElementLocatorType _propLocatorType;
        /// <summary>Тип локатора для поиска элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorType)]
        public ElementLocatorType Prop_LocatorType
        {
            get => _propLocatorType;
            set { _propLocatorType = value; InvokePropertyChanged(this, nameof(Prop_LocatorType)); }
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
            set { _propLocatorValue = value; InvokePropertyChanged(this, nameof(Prop_LocatorValue)); }
        }

        private string _propWaitMode;
        /// <summary>Режим ожидания элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Wait),
         System.ComponentModel.DisplayName("Режим ожидания")]
        public ElementWaitMode Prop_WaitMode
        {
            get => (ElementWaitMode)Enum.Parse(typeof(ElementWaitMode), _propWaitMode ?? "Clickable");
            set { _propWaitMode = value.ToString(); InvokePropertyChanged(this, nameof(Prop_WaitMode)); }
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
            set { _propWaitTimeout = value; InvokePropertyChanged(this, nameof(Prop_WaitTimeout)); }
        }

        private SelectMode _propSelectMode;
        /// <summary>Режим выбора опции.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_SelectMode)]
        public SelectMode Prop_SelectMode
        {
            get => _propSelectMode;
            set { _propSelectMode = value; InvokePropertyChanged(this, nameof(Prop_SelectMode)); }
        }

        private string _propSelectValue;
        /// <summary>Значение для выбора (текст, value или индекс).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_SelectValue)]
        public string Prop_SelectValue
        {
            get => _propSelectValue;
            set { _propSelectValue = value; InvokePropertyChanged(this, nameof(Prop_SelectValue)); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementSelectBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ElementSelect;
            sdkComponentHelp =
                "Выбирает опцию из выпадающего списка.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии   — идентификатор сессии браузера\n" +
                "ID элемента — идентификатор select (если уже найден)\n" +
                "Режим выбора — способ выбора опции\n" +
                "Значение    — текст, value или индекс для выбора\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение локатора — конкретное значение для поиска\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Таймаут (сек) — время ожидания появления элемента\n" +
                "\n" +
                "── Режимы выбора ──────────────────────────────\n" +
                "ByText  — по видимому тексту опции\n" +
                "ByValue — по значению атрибута value\n" +
                "ByIndex — по порядковому номеру (с 0)\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Если указан локатор, элемент будет найден автоматически.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.Enum<ElementWaitMode>("Prop_WaitMode", "Режим ожидания"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.Enum<SelectMode>("Prop_SelectMode", "Режим выбора"),
                PropertyBuilder.String("Prop_SelectValue", "Значение для выбора")
            };

            InitClass(container);

            Prop_SessionId = "\"\"";
            Prop_ElementId = "\"\"";
            Prop_LocatorType = ElementLocatorType.Id;
            Prop_LocatorValue = "\"\"";
            Prop_WaitMode = ElementWaitMode.Clickable;
            Prop_WaitTimeout = "10";
            Prop_SelectMode = SelectMode.ByText;
            Prop_SelectValue = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return SafeExecute(() =>
            {
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                string elementId = GetPropertyValue<string>(Prop_ElementId, nameof(Prop_ElementId), sd);
                string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), sd);
                string selectValue = GetPropertyValue<string>(Prop_SelectValue, nameof(Prop_SelectValue), sd);

                Logger.LogInfo(sdkComponentName, "Начинается выбор из списка для сессии: {0}", sessionId);

                Guard.NotNullOrWhiteSpace(selectValue, nameof(selectValue));

                var driver = GetDriverFromContext(sessionId);

                IWebElement element;
                if (!string.IsNullOrWhiteSpace(locatorValue))
                {
                    string timeoutStr = GetPropertyValue<string>(Prop_WaitTimeout, nameof(Prop_WaitTimeout), sd) ?? "10";
                    int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                    ValidatePositive(timeout, "Prop_WaitTimeout");

                    var elementLocator = new ElementLocator();
                    var locatorType = ConvertLocatorType(Prop_LocatorType);
                    
                    Logger.LogDebug(sdkComponentName, 
                        "Ожидание select элемента: {0}='{1}', режим: {2}", 
                        locatorType, locatorValue, Prop_WaitMode);

                    element = elementLocator.FindElementWithWaitMode(
                        driver, locatorType, locatorValue, timeout, Prop_WaitMode);
                    
                    Logger.LogDebug(sdkComponentName, "Элемент найден по локатору: {0}={1}", Prop_LocatorType, locatorValue);
                }
                else if (!string.IsNullOrWhiteSpace(elementId))
                {
                    element = ElementRepository.GetElement(elementId);
                    if (element == null)
                        throw new ArgumentException($"Элемент с ID '{elementId}' не найден в репозитории");
                    
                    // Проверяем кликабельность select элемента из репозитория
                    if (!element.Displayed || !element.Enabled)
                    {
                        throw new ElementNotInteractableException(
                            $"Select элемент с ID '{elementId}' не кликабельный (Displayed: {element.Displayed}, Enabled: {element.Enabled})");
                    }
                    
                    Logger.LogDebug(sdkComponentName, "Использован сохраненный элемент: {0}", elementId);
                }
                else
                {
                    throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор для поиска");
                }

                var select = new SelectElement(element);

                switch (Prop_SelectMode)
                {
                    case SelectMode.ByText:
                        select.SelectByText(selectValue);
                        Logger.LogDebug(sdkComponentName, "Выбрано по тексту: {0}", selectValue);
                        break;

                    case SelectMode.ByValue:
                        select.SelectByValue(selectValue);
                        Logger.LogDebug(sdkComponentName, "Выбрано по значению: {0}", selectValue);
                        break;

                    case SelectMode.ByIndex:
                        if (int.TryParse(selectValue, out int index))
                        {
                            select.SelectByIndex(index);
                            Logger.LogDebug(sdkComponentName, "Выбрано по индексу: {0}", index);
                        }
                        else
                            throw new ArgumentException($"Некорректный индекс: {selectValue}");
                        break;

                    default:
                        throw new ArgumentException($"Неизвестный режим выбора: {Prop_SelectMode}");
                }

                string resultMsg = !string.IsNullOrWhiteSpace(locatorValue)
                    ? $"[Выбрать из списка] Выбрано ({Prop_SelectMode}): {selectValue}, локатор: {Prop_LocatorType}={locatorValue}"
                    : $"[Выбрать из списка] Выбрано ({Prop_SelectMode}): {selectValue}, элемент: {elementId}";

                Logger.LogInfo(sdkComponentName, "Выбор из списка завершен успешно");
                return CreateSuccessResult(resultMsg);
            }, "Выбрать из списка");
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            ret.ValidateRequired(Prop_SelectValue, ActivityStrings.Field_SelectValue, "Значение для выбора обязательно");

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

            return ret;
        }
    }
}
