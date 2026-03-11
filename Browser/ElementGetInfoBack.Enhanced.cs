// =============================================================================
// ElementGetInfoBack.Enhanced.cs — улучшенная активность «Получить информацию об элементе»
//
// Использует ElementWaitingActivityBase для унифицированной логики ожидания.
// Поддерживает различные режимы ожидания элемента перед получением информации.
//
// Режимы ожидания:
//   - Present:   элемент появился в DOM
//   - Visible:   элемент видим на странице  
//   - Clickable: элемент кликабельный
//   - None:      не ожидать, найти немедленно
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
    /// Активность для получения информации об элементе с поддержкой ожидания.
    /// </summary>
    public class ElementGetInfoBackEnhanced : ElementWaitingActivityBase<ElementGetInfo>
    {
        public override string GroupName
        {
            get => ActivityCategories.Browser;
            protected set { }
        }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propSessionId;
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
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ElementId)]
        public string Prop_ElementId
        {
            get => _propElementId;
            set { _propElementId = value; InvokePropertyChanged(this, "Prop_ElementId"); }
        }

        private string _propLocatorType;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorType)]
        public ElementLocatorType Prop_LocatorType
        {
            get => (ElementLocatorType)Enum.Parse(typeof(ElementLocatorType), _propLocatorType ?? "Id");
            set { _propLocatorType = value.ToString(); InvokePropertyChanged(this, "Prop_LocatorType"); }
        }

        private string _propLocatorValue;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorValue)]
        public string Prop_LocatorValue
        {
            get => _propLocatorValue;
            set { _propLocatorValue = value; InvokePropertyChanged(this, "Prop_LocatorValue"); }
        }

        private string _propWaitMode;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Wait),
         System.ComponentModel.DisplayName("Режим ожидания")]
        public ElementWaitMode Prop_WaitMode
        {
            get => (ElementWaitMode)Enum.Parse(typeof(ElementWaitMode), _propWaitMode ?? "Present");
            set { _propWaitMode = value.ToString(); InvokePropertyChanged(this, "Prop_WaitMode"); }
        }

        private string _propWaitTimeout;
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

        private string _propTagName;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Тег элемента")]
        public string Prop_TagName
        {
            get => _propTagName;
            set { _propTagName = value; InvokePropertyChanged(this, "Prop_TagName"); }
        }

        private string _propText;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Текст элемента")]
        public string Prop_Text
        {
            get => _propText;
            set { _propText = value; InvokePropertyChanged(this, "Prop_Text"); }
        }

        private string _propIsVisible;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Элемент видим")]
        public string Prop_IsVisible
        {
            get => _propIsVisible;
            set { _propIsVisible = value; InvokePropertyChanged(this, "Prop_IsVisible"); }
        }

        private string _propIsEnabled;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Элемент включён")]
        public string Prop_IsEnabled
        {
            get => _propIsEnabled;
            set { _propIsEnabled = value; InvokePropertyChanged(this, "Prop_IsEnabled"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementGetInfoBackEnhanced(IWFContainer container) : base(container)
        {
            sdkComponentName = "Получить информацию об элементе (Enhanced)";
            sdkComponentHelp =
                "Получает информацию об элементе с поддержкой ожидания.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии   — идентификатор сессии браузера\n" +
                "ID элемента — идентификатор элемента (если уже найден)\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение локатора — конкретное значение для поиска\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Режим ожидания — стратегия ожидания элемента\n" +
                "Таймаут (сек) — время ожидания элемента\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Тег элемента — HTML тег (div, input, button и т.д.)\n" +
                "Текст элемента — видимый текст элемента\n" +
                "Элемент видим — элемент отображается\n" +
                "Элемент включён — элемент активен";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId",   "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId",   "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue",          "Значение локатора"),
                PropertyBuilder.Enum<ElementWaitMode>("Prop_WaitMode", "Режим ожидания"),
                PropertyBuilder.Int("Prop_WaitTimeout",              "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.Variable<string>("Prop_TagName",     "Тег элемента"),
                PropertyBuilder.Variable<string>("Prop_Text",        "Текст элемента"),
                PropertyBuilder.Variable<bool>("Prop_IsVisible",     "Элемент видим"),
                PropertyBuilder.Variable<bool>("Prop_IsEnabled",     "Элемент включён")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_WaitMode = ElementWaitMode.Present;
            this.Prop_WaitTimeout = "10";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return SafeExecute(() =>
            {
                // Читаем входные параметры
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                string elementId = GetPropertyValue<string>(Prop_ElementId, nameof(Prop_ElementId), sd);
                string locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

                Logger.LogInfo(sdkComponentName,
                    "Получение информации об элементе для сессии: {0}, режим ожидания: {1}", 
                    sessionId, Prop_WaitMode);

                IWebDriver driver = GetDriverFromContext(sessionId);

                // Парсим таймаут
                string waitStr = GetPropertyValue<string>(Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                int waitTimeout = ParseAndValidateTimeout(waitStr, "Prop_WaitTimeout");

                // Ищем элемент с указанным режимом ожидания
                IWebElement element = ResolveElementWithWait(
                    sd, driver, elementId, locatorValue, Prop_LocatorType, Prop_WaitMode, waitTimeout);

                if (element == null)
                {
                    throw new NoSuchElementException(
                        $"Элемент не найден или не соответствует режиму ожидания {Prop_WaitMode}");
                }

                // Получаем информацию об элементе
                string tagName = element.TagName ?? "";
                string text = element.Text ?? "";
                bool isVisible = element.Displayed;
                bool isEnabled = element.Enabled;

                // Записываем результаты
                SetVariableValue(Prop_TagName, tagName, sd);
                SetVariableValue(Prop_Text, text, sd);
                SetVariableValue(Prop_IsVisible, isVisible, sd);
                SetVariableValue(Prop_IsEnabled, isEnabled, sd);

                Logger.LogInfo(sdkComponentName, 
                    "Информация получена: Тег={0}, Текст='{1}', Видим={2}, Включён={3}",
                    tagName, text.Length > 50 ? text.Substring(0, 50) + "..." : text, isVisible, isEnabled);

                return CreateSuccessResult(
                    $"[Получить информацию] Тег: {tagName}, Видим: {isVisible}, Включён: {isEnabled}");

            }, "Получить информацию об элементе");
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            // Проверяем, что указан хотя бы один из способов идентификации элемента
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