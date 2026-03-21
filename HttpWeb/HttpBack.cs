// =============================================================================
// HttpBack.cs — активность «HTTP Запрос».
//
// Выполняет HTTP-запросы к внешним API с поддержкой различных методов,
// настройкой заголовков, тела запроса, таймаутов и клиентских сертификатов.
//
// Поддерживаемые методы:
//   GET    — получение данных
//   POST   — отправка данных
//   PUT    — обновление ресурса
//   DELETE — удаление ресурса
//   PATCH  — частичное обновление
//
// ВАЖНО: При использовании клиентского сертификата файл .pfx должен существовать.
//        Для игнорирования SSL-ошибок используйте флаг только в тестовых средах.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для выполнения HTTP-запросов к внешним API.
    /// Поддерживает настройку заголовков, тела запроса, таймаутов и клиентских сертификатов.
    /// </summary>
    public class HttpBack : PrimoComponentTO<Http>
    {
        // ── Константы и свойства SDK ───────────────────────────────────

        /// <summary>
        /// Имя группы компонента
        /// </summary>
        public override string GroupName
        {
            get => ActivityCategories.HttpWeb;
            protected set { }
        }

        /// <summary>
        /// Возвращает значение таймаута для SDK.
        /// Используется системой для контроля времени выполнения активности.
        /// </summary>
        protected override int sdkTimeOut
        {
            get
            {
                string timeoutRaw = this.Prop_Timeout?.Trim('"');
                if (string.IsNullOrWhiteSpace(timeoutRaw))
                    return 100000;

                if (int.TryParse(timeoutRaw, out int timeout))
                    return timeout * 1000;

                return 100000;
            }
            set { }
        }
        // ── Входные параметры: Основные ───────────────────────────────

        private string _propUrl;
        /// <summary>
        /// Адрес запроса (URL).
        /// Пример: "https://api.example.com/users"
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("URL")]
        public string Prop_Url
        {
            get => _propUrl;
            set { _propUrl = value; InvokePropertyChanged(this, "Prop_Url"); }
        }

        private HttpMethodType _propMethod;
        /// <summary>
        /// HTTP-метод запроса.
        /// Выберите один из: GET, POST, PUT, DELETE, PATCH.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Метод")]
        public HttpMethodType Prop_Method
        {
            get => _propMethod;
            set { _propMethod = value; InvokePropertyChanged(this, "Prop_Method"); }
        }

        private string _propHeaders;
        /// <summary>
        /// Заголовки запроса в формате JSON.
        /// Пример: {"Content-Type": "application/json", "Authorization": "Bearer token"}
        /// Если не указано — используются заголовки по умолчанию.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Заголовки (JSON)")]
        public string Prop_Headers
        {
            get => _propHeaders;
            set { _propHeaders = value; InvokePropertyChanged(this, "Prop_Headers"); }
        }

        private string _propBody;
        /// <summary>
        /// Тело запроса (для POST, PUT, PATCH).
        /// Может быть JSON, XML, plain text или любой другой формат.
        /// Пример: {"name": "John", "age": 30}
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Тело запроса")]
        public string Prop_Body
        {
            get => _propBody;
            set { _propBody = value; InvokePropertyChanged(this, "Prop_Body"); }
        }

        private string _propTimeout;
        /// <summary>
        /// Таймаут запроса в секундах.
        /// По умолчанию: 100 секунд.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Таймаут (сек)")]
        public string Prop_Timeout
        {
            get => _propTimeout;
            set { _propTimeout = value; InvokePropertyChanged(this, "Prop_Timeout"); }
        }

        // ── Входные параметры: Сертификаты и SSL ───────────────────────

        private bool _propUseCertificate;
        /// <summary>
        /// Использовать клиентский сертификат для аутентификации.
        /// Если true — необходимо указать путь к файлу .pfx и пароль.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Sertificates_SSL),
         System.ComponentModel.DisplayName("Использовать сертификат")]
        public bool Prop_UseCertificate
        {
            get => _propUseCertificate;
            set { _propUseCertificate = value; InvokePropertyChanged(this, "Prop_UseCertificate"); }
        }

        private string _propCertPath;
        /// <summary>
        /// Путь к файлу клиентского сертификата (.pfx).
        /// Пример: "C:\Certificates\client.pfx"
        /// Обязательно, если включен флаг "Использовать сертификат".
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Sertificates_SSL),
         System.ComponentModel.DisplayName("Путь к сертификату")]
        public string Prop_CertPath
        {
            get => _propCertPath;
            set { _propCertPath = value; InvokePropertyChanged(this, "Prop_CertPath"); }
        }

        private string _propCertPassword;
        /// <summary>
        /// Пароль к файлу сертификата.
        /// Используется для расшифровки .pfx файла.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Sertificates_SSL),
         System.ComponentModel.DisplayName("Пароль сертификата")]
        public string Prop_CertPassword
        {
            get => _propCertPassword;
            set { _propCertPassword = value; InvokePropertyChanged(this, "Prop_CertPassword"); }
        }

        private bool _propIgnoreSslErrors;
        /// <summary>
        /// Игнорировать ошибки проверки SSL-сертификата сервера.
        /// ВНИМАНИЕ: Используйте только в тестовых средах!
        /// В продакшене это создаёт угрозу безопасности.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Sertificates_SSL),
         System.ComponentModel.DisplayName("Игнорировать ошибки SSL")]
        public bool Prop_IgnoreSslErrors
        {
            get => _propIgnoreSslErrors;
            set { _propIgnoreSslErrors = value; InvokePropertyChanged(this, "Prop_IgnoreSslErrors"); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propStatusCode;
        /// <summary>
        /// Код HTTP-ответа сервера.
        /// Примеры: 200 (OK), 404 (Not Found), 500 (Internal Server Error).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Код ответа")]
        public string Prop_StatusCode
        {
            get => _propStatusCode;
            set { _propStatusCode = value; InvokePropertyChanged(this, "Prop_StatusCode"); }
        }

        private string _propResponseContent;
        /// <summary>
        /// Тело ответа сервера.
        /// Содержит данные, возвращённые сервером (JSON, XML, HTML и т.д.).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Тело ответа")]
        public string Prop_ResponseContent
        {
            get => _propResponseContent;
            set { _propResponseContent = value; InvokePropertyChanged(this, "Prop_ResponseContent"); }
        }

        private string _propResponseHeaders;
        /// <summary>
        /// Заголовки ответа сервера в формате JSON.
        /// Пример: {"Content-Type": "application/json", "Server": "nginx"}
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Заголовки ответа (JSON)")]
        public string Prop_ResponseHeaders
        {
            get => _propResponseHeaders;
            set { _propResponseHeaders = value; InvokePropertyChanged(this, "Prop_ResponseHeaders"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        /// <summary>
        /// Конструктор активности HTTP Запрос.
        /// Инициализирует метаданные компонента и значения по умолчанию.
        /// </summary>
        public HttpBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "HTTP Запрос";
            sdkComponentHelp =
                "Выполняет HTTP-запросы к внешним API с поддержкой различных методов.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "URL              — адрес запроса (обязательно)\n" +
                "Метод            — GET, POST, PUT, DELETE, PATCH\n" +
                "Заголовки (JSON) — словарь заголовков запроса\n" +
                "Тело запроса     — данные для POST/PUT/PATCH\n" +
                "Таймаут (сек)    — время ожидания ответа (по умолчанию 100)\n" +
                "\n" +
                "── Сертификаты и SSL ──────────────────────────\n" +
                "Использовать сертификат — флаг клиентской аутентификации\n" +
                "Путь к сертификату      — файл .pfx\n" +
                "Пароль сертификата      — для расшифровки .pfx\n" +
                "Игнорировать ошибки SSL — только для тестовых сред!\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Код ответа          — HTTP status code (200, 404, 500...)\n" +
                "Тело ответа         — содержимое ответа сервера\n" +
                "Заголовки ответа    — заголовки в формате JSON";

            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<string>("Prop_Url", "Адрес запроса (URL)"),
                PropertyBuilder.Enum<HttpMethodType>("Prop_Method", "HTTP-метод запроса"),
                PropertyBuilder.Script<string>("Prop_Headers", "Заголовки запроса в формате JSON"),
                PropertyBuilder.Script<string>("Prop_Body", "Тело запроса (для POST, PUT, PATCH)"),
                PropertyBuilder.Script<int>("Prop_Timeout", "Таймаут запроса в секундах"),
                PropertyBuilder.BooleanObject("Prop_UseCertificate", "Использовать клиентский сертификат"),
                PropertyBuilder.FileSelector("Prop_CertPath", "Путь к файлу сертификата (.pfx)"),
                PropertyBuilder.Script<string>("Prop_CertPassword", "Пароль к файлу сертификата"),
                PropertyBuilder.BooleanObject("Prop_IgnoreSslErrors", "Игнорировать ошибки SSL (только для тестовых сред)"),
                PropertyBuilder.Variable<int>("Prop_StatusCode", "Код HTTP-ответа сервера"),
                PropertyBuilder.Variable<string>("Prop_ResponseContent", "Тело ответа сервера"),
                PropertyBuilder.Variable<string>("Prop_ResponseHeaders", "Заголовки ответа в формате JSON")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_Url = "\"https://api.example.com\"";
            this.Prop_Method = HttpMethodType.GET;
            this.Prop_Headers = "\"{}\"";
            this.Prop_Body = "\"\"";
            this.Prop_Timeout = "100";
            this.Prop_UseCertificate = false;
            this.Prop_CertPath = "\"\"";
            this.Prop_CertPassword = "\"\"";
            this.Prop_IgnoreSslErrors = false;
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        /// <summary>
        /// Основной метод выполнения HTTP-запроса.
        /// Создаёт HttpClient с настройками, выполняет запрос и записывает результаты.
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Чтение входных параметров ──────────────────────────
                string url = GetPropertyValue<string>(this.Prop_Url, "Prop_Url", sd);
                if (string.IsNullOrWhiteSpace(url))
                    throw new ArgumentException("URL не может быть пустым");

                string headersJson = GetPropertyValue<string>(this.Prop_Headers, "Prop_Headers", sd) ?? "{}";
                string body = GetPropertyValue<string>(this.Prop_Body, "Prop_Body", sd) ?? string.Empty;

                string timeoutStr = GetPropertyValue<string>(this.Prop_Timeout, "Prop_Timeout", sd) ?? "100";
                int timeout = HttpLogic.ParseTimeout(timeoutStr, 100);

                // ── Создание HttpClient с настройками ──────────────────
                using (var client = CreateHttpClient(sd, timeout))
                {
                    // Парсинг и добавление заголовков
                    var headers = HttpLogic.ParseHeaders(headersJson);
                    var normalizedHeaders = HttpLogic.NormalizeHeaders(headers);
                    normalizedHeaders.ToList()
                                    .ForEach(h => client.DefaultRequestHeaders.TryAddWithoutValidation(h.Key, h.Value));

                    // ── Выполнение запроса ─────────────────────────────────
                    var response = ExecuteRequest(client, url, body, sd).Result;

                    // ── Обработка ответа ───────────────────────────────────
                    int statusCode = (int)response.StatusCode;
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    string responseHeaders = HttpLogic.SerializeResponseHeaders(response.Headers, response.Content.Headers);

                    // Запись результатов в выходные переменные
                    if (!string.IsNullOrWhiteSpace(this.Prop_StatusCode))
                        SetVariableValue(this.Prop_StatusCode, statusCode, sd);

                    if (!string.IsNullOrWhiteSpace(this.Prop_ResponseContent))
                        SetVariableValue(this.Prop_ResponseContent, responseContent, sd);

                    if (!string.IsNullOrWhiteSpace(this.Prop_ResponseHeaders))
                        SetVariableValue(this.Prop_ResponseHeaders, responseHeaders, sd);

                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = $"[HTTP Запрос] Выполнен {this.Prop_Method} {url} → {statusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [HTTP Запрос]: {ex.Message}"
                };
            }
        }

        // ── Приватные методы — реализация логики ──────────────────────

        /// <summary>
        /// Создаёт настроенный HttpClient с поддержкой сертификатов и SSL.
        /// Применяет таймаут, загружает клиентский сертификат (если указан),
        /// настраивает игнорирование SSL-ошибок (если включено).
        /// </summary>
        private HttpClient CreateHttpClient(ScriptingData sd, int timeout)
        {
            var handler = new HttpClientHandler();

            // ── Настройка клиентского сертификата ──────────────────────
            if (this.Prop_UseCertificate)
            {
                string certPath = GetPropertyValue<string>(this.Prop_CertPath, "Prop_CertPath", sd);
                string certPassword = GetPropertyValue<string>(this.Prop_CertPassword, "Prop_CertPassword", sd) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(certPath))
                    throw new ArgumentException("Путь к сертификату не указан");

                if (!System.IO.File.Exists(certPath))
                    throw new System.IO.FileNotFoundException($"Файл сертификата не найден: {certPath}");

                var certificate = new X509Certificate2(certPath, certPassword);
                handler.ClientCertificates.Add(certificate);
            }

            // ── Игнорирование SSL-ошибок (только для тестовых сред) ────
            if (this.Prop_IgnoreSslErrors)
            {
                handler.ServerCertificateCustomValidationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;
            }

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            };

            return client;
        }



        /// <summary>
        /// Выполняет HTTP-запрос в соответствии с выбранным методом.
        /// Для POST/PUT/PATCH добавляет тело запроса.
        /// Возвращает HttpResponseMessage с результатом.
        /// </summary>
        private async System.Threading.Tasks.Task<HttpResponseMessage> ExecuteRequest(
            HttpClient client, string url, string body, ScriptingData sd)
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
                    throw new NotSupportedException($"Метод {this.Prop_Method} не поддерживается");
            }

            return response;
        }

        /// <summary>
        /// Создаёт StringContent из строки тела запроса.
        /// Использует UTF-8 кодировку и тип содержимого application/json по умолчанию.
        /// </summary>
        private StringContent CreateStringContent(string body)
        {
            if (string.IsNullOrEmpty(body))
                return new StringContent(string.Empty, Encoding.UTF8, "application/json");

            return new StringContent(body, Encoding.UTF8, "application/json");
        }



        // ── Валидация ──────────────────────────────────────────────────

        /// <summary>
        /// Валидирует обязательные параметры активности.
        /// Проверяет наличие URL и корректность настроек сертификата.
        /// </summary>
        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            // URL обязателен
            ret.ValidateRequired(this.Prop_Url, "URL", ActivityStrings.Error_UrlRequired);

            // Если включен сертификат — путь обязателен
            if (this.Prop_UseCertificate)
                ret.ValidateRequired(this.Prop_CertPath, "Путь к сертификату", "Путь к сертификату обязателен при включенном флаге");

            return ret;
        }
    }
}
