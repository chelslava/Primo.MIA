// =============================================================================
// BrowserOpenBack.cs — активность «Открыть браузер».
//
// Инициализирует новую сессию браузера с заданными параметрами.
// Поддерживает Chrome, Firefox, Edge с настройкой headless режима,
// инкогнито и других опций.
//
// Поддерживаемые браузеры:
//   Chrome   — Google Chrome / Chromium
//   Firefox  — Mozilla Firefox
//   Edge     — Microsoft Edge
//
// ВАЖНО: Сессия сохраняется в RepoDict и должна быть закрыта через BrowserClose.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Edge;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для открытия браузера и создания новой сессии.
    /// Поддерживает настройку headless режима, инкогнито и других опций.
    /// </summary>
    public class BrowserOpenBack : PrimoComponentTO<BrowserOpen>
    {
        // ── Константы и свойства SDK ───────────────────────────────────

        public override string GroupName
        {
            get => ActivityCategories.Browser;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 60000; // 60 секунд на запуск браузера
            set { }
        }

        // ── Входные параметры ──────────────────────────────────────────

        private BrowserType _propBrowserType;
        /// <summary>Тип браузера для запуска.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_BrowserType)]
        public BrowserType Prop_BrowserType
        {
            get => _propBrowserType;
            set { _propBrowserType = value; InvokePropertyChanged(this, "Prop_BrowserType"); }
        }

        private bool _propHeadless;
        /// <summary>Запустить браузер в headless режиме (без GUI).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_BrowserOptions),
         System.ComponentModel.DisplayName(ActivityStrings.Field_Headless)]
        public bool Prop_Headless
        {
            get => _propHeadless;
            set { _propHeadless = value; InvokePropertyChanged(this, "Prop_Headless"); }
        }

        private bool _propIncognitoMode;
        /// <summary>Запустить браузер в режиме инкогнито.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_BrowserOptions),
         System.ComponentModel.DisplayName(ActivityStrings.Field_IncognitoMode)]
        public bool Prop_IncognitoMode
        {
            get => _propIncognitoMode;
            set { _propIncognitoMode = value; InvokePropertyChanged(this, "Prop_IncognitoMode"); }
        }

        private bool _propDisableImages;
        /// <summary>Отключить загрузку изображений для ускорения.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_BrowserOptions),
         System.ComponentModel.DisplayName(ActivityStrings.Field_DisableImages)]
        public bool Prop_DisableImages
        {
            get => _propDisableImages;
            set { _propDisableImages = value; InvokePropertyChanged(this, "Prop_DisableImages"); }
        }

        private string _propUserAgent;
        /// <summary>Пользовательский User-Agent (опционально).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_BrowserOptions),
         System.ComponentModel.DisplayName(ActivityStrings.Field_UserAgent)]
        public string Prop_UserAgent
        {
            get => _propUserAgent;
            set { _propUserAgent = value; InvokePropertyChanged(this, "Prop_UserAgent"); }
        }

        private string _propDriverPath;
        /// <summary>Путь к драйверу Selenium (chromedriver.exe, geckodriver.exe и т.д.). Если не указан, используется драйвер из PATH.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_BrowserOptions),
         System.ComponentModel.DisplayName(ActivityStrings.Field_DriverPath)]
        public string Prop_DriverPath
        {
            get => _propDriverPath;
            set { _propDriverPath = value; InvokePropertyChanged(this, "Prop_DriverPath"); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propSessionId;
        /// <summary>ID созданной сессии браузера.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_SessionId)]
        public string Prop_SessionId
        {
            get => _propSessionId;
            set { _propSessionId = value; InvokePropertyChanged(this, "Prop_SessionId"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public BrowserOpenBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_BrowserOpen;
            sdkComponentHelp =
                "Открывает новый браузер и создаёт сессию для автоматизации.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "Тип браузера     — Chrome, Firefox или Edge\n" +
                "\n" +
                "── Опции браузера ─────────────────────────────\n" +
                "Headless режим   — запуск без GUI (фоновый режим)\n" +
                "Режим инкогнито  — приватный режим браузера\n" +
                "Отключить изображения — ускорение загрузки\n" +
                "User-Agent       — пользовательский UA строка\n" +
                "Путь к драйверу  — путь к chromedriver.exe / geckodriver.exe\n" +
                "                    (если не указан, используется из PATH)\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "ID сессии        — уникальный идентификатор для других активностей";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<BrowserType>("Prop_BrowserType", "Тип браузера для запуска"),
                PropertyBuilder.BooleanObject("Prop_Headless", "Запустить в headless режиме"),
                PropertyBuilder.BooleanObject("Prop_IncognitoMode", "Запустить в режиме инкогнито"),
                PropertyBuilder.BooleanObject("Prop_DisableImages", "Отключить загрузку изображений"),
                PropertyBuilder.String("Prop_UserAgent", "Пользовательский User-Agent"),
                PropertyBuilder.FileSelector("Prop_DriverPath", "Путь к драйверу Selenium"),
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID созданной сессии")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_BrowserType = BrowserType.Chrome;
            this.Prop_Headless = false;
            this.Prop_IncognitoMode = false;
            this.Prop_DisableImages = false;
            this.Prop_UserAgent = "\"\"";
            this.Prop_DriverPath = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        /// <summary>
        /// Основной метод выполнения — создаёт браузер и сохраняет сессию.
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Чтение параметров ──────────────────────────────────
                string userAgent = GetPropertyValue<string>(this.Prop_UserAgent, "Prop_UserAgent", sd) ?? string.Empty;
                string driverPath = GetPropertyValue<string>(this.Prop_DriverPath, "Prop_DriverPath", sd) ?? string.Empty;

                // ── Генерация уникального ID сессии ────────────────────
                string sessionId = SeleniumHelper.GenerateSessionId();

                // ── Создание драйвера в зависимости от типа браузера ──
                IWebDriver driver = CreateDriver(this.Prop_BrowserType, userAgent, driverPath);

                // ── Сохранение в RepoDict ──────────────────────────────
                RepoDict.Set(sessionId, driver);

                // ── Запись ID сессии в выходную переменную ────────────
                if (!string.IsNullOrWhiteSpace(this.Prop_SessionId))
                    SetVariableValue(this.Prop_SessionId, sessionId, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Открыть браузер] {this.Prop_BrowserType} → {sessionId}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [Открыть браузер]: {ex.Message}"
                };
            }
        }

        // ── Приватные методы — реализация логики ──────────────────────

        /// <summary>
        /// Создаёт экземпляр WebDriver в зависимости от типа браузера.
        /// Применяет все настроенные опции (headless, incognito и т.д.).
        /// </summary>
        private IWebDriver CreateDriver(BrowserType browserType, string userAgent, string driverPath)
        {
            switch (browserType)
            {
                case BrowserType.Chrome:
                    return CreateChromeDriver(userAgent, driverPath);

                case BrowserType.Firefox:
                    return CreateFirefoxDriver(userAgent, driverPath);

                case BrowserType.Edge:
                    return CreateEdgeDriver(userAgent, driverPath);

                default:
                    throw new NotSupportedException($"Браузер {browserType} не поддерживается");
            }
        }

        /// <summary>
        /// Создаёт Chrome WebDriver с настроенными опциями.
        /// </summary>
        private IWebDriver CreateChromeDriver(string userAgent, string driverPath)
        {
            var options = new ChromeOptions();

            // Headless режим
            if (this.Prop_Headless)
                options.AddArgument("--headless");

            // Режим инкогнито
            if (this.Prop_IncognitoMode)
                options.AddArgument("--incognito");

            // Отключение изображений
            if (this.Prop_DisableImages)
            {
                options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
            }

            // Пользовательский User-Agent
            if (!string.IsNullOrWhiteSpace(userAgent))
                options.AddArgument($"--user-agent={userAgent}");

            // Дополнительные опции для стабильности
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");

            // Создание драйвера с указанием пути или без
            if (!string.IsNullOrWhiteSpace(driverPath))
                return new ChromeDriver(driverPath, options);
            else
                return new ChromeDriver(options);
        }

        /// <summary>
        /// Создаёт Firefox WebDriver с настроенными опциями.
        /// </summary>
        private IWebDriver CreateFirefoxDriver(string userAgent, string driverPath)
        {
            var options = new FirefoxOptions();

            // Headless режим
            if (this.Prop_Headless)
                options.AddArgument("--headless");

            // Режим инкогнито
            if (this.Prop_IncognitoMode)
                options.AddArgument("-private");

            // Отключение изображений
            if (this.Prop_DisableImages)
            {
                options.SetPreference("permissions.default.image", 2);
            }

            // Пользовательский User-Agent
            if (!string.IsNullOrWhiteSpace(userAgent))
                options.SetPreference("general.useragent.override", userAgent);

            // Создание драйвера с указанием пути или без
            if (!string.IsNullOrWhiteSpace(driverPath))
                return new FirefoxDriver(driverPath, options);
            else
                return new FirefoxDriver(options);
        }

        /// <summary>
        /// Создаёт Edge WebDriver с настроенными опциями.
        /// </summary>
        private IWebDriver CreateEdgeDriver(string userAgent, string driverPath)
        {
            var options = new EdgeOptions();

            // Headless режим
            if (this.Prop_Headless)
                options.AddArgument("--headless");

            // Режим инкогнито
            if (this.Prop_IncognitoMode)
                options.AddArgument("--inprivate");

            // Отключение изображений
            if (this.Prop_DisableImages)
            {
                options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
            }

            // Пользовательский User-Agent
            if (!string.IsNullOrWhiteSpace(userAgent))
                options.AddArgument($"--user-agent={userAgent}");

            // Дополнительные опции для стабильности
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");

            // Создание драйвера с указанием пути или без
            if (!string.IsNullOrWhiteSpace(driverPath))
                return new EdgeDriver(driverPath, options);
            else
                return new EdgeDriver(options);
        }

        // ── Валидация ──────────────────────────────────────────────────

        /// <summary>
        /// Валидирует обязательные параметры активности.
        /// </summary>
        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            // Валидация входных параметров
            // Prop_SessionId — выходная переменная, не требует валидации

            return ret;
        }
    }
}
