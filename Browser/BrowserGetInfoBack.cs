// =============================================================================
// BrowserGetInfoBack.cs — активность «Получить информацию о браузере».
//
// Извлекает информацию о текущем состоянии браузера:
//   - Текущий URL
//   - Заголовок страницы
//   - HTML код страницы
//
// Используется для проверки состояния и отладки.
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
    /// Активность для получения информации о текущем состоянии браузера.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class BrowserGetInfoBack : BrowserActivityBase<BrowserGetInfo>
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

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propCurrentUrl;
        /// <summary>Текущий URL страницы.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CurrentUrl)]
        public string Prop_CurrentUrl
        {
            get => _propCurrentUrl;
            set { _propCurrentUrl = value; InvokePropertyChanged(this, "Prop_CurrentUrl"); }
        }

        private string _propPageTitle;
        /// <summary>Заголовок страницы.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_PageTitle)]
        public string Prop_PageTitle
        {
            get => _propPageTitle;
            set { _propPageTitle = value; InvokePropertyChanged(this, "Prop_PageTitle"); }
        }

        private string _propPageSource;
        /// <summary>HTML код страницы.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_PageSource)]
        public string Prop_PageSource
        {
            get => _propPageSource;
            set { _propPageSource = value; InvokePropertyChanged(this, "Prop_PageSource"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public BrowserGetInfoBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_BrowserGetInfo;
            sdkComponentHelp =
                "Получает информацию о текущем состоянии браузера.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Текущий URL      — адрес текущей страницы\n" +
                "Заголовок        — title страницы\n" +
                "HTML код         — полный исходный код страницы";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Variable<string>("Prop_CurrentUrl", "Текущий URL страницы"),
                PropertyBuilder.Variable<string>("Prop_PageTitle", "Заголовок страницы"),
                PropertyBuilder.Variable<string>("Prop_PageSource", "HTML код страницы")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return SafeExecute(() =>
            {
                // Получаем sessionId
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);

                // Получение драйвера через базовый класс
                IWebDriver driver = GetDriverFromContext(sessionId);

                // Извлечение информации
                string currentUrl = driver.Url ?? string.Empty;
                string pageTitle = driver.Title ?? string.Empty;
                string pageSource = driver.PageSource ?? string.Empty;

                // Запись в выходные переменные
                if (!string.IsNullOrWhiteSpace(Prop_CurrentUrl))
                    SetVariableValue(Prop_CurrentUrl, currentUrl, sd);

                if (!string.IsNullOrWhiteSpace(Prop_PageTitle))
                    SetVariableValue(Prop_PageTitle, pageTitle, sd);

                if (!string.IsNullOrWhiteSpace(Prop_PageSource))
                    SetVariableValue(Prop_PageSource, pageSource, sd);

                // Логирование через сервис
                Logger.LogInfo(sdkComponentName, "Информация о браузере получена: URL={0}, Title={1}", currentUrl, pageTitle);

                return CreateSuccessResult($"[Получить информацию] URL: {currentUrl}, Title: {pageTitle}");
            }, "Получить информацию");
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            return ret;
        }
    }
}
