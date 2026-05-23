// =============================================================================
// BrowserOpenBack.cs — активность-контейнер «Открыть браузер».
//
// Открывает браузер, регистрирует сессию в ambient-контексте и выполняет
// вложенные активности.
//
// ВАЖНО: Ambient-контекст (BrowserSessionContext) работает следующим образом:
//   1. BrowserOpen вызывает Push(sessionId, isContainerEntry: true)
//   2. Push() автоматически вызывает Pop() для предыдущей сессии (если есть)
//   3. Все дочерние активности с пустым Prop_SessionId автоматически получают
//      sessionId через SessionResolver.Resolve() → BrowserSessionContext.Current
//   4. При следующем вызове BrowserOpen контекст автоматически очищается
//
// Это обеспечивает:
//   - Автоматическую очистку при выходе из контейнера (через следующий Push)
//   - Возможность переключения между сессиями через AttachToSessionId
//   - Корректную работу последовательных контейнеров BrowserOpen
//
// BrowserClose также вызывает Pop() явно для немедленной очистки контекста.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Активность-контейнер для открытия браузера.
    /// SimpleAction открывает браузер и регистрирует сессию в контексте,
    /// после чего движок SDK выполняет вложенные активности.
    /// </summary>
    public class BrowserOpenBack : PrimoContainerCustom<BrowserOpen>
    {
        // ── Группа ────────────────────────────────────────────────────────────

        public override string GroupName
        {
            get => ActivityCategories.Browser;
            protected set { }
        }

        // ── Входные параметры ──────────────────────────────────────────────────

        #region Prop_BrowserType

        private BrowserType _propBrowserType;

        /// <summary>Тип браузера для запуска.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_BrowserType)]
        public BrowserType Prop_BrowserType
        {
            get => _propBrowserType;
            set { _propBrowserType = value; InvokePropertyChanged(this, nameof(Prop_BrowserType)); }
        }

        #endregion

        #region Prop_Headless

        private bool _propHeadless;

        /// <summary>Запустить браузер в headless режиме (без GUI).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_BrowserOptions),
         System.ComponentModel.DisplayName(ActivityStrings.Field_Headless)]
        public bool Prop_Headless
        {
            get => _propHeadless;
            set { _propHeadless = value; InvokePropertyChanged(this, nameof(Prop_Headless)); }
        }

        #endregion

        #region Prop_IncognitoMode

        private bool _propIncognitoMode;

        /// <summary>Запустить браузер в режиме инкогнито.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_BrowserOptions),
         System.ComponentModel.DisplayName(ActivityStrings.Field_IncognitoMode)]
        public bool Prop_IncognitoMode
        {
            get => _propIncognitoMode;
            set { _propIncognitoMode = value; InvokePropertyChanged(this, nameof(Prop_IncognitoMode)); }
        }

        #endregion

        #region Prop_DisableImages

        private bool _propDisableImages;

        /// <summary>Отключить загрузку изображений для ускорения.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_BrowserOptions),
         System.ComponentModel.DisplayName(ActivityStrings.Field_DisableImages)]
        public bool Prop_DisableImages
        {
            get => _propDisableImages;
            set { _propDisableImages = value; InvokePropertyChanged(this, nameof(Prop_DisableImages)); }
        }

        #endregion

        #region Prop_UserAgent

        private string _propUserAgent;

        /// <summary>Пользовательский User-Agent (опционально).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_BrowserOptions),
         System.ComponentModel.DisplayName(ActivityStrings.Field_UserAgent)]
        public string Prop_UserAgent
        {
            get => _propUserAgent;
            set { _propUserAgent = value; InvokePropertyChanged(this, nameof(Prop_UserAgent)); }
        }

        #endregion

        #region Prop_DriverPath

        private string _propDriverPath;

        /// <summary>
        /// Путь к драйверу Selenium (chromedriver.exe, geckodriver.exe и т.д.).
        /// Если не указан — используется драйвер из PATH.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_BrowserOptions),
         System.ComponentModel.DisplayName(ActivityStrings.Field_DriverPath)]
        public string Prop_DriverPath
        {
            get => _propDriverPath;
            set { _propDriverPath = value; InvokePropertyChanged(this, nameof(Prop_DriverPath)); }
        }

        #endregion

        #region Prop_AttachToSessionId

        private string _propAttachToSessionId;

        /// <summary>
        /// ID существующей сессии для присоединения (опционально).
        /// Если указан — браузер не открывается, активность присоединяется к существующей сессии.
        /// Если пуст — создается новая сессия браузера.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Присоединиться к сессии")]
        public string Prop_AttachToSessionId
        {
            get => _propAttachToSessionId;
            set { _propAttachToSessionId = value; InvokePropertyChanged(this, nameof(Prop_AttachToSessionId)); }
        }

        #endregion

        // ── Выходные параметры ─────────────────────────────────────────────────

        #region Prop_SessionId

        private string _propSessionId;

        /// <summary>
        /// ID созданной сессии браузера.
        /// Автоматически доступен дочерним активностям с пустым Prop_SessionId
        /// через ambient-контекст BrowserSessionContext.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_SessionId)]
        public string Prop_SessionId
        {
            get => _propSessionId;
            set { _propSessionId = value; InvokePropertyChanged(this, nameof(Prop_SessionId)); }
        }

        #endregion

        // ── Конструктор ────────────────────────────────────────────────────────

        public BrowserOpenBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_BrowserOpen;
            sdkComponentHelp =
                "Открывает браузер и выполняет вложенные активности.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "Тип браузера     — Chrome, Firefox или Edge\n" +
                "\n" +
                "── Опции браузера ─────────────────────────────\n" +
                "Headless режим   — запуск без GUI (фоновый режим)\n" +
                "Режим инкогнито  — приватный режим браузера\n" +
                "Отключить изображения — ускорение загрузки страниц\n" +
                "User-Agent       — пользовательская UA строка\n" +
                "Путь к драйверу  — chromedriver.exe / geckodriver.exe\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "ID сессии — автоматически передаётся дочерним активностям.\n" +
                "            В дочерних активностях Prop_SessionId можно не заполнять.\n" +
                "\n" +
                "ВАЖНО: Поместите активность «Закрыть браузер» последней внутри\n" +
                "контейнера — браузер не закрывается автоматически.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<BrowserType>("Prop_BrowserType", "Тип браузера для запуска"),
                PropertyBuilder.String("Prop_AttachToSessionId", "ID существующей сессии для присоединения"),
                PropertyBuilder.BooleanObject("Prop_Headless", "Запустить в headless режиме"),
                PropertyBuilder.BooleanObject("Prop_IncognitoMode", "Запустить в режиме инкогнито"),
                PropertyBuilder.BooleanObject("Prop_DisableImages", "Отключить загрузку изображений"),
                PropertyBuilder.String("Prop_UserAgent", "Пользовательский User-Agent"),
                PropertyBuilder.FileSelector("Prop_DriverPath", "Путь к драйверу Selenium"),
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID созданной/присоединенной сессии")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_BrowserType = BrowserType.Chrome;
            this.Prop_AttachToSessionId = "\"\"";
            this.Prop_Headless = false;
            this.Prop_IncognitoMode = false;
            this.Prop_DisableImages = false;
            this.Prop_UserAgent = "\"\"";
            this.Prop_DriverPath = "\"\"";
        }

        // ── SimpleAction — выполняется ДО дочерних активностей ────────────────

        /// <summary>
        /// Открывает браузер и регистрирует сессию в ambient-контексте.
        /// После return движок SDK выполняет вложенные активности контейнера.
        /// Закрытие браузера — через BrowserCloseBack последней внутри контейнера.
        /// </summary>
        public override ExecutionResult SimpleAction(ScriptingData sd)
        {
            // ВАЖНО: Push() автоматически вызывает Pop() для предыдущей сессии.
            // Это обеспечивает автоматическую очистку контекста при выходе из
            // предыдущего контейнера BrowserOpen.

            try
            {
                string attachToSessionId = GetPropertyValue<string>(this.Prop_AttachToSessionId, nameof(Prop_AttachToSessionId), sd) ?? string.Empty;
                string sessionId;

                // Режим 1: Присоединение к существующей сессии
                if (!string.IsNullOrWhiteSpace(attachToSessionId))
                {
                    // Проверяем, что сессия существует в RepoDict
                    var existingDriver = SeleniumHelper.GetDriver(attachToSessionId);
                    if (existingDriver == null)
                    {
                        return new ExecutionResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"[Открыть браузер] Сессия {attachToSessionId} не найдена в RepoDict"
                        };
                    }

                    sessionId = attachToSessionId;

                    // Регистрируем существующую сессию в ambient-контексте
                    // При каждом вызове "Открыть браузер" текущий ID меняется на указанный
                    BrowserSessionContext.Push(sessionId);

                    // Записываем ID в выходную переменную (если задана)
                    if (!string.IsNullOrWhiteSpace(this.Prop_SessionId))
                        SetVariableValue(this.Prop_SessionId, sessionId, sd);

                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = $"[Открыть браузер] Присоединено к сессии {sessionId}"
                    };
                }

                // Режим 2: Создание новой сессии
                string userAgent = GetPropertyValue<string>(this.Prop_UserAgent, nameof(Prop_UserAgent), sd) ?? string.Empty;
                string driverPath = GetPropertyValue<string>(this.Prop_DriverPath, nameof(Prop_DriverPath), sd) ?? string.Empty;

                // Генерируем ID сессии и создаём WebDriver
                sessionId = SeleniumHelper.GenerateSessionId();
                IWebDriver newDriver = CreateDriver(this.Prop_BrowserType, userAgent, driverPath);

                // Сохраняем драйвер в репозитории
                RepoDict.Set(sessionId, newDriver);

                // Регистрируем сессию в ambient-контексте
                // дочерние активности с пустым Prop_SessionId получат её автоматически
                // через SessionResolver.Resolve → BrowserSessionContext.Current
                BrowserSessionContext.Push(sessionId);

                // Записываем ID в выходную переменную (если задана пользователем)
                if (!string.IsNullOrWhiteSpace(this.Prop_SessionId))
                    SetVariableValue(this.Prop_SessionId, sessionId, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Открыть браузер] {this.Prop_BrowserType} → сессия {sessionId}"
                };
            }
            catch (OpenQA.Selenium.WebDriverException ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка WebDriver: {ex.Message}"
                };
            }
            catch (InvalidOperationException ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Недопустимая операция: {ex.Message}"
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

        // ── Приватные методы ───────────────────────────────────────────────────

        /// <summary>
        /// Создаёт WebDriver в зависимости от типа браузера.
        /// </summary>
        private IWebDriver CreateDriver(BrowserType browserType, string userAgent, string driverPath)
        {
            switch (browserType)
            {
                case BrowserType.Chrome: return CreateChromeDriver(userAgent, driverPath);
                case BrowserType.Firefox: return CreateFirefoxDriver(userAgent, driverPath);
                case BrowserType.Edge: return CreateEdgeDriver(userAgent, driverPath);
                default:
                    throw new NotSupportedException($"Браузер {browserType} не поддерживается");
            }
        }

        /// <summary>Создаёт Chrome WebDriver с настроенными опциями.</summary>
        private IWebDriver CreateChromeDriver(string userAgent, string driverPath)
        {
            var options = new ChromeOptions();

            if (this.Prop_Headless) options.AddArgument("--headless");
            if (this.Prop_IncognitoMode) options.AddArgument("--incognito");
            if (this.Prop_DisableImages) options.AddUserProfilePreference(
                "profile.default_content_setting_values.images", 2);
            if (!string.IsNullOrWhiteSpace(userAgent))
                options.AddArgument($"--user-agent={userAgent}");

            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");

            return !string.IsNullOrWhiteSpace(driverPath)
                ? new ChromeDriver(driverPath, options)
                : new ChromeDriver(options);
        }

        /// <summary>Создаёт Firefox WebDriver с настроенными опциями.</summary>
        private IWebDriver CreateFirefoxDriver(string userAgent, string driverPath)
        {
            var options = new FirefoxOptions();

            if (this.Prop_Headless) options.AddArgument("--headless");
            if (this.Prop_IncognitoMode) options.AddArgument("-private");
            if (this.Prop_DisableImages) options.SetPreference("permissions.default.image", 2);
            if (!string.IsNullOrWhiteSpace(userAgent))
                options.SetPreference("general.useragent.override", userAgent);

            return !string.IsNullOrWhiteSpace(driverPath)
                ? new FirefoxDriver(driverPath, options)
                : new FirefoxDriver(options);
        }

        /// <summary>Создаёт Edge WebDriver с настроенными опциями.</summary>
        private IWebDriver CreateEdgeDriver(string userAgent, string driverPath)
        {
            var options = new EdgeOptions();

            if (this.Prop_Headless) options.AddArgument("--headless");
            if (this.Prop_IncognitoMode) options.AddArgument("--inprivate");
            if (this.Prop_DisableImages) options.AddUserProfilePreference(
                "profile.default_content_setting_values.images", 2);
            if (!string.IsNullOrWhiteSpace(userAgent))
                options.AddArgument($"--user-agent={userAgent}");

            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");

            return !string.IsNullOrWhiteSpace(driverPath)
                ? new EdgeDriver(driverPath, options)
                : new EdgeDriver(options);
        }

        // ── Валидация ──────────────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            return new ValidationResult();
        }
    }
}
