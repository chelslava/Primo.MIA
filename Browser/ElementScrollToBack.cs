// =============================================================================
// ElementScrollToBack.cs — активность «Прокрутить к элементу».
//
// Прокручивает страницу к указанному элементу.
// Поддерживает различные варианты выравнивания элемента в окне.
//
// Выравнивание:
//   Top    — элемент в верхней части окна
//   Center — элемент в центре окна
//   Bottom — элемент в нижней части окна
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
    /// Активность для прокрутки страницы к элементу.
    /// </summary>
    public class ElementScrollToBack : PrimoComponentTO<ElementScrollTo>
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
        /// <summary>ID элемента для прокрутки (если элемент уже найден).</summary>
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

        private ScrollAlignment _propAlignment;
        /// <summary>Выравнивание элемента в окне.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ScrollAlignment)]
        public ScrollAlignment Prop_Alignment
        {
            get => _propAlignment;
            set { _propAlignment = value; InvokePropertyChanged(this, "Prop_Alignment"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

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

            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_WaitTimeout = "10";
            this.Prop_Alignment = ScrollAlignment.Center;
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Чтение параметров через SessionResolver (поддержка ambient-контекста)
                string sessionId = SessionResolver.Resolve(
                    GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
                string elementId = GetPropertyValue<string>(this.Prop_ElementId, nameof(Prop_ElementId), sd);
                string locatorValue = GetPropertyValue<string>(this.Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

                // Получение драйвера
                var driver = SeleniumHelper.GetDriver(sessionId);

                // Получение элемента: либо по ID, либо поиск по локатору
                IWebElement element;
                if (!string.IsNullOrWhiteSpace(locatorValue))
                {
                    // Поиск элемента по локатору
                    string timeoutStr = GetPropertyValue<string>(this.Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                    int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                    timeout = SeleniumHelper.ValidateTimeout(timeout, 10);

                    var locator = SeleniumHelper.CreateLocator(this.Prop_LocatorType, locatorValue);
                    element = SeleniumHelper.WaitForElement(driver, locator, timeout);
                }
                else if (!string.IsNullOrWhiteSpace(elementId))
                {
                    // Использование существующего элемента
                    element = SeleniumHelper.GetElement(elementId);
                }
                else
                {
                    throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор для поиска");
                }

                // Прокрутка к элементу
                SeleniumHelper.ScrollToElement(driver, element, this.Prop_Alignment);

                string resultMsg = !string.IsNullOrWhiteSpace(locatorValue)
                    ? $"[Прокрутить к элементу] {this.Prop_LocatorType}={locatorValue} ({this.Prop_Alignment})"
                    : $"[Прокрутить к элементу] {elementId} ({this.Prop_Alignment})";

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
                    ErrorMessage = $"Ошибка [Прокрутить к элементу]: {ex.Message}"
                };
            }
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
