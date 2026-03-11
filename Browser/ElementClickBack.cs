// =============================================================================
// ElementClickUnifiedBack.cs — объединённая активность «Клик по элементу».
//
// Объединяет четыре режима клика в одной активности:
//   - Click          — обычный клик (с опциональным JS и паузой после)
//   - DoubleClick    — двойной клик (с опциональным JS и паузой после)
//   - RightClick     — правый клик / контекстное меню (с паузой после)
//   - ClickAndHold   — клик с удержанием (с указанием длительности)
//
// Режим выбирается через enum ClickMode.
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
    // ── Основной класс активности ──────────────────────────────────────────────

    /// <summary>
    /// Объединённая активность для выполнения различных типов кликов по элементу.
    /// Режим клика задаётся через свойство <see cref="Prop_ClickMode"/>.
    /// </summary>
    public class ElementClickBack : PrimoComponentTO<ElementClick>
    {
        // ── Группа и таймаут ──────────────────────────────────────────────────

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

        // ── Входные параметры ──────────────────────────────────────────────────

        #region Prop_ClickMode

        private ClickMode _propClickMode;

        /// <summary>
        /// Режим клика: Click, DoubleClick, RightClick или ClickAndHold.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Режим клика")]
        public ClickMode Prop_ClickMode
        {
            get => _propClickMode;
            set { _propClickMode = value; InvokePropertyChanged(this, nameof(Prop_ClickMode)); }
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

        /// <summary>ID элемента (если элемент уже найден ранее).</summary>
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

        #region Локатор

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

        #region Prop_UseJavaScript (только Click и DoubleClick)

        private bool _propUseJavaScript;

        /// <summary>
        /// Использовать JavaScript для клика.
        /// Применяется только в режимах Click и DoubleClick.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Использовать JavaScript")]
        public bool Prop_UseJavaScript
        {
            get => _propUseJavaScript;
            set { _propUseJavaScript = value; InvokePropertyChanged(this, nameof(Prop_UseJavaScript)); }
        }

        #endregion

        #region Prop_WaitAfterClick (не для ClickAndHold)

        private string _propWaitAfterClick;

        /// <summary>
        /// Ожидание после клика (мс).
        /// Применяется в режимах Click, DoubleClick, RightClick.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_WaitAfterClick)]
        public string Prop_WaitAfterClick
        {
            get => _propWaitAfterClick;
            set { _propWaitAfterClick = value; InvokePropertyChanged(this, nameof(Prop_WaitAfterClick)); }
        }

        #endregion

        #region Prop_HoldDuration (только ClickAndHold)

        private string _propHoldDuration;

        /// <summary>
        /// Длительность удержания кнопки мыши (мс).
        /// Применяется только в режиме ClickAndHold.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Длительность удержания (мс)")]
        public string Prop_HoldDuration
        {
            get => _propHoldDuration;
            set { _propHoldDuration = value; InvokePropertyChanged(this, nameof(Prop_HoldDuration)); }
        }

        #endregion

        // ── Конструктор ────────────────────────────────────────────────────────

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

            // Определяем список свойств через LINQ для удобного управления составом
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

            // Установка значений по умолчанию
            this.Prop_ClickMode = ClickMode.Click;
            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_WaitTimeout = "10";
            this.Prop_UseJavaScript = false;
            this.Prop_WaitAfterClick = "0";
            this.Prop_HoldDuration = "1000";
        }

        // ── TimedAction — точка входа ──────────────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Чтение обязательных параметров через SessionResolver (поддержка ambient-контекста)
                string sessionId = SessionResolver.Resolve(
                    GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
                string elementId = GetPropertyValue<string>(this.Prop_ElementId, nameof(Prop_ElementId), sd);
                string locatorValue = GetPropertyValue<string>(this.Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

                // Получение WebDriver по ID сессии
                var driver = SeleniumHelper.GetDriver(sessionId);

                // ── Поиск элемента ─────────────────────────────────────────────
                IWebElement element = ResolveElement(sd, driver, elementId, locatorValue);

                // ── Выполнение нужного типа клика ──────────────────────────────
                string resultMsg = ExecuteClick(sd, driver, element, elementId, locatorValue);

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
                    ErrorMessage = $"Ошибка [Клик — {this.Prop_ClickMode}]: {ex.Message}"
                };
            }
        }

        // ── Вспомогательные методы ─────────────────────────────────────────────

        /// <summary>
        /// Разрешает IWebElement: сначала по локатору (если задан), затем по ID.
        /// Выбрасывает исключение, если ни один идентификатор не указан.
        /// </summary>
        private IWebElement ResolveElement(ScriptingData sd, IWebDriver driver, string elementId, string locatorValue)
        {
            if (!string.IsNullOrWhiteSpace(locatorValue))
            {
                // Парсим таймаут с защитой от некорректных значений
                string timeoutStr = GetPropertyValue<string>(this.Prop_WaitTimeout, nameof(Prop_WaitTimeout), sd) ?? "10";
                int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                timeout = SeleniumHelper.ValidateTimeout(timeout, 10);

                var locator = SeleniumHelper.CreateLocator(this.Prop_LocatorType, locatorValue);
                return SeleniumHelper.WaitForElementClickable(driver, locator, timeout);
            }

            if (!string.IsNullOrWhiteSpace(elementId))
                return SeleniumHelper.GetElement(elementId);

            throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор для поиска");
        }

        /// <summary>
        /// Выполняет клик в соответствии с выбранным режимом <see cref="Prop_ClickMode"/>.
        /// Возвращает строку с описанием выполненного действия.
        /// </summary>
        private string ExecuteClick(ScriptingData sd, IWebDriver driver, IWebElement element, string elementId, string locatorValue)
        {
            // Формируем метку элемента для сообщения результата через лямбда-выражение
            Func<string> elementLabel = () => !string.IsNullOrWhiteSpace(locatorValue)
                ? $"{this.Prop_LocatorType}={locatorValue}"
                : elementId;

            switch (this.Prop_ClickMode)
            {
                // ── Обычный клик ───────────────────────────────────────────────
                case ClickMode.Click:
                    {
                        if (this.Prop_UseJavaScript)
                            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
                        else
                            element.Click();

                        ApplyWaitAfterClick(sd);
                        return $"[Click] Выполнен клик: {elementLabel()}";
                    }

                // ── Двойной клик ───────────────────────────────────────────────
                case ClickMode.DoubleClick:
                    {
                        if (this.Prop_UseJavaScript)
                            SeleniumHelper.DoubleClickJS(driver, element);
                        else
                            SeleniumHelper.DoubleClick(driver, element);

                        ApplyWaitAfterClick(sd);
                        return $"[DoubleClick] Выполнен двойной клик: {elementLabel()}";
                    }

                // ── Правый клик ────────────────────────────────────────────────
                case ClickMode.RightClick:
                    {
                        SeleniumHelper.RightClick(driver, element);

                        ApplyWaitAfterClick(sd);
                        return $"[RightClick] Выполнен правый клик: {elementLabel()}";
                    }

                // ── Клик с удержанием ──────────────────────────────────────────
                case ClickMode.ClickAndHold:
                    {
                        // Читаем и валидируем длительность удержания
                        string durationStr = GetPropertyValue<string>(this.Prop_HoldDuration, nameof(Prop_HoldDuration), sd) ?? "1000";
                        int duration = int.TryParse(durationStr, out int d) ? Math.Max(d, 0) : 1000;

                        SeleniumHelper.ClickAndHold(driver, element, duration);
                        return $"[ClickAndHold] Удержание {duration}мс: {elementLabel()}";
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(Prop_ClickMode),
                        $"Неизвестный режим клика: {this.Prop_ClickMode}");
            }
        }

        /// <summary>
        /// Выполняет паузу после клика, если задано положительное значение Prop_WaitAfterClick.
        /// </summary>
        private void ApplyWaitAfterClick(ScriptingData sd)
        {
            // Читаем паузу и применяем только если она больше нуля
            string waitStr = GetPropertyValue<string>(this.Prop_WaitAfterClick, nameof(Prop_WaitAfterClick), sd) ?? "0";
            if (int.TryParse(waitStr, out int waitMs) && waitMs > 0)
                System.Threading.Thread.Sleep(waitMs);
        }

        // ── Валидация ──────────────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();


            // Должен быть указан хотя бы один идентификатор элемента
            bool hasElementId = !string.IsNullOrWhiteSpace(this.Prop_ElementId);
            bool hasLocator = !string.IsNullOrWhiteSpace(this.Prop_LocatorValue);

            if (!hasElementId && !hasLocator)
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = "ElementId/LocatorValue",
                    Error = "Необходимо указать либо ID элемента, либо локатор для поиска"
                });
            }

            // Дополнительная валидация: длительность удержания должна быть неотрицательной
            if (this.Prop_ClickMode == ClickMode.ClickAndHold
                && int.TryParse(this.Prop_HoldDuration, out int dur)
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
    }
}