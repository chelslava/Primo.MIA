// =============================================================================
// BrowserGetLogsBack.cs — активность «Получение логов браузера».
//
// Получает логи консоли, сети, браузера и производительности.
// Полезно для отладки и мониторинга веб-приложений.
//
// Используется для:
//   - Получения ошибок JavaScript из консоли
//   - Мониторинга сетевых запросов
//   - Анализа производительности
//   - Отладки веб-приложений
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
    /// Активность для получения логов браузера.
    /// REFACTORED: Использует BrowserActivityBase для устранения дублирования кода
    /// </summary>
    public class BrowserGetLogsBack : BrowserActivityBase<BrowserGetLogs>
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

        private BrowserLogType _propLogType;
        /// <summary>Тип логов.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Тип логов")]
        public BrowserLogType Prop_LogType
        {
            get => _propLogType;
            set { _propLogType = value; InvokePropertyChanged(this, "Prop_LogType"); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propOutLogs;
        /// <summary>Список логов.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Логи")]
        public string Prop_OutLogs
        {
            get => _propOutLogs;
            set { _propOutLogs = value; InvokePropertyChanged(this, "Prop_OutLogs"); }
        }

        private string _propOutLogCount;
        /// <summary>Количество записей логов.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Количество записей")]
        public string Prop_OutLogCount
        {
            get => _propOutLogCount;
            set { _propOutLogCount = value; InvokePropertyChanged(this, "Prop_OutLogCount"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public BrowserGetLogsBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Браузер: Получить логи";
            sdkComponentHelp =
                "Получает логи браузера указанного типа.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "Тип логов — тип логов для получения\n" +
                "\n" +
                "── Типы логов ────────────────────────────────\n" +
                "Browser — логи браузера (ошибки, предупреждения)\n" +
                "Driver — логи драйвера Selenium\n" +
                "Client — логи клиента\n" +
                "Server — логи сервера\n" +
                "Performance — логи производительности\n" +
                "\n" +
                "── Выходные данные ───────────────────────────\n" +
                "Логи — список строк с записями логов\n" +
                "Количество записей — количество полученных логов\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Не все браузеры поддерживают все типы логов.\n" +
                "Для получения логов консоли используйте тип Browser.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Enum<BrowserLogType>("Prop_LogType", "Тип логов"),
                PropertyBuilder.Variable<List<string>>("Prop_OutLogs", "Логи"),
                PropertyBuilder.Variable<int>("Prop_OutLogCount", "Количество записей")
            };

            InitClass(container);

            Prop_SessionId = "\"\"";
            Prop_LogType = BrowserLogType.Browser;
            Prop_OutLogs = "";
            Prop_OutLogCount = "";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Получаем sessionId
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);

                // Получение драйвера через базовый класс
                IWebDriver driver = GetDriverFromContext(sessionId);

                // Получение логов
                var logs = SeleniumHelper.GetBrowserLogs(driver, Prop_LogType);

                // Запись результатов
                SetVariableValue(Prop_OutLogs, logs, sd);
                SetVariableValue(Prop_OutLogCount, logs.Count, sd);

                return CreateSuccessResult($"[Получить логи] Получено записей: {logs.Count} ({Prop_LogType})");
            }
            catch (Exception ex)
            {
                return CreateErrorResult(ex, "Получить логи");
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            ret.ValidateRequired(Prop_OutLogs, "Логи", "Переменная для логов обязательна");

            return ret;
        }
    }
}
