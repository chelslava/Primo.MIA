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
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для проверки существования элемента на странице.
    /// </summary>
    public class ElementExistsBack : PrimoComponentTO<ElementExists>
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
                PropertyBuilder.Variable<bool>("Prop_Exists", "Элемент существует")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Чтение параметров через SessionResolver (поддержка ambient-контекста)
                string sessionId = SessionResolver.Resolve(
                    GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
                string locatorValue = GetPropertyValue<string>(this.Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

                if (string.IsNullOrWhiteSpace(locatorValue))
                    throw new ArgumentException("Значение локатора не может быть пустым");

                // Получение драйвера
                var driver = SeleniumHelper.GetDriver(sessionId);

                // Создание локатора
                var locator = SeleniumHelper.CreateLocator(this.Prop_LocatorType, locatorValue);

                // Проверка существования
                bool exists = SeleniumHelper.ElementExists(driver, locator);

                // Запись результата
                if (!string.IsNullOrWhiteSpace(this.Prop_Exists))
                    SetVariableValue(this.Prop_Exists, exists, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Проверить существование] {this.Prop_LocatorType}={locatorValue} → {exists}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [Проверить существование]: {ex.Message}"
                };
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
             
            ret.ValidateRequired(this.Prop_LocatorValue, ActivityStrings.Field_LocatorValue, "Значение локатора обязательно");
            return ret;
        }
    }
}
