// =============================================================================
// ElementSendKeysBack.cs — активность «Нажать клавишу».
//
// Отправляет специальные клавиши в элемент (Enter, Tab, Escape и т.д.).
// Поддерживает функциональные клавиши, стрелки, комбинации Ctrl/Shift/Alt.
//
// Используется для:
//   - Подтверждения ввода (Enter)
//   - Навигации между полями (Tab)
//   - Управления элементами (стрелки)
//   - Отправки горячих клавиш
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
    /// Активность для отправки специальных клавиш в элемент.
    /// </summary>
    public class ElementSendKeysBack : PrimoComponentTO<ElementSendKeys>
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
        /// <summary>ID элемента (если элемент уже найден).</summary>
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

        private SpecialKeyType _propSpecialKey;
        /// <summary>Специальная клавиша для отправки.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_SpecialKey)]
        public SpecialKeyType Prop_SpecialKey
        {
            get => _propSpecialKey;
            set { _propSpecialKey = value; InvokePropertyChanged(this, "Prop_SpecialKey"); }
        }

        private string _propRepeatCount;
        /// <summary>Количество повторений нажатия.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_RepeatCount)]
        public string Prop_RepeatCount
        {
            get => _propRepeatCount;
            set { _propRepeatCount = value; InvokePropertyChanged(this, "Prop_RepeatCount"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementSendKeysBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ElementSendKeys;
            sdkComponentHelp =
                "Отправляет специальные клавиши в элемент.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "ID элемента — идентификатор элемента (если уже найден)\n" +
                "Специальная клавиша — клавиша для отправки\n" +
                "Количество повторений — сколько раз нажать клавишу\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение локатора — конкретное значение для поиска\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Таймаут (сек) — время ожидания появления элемента\n" +
                "\n" +
                "ПОДДЕРЖИВАЕМЫЕ КЛАВИШИ:\n" +
                "• Enter, Tab, Escape, Space\n" +
                "• Backspace, Delete\n" +
                "• Стрелки (Up, Down, Left, Right)\n" +
                "• Home, End, PageUp, PageDown\n" +
                "• F1-F12\n" +
                "• Комбинации: Ctrl+A/C/V/X/Z, Shift+Tab, Alt+F4";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.Enum<SpecialKeyType>("Prop_SpecialKey", "Специальная клавиша"),
                PropertyBuilder.Int("Prop_RepeatCount", "Количество повторений")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_WaitTimeout = "10";
            this.Prop_SpecialKey = SpecialKeyType.Enter;
            this.Prop_RepeatCount = "1";
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

                // Получение количества повторений
                string repeatStr = GetPropertyValue<string>(this.Prop_RepeatCount, "Prop_RepeatCount", sd) ?? "1";
                int repeatCount = int.TryParse(repeatStr, out int r) ? r : 1;
                repeatCount = Math.Max(1, Math.Min(repeatCount, 100)); // 1-100

                // Преобразование специальной клавиши в Selenium Keys
                string seleniumKey = ConvertToSeleniumKey(this.Prop_SpecialKey);

                // Отправка клавиши
                for (int i = 0; i < repeatCount; i++)
                {
                    element.SendKeys(seleniumKey);
                }

                string resultMsg = !string.IsNullOrWhiteSpace(locatorValue)
                    ? $"[Нажать клавишу] Отправлено: {this.Prop_SpecialKey} x{repeatCount} → {this.Prop_LocatorType}={locatorValue}"
                    : $"[Нажать клавишу] Отправлено: {this.Prop_SpecialKey} x{repeatCount} → {elementId}";

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
                    ErrorMessage = $"Ошибка [Нажать клавишу]: {ex.Message}"
                };
            }
        }

        // ── Приватные методы ───────────────────────────────────────────

        /// <summary>
        /// Преобразует SpecialKeyType в строку Selenium Keys.
        /// </summary>
        private string ConvertToSeleniumKey(SpecialKeyType keyType)
        {
            switch (keyType)
            {
                case SpecialKeyType.Enter:
                    return Keys.Enter;
                case SpecialKeyType.Tab:
                    return Keys.Tab;
                case SpecialKeyType.Escape:
                    return Keys.Escape;
                case SpecialKeyType.Backspace:
                    return Keys.Backspace;
                case SpecialKeyType.Delete:
                    return Keys.Delete;
                case SpecialKeyType.Space:
                    return Keys.Space;
                case SpecialKeyType.ArrowUp:
                    return Keys.ArrowUp;
                case SpecialKeyType.ArrowDown:
                    return Keys.ArrowDown;
                case SpecialKeyType.ArrowLeft:
                    return Keys.ArrowLeft;
                case SpecialKeyType.ArrowRight:
                    return Keys.ArrowRight;
                case SpecialKeyType.Home:
                    return Keys.Home;
                case SpecialKeyType.End:
                    return Keys.End;
                case SpecialKeyType.PageUp:
                    return Keys.PageUp;
                case SpecialKeyType.PageDown:
                    return Keys.PageDown;
                case SpecialKeyType.F1:
                    return Keys.F1;
                case SpecialKeyType.F2:
                    return Keys.F2;
                case SpecialKeyType.F3:
                    return Keys.F3;
                case SpecialKeyType.F4:
                    return Keys.F4;
                case SpecialKeyType.F5:
                    return Keys.F5;
                case SpecialKeyType.F6:
                    return Keys.F6;
                case SpecialKeyType.F7:
                    return Keys.F7;
                case SpecialKeyType.F8:
                    return Keys.F8;
                case SpecialKeyType.F9:
                    return Keys.F9;
                case SpecialKeyType.F10:
                    return Keys.F10;
                case SpecialKeyType.F11:
                    return Keys.F11;
                case SpecialKeyType.F12:
                    return Keys.F12;
                case SpecialKeyType.CtrlA:
                    return Keys.Control + "a";
                case SpecialKeyType.CtrlC:
                    return Keys.Control + "c";
                case SpecialKeyType.CtrlV:
                    return Keys.Control + "v";
                case SpecialKeyType.CtrlX:
                    return Keys.Control + "x";
                case SpecialKeyType.CtrlZ:
                    return Keys.Control + "z";
                case SpecialKeyType.ShiftTab:
                    return Keys.Shift + Keys.Tab;
                case SpecialKeyType.AltF4:
                    return Keys.Alt + Keys.F4;
                default:
                    return Keys.Enter;
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_SessionId, ActivityStrings.Field_SessionId, "ID сессии обязателен");

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
