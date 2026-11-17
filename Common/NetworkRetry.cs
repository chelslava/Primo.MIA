// =============================================================================
// NetworkRetry.cs — общие retry-параметры сетевых активностей и
// классификация транзиентных (временных) ошибок сети (issue #43).
//
// Параметры повторов (Prop_RetryCount / Prop_RetryDelayMs, строки — как все
// свойства активностей) живут в базовых классах модулей SFTP/FTPS/MQ;
// разбор и классификация — здесь, чтобы покрывались юнит-тестами.
//
// Политика: повторять ТОЛЬКО сетевые сбои уровня транспорта (соединение,
// DNS, таймаут транспорта, сброс сокета). Детерминированные ошибки —
// аутентификация, отсутствие пути/очереди, отказ прав — НЕ повторяются:
// повтор не меняет исход и лишь затягивает падение.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Retry-параметры сетевых активностей и классификация транзиентных ошибок.
    /// </summary>
    public static class NetworkRetry
    {
        /// <summary>Попыток по умолчанию: 1 (повторов нет).</summary>
        public const int DefaultRetryCount = 0;

        /// <summary>Пауза между повторами по умолчанию, мс.</summary>
        public const int DefaultRetryDelayMs = 1000;

        /// <summary>Верхняя граница числа повторов (защита от «9999»).</summary>
        public const int MaxRetryCount = 20;

        /// <summary>Верхняя граница паузы между повторами, мс.</summary>
        public const int MaxRetryDelayMs = 60000;

        /// <summary>
        /// Разбирает строку «число повторов» (0 — без повторов).
        /// Пустое/нечисловое → значение по умолчанию; вне диапазона — клампится.
        /// </summary>
        public static int ParseRetryCount(string raw)
        {
            return Clamp(StringHelper.TryParseInt(raw) ?? DefaultRetryCount, 0, MaxRetryCount);
        }

        /// <summary>
        /// Разбирает строку «пауза между повторами, мс».
        /// Пустое/нечисловое → значение по умолчанию; вне диапазона — клампится.
        /// </summary>
        public static int ParseRetryDelayMs(string raw)
        {
            return Clamp(StringHelper.TryParseInt(raw) ?? DefaultRetryDelayMs, 0, MaxRetryDelayMs);
        }

        /// <summary>
        /// Транзиентная ли ошибка: стоит ли повторять операцию.
        /// Сетевые сбои транспорта — да; детерминированные
        /// (аутентификация, пути, права, отмена) — нет.
        /// </summary>
        public static bool IsTransient(Exception ex)
        {
            if (ex == null)
                return false;

            // Отмена и таймаут уровня активности не повторяем: отмену нельзя
            // «перез waiting'ть», а таймауты базовых классов и так ограничены sdkTimeOut
            if (ex is OperationCanceledException || ex is TimeoutException)
                return false;

            switch (ex)
            {
                // ── FTPS (FtpWebRequest) ────────────────────────────────
                case WebException web:
                    return IsTransientWebStatus(web.Status);

                // ── низкий уровень (SSH.NET поверх сокетов) ─────────────
                case SocketException:
                    return true;

                // ── SFTP (SSH.NET): соединение/транспорт — да ───────────
                case Renci.SshNet.Common.SshConnectionException:
                case Renci.SshNet.Common.SshOperationTimeoutException:
                    return true;

                // аутентификация/путь — детерминированные ошибки
                case Renci.SshNet.Common.SshAuthenticationException:
                case Renci.SshNet.Common.SftpPathNotFoundException:
                    return false;

                // ── RabbitMQ ─────────────────────────────────────────────
                case RabbitMQ.Client.Exceptions.BrokerUnreachableException:
                    return true;
                case RabbitMQ.Client.Exceptions.OperationInterruptedException:
                    // закрытие канала/соединения брокером — обычно причина
                    // детерминирована (нет очереди, нет доступа)
                    return false;

                // ── Kafka (Confluent) ────────────────────────────────────
                case Confluent.Kafka.KafkaException:
                    //produce-ошибки Confluent в массе своей ретраебельны
                    //(лидер недоступен, таймаут брокера); редкие детерминированные
                    // (MessageSizeTooLarge) имеют свой тип и не проходят сюда
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsTransientWebStatus(WebExceptionStatus status)
        {
            switch (status)
            {
                case WebExceptionStatus.ConnectFailure:
                case WebExceptionStatus.Timeout:
                case WebExceptionStatus.NameResolutionFailure:
                case WebExceptionStatus.KeepAliveFailure:
                case WebExceptionStatus.ConnectionClosed:
                case WebExceptionStatus.ReceiveFailure:
                case WebExceptionStatus.SendFailure:
                case WebExceptionStatus.PipelineFailure:
                    return true;
                default:
                    // ProtocolError (530 логин / 550 путь) и прочее — детерминированные
                    return false;
            }
        }

        // =====================================================================
        // База данных (issue #43): повторы ТОЛЬКО для read-операций —
        // SELECT/метаданные идемпотентны; повтор NonQuery/StoredProcedure при
        // потере ответа после коммита дал бы двойное исполнение.
        // =====================================================================

        /// <summary>
        /// Числа ошибок SQL Server, транзиентные для ретрая
        /// (типовой облачный список + дедлок 1205 + таймаут -2).
        /// </summary>
        private static readonly HashSet<int> TransientSqlNumbers = new HashSet<int>
        {
            4060,   // не удалось открыть базу (переконфигурация/failover)
            40143,  // база недоступна
            40197,  // служба занята, повторите
            40501,  // занята (throttling)
            40540,  // временная ошибка
            40613,  // база Azure недоступна
            40643,  // превышен порог
            40664,  // внутренняя ошибка, повторите
            4221,   // не удалось установить соединение
            45317,  // потеря соединения с репликой
            1205,   // дедлок (жертва)
            -2      // таймаут сервера
        };

        /// <summary>
        /// Транзиентная ли ошибка БД (для read-операций). Ошибки провайдеров:
        /// Npgsql (IsTransient), SQL Server (список кодов, вкл. дедлок).
        /// Прочее (синтаксис, права, отсутствие объекта) — детерминированное.
        /// </summary>
        public static bool IsTransientDb(Exception ex)
        {
            if (ex == null)
                return false;

            switch (ex)
            {
                case Npgsql.NpgsqlException npg:
                    return npg.IsTransient;

                case System.Data.SqlClient.SqlException sql:
                    return TransientSqlNumbers.Contains(sql.Number);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Выполняет read-операцию БД с опциональными повторами при транзиентных
        /// ошибках. Возвращает результат или пробрасывает последнее исключение.
        /// </summary>
        /// <param name="retryCountRaw">Строковое значение Prop_RetryCount.</param>
        /// <param name="retryDelayMsRaw">Строковое значение Prop_RetryDelayMs.</param>
        /// <param name="operation">Read-операция.</param>
        /// <param name="componentName">Имя активности для файлового лога.</param>
        public static T ExecuteDbRead<T>(
            string retryCountRaw, string retryDelayMsRaw, Func<T> operation, string componentName)
        {
            var outcome = RetryPolicy.Execute(
                _ => operation(),
                ParseRetryCount(retryCountRaw) + 1,
                shouldRetryException: (ex, attemptNumber) => IsTransientDb(ex),
                delayMs: ParseRetryDelayMs(retryDelayMsRaw),
                failureCallback: (ex, attemptNumber) => ActivityFileLogger.Warning(
                    componentName, $"Попытка {attemptNumber} не удалась: {ex.Message}"));

            return outcome.ResultOrThrow();
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
