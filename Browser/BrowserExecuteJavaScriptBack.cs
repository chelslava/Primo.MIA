// =============================================================================
// BrowserExecuteJavaScriptBack.cs — активность «Выполнить JavaScript».
//
// Выполняет JavaScript код в контексте текущей страницы браузера.
// Поддерживает передачу аргументов и получение результата выполнения.
//
// Используется для:
//   - Манипуляции DOM
//   - Извлечения данных
//   - Выполнения сложных действий
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
    /// Активность для выполнения JavaScript кода в браузере.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class BrowserExecuteJavaScriptBack : BrowserActivityBase<BrowserExecuteJavaScript>
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

        private string _propScript;
        /// <summary>JavaScript код для выполнения.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_JavaScriptCode)]
        public string Prop_Script
        {
            get => _propScript;
            set { _propScript = value; InvokePropertyChanged(this, "Prop_Script"); }
        }

        private string _propArguments;
        /// <summary>Аргументы для скрипта.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<object>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ScriptArguments)]
        public string Prop_Arguments
        {
            get => _propArguments;
            set { _propArguments = value; InvokePropertyChanged(this, "Prop_Arguments"); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propResult;
        /// <summary>Результат выполнения скрипта.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(object))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ScriptResult)]
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, "Prop_Result"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public BrowserExecuteJavaScriptBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_BrowserExecuteJS;
            sdkComponentHelp =
                "Выполняет JavaScript код в контексте браузера.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "JavaScript код — код для выполнения\n" +
                "Аргументы — список аргументов (необязательно)\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Результат — возвращаемое значение скрипта\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Используйте 'arguments[0]', 'arguments[1]' для доступа к аргументам.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.String("Prop_Script", "JavaScript код"),
                PropertyBuilder.Variable<List<object>>("Prop_Arguments", "Аргументы скрипта"),
                PropertyBuilder.Variable<object>("Prop_Result", "Результат выполнения")
            };

            InitClass(container);

            Prop_SessionId = "\"\"";
            Prop_Script = "\"return document.title;\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Получаем sessionId
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                string script = GetPropertyValue<string>(Prop_Script, nameof(Prop_Script), sd);

                if (string.IsNullOrWhiteSpace(script))
                    throw new ArgumentException("JavaScript код не может быть пустым");

                // Получение драйвера через базовый класс
                IWebDriver driver = GetDriverFromContext(sessionId);
                var jsExecutor = (IJavaScriptExecutor)driver;

                // Получение аргументов (если есть)
                object[] args = null;
                if (!string.IsNullOrWhiteSpace(Prop_Arguments))
                {
                    var argsList = GetPropertyValue<List<object>>(Prop_Arguments, "Prop_Arguments", sd);
                    if (argsList != null && argsList.Count > 0)
                        args = argsList.ToArray();
                }

                // Выполнение скрипта
                object result = args != null 
                    ? jsExecutor.ExecuteScript(script, args)
                    : jsExecutor.ExecuteScript(script);

                // Запись результата
                if (!string.IsNullOrWhiteSpace(Prop_Result))
                    SetVariableValue(Prop_Result, result, sd);

                return CreateSuccessResult("[Выполнить JavaScript] Скрипт выполнен успешно");
            }
            catch (Exception ex)
            {
                return CreateErrorResult(ex, "Выполнить JavaScript");
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
             
            ret.ValidateRequired(Prop_Script, ActivityStrings.Field_JavaScriptCode, "JavaScript код обязателен");
            return ret;
        }
    }
}
