// =============================================================================
// ElementHoverBack.cs — объединённая активность «Навести курсор на элемент».
//
// Объединяет два режима наведения в одной активности:
//   - Center       — навести на центр элемента
//   - WithOffset   — навести с указанием смещения от центра
//
// Режим выбирается через enum HoverMode.
// Поддерживает поиск элемента по ID или по локатору.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Объединённая активность для наведения курсора на элемент.
    /// Режим наведения задаётся через свойство <see cref="Prop_HoverMode"/>.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class ElementHoverBack : BrowserActivityBase<ElementHover>
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

        #region Prop_HoverMode

        private HoverMode _propHoverMode;

        /// <summary>
        /// Режим наведения: Center или WithOffset.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Режим наведения")]
        public HoverMode Prop_HoverMode
        {
            get => _propHoverMode;
            set { _propHoverMode = value; InvokePropertyChanged(this, nameof(Prop_HoverMode)); }
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

        /// <summary>ID элемента для наведения (если элемент уже найден).</summary>
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

        #region Prop_OffsetX

        private string _propOffsetX;

        /// <summary>Смещение по X от центра элемента (только для режима WithOffset).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Offset),
         System.ComponentModel.DisplayName("Смещение X")]
        public string Prop_OffsetX
        {
            get => _propOffsetX;
            set { _propOffsetX = value; InvokePropertyChanged(this, nameof(Prop_OffsetX)); }
        }

        #endregion

        #region Prop_OffsetY

        private string _propOffsetY;

        /// <summary>Смещение по Y от центра элемента (только для режима WithOffset).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Offset),
         System.ComponentModel.DisplayName("Смещение Y")]
        public string Prop_OffsetY
        {
            get => _propOffsetY;
            set { _propOffsetY = value; InvokePropertyChanged(this, nameof(Prop_OffsetY)); }
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

        // ── Конструктор ────────────────────────────────────────────────

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

            this.Prop_HoverMode = HoverMode.Center;
            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_OffsetX = "0";
            this.Prop_OffsetY = "0";
            this.Prop_WaitTimeout = "10";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return SafeExecute(() =>
            {
                // Чтение параметров
                string sessionId = GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd);
                string elementId = GetPropertyValue<string>(this.Prop_ElementId, nameof(Prop_ElementId), sd);
                string locatorValue = GetPropertyValue<string>(this.Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

                Logger.LogInfo(sdkComponentName, "Начинается наведение курсора для сессии: {0}", sessionId);

                // Получение драйвера
                var driver = GetDriverFromContext(sessionId);

                // Получение элемента: либо по ID, либо поиск по локатору
                IWebElement element;
                if (!string.IsNullOrWhiteSpace(locatorValue))
                {
                    // Поиск элемента по локатору
                    string timeoutStr = GetPropertyValue<string>(this.Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                    int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                    ValidatePositive(timeout, "Prop_WaitTimeout");

                    var locatorType = ConvertLocatorType(this.Prop_LocatorType);
                    element = ElementLocator.WaitForVisible(driver, locatorType, locatorValue, timeout);
                    
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

                // Выполнение наведения в зависимости от режима
                string resultMsg;
                Logger.LogDebug(sdkComponentName, "Режим наведения: {0}", this.Prop_HoverMode);

                switch (this.Prop_HoverMode)
                {
                    case HoverMode.Center:
                        // Наведение на центр элемента
                        var actions = new Actions(driver);
                        actions.MoveToElement(element).Perform();

                        resultMsg = !string.IsNullOrWhiteSpace(locatorValue)
                            ? $"[Навести курсор] Курсор наведён на центр: {this.Prop_LocatorType}={locatorValue}"
                            : $"[Навести курсор] Курсор наведён на центр элемента {elementId}";
                        
                        Logger.LogDebug(sdkComponentName, "Курсор наведен на центр элемента");
                        break;

                    case HoverMode.WithOffset:
                        // Наведение с смещением
                        string offsetXStr = GetPropertyValue<string>(this.Prop_OffsetX, "Prop_OffsetX", sd) ?? "0";
                        string offsetYStr = GetPropertyValue<string>(this.Prop_OffsetY, "Prop_OffsetY", sd) ?? "0";
                        int offsetX = int.TryParse(offsetXStr, out int ox) ? ox : 0;
                        int offsetY = int.TryParse(offsetYStr, out int oy) ? oy : 0;

                        var actionsWithOffset = new Actions(driver);
                        actionsWithOffset.MoveToElement(element, offsetX, offsetY).Perform();

                        resultMsg = $"[Навести курсор] Выполнено с смещением ({offsetX}, {offsetY})";
                        Logger.LogDebug(sdkComponentName, "Курсор наведен с смещением: ({0}, {1})", offsetX, offsetY);
                        break;

                    default:
                        throw new ArgumentException($"Неизвестный режим наведения: {this.Prop_HoverMode}");
                }

                Logger.LogInfo(sdkComponentName, "Наведение курсора завершено успешно");
                return CreateSuccessResult(resultMsg);
            }, "Навести курсор");
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
