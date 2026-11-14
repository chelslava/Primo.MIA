// =============================================================================
// WebhookServerCore.cs — общая чистая логика webhook-активностей (issue #23).
//
// WebhookBack (отправка) и WebhookListenBack (приём) используют одни и те же
// операции: payload-шаблоны, retry-политика, нормализация префиксов,
// разбор методов/заголовков/query, проверка секретного токена. Раньше логика
// была размазана по двум *Back.cs — правки не попадали в обе стороны.
// Только чистые функции: сеть (HttpClient/HttpListener) остаётся в активностях,
// unit-тесты работают без портов и сети.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    /// <summary>Общая чистая логика webhook-активностей (отправка и приём).</summary>
    public static class WebhookServerCore
    {
        /// <summary>Заголовки, в которых ищется секретный токен (по порядку).</summary>
        private static readonly string[] SecretTokenHeaders =
        {
            "X-Webhook-Secret", "X-Hub-Signature-256", "Authorization"
        };

        // ── Отправка: payload ─────────────────────────────────────────────

        /// <summary>
        /// Выбирает payload для отправки. Приоритет: Template &gt; Payload;
        /// оба пустые — null (нечего отправлять).
        /// </summary>
        public static string ResolvePayload(string template, string payload)
        {
            if (!string.IsNullOrWhiteSpace(template))
                return template;
            return string.IsNullOrWhiteSpace(payload) ? null : payload;
        }

        /// <summary>
        /// Подставляет переменные в шаблон: «{{ключ}}» → значение
        /// (без учёта регистра ключа, пробелы внутри скобок игнорируются).
        /// Не найденный плейсхолдер остаётся в тексте как есть.
        /// </summary>
        public static string ApplyTemplate(string template, Dictionary<string, string> vars)
        {
            if (string.IsNullOrEmpty(template))
                return template;
            if (vars == null || vars.Count == 0)
                return template;

            return Regex.Replace(template, @"\{\{([^}]+)\}\}", match =>
            {
                var key = match.Groups[1].Value.Trim();
                return vars
                    .Where(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    .Select(kv => kv.Value)
                    .FirstOrDefault() ?? match.Value;
            });
        }

        // ── Отправка: retry-политика ──────────────────────────────────────

        /// <summary>2xx — успешный ответ.</summary>
        public static bool IsSuccessStatusCode(int statusCode)
        {
            return statusCode >= 200 && statusCode < 300;
        }

        /// <summary>
        /// Повторять ли попытку: да — при сетевом исключении или 5xx,
        /// пока попытки остались. Успех и 4xx повторов не требуют.
        /// </summary>
        /// <param name="statusCode">0 — сетевая ошибка (ответа нет).</param>
        /// <param name="attempt">Номер текущей попытки (с 1).</param>
        /// <param name="maxAttempts">Общее число попыток.</param>
        public static bool ShouldRetry(int statusCode, bool hadException, int attempt, int maxAttempts)
        {
            if (attempt >= maxAttempts)
                return false;
            if (hadException)
                return true;
            if (statusCode == 0)
                return true;
            return statusCode >= 500;
        }

        /// <summary>Сообщение результата отправки.</summary>
        public static string BuildResultMessage(int statusCode, bool success, int attempts)
        {
            var attemptsSuffix = attempts > 1 ? $", попыток: {attempts}" : string.Empty;
            return success
                ? $"Вебхук отправлен → {statusCode}{attemptsSuffix}"
                : $"Вебхук вернул {statusCode}{attemptsSuffix}";
        }

        /// <summary>Ошибки валидации входных данных отправки (пустой список — всё валидно).</summary>
        public static List<string> ValidateSendInput(string url, string payload, string template)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(url))
                errors.Add("URL вебхука обязателен");
            if (string.IsNullOrWhiteSpace(payload) && string.IsNullOrWhiteSpace(template))
                errors.Add("Укажите Payload или Template");
            return errors;
        }

        // ── Приём: listener ───────────────────────────────────────────────

        /// <summary>Нормализует URL-префикс HttpListener: завершающий слеш обязателен.</summary>
        public static string NormalizePrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return prefix;
            return prefix.EndsWith("/", StringComparison.Ordinal)
                ? prefix
                : prefix + "/";
        }

        /// <summary>
        /// Разбирает строку разрешённых методов («GET, POST; DELETE»)
        /// в регистронезависимое множество. Пустая строка — пустое множество
        /// (все методы разрешены).
        /// </summary>
        public static HashSet<string> ParseAllowedMethods(string raw)
        {
            return new HashSet<string>(
                (raw ?? string.Empty)
                    .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim().ToUpperInvariant())
                    .Where(m => !string.IsNullOrEmpty(m)),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Код ответа: 0/отрицательный — 200 по умолчанию.</summary>
        public static int ResolveResponseCode(int requested)
        {
            return requested > 0 ? requested : 200;
        }

        /// <summary>
        /// Проверяет секретный токен: ищется в X-Webhook-Secret,
        /// X-Hub-Signature-256, Authorization (по порядку). Пустой ожидаемый
        /// токен — проверка отключена (всегда валидно).
        /// </summary>
        public static bool IsTokenValid(Dictionary<string, string> headers, string secretToken)
        {
            if (string.IsNullOrWhiteSpace(secretToken))
                return true;
            if (headers == null)
                return false;

            foreach (var headerName in SecretTokenHeaders)
            {
                if (headers.TryGetValue(headerName, out var value))
                {
                    return string.Equals(
                        value?.TrimStart(),
                        secretToken,
                        StringComparison.Ordinal);
                }
            }

            return false;
        }

        // ── Приём: разбор запроса ─────────────────────────────────────────

        /// <summary>NameValueCollection → Dictionary (null-ключи отбрасываются, регистр ключей игнорируется).</summary>
        public static Dictionary<string, string> ParseNameValues(NameValueCollection source)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (source == null)
                return result;

            foreach (var key in source.AllKeys)
            {
                if (key == null)
                    continue; // ключи без имени (multipart) не попадают в вывод
                result[key] = source[key] ?? string.Empty;
            }
            return result;
        }

        /// <summary>Сообщение результата приёма запроса.</summary>
        public static string BuildReceiveMessage(string method, string remoteEndPoint, int bodyLength, bool isTokenValid)
        {
            return $"Получен {method} от {remoteEndPoint}, тело: {bodyLength} байт" +
                   (isTokenValid ? string.Empty : " ⚠ токен не совпал");
        }
    }
}
