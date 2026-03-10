// =============================================================================
// ElementFindUnifiedBack.cs — объединённая активность «Найти элемент(ы)».
//
// Объединяет два режима поиска в одной активности:
//   - FindOne — поиск первого совпадающего элемента,
//               возвращает один ID (string)
//   - FindAll — поиск всех совпадающих элементов,
//               возвращает список ID (List<string>) и их количество
//
// Режим выбирается через enum FindMode.
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
    /// Объединённая активность поиска элементов.
    /// Режим задаётся свойством <see cref="Prop_FindMode"/>.
    /// </summary>
    public class ElementFindBack : PrimoComponentSimple<ElementFind>
    {
        // ── Группа и таймаут ──────────────────────────────────────────────────

        public override string GroupName
        {
            get => ActivityCategories.Browser;
            protected set { }
        }

        // ── Входные параметры ──────────────────────────────────────────────────

        #region Prop_FindMode

        private FindMode _propFindMode;

        /// <summary>
        /// Режим поиска: FindOne (один элемент) или FindAll (все элементы).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Режим поиска")]
        public FindMode Prop_FindMode
        {
            get => _propFindMode;
            set { _propFindMode = value; InvokePropertyChanged(this, nameof(Prop_FindMode)); }
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

        #region Локатор

        private ElementLocatorType _propLocatorType;

        /// <summary>Тип локатора для поиска элемента(ов).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorType)]
        public ElementLocatorType Prop_LocatorType
        {
            get => _propLocatorType;
            set { _propLocatorType = value; InvokePropertyChanged(this, nameof(Prop_LocatorType)); }
        }

        private string _propLocatorValue;

        /// <summary>Значение локатора для поиска элемента(ов).</summary>
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

        /// <summary>Таймаут ожидания элемента(ов) (сек).</summary>
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

        // ── Выходные параметры ─────────────────────────────────────────────────

        #region Prop_ElementId (только FindOne)

        private string _propElementId;

        /// <summary>
        /// ID найденного элемента.
        /// Заполняется только в режиме FindOne.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ElementId)]
        public string Prop_ElementId
        {
            get => _propElementId;
            set { _propElementId = value; InvokePropertyChanged(this, nameof(Prop_ElementId)); }
        }

        #endregion

        #region Prop_ElementIds (только FindAll)

        private string _propElementIds;

        /// <summary>
        /// Список ID найденных элементов.
        /// Заполняется только в режиме FindAll.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ElementIds)]
        public string Prop_ElementIds
        {
            get => _propElementIds;
            set { _propElementIds = value; InvokePropertyChanged(this, nameof(Prop_ElementIds)); }
        }

        #endregion

        #region Prop_Count (только FindAll)

        private string _propCount;

        /// <summary>
        /// Количество найденных элементов.
        /// Заполняется только в режиме FindAll.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ElementCount)]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, nameof(Prop_Count)); }
        }

        #endregion

        // ── Конструктор ────────────────────────────────────────────────────────

        public ElementFindBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Элемент: Найти";
            sdkComponentHelp =
                "Находит элемент(ы) на странице по заданному локатору.\n" +
                "\n" +
                "── Режимы поиска ─────────────────────────────\n" +
                "FindOne — найти первый подходящий элемент\n" +
                "FindAll — найти все подходящие элементы\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "\n" +
                "── Локатор ────────────────────────────────────\n" +
                "Тип локатора — способ поиска элемента(ов)\n" +
                "Значение     — конкретное значение для поиска\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Таймаут (сек) — время ожидания появления элемента(ов)\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "ID элемента  — идентификатор найденного элемента (FindOne)\n" +
                "Список ID    — список идентификаторов элементов (FindAll)\n" +
                "Количество   — число найденных элементов (FindAll)\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Каждый элемент сохраняется в RepoDict с уникальным ID.";

            sdkComponentIcon = ActivityIcons.Browser;

            // Список всех свойств — движок сам скроет неактуальные по режиму
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<FindMode>("Prop_FindMode", "Режим поиска"),
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue", "Значение локатора"),
                PropertyBuilder.Int("Prop_WaitTimeout", "Таймаут ожидания (сек)"),
                // FindOne
                PropertyBuilder.Variable<string>("Prop_ElementId", "ID найденного элемента"),
                // FindAll
                PropertyBuilder.Variable<List<string>>("Prop_ElementIds", "Список ID найденных элементов"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество найденных элементов")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_FindMode    = FindMode.FindOne;
            this.Prop_SessionId   = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_WaitTimeout = "10";
        }

        // ── TimedAction — точка входа ──────────────────────────────────────────

        public override ExecutionResult SimpleAction(ScriptingData sd)
        {
            try
            {
                // Чтение обязательных параметров через SessionResolver (поддержка ambient-контекста)
                string sessionId = SessionResolver.Resolve(
                    GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
                string locatorValue = GetPropertyValue<string>(this.Prop_LocatorValue, nameof(Prop_LocatorValue), sd);

                if (string.IsNullOrWhiteSpace(locatorValue))
                    throw new ArgumentException("Значение локатора не может быть пустым");

                // Получение WebDriver по ID сессии
                var driver = SeleniumHelper.GetDriver(sessionId);

                // Парсинг и валидация таймаута
                string timeoutStr = GetPropertyValue<string>(this.Prop_WaitTimeout, nameof(Prop_WaitTimeout), sd) ?? "10";
                int timeout       = int.TryParse(timeoutStr, out int t) ? t : 10;
                timeout           = SeleniumHelper.ValidateTimeout(timeout, 10);

                // Создание локатора один раз для обоих режимов
                var locator = SeleniumHelper.CreateLocator(this.Prop_LocatorType, locatorValue);

                // Делегируем выполнение нужному методу в зависимости от режима
                return this.Prop_FindMode == FindMode.FindOne
                    ? ExecuteFindOne(sd, driver, locator, locatorValue, timeout)
                    : ExecuteFindAll(sd, driver, locator, locatorValue, timeout);
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess    = false,
                    ErrorMessage = $"Ошибка [Найти — {this.Prop_FindMode}]: {ex.Message}"
                };
            }
        }

        // ── Вспомогательные методы ─────────────────────────────────────────────

        /// <summary>
        /// Ищет первый подходящий элемент и записывает его ID в <see cref="Prop_ElementId"/>.
        /// </summary>
        private ExecutionResult ExecuteFindOne(
            ScriptingData sd, IWebDriver driver,
            By locator, string locatorValue, int timeout)
        {
            // Ожидаем появления элемента
            var element = SeleniumHelper.WaitForElement(driver, locator, timeout);

            // Генерируем уникальный ID и сохраняем элемент в общем репозитории
            string elementId = SeleniumHelper.GenerateElementId();
            RepoDict.Set(elementId, element);

            // Записываем результат в выходную переменную (если она задана)
            if (!string.IsNullOrWhiteSpace(this.Prop_ElementId))
                SetVariableValue(this.Prop_ElementId, elementId, sd);

            return new ExecutionResult
            {
                IsSuccess      = true,
                SuccessMessage = $"[FindOne] Элемент найден: {this.Prop_LocatorType}={locatorValue}, ID={elementId}"
            };
        }

        /// <summary>
        /// Ищет все подходящие элементы и записывает их ID в <see cref="Prop_ElementIds"/>
        /// и количество в <see cref="Prop_Count"/>.
        /// </summary>
        private ExecutionResult ExecuteFindAll(
            ScriptingData sd, IWebDriver driver,
            By locator, string locatorValue, int timeout)
        {
            // Ожидаем появления хотя бы одного элемента; при отсутствии возвращаем пустой список
            try { SeleniumHelper.WaitForElement(driver, locator, timeout); }
            catch { /* элементы не найдены — нормальная ситуация для FindAll */ }

            // Ищем все совпадающие элементы и сохраняем каждый через LINQ
            var elementIds = SeleniumHelper
                .FindElements(driver, locator)
                .Select(element =>
                {
                    // Генерируем уникальный ID для каждого элемента и сохраняем в репозиторий
                    string id = SeleniumHelper.GenerateElementId();
                    RepoDict.Set(id, element);
                    return id;
                })
                .ToList();

            // Записываем результаты в выходные переменные (если они заданы)
            if (!string.IsNullOrWhiteSpace(this.Prop_ElementIds))
                SetVariableValue(this.Prop_ElementIds, elementIds, sd);

            if (!string.IsNullOrWhiteSpace(this.Prop_Count))
                SetVariableValue(this.Prop_Count, elementIds.Count, sd);

            return new ExecutionResult
            {
                IsSuccess      = true,
                SuccessMessage = $"[FindAll] Найдено {elementIds.Count} элементов: {this.Prop_LocatorType}={locatorValue}"
            };
        }

        // ── Валидация ──────────────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            // Обязательные поля для обоих режимов
            ret.ValidateRequired(this.Prop_LocatorValue, ActivityStrings.Field_LocatorValue, "Значение локатора обязательно");

            return ret;
        }
    }
}
