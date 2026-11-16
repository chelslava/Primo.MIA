// =============================================================================
// RetryPolicy.cs — универсальный механизм повторов для сетевых операций
// (issue #43: до него retry был реализован трижды — HttpHelper, WebhookBack,
// BrowserElementHelper — а Database/SFTP/Mail повторов не имели вовсе).
//
// Контракт:
//   • attempt(1..maxAttempts) выполняется минимум один раз;
//   • исключение: если shouldRetryException вернула false — исключение
//     пробрасывается наружу немедленно (fail-fast, как в Browser);
//     если true и попытки остались — пауза и повтор;
//   • результат: если shouldRetryResult вернула true и попытки остались —
//     пауза и повтор (например, HTTP 5xx — «успешный» ответ, но повторяем);
//   • попытки исчерпаны — возвращается Outcome с последним исключением
//     или последним результатом (решение — за вызывающим кодом);
//   • пауза: линейная или экспоненциальная (delayMs × 2^(n−1), с потолком).
// =============================================================================

using System;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Итог выполнения <see cref="RetryPolicy.Execute{T}"/>: число попыток,
    /// последний результат (если получен) и/или последнее исключение.
    /// </summary>
    public sealed class RetryOutcome<T>
    {
        /// <summary>Фактическое число выполненных попыток (от 1).</summary>
        public int Attempts { get; }

        /// <summary>Последний результат попытки, если хоть одна завершилась без исключения.</summary>
        public T LastResult { get; }

        /// <summary>Был ли получен хотя бы один результат (без исключения).</summary>
        public bool HasResult { get; }

        /// <summary>Последнее исключение, если все попытки завершились ошибкой.</summary>
        public Exception LastException { get; }

        internal RetryOutcome(int attempts, bool hasResult, T lastResult, Exception lastException)
        {
            Attempts = attempts;
            HasResult = hasResult;
            LastResult = lastResult;
            LastException = lastException;
        }
    }

    /// <summary>
    /// Универсальный механизм повторов (issue #43). Единая точка retry-логики
    /// для webhook, browser и будущих network-активностей (DB/SFTP/Mail).
    /// </summary>
    public static class RetryPolicy
    {
        /// <summary>
        /// Выполняет <paramref name="attempt"/> до <paramref name="maxAttempts"/> раз
        /// с паузами и предикатами повтора.
        /// </summary>
        /// <param name="attempt">Попытка: принимает номер попытки (с 1), возвращает результат.</param>
        /// <param name="maxAttempts">Максимальное число попыток (минимум 1).</param>
        /// <param name="shouldRetryException">Повторять ли исключение (ex, номер попытки).
        /// null — повторять любые. false — исключение пробрасывается немедленно.</param>
        /// <param name="shouldRetryResult">Повторять ли «успешный» результат (результат, номер попытки),
        /// например HTTP 5xx. null — результат считается финальным.</param>
        /// <param name="delayMs">Базовая пауза между попытками, мс (0 — без паузы, удобно в тестах).</param>
        /// <param name="exponentialBackoff">Экспоненциальный рост паузы: delayMs × 2^(n−1).</param>
        /// <param name="maxDelayMs">Потолок паузы при экспоненциальном росте.</param>
        /// <param name="failureCallback">Лог неудачной попытки (исключение, номер попытки).</param>
        /// <param name="delayCallback">Лог паузы (номер попытки, задержка мс).</param>
        /// <returns>Итог: число попыток и последний результат/исключение.</returns>
        public static RetryOutcome<T> Execute<T>(
            Func<int, T> attempt,
            int maxAttempts,
            Func<Exception, int, bool> shouldRetryException = null,
            Func<T, int, bool> shouldRetryResult = null,
            int delayMs = 1000,
            bool exponentialBackoff = false,
            int maxDelayMs = 30000,
            Action<Exception, int> failureCallback = null,
            Action<int, int> delayCallback = null)
        {
            if (attempt == null)
                throw new ArgumentNullException(nameof(attempt));

            int totalAttempts = Math.Max(1, maxAttempts);
            Exception lastException = null;
            bool hasResult = false;
            T lastResult = default(T);

            for (int attemptNumber = 1; attemptNumber <= totalAttempts; attemptNumber++)
            {
                try
                {
                    T result = attempt(attemptNumber);

                    bool retryResult = attemptNumber < totalAttempts
                                       && shouldRetryResult != null
                                       && shouldRetryResult(result, attemptNumber);

                    if (!retryResult)
                        return new RetryOutcome<T>(attemptNumber, true, result, null);

                    // «Успешный», но повторяемый результат — запоминаем и продолжаем
                    hasResult = true;
                    lastResult = result;
                    WaitBetweenAttempts(attemptNumber, delayMs, exponentialBackoff, maxDelayMs, delayCallback);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    failureCallback?.Invoke(ex, attemptNumber);

                    bool attemptsRemain = attemptNumber < totalAttempts;
                    bool retryable = attemptsRemain
                                     && (shouldRetryException == null || shouldRetryException(ex, attemptNumber));

                    if (!retryable)
                    {
                        if (attemptsRemain)
                            throw; // неповторяемое исключение — fail-fast с оригинальным стеком
                        break;     // попытки исчерпаны — решение за вызывающим кодом
                    }

                    WaitBetweenAttempts(attemptNumber, delayMs, exponentialBackoff, maxDelayMs, delayCallback);
                }
            }

            // Попытки исчерпаны: решение (бросить/вернуть результат) — за вызывающим
            return new RetryOutcome<T>(totalAttempts, hasResult, lastResult, lastException);
        }

        private static void WaitBetweenAttempts(
            int attemptNumber, int delayMs, bool exponentialBackoff, int maxDelayMs,
            Action<int, int> delayCallback)
        {
            int delay = exponentialBackoff
                ? Math.Min(delayMs * (int)Math.Pow(2, attemptNumber - 1), maxDelayMs)
                : delayMs;

            if (delay <= 0)
                return;

            delayCallback?.Invoke(attemptNumber, delay);
            System.Threading.Thread.Sleep(delay);
        }
    }
}
