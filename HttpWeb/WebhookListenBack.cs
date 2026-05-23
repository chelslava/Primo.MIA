// =============================================================================
// WebhookListenBack.cs — активность «Webhook: Получить».
//
// Поднимает локальный HTTP-сервер через HttpListener и ждёт входящий запрос.
// Принимает один запрос, извлекает тело, заголовки и метод, затем завершается.
//
// Ограничения:
//   - Слушает localhost или IP машины (не интернет без туннеля/прокси)
//   - Принимает один запрос и завершается (не постоянный сервер)
//   - На Windows требует прав администратора или регистрации URL:
//     netsh http add urlacl url=http://+:8080/webhook/ user=DOMAIN\User
//
// Паттерн использования:
//   1. RPA отправляет задачу во внешнюю систему
//   2. «Webhook: Получить» (порт 8080, таймаут 120 сек)
//   3. Внешняя система POST → http://localhost:8080/webhook/
//   4. RPA получает тело запроса и продолжает
// =============================================================================

using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Webhook: Получить».
    /// Поднимает локальный HTTP-сервер и ждёт один входящий запрос.
    /// </summary>
    public class WebhookListenBack : PrimoComponentTO<WebhookListen>
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
                // SDK-таймаут = таймаут ожидания + 15 сек запаса
                object obj = this.Prop_TimeoutSeconds;
                int.TryParse(obj?.ToString()?.Trim('"'), out int t);
                return (t > 0 ? t + 15 : 130) * 1000;
            }
            set { }
        }

        // =====================================================================
        // Свойства
        // =====================================================================

        #region Prop_Prefix
        private string _propPrefix;
        /// <summary>
        /// URL-префикс для прослушивания.
        /// Формат: http://localhost:PORT/PATH/
        /// Обязательно завершать слешем.
        /// Примеры:
        ///   http://localhost:8080/webhook/
        ///   http://+:8080/callback/     (все интерфейсы, требует прав)
        ///   http://192.168.1.10:9000/   (конкретный IP)
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("URL-префикс")]
        public string Prop_Prefix
        {
            get => _propPrefix;
            set { _propPrefix = value; InvokePropertyChanged(this, nameof(Prop_Prefix)); }
        }
        #endregion

        #region Prop_TimeoutSeconds
        private string _propTimeoutSeconds;
        /// <summary>
        /// Максимальное время ожидания запроса в секундах.
        /// По истечении — Prop_IsTimeout = true, активность завершается без ошибки.
        /// По умолчанию: 120 секунд.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Таймаут ожидания (сек)")]
        public string Prop_TimeoutSeconds
        {
            get => _propTimeoutSeconds;
            set { _propTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_TimeoutSeconds)); }
        }
        #endregion

        #region Prop_AllowedMethods
        private string _propAllowedMethods;
        /// <summary>
        /// Разрешённые HTTP-методы через запятую.
        /// Запросы с другими методами получают 405 Method Not Allowed.
        /// Пусто = принимать любой метод.
        /// Пример: "POST,PUT"
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Разрешённые методы")]
        public string Prop_AllowedMethods
        {
            get => _propAllowedMethods;
            set { _propAllowedMethods = value; InvokePropertyChanged(this, nameof(Prop_AllowedMethods)); }
        }
        #endregion

        #region Prop_SecretToken
        private string _propSecretToken;
        /// <summary>
        /// Секретный токен для проверки подлинности запроса.
        /// Сравнивается с заголовком X-Webhook-Secret (или X-Hub-Signature для GitHub).
        /// Пусто = проверка не выполняется.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Секретный токен")]
        public string Prop_SecretToken
        {
            get => _propSecretToken;
            set { _propSecretToken = value; InvokePropertyChanged(this, nameof(Prop_SecretToken)); }
        }
        #endregion

        #region Prop_ResponseCode
        private string _propResponseCode;
        /// <summary>
        /// HTTP-код ответа отправителю.
        /// По умолчанию 200. Используйте 202 (Accepted) для асинхронных вебхуков.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Код ответа отправителю")]
        public string Prop_ResponseCode
        {
            get => _propResponseCode;
            set { _propResponseCode = value; InvokePropertyChanged(this, nameof(Prop_ResponseCode)); }
        }
        #endregion

        #region Prop_ResponseBody
        private string _propResponseBody;
        /// <summary>
        /// Тело ответа отправителю.
        /// По умолчанию пустая строка. Пример: "{\"status\":\"ok\"}"
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName("Тело ответа отправителю")]
        public string Prop_ResponseBody
        {
            get => _propResponseBody;
            set { _propResponseBody = value; InvokePropertyChanged(this, nameof(Prop_ResponseBody)); }
        }
        #endregion

        // ── Выходные параметры ─────────────────────────────────────────────

        #region Prop_ReceivedBody (Выходной)
        private string _propReceivedBody;
        /// <summary>Тело полученного запроса (строка).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Тело запроса")]
        public string Prop_ReceivedBody
        {
            get => _propReceivedBody;
            set { _propReceivedBody = value; InvokePropertyChanged(this, nameof(Prop_ReceivedBody)); }
        }
        #endregion

        #region Prop_ReceivedHeaders (Выходной)
        private string _propReceivedHeaders;
        /// <summary>Заголовки входящего запроса в виде Dictionary&lt;string,string&gt;.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Заголовки запроса")]
        public string Prop_ReceivedHeaders
        {
            get => _propReceivedHeaders;
            set { _propReceivedHeaders = value; InvokePropertyChanged(this, nameof(Prop_ReceivedHeaders)); }
        }
        #endregion

        #region Prop_ReceivedMethod (Выходной)
        private string _propReceivedMethod;
        /// <summary>HTTP-метод входящего запроса (GET, POST, PUT и т.д.).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Метод запроса")]
        public string Prop_ReceivedMethod
        {
            get => _propReceivedMethod;
            set { _propReceivedMethod = value; InvokePropertyChanged(this, nameof(Prop_ReceivedMethod)); }
        }
        #endregion

        #region Prop_ReceivedUrl (Выходной)
        private string _propReceivedUrl;
        /// <summary>Полный URL входящего запроса включая query string.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("URL запроса")]
        public string Prop_ReceivedUrl
        {
            get => _propReceivedUrl;
            set { _propReceivedUrl = value; InvokePropertyChanged(this, nameof(Prop_ReceivedUrl)); }
        }
        #endregion

        #region Prop_QueryParams (Выходной)
        private string _propQueryParams;
        /// <summary>Query-параметры URL в виде Dictionary&lt;string,string&gt;.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Query-параметры")]
        public string Prop_QueryParams
        {
            get => _propQueryParams;
            set { _propQueryParams = value; InvokePropertyChanged(this, nameof(Prop_QueryParams)); }
        }
        #endregion

        #region Prop_IsTimeout (Выходной)
        private string _propIsTimeout;
        /// <summary>
        /// true если запрос не пришёл в течение таймаута.
        /// false если запрос получен успешно.
        /// Не является ошибкой — IsSuccess всегда true.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Таймаут истёк")]
        public string Prop_IsTimeout
        {
            get => _propIsTimeout;
            set { _propIsTimeout = value; InvokePropertyChanged(this, nameof(Prop_IsTimeout)); }
        }
        #endregion

        #region Prop_IsTokenValid (Выходной)
        private string _propIsTokenValid;
        /// <summary>
        /// true если секретный токен совпал (или токен не задан).
        /// false если токен задан но не совпал с заголовком запроса.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Токен валиден")]
        public string Prop_IsTokenValid
        {
            get => _propIsTokenValid;
            set { _propIsTokenValid = value; InvokePropertyChanged(this, nameof(Prop_IsTokenValid)); }
        }
        #endregion

        // =====================================================================
        // Конструктор
        // =====================================================================

        public WebhookListenBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Webhook: Получить";
            sdkComponentHelp =
                "Поднимает локальный HTTP-сервер и ждёт один входящий запрос.\n" +
                "\n" +
                "── URL-префикс ─────────────────────────────────\n" +
                "http://localhost:8080/webhook/   — только локально\n" +
                "http://+:8080/webhook/           — все интерфейсы (нужны права)\n" +
                "\n" +
                "── Права на Windows ────────────────────────────\n" +
                "Для localhost — права не нужны.\n" +
                "Для http://+: или конкретного IP — нужна регистрация:\n" +
                "  netsh http add urlacl url=http://+:8080/webhook/ user=DOMAIN\\User\n" +
                "\n" +
                "── Безопасность ────────────────────────────────\n" +
                "SecretToken — сравнивается с заголовком X-Webhook-Secret\n" +
                "IsTokenValid = false если токен не совпал (запрос всё равно принят)\n" +
                "\n" +
                "── Выходные данные ─────────────────────────────\n" +
                "ReceivedBody    — тело запроса\n" +
                "ReceivedHeaders — заголовки (Dict<string,string>)\n" +
                "ReceivedMethod  — HTTP-метод (POST, GET...)\n" +
                "ReceivedUrl     — полный URL запроса\n" +
                "QueryParams     — query-параметры (Dict<string,string>)\n" +
                "IsTimeout       — true если никто не постучал\n" +
                "IsTokenValid    — true если токен прошёл проверку";

            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Основные
                PropertyBuilder.Script<string>("Prop_Prefix",         "URL-префикс. Пример: http://localhost:8080/webhook/"),
                PropertyBuilder.Script<int>("Prop_TimeoutSeconds",    "Максимальное время ожидания (сек). По умолч. 120."),
                // Настройки
                PropertyBuilder.Script<string>("Prop_AllowedMethods", "Разрешённые методы через запятую (пусто = любой)"),
                PropertyBuilder.Script<string>("Prop_SecretToken",    "Секретный токен (сравнивается с X-Webhook-Secret)"),
                PropertyBuilder.Script<int>("Prop_ResponseCode",      "Код ответа отправителю (200, 202...)"),
                PropertyBuilder.Script<string>("Prop_ResponseBody",   "Тело ответа отправителю"),
                // Выходные
                PropertyBuilder.Variable<string>("Prop_ReceivedBody",    "Тело входящего запроса"),
                PropertyBuilder.Variable<Dictionary<string,string>>("Prop_ReceivedHeaders", "Заголовки запроса"),
                PropertyBuilder.Variable<string>("Prop_ReceivedMethod",  "HTTP-метод запроса"),
                PropertyBuilder.Variable<string>("Prop_ReceivedUrl",     "URL запроса"),
                PropertyBuilder.Variable<Dictionary<string,string>>("Prop_QueryParams", "Query-параметры"),
                PropertyBuilder.Variable<bool>("Prop_IsTimeout",         "true если таймаут истёк"),
                PropertyBuilder.Variable<bool>("Prop_IsTokenValid",      "true если токен валиден"),
            };

            InitClass(container);

            this.Prop_Prefix         = this.IsNoCode(nameof(Prop_Prefix))
                ? "http://localhost:8080/webhook/"
                : "\"http://localhost:8080/webhook/\"";
            this.Prop_TimeoutSeconds = "120";
            this.Prop_AllowedMethods = this.IsNoCode(nameof(Prop_AllowedMethods)) ? "POST" : "\"POST\"";
            this.Prop_ResponseCode   = "200";
            this.Prop_ResponseBody   = this.IsNoCode(nameof(Prop_ResponseBody)) ? "" : "\"\"";
        }

        // =====================================================================
        // Выполнение
        // =====================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            HttpListener listener = null;
            try
            {
                // ── Читаем параметры ───────────────────────────────────────

                string prefix = GetPropertyValue<string>(
                    this.Prop_Prefix, nameof(Prop_Prefix), sd);
                if (string.IsNullOrWhiteSpace(prefix))
                    return Fail("URL-префикс обязателен");

                // Нормализуем префикс — должен завершаться слешем
                if (!prefix.EndsWith("/"))
                    prefix += "/";

                object timeoutObj = GetPropertyValue(
                    this.Prop_TimeoutSeconds, nameof(Prop_TimeoutSeconds), sd);
                int.TryParse(timeoutObj?.ToString(), out int timeoutSec);
                if (timeoutSec <= 0) timeoutSec = 120;

                string allowedMethodsRaw = GetPropertyValue<string>(
                    this.Prop_AllowedMethods, nameof(Prop_AllowedMethods), sd) ?? string.Empty;

                // Разбираем разрешённые методы через LINQ
                HashSet<string> allowedMethods = new HashSet<string>(
                    allowedMethodsRaw
                        .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(m => m.Trim().ToUpperInvariant())
                        .Where(m => !string.IsNullOrEmpty(m)),
                    StringComparer.OrdinalIgnoreCase);

                string secretToken = GetPropertyValue<string>(
                    this.Prop_SecretToken, nameof(Prop_SecretToken), sd) ?? string.Empty;

                object codeObj = GetPropertyValue(
                    this.Prop_ResponseCode, nameof(Prop_ResponseCode), sd);
                int.TryParse(codeObj?.ToString(), out int responseCode);
                if (responseCode <= 0) responseCode = 200;

                string responseBodyText = GetPropertyValue<string>(
                    this.Prop_ResponseBody, nameof(Prop_ResponseBody), sd) ?? string.Empty;

                // ── Запускаем HttpListener ─────────────────────────────────

                listener = new HttpListener();
                listener.Prefixes.Add(prefix);

                try
                {
                    listener.Start();
                }
                catch (HttpListenerException ex)
                {
                    // Понятное сообщение об ошибке прав доступа
                    if (ex.ErrorCode == 5) // Access Denied
                        return Fail(
                            $"Нет прав для прослушивания {prefix}.\n" +
                            $"Для localhost права не нужны.\n" +
                            $"Для других адресов выполните:\n" +
                            $"  netsh http add urlacl url={prefix} user={Environment.UserDomainName}\\{Environment.UserName}");

                    return Fail($"Ошибка запуска сервера: {ex.Message} (код {ex.ErrorCode})");
                }

                // ── Ждём входящий запрос с таймаутом ──────────────────────

                // GetContextAsync + CancellationToken через Task.WhenAny
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec)))
                {
                    var contextTask = listener.GetContextAsync();

                    // Ждём либо запрос либо таймаут через Task.WhenAny
                    var completedTask = System.Threading.Tasks.Task.WhenAny(
                        contextTask,
                        System.Threading.Tasks.Task.Delay(timeoutSec * 1000, cts.Token)
                    ).ConfigureAwait(false).GetAwaiter().GetResult();

                    bool received = (completedTask == contextTask);

                    if (!received || cts.IsCancellationRequested)
                    {
                        // Таймаут — штатный результат, не ошибка
                        SetVariableValue(this.Prop_IsTimeout,    true,        sd);
                        SetVariableValue(this.Prop_IsTokenValid, true,        sd);
                        SetVariableValue(this.Prop_ReceivedBody, string.Empty, sd);

                        return new ExecutionResult
                        {
                            IsSuccess      = true,
                            SuccessMessage = $"Таймаут {timeoutSec} с — запрос не получен"
                        };
                    }

                    // ── Обрабатываем входящий запрос ──────────────────────────

                    HttpListenerContext context  = contextTask.ConfigureAwait(false).GetAwaiter().GetResult();
                HttpListenerRequest  request  = context.Request;
                HttpListenerResponse response = context.Response;

                string receivedMethod = request.HttpMethod.ToUpperInvariant();
                string receivedUrl    = request.Url?.ToString() ?? string.Empty;

                // Проверяем разрешённый метод
                if (allowedMethods.Count > 0 && !allowedMethods.Contains(receivedMethod))
                {
                    SendResponse(response, 405, $"Метод {receivedMethod} не разрешён");

                    return Fail(
                        $"Получен запрос с методом {receivedMethod}, " +
                        $"разрешены: {string.Join(", ", allowedMethods)}");
                }

                // Читаем тело запроса
                string receivedBody;
                using (var reader = new StreamReader(request.InputStream,
                    request.ContentEncoding ?? Encoding.UTF8))
                {
                    receivedBody = reader.ReadToEnd();
                }

                // Читаем заголовки в Dictionary через LINQ
                Dictionary<string, string> receivedHeaders = request.Headers.AllKeys
                    .Where(k => k != null)
                    .ToDictionary(
                        k  => k,
                        k  => request.Headers[k] ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase
                    );

                // Читаем query-параметры через LINQ
                Dictionary<string, string> queryParams = request.QueryString.AllKeys
                    .Where(k => k != null)
                    .ToDictionary(
                        k  => k,
                        k  => request.QueryString[k] ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase
                    );

                // ── Проверяем секретный токен ──────────────────────────────

                bool isTokenValid = true;
                if (!string.IsNullOrWhiteSpace(secretToken))
                {
                    // Проверяем стандартные заголовки для токенов
                    string receivedToken =
                        receivedHeaders.TryGetValue("X-Webhook-Secret", out string t1) ? t1 :
                        receivedHeaders.TryGetValue("X-Hub-Signature-256", out string t2) ? t2 :
                        receivedHeaders.TryGetValue("Authorization", out string t3) ? t3 :
                        string.Empty;

                    isTokenValid = string.Equals(
                        receivedToken.TrimStart(),
                        secretToken,
                        StringComparison.Ordinal);
                }

                // ── Отправляем ответ отправителю ──────────────────────────

                SendResponse(response, responseCode, responseBodyText);

                // ── Записываем выходные параметры ─────────────────────────

                SetVariableValue(this.Prop_ReceivedBody,    receivedBody,    sd);
                SetVariableValue(this.Prop_ReceivedHeaders, receivedHeaders, sd);
                SetVariableValue(this.Prop_ReceivedMethod,  receivedMethod,  sd);
                SetVariableValue(this.Prop_ReceivedUrl,     receivedUrl,     sd);
                SetVariableValue(this.Prop_QueryParams,     queryParams,     sd);
                SetVariableValue(this.Prop_IsTimeout,       false,           sd);
                SetVariableValue(this.Prop_IsTokenValid,    isTokenValid,    sd);

                    string tokenWarning = isTokenValid ? string.Empty : " ⚠ токен не совпал";
                    return new ExecutionResult
                    {
                        IsSuccess      = true,
                        SuccessMessage =
                            $"Получен {receivedMethod} от {request.RemoteEndPoint}" +
                            $", тело: {receivedBody.Length} байт{tokenWarning}"
                    };
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                return Fail($"Ошибка HTTP: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                return Fail($"Превышено время ожидания: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка вебхук-сервера: {ex.Message}");
            }
            finally
            {
                // Всегда останавливаем сервер — даже при исключении
                try { listener?.Stop(); } catch { /* игнорируем */ }
                try { listener?.Close(); } catch { /* игнорируем */ }
            }
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>
        /// Отправляет HTTP-ответ отправителю запроса.
        /// Всегда закрывает поток — иначе соединение зависнет.
        /// </summary>
        private static void SendResponse(
            HttpListenerResponse response, int statusCode, string body)
        {
            try
            {
                response.StatusCode  = statusCode;
                response.ContentType = "application/json; charset=utf-8";

                byte[] buffer = Encoding.UTF8.GetBytes(body ?? string.Empty);
                response.ContentLength64 = buffer.Length;

                if (buffer.Length > 0)
                    response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            finally
            {
                // Close() обязателен — отправляет ответ и освобождает соединение
                try { response.OutputStream.Close(); } catch { /* игнорируем */ }
            }
        }

        // =====================================================================
        // Валидация
        // =====================================================================

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_Prefix))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_Prefix),
                    Error        = "URL-префикс обязателен. Пример: http://localhost:8080/webhook/"
                });

            return ret;
        }

        private static ExecutionResult Fail(string msg) =>
            new ExecutionResult { IsSuccess = false, ErrorMessage = msg };
    }
}
