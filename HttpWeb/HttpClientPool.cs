// =============================================================================
// HttpClientPool.cs — пул HttpClientHandler для HTTP-активностей.
//
// Проблема: создание HttpClient/HttpClientHandler на каждый запрос —
// документированный антипаттерн .NET: исчерпание сокетов (TIME_WAIT)
// и потеря пула соединений при dispose клиента.
//
// Решение: HttpClientHandler кэшируется по конфигурации (сертификат,
// игнорирование SSL), HttpClient создаётся на каждый запрос с
// disposeHandler: false — dispose клиента НЕ закрывает пул соединений.
//
// Ротация: handler живёт HandlerLifetime (10 минут), чтобы long-running
// робот подхватил смену DNS и обновлённый файл сертификата.
// Отработанные handler'ы не disposируются сразу (in-flight запросы),
// а переносятся в "ретирмент" и закрываются после GracePeriod.
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Primo.MIA
{
    /// <summary>
    /// Статический пул HttpClientHandler, разделяемый HTTP-активностями.
    /// Потокобезопасен.
    /// </summary>
    public static class HttpClientPool
    {
        /// <summary>Время жизни handler'а в пуле до ротации (DNS/сертификаты).</summary>
        private static readonly TimeSpan HandlerLifetime = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Отсрочка dispose отработанного handler'а: ждём завершения in-flight
        /// запросов (максимальный таймаут активностей — 300 с, берём с запасом).
        /// </summary>
        private static readonly TimeSpan RetireGracePeriod = TimeSpan.FromMinutes(30);

        private static readonly object SyncRoot = new object();
        private static readonly ConcurrentDictionary<string, PoolEntry> Handlers =
            new ConcurrentDictionary<string, PoolEntry>(StringComparer.Ordinal);

        // (handler, время вывода из пула) — dispose через GracePeriod
        private static readonly ConcurrentQueue<PoolEntry> Retired =
            new ConcurrentQueue<PoolEntry>();

        private sealed class PoolEntry
        {
            public HttpClientHandler Handler;
            public List<X509Certificate2> Certificates;
            public DateTime CreatedUtc;

            /// <summary>Момент вывода из пула (dispose после GracePeriod).</summary>
            public DateTime RetireAtUtc = DateTime.MaxValue;
        }

        /// <summary>
        /// Создаёт HttpClient с handler'ом из пула.
        /// Клиент можно (и нужно) диспозировать после запроса — пул соединений
        /// в handler'е сохранится и будет переиспользован следующими активностями.
        /// </summary>
        /// <param name="useCertificate">Подключать клиентский сертификат.</param>
        /// <param name="certPath">Путь к PFX/PKCS#12 файлу сертификата.</param>
        /// <param name="certPassword">Пароль файла сертификата.</param>
        /// <param name="ignoreSslErrors">Игнорировать ошибки TLS-сертификата сервера (тестовые среды).</param>
        /// <param name="timeout">Таймаут запроса.</param>
        /// <returns>Готовый к использованию HttpClient.</returns>
        public static HttpClient CreateClient(
            bool useCertificate,
            string certPath,
            string certPassword,
            bool ignoreSslErrors,
            TimeSpan timeout)
        {
            // Выносим отработанные handler'ы, у которых истёк grace-период
            PurgeRetired();

            var key = BuildKey(useCertificate, certPath, certPassword, ignoreSslErrors);

            if (!Handlers.TryGetValue(key, out var entry) || IsStale(entry))
            {
                lock (SyncRoot)
                {
                    if (!Handlers.TryGetValue(key, out entry) || IsStale(entry))
                    {
                        if (entry != null)
                        {
                            // Не disposируем сразу — могут быть in-flight запросы
                            Handlers.TryRemove(key, out _);
                            entry.RetireAtUtc = DateTime.UtcNow;
                            Retired.Enqueue(entry);
                        }
                        entry = CreateEntry(useCertificate, certPath, certPassword, ignoreSslErrors);
                        Handlers[key] = entry;
                    }
                }
            }

            return new HttpClient(entry.Handler, disposeHandler: false)
            {
                Timeout = timeout
            };
        }

        /// <summary>
        /// Создаёт HttpClient из пула с настройками по умолчанию
        /// (без сертификата, без игнорирования SSL-ошибок).
        /// </summary>
        public static HttpClient CreateClient(TimeSpan timeout)
        {
            return CreateClient(false, null, null, false, timeout);
        }

        private static PoolEntry CreateEntry(
            bool useCertificate, string certPath, string certPassword, bool ignoreSslErrors)
        {
            var handler = new HttpClientHandler();
            var certificates = new List<X509Certificate2>();

            if (useCertificate)
            {
                if (string.IsNullOrWhiteSpace(certPath))
                    throw new ArgumentException("Путь к сертификату не может быть пустым", nameof(certPath));

                if (!File.Exists(certPath))
                    throw new FileNotFoundException($"Файл сертификата не найден: {certPath}");

                var certificate = new X509Certificate2(certPath, certPassword ?? string.Empty);
                handler.ClientCertificates.Add(certificate);
                certificates.Add(certificate);
            }

            if (ignoreSslErrors)
            {
                handler.ServerCertificateCustomValidationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;
            }

            return new PoolEntry
            {
                Handler = handler,
                Certificates = certificates,
                CreatedUtc = DateTime.UtcNow
            };
        }

        private static bool IsStale(PoolEntry entry)
        {
            return DateTime.UtcNow - entry.CreatedUtc > HandlerLifetime;
        }

        /// <summary>
        /// Dispose отработанных handler'ов, у которых истёк grace-период.
        /// Вызывается при каждом CreateClient — без таймерных потоков.
        /// </summary>
        private static void PurgeRetired()
        {
            var now = DateTime.UtcNow;
            while (Retired.TryPeek(out var oldest) && now - oldest.RetireAtUtc > RetireGracePeriod)
            {
                if (!Retired.TryDequeue(out var entry))
                    break;
                DisposeEntry(entry);
            }
        }

        private static void DisposeEntry(PoolEntry entry)
        {
            try
            {
                entry.Handler?.Dispose();
            }
            catch
            {
                // handler может быть уже disposирован параллельной веткой — не критично
            }

            if (entry.Certificates != null)
            {
                foreach (var certificate in entry.Certificates)
                {
                    try
                    {
                        certificate.Dispose();
                    }
                    catch
                    {
                        // сертификат уже освобождён — не критично
                    }
                }
            }
        }

        private static string BuildKey(
            bool useCertificate, string certPath, string certPassword, bool ignoreSslErrors)
        {
            return $"{useCertificate}|{certPath ?? string.Empty}|{certPassword ?? string.Empty}|{ignoreSslErrors}";
        }
    }
}
