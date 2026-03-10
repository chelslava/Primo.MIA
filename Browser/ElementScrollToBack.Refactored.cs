// =============================================================================
// ElementScrollToBack.cs — активность «Прокрутить к элементу».
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
    /// Активность для прокрутки страницы к элементу.
    /// </summary>
    public class ElementScrollToBack : BrowserActivityBase<ElementScrollTo>
    {
        #region Properties and Configuration

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

        #endregion

        #region Input Properties

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

        private ScrollAlignment _propAlignment;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_ScrollAlignment)]
        public ScrollAlignment Prop_Alignment
        {
            get => _propAlignment;
            set { _propAlignment = value; InvokePropertyChanged(this, nameof(Prop_Alignment)); }
        }

        #endregion

        #region Constructor

        public ElementScrollToBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ElementScrollTo;
            sdkComponentHelp =
                "Прокручивает страницу к указанному элементу.\n" +
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
                "── Настройки ──────────────────────────────────\n" +
                "Выравнивание — положение элемента в окне\n" +
                "  Top    — в верхней части\n" +
                "  Center — в центре\n" +
                "  Bottom — в нижней части\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Если указан локатор, элемент будет найден автоматически.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.Enum<ScrollAlignment>("Prop_Alignment", "Выравнивание элемента")
            };

            InitClass(container);

            // Defaults
            Prop_SessionId = "\"\"";
            Prop_ElementId = "\"\"";
            Prop_LocatorType = ElementLocatorType.Id;
            Prop_LocatorValue = "\"\"";
            Prop_WaitTimeout = "10";
            Prop_Alignment = ScrollAlignment.Center;
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

                // Прокрутка к элементу
                SeleniumHelper.ScrollToElement(driver, element, Prop_Alignment);

                string resultMsg = $"[Прокрутить к элементу] {GetElementLabel()} ({Prop_Alignment})";
                return CreateSuccessResult(resultMsg);
            },
            "Прокрутить к элементу");
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

            return ret;
        }

        #endregion
    }
}
