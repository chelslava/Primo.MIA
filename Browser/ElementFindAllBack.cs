// =============================================================================
// ElementFindAllBack.cs — активность «Найти все элементы».
//
// Находит все элементы на странице по заданному локатору.
// Возвращает список ID элементов для дальнейшей обработки.
//
// Используется для работы с множественными элементами:
//   - Списки товаров
//   - Таблицы данных
//   - Наборы кнопок
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для поиска всех элементов на странице по локатору.
    /// </summary>
    public class ElementFindAllBack : PrimoComponentTO<ElementFindAll>
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

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propElementIds;
        /// <summary>Список ID найденных элементов.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ElementIds)]
        public string Prop_ElementIds
        {
            get => _propElementIds;
            set { _propElementIds = value; InvokePropertyChanged(this, "Prop_ElementIds"); }
        }

        private string _propCount;
        /// <summary>Количество найденных элементов.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ElementCount)]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, "Prop_Count"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementFindAllBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ElementFindAll;
            sdkComponentHelp =
                "Находит все элементы на странице по локатору.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "\n" +
                "── Локатор ────────────────────────────────────\n" +
                "Тип локатора — способ поиска элементов\n" +
                "Значение     — конкретное значение для поиска\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Таймаут (сек) — время ожидания появления элементов\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Список ID    — список идентификаторов элементов\n" +
                "Количество   — число найденных элементов\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Каждый элемент сохраняется в RepoDict с уникальным ID.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания элементов (сек)"),
                PropertyBuilder.Variable<List<string>>("Prop_ElementIds", "Список ID найденных элементов"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество найденных элементов")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.ClassName;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_WaitTimeout = "10";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Чтение параметров
                string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
                string locatorValue = GetPropertyValue<string>(this.Prop_LocatorValue, "Prop_LocatorValue", sd);

                if (string.IsNullOrWhiteSpace(sessionId))
                    throw new ArgumentException("ID сессии не может быть пустым");

                if (string.IsNullOrWhiteSpace(locatorValue))
                    throw new ArgumentException("Значение локатора не может быть пустым");

                // Получение драйвера
                var driver = SeleniumHelper.GetDriver(sessionId);

                // Чтение таймаута
                string timeoutStr = GetPropertyValue<string>(this.Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                int timeout = int.TryParse(timeoutStr, out int t) ? t : 10;
                timeout = SeleniumHelper.ValidateTimeout(timeout, 10);

                // Создание локатора
                var locator = SeleniumHelper.CreateLocator(this.Prop_LocatorType, locatorValue);

                // Ожидание появления хотя бы одного элемента
                try
                {
                    SeleniumHelper.WaitForElement(driver, locator, timeout);
                }
                catch
                {
                    // Если элементы не найдены, возвращаем пустой список
                }

                // Поиск всех элементов
                var elements = SeleniumHelper.FindElements(driver, locator);

                // Сохранение элементов в RepoDict и создание списка ID
                var elementIds = elements
                    .Select(element =>
                    {
                        string elementId = SeleniumHelper.GenerateElementId();
                        RepoDict.Set(elementId, element);
                        return elementId;
                    })
                    .ToList();

                // Запись результатов
                if (!string.IsNullOrWhiteSpace(this.Prop_ElementIds))
                    SetVariableValue(this.Prop_ElementIds, elementIds, sd);

                if (!string.IsNullOrWhiteSpace(this.Prop_Count))
                    SetVariableValue(this.Prop_Count, elementIds.Count, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Найти все элементы] Найдено {elementIds.Count} элементов: {this.Prop_LocatorType}={locatorValue}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [Найти все элементы]: {ex.Message}"
                };
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_SessionId, ActivityStrings.Field_SessionId, "ID сессии обязателен");
            ret.ValidateRequired(this.Prop_LocatorValue, ActivityStrings.Field_LocatorValue, "Значение локатора обязательно");
            return ret;
        }
    }
}
