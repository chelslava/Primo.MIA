// =============================================================================
// HttpLogic.cs — тестируемая логика для HTTP активности.
//
// Содержит статические методы для обработки HTTP-запросов, которые можно
// покрыть unit-тестами без зависимости от SDK инфраструктуры.
// =============================================================================

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

namespace Primo.MIA
{
    /// <summary>
    /// Логика обработки HTTP-запросов.
    /// Содержит методы для парсинга заголовков, валидации параметров и сериализации ответов.
    /// </summary>
    public static class HttpLogic
    {
        /// <summary>
        /// Парсит JSON-строку заголовков в словарь.
        /// Если парсинг не удался или строка пустая — возвращает пустой словарь.
        /// </summary>
        /// <param name="headersJson">JSON-строка с заголовками. Пример: {"Content-Type": "application/json"}</param>
        /// <returns>Словарь заголовков или пустой словарь при ошибке</returns>
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
        /// Сериализует заголовки HTTP-ответа в JSON-строку.
        /// Объединяет заголовки из HttpResponseHeaders и HttpContentHeaders.
        /// </summary>
        /// <param name="responseHeaders">Заголовки ответа</param>
        /// <param name="contentHeaders">Заголовки содержимого</param>
        /// <returns>JSON-строка с заголовками</returns>
        public static string SerializeResponseHeaders(
            HttpResponseHeaders responseHeaders,
            HttpContentHeaders contentHeaders)
        {
            var allHeaders = new Dictionary<string, string>();

            // Собираем заголовки ответа
            if (responseHeaders != null)
            {
                responseHeaders
                    .Where(h => h.Value != null)
                    .ToList()
                    .ForEach(h => allHeaders[h.Key] = string.Join(", ", h.Value));
            }

            // Собираем заголовки содержимого
            if (contentHeaders != null)
            {
                contentHeaders
                    .Where(h => h.Value != null)
                    .ToList()
                    .ForEach(h => allHeaders[h.Key] = string.Join(", ", h.Value));
            }

            return JsonConvert.SerializeObject(allHeaders, Formatting.None);
        }

        /// <summary>
        /// Валидирует URL на корректность формата.
        /// Проверяет что строка не пустая и является валидным URI с http/https схемой.
        /// </summary>
        /// <param name="url">URL для проверки</param>
        /// <returns>true если URL валиден, иначе false</returns>
        public static bool ValidateUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
                return false;

            return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
        }

        /// <summary>
        /// Парсит строку таймаута в целое число секунд.
        /// Если парсинг не удался или значение отрицательное — возвращает значение по умолчанию.
        /// </summary>
        /// <param name="timeoutStr">Строка с таймаутом</param>
        /// <param name="defaultValue">Значение по умолчанию (секунды)</param>
        /// <returns>Таймаут в секундах</returns>
        public static int ParseTimeout(string timeoutStr, int defaultValue = 100)
        {
            if (string.IsNullOrWhiteSpace(timeoutStr))
                return defaultValue;

            if (!int.TryParse(timeoutStr, out int timeout))
                return defaultValue;

            return timeout > 0 ? timeout : defaultValue;
        }

        /// <summary>
        /// Проверяет что путь к файлу сертификата существует и имеет расширение .pfx.
        /// </summary>
        /// <param name="certPath">Путь к файлу сертификата</param>
        /// <returns>true если файл существует и имеет расширение .pfx</returns>
        public static bool ValidateCertificatePath(string certPath)
        {
            if (string.IsNullOrWhiteSpace(certPath))
                return false;

            if (!System.IO.File.Exists(certPath))
                return false;

            return certPath.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Нормализует заголовки, удаляя пустые ключи и значения.
        /// </summary>
        /// <param name="headers">Исходный словарь заголовков</param>
        /// <returns>Отфильтрованный словарь заголовков</returns>
        public static Dictionary<string, string> NormalizeHeaders(Dictionary<string, string> headers)
        {
            if (headers == null)
                return new Dictionary<string, string>();

            return headers
                .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value != null)
                .ToDictionary(kvp => kvp.Key.Trim(), kvp => kvp.Value);
        }
    }
}
