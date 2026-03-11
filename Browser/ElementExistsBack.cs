// =============================================================================
// ElementExistsBack.cs — активность «Проверить существование элемента».
//
// Проверяет наличие элемента на странице без ожидания.
// Не выбрасывает исключение если элемент не найден.
//
// Используется для условной логики в процессах автоматизации.
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
    /// Активность для проверки существования элемента на странице.
    /// REFACTORED: Использует BrowserActivityBase и сервисы для устранения дублирования кода
    /// </summary>
    public class ElementExistsBack : BrowserActivityBase<ElementExists>
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

        private ElementLocatorType _propLocatorType;
        /// <summary>Тип локатора.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorType)]
        public ElementLocatorType Prop_LocatorType
        {
            get => _propLocatorType;
            set { _propLocatorType = value; InvokePropertyChanged(this, "Prop_LocatorType"); }
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
            set { _propLocatorValue = value; InvokePropertyChanged(this, "Prop_LocatorValue"); }
        }

        private string _propWaitMode;
        /// <summary>Режим ожидания элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Wait),
         System.ComponentModel.DisplayName("Режим ожидания")]
        public ElementWaitMode Prop_WaitMode
        {
            get => (ElementWaitMode)Enum.Parse(typeof(ElementWaitMode), _propWaitMode ?? "Present");
            set { _propWaitMode = value.ToString(); InvokePropertyChanged(this, "Prop_WaitMode"); }
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

        private string _propExists;
        /// <summary>Элемент существует.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ElementExists)]
        public string Prop_Exists
        {
            get => _propExists;
            set { _propExists = value; InvokePropertyChanged(this, "Prop_Exists"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementExistsBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ElementExists;
            sdkComponentHelp =
                "Проверяет существование элемента на странице.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "\n" +
                "── Локатор ────────────────────────────────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение     — конкретное значение для поиска\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Существует — true если элемент найден\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Не выбрасывает исключение при отсутствии элемента.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.Enum<ElementWaitMode>("Prop_WaitMode", "Режим ожидания"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.Variable<bool>("Prop_Exists", "Элемент существует")
            };

            InitClass(container);

            Prop_SessionId = "\"\"";
            Prop_LocatorType = ElementLocatorType.Id;
            Prop_LocatorValue = "\"\"";
            Prop_WaitMode = ElementWaitMode.Present;
            Prop_WaitTimeout = "3"; // Быстрая проверка по умолчанию
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            bool exists = false;
            string locatorValue = string.Empty;

            var result = SafeExecute(() =>
            {
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                locatorValue = GetPropertyValue<string>(Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

                ValidateNotEmpty(locatorValue, nameof(Prop_LocatorValue));

                // Получение драйвера
                IWebDriver driver = GetDriverFromContext(sessionId);

                // Парсим таймаут
                string waitStr = GetPropertyValue<string>(Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "3";
                int waitTimeout = int.TryParse(waitStr, out int wt) ? wt : 3;
                ValidatePositive(waitTimeout, "Prop_WaitTimeout");

                // Логирование начала операции
                Logger.LogInfo(sdkComponentName, 
                    "Проверка существования элемента: {0}={1}, режим: {2}, таймаут: {3}с", 
                    Prop_LocatorType, locatorValue, Prop_WaitMode, waitTimeout);

                // Использование ElementLocator с новой системой ожидания
                var elementLocator = new ElementLocator();
                var locatorType = ConvertLocatorType(Prop_LocatorType);
                var element = elementLocator.TryFindElementWithWaitMode(
                    driver, locatorType, locatorValue, waitTimeout, Prop_WaitMode);
                
                exists = element != null;

                // Запись результата в выходную переменную
                if (!string.IsNullOrWhiteSpace(Prop_Exists))
                    SetVariableValue(Prop_Exists, exists, sd);

                // Логирование результата
                Logger.LogInfo(sdkComponentName, 
                    "Элемент {0}: {1}={2} (режим: {3})", 
                    exists ? "найден" : "не найден", Prop_LocatorType, locatorValue, Prop_WaitMode);

            }, "Проверить существование");

            if (result.IsSuccess)
                result.SuccessMessage = $"[Проверить существование] {Prop_LocatorType}={locatorValue} → {exists} (режим: {Prop_WaitMode})";

            return result;
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