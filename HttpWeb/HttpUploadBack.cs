// =============================================================================
// HttpUploadBack.cs — активность «HTTP: Загрузить файл».
//
// Загружает файлы на сервер через multipart/form-data с поддержкой:
// - Одиночных и множественных файлов
// - Дополнительных полей формы
// - Настраиваемого MIME-типа
// - SSL и клиентских сертификатов
//
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для загрузки файлов на сервер через HTTP multipart/form-data.
    /// </summary>
    public class HttpUploadBack : HttpActivityBase<HttpUpload>
    {
        // ── Входные параметры: Основные ───────────────────────────────────

        #region Prop_Url

        private string _propUrl;

        /// <summary>
        /// URL endpoint для загрузки файла.
        /// Пример: "https://api.example.com/upload"
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

        #region Prop_FilePath

        private string _propFilePath;

        /// <summary>
        /// Локальный путь к файлу для загрузки.
        /// Пример: "C:\Documents\report.pdf"
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Путь к файлу")]
        public string Prop_FilePath
        {
            get => _propFilePath;
            set { _propFilePath = value; InvokePropertyChanged(this, nameof(Prop_FilePath)); }
        }

        #endregion

        #region Prop_FieldName

        private string _propFieldName;

        /// <summary>
        /// Имя поля form-data для файла.
        /// По умолчанию: "file".
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Имя поля")]
        public string Prop_FieldName
        {
            get => _propFieldName;
            set { _propFieldName = value; InvokePropertyChanged(this, nameof(Prop_FieldName)); }
        }

        #endregion

        #region Prop_AdditionalFields

        private string _propAdditionalFields;

        /// <summary>
        /// Дополнительные поля формы в формате JSON.
        /// Пример: {"category": "documents", "description": "Отчёт за 2024 год"}
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Доп. поля (JSON)")]
        public string Prop_AdditionalFields
        {
            get => _propAdditionalFields;
            set { _propAdditionalFields = value; InvokePropertyChanged(this, nameof(Prop_AdditionalFields)); }
        }

        #endregion

        #region Prop_Headers

        private string _propHeaders;

        /// <summary>
        /// Заголовки запроса в формате JSON.
        /// Пример: {"Authorization": "Bearer token"}
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

        #region Prop_Timeout

        private string _propTimeout;

        /// <summary>
        /// Таймаут загрузки в секундах.
        /// Для больших файлов рекомендуется увеличить.
        /// По умолчанию: 300 секунд.
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

        // ── Выходные параметры ────────────────────────────────────────────

        #region Prop_StatusCode

        private string _propStatusCode;

        /// <summary>
        /// HTTP код ответа сервера.
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
        /// Заголовки ответа сервера в формате JSON.
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

        #region Prop_Success

        private string _propSuccess;

        /// <summary>
        /// Признак успешной загрузки.
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
        /// Конструктор активности «HTTP: Загрузить файл».
        /// </summary>
        public HttpUploadBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "HTTP: Загрузить файл";
            sdkComponentHelp =
                "Загружает файлы на сервер через multipart/form-data.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "URL              — endpoint для загрузки\n" +
                "Путь к файлу     — локальный путь к файлу\n" +
                "Имя поля         — имя поля form-data (по умолчанию 'file')\n" +
                "Доп. поля (JSON) — дополнительные поля формы\n" +
                "Заголовки (JSON) — заголовки запроса\n" +
                "Таймаут (сек)    — время ожидания (по умолчанию 300)\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Код ответа       — HTTP status code\n" +
                "Тело ответа      — ответ сервера\n" +
                "Заголовки ответа — заголовки в формате JSON\n" +
                "Успешно          — признак успешной загрузки\n" +
                "\n" +
                "── Сертификаты и SSL ──────────────────────────\n" +
                "Наследует настройки SSL и сертификатов от базового класса.";

            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<string>("Prop_Url", "URL endpoint для загрузки"),
                PropertyBuilder.FileSelector("Prop_FilePath", "Локальный путь к файлу"),
                PropertyBuilder.Script<string>("Prop_FieldName", "Имя поля form-data"),
                PropertyBuilder.Script<string>("Prop_AdditionalFields", "Дополнительные поля формы (JSON)"),
                PropertyBuilder.Script<string>("Prop_Headers", "Заголовки запроса в формате JSON"),
                PropertyBuilder.Script<int>("Prop_Timeout", "Таймаут загрузки в секундах"),
                PropertyBuilder.BooleanObject("Prop_IgnoreSslErrors", "Игнорировать ошибки SSL"),
                PropertyBuilder.BooleanObject("Prop_UseCertificate", "Использовать клиентский сертификат"),
                PropertyBuilder.FileSelector("Prop_CertPath", "Путь к файлу сертификата (.pfx)"),
                PropertyBuilder.Script<string>("Prop_CertPassword", "Пароль к файлу сертификата"),
                PropertyBuilder.Variable<int>("Prop_StatusCode", "HTTP код ответа"),
                PropertyBuilder.Variable<string>("Prop_ResponseContent", "Тело ответа сервера"),
                PropertyBuilder.Variable<string>("Prop_ResponseHeaders", "Заголовки ответа (JSON)"),
                PropertyBuilder.Variable<bool>("Prop_Success", "Признак успешной загрузки")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_Url = "\"https://api.example.com/upload\"";
            this.Prop_FilePath = "\"C:\\Documents\\file.pdf\"";
            this.Prop_FieldName = "\"file\"";
            this.Prop_AdditionalFields = "\"{}\"";
            this.Prop_Headers = "\"{}\"";
            this.Prop_Timeout = "300";
        }

        // ── TimedAction — точка входа ──────────────────────────────────────

        /// <summary>
        /// Основной метод выполнения загрузки файла.
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Чтение входных параметров ──────────────────────────────
                string url = GetPropertyValue<string>(this.Prop_Url, nameof(Prop_Url), sd);
                if (string.IsNullOrWhiteSpace(url))
                    throw new ArgumentException("URL не может быть пустым");

                string filePath = GetPropertyValue<string>(this.Prop_FilePath, nameof(Prop_FilePath), sd);
                if (string.IsNullOrWhiteSpace(filePath))
                    throw new ArgumentException("Путь к файлу не может быть пустым");

                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"Файл не найден: {filePath}");

                string fieldName = GetPropertyValue<string>(this.Prop_FieldName, nameof(Prop_FieldName), sd);
                if (string.IsNullOrWhiteSpace(fieldName))
                    fieldName = "file";

                string additionalFieldsJson = GetPropertyValue<string>(this.Prop_AdditionalFields, nameof(Prop_AdditionalFields), sd) ?? "{}";
                string headersJson = GetPropertyValue<string>(this.Prop_Headers, nameof(Prop_Headers), sd) ?? "{}";

                string timeoutStr = GetPropertyValue<string>(this.Prop_Timeout, nameof(Prop_Timeout), sd) ?? "300";
                int timeout = HttpHelper.ParseTimeout(timeoutStr, 300);

                // ── Создание HttpClient ───────────────────────────────────
                using (var client = CreateHttpClient(sd, timeout))
                {
                    // Добавление заголовков
                    var headers = HttpHelper.ParseHeaders(headersJson);
                    var normalizedHeaders = HttpHelper.NormalizeHeaders(headers);
                    normalizedHeaders.ToList()
                        .ForEach(h => client.DefaultRequestHeaders.TryAddWithoutValidation(h.Key, h.Value));

                    // ── Создание multipart content ─────────────────────────
                    using (var content = CreateMultipartContent(filePath, fieldName, additionalFieldsJson))
                    {
                        // ── Выполнение запроса ─────────────────────────────
                        var response = client.PostAsync(url, content).ConfigureAwait(false).GetAwaiter().GetResult();
                        int statusCode = (int)response.StatusCode;

                        // ── Обработка ответа ───────────────────────────────
                        string responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                        string responseHeaders = HttpHelper.SerializeResponseHeaders(response.Headers, response.Content.Headers);

                        // ── Запись выходных параметров ────────────────────
                        if (!string.IsNullOrWhiteSpace(this.Prop_StatusCode))
                            SetVariableValue(this.Prop_StatusCode, statusCode, sd);

                        if (!string.IsNullOrWhiteSpace(this.Prop_ResponseContent))
                            SetVariableValue(this.Prop_ResponseContent, responseContent, sd);

                        if (!string.IsNullOrWhiteSpace(this.Prop_ResponseHeaders))
                            SetVariableValue(this.Prop_ResponseHeaders, responseHeaders, sd);

                        if (!string.IsNullOrWhiteSpace(this.Prop_Success))
                            SetVariableValue(this.Prop_Success, response.IsSuccessStatusCode, sd);

                        if (response.IsSuccessStatusCode)
                        {
                            return new ExecutionResult
                            {
                                IsSuccess = true,
                                SuccessMessage = $"[HTTP: Загрузить файл] Загружено: {filePath} → {url} (HTTP {statusCode})"
                            };
                        }
                        else
                        {
                            return new ExecutionResult
                            {
                                IsSuccess = false,
                                ErrorMessage = $"Ошибка загрузки: HTTP {statusCode} — {response.ReasonPhrase}. Ответ: {responseContent}"
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [HTTP: Загрузить файл]: {ex.Message}"
                };
            }
        }

        // ── Приватные методы ──────────────────────────────────────────────

        /// <summary>
        /// Создаёт MultipartFormDataContent для загрузки файла.
        /// Использует потоковую загрузку для поддержки больших файлов.
        /// ВНИМАНИЕ: MultipartFormDataContent接管 владение FileStream и диспозит его при своём Dispose.
        /// </summary>
        private MultipartFormDataContent CreateMultipartContent(string filePath, string fieldName, string additionalFieldsJson)
        {
            var content = new MultipartFormDataContent();

            // Добавляем файл через StreamContent для поддержки больших файлов.
            // FileStream передаёт владение StreamContent, который диспозится вместе с MultipartFormDataContent.
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            var fileContent = new StreamContent(fileStream);
            string mimeType = HttpHelper.GetMimeType(filePath);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

            string fileName = Path.GetFileName(filePath);
            content.Add(fileContent, fieldName, fileName);

            // Добавляем дополнительные поля
            var additionalFields = HttpHelper.ParseHeaders(additionalFieldsJson);
            foreach (var field in additionalFields)
            {
                if (!string.IsNullOrWhiteSpace(field.Key))
                    content.Add(new StringContent(field.Value ?? string.Empty), field.Key);
            }

            return content;
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

            if (string.IsNullOrWhiteSpace(this.Prop_FilePath))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = "Prop_FilePath",
                    Error = "Путь к файлу не может быть пустым"
                });
            }

            return ret;
        }
    }
}
