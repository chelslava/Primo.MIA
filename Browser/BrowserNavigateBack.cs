// =============================================================================
// BrowserNavigateBack.cs — активность «Навигация в браузере».
//
// Переходит по указанному URL в открытом браузере.
// Поддерживает ожидание загрузки страницы.
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
    /// Активность для навигации по URL в браузере.
    /// </summary>
    public class BrowserNavigateBack : PrimoComponentTO<BrowserNavigate>
    {
        public override string GroupName
        {
            get => ActivityCategories.Browser;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 60000; // 60 секунд на загрузку страницы
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

        private string _propUrl;
        /// <summary>URL для перехода.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_NavigateUrl)]
        public string Prop_Url
        {
            get => _propUrl;
            set { _propUrl = value; InvokePropertyChanged(this, "Prop_Url"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public BrowserNavigateBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_BrowserNavigate;
            sdkComponentHelp =
                "Переходит по указанному URL в браузере.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "URL       — адрес для перехода";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.String("Prop_Url", "URL для перехода")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_Url = "\"https://example.com\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        /// <summary>
        /// Выполняет навигацию по указанному URL в браузере.
        /// Получает драйвер из репозитория сессий и вызывает Navigate().GoToUrl().
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Чтение параметров
                string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
                string url = GetPropertyValue<string>(this.Prop_Url, "Prop_Url", sd);

                if (string.IsNullOrWhiteSpace(sessionId))
                    throw new ArgumentException("ID сессии не может быть пустым");

                if (string.IsNullOrWhiteSpace(url))
                    throw new ArgumentException("URL не может быть пустым");

                // Получение драйвера из репозитория сессий
                var driver = SeleniumHelper.GetDriver(sessionId);

                // Выполнение навигации
                driver.Navigate().GoToUrl(url);

                // Ожидание загрузки страницы (неявное через Selenium)
                System.Threading.Thread.Sleep(500);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Навигация] Переход на {url}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [Навигация]: {ex.Message}"
                };
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        /// <summary>
        /// Проверяет обязательность заполнения ID сессии и URL.
        /// </summary>
        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_SessionId, "ID сессии", "ID сессии обязателен");
            ret.ValidateRequired(this.Prop_Url, "URL", "URL обязателен");
            return ret;
        }
    }
}
