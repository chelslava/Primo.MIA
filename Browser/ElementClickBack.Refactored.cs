// =============================================================================
// ElementClickUnifiedBack.cs — объединённая активность «Клик по элементу».
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
    /// Объединённая активность для выполнения различных типов кликов по элементу.
    /// Режим клика задаётся через свойство <see cref="Prop_ClickMode"/>.
    /// </summary>
    public class ElementClickBack : BrowserActivityBase<ElementClick>
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

        private ClickMode _propClickMode;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName("Режим клика")]
        public ClickMode Prop_ClickMode
        {
            get => _propClickMode;
            set { _propClickMode = value; InvokePropertyChanged(this, nameof(Prop_ClickMode)); }
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

        private bool _propUseJavaScript;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName("Использовать JavaScript")]
        public bool Prop_UseJavaScript
        {
            get => _propUseJavaScript;
            set { _propUseJavaScript = value; InvokePropertyChanged(this, nameof(Prop_UseJavaScript)); }
        }

        private string _propWaitAfterClick;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_WaitAfterClick)]
        public string Prop_WaitAfterClick
        {
            get => _propWaitAfterClick;
            set { _propWaitAfterClick = value; InvokePropertyChanged(this, nameof(Prop_WaitAfterClick)); }
        }

        private string _propHoldDuration;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName("Длительность удержания (мс)")]
        public string Prop_HoldDuration
        {
            get => _propHoldDuration;
            set { _propHoldDuration = value; InvokePropertyChanged(this, nameof(Prop_HoldDuration)); }
        }

        #endregion

        #region Constructor

        public ElementClickBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Элемент: Клик";
            sdkComponentHelp =
                "Выполняет клик по элементу в заданном режиме.\n" +
                "\n" +
                "── Режимы клика ──────────────────────────────\n" +
                "Click        — обычный одиночный клик\n" +
                "DoubleClick  — двойной клик\n" +
                "RightClick   — правый клик (контекстное меню)\n" +
                "ClickAndHold — клик с удержанием кнопки мыши\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии        — идентификатор сессии браузера\n" +
                "ID элемента      — идентификатор элемента (если уже найден)\n" +
                "Использовать JS  — JS-клик (только для Click и DoubleClick)\n" +
                "Ожидание после   — пауза после клика, мс (не для ClickAndHold)\n" +
                "Удержание (мс)   — время удержания (только для ClickAndHold)\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора    — способ поиска элемента\n" +
                "Значение        — конкретное значение для поиска\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Таймаут (сек)   — время ожидания появления элемента";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<ClickMode>("Prop_ClickMode", "Режим клика"),
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.BooleanObject("Prop_UseJavaScript", "Использовать JavaScript"),
                PropertyBuilder.Int("Prop_WaitAfterClick", "Ожидание после клика (мс)"),
                PropertyBuilder.Int("Prop_HoldDuration", "Длительность удержания (мс)")
            };

            InitClass(container);

            // Значения по умолчанию
            Prop_ClickMode = ClickMode.Click;
            Prop_SessionId = "\"\"";
            Prop_ElementId = "\"\"";
            Prop_LocatorType = ElementLocatorType.Id;
            Prop_LocatorValue = "\"\"";
            Prop_WaitTimeout = "10";
            Prop_UseJavaScript = false;
            Prop_WaitAfterClick = "0";
            Prop_HoldDuration = "1000";
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

                // Выполнение клика
                string resultMsg = PerformClickAction(sd, driver, element);

                return CreateSuccessResult(resultMsg);
            }, 
            $"Клик — {Prop_ClickMode}");
        }

        #endregion

        #region Element Resolution

        /// <summary>
        /// Разрешает IWebElement: сначала по локатору (если задан), затем по ID.
        /// </summary>
        private IWebElement ResolveElement(ScriptingData sd, IWebDriver driver)
        {
            string elementId = GetPropertyValue<string>(Prop_ElementId, nameof(Prop_ElementId), sd);
            string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

            // Guard clause: проверка что хотя бы один идентификатор указан
            if (string.IsNullOrWhiteSpace(locatorValue) && string.IsNullOrWhiteSpace(elementId))
                throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор для поиска");

            // Приоритет: локатор > elementId
            if (!string.IsNullOrWhiteSpace(locatorValue))
            {
                int timeout = ParseTimeout(sd);
                return FindElementByLocator(driver, locatorValue, timeout);
            }

            return SeleniumHelper.GetElement(elementId);
        }

        /// <summary>
        /// Находит элемент по локатору с ожиданием кликабельности.
        /// </summary>
        private IWebElement FindElementByLocator(IWebDriver driver, string locatorValue, int timeout)
        {
            By locator = SeleniumHelper.CreateLocator(Prop_LocatorType, locatorValue);
            return SeleniumHelper.WaitForElementClickable(driver, locator, timeout);
        }

        /// <summary>
        /// Парсит и валидирует таймаут из свойства.
        /// </summary>
        private int ParseTimeout(ScriptingData sd)
        {
            string timeoutStr = GetPropertyValue<string>(Prop_WaitTimeout, nameof(Prop_WaitTimeout), sd) ?? "10";
            int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
            return SeleniumHelper.ValidateTimeout(timeout, 10);
        }

        #endregion

        #region Click Actions

        /// <summary>
        /// Выполняет клик в соответствии с выбранным режимом.
        /// </summary>
        private string PerformClickAction(ScriptingData sd, IWebDriver driver, IWebElement element)
        {
            string elementLabel = GetElementLabel();

            switch (Prop_ClickMode)
            {
                case ClickMode.Click:
                    return PerformSingleClick(driver, element, elementLabel, sd);

                case ClickMode.DoubleClick:
                    return PerformDoubleClick(driver, element, elementLabel, sd);

                case ClickMode.RightClick:
                    return PerformRightClick(driver, element, elementLabel, sd);

                case ClickMode.ClickAndHold:
                    return PerformClickAndHold(driver, element, elementLabel, sd);

                default:
                    throw new ArgumentOutOfRangeException(nameof(Prop_ClickMode),
                        $"Неизвестный режим клика: {Prop_ClickMode}");
            }
        }

        private string PerformSingleClick(IWebDriver driver, IWebElement element, string label, ScriptingData sd)
        {
            if (Prop_UseJavaScript)
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
            else
                element.Click();

            ApplyWaitAfterClick(sd);
            return $"[Click] Выполнен клик: {label}";
        }

        private string PerformDoubleClick(IWebDriver driver, IWebElement element, string label, ScriptingData sd)
        {
            if (Prop_UseJavaScript)
                SeleniumHelper.DoubleClickJS(driver, element);
            else
                SeleniumHelper.DoubleClick(driver, element);

            ApplyWaitAfterClick(sd);
            return $"[DoubleClick] Выполнен двойной клик: {label}";
        }

        private string PerformRightClick(IWebDriver driver, IWebElement element, string label, ScriptingData sd)
        {
            SeleniumHelper.RightClick(driver, element);
            ApplyWaitAfterClick(sd);
            return $"[RightClick] Выполнен правый клик: {label}";
        }

        private string PerformClickAndHold(IWebDriver driver, IWebElement element, string label, ScriptingData sd)
        {
            int duration = ParseHoldDuration(sd);
            SeleniumHelper.ClickAndHold(driver, element, duration);
            return $"[ClickAndHold] Удержание {duration}мс: {label}";
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Формирует метку элемента для сообщения результата.
        /// </summary>
        private string GetElementLabel()
        {
            if (!string.IsNullOrWhiteSpace(Prop_LocatorValue))
                return $"{Prop_LocatorType}={Prop_LocatorValue}";
            
            return Prop_ElementId;
        }

        /// <summary>
        /// Выполняет паузу после клика, если задано положительное значение.
        /// </summary>
        private void ApplyWaitAfterClick(ScriptingData sd)
        {
            string waitStr = GetPropertyValue<string>(Prop_WaitAfterClick, nameof(Prop_WaitAfterClick), sd) ?? "0";
            if (int.TryParse(waitStr, out int waitMs) && waitMs > 0)
                System.Threading.Thread.Sleep(waitMs);
        }

        /// <summary>
        /// Парсит и валидирует длительность удержания.
        /// </summary>
        private int ParseHoldDuration(ScriptingData sd)
        {
            string durationStr = GetPropertyValue<string>(Prop_HoldDuration, nameof(Prop_HoldDuration), sd) ?? "1000";
            int duration = int.TryParse(durationStr, out int d) ? Math.Max(d, 0) : 1000;
            return duration;
        }

        #endregion

        #region Validation

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            // Должен быть указан хотя бы один идентификатор элемента
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

            // Валидация длительности удержания
            if (Prop_ClickMode == ClickMode.ClickAndHold
                && int.TryParse(Prop_HoldDuration, out int dur)
                && dur < 0)
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_HoldDuration),
                    Error = "Длительность удержания не может быть отрицательной"
                });
            }

            return ret;
        }

        #endregion
    }
}
