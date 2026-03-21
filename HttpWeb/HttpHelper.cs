// =============================================================================
// HttpHelper.cs — вспомогательные методы для HTTP-активностей.
//
// Содержит статические методы для:
// - Парсинга заголовков
// - Валидации URL
// - Вычисления задержек retry
// - Сериализации/десериализации данных
//
// Методы спроектированы для тестируемости без зависимости от SDK.
// =============================================================================

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Primo.MIA
{
    /// <summary>
    /// Вспомогательные методы для HTTP-активностей.
    /// Содержит тестируемую логику без зависимости от SDK.
    /// </summary>
    public static class HttpHelper
    {
        // ── Константы ─────────────────────────────────────────────────────

        /// <summary>Коды HTTP, при которых следует повторить запрос.</summary>
        public static readonly int[] DefaultRetryStatusCodes = { 500, 502, 503, 504, 408, 429 };

        /// <summary>Размер буфера по умолчанию для файловых операций.</summary>
        public const int DefaultBufferSize = 8192;

        // ── Парсинг заголовков ────────────────────────────────────────────

        /// <summary>
        /// Парсит JSON-строку заголовков в словарь.
        /// </summary>
        /// <param name="headersJson">JSON-строка с заголовками. Пример: {"Content-Type": "application/json"}</param>
        /// <returns>Словарь заголовков или пустой словарь при ошибке.</returns>
        public static Dictionary<string, string> ParseHeaders(string headersJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(headersJson) || headersJson.Trim() == "{}")
                    return new Dictionary<string, string>();

                var parsed = JsonConvert.DeserializeObject<Dictionary<string, string>>(headersJson);
                return parsed ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Нормализует заголовки, удаляя пустые ключи и значения.
        /// </summary>
        /// <param name="headers">Исходный словарь заголовков.</param>
        /// <returns>Отфильтрованный словарь заголовков.</returns>
        public static Dictionary<string, string> NormalizeHeaders(Dictionary<string, string> headers)
        {
            if (headers == null)
                return new Dictionary<string, string>();

            return headers
                .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value != null)
                .ToDictionary(kvp => kvp.Key.Trim(), kvp => kvp.Value);
        }

        /// <summary>
        /// Сериализует заголовки HTTP-ответа в JSON-строку.
        /// </summary>
        /// <param name="responseHeaders">Заголовки ответа.</param>
        /// <param name="contentHeaders">Заголовки содержимого.</param>
        /// <returns>JSON-строка с заголовками.</returns>
        public static string SerializeResponseHeaders(
            HttpResponseHeaders responseHeaders,
            HttpContentHeaders contentHeaders)
        {
            var allHeaders = new Dictionary<string, string>();

            if (responseHeaders != null)
            {
                responseHeaders
                    .Where(h => h.Value != null)
                    .ToList()
                    .ForEach(h => allHeaders[h.Key] = string.Join(", ", h.Value));
            }

            if (contentHeaders != null)
            {
                contentHeaders
                    .Where(h => h.Value != null)
                    .ToList()
                    .ForEach(h => allHeaders[h.Key] = string.Join(", ", h.Value));
            }

            return JsonConvert.SerializeObject(allHeaders, Formatting.None);
        }

        // ── Валидация ─────────────────────────────────────────────────────

        /// <summary>
        /// Валидирует URL на корректность формата.
        /// </summary>
        /// <param name="url">URL для проверки.</param>
        /// <returns>true если URL валиден, иначе false.</returns>
        public static bool ValidateUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
                return false;

            return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
        }

        /// <summary>
        /// Проверяет что путь к файлу сертификата существует и имеет расширение .pfx.
        /// </summary>
        /// <param name="certPath">Путь к файлу сертификата.</param>
        /// <returns>true если файл существует и имеет расширение .pfx.</returns>
        public static bool ValidateCertificatePath(string certPath)
        {
            if (string.IsNullOrWhiteSpace(certPath))
                return false;

            if (!File.Exists(certPath))
                return false;

            return certPath.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase);
        }

        // ── Retry логика ──────────────────────────────────────────────────

        /// <summary>
        /// Парсит строку таймаута в целое число секунд.
        /// </summary>
        /// <param name="timeoutStr">Строка с таймаутом.</param>
        /// <param name="defaultValue">Значение по умолчанию (секунды).</param>
        /// <returns>Таймаут в секундах.</returns>
        public static int ParseTimeout(string timeoutStr, int defaultValue = 100)
        {
            if (string.IsNullOrWhiteSpace(timeoutStr))
                return defaultValue;

            if (!int.TryParse(timeoutStr, out int timeout))
                return defaultValue;

            return timeout > 0 ? timeout : defaultValue;
        }

        /// <summary>
        /// Вычисляет задержку для повтора запроса в зависимости от стратегии.
        /// </summary>
        /// <param name="strategy">Стратегия задержки.</param>
        /// <param name="baseDelayMs">Базовая задержка в миллисекундах.</param>
        /// <param name="attempt">Номер попытки (начиная с 1).</param>
        /// <returns>Задержка в миллисекундах.</returns>
        public static int CalculateRetryDelay(HttpRetryDelayStrategy strategy, int baseDelayMs, int attempt)
        {
            if (baseDelayMs <= 0)
                baseDelayMs = 1000;

            if (attempt <= 0)
                attempt = 1;

            switch (strategy)
            {
                case HttpRetryDelayStrategy.Fixed:
                    return baseDelayMs;

                case HttpRetryDelayStrategy.Linear:
                    return baseDelayMs * attempt;

                case HttpRetryDelayStrategy.Exponential:
                    // delay * 2^(attempt-1)
                    return baseDelayMs * (int)Math.Pow(2, attempt - 1);

                default:
                    return baseDelayMs;
            }
        }

        /// <summary>
        /// Парсит список кодов для повтора из строки.
        /// </summary>
        /// <param name="codesStr">Строка с кодами через запятую. Пример: "500,502,503"</param>
        /// <returns>Массив кодов состояния.</returns>
        public static int[] ParseRetryStatusCodes(string codesStr)
        {
            if (string.IsNullOrWhiteSpace(codesStr))
                return DefaultRetryStatusCodes;

            var codes = new List<int>();
            foreach (var part in codesStr.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), out int code) && code > 0)
                    codes.Add(code);
            }

            return codes.Count > 0 ? codes.ToArray() : DefaultRetryStatusCodes;
        }

        /// <summary>
        /// Проверяет, следует ли повторить запрос для данного кода состояния.
        /// </summary>
        /// <param name="statusCode">HTTP код состояния.</param>
        /// <param name="retryCodes">Массив кодов для повтора.</param>
        /// <returns>true если запрос следует повторить.</returns>
        public static bool ShouldRetry(int statusCode, int[] retryCodes)
        {
            if (retryCodes == null || retryCodes.Length == 0)
                retryCodes = DefaultRetryStatusCodes;

            return retryCodes.Contains(statusCode);
        }

        // ── Работа с файлами ──────────────────────────────────────────────

        /// <summary>
        /// Определяет MIME-тип по расширению файла.
        /// </summary>
        /// <param name="filePath">Путь к файлу.</param>
        /// <returns>MIME-тип или "application/octet-stream" по умолчанию.</returns>
        public static string GetMimeType(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "application/octet-stream";

            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            var mimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { ".txt", "text/plain" },
                { ".pdf", "application/pdf" },
                { ".doc", "application/msword" },
                { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                { ".xls", "application/vnd.ms-excel" },
                { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                { ".png", "image/png" },
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".gif", "image/gif" },
                { ".csv", "text/csv" },
                { ".xml", "application/xml" },
                { ".json", "application/json" },
                { ".zip", "application/zip" },
                { ".rar", "application/x-rar-compressed" }
            };

            return mimeTypes.TryGetValue(ext, out string mime) ? mime : "application/octet-stream";
        }

        /// <summary>
        /// Формирует имя файла из URL или Content-Disposition заголовка.
        /// </summary>
        /// <param name="url">URL запроса.</param>
        /// <param name="contentDisposition">Заголовок Content-Disposition.</param>
        /// <returns>Имя файла.</returns>
        public static string ExtractFileName(string url, string contentDisposition)
        {
            // Пытаемся извлечь из Content-Disposition
            if (!string.IsNullOrWhiteSpace(contentDisposition))
            {
                // filename="file.pdf" или filename=file.pdf
                var match = System.Text.RegularExpressions.Regex.Match(
                    contentDisposition,
                    @"filename[*]?=[""']?([^""';\s]+)[""']?",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                    return match.Groups[1].Value;
            }

            // Извлекаем из URL
            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    var uri = new Uri(url);
                    string fileName = Path.GetFileName(uri.LocalPath);
                    if (!string.IsNullOrWhiteSpace(fileName))
                        return fileName;
                }
                catch
                {
                    // Игнорируем ошибки парсинга URL
                }
            }

            // Имя по умолчанию
            return $"download_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        // ── JSON утилиты ──────────────────────────────────────────────────

        /// <summary>
        /// Проверяет, является ли строка валидным JSON.
        /// </summary>
        /// <param name="json">Строка для проверки.</param>
        /// <returns>true если строка валидный JSON.</returns>
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            json = json.Trim();

            if ((!json.StartsWith("{") || !json.EndsWith("}")) &&
                (!json.StartsWith("[") || !json.EndsWith("]")))
                return false;

            try
            {
                JsonConvert.DeserializeObject(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Форматирует JSON-строку с отступами.
        /// </summary>
        /// <param name="json">JSON-строка.</param>
        /// <returns>Форматированный JSON или исходная строка при ошибке.</returns>
        public static string FormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json;

            try
            {
                var obj = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }

        /// <summary>
        /// Минифицирует JSON-строку (удаляет пробелы и переносы).
        /// </summary>
        /// <param name="json">JSON-строка.</param>
        /// <returns>Минифицированный JSON или исходная строка при ошибке.</returns>
        public static string MinifyJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json;

            try
            {
                var obj = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(obj, Formatting.None);
            }
            catch
            {
                return json;
            }
        }
    }
}
