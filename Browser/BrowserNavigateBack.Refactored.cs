// =============================================================================
// BrowserNavigateBack.cs — активность «Навигация в браузере».
//
// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using Primo.MIA.Browser;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для навигации по URL в браузере.
    /// </summary>
    public class BrowserNavigateBack : BrowserActivityBase<BrowserNavigate>
    {
        #region Properties and Configuration

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

        #endregion

        #region Input Properties

        private NavigateMode _propMode;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName("Режим")]
        public NavigateMode Prop_Mode
        {
            get => _propMode;
            set { _propMode = value; InvokePropertyChanged(this, nameof(Prop_Mode)); }
        }

        private string _propSessionId;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_SessionId)]
        public string Prop_SessionId
        {
            get => _propSessionId;
            set { _propSessionId = value; InvokePropertyChanged(this, nameof(Prop_SessionId)); }
        }

        private string _propUrl;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main)]
        [System.ComponentModel.DisplayName(ActivityStrings.Field_NavigateUrl)]
        public string Prop_Url
        {
            get => _propUrl;
            set { _propUrl = value; InvokePropertyChanged(this, nameof(Prop_Url)); }
        }

        #endregion

        #region Constructor

        public BrowserNavigateBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_BrowserNavigate;
            sdkComponentHelp =
                "Выполняет навигацию в браузере.\n" +
                "\n" +
                "── Режимы работы ──────────────────────────────\n" +
                "ToUrl    — перейти по указанному URL\n" +
                "Back     — назад в истории браузера\n" +
                "Forward  — вперед в истории браузера\n" +
                "Refresh  — обновить текущую страницу\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "Режим     — тип навигации\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "URL       — адрес для перехода (только для ToUrl)";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<NavigateMode>("Prop_Mode", "Режим навигации"),
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.String("Prop_Url", "URL для перехода (для ToUrl)")
            };

            InitClass(container);

            // Defaults
            Prop_Mode = NavigateMode.ToUrl;
            Prop_SessionId = "\"\"";
            Prop_Url = "\"https://example.com\"";
        }

        #endregion

        #region Main Execution

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return SafeExecute(() =>
            {
                // Получение драйвера через базовый класс
                IWebDriver driver = GetDriverFromContext(sd, nameof(Prop_SessionId));

                // Выполнение навигации
                string message = PerformNavigation(sd, driver);

                // Небольшая пауза для стабилизации
                System.Threading.Thread.Sleep(500);

                return CreateSuccessResult(message);
            },
            $"Навигация — {Prop_Mode}");
        }

        #endregion

        #region Navigation Actions

        /// <summary>
        /// Выполняет навигацию в соответствии с выбранным режимом.
        /// </summary>
        private string PerformNavigation(ScriptingData sd, IWebDriver driver)
        {
            switch (Prop_Mode)
            {
                case NavigateMode.ToUrl:
                    return NavigateToUrl(sd, driver);

                case NavigateMode.Back:
                    return NavigateBack(driver);

                case NavigateMode.Forward:
                    return NavigateForward(driver);

                case NavigateMode.Refresh:
                    return RefreshPage(driver);

                default:
                    throw new ArgumentOutOfRangeException(nameof(Prop_Mode),
                        $"Неизвестный режим навигации: {Prop_Mode}");
            }
        }

        /// <summary>
        /// Переходит по указанному URL.
        /// </summary>
        private string NavigateToUrl(ScriptingData sd, IWebDriver driver)
        {
            string url = GetPropertyValue<string>(Prop_Url, nameof(Prop_Url), sd);
            
            // Guard clause
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL не может быть пустым");

            // Валидация URL
            ValidateUrl(url);

            driver.Navigate().GoToUrl(url);
            LogInfo($"Переход на URL: {url}");
            
            return $"[Навигация] Переход на {url}";
        }

        /// <summary>
        /// Переходит назад в истории браузера.
        /// </summary>
        private string NavigateBack(IWebDriver driver)
        {
            driver.Navigate().Back();
            LogInfo("Переход назад в истории");
            
            return "[Навигация] Назад в истории";
        }

        /// <summary>
        /// Переходит вперед в истории браузера.
        /// </summary>
        private string NavigateForward(IWebDriver driver)
        {
            driver.Navigate().Forward();
            LogInfo("Переход вперед в истории");
            
            return "[Навигация] Вперед в истории";
        }

        /// <summary>
        /// Обновляет текущую страницу.
        /// </summary>
        private string RefreshPage(IWebDriver driver)
        {
            driver.Navigate().Refresh();
            LogInfo("Обновление страницы");
            
            return "[Навигация] Обновление страницы";
        }

        #endregion

        #region Validation

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            // URL обязателен только для режима ToUrl
            if (Prop_Mode == NavigateMode.ToUrl)
            {
                ret.ValidateRequired(Prop_Url, "URL", "URL обязателен для режима ToUrl");
            }

            return ret;
        }

        #endregion
    }
}
