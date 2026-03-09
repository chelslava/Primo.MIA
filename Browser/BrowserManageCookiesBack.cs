// =============================================================================
// BrowserManageCookiesBack.cs — активность «Управление cookies».
//
// Работает с cookies браузера: получение, установка, удаление.
// Используется для управления сессиями и состоянием браузера.
//
// Операции:
//   Get       — получить cookie по имени
//   GetAll    — получить все cookies
//   Set       — установить cookie
//   Delete    — удалить cookie по имени
//   DeleteAll — удалить все cookies
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Newtonsoft.Json;
using OpenQA.Selenium;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для управления cookies браузера.
    /// </summary>
    public class BrowserManageCookiesBack : PrimoComponentTO<BrowserManageCookies>
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

        private CookieOperation _propOperation;
        /// <summary>Операция с cookies.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CookieOperation)]
        public CookieOperation Prop_Operation
        {
            get => _propOperation;
            set { _propOperation = value; InvokePropertyChanged(this, "Prop_Operation"); }
        }

        private string _propCookieName;
        /// <summary>Имя cookie.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CookieName)]
        public string Prop_CookieName
        {
            get => _propCookieName;
            set { _propCookieName = value; InvokePropertyChanged(this, "Prop_CookieName"); }
        }

        private string _propCookieValue;
        /// <summary>Значение cookie (для Set).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CookieValue)]
        public string Prop_CookieValue
        {
            get => _propCookieValue;
            set { _propCookieValue = value; InvokePropertyChanged(this, "Prop_CookieValue"); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propResult;
        /// <summary>Результат операции (значение cookie или JSON всех cookies).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_Cookies)]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public BrowserManageCookiesBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_BrowserManageCookies;
            sdkComponentHelp =
                "Управляет cookies браузера.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "Операция  — действие с cookies\n" +
                "Имя       — имя cookie (для Get, Set, Delete)\n" +
                "Значение  — значение cookie (для Set)\n" +
                "\n" +
                "Операции:\n" +
                "  Get       — получить cookie по имени\n" +
                "  GetAll    — получить все cookies (JSON)\n" +
                "  Set       — установить cookie\n" +
                "  Delete    — удалить cookie\n" +
                "  DeleteAll — удалить все cookies\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Результат — значение cookie или JSON всех cookies";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Enum<CookieOperation>("Prop_Operation", "Операция с cookies"),
                PropertyBuilder.String("Prop_CookieName", "Имя cookie"),
                PropertyBuilder.String("Prop_CookieValue", "Значение cookie"),
                PropertyBuilder.Variable<string>("Prop_Result", "Результат операции")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_Operation = CookieOperation.GetAll;
            this.Prop_CookieName = "\"\"";
            this.Prop_CookieValue = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Чтение параметров
                string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
                string cookieName = GetPropertyValue<string>(this.Prop_CookieName, "Prop_CookieName", sd) ?? string.Empty;
                string cookieValue = GetPropertyValue<string>(this.Prop_CookieValue, "Prop_CookieValue", sd) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(sessionId))
                    throw new ArgumentException("ID сессии не может быть пустым");

                // Получение драйвера
                var driver = SeleniumHelper.GetDriver(sessionId);
                var cookieManager = driver.Manage().Cookies;

                string result = string.Empty;
                string actionMessage = string.Empty;

                // Выполнение операции
                switch (this.Prop_Operation)
                {
                    case CookieOperation.Get:
                        if (string.IsNullOrWhiteSpace(cookieName))
                            throw new ArgumentException("Имя cookie не может быть пустым");
                        
                        var cookie = cookieManager.GetCookieNamed(cookieName);
                        result = cookie?.Value ?? string.Empty;
                        actionMessage = $"Получен cookie '{cookieName}': {result}";
                        break;

                    case CookieOperation.GetAll:
                        var allCookies = cookieManager.AllCookies
                            .ToDictionary(c => c.Name, c => c.Value);
                        result = JsonConvert.SerializeObject(allCookies);
                        actionMessage = $"Получено {allCookies.Count} cookies";
                        break;

                    case CookieOperation.Set:
                        if (string.IsNullOrWhiteSpace(cookieName))
                            throw new ArgumentException("Имя cookie не может быть пустым");
                        
                        var newCookie = new Cookie(cookieName, cookieValue);
                        cookieManager.AddCookie(newCookie);
                        actionMessage = $"Установлен cookie '{cookieName}'";
                        break;

                    case CookieOperation.Delete:
                        if (string.IsNullOrWhiteSpace(cookieName))
                            throw new ArgumentException("Имя cookie не может быть пустым");
                        
                        cookieManager.DeleteCookieNamed(cookieName);
                        actionMessage = $"Удалён cookie '{cookieName}'";
                        break;

                    case CookieOperation.DeleteAll:
                        cookieManager.DeleteAllCookies();
                        actionMessage = "Удалены все cookies";
                        break;

                    default:
                        throw new NotSupportedException($"Операция {this.Prop_Operation} не поддерживается");
                }

                // Запись результата
                if (!string.IsNullOrWhiteSpace(this.Prop_Result) && !string.IsNullOrEmpty(result))
                    SetVariableValue(this.Prop_Result, result, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Управление cookies] {actionMessage}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [Управление cookies]: {ex.Message}"
                };
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_SessionId, ActivityStrings.Field_SessionId, "ID сессии обязателен");

            // Для операций с конкретным cookie имя обязательно
            if (this.Prop_Operation == CookieOperation.Get ||
                this.Prop_Operation == CookieOperation.Set ||
                this.Prop_Operation == CookieOperation.Delete)
            {
                ret.ValidateRequired(this.Prop_CookieName, ActivityStrings.Field_CookieName, "Имя cookie обязательно");
            }

            // Для Set значение обязательно
            if (this.Prop_Operation == CookieOperation.Set)
            {
                ret.ValidateRequired(this.Prop_CookieValue, ActivityStrings.Field_CookieValue, "Значение cookie обязательно");
            }

            return ret;
        }
    }
}
