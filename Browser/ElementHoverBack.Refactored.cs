// =============================================================================
// ElementHoverBack.cs — объединённая активность «Навести курсор на элемент».
//
// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Primo.MIA.Browser;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Объединённая активность для наведения курсора на элемент.
    /// Режим наведения задаётся через свойство <see cref="Prop_HoverMode"/>.
    /// </summary>
    public class ElementHoverBack : BrowserActivityBase<ElementHover>
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

        private HoverMode _propHoverMode;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName("Режим наведения")]
        public HoverMode Prop_HoverMode
        {
            get => _propHoverMode;
            set { _propHoverMode = value; InvokePropertyChanged(this, nameof(Prop_HoverMode)); }
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

        private string _propOffsetX;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Offset)]
        [System.ComponentModel.DisplayName("Смещение X")]
        public string Prop_OffsetX
        {
            get => _propOffsetX;
            set { _propOffsetX = value; InvokePropertyChanged(this, nameof(Prop_OffsetX)); }
        }

        private string _propOffsetY;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Offset)]
        [System.ComponentModel.DisplayName("Смещение Y")]
        public string Prop_OffsetY
        {
            get => _propOffsetY;
            set { _propOffsetY = value; InvokePropertyChanged(this, nameof(Prop_OffsetY)); }
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

        #endregion

        #region Constructor

        public ElementHoverBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Элемент: Навести курсор";
            sdkComponentHelp =
                "Наводит курсор мыши на элемент.\n" +
                "\n" +
                "── Режимы наведения ───────────────────────────\n" +
                "Center      — навести на центр элемента\n" +
                "WithOffset  — навести с указанием смещения\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии   — идентификатор сессии браузера\n" +
                "ID элемента — элемент для наведения (если уже найден)\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение локатора — конкретное значение для поиска\n" +
                "\n" +
                "── Смещение (только для WithOffset) ──────────\n" +
                "Смещение X — смещение по горизонтали (пиксели)\n" +
                "Смещение Y — смещение по вертикали (пиксели)\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Таймаут (сек) — время ожидания появления элемента\n" +
                "\n" +
                "Используется для:\n" +
                "  - Активации выпадающих меню\n" +
                "  - Отображения всплывающих подсказок\n" +
                "  - Триггера hover-эффектов\n" +
                "  - Точного позиционирования курсора\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Если указан локатор, элемент будет найден автоматически.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<HoverMode>("Prop_HoverMode", "Режим наведения"),
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.Int("Prop_OffsetX", "Смещение X (для WithOffset)"),
                PropertyBuilder.Int("Prop_OffsetY", "Смещение Y (для WithOffset)"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)")
            };

            InitClass(container);

            // Defaults
            Prop_HoverMode = HoverMode.Center;
            Prop_SessionId = "\"\"";
            Prop_ElementId = "\"\"";
            Prop_LocatorType = ElementLocatorType.Id;
            Prop_LocatorValue = "\"\"";
            Prop_OffsetX = "0";
            Prop_OffsetY = "0";
            Prop_WaitTimeout = "10";
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

                // Выполнение наведения
                string resultMsg = PerformHoverAction(sd, driver, element);

                return CreateSuccessResult(resultMsg);
            },
            $"Навести курсор — {Prop_HoverMode}");
        }

        #endregion

        #region Element Resolution

        /// <summary>
        /// Разрешает IWebElement: сначала по локатору (если задан), затем по ID.
        /// Ожидает видимости элемента для наведения курсора.
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
                
                // Ожидаем видимости элемента для наведения
                return ElementLocator.WaitForVisible(driver, locatorType, locatorValue, timeout);
            }

            var element = SeleniumHelper.GetElement(elementId);
            
            // Проверяем видимость элемента из репозитория
            if (element != null && !element.Displayed)
            {
                throw new ElementNotInteractableException(
                    $"Элемент с ID '{elementId}' не видим на странице");
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

        #region Hover Actions

        /// <summary>
        /// Выполняет наведение курсора в соответствии с выбранным режимом.
        /// </summary>
        private string PerformHoverAction(ScriptingData sd, IWebDriver driver, IWebElement element)
        {
            switch (Prop_HoverMode)
            {
                case HoverMode.Center:
                    return PerformHoverToCenter(driver, element);

                case HoverMode.WithOffset:
                    return PerformHoverWithOffset(sd, driver, element);

                default:
                    throw new ArgumentOutOfRangeException(nameof(Prop_HoverMode),
                        $"Неизвестный режим наведения: {Prop_HoverMode}");
            }
        }

        /// <summary>
        /// Наводит курсор на центр элемента.
        /// </summary>
        private string PerformHoverToCenter(IWebDriver driver, IWebElement element)
        {
            var actions = new Actions(driver);
            actions.MoveToElement(element).Perform();

            return $"[Навести курсор] Курсор наведён на центр: {GetElementLabel()}";
        }

        /// <summary>
        /// Наводит курсор с указанным смещением от центра элемента.
        /// </summary>
        private string PerformHoverWithOffset(ScriptingData sd, IWebDriver driver, IWebElement element)
        {
            int offsetX = ParseOffset(sd, Prop_OffsetX, nameof(Prop_OffsetX));
            int offsetY = ParseOffset(sd, Prop_OffsetY, nameof(Prop_OffsetY));

            SeleniumHelper.HoverWithOffset(driver, element, offsetX, offsetY);

            return $"[Навести курсор] Выполнено с смещением ({offsetX}, {offsetY}): {GetElementLabel()}";
        }

        /// <summary>
        /// Парсит значение смещения.
        /// </summary>
        private int ParseOffset(ScriptingData sd, string propertyValue, string propertyName)
        {
            string offsetStr = GetPropertyValue<string>(propertyValue, propertyName, sd) ?? "0";
            return int.TryParse(offsetStr, out int offset) ? offset : 0;
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
