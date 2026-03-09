// =============================================================================
// ElementDoubleClickBack.cs — активность «Двойной клик по элементу».
//
// Выполняет двойной клик по элементу на странице.
// Поддерживает обычный двойной клик через Actions API и JavaScript клик.
//
// Используется для:
//   - Открытия файлов в веб-проводниках
//   - Редактирования ячеек в таблицах
//   - Активации элементов требующих двойного клика
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
    /// Активность для двойного клика по элементу на странице.
    /// </summary>
    public class ElementDoubleClickBack : PrimoComponentTO<ElementDoubleClick>
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
        /// <summary>ID элемента для двойного клика.</summary>
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

        private bool _propUseJavaScript;
        /// <summary>Использовать JavaScript для двойного клика.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Использовать JavaScript")]
        public bool Prop_UseJavaScript
        {
            get => _propUseJavaScript;
            set { _propUseJavaScript = value; InvokePropertyChanged(this, "Prop_UseJavaScript"); }
        }

        private string _propWaitAfterClick;
        /// <summary>Ожидание после клика (мс).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_WaitAfterClick)]
        public string Prop_WaitAfterClick
        {
            get => _propWaitAfterClick;
            set { _propWaitAfterClick = value; InvokePropertyChanged(this, "Prop_WaitAfterClick"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementDoubleClickBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Элемент: Двойной клик";
            sdkComponentHelp =
                "Выполняет двойной клик по элементу на странице.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "ID элемента — идентификатор элемента (если уже найден)\n" +
                "Использовать JavaScript — двойной клик через JS\n" +
                "Ожидание после клика — пауза после клика (мс)\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение локатора — конкретное значение для поиска\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Таймаут (сек) — время ожидания появления элемента\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: JavaScript клик работает для элементов,\n" +
                "которые не реагируют на стандартный двойной клик.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.BooleanObject("Prop_UseJavaScript", "Использовать JavaScript"),
                PropertyBuilder.Int("Prop_WaitAfterClick", "Ожидание после клика (мс)")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_WaitTimeout = "10";
            this.Prop_UseJavaScript = false;
            this.Prop_WaitAfterClick = "0";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Чтение параметров
                string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
                string elementId = GetPropertyValue<string>(this.Prop_ElementId, "Prop_ElementId", sd);
                string locatorValue = GetPropertyValue<string>(this.Prop_LocatorValue, "Prop_LocatorValue", sd);

                if (string.IsNullOrWhiteSpace(sessionId))
                    throw new ArgumentException("ID сессии не может быть пустым");

                // Получение драйвера
                var driver = SeleniumHelper.GetDriver(sessionId);

                // Получение элемента
                IWebElement element;
                if (!string.IsNullOrWhiteSpace(locatorValue))
                {
                    string timeoutStr = GetPropertyValue<string>(this.Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                    int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                    timeout = SeleniumHelper.ValidateTimeout(timeout, 10);

                    var locator = SeleniumHelper.CreateLocator(this.Prop_LocatorType, locatorValue);
                    element = SeleniumHelper.WaitForElementClickable(driver, locator, timeout);
                }
                else if (!string.IsNullOrWhiteSpace(elementId))
                {
                    element = SeleniumHelper.GetElement(elementId);
                }
                else
                {
                    throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор для поиска");
                }

                // Выполнение двойного клика
                if (this.Prop_UseJavaScript)
                {
                    SeleniumHelper.DoubleClickJS(driver, element);
                }
                else
                {
                    SeleniumHelper.DoubleClick(driver, element);
                }

                // Ожидание после клика
                string waitStr = GetPropertyValue<string>(this.Prop_WaitAfterClick, "Prop_WaitAfterClick", sd) ?? "0";
                if (int.TryParse(waitStr, out int waitMs) && waitMs > 0)
                {
                    System.Threading.Thread.Sleep(waitMs);
                }

                string resultMsg = !string.IsNullOrWhiteSpace(locatorValue)
                    ? $"[Двойной клик] Выполнен: {this.Prop_LocatorType}={locatorValue}"
                    : $"[Двойной клик] Выполнен: {elementId}";

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
                    ErrorMessage = $"Ошибка [Двойной клик]: {ex.Message}"
                };
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_SessionId, ActivityStrings.Field_SessionId, "ID сессии обязателен");

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
