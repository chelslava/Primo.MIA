// =============================================================================
// BrowserWaitForBack.cs — активность «Ожидание условия».
//
// Универсальная активность для ожидания различных условий в браузере.
// Поддерживает ожидание элементов, текста, URL, алертов и других условий.
//
// Типы условий:
//   ElementExists    — элемент существует в DOM
//   ElementVisible   — элемент видим
//   ElementClickable — элемент кликабелен
//   ElementInvisible — элемент невидим
//   TextPresent      — текст присутствует в элементе
//   TitleContains    — заголовок содержит текст
//   UrlContains      — URL содержит текст
//   AlertPresent     — алерт присутствует
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
    /// Активность для ожидания различных условий в браузере.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class BrowserWaitForBack : BrowserActivityBase<BrowserWaitFor>
    {
        public override string GroupName
        {
            get => ActivityCategories.Browser;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get
            {
                string timeoutStr = GetPropertyValue<string>(this.Prop_Timeout, nameof(Prop_Timeout), null) ?? "30";
                if (int.TryParse(timeoutStr, out int timeout))
                    return timeout * 1000;
                return 30000;
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
            set { _propSessionId = value; InvokePropertyChanged(this, nameof(Prop_SessionId)); }
        }

        private WaitConditionType _propCondition;
        /// <summary>Тип условия ожидания.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Wait),
         System.ComponentModel.DisplayName(ActivityStrings.Field_WaitCondition)]
        public WaitConditionType Prop_Condition
        {
            get => _propCondition;
            set { _propCondition = value; InvokePropertyChanged(this, nameof(Prop_Condition)); }
        }

        private ElementLocatorType _propLocatorType;
        /// <summary>Тип локатора (для условий с элементами).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorType)]
        public ElementLocatorType Prop_LocatorType
        {
            get => _propLocatorType;
            set { _propLocatorType = value; InvokePropertyChanged(this, nameof(Prop_LocatorType)); }
        }

        private string _propLocatorValue;
        /// <summary>Значение локатора.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorValue)]
        public string Prop_LocatorValue
        {
            get => _propLocatorValue;
            set { _propLocatorValue = value; InvokePropertyChanged(this, nameof(Prop_LocatorValue)); }
        }

        private string _propText;
        /// <summary>Текст для проверки (для TextPresent, TitleContains, UrlContains).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Wait),
         System.ComponentModel.DisplayName(ActivityStrings.Field_Text)]
        public string Prop_Text
        {
            get => _propText;
            set { _propText = value; InvokePropertyChanged(this, nameof(Prop_Text)); }
        }

        private string _propTimeout;
        /// <summary>Таймаут ожидания в секундах.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Wait),
         System.ComponentModel.DisplayName(ActivityStrings.Field_WaitTimeout)]
        public string Prop_Timeout
        {
            get => _propTimeout;
            set { _propTimeout = value; InvokePropertyChanged(this, nameof(Prop_Timeout)); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propConditionMet;
        /// <summary>Условие выполнено.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Условие выполнено")]
        public string Prop_ConditionMet
        {
            get => _propConditionMet;
            set { _propConditionMet = value; InvokePropertyChanged(this, nameof(Prop_ConditionMet)); }
        }

        private string _propWaitTime;
        /// <summary>Время ожидания в миллисекундах.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_WaitTime)]
        public string Prop_WaitTime
        {
            get => _propWaitTime;
            set { _propWaitTime = value; InvokePropertyChanged(this, nameof(Prop_WaitTime)); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public BrowserWaitForBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_BrowserWaitFor;
            sdkComponentHelp =
                "Ожидает выполнения условия в браузере.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "\n" +
                "── Ожидание ───────────────────────────────────\n" +
                "Условие  — тип проверяемого условия\n" +
                "Таймаут  — максимальное время ожидания (сек)\n" +
                "\n" +
                "── Локатор (для условий с элементами) ─────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение     — конкретное значение\n" +
                "\n" +
                "── Текст (для текстовых условий) ──────────────\n" +
                "Текст — строка для проверки\n" +
                "\n" +
                "Типы условий:\n" +
                "  ElementExists    — элемент в DOM\n" +
                "  ElementVisible   — элемент видим\n" +
                "  ElementClickable — элемент кликабелен\n" +
                "  ElementInvisible — элемент невидим\n" +
                "  TextPresent      — текст в элементе\n" +
                "  TitleContains    — заголовок содержит текст\n" +
                "  UrlContains      — URL содержит текст\n" +
                "  AlertPresent     — алерт присутствует";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Enum<WaitConditionType>("Prop_Condition", "Тип условия ожидания"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.String("Prop_Text", "Текст для проверки"),
                PropertyBuilder.Int("Prop_Timeout", "Таймаут ожидания (сек)"),
                PropertyBuilder.Variable<bool>("Prop_ConditionMet", "Условие выполнено"),
                PropertyBuilder.Variable<int>("Prop_WaitTime", "Время ожидания (мс)")
            };

            InitClass(container);

            Prop_SessionId = "\"\"";
            Prop_Condition = WaitConditionType.ElementVisible;
            Prop_LocatorType = ElementLocatorType.Id;
            Prop_LocatorValue = "\"\"";
            Prop_Text = "\"\"";
            Prop_Timeout = "30";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var startTime = DateTime.Now;

            try
            {
                // Получаем sessionId
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                string timeoutStr = GetPropertyValue<string>(Prop_Timeout, nameof(Prop_Timeout), sd) ?? "30";

                int timeout = int.TryParse(timeoutStr, out int t) ? t : 30;
                timeout = SeleniumHelper.ValidateTimeout(timeout, 30);

                // Получение драйвера через базовый класс
                IWebDriver driver = GetDriverFromContext(sessionId);

                // Выполнение ожидания в зависимости от условия
                bool conditionMet = WaitForCondition(driver, timeout, sd);

                // Вычисление времени ожидания
                int waitTime = (int)(DateTime.Now - startTime).TotalMilliseconds;

                // Запись результатов
                if (!string.IsNullOrWhiteSpace(Prop_ConditionMet))
                    SetVariableValue(Prop_ConditionMet, conditionMet, sd);

                if (!string.IsNullOrWhiteSpace(Prop_WaitTime))
                    SetVariableValue(Prop_WaitTime, waitTime, sd);

                return CreateSuccessResult($"[Ожидание условия] {Prop_Condition} → {conditionMet} ({waitTime}мс)");
            }
            catch (OpenQA.Selenium.WebDriverException ex)
            {
                return CreateErrorResult(ex, "Ожидание условия");
            }
            catch (InvalidOperationException ex)
            {
                return CreateErrorResult(ex, "Ожидание условия");
            }
            catch (Exception ex)
            {
                return CreateErrorResult(ex, "Ожидание условия");
            }
        }

        // ── Приватные методы ───────────────────────────────────────────

        /// <summary>
        /// Выполняет ожидание условия в зависимости от типа.
        /// </summary>
        private bool WaitForCondition(IWebDriver driver, int timeout, ScriptingData sd)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));

            try
            {
                switch (Prop_Condition)
                {
                    case WaitConditionType.ElementExists:
                        return WaitForElementExists(driver, wait);

                    case WaitConditionType.ElementVisible:
                        return WaitForElementVisible(driver, wait);

                    case WaitConditionType.ElementClickable:
                        return WaitForElementClickable(driver, wait);

                    case WaitConditionType.ElementInvisible:
                        return WaitForElementInvisible(driver, wait);

                    case WaitConditionType.TextPresent:
                        return WaitForTextPresent(driver, wait, sd);

                    case WaitConditionType.TitleContains:
                        return WaitForTitleContains(driver, wait, sd);

                    case WaitConditionType.UrlContains:
                        return WaitForUrlContains(driver, wait, sd);

                    case WaitConditionType.AlertPresent:
                        return WaitForAlertPresent(wait);

                    default:
                        throw new NotSupportedException($"Условие {Prop_Condition} не поддерживается");
                }
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        private bool WaitForElementExists(IWebDriver driver, WebDriverWait wait)
        {
            string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), null);
            var locator = SeleniumHelper.CreateLocator(Prop_LocatorType, locatorValue);
            wait.Until(drv => drv.FindElement(locator));
            return true;
        }

        private bool WaitForElementVisible(IWebDriver driver, WebDriverWait wait)
        {
            string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), null);
            var locator = SeleniumHelper.CreateLocator(Prop_LocatorType, locatorValue);
            wait.Until(drv =>
            {
                try
                {
                    var element = drv.FindElement(locator);
                    return element.Displayed;
                }
                catch { return false; }
            });
            return true;
        }

        private bool WaitForElementClickable(IWebDriver driver, WebDriverWait wait)
        {
            string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), null);
            var locator = SeleniumHelper.CreateLocator(Prop_LocatorType, locatorValue);
            wait.Until(drv =>
            {
                try
                {
                    var element = drv.FindElement(locator);
                    return element.Displayed && element.Enabled;
                }
                catch { return false; }
            });
            return true;
        }

        private bool WaitForElementInvisible(IWebDriver driver, WebDriverWait wait)
        {
            string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), null);
            var locator = SeleniumHelper.CreateLocator(Prop_LocatorType, locatorValue);
            wait.Until(drv =>
            {
                try
                {
                    var element = drv.FindElement(locator);
                    return !element.Displayed;
                }
                catch (NoSuchElementException)
                {
                    return true; // Элемент не существует = невидим
                }
            });
            return true;
        }

        private bool WaitForTextPresent(IWebDriver driver, WebDriverWait wait, ScriptingData sd)
        {
            string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), sd);
            string text = GetPropertyValue<string>(Prop_Text, nameof(Prop_Text), sd);
            var locator = SeleniumHelper.CreateLocator(Prop_LocatorType, locatorValue);

            wait.Until(drv =>
            {
                try
                {
                    var element = drv.FindElement(locator);
                    return element.Text.Contains(text);
                }
                catch { return false; }
            });
            return true;
        }

        private bool WaitForTitleContains(IWebDriver driver, WebDriverWait wait, ScriptingData sd)
        {
            string text = GetPropertyValue<string>(Prop_Text, nameof(Prop_Text), sd);
            wait.Until(drv => drv.Title.Contains(text));
            return true;
        }

        private bool WaitForUrlContains(IWebDriver driver, WebDriverWait wait, ScriptingData sd)
        {
            string text = GetPropertyValue<string>(Prop_Text, nameof(Prop_Text), sd);
            wait.Until(drv => drv.Url.Contains(text));
            return true;
        }

        private bool WaitForAlertPresent(WebDriverWait wait)
        {
            wait.Until(drv =>
            {
                try
                {
                    drv.SwitchTo().Alert();
                    return true;
                }
                catch (NoAlertPresentException)
                {
                    return false;
                }
            });
            return true;
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            ret.ValidateRequired(Prop_LocatorValue, ActivityStrings.Field_LocatorValue, "Значение локатора обязательно");
            return ret;
        }
    }
}
