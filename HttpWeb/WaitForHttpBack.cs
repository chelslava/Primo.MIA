// =============================================================================
// WaitForHttpBack.cs — активность «HTTP: Ожидание условия» (issue #32).
//
// Poll-активность для long-running API-задач: запустил job → жди готовности.
// Условие: ожидаемый статус (+ опционально подстрока в теле), общий таймаут,
// интервал опроса. Сетевые ошибки не прерывают ожидание.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Primo.MIA
{
    /// <summary>Активность ожидания выполнения условия HTTP-эндпоинтом.</summary>
    public class WaitForHttpBack : PrimoComponentTO<WaitForHttp>
    {
        public override string GroupName { get => ActivityCategories.HttpWeb; protected set { } }

        protected override int sdkTimeOut { get => 3600000; set { } }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propUrl;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_WaitHttpUrl)]
        /// <summary>URL эндпоинта для опроса (GET).</summary>
        public string Prop_Url
        {
            get => _propUrl;
            set { _propUrl = value; InvokePropertyChanged(this, nameof(Prop_Url)); }
        }

        private string _propExpectedStatus;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_WaitHttpExpectedStatus)]
        /// <summary>Ожидаемый статус; 0 = любой 2xx.</summary>
        public string Prop_ExpectedStatus
        {
            get => _propExpectedStatus;
            set { _propExpectedStatus = value; InvokePropertyChanged(this, nameof(Prop_ExpectedStatus)); }
        }

        private string _propBodyContains;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_WaitHttpBodyContains)]
        /// <summary>Обязательная подстрока в теле ответа (пусто — не проверять).</summary>
        public string Prop_BodyContains
        {
            get => _propBodyContains;
            set { _propBodyContains = value; InvokePropertyChanged(this, nameof(Prop_BodyContains)); }
        }

        private string _propTimeoutSeconds;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_WaitHttpTimeout)]
        /// <summary>Общий таймаут ожидания, сек (по умолчанию 60, максимум 3600).</summary>
        public string Prop_TimeoutSeconds
        {
            get => _propTimeoutSeconds;
            set { _propTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_TimeoutSeconds)); }
        }

        private string _propPollIntervalMs;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_WaitHttpInterval)]
        /// <summary>Интервал опроса, мс (по умолчанию 1000, минимум 100).</summary>
        public string Prop_PollIntervalMs
        {
            get => _propPollIntervalMs;
            set { _propPollIntervalMs = value; InvokePropertyChanged(this, nameof(Prop_PollIntervalMs)); }
        }

        private bool _propIgnoreSslErrors;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_WaitHttpIgnoreSsl)]
        /// <summary>Игнорировать ошибки SSL (только тестовые среды).</summary>
        public bool Prop_IgnoreSslErrors
        {
            get => _propIgnoreSslErrors;
            set { _propIgnoreSslErrors = value; InvokePropertyChanged(this, nameof(Prop_IgnoreSslErrors)); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_WaitHttpResult)]
        /// <summary>Условие выполнено (имя выходной переменной).</summary>
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
        }

        private string _propLastBody;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_WaitHttpLastBody)]
        /// <summary>Последнее тело ответа (имя выходной переменной).</summary>
        public string Prop_LastBody
        {
            get => _propLastBody;
            set { _propLastBody = value; InvokePropertyChanged(this, nameof(Prop_LastBody)); }
        }

        private string _propAttempts;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_WaitHttpAttempts)]
        /// <summary>Число попыток (имя выходной переменной).</summary>
        public string Prop_Attempts
        {
            get => _propAttempts;
            set { _propAttempts = value; InvokePropertyChanged(this, nameof(Prop_Attempts)); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public WaitForHttpBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_WaitForHttp;
            sdkComponentHelp = @"Компонент ""HTTP: Ожидание условия""
Опрашивает endpoint (GET) до выполнения условия — для long-running API-задач.

Основные:
URL*: [String] Эндпоинт для опроса.

Настройки:
Ожидаемый статус: [Int32] 200 (по умолчанию), 0 = любой 2xx.
Тело содержит: [String] Обязательная подстрока тела (пусто — не проверять).
Таймаут (сек): [Int32] Общий таймаут ожидания, по умолчанию 60, максимум 3600.
Интервал (мс): [Int32] Между попытками, по умолчанию 1000, минимум 100.
Игнорировать SSL: [Boolean] Только для тестовых сред.

Выходные данные:
Условие выполнено: [Boolean] (таймаут — false).
Последнее тело: [String]
Попыток: [Int32]";

            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Url", "URL эндпоинта для опроса (GET)"),
                PropertyBuilder.Int("Prop_ExpectedStatus", "Ожидаемый статус, 0 = любой 2xx"),
                PropertyBuilder.String("Prop_BodyContains", "Обязательная подстрока тела"),
                PropertyBuilder.Int("Prop_TimeoutSeconds", "Общий таймаут, сек (по умолчанию 60)"),
                PropertyBuilder.Int("Prop_PollIntervalMs", "Интервал опроса, мс (по умолчанию 1000)"),
                PropertyBuilder.BooleanObject("Prop_IgnoreSslErrors", "Игнорировать ошибки SSL"),
                PropertyBuilder.Variable<bool>("Prop_Result", "Условие выполнено"),
                PropertyBuilder.Variable<string>("Prop_LastBody", "Последнее тело ответа"),
                PropertyBuilder.Variable<int>("Prop_Attempts", "Число попыток")
            };

            InitClass(container);

            this.Prop_Url = "\"\"";
            this.Prop_BodyContains = "\"\"";
            this.Prop_ExpectedStatus = "200";
            this.Prop_TimeoutSeconds = "60";
            this.Prop_PollIntervalMs = "1000";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new WaitForHttpLogic();
            try
            {
                var url = GetPropertyValue<string>(this.Prop_Url, nameof(Prop_Url), sd);
                Guard.NotNullOrWhiteSpace(url, nameof(Prop_Url));

                var expectedStatusRaw = GetPropertyValue<string>(this.Prop_ExpectedStatus, nameof(Prop_ExpectedStatus), sd);
                var bodyContains = GetPropertyValue<string>(this.Prop_BodyContains, nameof(Prop_BodyContains), sd);
                var timeoutSeconds = StringHelper.TryParseInt(GetPropertyValue<string>(this.Prop_TimeoutSeconds, nameof(Prop_TimeoutSeconds), sd)) ?? WaitForHttpLogic.DefaultTimeoutSeconds;
                var pollIntervalRaw = StringHelper.TryParseInt(GetPropertyValue<string>(this.Prop_PollIntervalMs, nameof(Prop_PollIntervalMs), sd)) ?? WaitForHttpLogic.DefaultPollIntervalMs;

                int? expectedStatus = string.IsNullOrWhiteSpace(expectedStatusRaw)
                    ? (int?)null
                    : StringHelper.TryParseInt(expectedStatusRaw);

                var totalTimeout = TimeSpan.FromSeconds(logic.NormalizeTimeout(timeoutSeconds));
                var interval = TimeSpan.FromMilliseconds(logic.NormalizeInterval(pollIntervalRaw));
                var ignoreSsl = this.Prop_IgnoreSslErrors;

                var result = logic.PollUntil(
                    () => FetchOnce(url, ignoreSsl, interval),
                    expectedStatus,
                    bodyContains,
                    totalTimeout,
                    interval,
                    wait => Thread.Sleep(wait));

                SetVariableValue(this.Prop_Result, result.ConditionMet, sd);
                SetVariableValue(this.Prop_LastBody, result.LastBody, sd);
                SetVariableValue(this.Prop_Attempts, result.Attempts, sd);

                if (result.ConditionMet)
                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = $"[HTTP: Ожидание] условие выполнено за {result.Attempts} попыток ({result.Elapsed.TotalSeconds:F1} сек)"
                    };

                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Условие не выполнено за {totalTimeout.TotalSeconds:F0} сек ({result.Attempts} попыток). Последний статус: {result.LastStatus?.ToString() ?? "нет ответа"}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка HTTP: Ожидание — {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Одна попытка GET: (статус, тело). Исключения = сетевая ошибка
        /// (логика продолжает опрос).
        /// </summary>
        private (int? Status, string Body) FetchOnce(string url, bool ignoreSsl, TimeSpan requestTimeout)
        {
            using (var client = HttpClientPool.CreateClient(false, null, null, ignoreSsl, requestTimeout))
            using (var response = client.GetAsync(url).GetAwaiter().GetResult())
            using (var content = response.Content)
            {
                var body = content.ReadAsStringAsync().GetAwaiter().GetResult();
                return ((int)response.StatusCode, body);
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Url, ActivityStrings.Field_WaitHttpUrl, ActivityStrings.Error_WaitHttpUrlRequired);
            return ret;
        }
    }
}
