// =============================================================================
// ElementDragDropBack.cs — активность «Перетаскивание элемента».
//
// Выполняет Drag and Drop элемента из одной позиции в другую.
// Поддерживает перетаскивание на элемент или на координаты со смещением.
//
// Используется для:
//   - Перемещения элементов в списках
//   - Изменения порядка элементов
//   - Drag and Drop в веб-приложениях
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
    /// Активность для перетаскивания элемента на странице.
    /// </summary>
    public class ElementDragDropBack : PrimoComponentTO<ElementDragDrop>
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

        private string _propSourceElementId;
        /// <summary>ID исходного элемента для перетаскивания.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("ID исходного элемента")]
        public string Prop_SourceElementId
        {
            get => _propSourceElementId;
            set { _propSourceElementId = value; InvokePropertyChanged(this, "Prop_SourceElementId"); }
        }

        private ElementLocatorType _propSourceLocatorType;
        /// <summary>Тип локатора исходного элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName("Тип локатора (исходный)")]
        public ElementLocatorType Prop_SourceLocatorType
        {
            get => _propSourceLocatorType;
            set { _propSourceLocatorType = value; InvokePropertyChanged(this, "Prop_SourceLocatorType"); }
        }

        private string _propSourceLocatorValue;
        /// <summary>Значение локатора исходного элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName("Значение локатора (исходный)")]
        public string Prop_SourceLocatorValue
        {
            get => _propSourceLocatorValue;
            set { _propSourceLocatorValue = value; InvokePropertyChanged(this, "Prop_SourceLocatorValue"); }
        }

        private string _propTargetElementId;
        /// <summary>ID целевого элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("ID целевого элемента")]
        public string Prop_TargetElementId
        {
            get => _propTargetElementId;
            set { _propTargetElementId = value; InvokePropertyChanged(this, "Prop_TargetElementId"); }
        }

        private ElementLocatorType _propTargetLocatorType;
        /// <summary>Тип локатора целевого элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName("Тип локатора (целевой)")]
        public ElementLocatorType Prop_TargetLocatorType
        {
            get => _propTargetLocatorType;
            set { _propTargetLocatorType = value; InvokePropertyChanged(this, "Prop_TargetLocatorType"); }
        }

        private string _propTargetLocatorValue;
        /// <summary>Значение локатора целевого элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName("Значение локатора (целевой)")]
        public string Prop_TargetLocatorValue
        {
            get => _propTargetLocatorValue;
            set { _propTargetLocatorValue = value; InvokePropertyChanged(this, "Prop_TargetLocatorValue"); }
        }

        private string _propOffsetX;
        /// <summary>Смещение по X (альтернатива целевому элементу).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Offset),
         System.ComponentModel.DisplayName("Смещение X (пиксели)")]
        public string Prop_OffsetX
        {
            get => _propOffsetX;
            set { _propOffsetX = value; InvokePropertyChanged(this, "Prop_OffsetX"); }
        }

        private string _propOffsetY;
        /// <summary>Смещение по Y (альтернатива целевому элементу).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Offset),
         System.ComponentModel.DisplayName("Смещение Y (пиксели)")]
        public string Prop_OffsetY
        {
            get => _propOffsetY;
            set { _propOffsetY = value; InvokePropertyChanged(this, "Prop_OffsetY"); }
        }

        private string _propWaitTimeout;
        /// <summary>Таймаут ожидания элементов (сек).</summary>
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
        /// <summary>Использовать JavaScript для drag and drop.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Использовать JavaScript")]
        public bool Prop_UseJavaScript
        {
            get => _propUseJavaScript;
            set { _propUseJavaScript = value; InvokePropertyChanged(this, "Prop_UseJavaScript"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementDragDropBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Элемент: Перетаскивание";
            sdkComponentHelp =
                "Выполняет Drag and Drop элемента.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "ID исходного элемента — элемент для перетаскивания\n" +
                "ID целевого элемента — куда перетащить\n" +
                "Использовать JavaScript — JS для сложных случаев\n" +
                "\n" +
                "── Локаторы (альтернатива ID) ────────────────\n" +
                "Тип/Значение локатора (исходный) — поиск источника\n" +
                "Тип/Значение локатора (целевой) — поиск цели\n" +
                "\n" +
                "── Смещение (альтернатива целевому элементу) ─\n" +
                "Смещение X/Y — перетащить на координаты\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Таймаут (сек) — время ожидания элементов\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Укажите либо целевой элемент, либо смещение.\n" +
                "JavaScript режим полезен для HTML5 drag and drop.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_SourceElementId", "ID исходного элемента"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_SourceLocatorType", "Тип локатора (исходный)"),
                PropertyBuilder.String("Prop_SourceLocatorValue", "Значение локатора (исходный)"),
                PropertyBuilder.Variable<string>("Prop_TargetElementId", "ID целевого элемента"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_TargetLocatorType", "Тип локатора (целевой)"),
                PropertyBuilder.String("Prop_TargetLocatorValue", "Значение локатора (целевой)"),
                PropertyBuilder.Int("Prop_OffsetX", "Смещение X (пиксели)"),
                PropertyBuilder.Int("Prop_OffsetY", "Смещение Y (пиксели)"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элементов (сек)"),
                PropertyBuilder.BooleanObject("Prop_UseJavaScript", "Использовать JavaScript")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_SourceElementId = "\"\"";
            this.Prop_SourceLocatorType = ElementLocatorType.Id;
            this.Prop_SourceLocatorValue = "\"\"";
            this.Prop_TargetElementId = "\"\"";
            this.Prop_TargetLocatorType = ElementLocatorType.Id;
            this.Prop_TargetLocatorValue = "\"\"";
            this.Prop_OffsetX = "0";
            this.Prop_OffsetY = "0";
            this.Prop_WaitTimeout = "10";
            this.Prop_UseJavaScript = false;
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Чтение параметров через SessionResolver (поддержка ambient-контекста)
                string sessionId = SessionResolver.Resolve(
                    GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));

                var driver = SeleniumHelper.GetDriver(sessionId);

                // Получение исходного элемента
                IWebElement sourceElement = GetSourceElement(driver, sd);

                // Определяем режим: перетаскивание на элемент или на смещение
                string targetElementId = GetPropertyValue<string>(this.Prop_TargetElementId, "Prop_TargetElementId", sd);
                string targetLocatorValue = GetPropertyValue<string>(this.Prop_TargetLocatorValue, "Prop_TargetLocatorValue", sd);
                
                string offsetXStr = GetPropertyValue<string>(this.Prop_OffsetX, "Prop_OffsetX", sd) ?? "0";
                string offsetYStr = GetPropertyValue<string>(this.Prop_OffsetY, "Prop_OffsetY", sd) ?? "0";
                int offsetX = int.TryParse(offsetXStr, out int ox) ? ox : 0;
                int offsetY = int.TryParse(offsetYStr, out int oy) ? oy : 0;

                bool hasTargetElement = !string.IsNullOrWhiteSpace(targetElementId) || !string.IsNullOrWhiteSpace(targetLocatorValue);
                bool hasOffset = offsetX != 0 || offsetY != 0;

                string resultMsg;

                if (hasTargetElement)
                {
                    // Перетаскивание на целевой элемент
                    IWebElement targetElement = GetTargetElement(driver, sd);

                    if (this.Prop_UseJavaScript)
                    {
                        SeleniumHelper.DragAndDropJS(driver, sourceElement, targetElement);
                    }
                    else
                    {
                        SeleniumHelper.DragAndDrop(driver, sourceElement, targetElement);
                    }

                    resultMsg = $"[Перетаскивание] Выполнено на целевой элемент";
                }
                else if (hasOffset)
                {
                    // Перетаскивание на смещение
                    SeleniumHelper.DragAndDropByOffset(driver, sourceElement, offsetX, offsetY);
                    resultMsg = $"[Перетаскивание] Выполнено на смещение ({offsetX}, {offsetY})";
                }
                else
                {
                    throw new ArgumentException("Необходимо указать либо целевой элемент, либо смещение");
                }

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
                    ErrorMessage = $"Ошибка [Перетаскивание]: {ex.Message}"
                };
            }
        }

        // ── Приватные методы ───────────────────────────────────────────

        /// <summary>
        /// Получает исходный элемент для перетаскивания.
        /// </summary>
        private IWebElement GetSourceElement(IWebDriver driver, ScriptingData sd)
        {
            string elementId = GetPropertyValue<string>(this.Prop_SourceElementId, "Prop_SourceElementId", sd);
            string locatorValue = GetPropertyValue<string>(this.Prop_SourceLocatorValue, "Prop_SourceLocatorValue", sd);

            if (!string.IsNullOrWhiteSpace(locatorValue))
            {
                string timeoutStr = GetPropertyValue<string>(this.Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                timeout = SeleniumHelper.ValidateTimeout(timeout, 10);

                var locator = SeleniumHelper.CreateLocator(this.Prop_SourceLocatorType, locatorValue);
                return SeleniumHelper.WaitForElementVisible(driver, locator, timeout);
            }
            else if (!string.IsNullOrWhiteSpace(elementId))
            {
                return SeleniumHelper.GetElement(elementId);
            }
            else
            {
                throw new ArgumentException("Необходимо указать исходный элемент (ID или локатор)");
            }
        }

        /// <summary>
        /// Получает целевой элемент для перетаскивания.
        /// </summary>
        private IWebElement GetTargetElement(IWebDriver driver, ScriptingData sd)
        {
            string elementId = GetPropertyValue<string>(this.Prop_TargetElementId, "Prop_TargetElementId", sd);
            string locatorValue = GetPropertyValue<string>(this.Prop_TargetLocatorValue, "Prop_TargetLocatorValue", sd);

            if (!string.IsNullOrWhiteSpace(locatorValue))
            {
                string timeoutStr = GetPropertyValue<string>(this.Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                timeout = SeleniumHelper.ValidateTimeout(timeout, 10);

                var locator = SeleniumHelper.CreateLocator(this.Prop_TargetLocatorType, locatorValue);
                return SeleniumHelper.WaitForElementVisible(driver, locator, timeout);
            }
            else if (!string.IsNullOrWhiteSpace(elementId))
            {
                return SeleniumHelper.GetElement(elementId);
            }
            else
            {
                throw new ArgumentException("Необходимо указать целевой элемент (ID или локатор)");
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
             

            // Проверяем наличие исходного элемента
            bool hasSourceId = !string.IsNullOrWhiteSpace(this.Prop_SourceElementId);
            bool hasSourceLocator = !string.IsNullOrWhiteSpace(this.Prop_SourceLocatorValue);

            if (!hasSourceId && !hasSourceLocator)
            {
                ret.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = "SourceElement",
                    Error = "Необходимо указать исходный элемент (ID или локатор)"
                });
            }

            // Проверяем наличие цели (элемент или смещение)
            bool hasTargetId = !string.IsNullOrWhiteSpace(this.Prop_TargetElementId);
            bool hasTargetLocator = !string.IsNullOrWhiteSpace(this.Prop_TargetLocatorValue);
            bool hasOffset = !string.IsNullOrWhiteSpace(this.Prop_OffsetX) || !string.IsNullOrWhiteSpace(this.Prop_OffsetY);

            if (!hasTargetId && !hasTargetLocator && !hasOffset)
            {
                ret.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = "Target",
                    Error = "Необходимо указать либо целевой элемент, либо смещение"
                });
            }

            return ret;
        }
    }
}
