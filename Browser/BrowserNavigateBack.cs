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

namespace Primo.MIA
{
    /// <summary>
    /// Активность для навигации по URL в браузере.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class BrowserNavigateBack : BrowserActivityBase<BrowserNavigate>
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

        private NavigateMode _propMode;
        /// <summary>Режим навигации.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Режим")]
        public NavigateMode Prop_Mode
        {
            get => _propMode;
            set { _propMode = value; InvokePropertyChanged(this, "Prop_Mode"); }
        }

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
        /// <summary>URL для перехода (для режима ToUrl).</summary>
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

            Prop_Mode = NavigateMode.ToUrl;
            Prop_SessionId = "\"\"";
            Prop_Url = "\"https://example.com\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        /// <summary>
        /// Выполняет навигацию в браузере в зависимости от выбранного режима.
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            string message = string.Empty;

            var result = SafeExecute(() =>
            {
                // Получаем sessionId
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);

                // Получение драйвера через базовый класс
                IWebDriver driver = GetDriverFromContext(sessionId);

                // Логирование начала операции
                Logger.LogInfo(sdkComponentName, "Начинается навигация {0} для сессии: {1}", Prop_Mode, sessionId);

                // Выполнение навигации в зависимости от режима
                switch (Prop_Mode)
                {
                    case NavigateMode.ToUrl:
                        string url = GetPropertyValue<string>(Prop_Url, "Prop_Url", sd);
                        ValidateNotEmpty(url, nameof(Prop_Url));
                        ValidateUrl(url);

                        driver.Navigate().GoToUrl(url);
                        message = $"[Навигация] Переход на {url}";
                        Logger.LogInfo(sdkComponentName, "Выполнен переход на URL: {0}", url);
                        break;

                    case NavigateMode.Back:
                        driver.Navigate().Back();
                        message = "[Навигация] Назад в истории";
                        Logger.LogInfo(sdkComponentName, "Выполнен переход назад в истории");
                        break;

                    case NavigateMode.Forward:
                        driver.Navigate().Forward();
                        message = "[Навигация] Вперед в истории";
                        Logger.LogInfo(sdkComponentName, "Выполнен переход вперед в истории");
                        break;

                    case NavigateMode.Refresh:
                        driver.Navigate().Refresh();
                        message = "[Навигация] Обновление страницы";
                        Logger.LogInfo(sdkComponentName, "Выполнено обновление страницы");
                        break;

                    default:
                        throw new ArgumentException($"Неизвестный режим навигации: {Prop_Mode}");
                }

                // Небольшая пауза для стабилизации
                System.Threading.Thread.Sleep(500);

                Logger.LogInfo(sdkComponentName, "Навигация {0} успешно завершена", Prop_Mode);

            }, "Навигация");

            if (result.IsSuccess)
                result.SuccessMessage = message;

            return result;
        }

        // ── Валидация ──────────────────────────────────────────────────

        /// <summary>
        /// Проверяет обязательность заполнения параметров в зависимости от режима.
        /// </summary>
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
    }
}
