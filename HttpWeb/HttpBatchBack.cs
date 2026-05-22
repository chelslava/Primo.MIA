// =============================================================================
// HttpBatchBack.cs — активность «HTTP: Пакетные запросы».
//
// Выполняет несколько HTTP запросов из списка.
// Поддерживает параллельное и последовательное выполнение.
//
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для выполнения пакетных HTTP запросов.
    /// </summary>
    public class HttpBatchBack : HttpActivityBase<HttpBatch>
    {
        // ── Входные параметры ─────────────────────────────────────────────

        #region Prop_RequestsTable

        private string _propRequestsTable;

        /// <summary>
        /// Таблица с запросами (DataTable).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Таблица запросов")]
        public string Prop_RequestsTable
        {
            get => _propRequestsTable;
            set { _propRequestsTable = value; InvokePropertyChanged(this, nameof(Prop_RequestsTable)); }
        }

        #endregion

        #region Prop_MaxParallel

        private string _propMaxParallel = "5";

        /// <summary>
        /// Максимальное количество параллельных запросов.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Макс. параллельных")]
        public string Prop_MaxParallel
        {
            get => _propMaxParallel;
            set { _propMaxParallel = value; InvokePropertyChanged(this, nameof(Prop_MaxParallel)); }
        }

        #endregion

        #region Prop_StopOnError

        private bool _propStopOnError = false;

        /// <summary>
        /// Остановить при ошибке.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Остановить при ошибке")]
        public bool Prop_StopOnError
        {
            get => _propStopOnError;
            set { _propStopOnError = value; InvokePropertyChanged(this, nameof(Prop_StopOnError)); }
        }

        #endregion

        #region Prop_TimeoutSeconds

        private string _propTimeoutSeconds = "30";

        /// <summary>
        /// Таймаут выполнения всех запросов (секунды).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Таймаут (сек)")]
        public string Prop_TimeoutSeconds
        {
            get => _propTimeoutSeconds;
            set { _propTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_TimeoutSeconds)); }
        }

        #endregion

        // ── Выходные параметры ────────────────────────────────────────────

        #region Prop_ResultsTable

        private string _propResultsTable;

        /// <summary>
        /// Таблица с результатами (DataTable).
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Таблица результатов")]
        public string Prop_ResultsTable
        {
            get => _propResultsTable;
            set { _propResultsTable = value; InvokePropertyChanged(this, nameof(Prop_ResultsTable)); }
        }

        #endregion

        #region Prop_SuccessCount

        private string _propSuccessCount;

        /// <summary>
        /// Количество успешных запросов.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Успешных")]
        public string Prop_SuccessCount
        {
            get => _propSuccessCount;
            set { _propSuccessCount = value; InvokePropertyChanged(this, nameof(Prop_SuccessCount)); }
        }

        #endregion

        #region Prop_ErrorCount

        private string _propErrorCount;

        /// <summary>
        /// Количество ошибок.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Ошибок")]
        public string Prop_ErrorCount
        {
            get => _propErrorCount;
            set { _propErrorCount = value; InvokePropertyChanged(this, nameof(Prop_ErrorCount)); }
        }

        #endregion

        // ── Конструктор ───────────────────────────────────────────────────

        /// <summary>
        /// Конструктор активности «HTTP: Пакетные запросы».
        /// </summary>
        public HttpBatchBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "HTTP: Пакетные запросы";
            sdkComponentHelp =
                "Выполняет несколько HTTP запросов из таблицы.\n" +
                "\n" +
                "── Входные параметры ──────────────────────────\n" +
                "Таблица запросов — DataTable с колонками:\n" +
                "  • Url (обязательно)\n" +
                "  • Method (GET/POST/PUT/DELETE, по умолчанию GET)\n" +
                "  • Headers (JSON-строка с заголовками)\n" +
                "  • Body (тело запроса)\n" +
                "  • ContentType (тип содержимого)\n" +
                "\n" +
                "Макс. параллельных — ограничение параллельных запросов\n" +
                "Остановить при ошибке — прервать при первой ошибке\n" +
                "\n" +
                "── Выходные параметры ─────────────────────────\n" +
                "Таблица результатов — DataTable с колонками:\n" +
                "  • Index, Url, StatusCode, Success, Response, Error\n" +
                "Успешных — количество успешных запросов\n" +
                "Ошибок — количество ошибок";

            sdkComponentIcon = ActivityIcons.Http;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<DataTable>("Prop_RequestsTable", "Таблица запросов"),
                PropertyBuilder.Int("Prop_MaxParallel", "Макс. параллельных"),
                PropertyBuilder.BooleanObject("Prop_StopOnError", "Остановить при ошибке"),
                PropertyBuilder.Int("Prop_TimeoutSeconds", "Таймаут (сек)"),
                PropertyBuilder.BooleanObject("Prop_IgnoreSslErrors", "Игнорировать ошибки SSL"),
                PropertyBuilder.Variable<DataTable>("Prop_ResultsTable", "Таблица результатов"),
                PropertyBuilder.Variable<int>("Prop_SuccessCount", "Успешных"),
                PropertyBuilder.Variable<int>("Prop_ErrorCount", "Ошибок")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_MaxParallel = "5";
            this.Prop_TimeoutSeconds = "30";
        }

        // ── TimedAction — точка входа ──────────────────────────────────────

        /// <summary>
        /// Основной метод выполнения пакетных запросов.
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Чтение входных параметров ──────────────────────────────
                DataTable requestsTable = GetPropertyValue<DataTable>(this.Prop_RequestsTable, nameof(Prop_RequestsTable), sd);
                if (requestsTable == null || requestsTable.Rows.Count == 0)
                    throw new ArgumentException("Таблица запросов пуста или не задана");

                string maxParallelStr = GetPropertyValue<string>(this.Prop_MaxParallel, nameof(Prop_MaxParallel), sd);
                int maxParallel = 5;
                if (!string.IsNullOrWhiteSpace(maxParallelStr) && int.TryParse(maxParallelStr, out int mp))
                {
                    maxParallel = mp;
                }
                if (maxParallel < 1) maxParallel = 1;
                if (maxParallel > 50) maxParallel = 50;

                string timeoutStr = GetPropertyValue<string>(this.Prop_TimeoutSeconds, nameof(Prop_TimeoutSeconds), sd);
                int timeoutSeconds = 30;
                if (!string.IsNullOrWhiteSpace(timeoutStr) && int.TryParse(timeoutStr, out int to))
                {
                    timeoutSeconds = to;
                }
                if (timeoutSeconds < 1) timeoutSeconds = 30;

                // ── Создание таблицы результатов ───────────────────────────
                var resultsTable = CreateResultsTable();

                // ── Подготовка списка запросов ─────────────────────────────
                var requests = PrepareRequests(requestsTable);

                // ── Выполнение запросов ───────────────────────────────────
                int successCount = 0;
                int errorCount = 0;
                bool stoppedOnError = false;

                using (var client = CreateHttpClient(sd, timeoutSeconds))
                {
                    // Ограничение параллельности через SemaphoreSlim с корректным Dispose
                    using (var semaphore = new System.Threading.SemaphoreSlim(maxParallel, maxParallel))
                    {
                        var tasks = new List<Task<BatchResult>>();

                        for (int i = 0; i < requests.Count; i++)
                        {
                            var request = requests[i];
                            int index = i;

                            var task = Task.Run(async () =>
                            {
                                await semaphore.WaitAsync();
                                try
                                {
                                    return await ExecuteRequestAsync(client, index, request);
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            });

                            tasks.Add(task);
                        }

                        // Ожидание завершения всех задач
                        bool allCompleted = Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(timeoutSeconds + 10));

                        // Сбор результатов
                        for (int i = 0; i < tasks.Count; i++)
                        {
                            var task = tasks[i];
                            var request = requests[i];

                            if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
                            {
                                var result = task.Result;
                                AddResultRow(resultsTable, result);

                                if (result.Success)
                                    successCount++;
                                else
                                {
                                    errorCount++;
                                    if (this.Prop_StopOnError)
                                    {
                                        stoppedOnError = true;
                                        break;
                                    }
                                }
                            }
                            else if (task.IsFaulted)
                            {
                                errorCount++;
                                string errorMsg = task.Exception?.InnerException?.Message ?? task.Exception?.Message ?? "Неизвестная ошибка";
                                AddResultRow(resultsTable, new BatchResult
                                {
                                    Index = i,
                                    Url = request?.Url ?? "",
                                    Success = false,
                                    Error = $"Ошибка: {errorMsg}"
                                });
                            }
                            else if (task.IsCanceled)
                            {
                                errorCount++;
                                AddResultRow(resultsTable, new BatchResult
                                {
                                    Index = i,
                                    Url = request?.Url ?? "",
                                    Success = false,
                                    Error = "Задача отменена"
                                });
                            }
                            else
                            {
                                // Задача не завершилась за timeout — помечаем как таймаут
                                errorCount++;
                                AddResultRow(resultsTable, new BatchResult
                                {
                                    Index = i,
                                    Url = request?.Url ?? "",
                                    Success = false,
                                    Error = !allCompleted ? "Таймаут выполнения пакета" : "Таймаут выполнения запроса"
                                });
                            }
                        }
                    }
                }

                // ── Запись выходных параметров ────────────────────────────
                if (!string.IsNullOrWhiteSpace(this.Prop_ResultsTable))
                    SetVariableValue(this.Prop_ResultsTable, resultsTable, sd);

                if (!string.IsNullOrWhiteSpace(this.Prop_SuccessCount))
                    SetVariableValue(this.Prop_SuccessCount, successCount, sd);

                if (!string.IsNullOrWhiteSpace(this.Prop_ErrorCount))
                    SetVariableValue(this.Prop_ErrorCount, errorCount, sd);

                string statusMsg = stoppedOnError
                    ? " (остановлено при ошибке)"
                    : "";

                return new ExecutionResult
                {
                    IsSuccess = errorCount == 0,
                    SuccessMessage = $"[HTTP: Пакетные запросы] Выполнено: {successCount} успешно, {errorCount} ошибок{statusMsg}",
                    ErrorMessage = errorCount > 0 ? $"Ошибок: {errorCount}" : null
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [HTTP: Пакетные запросы]: {ex.Message}"
                };
            }
        }

        // ── Приватные методы ──────────────────────────────────────────────

        /// <summary>
        /// Создаёт таблицу результатов.
        /// </summary>
        private DataTable CreateResultsTable()
        {
            var table = new DataTable("Results");
            table.Columns.Add("Index", typeof(int));
            table.Columns.Add("Url", typeof(string));
            table.Columns.Add("StatusCode", typeof(int));
            table.Columns.Add("Success", typeof(bool));
            table.Columns.Add("Response", typeof(string));
            table.Columns.Add("Error", typeof(string));
            return table;
        }

        /// <summary>
        /// Подготавливает список запросов из таблицы.
        /// </summary>
        private List<BatchRequest> PrepareRequests(DataTable table)
        {
            var requests = new List<BatchRequest>();

            foreach (DataRow row in table.Rows)
            {
                var request = new BatchRequest
                {
                    Url = row["Url"]?.ToString() ?? "",
                    Method = row.Table.Columns.Contains("Method")
                        ? row["Method"]?.ToString()?.ToUpper() ?? "GET"
                        : "GET",
                    Headers = row.Table.Columns.Contains("Headers")
                        ? row["Headers"]?.ToString() ?? ""
                        : "",
                    Body = row.Table.Columns.Contains("Body")
                        ? row["Body"]?.ToString() ?? ""
                        : "",
                    ContentType = row.Table.Columns.Contains("ContentType")
                        ? row["ContentType"]?.ToString() ?? "application/json"
                        : "application/json"
                };

                if (!string.IsNullOrWhiteSpace(request.Url))
                    requests.Add(request);
            }

            return requests;
        }

        /// <summary>
        /// Выполняет один HTTP запрос асинхронно.
        /// Заголовки добавляются в индивидуальный запрос для избежания race condition.
        /// </summary>
        private async Task<BatchResult> ExecuteRequestAsync(HttpClient client, int index, BatchRequest request)
        {
            var result = new BatchResult
            {
                Index = index,
                Url = request.Url
            };

            try
            {
                // Создание запроса
                HttpRequestMessage httpRequest = CreateHttpRequest(request);

                // Добавление заголовков в индивидуальный запрос (не в client.DefaultRequestHeaders)
                if (!string.IsNullOrWhiteSpace(request.Headers))
                {
                    var headers = ParseHeaders(request.Headers);
                    foreach (var header in headers)
                    {
                        httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                // Выполнение
                var response = await client.SendAsync(httpRequest).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                result.StatusCode = (int)response.StatusCode;
                result.Success = response.IsSuccessStatusCode;
                result.Response = content;

                if (!response.IsSuccessStatusCode)
                    result.Error = $"HTTP {result.StatusCode}";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Создаёт HttpRequestMessage из BatchRequest.
        /// </summary>
        private HttpRequestMessage CreateHttpRequest(BatchRequest request)
        {
            HttpMethod method;
            string methodUpper = (request.Method ?? "GET").ToUpperInvariant();

            switch (methodUpper)
            {
                case "GET":
                    method = HttpMethod.Get;
                    break;
                case "POST":
                    method = HttpMethod.Post;
                    break;
                case "PUT":
                    method = HttpMethod.Put;
                    break;
                case "DELETE":
                    method = HttpMethod.Delete;
                    break;
                case "PATCH":
                    method = new HttpMethod("PATCH");
                    break;
                default:
                    method = HttpMethod.Get;
                    break;
            }

            var httpRequest = new HttpRequestMessage(method, new Uri(request.Url));

            if (!string.IsNullOrWhiteSpace(request.Body) && method != HttpMethod.Get)
            {
                httpRequest.Content = new StringContent(request.Body, System.Text.Encoding.UTF8, request.ContentType);
            }

            return httpRequest;
        }

        /// <summary>
        /// Парсит заголовки из JSON-строки.
        /// </summary>
        private Dictionary<string, string> ParseHeaders(string headersJson)
        {
            var headers = new Dictionary<string, string>();

            try
            {
                if (string.IsNullOrWhiteSpace(headersJson))
                    return headers;

                var obj = Newtonsoft.Json.Linq.JObject.Parse(headersJson);
                foreach (var prop in obj.Properties())
                {
                    headers[prop.Name] = prop.Value?.ToString() ?? "";
                }
            }
            catch
            {
                // Игнорируем ошибки парсинга заголовков
            }

            return headers;
        }

        /// <summary>
        /// Добавляет строку результата в таблицу.
        /// </summary>
        private void AddResultRow(DataTable table, BatchResult result)
        {
            var row = table.NewRow();
            row["Index"] = result.Index;
            row["Url"] = result.Url;
            row["StatusCode"] = result.StatusCode;
            row["Success"] = result.Success;
            row["Response"] = result.Response ?? "";
            row["Error"] = result.Error ?? "";
            table.Rows.Add(row);
        }

        // ── Внутренние классы ─────────────────────────────────────────────

        /// <summary>
        /// Модель запроса для пакета.
        /// </summary>
        private class BatchRequest
        {
            public string Url { get; set; }
            public string Method { get; set; }
            public string Headers { get; set; }
            public string Body { get; set; }
            public string ContentType { get; set; }
        }

        /// <summary>
        /// Модель результата запроса.
        /// </summary>
        private class BatchResult
        {
            public int Index { get; set; }
            public string Url { get; set; }
            public int StatusCode { get; set; }
            public bool Success { get; set; }
            public string Response { get; set; }
            public string Error { get; set; }
        }

        // ── Валидация ─────────────────────────────────────────────────────

        /// <summary>
        /// Валидация параметров активности.
        /// </summary>
        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_RequestsTable))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = "Prop_RequestsTable",
                    Error = "Таблица запросов не может быть пустой"
                });
            }

            return ret;
        }
    }
}
