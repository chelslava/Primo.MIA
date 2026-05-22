// =============================================================================
// HttpRetryBack.cs — активность «HTTP: Запрос с повторами».
//
// Выполняет HTTP-запросы с настраиваемой политикой повторов:
// - Количество попыток
// - Стратегии задержки: Fixed, Linear, Exponential
// - Настраиваемые коды для повтора
// - Полная информация о попытках
//
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для выполнения HTTP-запросов с политикой повторов.
    /// Поддерживает различные стратегии задержки между попытками.
    /// </summary>
    public class HttpRetryBack : HttpActivityBase<HttpRetry>
    {
        // ── Входные параметры: Основные ───────────────────────────────────

        #region Prop_Url

        private string _propUrl;

        /// <summary>
        /// URL запроса.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("URL")]
        public string Prop_Url
        {
            get => _propUrl;
            set { _propUrl = value; InvokePropertyChanged(this, nameof(Prop_Url)); }
        }

        #endregion

        #region Prop_Method

        private HttpMethodType _propMethod;

        /// <summary>
        /// HTTP-метод запроса.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Метод")]
        public HttpMethodType Prop_Method
        {
            get => _propMethod;
            set { _propMethod = value; InvokePropertyChanged(this, nameof(Prop_Method)); }
        }

        #endregion

        #region Prop_Headers

        private string _propHeaders;

        /// <summary>
        /// Заголовки запроса в формате JSON.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Заголовки (JSON)")]
        public string Prop_Headers
        {
            get => _propHeaders;
            set { _propHeaders = value; InvokePropertyChanged(this, nameof(Prop_Headers)); }
        }

        #endregion

        #region Prop_Body

        private string _propBody;

        /// <summary>
        /// Тело запроса для POST/PUT/PATCH.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Тело запроса")]
        public string Prop_Body
        {
            get => _propBody;
            set { _propBody = value; InvokePropertyChanged(this, nameof(Prop_Body)); }
        }

        #endregion

        #region Prop_Timeout

        private string _propTimeout;

        /// <summary>
        /// Таймаут каждой попытки в секундах.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Таймаут (сек)")]
        public string Prop_Timeout
        {
            get => _propTimeout;
            set { _propTimeout = value; InvokePropertyChanged(this, nameof(Prop_Timeout)); }
        }

        #endregion

        // ── Входные параметры: Retry политика ──────────────────────────────

        #region Prop_MaxRetries

        private string _propMaxRetries;

        /// <summary>
        /// Максимальное количество попыток.
        /// По умолчанию: 3.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Повторы"),
         System.ComponentModel.DisplayName("Максимум попыток")]
        public string Prop_MaxRetries
        {
            get => _propMaxRetries;
            set { _propMaxRetries = value; InvokePropertyChanged(this, nameof(Prop_MaxRetries)); }
        }

        #endregion

        #region Prop_DelayMs

        private string _propDelayMs;

        /// <summary>
        /// Базовая задержка между попытками в миллисекундах.
        /// По умолчанию: 1000 (1 секунда).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Повторы"),
         System.ComponentModel.DisplayName("Задержка (мс)")]
        public string Prop_DelayMs
        {
            get => _propDelayMs;
            set { _propDelayMs = value; InvokePropertyChanged(this, nameof(Prop_DelayMs)); }
        }

        #endregion

        #region Prop_DelayStrategy

        private HttpRetryDelayStrategy _propDelayStrategy;

        /// <summary>
        /// Стратегия задержки между попытками.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("Повторы"),
         System.ComponentModel.DisplayName("Стратегия задержки")]
        public HttpRetryDelayStrategy Prop_DelayStrategy
        {
            get => _propDelayStrategy;
            set { _propDelayStrategy = value; InvokePropertyChanged(this, nameof(Prop_DelayStrategy)); }
        }

        #endregion

        #region Prop_RetryStatusCodes

        private string _propRetryStatusCodes;

        /// <summary>
        /// Коды HTTP для повтора через запятую.
        /// По умолчанию: "500,502,503,504,408,429".
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Повторы"),
         System.ComponentModel.DisplayName("Коды для повтора")]
        public string Prop_RetryStatusCodes
        {
            get => _propRetryStatusCodes;
            set { _propRetryStatusCodes = value; InvokePropertyChanged(this, nameof(Prop_RetryStatusCodes)); }
        }

        #endregion

        // ── Выходные параметры ────────────────────────────────────────────

        #region Prop_StatusCode

        private string _propStatusCode;

        /// <summary>
        /// HTTP код ответа последней попытки.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Код ответа")]
        public string Prop_StatusCode
        {
            get => _propStatusCode;
            set { _propStatusCode = value; InvokePropertyChanged(this, nameof(Prop_StatusCode)); }
        }

        #endregion

        #region Prop_ResponseContent

        private string _propResponseContent;

        /// <summary>
        /// Тело ответа сервера.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Тело ответа")]
        public string Prop_ResponseContent
        {
            get => _propResponseContent;
            set { _propResponseContent = value; InvokePropertyChanged(this, nameof(Prop_ResponseContent)); }
        }

        #endregion

        #region Prop_ResponseHeaders

        private string _propResponseHeaders;

        /// <summary>
        /// Заголовки ответа в формате JSON.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Заголовки ответа (JSON)")]
        public string Prop_ResponseHeaders
        {
            get => _propResponseHeaders;
            set { _propResponseHeaders = value; InvokePropertyChanged(this, nameof(Prop_ResponseHeaders)); }
        }

        #endregion

        #region Prop_AttemptsCount

        private string _propAttemptsCount;

        /// <summary>
        /// Фактическое количество попыток.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Количество попыток")]
        public string Prop_AttemptsCount
        {
            get => _propAttemptsCount;
            set { _propAttemptsCount = value; InvokePropertyChanged(this, nameof(Prop_AttemptsCount)); }
        }

        #endregion

        #region Prop_Success

        private string _propSuccess;

        /// <summary>
        /// Признак успешного выполнения.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Успешно")]
        public string Prop_Success
        {
            get => _propSuccess;
            set { _propSuccess = value; InvokePropertyChanged(this, nameof(Prop_Success)); }
        }

        #endregion

        // ── Конструктор ───────────────────────────────────────────────────

        /// <summary>
        /// Конструктор активности «HTTP: Запрос с повторами».
        /// </summary>
        public HttpRetryBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "HTTP: Запрос с повторами";
            sdkComponentHelp =
                "Выполняет HTTP-запросы с настраиваемой политикой повторов.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "URL              — адрес запроса\n" +
                "Метод            — GET, POST, PUT, DELETE, PATCH\n" +
                "Заголовки (JSON) — заголовки запроса\n" +
                "Тело запроса     — данные для POST/PUT/PATCH\n" +
                "Таймаут (сек)    — таймаут каждой попытки\n" +
                "\n" +
                "── Политика повторов ─────────────────────────\n" +
                "Максимум попыток   — число попыток (по умолчанию 3)\n" +
                "Задержка (мс)      — базовая задержка между попытками\n" +
                "Стратегия задержки — Fixed, Linear, Exponential\n" +
                "Коды для повтора   — HTTP коды через запятую\n" +
                "\n" +
                "── Стратегии задержки ────────────────────────\n" +
                "Fixed       — постоянная задержка\n" +
                "Linear      — задержка * номер попытки\n" +
                "Exponential — задержка * 2^(попытка-1)\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Код ответа        — HTTP status code\n" +
                "Тело ответа       — ответ сервера\n" +
                "Количество попыток — фактически выполнено\n" +
                "Успешно           — признак успеха";

            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<string>("Prop_Url", "URL запроса"),
                PropertyBuilder.Enum<HttpMethodType>("Prop_Method", "HTTP-метод"),
                PropertyBuilder.Script<string>("Prop_Headers", "Заголовки запроса (JSON)"),
                PropertyBuilder.Script<string>("Prop_Body", "Тело запроса"),
                PropertyBuilder.Script<int>("Prop_Timeout", "Таймаут каждой попытки (сек)"),
                PropertyBuilder.Script<int>("Prop_MaxRetries", "Максимальное количество попыток"),
                PropertyBuilder.Script<int>("Prop_DelayMs", "Базовая задержка (мс)"),
                PropertyBuilder.Enum<HttpRetryDelayStrategy>("Prop_DelayStrategy", "Стратегия задержки"),
                PropertyBuilder.Script<string>("Prop_RetryStatusCodes", "Коды HTTP для повтора"),
                PropertyBuilder.BooleanObject("Prop_IgnoreSslErrors", "Игнорировать ошибки SSL"),
                PropertyBuilder.BooleanObject("Prop_UseCertificate", "Использовать сертификат"),
                PropertyBuilder.FileSelector("Prop_CertPath", "Путь к сертификату (.pfx)"),
                PropertyBuilder.Script<string>("Prop_CertPassword", "Пароль сертификата"),
                PropertyBuilder.Variable<int>("Prop_StatusCode", "HTTP код ответа"),
                PropertyBuilder.Variable<string>("Prop_ResponseContent", "Тело ответа"),
                PropertyBuilder.Variable<string>("Prop_ResponseHeaders", "Заголовки ответа (JSON)"),
                PropertyBuilder.Variable<int>("Prop_AttemptsCount", "Количество попыток"),
                PropertyBuilder.Variable<bool>("Prop_Success", "Признак успеха")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_Url = "\"https://api.example.com\"";
            this.Prop_Method = HttpMethodType.GET;
            this.Prop_Headers = "\"{}\"";
            this.Prop_Body = "\"\"";
            this.Prop_Timeout = "100";
            this.Prop_MaxRetries = "3";
            this.Prop_DelayMs = "1000";
            this.Prop_DelayStrategy = HttpRetryDelayStrategy.Exponential;
            this.Prop_RetryStatusCodes = "\"500,502,503,504,408,429\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────────

        /// <summary>
        /// Основной метод выполнения HTTP-запроса с повторами.
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Чтение входных параметров ──────────────────────────────
                string url = GetPropertyValue<string>(this.Prop_Url, nameof(Prop_Url), sd);
                if (string.IsNullOrWhiteSpace(url))
                    throw new ArgumentException("URL не может быть пустым");

                string headersJson = GetPropertyValue<string>(this.Prop_Headers, nameof(Prop_Headers), sd) ?? "{}";
                string body = GetPropertyValue<string>(this.Prop_Body, nameof(Prop_Body), sd) ?? string.Empty;

                string timeoutStr = GetPropertyValue<string>(this.Prop_Timeout, nameof(Prop_Timeout), sd) ?? "100";
                int timeout = HttpHelper.ParseTimeout(timeoutStr, 100);

                string maxRetriesStr = GetPropertyValue<string>(this.Prop_MaxRetries, nameof(Prop_MaxRetries), sd) ?? "3";
                int maxRetries = HttpHelper.ParseTimeout(maxRetriesStr, 3);
                if (maxRetries < 1) maxRetries = 1;

                string delayMsStr = GetPropertyValue<string>(this.Prop_DelayMs, nameof(Prop_DelayMs), sd) ?? "1000";
                int baseDelayMs = HttpHelper.ParseTimeout(delayMsStr, 1000);

                string retryCodesStr = GetPropertyValue<string>(this.Prop_RetryStatusCodes, nameof(Prop_RetryStatusCodes), sd);
                int[] retryCodes = HttpHelper.ParseRetryStatusCodes(retryCodesStr);

                // ── Создание HttpClient ───────────────────────────────────
                using (var client = CreateHttpClient(sd, timeout))
                {
                    // Добавление заголовков
                    var headers = HttpHelper.ParseHeaders(headersJson);
                    var normalizedHeaders = HttpHelper.NormalizeHeaders(headers);
                    normalizedHeaders.ToList()
                        .ForEach(h => client.DefaultRequestHeaders.TryAddWithoutValidation(h.Key, h.Value));

                    // ── Выполнение запросов с повторами ───────────────────
                    HttpResponseMessage response = null;
                    int attemptsCount = 0;
                    List<string> attemptLogs = new List<string>();

                    for (int attempt = 1; attempt <= maxRetries; attempt++)
                    {
                        attemptsCount = attempt;

                        // Освобождаем предыдущий response перед новой попыткой для предотвращения утечки соединений
                        response?.Dispose();
                        response = null;

                        try
                        {
                            response = ExecuteRequest(client, url, body).ConfigureAwait(false).GetAwaiter().GetResult();
                            int statusCode = (int)response.StatusCode;

                            attemptLogs.Add($"Попытка {attempt}: HTTP {statusCode}");

                            // Проверяем, нужен ли повтор
                            if (response.IsSuccessStatusCode)
                            {
                                // Успех — выходим из цикла
                                break;
                            }

                            if (!HttpHelper.ShouldRetry(statusCode, retryCodes))
                            {
                                // Код не в списке для повтора — выходим
                                break;
                            }

                            // Если это не последняя попытка — ждём
                            if (attempt < maxRetries)
                            {
                                int delay = HttpHelper.CalculateRetryDelay(this.Prop_DelayStrategy, baseDelayMs, attempt);
                                attemptLogs.Add($"Ожидание {delay}мс перед повтором...");
                                Thread.Sleep(delay);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Освобождаем ресурсы при ошибке для предотвращения утечки соединений
                            response?.Dispose();
                            response = null;

                            attemptLogs.Add($"Попытка {attempt}: Ошибка — {ex.Message}");

                            // Если это не последняя попытка — ждём и продолжаем
                            if (attempt < maxRetries)
                            {
                                int delay = HttpHelper.CalculateRetryDelay(this.Prop_DelayStrategy, baseDelayMs, attempt);
                                Thread.Sleep(delay);
                            }
                            else
                            {
                                // Последняя попытка — пробрасываем ошибку
                                throw;
                            }
                        }
                    }

                    // ── Обработка финального ответа ────────────────────────
                    if (response == null)
                    {
                        return new ExecutionResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"Все попытки ({attemptsCount}) завершились ошибкой"
                        };
                    }

                    int finalStatusCode = (int)response.StatusCode;
                    string responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    string responseHeaders = HttpHelper.SerializeResponseHeaders(response.Headers, response.Content.Headers);

                    // ── Запись выходных параметров ────────────────────────
                    if (!string.IsNullOrWhiteSpace(this.Prop_StatusCode))
                        SetVariableValue(this.Prop_StatusCode, finalStatusCode, sd);

                    if (!string.IsNullOrWhiteSpace(this.Prop_ResponseContent))
                        SetVariableValue(this.Prop_ResponseContent, responseContent, sd);

                    if (!string.IsNullOrWhiteSpace(this.Prop_ResponseHeaders))
                        SetVariableValue(this.Prop_ResponseHeaders, responseHeaders, sd);

                    if (!string.IsNullOrWhiteSpace(this.Prop_AttemptsCount))
                        SetVariableValue(this.Prop_AttemptsCount, attemptsCount, sd);

                    if (!string.IsNullOrWhiteSpace(this.Prop_Success))
                        SetVariableValue(this.Prop_Success, response.IsSuccessStatusCode, sd);

                    if (response.IsSuccessStatusCode)
                    {
                        return new ExecutionResult
                        {
                            IsSuccess = true,
                            SuccessMessage = $"[HTTP: Запрос с повторами] {this.Prop_Method} {url} → HTTP {finalStatusCode} (попыток: {attemptsCount})"
                        };
                    }
                    else
                    {
                        return new ExecutionResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"HTTP {finalStatusCode} после {attemptsCount} попыток. Ответ: {responseContent}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [HTTP: Запрос с повторами]: {ex.Message}"
                };
            }
        }

        // ── Приватные методы ──────────────────────────────────────────────

        /// <summary>
        /// Выполняет HTTP-запрос в соответствии с выбранным методом.
        /// </summary>
        private async System.Threading.Tasks.Task<HttpResponseMessage> ExecuteRequest(
            HttpClient client, string url, string body)
        {
            HttpResponseMessage response;

            switch (this.Prop_Method)
            {
                case HttpMethodType.GET:
                    response = await client.GetAsync(url);
                    break;

                case HttpMethodType.POST:
                    response = await client.PostAsync(url, CreateStringContent(body));
                    break;

                case HttpMethodType.PUT:
                    response = await client.PutAsync(url, CreateStringContent(body));
                    break;

                case HttpMethodType.DELETE:
                    response = await client.DeleteAsync(url);
                    break;

                case HttpMethodType.PATCH:
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                    {
                        Content = CreateStringContent(body)
                    };
                    response = await client.SendAsync(request);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(Prop_Method),
                        $"Неподдерживаемый HTTP-метод: {this.Prop_Method}");
            }

            return response;
        }

        /// <summary>
        /// Создаёт StringContent для тела запроса.
        /// </summary>
        private StringContent CreateStringContent(string body)
        {
            return new StringContent(body ?? string.Empty, Encoding.UTF8, "application/json");
        }

        // ── Валидация ─────────────────────────────────────────────────────

        /// <summary>
        /// Валидация параметров активности.
        /// </summary>
        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_Url))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = "Prop_Url",
                    Error = "URL не может быть пустым"
                });
            }

            return ret;
        }
    }
}
