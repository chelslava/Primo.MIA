// =============================================================================
// WaitForHttpLogic.cs — чистая логика poll-ожидания HTTP (issue #32).
//
// WaitForHttp: опрашивать endpoint до выполнения условия (ожидаемый
// статус и/или тело содержит подстроку) с общим таймаутом и интервалом.
// Условие и цикл — чистые функции с инъекцией fetch-делегата и задержки:
// unit-тесты без сети и без Thread.Sleep.
// =============================================================================

using Primo.MIA.Common;
using System;
using System.Diagnostics;
using System.Threading;

namespace Primo.MIA
{
    /// <summary>Результат poll-ожидания.</summary>
    public class WaitForHttpResult
    {
        /// <summary>Условие выполнено (успех) — true; таймаут — false.</summary>
        public bool ConditionMet { get; set; }

        /// <summary>Последний HTTP-статус (null — ни одного ответа).</summary>
        public int? LastStatus { get; set; }

        /// <summary>Последнее тело ответа (может быть null при сетевой ошибке).</summary>
        public string LastBody { get; set; }

        /// <summary>Число выполненных попыток.</summary>
        public int Attempts { get; set; }

        /// <summary>Фактически затраченное время.</summary>
        public TimeSpan Elapsed { get; set; }

        /// <summary>Последняя сетевая ошибка (если была).</summary>
        public string LastError { get; set; }
    }

    /// <summary>
    /// Логика poll-ожидания HTTP-endpoint'а до выполнения условия.
    /// </summary>
    public class WaitForHttpLogic
    {
        public const int DefaultPollIntervalMs = 1000;
        public const int DefaultTimeoutSeconds = 60;
        public const int MaxTimeoutSeconds = 3600;
        public const int MinPollIntervalMs = 100;

        /// <summary>
        /// Проверяет условие остановки: статус совпал И (если задано)
        /// тело содержит подстроку.
        /// </summary>
        /// <param name="status">Последний статус (null — ответа нет).</param>
        /// <param name="expectedStatus">Ожидаемый статус; null/0 — любой 2xx.</param>
        /// <param name="body">Последнее тело (может быть null).</param>
        /// <param name="bodyContains">Подстрока, обязательная в теле; null/пусто — не проверяется.</param>
        public bool IsConditionMet(int? status, int? expectedStatus, string body, string bodyContains)
        {
            if (status == null)
                return false;

            bool statusOk;
            if (expectedStatus.HasValue && expectedStatus.Value > 0)
                statusOk = status.Value == expectedStatus.Value;
            else
                statusOk = status.Value >= 200 && status.Value < 300;

            if (!statusOk)
                return false;

            if (!string.IsNullOrEmpty(bodyContains))
                return body != null && body.Contains(bodyContains);

            return true;
        }

        /// <summary>
        /// Ограничивает интервал опроса снизу (защита от «молотилки» по серверу).
        /// </summary>
        public int NormalizeInterval(int pollIntervalMs)
        {
            return pollIntervalMs < MinPollIntervalMs ? MinPollIntervalMs : pollIntervalMs;
        }

        /// <summary>
        /// Ограничивает общий таймаут сверху (час).
        /// </summary>
        public int NormalizeTimeout(int timeoutSeconds)
        {
            if (timeoutSeconds <= 0)
                return DefaultTimeoutSeconds;
            return timeoutSeconds > MaxTimeoutSeconds ? MaxTimeoutSeconds : timeoutSeconds;
        }

        /// <summary>
        /// Цикл опроса до выполнения условия или таймаута.
        /// </summary>
        /// <param name="fetch">Сетевой делегат: (статус, тело); исключение = сетевая ошибка (попытка продолжается).</param>
        /// <param name="expectedStatus">Ожидаемый статус; null/0 — любой 2xx.</param>
        /// <param name="bodyContains">Обязательная подстрока тела; null/пусто — нет.</param>
        /// <param name="totalTimeout">Общий таймаут ожидания.</param>
        /// <param name="pollInterval">Интервал между попытками.</param>
        /// <param name="delay">Реализация задержки (Thread.Sleep в активности, 0 в тестах).</param>
        public WaitForHttpResult PollUntil(
            Func<(int? Status, string Body)> fetch,
            int? expectedStatus,
            string bodyContains,
            TimeSpan totalTimeout,
            TimeSpan pollInterval,
            Action<TimeSpan> delay)
        {
            Guard.NotNull(fetch, nameof(fetch));
            Guard.NotNull(delay, nameof(delay));

            var stopwatch = Stopwatch.StartNew();
            var result = new WaitForHttpResult();

            // Первая попытка — сразу; последующие — после интервала
            while (true)
            {
                result.Attempts++;

                try
                {
                    var (status, body) = fetch();
                    result.LastStatus = status;
                    result.LastBody = body;
                    result.LastError = null;

                    if (IsConditionMet(status, expectedStatus, body, bodyContains))
                    {
                        result.ConditionMet = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // Сетевые ошибки не прерывают ожидание: сервер может подняться
                    result.LastError = ex.Message;
                    result.LastBody = null;
                    result.LastStatus = null;
                }

                var remaining = totalTimeout - stopwatch.Elapsed;
                if (remaining <= TimeSpan.Zero)
                    break;

                var wait = pollInterval < remaining ? pollInterval : remaining;
                delay(wait);
            }

            result.Elapsed = stopwatch.Elapsed;
            return result;
        }
    }
}
