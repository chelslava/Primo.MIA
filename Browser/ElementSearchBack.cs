// =============================================================================
// ElementSearchBack.cs — универсальная активность «Поиск и проверка элементов».
//
// Объединяет функциональность поиска, проверки существования, видимости,
// кликабельности и состояния элементов в одной активности.
//
// Заменяет активности:
//   - ElementExistsBack
//   - ElementIsVisibleBack  
//   - ElementWaitAndCheckBack (частично)
//
// Режимы работы:
//   - Exists: проверка существования в DOM
//   - IsVisible: проверка видимости
//   - IsClickable: проверка кликабельности
//   - WaitAndCheck: ожидание и полная проверка состояния
//   - FindAll: поиск всех элементов по локатору
//   - Count: подсчет количества элементов
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Универсальная активность для поиска и проверки элементов на странице.
    /// Объединяет функциональность нескольких специализированных активностей.
    /// </summary>
    public class ElementSearchBack : BrowserActivityBase<ElementSearch>
    {
        public override string GroupName
        {
            get => ActivityCategories.Browser;
            protected set { }
        }

        /// <summary>
        /// Таймаут SDK в миллисекундах.
        /// Вычисляется автоматически как Prop_WaitTimeout + 5 секунд.
        /// </summary>
        protected override int sdkTimeOut
        {
            get
            {
                string waitStr = this.Prop_WaitTimeout?.Trim('"') ?? "10";
                int waitSec = int.TryParse(waitStr, out int w) ? w : 10;
                return (waitSec + 5) * 1000;
            }
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
        /// <summary>ID элемента для проверки (если элемент уже найден).</summary>
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
        /// <summary>Тип локатора для поиска элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Locator),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorType)]
        public ElementLocatorType Prop_LocatorType
        {
            get => (ElementLocatorType)Enum.Parse(typeof(ElementLocatorType), _propLocatorType ?? "Id");
            set { _propLocatorType = value.ToString(); InvokePropertyChanged(this, "Prop_LocatorType"); }
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

        private string _propSearchMode;
        /// <summary>Режим поиска и проверки элемента.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Режим поиска")]
        public ElementSearchMode Prop_SearchMode
        {
            get => (ElementSearchMode)Enum.Parse(typeof(ElementSearchMode), _propSearchMode ?? "Exists");
            set { _propSearchMode = value.ToString(); InvokePropertyChanged(this, "Prop_SearchMode"); }
        }

        private string _propWaitMode;
        /// <summary>Режим ожидания элемента (для режимов с ожиданием).</summary>
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

        private string _propElementFound;
        /// <summary>Элемент найден.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Элемент найден")]
        public string Prop_ElementFound
        {
            get => _propElementFound;
            set { _propElementFound = value; InvokePropertyChanged(this, "Prop_ElementFound"); }
        }

        private string _propIsVisible;
        /// <summary>Элемент видим.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_IsVisible)]
        public string Prop_IsVisible
        {
            get => _propIsVisible;
            set { _propIsVisible = value; InvokePropertyChanged(this, "Prop_IsVisible"); }
        }

        private string _propIsEnabled;
        /// <summary>Элемент включён.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_IsEnabled)]
        public string Prop_IsEnabled
        {
            get => _propIsEnabled;
            set { _propIsEnabled = value; InvokePropertyChanged(this, "Prop_IsEnabled"); }
        }

        private string _propIsSelected;
        /// <summary>Элемент выбран.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_IsSelected)]
        public string Prop_IsSelected
        {
            get => _propIsSelected;
            set { _propIsSelected = value; InvokePropertyChanged(this, "Prop_IsSelected"); }
        }

        private string _propIsClickable;
        /// <summary>Элемент кликабельный.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Элемент кликабельный")]
        public string Prop_IsClickable
        {
            get => _propIsClickable;
            set { _propIsClickable = value; InvokePropertyChanged(this, "Prop_IsClickable"); }
        }

        private string _propElementCount;
        /// <summary>Количество найденных элементов.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Количество элементов")]
        public string Prop_ElementCount
        {
            get => _propElementCount;
            set { _propElementCount = value; InvokePropertyChanged(this, "Prop_ElementCount"); }
        }

        private string _propElementsFound;
        /// <summary>Список найденных элементов (ID).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Найденные элементы")]
        public string Prop_ElementsFound
        {
            get => _propElementsFound;
            set { _propElementsFound = value; InvokePropertyChanged(this, "Prop_ElementsFound"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ElementSearchBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Поиск и проверка элементов";
            sdkComponentHelp =
                "Универсальная активность для поиска и проверки элементов.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии   — идентификатор сессии браузера\n" +
                "ID элемента — идентификатор элемента (если уже найден)\n" +
                "Режим поиска — тип операции поиска/проверки\n" +
                "\n" +
                "── Локатор (альтернатива ID элемента) ────────\n" +
                "Тип локатора — способ поиска элемента\n" +
                "Значение локатора — конкретное значение для поиска\n" +
                "\n" +
                "── Ожидание ──────────────────────────────────\n" +
                "Режим ожидания — стратегия ожидания элемента\n" +
                "Таймаут (сек) — время ожидания элемента\n" +
                "\n" +
                "── Режимы поиска ──────────────────────────────\n" +
                "Exists      — проверить существование в DOM\n" +
                "IsVisible   — проверить видимость на странице\n" +
                "IsClickable — проверить кликабельность\n" +
                "WaitAndCheck— ожидать и проверить состояние\n" +
                "FindAll     — найти все элементы по локатору\n" +
                "Count       — подсчитать количество элементов\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Элемент найден — элемент обнаружен\n" +
                "Видим    — элемент отображается (для режимов с проверкой)\n" +
                "Включён  — элемент активен (для режимов с проверкой)\n" +
                "Выбран   — элемент выбран (для режимов с проверкой)\n" +
                "Кликабельный — элемент кликабельный (для IsClickable)\n" +
                "Количество — число найденных элементов (для Count/FindAll)\n" +
                "Найденные элементы — список ID элементов (для FindAll)";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId",   "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_ElementId",   "ID элемента (если уже найден)"),
                PropertyBuilder.Enum<ElementLocatorType>("Prop_LocatorType", "Тип локатора"),
                PropertyBuilder.String("Prop_LocatorValue",          "Значение локатора"),
                PropertyBuilder.Enum<ElementSearchMode>("Prop_SearchMode", "Режим поиска"),
                PropertyBuilder.Enum<ElementWaitMode>("Prop_WaitMode", "Режим ожидания"),
                PropertyBuilder.Int("Prop_WaitTimeout",              "Таймаут ожидания элемента (сек)"),
                PropertyBuilder.Variable<bool>("Prop_ElementFound",  "Элемент найден"),
                PropertyBuilder.Variable<bool>("Prop_IsVisible",     "Элемент видим"),
                PropertyBuilder.Variable<bool>("Prop_IsEnabled",     "Элемент включён"),
                PropertyBuilder.Variable<bool>("Prop_IsSelected",    "Элемент выбран"),
                PropertyBuilder.Variable<bool>("Prop_IsClickable",   "Элемент кликабельный"),
                PropertyBuilder.Variable<int>("Prop_ElementCount",   "Количество элементов"),
                PropertyBuilder.Variable<List<string>>("Prop_ElementsFound", "Найденные элементы")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_SessionId = "\"\"";
            this.Prop_ElementId = "\"\"";
            this.Prop_LocatorType = ElementLocatorType.Id;
            this.Prop_LocatorValue = "\"\"";
            this.Prop_SearchMode = ElementSearchMode.Exists;
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
                    "Начинается поиск элементов для сессии: {0}, режим: {1}", 
                    sessionId, Prop_SearchMode);

                IWebDriver driver = GetDriverFromContext(sessionId);

                // Парсим таймаут
                string waitStr = GetPropertyValue<string>(Prop_WaitTimeout, "Prop_WaitTimeout", sd) ?? "10";
                int waitTimeout = int.TryParse(waitStr, out int wt) ? wt : 10;
                ValidatePositive(waitTimeout, "Prop_WaitTimeout");

                // Выполняем операцию в зависимости от режима
                switch (Prop_SearchMode)
                {
                    case ElementSearchMode.Exists:
                        return ExecuteExistsMode(driver, elementId, locatorValue, waitTimeout, sd);

                    case ElementSearchMode.IsVisible:
                        return ExecuteIsVisibleMode(driver, elementId, locatorValue, waitTimeout, sd);

                    case ElementSearchMode.IsClickable:
                        return ExecuteIsClickableMode(driver, elementId, locatorValue, waitTimeout, sd);

                    case ElementSearchMode.WaitAndCheck:
                        return ExecuteWaitAndCheckMode(driver, elementId, locatorValue, waitTimeout, sd);

                    case ElementSearchMode.FindAll:
                        return ExecuteFindAllMode(driver, locatorValue, waitTimeout, sd);

                    case ElementSearchMode.Count:
                        return ExecuteCountMode(driver, locatorValue, waitTimeout, sd);

                    default:
                        throw new ArgumentException($"Неподдерживаемый режим поиска: {Prop_SearchMode}");
                }

            }, "Поиск и проверка элементов");
        }

        // ── Методы выполнения различных режимов ────────────────────────

        /// <summary>
        /// Режим проверки существования элемента.
        /// </summary>
        private ExecutionResult ExecuteExistsMode(IWebDriver driver, string elementId, string locatorValue, int waitTimeout, ScriptingData sd)
        {
            bool found = false;

            if (!string.IsNullOrWhiteSpace(elementId))
            {
                // Проверяем элемент из репозитория
                try
                {
                    var element = this.ElementRepository.GetElement(elementId);
                    found = element != null;
                }
                catch
                {
                    found = false;
                }
            }
            else if (!string.IsNullOrWhiteSpace(locatorValue))
            {
                // Ищем по локатору
                var elementLocator = new ElementLocator();
                var locatorType = ConvertLocatorType(Prop_LocatorType);
                var element = elementLocator.TryFindElementWithWaitMode(
                    driver, locatorType, locatorValue, waitTimeout, ElementWaitMode.Present);
                found = element != null;
            }

            SetVariableValue(Prop_ElementFound, found, sd);

            Logger.LogInfo(sdkComponentName, "Проверка существования: элемент {0}", found ? "найден" : "не найден");
            return CreateSuccessResult($"[Поиск элементов] Существование: {found}");
        }

        /// <summary>
        /// Режим проверки видимости элемента.
        /// </summary>
        private ExecutionResult ExecuteIsVisibleMode(IWebDriver driver, string elementId, string locatorValue, int waitTimeout, ScriptingData sd)
        {
            bool found = false;
            bool visible = false;

            var element = FindElement(driver, elementId, locatorValue, waitTimeout, ElementWaitMode.Present);
            if (element != null)
            {
                found = true;
                visible = element.Displayed;
            }

            SetVariableValue(Prop_ElementFound, found, sd);
            SetVariableValue(Prop_IsVisible, visible, sd);

            Logger.LogInfo(sdkComponentName, "Проверка видимости: найден={0}, видим={1}", found, visible);
            return CreateSuccessResult($"[Поиск элементов] Найден: {found}, Видим: {visible}");
        }

        /// <summary>
        /// Режим проверки кликабельности элемента.
        /// </summary>
        private ExecutionResult ExecuteIsClickableMode(IWebDriver driver, string elementId, string locatorValue, int waitTimeout, ScriptingData sd)
        {
            bool found = false;
            bool visible = false;
            bool enabled = false;
            bool clickable = false;

            var element = FindElement(driver, elementId, locatorValue, waitTimeout, ElementWaitMode.Present);
            if (element != null)
            {
                found = true;
                visible = element.Displayed;
                enabled = element.Enabled;
                clickable = visible && enabled;
            }

            SetVariableValue(Prop_ElementFound, found, sd);
            SetVariableValue(Prop_IsVisible, visible, sd);
            SetVariableValue(Prop_IsEnabled, enabled, sd);
            SetVariableValue(Prop_IsClickable, clickable, sd);

            Logger.LogInfo(sdkComponentName, "Проверка кликабельности: найден={0}, кликабельный={1}", found, clickable);
            return CreateSuccessResult($"[Поиск элементов] Найден: {found}, Кликабельный: {clickable}");
        }

        /// <summary>
        /// Режим ожидания и полной проверки состояния элемента.
        /// </summary>
        private ExecutionResult ExecuteWaitAndCheckMode(IWebDriver driver, string elementId, string locatorValue, int waitTimeout, ScriptingData sd)
        {
            bool found = false;
            bool visible = false;
            bool enabled = false;
            bool selected = false;

            var element = FindElement(driver, elementId, locatorValue, waitTimeout, Prop_WaitMode);
            if (element != null)
            {
                found = true;
                visible = element.Displayed;
                enabled = element.Enabled;
                selected = element.Selected;
            }

            SetVariableValue(Prop_ElementFound, found, sd);
            SetVariableValue(Prop_IsVisible, visible, sd);
            SetVariableValue(Prop_IsEnabled, enabled, sd);
            SetVariableValue(Prop_IsSelected, selected, sd);

            Logger.LogInfo(sdkComponentName, 
                "Ожидание и проверка: найден={0}, видим={1}, включён={2}, выбран={3}", 
                found, visible, enabled, selected);
            return CreateSuccessResult(
                $"[Поиск элементов] Найден: {found}, Видим: {visible}, Включён: {enabled}, Выбран: {selected}");
        }

        /// <summary>
        /// Режим поиска всех элементов по локатору.
        /// </summary>
        private ExecutionResult ExecuteFindAllMode(IWebDriver driver, string locatorValue, int waitTimeout, ScriptingData sd)
        {
            Guard.NotNullOrWhiteSpace(locatorValue, nameof(locatorValue));

            var elementLocator = new ElementLocator();
            var locatorType = ConvertLocatorType(Prop_LocatorType);
            var elements = elementLocator.FindElements(driver, locatorType, locatorValue);

            int count = elements.Count;
            var elementIds = new List<string>();

            // Сохраняем найденные элементы в репозиторий и собираем их ID
            int i = 0;
            foreach (var element in elements)
            {
                string elementId = $"FoundElement_{Guid.NewGuid():N}";
                this.ElementRepository.StoreElement(elementId, element);
                elementIds.Add(elementId);
                i++;
            }

            SetVariableValue(Prop_ElementCount, count, sd);
            SetVariableValue(Prop_ElementsFound, elementIds, sd);

            Logger.LogInfo(sdkComponentName, "Найдено элементов: {0}", count);
            return CreateSuccessResult($"[Поиск элементов] Найдено элементов: {count}");
        }

        /// <summary>
        /// Режим подсчета количества элементов.
        /// </summary>
        private ExecutionResult ExecuteCountMode(IWebDriver driver, string locatorValue, int waitTimeout, ScriptingData sd)
        {
            Guard.NotNullOrWhiteSpace(locatorValue, nameof(locatorValue));

            var elementLocator = new ElementLocator();
            var locatorType = ConvertLocatorType(Prop_LocatorType);
            var elements = elementLocator.FindElements(driver, locatorType, locatorValue);

            int count = elements.Count;
            SetVariableValue(Prop_ElementCount, count, sd);

            Logger.LogInfo(sdkComponentName, "Подсчитано элементов: {0}", count);
            return CreateSuccessResult($"[Поиск элементов] Количество: {count}");
        }

        /// <summary>
        /// Универсальный метод поиска элемента.
        /// </summary>
        private IWebElement FindElement(IWebDriver driver, string elementId, string locatorValue, int waitTimeout, ElementWaitMode waitMode)
        {
            if (!string.IsNullOrWhiteSpace(elementId))
            {
                try
                {
                    return this.ElementRepository.GetElement(elementId);
                }
                catch
                {
                    return null;
                }
            }

            if (!string.IsNullOrWhiteSpace(locatorValue))
            {
                var elementLocator = new ElementLocator();
                var locatorType = ConvertLocatorType(Prop_LocatorType);
                return elementLocator.TryFindElementWithWaitMode(
                    driver, locatorType, locatorValue, waitTimeout, waitMode);
            }

            return null;
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            // Для режимов FindAll и Count обязательно нужен локатор
            if (Prop_SearchMode == ElementSearchMode.FindAll || Prop_SearchMode == ElementSearchMode.Count)
            {
                if (string.IsNullOrWhiteSpace(this.Prop_LocatorValue))
                {
                    ret.Items.Add(new ValidationResult.ValidationItem()
                    {
                        PropertyName = "Prop_LocatorValue",
                        Error = $"Для режима {Prop_SearchMode} необходимо указать локатор"
                    });
                }
            }
            else
            {
                // Для остальных режимов нужен либо ID элемента, либо локатор
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
            }

            return ret;
        }
    }
}
