// =============================================================================
// ElementIsVisibleBack.cs — активность «Проверить видимость элемента».
//
// Проверяет состояние элемента: видимость, включённость, выбранность.
// Используется для условной логики и валидации состояния UI.
//
// Проверяемые состояния:
//   - Displayed: элемент видим на странице
//   - Enabled: элемент включён (не disabled)
//   - Selected: элемент выбран (для checkbox, radio)
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
    /// Активность для проверки состояния элемента на странице.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class ElementIsVisibleBack : BrowserActivityBase<ElementIsVisible>
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
        /// <summary>ID элемента для проверки (если элемент уже найден).</summary>
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

        private string _propIsVisible;
        /// <summary>Элемент видим.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_IsVisible)]
        public string Prop_IsVisible
        {
            get => _propIsVisible;
            set { _propIsVisible = value; InvokePropertyChanged(this, "Prop_IsVisible"); }
        }

        private string _propIsEnabled;
        /// <summary>Элемент включён.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_IsEnabled)]
        public string Prop_IsEnabled
        {
            get => _propIsEnabled;
            set { _propIsEnabled = value; InvokePropertyChanged(this, "Prop_IsEnabled"); }
        }

        private string _propIsSelected;
        /// <summary>Элемент выбран.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_IsSelected)]
        public string Prop_IsSelected
        {
            get => _propIsSelected;
            set { _propIsSelected = value; InvokePropertyChanged(this, "Prop_IsSelected"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementIsVisibleBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ElementIsVisible;
            sdkComponentHelp =
                "Проверяет состояние элемента на странице.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии   — идентификатор сессии браузера\n" +
                "ID элемента — идентификатор элемента (если уже найден)\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение локатора — конкретное значение для поиска\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Таймаут (сек) — время ожидания появления элемента\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Видим    — элемент отображается (Displayed)\n" +
                "Включён  — элемент активен (Enabled)\n" +
                "Выбран   — элемент выбран (Selected)\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Selected применимо для checkbox и radio.\n" +
                "Если указан локатор, элемент будет найден автоматически.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.Variable<bool>("Prop_IsVisible", "Элемент видим"),
                PropertyBuilder.Variable<bool>("Prop_IsEnabled", "Элемент включён"),
                PropertyBuilder.Variable<bool>("Prop_IsSelected", "Элемент выбран")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_WaitTimeout = "10";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return SafeExecute(() =>
            {
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                string elementId = GetPropertyValue<string>(Prop_ElementId, nameof(Prop_ElementId), sd);
                string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

                Logger.LogInfo(sdkComponentName, "Начинается проверка видимости элемента для сессии: {0}", sessionId);

                IWebDriver driver = GetDriverFromContext(sessionId);

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

                // Проверяем состояние элемента
                bool isVisible = element.Displayed;
                bool isEnabled = element.Enabled;
                bool isSelected = element.Selected;

                Logger.LogDebug(sdkComponentName, "Состояние элемента - Видим: {0}, Включён: {1}, Выбран: {2}", 
                    isVisible, isEnabled, isSelected);

                // Устанавливаем выходные переменные
                if (!string.IsNullOrWhiteSpace(Prop_IsVisible))
                    SetVariableValue(Prop_IsVisible, isVisible, sd);

                if (!string.IsNullOrWhiteSpace(Prop_IsEnabled))
                    SetVariableValue(Prop_IsEnabled, isEnabled, sd);

                if (!string.IsNullOrWhiteSpace(Prop_IsSelected))
                    SetVariableValue(Prop_IsSelected, isSelected, sd);

                Logger.LogInfo(sdkComponentName, "Проверка видимости завершена успешно");
                return CreateSuccessResult($"[Проверить видимость] Видим: {isVisible}, Включён: {isEnabled}, Выбран: {isSelected}");
            }, "Проверить видимость");
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();


            // Проверяем что указан либо ElementId, либо LocatorValue
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

            return ret;
        }
    }
}
