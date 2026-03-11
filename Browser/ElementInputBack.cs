// =============================================================================
// ElementInputBack.cs — объединённая активность «Ввод в элемент».
//
// Объединяет три режима работы с полями ввода:
//   - TypeText  — ввести текст (очистить поле и ввести)
//   - SendKeys  — отправить клавиши (включая специальные)
//   - Clear     — очистить поле ввода
//
// Режим выбирается через enum InputMode.
// Поддерживает поиск элемента по ID или по локатору.
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
    /// Объединённая активность для работы с вводом в элементы.
    /// Режим работы задаётся через свойство <see cref="Prop_InputMode"/>.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class ElementInputBack : BrowserActivityBase<ElementInput>
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

        #region Prop_InputMode

        private InputMode _propInputMode;

        /// <summary>
        /// Режим ввода: TypeText, SendKeys или Clear.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Режим ввода")]
        public InputMode Prop_InputMode
        {
            get => _propInputMode;
            set { _propInputMode = value; InvokePropertyChanged(this, nameof(Prop_InputMode)); }
        }

        #endregion

        #region Prop_SessionId

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

        #endregion

        #region Prop_ElementId

        private string _propElementId;

        /// <summary>ID элемента (если элемент уже найден).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ElementId)]
        public string Prop_ElementId
        {
            get => _propElementId;
            set { _propElementId = value; InvokePropertyChanged(this, nameof(Prop_ElementId)); }
        }

        #endregion

        #region Prop_LocatorType

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

        #endregion

        #region Prop_LocatorValue

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

        #endregion

        #region Prop_WaitTimeout

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

        #endregion

        // ── Параметры для TypeText ────────────────────────────────────

        #region Prop_Text

        private string _propText;

        /// <summary>Текст для ввода (только для режима TypeText).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_Text)]
        public string Prop_Text
        {
            get => _propText;
            set { _propText = value; InvokePropertyChanged(this, nameof(Prop_Text)); }
        }

        #endregion

        #region Prop_ClearBeforeType

        private bool _propClearBeforeType;

        /// <summary>Очистить поле перед вводом (только для TypeText).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Очистить перед вводом")]
        public bool Prop_ClearBeforeType
        {
            get => _propClearBeforeType;
            set { _propClearBeforeType = value; InvokePropertyChanged(this, nameof(Prop_ClearBeforeType)); }
        }

        #endregion

        #region Prop_SimulateTyping

        private bool _propSimulateTyping;

        /// <summary>Симулировать печать с задержкой (только для TypeText).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Симулировать печать")]
        public bool Prop_SimulateTyping
        {
            get => _propSimulateTyping;
            set { _propSimulateTyping = value; InvokePropertyChanged(this, nameof(Prop_SimulateTyping)); }
        }

        #endregion

        #region Prop_TypeDelay

        private string _propTypeDelay;

        /// <summary>Задержка между символами в мс (только для TypeText).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Задержка между символами (мс)")]
        public string Prop_TypeDelay
        {
            get => _propTypeDelay;
            set { _propTypeDelay = value; InvokePropertyChanged(this, nameof(Prop_TypeDelay)); }
        }

        #endregion

        #region Prop_ClickBeforeType

        private bool _propClickBeforeType;

        /// <summary>Кликнуть по элементу перед вводом (только для TypeText).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ClickBeforeType)]
        public bool Prop_ClickBeforeType
        {
            get => _propClickBeforeType;
            set { _propClickBeforeType = value; InvokePropertyChanged(this, nameof(Prop_ClickBeforeType)); }
        }

        #endregion

        #region Prop_PressEnterAfter

        private bool _propPressEnterAfter;

        /// <summary>Нажать Enter после ввода (только для TypeText).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_PressEnterAfter)]
        public bool Prop_PressEnterAfter
        {
            get => _propPressEnterAfter;
            set { _propPressEnterAfter = value; InvokePropertyChanged(this, nameof(Prop_PressEnterAfter)); }
        }

        #endregion

        // ── Параметры для SendKeys ─────────────────────────────────────

        #region Prop_SpecialKey

        private SpecialKeyType _propSpecialKey;

        /// <summary>Специальная клавиша для отправки (только для SendKeys).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_SpecialKey)]
        public SpecialKeyType Prop_SpecialKey
        {
            get => _propSpecialKey;
            set { _propSpecialKey = value; InvokePropertyChanged(this, nameof(Prop_SpecialKey)); }
        }

        #endregion

        #region Prop_RepeatCount

        private string _propRepeatCount;

        /// <summary>Количество повторений нажатия (только для SendKeys).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_RepeatCount)]
        public string Prop_RepeatCount
        {
            get => _propRepeatCount;
            set { _propRepeatCount = value; InvokePropertyChanged(this, nameof(Prop_RepeatCount)); }
        }

        #endregion

        // ── Конструктор ────────────────────────────────────────────────

        public ElementInputBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Элемент: Ввод";
            sdkComponentHelp =
                "Объединённая активность для работы с вводом в элементы.\n" +
                "\n" +
                "── Режимы ввода ───────────────────────────────\n" +
                "TypeText  — ввести текст (очистить и ввести)\n" +
                "SendKeys  — отправить специальные клавиши\n" +
                "Clear     — очистить поле ввода\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии   — идентификатор сессии браузера\n" +
                "ID элемента — идентификатор элемента (если уже найден)\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение локатора — конкретное значение для поиска\n" +
                "\n" +
                "── Параметры TypeText ────────────────────────\n" +
                "Текст — текст для ввода\n" +
                "Очистить перед вводом — очистить поле перед вводом\n" +
                "Кликнуть перед вводом — кликнуть по элементу\n" +
                "Симулировать печать — вводить с задержкой\n" +
                "Задержка (мс) — задержка между символами\n" +
                "Нажать Enter после — нажать Enter после ввода\n" +
                "\n" +
                "── Параметры SendKeys ────────────────────────\n" +
                "Специальная клавиша — клавиша для отправки\n" +
                "Количество повторений — сколько раз нажать\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Если указан локатор, элемент будет найден автоматически.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<InputMode>("Prop_InputMode", "Режим ввода"),
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)"),
                // TypeText
                PropertyBuilder.String("Prop_Text", "Текст для ввода (TypeText)"),
                PropertyBuilder.BooleanObject("Prop_ClearBeforeType", "Очистить перед вводом (TypeText)"),
                PropertyBuilder.BooleanObject("Prop_ClickBeforeType", "Кликнуть перед вводом (TypeText)"),
                PropertyBuilder.BooleanObject("Prop_SimulateTyping", "Симулировать печать (TypeText)"),
                PropertyBuilder.Int("Prop_TypeDelay", "Задержка между символами мс (TypeText)"),
                PropertyBuilder.BooleanObject("Prop_PressEnterAfter", "Нажать Enter после (TypeText)"),
                // SendKeys
                PropertyBuilder.Enum<SpecialKeyType>("Prop_SpecialKey", "Специальная клавиша (SendKeys)"),
                PropertyBuilder.Int("Prop_RepeatCount", "Количество повторений (SendKeys)")
            };

            InitClass(container);

            this.Prop_InputMode = InputMode.TypeText;
            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_WaitTimeout = "10";
            // TypeText defaults
            this.Prop_Text = "\"\"";
            this.Prop_ClearBeforeType = true;
            this.Prop_ClickBeforeType = false;
            this.Prop_SimulateTyping = false;
            this.Prop_TypeDelay = "50";
            this.Prop_PressEnterAfter = false;
            // SendKeys defaults
            this.Prop_SpecialKey = SpecialKeyType.Enter;
            this.Prop_RepeatCount = "1";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return SafeExecute(() =>
            {
                // Чтение общих параметров
                string sessionId = GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd);
                string elementId = GetPropertyValue<string>(this.Prop_ElementId, nameof(Prop_ElementId), sd);
                string locatorValue = GetPropertyValue<string>(this.Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

                Logger.LogInfo(sdkComponentName, "Начинается ввод в элемент для сессии: {0}", sessionId);

                // Получение драйвера
                var driver = GetDriverFromContext(sessionId);

                // Получение элемента: либо по ID, либо поиск по локатору
                IWebElement element;
                if (!string.IsNullOrWhiteSpace(locatorValue))
                {
                    string timeoutStr = GetPropertyValue<string>(this.Prop_WaitTimeout, nameof(Prop_WaitTimeout), sd) ?? "10";
                    int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                    ValidatePositive(timeout, "Prop_WaitTimeout");

                    var locatorType = ConvertLocatorType(this.Prop_LocatorType);
                    element = ElementLocator.FindElement(driver, locatorType, locatorValue, timeout);
                    
                    Logger.LogDebug(sdkComponentName, "Элемент найден по локатору: {0}={1}", this.Prop_LocatorType, locatorValue);
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

                // Выполнение действия в зависимости от режима
                string resultMsg;
                Logger.LogDebug(sdkComponentName, "Режим ввода: {0}", this.Prop_InputMode);

                switch (this.Prop_InputMode)
                {
                    case InputMode.TypeText:
                        resultMsg = ExecuteTypeText(element, elementId, locatorValue, sd);
                        break;

                    case InputMode.SendKeys:
                        resultMsg = ExecuteSendKeys(element, elementId, locatorValue, sd);
                        break;

                    case InputMode.Clear:
                        element.Clear();
                        resultMsg = !string.IsNullOrWhiteSpace(locatorValue)
                            ? $"[Очистить поле] Поле очищено: {this.Prop_LocatorType}={locatorValue}"
                            : $"[Очистить поле] Поле очищено: {elementId}";
                        Logger.LogDebug(sdkComponentName, "Поле очищено");
                        break;

                    default:
                        throw new ArgumentException($"Неизвестный режим ввода: {this.Prop_InputMode}");
                }

                Logger.LogInfo(sdkComponentName, "Ввод в элемент завершен успешно");
                return CreateSuccessResult(resultMsg);
            }, "Ввод в элемент");
        }

        // ── Приватные методы ───────────────────────────────────────────

        /// <summary>
        /// Выполняет ввод текста в элемент.
        /// </summary>
        private string ExecuteTypeText(IWebElement element, string elementId, string locatorValue, ScriptingData sd)
        {
            string text = GetPropertyValue<string>(this.Prop_Text, nameof(Prop_Text), sd) ?? string.Empty;

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
                string delayStr = GetPropertyValue<string>(this.Prop_TypeDelay, nameof(Prop_TypeDelay), sd) ?? "50";
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

            return !string.IsNullOrWhiteSpace(locatorValue)
                ? $"[Ввести текст] Текст введён: {this.Prop_LocatorType}={locatorValue}"
                : $"[Ввести текст] Текст введён в элемент {elementId}";
        }

        /// <summary>
        /// Выполняет отправку специальных клавиш в элемент.
        /// </summary>
        private string ExecuteSendKeys(IWebElement element, string elementId, string locatorValue, ScriptingData sd)
        {
            // Получение количества повторений
            string repeatStr = GetPropertyValue<string>(this.Prop_RepeatCount, nameof(Prop_RepeatCount), sd) ?? "1";
            int repeatCount = int.TryParse(repeatStr, out int r) ? r : 1;
            repeatCount = Math.Max(1, Math.Min(repeatCount, 100)); // 1-100

            // Преобразование специальной клавиши в Selenium Keys
            string seleniumKey = ConvertToSeleniumKey(this.Prop_SpecialKey);

            // Отправка клавиши
            for (int i = 0; i < repeatCount; i++)
            {
                element.SendKeys(seleniumKey);
            }

            return !string.IsNullOrWhiteSpace(locatorValue)
                ? $"[Нажать клавишу] Отправлено: {this.Prop_SpecialKey} x{repeatCount} → {this.Prop_LocatorType}={locatorValue}"
                : $"[Нажать клавишу] Отправлено: {this.Prop_SpecialKey} x{repeatCount} → {elementId}";
        }

        /// <summary>
        /// Преобразует SpecialKeyType в строку Selenium Keys.
        /// </summary>
        private string ConvertToSeleniumKey(SpecialKeyType keyType)
        {
            switch (keyType)
            {
                case SpecialKeyType.Enter: return Keys.Enter;
                case SpecialKeyType.Tab: return Keys.Tab;
                case SpecialKeyType.Escape: return Keys.Escape;
                case SpecialKeyType.Backspace: return Keys.Backspace;
                case SpecialKeyType.Delete: return Keys.Delete;
                case SpecialKeyType.Space: return Keys.Space;
                case SpecialKeyType.ArrowUp: return Keys.ArrowUp;
                case SpecialKeyType.ArrowDown: return Keys.ArrowDown;
                case SpecialKeyType.ArrowLeft: return Keys.ArrowLeft;
                case SpecialKeyType.ArrowRight: return Keys.ArrowRight;
                case SpecialKeyType.Home: return Keys.Home;
                case SpecialKeyType.End: return Keys.End;
                case SpecialKeyType.PageUp: return Keys.PageUp;
                case SpecialKeyType.PageDown: return Keys.PageDown;
                case SpecialKeyType.F1: return Keys.F1;
                case SpecialKeyType.F2: return Keys.F2;
                case SpecialKeyType.F3: return Keys.F3;
                case SpecialKeyType.F4: return Keys.F4;
                case SpecialKeyType.F5: return Keys.F5;
                case SpecialKeyType.F6: return Keys.F6;
                case SpecialKeyType.F7: return Keys.F7;
                case SpecialKeyType.F8: return Keys.F8;
                case SpecialKeyType.F9: return Keys.F9;
                case SpecialKeyType.F10: return Keys.F10;
                case SpecialKeyType.F11: return Keys.F11;
                case SpecialKeyType.F12: return Keys.F12;
                case SpecialKeyType.CtrlA: return Keys.Control + "a";
                case SpecialKeyType.CtrlC: return Keys.Control + "c";
                case SpecialKeyType.CtrlV: return Keys.Control + "v";
                case SpecialKeyType.CtrlX: return Keys.Control + "x";
                case SpecialKeyType.CtrlZ: return Keys.Control + "z";
                case SpecialKeyType.ShiftTab: return Keys.Shift + Keys.Tab;
                case SpecialKeyType.AltF4: return Keys.Alt + Keys.F4;
                default: return Keys.Enter;
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

            // Для режима TypeText проверяем наличие текста
            if (this.Prop_InputMode == InputMode.TypeText)
            {
                ret.ValidateRequired(this.Prop_Text, ActivityStrings.Field_Text, "Текст для ввода обязателен");
            }

            return ret;
        }
    }
}
