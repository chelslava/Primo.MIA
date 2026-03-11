// =============================================================================
// ElementWaitAndCheckBack.cs — активность «Ожидать и проверить элемент».
//
// Объединяет функциональность ожидания элемента и проверки его состояния.
// Поддерживает различные режимы ожидания через enum ElementWaitMode.
//
// Режимы ожидания:
//   - Present:   элемент появился в DOM (может быть невидимым)
//   - Visible:   элемент видим на странице (Displayed = true)
//   - Clickable: элемент кликабельный (Displayed = true и Enabled = true)
//   - None:      не ожидать, попытаться найти немедленно
//
// Проверяемые состояния:
//   - Displayed: элемент видим на странице
//   - Enabled:   элемент включён (не disabled)
//   - Selected:  элемент выбран (для checkbox, radio)
//
// Таймаут активности (sdkTimeOut) вычисляется автоматически:
//   Prop_WaitTimeout + 5 секунд — пользователю не показывается.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для ожидания и проверки состояния элемента на странице.
    /// Объединяет логику ожидания элемента с проверкой его видимости.
    /// </summary>
    public class ElementWaitAndCheckBack : BrowserActivityBase<ElementWaitAndCheck>
    {
        public override string GroupName
        {
            get => ActivityCategories.Browser;
            protected set { }
        }

        /// <summary>
        /// Таймаут SDK в миллисекундах.
        /// Вычисляется автоматически как Prop_WaitTimeout + 5 секунд.
        /// </summary>
        protected override int sdkTimeOut
        {
            get
            {
                string waitStr = this.Prop_WaitTimeout?.Trim('"') ?? "10";
                int waitSec = int.TryParse(waitStr, out int w) ? w : 10;
                return (waitSec + 5) * 1000;
            }
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

        private string _propLocatorType;
        /// <summary>Тип локатора для поиска элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorType)]
        public ElementLocatorType Prop_LocatorType
        {
            get => (ElementLocatorType)Enum.Parse(typeof(ElementLocatorType), _propLocatorType ?? "Id");
            set { _propLocatorType = value.ToString(); InvokePropertyChanged(this, "Prop_LocatorType"); }
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
            get => (ElementWaitMode)Enum.Parse(typeof(ElementWaitMode), _propWaitMode ?? "Visible");
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

        private string _propElementFound;
        /// <summary>Элемент найден.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Элемент найден")]
        public string Prop_ElementFound
        {
            get => _propElementFound;
            set { _propElementFound = value; InvokePropertyChanged(this, "Prop_ElementFound"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementWaitAndCheckBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Ожидать и проверить элемент";
            sdkComponentHelp =
                "Ожидает появления элемента и проверяет его состояние.\n" +
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
                "Режим ожидания — стратегия ожидания элемента:\n" +
                "  • Present   — элемент появился в DOM\n" +
                "  • Visible   — элемент видим на странице\n" +
                "  • Clickable — элемент кликабельный\n" +
                "  • None      — не ожидать, найти немедленно\n" +
                "Таймаут (сек) — время ожидания элемента\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Элемент найден — элемент обнаружен\n" +
                "Видим    — элемент отображается (Displayed)\n" +
                "Включён  — элемент активен (Enabled)\n" +
                "Выбран   — элемент выбран (Selected)\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Selected применимо для checkbox и radio.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId",   "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId",   "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue",          "Значение локатора"),
                PropertyBuilder.Enum<ElementWaitMode>("Prop_WaitMode", "Режим ожидания"),
                PropertyBuilder.Int("Prop_WaitTimeout",              "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.Variable<bool>("Prop_ElementFound",  "Элемент найден"),
                PropertyBuilder.Variable<bool>("Prop_IsVisible",     "Элемент видим"),
                PropertyBuilder.Variable<bool>("Prop_IsEnabled",     "Элемент включён"),
                PropertyBuilder.Variable<bool>("Prop_IsSelected",    "Элемент выбран")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_WaitMode = ElementWaitMode.Visible;
            this.Prop_WaitTimeout = "10";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return SafeExecute(() =>
            {
                // Читаем входные параметры
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                string elementId = GetPropertyValue<string>(Prop_ElementId, nameof(Prop_ElementId), sd);
                string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

                Logger.LogInfo(sdkComponentName,
                    "Начинается ожидание и проверка элемента для сессии: {0}, режим: {1}", 
                    sessionId, Prop_WaitMode);

                IWebDriver driver = GetDriverFromContext(sessionId);

                // Читаем таймаут ожидания
                string waitStr = GetPropertyValue<string>(Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                int waitTimeout = int.TryParse(waitStr, out int wt) ? wt : 10;
                ValidatePositive(waitTimeout, "Prop_WaitTimeout");

                // Ищем элемент с указанным режимом ожидания
                IWebElement element = FindElementWithWaitMode(driver, elementId, locatorValue, waitTimeout);

                if (element == null)
                {
                    // Элемент не найден — присваиваем все флаги false
                    Logger.LogInfo(sdkComponentName, "Элемент не найден — все флаги установлены в false");
                    SetElementFlags(false, false, false, false, sd);
                    return CreateSuccessResult("[Ожидать и проверить] Элемент не найден");
                }

                // Элемент найден — читаем его реальное состояние
                bool isVisible = element.Displayed;
                bool isEnabled = element.Enabled;
                bool isSelected = element.Selected;

                Logger.LogDebug(sdkComponentName,
                    "Состояние элемента — Видим: {0}, Включён: {1}, Выбран: {2}",
                    isVisible, isEnabled, isSelected);

                SetElementFlags(true, isVisible, isEnabled, isSelected, sd);

                Logger.LogInfo(sdkComponentName, "Ожидание и проверка завершены успешно");
                return CreateSuccessResult(
                    $"[Ожидать и проверить] Найден: True, Видим: {isVisible}, Включён: {isEnabled}, Выбран: {isSelected}");

            }, "Ожидать и проверить элемент");
        }

        /// <summary>
        /// Находит элемент с указанным режимом ожидания.
        /// </summary>
        private IWebElement FindElementWithWaitMode(
            IWebDriver driver, string elementId, string locatorValue, int waitTimeout)
        {
            var elementLocator = new ElementLocator();

            // Если указан ID элемента из репозитория
            if (!string.IsNullOrWhiteSpace(elementId))
            {
                try
                {
                    return ElementRepository.GetElement(elementId);
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(sdkComponentName, "Элемент по ID не найден: {0}", ex.Message);
                    return null;
                }
            }

            // Если указан локатор
            if (!string.IsNullOrWhiteSpace(locatorValue))
            {
                var locatorType = ConvertLocatorType(Prop_LocatorType);
                return elementLocator.TryFindElementWithWaitMode(
                    driver, locatorType, locatorValue, waitTimeout, Prop_WaitMode);
            }

            return null;
        }

        /// <summary>
        /// Записывает значения флагов элемента в выходные переменные скрипта.
        /// </summary>
        private void SetElementFlags(bool found, bool isVisible, bool isEnabled, bool isSelected, ScriptingData sd)
        {
            new[]
            {
                (Name: Prop_ElementFound, Value: (object)found),
                (Name: Prop_IsVisible,    Value: (object)isVisible),
                (Name: Prop_IsEnabled,    Value: (object)isEnabled),
                (Name: Prop_IsSelected,   Value: (object)isSelected)
            }
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .ToList()
            .ForEach(p => SetVariableValue(p.Name, p.Value, sd));
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

            return ret;
        }
    }
}