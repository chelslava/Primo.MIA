// =============================================================================
// ElementTypeTextBack.cs — активность «Ввести текст».
//
// Вводит текст в поле ввода.
// Поддерживает обычный ввод и симуляцию печати.
//
// Используется для:
//   - Заполнения форм
//   - Ввода данных в поля
//   - Симуляции человеческого ввода
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
    /// Активность для ввода текста в поле.
    /// </summary>
    public class ElementTypeTextBack : PrimoComponentTO<ElementTypeText>
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
        /// <summary>ID элемента для очистки (если элемент уже найден).</summary>
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

        private string _propText;
        /// <summary>Текст для ввода.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_Text)]
        public string Prop_Text
        {
            get => _propText;
            set { _propText = value; InvokePropertyChanged(this, "Prop_Text"); }
        }

        private bool _propClearBeforeType;
        /// <summary>Очистить поле перед вводом.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Очистить перед вводом")]
        public bool Prop_ClearBeforeType
        {
            get => _propClearBeforeType;
            set { _propClearBeforeType = value; InvokePropertyChanged(this, "Prop_ClearBeforeType"); }
        }

        private bool _propSimulateTyping;
        /// <summary>Симулировать печать (с задержкой между символами).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Симулировать печать")]
        public bool Prop_SimulateTyping
        {
            get => _propSimulateTyping;
            set { _propSimulateTyping = value; InvokePropertyChanged(this, "Prop_SimulateTyping"); }
        }

        private string _propTypeDelay;
        /// <summary>Задержка между символами (мс).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Задержка между символами (мс)")]
        public string Prop_TypeDelay
        {
            get => _propTypeDelay;
            set { _propTypeDelay = value; InvokePropertyChanged(this, "Prop_TypeDelay"); }
        }

        private bool _propClickBeforeType;
        /// <summary>Кликнуть по элементу перед вводом.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ClickBeforeType)]
        public bool Prop_ClickBeforeType
        {
            get => _propClickBeforeType;
            set { _propClickBeforeType = value; InvokePropertyChanged(this, "Prop_ClickBeforeType"); }
        }

        private bool _propPressEnterAfter;
        /// <summary>Нажать Enter после ввода текста.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_PressEnterAfter)]
        public bool Prop_PressEnterAfter
        {
            get => _propPressEnterAfter;
            set { _propPressEnterAfter = value; InvokePropertyChanged(this, "Prop_PressEnterAfter"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementTypeTextBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Элемент: Ввести текст";
            sdkComponentHelp =
                "Вводит текст в поле ввода.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии   — идентификатор сессии браузера\n" +
                "ID элемента — идентификатор поля ввода (если уже найден)\n" +
                "Текст — текст для ввода\n" +
                "Кликнуть перед вводом — кликнуть по элементу перед вводом\n" +
                "Очистить перед вводом — очистить поле перед вводом\n" +
                "Симулировать печать — вводить с задержкой между символами\n" +
                "Задержка (мс) — задержка между символами при симуляции\n" +
                "Нажать Enter после — нажать Enter после ввода текста\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение локатора — конкретное значение для поиска\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Таймаут (сек) — время ожидания появления элемента\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Симуляция печати делает ввод более естественным.\n" +
                "Если указан локатор, элемент будет найден автоматически.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.String("Prop_Text", "Текст для ввода"),
                PropertyBuilder.BooleanObject("Prop_ClickBeforeType", "Кликнуть перед вводом"),
                PropertyBuilder.BooleanObject("Prop_ClearBeforeType", "Очистить перед вводом"),
                PropertyBuilder.BooleanObject("Prop_SimulateTyping", "Симулировать печать"),
                PropertyBuilder.Int("Prop_TypeDelay", "Задержка между символами (мс)"),
                PropertyBuilder.BooleanObject("Prop_PressEnterAfter", "Нажать Enter после ввода")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_WaitTimeout = "10";
            this.Prop_Text = "\"\"";
            this.Prop_ClickBeforeType = false;
            this.Prop_ClearBeforeType = true;
            this.Prop_SimulateTyping = false;
            this.Prop_TypeDelay = "50";
            this.Prop_PressEnterAfter = false;
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
                string text = GetPropertyValue<string>(this.Prop_Text, "Prop_Text", sd) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(sessionId))
                    throw new ArgumentException("ID сессии не может быть пустым");

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

                // Клик перед вводом (если требуется)
                if (this.Prop_ClickBeforeType)
                {
                    element.Click();
                }

                // Очистка перед вводом (если требуется)
                if (this.Prop_ClearBeforeType)
                {
                    element.Clear();
                }

                // Ввод текста
                if (this.Prop_SimulateTyping)
                {
                    // Симуляция печати с задержкой
                    string delayStr = GetPropertyValue<string>(this.Prop_TypeDelay, "Prop_TypeDelay", sd) ?? "50";
                    int delay = int.TryParse(delayStr, out int d) ? d : 50;
                    delay = Math.Max(1, Math.Min(delay, 1000)); // 1-1000 мс

                    foreach (char c in text)
                    {
                        element.SendKeys(c.ToString());
                        if (delay > 0)
                            System.Threading.Thread.Sleep(delay);
                    }
                }
                else
                {
                    // Обычный ввод
                    element.SendKeys(text);
                }

                // Нажатие Enter после ввода (если требуется)
                if (this.Prop_PressEnterAfter)
                {
                    element.SendKeys(Keys.Enter);
                }

                string resultMsg = !string.IsNullOrWhiteSpace(locatorValue)
                    ? $"[Ввести текст] Текст введён: {this.Prop_LocatorType}={locatorValue}"
                    : $"[Ввести текст] Текст введён в элемент {elementId}";

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
                    ErrorMessage = $"Ошибка [Ввести текст]: {ex.Message}"
                };
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_SessionId, ActivityStrings.Field_SessionId, "ID сессии обязателен");
            ret.ValidateRequired(this.Prop_Text, ActivityStrings.Field_Text, "Текст для ввода обязателен");
            
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
