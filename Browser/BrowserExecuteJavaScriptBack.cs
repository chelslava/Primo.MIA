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
            set { _propSessionId = value; InvokePropertyChanged(this, nameof(Prop_SessionId)); }
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
            set { _propScript = value; InvokePropertyChanged(this, nameof(Prop_Script)); }
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
            set { _propArguments = value; InvokePropertyChanged(this, nameof(Prop_Arguments)); }
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
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
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
            string message = string.Empty;

            var result = SafeExecute(() =>
            {
                // Получаем sessionId
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                string script = GetPropertyValue<string>(Prop_Script, nameof(Prop_Script), sd);

                Guard.NotNullOrWhiteSpace(script, nameof(script));

                // Получение драйвера через базовый класс
                IWebDriver driver = GetDriverFromContext(sessionId);
                var jsExecutor = (IJavaScriptExecutor)driver;

                // Логирование начала операции
                Logger.LogInfo(sdkComponentName, "Выполнение JavaScript для сессии: {0}", sessionId);
                Logger.LogDebug(sdkComponentName, "JavaScript код: {0}", script.Length > 100 ? script.Substring(0, 100) + "..." : script);

                // Получение аргументов (если есть)
                object[] args = null;
                if (!string.IsNullOrWhiteSpace(Prop_Arguments))
                {
                    var argsList = GetPropertyValue<List<object>>(Prop_Arguments, nameof(Prop_Arguments), sd);
                    if (argsList != null && argsList.Count > 0)
                    {
                        args = argsList.ToArray();
                        Logger.LogDebug(sdkComponentName, "Передано аргументов: {0}", args.Length);
                    }
                }

                // Выполнение скрипта
                object scriptResult = args != null
                    ? jsExecutor.ExecuteScript(script, args)
                    : jsExecutor.ExecuteScript(script);

                // Запись результата
                if (!string.IsNullOrWhiteSpace(Prop_Result))
                {
                    SetVariableValue(Prop_Result, scriptResult, sd);
                    Logger.LogDebug(sdkComponentName, "Результат записан в переменную: {0}", Prop_Result);
                }

                message = "[Выполнить JavaScript] Скрипт выполнен успешно";
                Logger.LogInfo(sdkComponentName, "JavaScript успешно выполнен");

            }, "Выполнить JavaScript");

            if (result.IsSuccess)
                result.SuccessMessage = message;

            return result;
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
