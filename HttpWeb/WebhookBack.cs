// =============================================================================
// WebhookBack.cs — активность «Webhook: Отправить».
//
// Отправляет HTTP POST с JSON-payload на URL вебхука.
// Предназначена для интеграции с Slack, Teams, Jira, Discord и любыми
// системами поддерживающими входящие вебхуки.
//
// Режимы формирования payload:
//   Payload  — готовая JSON-строка или Dictionary<string,object> → JSON
//   Template — шаблон с плейсхолдерами {{ключ}} + словарь переменных
//
// Встроенный Retry:
//   RetryCount   — количество дополнительных попыток (0 = только одна)
//   RetryDelayMs — пауза между попытками в миллисекундах
//   Повтор при: сетевой ошибке или ответе сервера 5xx
// =============================================================================

using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Webhook: Отправить».
    /// Отправляет POST-запрос с JSON на URL вебхука с поддержкой шаблонов и Retry.
    /// </summary>
    public class WebhookBack : PrimoComponentTO<Webhook>
    {
        // =====================================================================
        // Служебные свойства
        // =====================================================================

        public override string GroupName
        {
            get => ActivityCategories.HttpWeb;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get
            {
                // SDK-таймаут = таймаут запроса × (попытки + 1) + запас
                object tObj = this.Prop_TimeoutSeconds;
                int.TryParse(tObj?.ToString()?.Trim('"'), out int t);
                if (t <= 0) t = 30;

                object rObj = this.Prop_RetryCount;
                int.TryParse(rObj?.ToString()?.Trim('"'), out int r);

                return (t * (r + 1) + 10) * 1000;
            }
            set { }
        }

        // =====================================================================
        // Входные свойства
        // =====================================================================

        #region Prop_Url
        private string _propUrl;
        /// <summary>URL вебхука. Пример: https://hooks.slack.com/services/...</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("URL вебхука")]
        public string Prop_Url
        {
            get => _propUrl;
            set { _propUrl = value; InvokePropertyChanged(this, nameof(Prop_Url)); }
        }
        #endregion

        #region Prop_Payload
        private string _propPayload;
        /// <summary>
        /// Тело запроса — JSON-строка или имя переменной Dictionary&lt;string,object&gt;.
        /// Если задан Prop_Template — это поле игнорируется.
        /// Пример: "{\"text\": \"Привет из Primo!\"}"
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Payload (JSON)")]
        public string Prop_Payload
        {
            get => _propPayload;
            set { _propPayload = value; InvokePropertyChanged(this, nameof(Prop_Payload)); }
        }
        #endregion

        #region Prop_Template
        private string _propTemplate;
        /// <summary>
        /// Шаблон JSON с плейсхолдерами {{ключ}}.
        /// Если задан — имеет приоритет над Prop_Payload.
        /// Переменные подставляются из Prop_TemplateVars.
        /// Пример: "{\"text\": \"Заказ №{{OrderId}} на {{Amount}} руб.\"}"
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Шаблон (с {{плейсхолдерами}})")]
        public string Prop_Template
        {
            get => _propTemplate;
            set { _propTemplate = value; InvokePropertyChanged(this, nameof(Prop_Template)); }
        }
        #endregion

        #region Prop_TemplateVars
        private string _propTemplateVars;
        /// <summary>
        /// Переменные для подстановки в шаблон — Dictionary&lt;string,string&gt;.
        /// Ключ = имя плейсхолдера без {{/}}, значение = подставляемая строка.
        /// Пример: { "OrderId" → "ЗК-1234", "Amount" → "14500" }
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Переменные шаблона")]
        public string Prop_TemplateVars
        {
            get => _propTemplateVars;
            set { _propTemplateVars = value; InvokePropertyChanged(this, nameof(Prop_TemplateVars)); }
        }
        #endregion

        #region Prop_ContentType
        private string _propContentType;
        /// <summary>Content-Type запроса. По умолчанию application/json.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Content-Type")]
        public string Prop_ContentType
        {
            get => _propContentType;
            set { _propContentType = value; InvokePropertyChanged(this, nameof(Prop_ContentType)); }
        }
        #endregion

        #region Prop_ExtraHeaders
        private string _propExtraHeaders;
        /// <summary>
        /// Дополнительные заголовки запроса Dictionary&lt;string,string&gt;.
        /// Пример: { "Authorization" → "Bearer token", "X-Custom-Header" → "value" }
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Доп. заголовки")]
        public string Prop_ExtraHeaders
        {
            get => _propExtraHeaders;
            set { _propExtraHeaders = value; InvokePropertyChanged(this, nameof(Prop_ExtraHeaders)); }
        }
        #endregion

        #region Prop_TimeoutSeconds
        private string _propTimeoutSeconds;
        /// <summary>Таймаут одной попытки в секундах (по умолч. 30).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Таймаут (сек)")]
        public string Prop_TimeoutSeconds
        {
            get => _propTimeoutSeconds;
            set { _propTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_TimeoutSeconds)); }
        }
        #endregion

        #region Prop_RetryCount
        private string _propRetryCount;
        /// <summary>
        /// Количество повторных попыток при ошибке. 0 = только одна попытка.
        /// Повтор выполняется при: сетевой ошибке или ответе сервера 5xx.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Повторных попыток")]
        public string Prop_RetryCount
        {
            get => _propRetryCount;
            set { _propRetryCount = value; InvokePropertyChanged(this, nameof(Prop_RetryCount)); }
        }
        #endregion

        #region Prop_RetryDelayMs
        private string _propRetryDelayMs;
        /// <summary>Пауза между попытками в миллисекундах (по умолч. 1000).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Пауза между попытками (мс)")]
        public string Prop_RetryDelayMs
        {
            get => _propRetryDelayMs;
            set { _propRetryDelayMs = value; InvokePropertyChanged(this, nameof(Prop_RetryDelayMs)); }
        }
        #endregion

        #region Prop_IgnoreSslErrors
        private bool _propIgnoreSslErrors = false;
        /// <summary>Игнорировать ошибки SSL. Только для тестовых сред!</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Игнорировать ошибки SSL")]
        public bool Prop_IgnoreSslErrors
        {
            get => _propIgnoreSslErrors;
            set { _propIgnoreSslErrors = value; InvokePropertyChanged(this, nameof(Prop_IgnoreSslErrors)); }
        }
        #endregion

        // =====================================================================
        // Выходные свойства
        // =====================================================================

        #region Prop_StatusCode (Выходной)
        private string _propStatusCode;
        /// <summary>HTTP-код ответа (200, 404, 500...).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Код ответа")]
        public string Prop_StatusCode
        {
            get => _propStatusCode;
            set { _propStatusCode = value; InvokePropertyChanged(this, nameof(Prop_StatusCode)); }
        }
        #endregion

        #region Prop_ResponseBody (Выходной)
        private string _propResponseBody;
        /// <summary>Тело ответа сервера.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Тело ответа")]
        public string Prop_ResponseBody
        {
            get => _propResponseBody;
            set { _propResponseBody = value; InvokePropertyChanged(this, nameof(Prop_ResponseBody)); }
        }
        #endregion

        #region Prop_IsSuccess (Выходной)
        private string _propIsSuccess;
        /// <summary>true если код ответа 2xx.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Успешно (2xx)")]
        public string Prop_IsSuccess
        {
            get => _propIsSuccess;
            set { _propIsSuccess = value; InvokePropertyChanged(this, nameof(Prop_IsSuccess)); }
        }
        #endregion

        #region Prop_AttemptCount (Выходной)
        private string _propAttemptCount;
        /// <summary>Сколько попыток потребовалось фактически.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Попыток выполнено")]
        public string Prop_AttemptCount
        {
            get => _propAttemptCount;
            set { _propAttemptCount = value; InvokePropertyChanged(this, nameof(Prop_AttemptCount)); }
        }
        #endregion

        // =====================================================================
        // Конструктор
        // =====================================================================

        public WebhookBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Webhook: Отправить";
            sdkComponentHelp =
                "Отправляет POST-запрос с JSON на URL вебхука.\n" +
                "Интеграция с Slack, Teams, Jira, Discord и любыми входящими вебхуками.\n" +
                "\n" +
                "── Режимы payload ──────────────────────────────\n" +
                "Payload  — готовая JSON-строка\n" +
                "Template — шаблон с {{плейсхолдерами}} + словарь переменных\n" +
                "           Template имеет приоритет над Payload\n" +
                "\n" +
                "── Retry ───────────────────────────────────────\n" +
                "RetryCount   — доп. попыток (0 = только одна)\n" +
                "RetryDelayMs — пауза между попытками (мс)\n" +
                "Повтор при: сетевой ошибке или ответе 5xx\n" +
                "\n" +
                "── Примеры шаблонов ────────────────────────────\n" +
                "Slack:  {\"text\": \"{{Message}}\"}\n" +
                "Teams:  {\"@type\":\"MessageCard\",\"text\":\"{{Message}}\"}\n" +
                "\n" +
                "── Выходные данные ─────────────────────────────\n" +
                "StatusCode   — HTTP-код ответа\n" +
                "ResponseBody — тело ответа\n" +
                "IsSuccess    — true если код 2xx\n" +
                "AttemptCount — сколько попыток выполнено";

            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Основные
                PropertyBuilder.Script<string>("Prop_Url",          "URL вебхука"),
                PropertyBuilder.Script<string>("Prop_Payload",      "Payload (JSON-строка). Игнорируется если задан шаблон."),
                PropertyBuilder.Script<string>("Prop_Template",     "Шаблон с {{плейсхолдерами}}. Приоритет над Payload."),
                PropertyBuilder.Script<Dictionary<string,string>>("Prop_TemplateVars", "Переменные для подстановки в шаблон"),
                // Настройки
                PropertyBuilder.Script<string>("Prop_ContentType",  "Content-Type (по умолч. application/json)"),
                PropertyBuilder.Script<Dictionary<string,string>>("Prop_ExtraHeaders", "Дополнительные заголовки"),
                PropertyBuilder.Script<int>("Prop_TimeoutSeconds",  "Таймаут одной попытки (сек)"),
                PropertyBuilder.Script<int>("Prop_RetryCount",      "Повторных попыток при ошибке (0 = одна попытка)"),
                PropertyBuilder.Script<int>("Prop_RetryDelayMs",    "Пауза между попытками (мс)"),
                PropertyBuilder.BooleanObject("Prop_IgnoreSslErrors", "Игнорировать SSL-ошибки (только для тестовых сред!)"),
                // Выходные
                PropertyBuilder.Variable<int>("Prop_StatusCode",    "Код HTTP-ответа"),
                PropertyBuilder.Variable<string>("Prop_ResponseBody","Тело ответа"),
                PropertyBuilder.Variable<bool>("Prop_IsSuccess",    "true если ответ 2xx"),
                PropertyBuilder.Variable<int>("Prop_AttemptCount",  "Фактическое количество попыток"),
            };

            InitClass(container);

            this.Prop_Url            = this.IsNoCode(nameof(Prop_Url))  ? "" : "\"\"";
            this.Prop_Payload        = this.IsNoCode(nameof(Prop_Payload)) ? "{}" : "\"{}\"";
            this.Prop_ContentType    = this.IsNoCode(nameof(Prop_ContentType)) ? "application/json" : "\"application/json\"";
            this.Prop_TimeoutSeconds = "30";
            this.Prop_RetryCount     = "0";
            this.Prop_RetryDelayMs   = "1000";
            this.Prop_IgnoreSslErrors = false;
        }

        // =====================================================================
        // Выполнение
        // =====================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Читаем входные параметры ───────────────────────────────

                string url = GetPropertyValue<string>(this.Prop_Url, nameof(Prop_Url), sd);
                if (string.IsNullOrWhiteSpace(url))
                    return Fail("URL вебхука обязателен");

                string contentType = GetPropertyValue<string>(
                    this.Prop_ContentType, nameof(Prop_ContentType), sd)
                    ?? "application/json";

                object timeoutObj = GetPropertyValue(this.Prop_TimeoutSeconds, nameof(Prop_TimeoutSeconds), sd);
                int.TryParse(timeoutObj?.ToString(), out int timeoutSec);
                if (timeoutSec <= 0) timeoutSec = 30;

                object retryObj = GetPropertyValue(this.Prop_RetryCount, nameof(Prop_RetryCount), sd);
                int.TryParse(retryObj?.ToString(), out int retryCount);

                object delayObj = GetPropertyValue(this.Prop_RetryDelayMs, nameof(Prop_RetryDelayMs), sd);
                int.TryParse(delayObj?.ToString(), out int retryDelayMs);
                if (retryDelayMs <= 0) retryDelayMs = 1000;

                // ── Формируем JSON-payload ─────────────────────────────────

                string json = BuildPayload(sd);
                if (json == null)
                    return Fail("Укажите Payload или Template");

                // ── Дополнительные заголовки ───────────────────────────────

                // ВАЖНО: Dictionary<string,string> из переменной скрипта — ScriptEngine
                Dictionary<string, string> extraHeaders = null;
                if (!string.IsNullOrWhiteSpace(this.Prop_ExtraHeaders))
                    extraHeaders = sd.ScriptEngine.ExecuteReturn(
                        this.Prop_ExtraHeaders,
                        this.IsNoCode(nameof(Prop_ExtraHeaders)),
                        new LTools.Scripting.Model.ScriptTransferObject { Variables = sd.Variables }
                    ) as Dictionary<string, string>;

                // ── Создаём HttpClient ─────────────────────────────────────

                var handler = new System.Net.Http.HttpClientHandler();
                if (this.Prop_IgnoreSslErrors)
                    handler.ServerCertificateCustomValidationCallback =
                        (msg, cert, chain, errors) => true;

                int    statusCode   = 0;
                string responseBody = string.Empty;
                bool   isSuccess    = false;
                int    attempts     = 0;
                int    maxAttempts  = retryCount + 1;
                Exception lastEx    = null;

                using (var client = new System.Net.Http.HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSec)
                })
                {
                    // ── Retry-цикл ─────────────────────────────────────────────

                    while (attempts < maxAttempts)
                    {
                        attempts++;
                        lastEx = null;

                        try
                        {
                            // Формируем StringContent с нужным Content-Type
                            var content = new System.Net.Http.StringContent(
                                json, Encoding.UTF8, contentType);

                            // Создаём запрос — POST всегда
                            var request = new System.Net.Http.HttpRequestMessage(
                                System.Net.Http.HttpMethod.Post, url)
                            {
                                Content = content
                            };

                            // Добавляем доп. заголовки через LINQ
                            extraHeaders?.ToList().ForEach(kv =>
                                request.Headers.TryAddWithoutValidation(kv.Key, kv.Value));

                            var response = client.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
                            statusCode   = (int)response.StatusCode;
                            responseBody = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                            isSuccess    = statusCode >= 200 && statusCode < 300;

                            // Успех или клиентская ошибка (4xx) — не повторяем
                            if (isSuccess || (statusCode >= 400 && statusCode < 500))
                                break;

                            // Серверная ошибка (5xx) — повторяем если есть попытки
                            if (attempts < maxAttempts)
                                Thread.Sleep(retryDelayMs);
                        }
                        catch (Exception ex)
                        {
                            lastEx = ex;
                            if (attempts < maxAttempts)
                                Thread.Sleep(retryDelayMs);
                        }
                    }
                }

                // ── Если все попытки исчерпаны с исключением ──────────────

                if (lastEx != null && statusCode == 0)
                    return Fail($"Ошибка отправки вебхука после {attempts} попыток: {lastEx.Message}");

                // ── Записываем выходные параметры ─────────────────────────

                SetVariableValue(this.Prop_StatusCode,   statusCode,   sd);
                SetVariableValue(this.Prop_ResponseBody, responseBody, sd);
                SetVariableValue(this.Prop_IsSuccess,    isSuccess,    sd);
                SetVariableValue(this.Prop_AttemptCount, attempts,     sd);

                string attemptsStr = attempts > 1 ? $", попыток: {attempts}" : string.Empty;
                string resultMsg   = isSuccess
                    ? $"Вебхук отправлен → {statusCode}{attemptsStr}"
                    : $"Вебхук вернул {statusCode}{attemptsStr}";

                return new ExecutionResult
                {
                    IsSuccess      = isSuccess,
                    SuccessMessage = isSuccess ? resultMsg : null,
                    ErrorMessage   = isSuccess ? null      : resultMsg
                };
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка вебхука: {ex.Message}");
            }
        }

        // =====================================================================
        // Формирование payload
        // =====================================================================

        /// <summary>
        /// Формирует JSON-строку для отправки.
        /// Приоритет: Template > Payload.
        /// Template — подставляет переменные через Regex.Replace.
        /// Payload   — возвращает как есть (уже JSON-строка).
        /// </summary>
        private string BuildPayload(ScriptingData sd)
        {
            // ── Режим шаблона ──────────────────────────────────────────────
            string template = GetPropertyValue<string>(
                this.Prop_Template, nameof(Prop_Template), sd);

            if (!string.IsNullOrWhiteSpace(template))
            {
                // Читаем словарь переменных через ScriptEngine
                // (Dictionary<string,string> — ссылочный тип, нужен ScriptEngine)
                Dictionary<string, string> vars = null;
                if (!string.IsNullOrWhiteSpace(this.Prop_TemplateVars))
                    vars = sd.ScriptEngine.ExecuteReturn(
                        this.Prop_TemplateVars,
                        this.IsNoCode(nameof(Prop_TemplateVars)),
                        new LTools.Scripting.Model.ScriptTransferObject { Variables = sd.Variables }
                    ) as Dictionary<string, string>;

                if (vars == null || vars.Count == 0)
                    return template;

                // Подстановка {{ключ}} → значение через Regex.Replace + LINQ
                // StringComparer.OrdinalIgnoreCase для регистронезависимого поиска
                string result = Regex.Replace(template, @"\{\{([^}]+)\}\}", match =>
                {
                    string key = match.Groups[1].Value.Trim();
                    return vars
                        .Where(kv => string.Compare(kv.Key, key,
                            StringComparison.OrdinalIgnoreCase) == 0)
                        .Select(kv => kv.Value)
                        .FirstOrDefault() ?? match.Value; // не найдено — оставляем {{ключ}}
                });

                return result;
            }

            // ── Режим payload ──────────────────────────────────────────────
            string payload = GetPropertyValue<string>(
                this.Prop_Payload, nameof(Prop_Payload), sd);

            return string.IsNullOrWhiteSpace(payload) ? null : payload;
        }

        // =====================================================================
        // Валидация
        // =====================================================================

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_Url))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_Url),
                    Error        = "URL вебхука обязателен"
                });

            bool hasPayload  = !string.IsNullOrWhiteSpace(this.Prop_Payload);
            bool hasTemplate = !string.IsNullOrWhiteSpace(this.Prop_Template);

            if (!hasPayload && !hasTemplate)
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_Payload),
                    Error        = "Укажите Payload или Template"
                });

            return ret;
        }

        private static ExecutionResult Fail(string msg) =>
            new ExecutionResult { IsSuccess = false, ErrorMessage = msg };
    }
}
