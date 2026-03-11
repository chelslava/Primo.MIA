// =============================================================================
// ElementInputBack.cs — объединённая активность «Ввод в элемент».
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
    /// Объединённая активность для работы с вводом в элементы.
    /// Режим работы задаётся через свойство <see cref="Prop_InputMode"/>.
    /// </summary>
    public class ElementInputBack : BrowserActivityBase<ElementInput>
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

        private InputMode _propInputMode;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName("Режим ввода")]
        public InputMode Prop_InputMode
        {
            get => _propInputMode;
            set { _propInputMode = value; InvokePropertyChanged(this, nameof(Prop_InputMode)); }
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

        // TypeText properties
        private string _propText;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_Text)]
        public string Prop_Text
        {
            get => _propText;
            set { _propText = value; InvokePropertyChanged(this, nameof(Prop_Text)); }
        }

        private bool _propClearBeforeType;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName("Очистить перед вводом")]
        public bool Prop_ClearBeforeType
        {
            get => _propClearBeforeType;
            set { _propClearBeforeType = value; InvokePropertyChanged(this, nameof(Prop_ClearBeforeType)); }
        }

        private bool _propSimulateTyping;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName("Симулировать печать")]
        public bool Prop_SimulateTyping
        {
            get => _propSimulateTyping;
            set { _propSimulateTyping = value; InvokePropertyChanged(this, nameof(Prop_SimulateTyping)); }
        }

        private string _propTypeDelay;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName("Задержка между символами (мс)")]
        public string Prop_TypeDelay
        {
            get => _propTypeDelay;
            set { _propTypeDelay = value; InvokePropertyChanged(this, nameof(Prop_TypeDelay)); }
        }

        private bool _propClickBeforeType;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_ClickBeforeType)]
        public bool Prop_ClickBeforeType
        {
            get => _propClickBeforeType;
            set { _propClickBeforeType = value; InvokePropertyChanged(this, nameof(Prop_ClickBeforeType)); }
        }

        private bool _propPressEnterAfter;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_PressEnterAfter)]
        public bool Prop_PressEnterAfter
        {
            get => _propPressEnterAfter;
            set { _propPressEnterAfter = value; InvokePropertyChanged(this, nameof(Prop_PressEnterAfter)); }
        }

        // SendKeys properties
        private SpecialKeyType _propSpecialKey;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_SpecialKey)]
        public SpecialKeyType Prop_SpecialKey
        {
            get => _propSpecialKey;
            set { _propSpecialKey = value; InvokePropertyChanged(this, nameof(Prop_SpecialKey)); }
        }

        private string _propRepeatCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_RepeatCount)]
        public string Prop_RepeatCount
        {
            get => _propRepeatCount;
            set { _propRepeatCount = value; InvokePropertyChanged(this, nameof(Prop_RepeatCount)); }
        }

        #endregion

        #region Constructor

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
                PropertyBuilder.String("Prop_Text", "Текст для ввода (TypeText)"),
                PropertyBuilder.BooleanObject("Prop_ClearBeforeType", "Очистить перед вводом (TypeText)"),
                PropertyBuilder.BooleanObject("Prop_ClickBeforeType", "Кликнуть перед вводом (TypeText)"),
                PropertyBuilder.BooleanObject("Prop_SimulateTyping", "Симулировать печать (TypeText)"),
                PropertyBuilder.Int("Prop_TypeDelay", "Задержка между символами мс (TypeText)"),
                PropertyBuilder.BooleanObject("Prop_PressEnterAfter", "Нажать Enter после (TypeText)"),
                PropertyBuilder.Enum<SpecialKeyType>("Prop_SpecialKey", "Специальная клавиша (SendKeys)"),
                PropertyBuilder.Int("Prop_RepeatCount", "Количество повторений (SendKeys)")
            };

            InitClass(container);

            // Defaults
            Prop_InputMode = InputMode.TypeText;
            Prop_SessionId = "\"\"";
            Prop_ElementId = "\"\"";
            Prop_LocatorType = ElementLocatorType.Id;
            Prop_LocatorValue = "\"\"";
            Prop_WaitTimeout = "10";
            Prop_Text = "\"\"";
            Prop_ClearBeforeType = true;
            Prop_ClickBeforeType = false;
            Prop_SimulateTyping = false;
            Prop_TypeDelay = "50";
            Prop_PressEnterAfter = false;
            Prop_SpecialKey = SpecialKeyType.Enter;
            Prop_RepeatCount = "1";
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

                // Выполнение действия в зависимости от режима
                string resultMsg = PerformInputAction(sd, element);

                return CreateSuccessResult(resultMsg);
            },
            $"Ввод в элемент — {Prop_InputMode}");
        }

        #endregion

        #region Element Resolution

        /// <summary>
        /// Разрешает IWebElement: сначала по локатору (если задан), затем по ID.
        /// Ожидает кликабельности элемента для ввода текста.
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
                var locatorType = ConvertLocatorType(Prop_LocatorType);
                
                // Ожидаем кликабельности элемента для ввода
                return ElementLocator.WaitForClickable(driver, locatorType, locatorValue, timeout);
            }

            var element = SeleniumHelper.GetElement(elementId);
            
            // Проверяем кликабельность элемента из репозитория
            if (element != null && (!element.Displayed || !element.Enabled))
            {
                throw new ElementNotInteractableException(
                    $"Элемент с ID '{elementId}' не кликабельный (Displayed: {element.Displayed}, Enabled: {element.Enabled})");
            }
            
            return element;
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

        #region Input Actions

        /// <summary>
        /// Выполняет действие ввода в соответствии с выбранным режимом.
        /// </summary>
        private string PerformInputAction(ScriptingData sd, IWebElement element)
        {
            switch (Prop_InputMode)
            {
                case InputMode.TypeText:
                    return PerformTypeText(sd, element);

                case InputMode.SendKeys:
                    return PerformSendKeys(sd, element);

                case InputMode.Clear:
                    return PerformClear(element);

                default:
                    throw new ArgumentOutOfRangeException(nameof(Prop_InputMode),
                        $"Неизвестный режим ввода: {Prop_InputMode}");
            }
        }

        #endregion

        #region TypeText Mode

        /// <summary>
        /// Выполняет ввод текста в элемент.
        /// </summary>
        private string PerformTypeText(ScriptingData sd, IWebElement element)
        {
            string text = GetPropertyValue<string>(Prop_Text, nameof(Prop_Text), sd) ?? string.Empty;

            // Клик перед вводом
            if (Prop_ClickBeforeType)
                element.Click();

            // Очистка перед вводом
            if (Prop_ClearBeforeType)
                element.Clear();

            // Ввод текста
            if (Prop_SimulateTyping)
                TypeTextWithDelay(element, text, sd);
            else
                element.SendKeys(text);

            // Нажатие Enter после ввода
            if (Prop_PressEnterAfter)
                element.SendKeys(Keys.Enter);

            return $"[Ввести текст] Текст введён: {GetElementLabel()}";
        }

        /// <summary>
        /// Вводит текст посимвольно с задержкой.
        /// </summary>
        private void TypeTextWithDelay(IWebElement element, string text, ScriptingData sd)
        {
            int delay = ParseTypeDelay(sd);

            foreach (char c in text)
            {
                element.SendKeys(c.ToString());
                if (delay > 0)
                    System.Threading.Thread.Sleep(delay);
            }
        }

        /// <summary>
        /// Парсит и валидирует задержку между символами.
        /// </summary>
        private int ParseTypeDelay(ScriptingData sd)
        {
            string delayStr = GetPropertyValue<string>(Prop_TypeDelay, nameof(Prop_TypeDelay), sd) ?? "50";
            int delay = int.TryParse(delayStr, out int d) ? d : 50;
            return Math.Max(1, Math.Min(delay, 1000)); // 1-1000 мс
        }

        #endregion

        #region SendKeys Mode

        /// <summary>
        /// Выполняет отправку специальных клавиш в элемент.
        /// </summary>
        private string PerformSendKeys(ScriptingData sd, IWebElement element)
        {
            int repeatCount = ParseRepeatCount(sd);
            string seleniumKey = ConvertToSeleniumKey(Prop_SpecialKey);

            for (int i = 0; i < repeatCount; i++)
            {
                element.SendKeys(seleniumKey);
            }

            return $"[Отправить клавиши] Клавиша '{Prop_SpecialKey}' отправлена {repeatCount} раз: {GetElementLabel()}";
        }

        /// <summary>
        /// Парсит и валидирует количество повторений.
        /// </summary>
        private int ParseRepeatCount(ScriptingData sd)
        {
            string repeatStr = GetPropertyValue<string>(Prop_RepeatCount, nameof(Prop_RepeatCount), sd) ?? "1";
            int repeatCount = int.TryParse(repeatStr, out int r) ? r : 1;
            return Math.Max(1, Math.Min(repeatCount, 100)); // 1-100
        }

        /// <summary>
        /// Преобразует SpecialKeyType в Selenium Keys.
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
                case SpecialKeyType.ArrowUp: return Keys.ArrowUp;
                case SpecialKeyType.ArrowDown: return Keys.ArrowDown;
                case SpecialKeyType.ArrowLeft: return Keys.ArrowLeft;
                case SpecialKeyType.ArrowRight: return Keys.ArrowRight;
                case SpecialKeyType.Home: return Keys.Home;
                case SpecialKeyType.End: return Keys.End;
                case SpecialKeyType.PageUp: return Keys.PageUp;
                case SpecialKeyType.PageDown: return Keys.PageDown;
                case SpecialKeyType.Space: return Keys.Space;
                default:
                    throw new ArgumentException($"Неподдерживаемая клавиша: {keyType}");
            }
        }

        #endregion

        #region Clear Mode

        /// <summary>
        /// Очищает поле ввода.
        /// </summary>
        private string PerformClear(IWebElement element)
        {
            element.Clear();
            return $"[Очистить поле] Поле очищено: {GetElementLabel()}";
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

            // Валидация для режима TypeText
            if (Prop_InputMode == InputMode.TypeText && string.IsNullOrWhiteSpace(Prop_Text))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_Text),
                    Error = "Текст для ввода не может быть пустым в режиме TypeText"
                });
            }

            return ret;
        }

        #endregion
    }
}
