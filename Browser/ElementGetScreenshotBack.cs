// =============================================================================
// ElementGetScreenshotBack.cs — активность «Скриншот элемента».
//
// Создаёт скриншот конкретного элемента (не всей страницы).
// Поддерживает сохранение в файл или возврат в Base64.
//
// Используется для:
//   - Создания скриншотов отдельных элементов
//   - Документирования состояния элементов
//   - Сравнения визуального состояния
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
    /// Активность для создания скриншота элемента.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class ElementGetScreenshotBack : BrowserActivityBase<ElementGetScreenshot>
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
        /// <summary>ID элемента для скриншота.</summary>
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

        private string _propFilePath;
        /// <summary>Путь для сохранения скриншота (опционально).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Путь к файлу")]
        public string Prop_FilePath
        {
            get => _propFilePath;
            set { _propFilePath = value; InvokePropertyChanged(this, "Prop_FilePath"); }
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

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propOutBase64;
        /// <summary>Скриншот в формате Base64.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Скриншот (Base64)")]
        public string Prop_OutBase64
        {
            get => _propOutBase64;
            set { _propOutBase64 = value; InvokePropertyChanged(this, "Prop_OutBase64"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementGetScreenshotBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Элемент: Скриншот";
            sdkComponentHelp =
                "Создаёт скриншот конкретного элемента.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "ID элемента — идентификатор элемента (если уже найден)\n" +
                "Путь к файлу — путь для сохранения (опционально)\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение локатора — конкретное значение для поиска\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Таймаут (сек) — время ожидания появления элемента\n" +
                "\n" +
                "── Выходные данные ───────────────────────────\n" +
                "Скриншот (Base64) — скриншот в формате Base64\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Если указан путь к файлу, скриншот\n" +
                "сохраняется в файл. Base64 возвращается всегда.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.String("Prop_FilePath", "Путь к файлу"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.Variable<string>("Prop_OutBase64", "Скриншот (Base64)")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_FilePath = "\"\"";
            this.Prop_WaitTimeout = "10";
            this.Prop_OutBase64 = "";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                string elementId = GetPropertyValue<string>(Prop_ElementId, nameof(Prop_ElementId), sd);
                string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), sd);
                string filePath = GetPropertyValue<string>(Prop_FilePath, nameof(Prop_FilePath), sd);

                IWebDriver driver = GetDriverFromContext(sessionId);

                IWebElement element;
                if (!string.IsNullOrWhiteSpace(locatorValue))
                {
                    string timeoutStr = GetPropertyValue<string>(Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                    int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                    timeout = SeleniumHelper.ValidateTimeout(timeout, 10);

                    var locator = SeleniumHelper.CreateLocator(Prop_LocatorType, locatorValue);
                    element = SeleniumHelper.WaitForElementVisible(driver, locator, timeout);
                }
                else if (!string.IsNullOrWhiteSpace(elementId))
                {
                    element = SeleniumHelper.GetElement(elementId);
                }
                else
                {
                    throw new ArgumentException("Необходимо указать либо ID элемента, либо локатор для поиска");
                }

                string base64Screenshot = SeleniumHelper.TakeElementScreenshotBase64(element);
                SetVariableValue(Prop_OutBase64, base64Screenshot, sd);

                if (!string.IsNullOrWhiteSpace(filePath))
                    SeleniumHelper.TakeElementScreenshotToFile(element, filePath);

                string resultMsg = !string.IsNullOrWhiteSpace(filePath)
                    ? $"[Скриншот элемента] Сохранён: {filePath}"
                    : "[Скриншот элемента] Создан (Base64)";

                return CreateSuccessResult(resultMsg);
            }
            catch (Exception ex)
            {
                return CreateErrorResult(ex, "Скриншот элемента");
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

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
