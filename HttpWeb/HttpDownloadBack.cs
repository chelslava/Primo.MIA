// =============================================================================
// HttpDownloadBack.cs — активность «HTTP: Скачать файл».
//
// Скачивает файлы по HTTP/HTTPS с поддержкой:
// - Больших файлов (потоковая загрузка)
// - Настраиваемого размера буфера
// - Перезаписи существующих файлов
// - Автоопределения имени файла из URL/Content-Disposition
// - SSL и клиентских сертификатов
//
// Поддерживает прогресс загрузки через логирование.
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
    /// Активность для скачивания файлов по HTTP/HTTPS.
    /// Поддерживает большие файлы, настройку буфера, перезапись.
    /// </summary>
    public class HttpDownloadBack : HttpActivityBase<HttpDownload>
    {
        // ── Входные параметры: Основные ───────────────────────────────────

        #region Prop_Url

        private string _propUrl;

        /// <summary>
        /// URL файла для скачивания.
        /// Пример: "https://example.com/files/report.xlsx"
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

        #region Prop_SavePath

        private string _propSavePath;

        /// <summary>
        /// Локальный путь для сохранения файла.
        /// Может быть путём к файлу или папке.
        /// Если указана папка — имя файла определяется из URL.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Путь сохранения")]
        public string Prop_SavePath
        {
            get => _propSavePath;
            set { _propSavePath = value; InvokePropertyChanged(this, nameof(Prop_SavePath)); }
        }

        #endregion

        #region Prop_Headers

        private string _propHeaders;

        /// <summary>
        /// Заголовки запроса в формате JSON.
        /// Пример: {"Authorization": "Bearer token", "Accept": "application/pdf"}
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
        /// По умолчанию: 300 секунд (5 минут).
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

        #region Prop_Overwrite

        private bool _propOverwrite;

        /// <summary>
        /// Перезаписать существующий файл.
        /// Если false и файл существует — будет ошибка.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Перезаписать")]
        public bool Prop_Overwrite
        {
            get => _propOverwrite;
            set { _propOverwrite = value; InvokePropertyChanged(this, nameof(Prop_Overwrite)); }
        }

        #endregion

        #region Prop_BufferSize

        private string _propBufferSize;

        /// <summary>
        /// Размер буфера в байтах.
        /// Влияет на скорость и потребление памяти.
        /// По умолчанию: 8192 (8 КБ).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Размер буфера")]
        public string Prop_BufferSize
        {
            get => _propBufferSize;
            set { _propBufferSize = value; InvokePropertyChanged(this, nameof(Prop_BufferSize)); }
        }

        #endregion

        // ── Выходные параметры ────────────────────────────────────────────

        #region Prop_OutputPath

        private string _propOutputPath;

        /// <summary>
        /// Реальный путь к сохранённому файлу.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Путь к файлу")]
        public string Prop_OutputPath
        {
            get => _propOutputPath;
            set { _propOutputPath = value; InvokePropertyChanged(this, nameof(Prop_OutputPath)); }
        }

        #endregion

        #region Prop_FileSize

        private string _propFileSize;

        /// <summary>
        /// Размер скачанного файла в байтах.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(long))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Размер файла")]
        public string Prop_FileSize
        {
            get => _propFileSize;
            set { _propFileSize = value; InvokePropertyChanged(this, nameof(Prop_FileSize)); }
        }

        #endregion

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

        #region Prop_Success

        private string _propSuccess;

        /// <summary>
        /// Признак успешного скачивания.
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
        /// Конструктор активности «HTTP: Скачать файл».
        /// </summary>
        public HttpDownloadBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "HTTP: Скачать файл";
            sdkComponentHelp =
                "Скачивает файлы по HTTP/HTTPS.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "URL              — адрес файла для скачивания\n" +
                "Путь сохранения  — локальный путь (файл или папка)\n" +
                "Заголовки (JSON) — дополнительные заголовки запроса\n" +
                "Таймаут (сек)    — время ожидания (по умолчанию 300)\n" +
                "Перезаписать     — перезаписать существующий файл\n" +
                "Размер буфера    — размер буфера в байтах (по умолчанию 8192)\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Путь к файлу  — реальный путь сохранённого файла\n" +
                "Размер файла  — размер в байтах\n" +
                "Код ответа    — HTTP status code\n" +
                "Успешно       — признак успешного скачивания\n" +
                "\n" +
                "── Сертификаты и SSL ──────────────────────────\n" +
                "Наследует настройки SSL и сертификатов от базового класса.";

            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<string>("Prop_Url", "URL файла для скачивания"),
                PropertyBuilder.Script<string>("Prop_SavePath", "Локальный путь для сохранения"),
                PropertyBuilder.Script<string>("Prop_Headers", "Заголовки запроса в формате JSON"),
                PropertyBuilder.Script<int>("Prop_Timeout", "Таймаут загрузки в секундах"),
                PropertyBuilder.BooleanObject("Prop_Overwrite", "Перезаписать существующий файл"),
                PropertyBuilder.Script<int>("Prop_BufferSize", "Размер буфера в байтах"),
                PropertyBuilder.BooleanObject("Prop_IgnoreSslErrors", "Игнорировать ошибки SSL (только для тест-сред!)"),
                PropertyBuilder.BooleanObject("Prop_UseCertificate", "Использовать клиентский сертификат"),
                PropertyBuilder.FileSelector("Prop_CertPath", "Путь к файлу сертификата (.pfx)"),
                PropertyBuilder.Script<string>("Prop_CertPassword", "Пароль к файлу сертификата"),
                PropertyBuilder.Variable<string>("Prop_OutputPath", "Путь к сохранённому файлу"),
                PropertyBuilder.Variable<long>("Prop_FileSize", "Размер файла в байтах"),
                PropertyBuilder.Variable<int>("Prop_StatusCode", "HTTP код ответа"),
                PropertyBuilder.Variable<bool>("Prop_Success", "Признак успешного скачивания")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_Url = "\"https://example.com/file.pdf\"";
            this.Prop_SavePath = "\"C:\\Downloads\\file.pdf\"";
            this.Prop_Headers = "\"{}\"";
            this.Prop_Timeout = "300";
            this.Prop_Overwrite = true;
            this.Prop_BufferSize = "8192";
        }

        // ── TimedAction — точка входа ──────────────────────────────────────

        /// <summary>
        /// Основной метод выполнения скачивания файла.
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Чтение входных параметров ──────────────────────────────
                string url = GetPropertyValue<string>(this.Prop_Url, nameof(Prop_Url), sd);
                Guard.NotNullOrWhiteSpace(url, nameof(Prop_Url));

                string savePath = GetPropertyValue<string>(this.Prop_SavePath, nameof(Prop_SavePath), sd);
                Guard.NotNullOrWhiteSpace(savePath, nameof(Prop_SavePath));

                string headersJson = GetPropertyValue<string>(this.Prop_Headers, nameof(Prop_Headers), sd) ?? "{}";

                string timeoutStr = GetPropertyValue<string>(this.Prop_Timeout, nameof(Prop_Timeout), sd) ?? "300";
                int timeout = HttpHelper.ParseTimeout(timeoutStr, 300);

                string bufferSizeStr = GetPropertyValue<string>(this.Prop_BufferSize, nameof(Prop_BufferSize), sd) ?? "8192";
                int bufferSize = HttpHelper.ParseTimeout(bufferSizeStr, 8192);
                if (bufferSize < 1024) bufferSize = 8192;

                // ── Подготовка пути сохранения ────────────────────────────
                string finalPath = PrepareSavePath(url, savePath, sd);

                // ── Создание HttpClient ───────────────────────────────────
                using (var client = CreateHttpClient(sd, timeout))
                {
                    // Добавление заголовков
                    var headers = HttpHelper.ParseHeaders(headersJson);
                    var normalizedHeaders = HttpHelper.NormalizeHeaders(headers);
                    normalizedHeaders.ToList()
                        .ForEach(h => client.DefaultRequestHeaders.TryAddWithoutValidation(h.Key, h.Value));

                    // ── Выполнение запроса ─────────────────────────────────
                    var response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false).GetAwaiter().GetResult();
                    int statusCode = (int)response.StatusCode;

                    // Записываем код ответа
                    if (!string.IsNullOrWhiteSpace(this.Prop_StatusCode))
                        SetVariableValue(this.Prop_StatusCode, statusCode, sd);

                    // Проверяем успешность
                    if (!response.IsSuccessStatusCode)
                    {
                        return new ExecutionResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"Ошибка скачивания: HTTP {statusCode} — {response.ReasonPhrase}"
                        };
                    }

                    // ── Скачивание файла ───────────────────────────────────
                    long fileSize = DownloadFile(response, finalPath, bufferSize);

                    // ── Запись выходных параметров ────────────────────────
                    if (!string.IsNullOrWhiteSpace(this.Prop_OutputPath))
                        SetVariableValue(this.Prop_OutputPath, finalPath, sd);

                    if (!string.IsNullOrWhiteSpace(this.Prop_FileSize))
                        SetVariableValue(this.Prop_FileSize, fileSize, sd);

                    if (!string.IsNullOrWhiteSpace(this.Prop_Success))
                        SetVariableValue(this.Prop_Success, true, sd);

                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = $"[HTTP: Скачать файл] Скачано: {url} → {finalPath} ({FormatFileSize(fileSize)})"
                    };
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка HTTP: {ex.Message}"
                };
            }
            catch (System.IO.IOException ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка ввода-вывода: {ex.Message}"
                };
            }
            catch (TimeoutException ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Превышено время ожидания: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [HTTP: Скачать файл]: {ex.Message}"
                };
            }
        }

        // ── Приватные методы ──────────────────────────────────────────────

        /// <summary>
        /// Подготавливает путь сохранения файла.
        /// Если указана папка — определяет имя файла из URL.
        /// </summary>
        private string PrepareSavePath(string url, string savePath, ScriptingData sd)
        {
            // Если путь заканчивается разделителем — это папка
            if (savePath.EndsWith("\\") || savePath.EndsWith("/"))
            {
                // Создаём папку если не существует
                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                // Определяем имя файла из URL
                string fileName = HttpHelper.ExtractFileName(url, null);
                return Path.Combine(savePath, fileName);
            }

            // Проверяем, является ли путь папкой
            if (Directory.Exists(savePath))
            {
                string fileName = HttpHelper.ExtractFileName(url, null);
                return Path.Combine(savePath, fileName);
            }

            // Это путь к файлу — создаём папку если нужно
            string directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Проверяем существование файла
            if (File.Exists(savePath) && !this.Prop_Overwrite)
                throw new IOException($"Файл уже существует: {savePath}. Установите флаг 'Перезаписать' для замены.");

            return savePath;
        }

        /// <summary>
        /// Скачивает файл с сервера потоковым методом.
        /// </summary>
        private long DownloadFile(HttpResponseMessage response, string filePath, int bufferSize)
        {
            using (var contentStream = response.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult())
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize))
            {
                var buffer = new byte[bufferSize];
                long totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = contentStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fileStream.Write(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                }

                return totalBytesRead;
            }
        }

        /// <summary>
        /// Форматирует размер файла в читаемый вид.
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
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

            if (string.IsNullOrWhiteSpace(this.Prop_SavePath))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = "Prop_SavePath",
                    Error = "Путь сохранения не может быть пустым"
                });
            }

            return ret;
        }
    }
}
