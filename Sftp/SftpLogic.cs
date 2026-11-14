// =============================================================================
// SftpLogic.cs — чистая логика SFTP-модуля (настройки, проверка host key,
// нормализация путей, маппинг листинга) + фабрика SftpClient.
// =============================================================================

using Primo.MIA.Common;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Настройки подключения к SFTP-серверу.
    /// </summary>
    public class SftpConnectionSettings
    {
        public string Host { get; set; }
        public int Port { get; set; } = 22;
        public string UserName { get; set; }
        public string Password { get; set; }
        public string PrivateKeyPath { get; set; }
        public string KeyPassphrase { get; set; }
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        public int OperationTimeoutSeconds { get; set; } = 30;

        /// <summary>Ожидаемый отпечаток host key ("SHA256:...", как в ssh-keygen -lf).</summary>
        public string ExpectedFingerprint { get; set; }

        /// <summary>Отключить проверку host key (небезопасно).</summary>
        public bool AllowAnyHostKey { get; set; }
    }

    /// <summary>
    /// Запись листинга удалённой директории.
    /// </summary>
    public class SftpEntry
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public long SizeBytes { get; set; }
        public DateTime ModifiedAt { get; set; }

        /// <summary>"Каталог" или "Файл" (для DataTable).</summary>
        public string TypeDisplay => IsDirectory ? "Каталог" : "Файл";
    }

    /// <summary>
    /// Логика SFTP-модуля: фабрика клиента, проверка host key, нормализация путей.
    /// Чистые функции покрыты unit-тестами.
    /// </summary>
    public class SftpLogic
    {
        /// <summary>
        /// Создаёт не подключённый SftpClient по настройкам.
        /// Валидирует обязательные поля и аутентификационные данные.
        /// </summary>
        public SftpClient CreateClient(SftpConnectionSettings settings)
        {
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNullOrWhiteSpace(settings.Host, nameof(settings.Host));
            Guard.NotNullOrWhiteSpace(settings.UserName, nameof(settings.UserName));

            var hasPassword = !string.IsNullOrEmpty(settings.Password);
            var hasKey = !string.IsNullOrWhiteSpace(settings.PrivateKeyPath);

            if (!hasPassword && !hasKey)
                Guard.That(hasPassword || hasKey,
                    "Не заданы данные аутентификации: укажите пароль или путь к приватному ключу",
                    nameof(settings));

            ConnectionInfo connectionInfo;
            if (hasKey)
            {
                if (!File.Exists(settings.PrivateKeyPath))
                    throw new FileNotFoundException($"Файл приватного ключа не найден: {settings.PrivateKeyPath}");

                var keyFile = string.IsNullOrEmpty(settings.KeyPassphrase)
                    ? new PrivateKeyFile(settings.PrivateKeyPath)
                    : new PrivateKeyFile(settings.PrivateKeyPath, settings.KeyPassphrase);
                connectionInfo = new ConnectionInfo(
                    settings.Host, settings.Port, settings.UserName,
                    new PrivateKeyAuthenticationMethod(settings.UserName, keyFile));
            }
            else
            {
                connectionInfo = new ConnectionInfo(
                    settings.Host, settings.Port, settings.UserName,
                    new PasswordAuthenticationMethod(settings.UserName, settings.Password));
            }

            var client = new SftpClient(connectionInfo)
            {
                OperationTimeout = TimeSpan.FromSeconds(settings.OperationTimeoutSeconds)
            };
            connectionInfo.Timeout = TimeSpan.FromSeconds(settings.ConnectionTimeoutSeconds);

            ApplyHostKeyPolicy(client, settings);
            return client;
        }

        /// <summary>
        /// Рекурсивно удаляет каталог с содержимым (SSH.NET 2024 не имеет
        /// перегрузки DeleteDirectory(path, recursive)).
        /// </summary>
        public void DeleteDirectoryRecursive(SftpClient client, string path)
        {
            Guard.NotNull(client, nameof(client));
            Guard.NotNullOrWhiteSpace(path, nameof(path));

            foreach (var file in client.ListDirectory(path))
            {
                if (file.Name == "." || file.Name == "..")
                    continue;

                if (file.IsDirectory)
                    DeleteDirectoryRecursive(client, file.FullName);
                else
                    client.DeleteFile(file.FullName);
            }

            client.DeleteDirectory(path);
        }

        /// <summary>
        /// Настраивает проверку host key клиента по настройкам.
        /// </summary>
        public void ApplyHostKeyPolicy(SftpClient client, SftpConnectionSettings settings)
        {
            Guard.NotNull(settings, nameof(settings));

            if (settings.AllowAnyHostKey)
            {
                client.HostKeyReceived += (sender, e) => { e.CanTrust = true; };
                return;
            }

            var expected = NormalizeFingerprint(settings.ExpectedFingerprint);
            if (string.IsNullOrEmpty(expected))
            {
                // issue #11: fail-closed. Без подписчика HostKeyReceived SSH.NET принимает
                // ЛЮБОЙ host key (CanTrust по умолчанию true) — пустой отпечаток не должен
                // молча включать такой режим.
                throw new SftpException(
                    "Отпечаток host key не задан (ExpectedFingerprint пуст) — соединение запрещено. " +
                    "Укажите отпечаток сервера (ssh-keygen -lf /etc/ssh/ssh_host_*_key.pub) " +
                    "или явно разрешите любой ключ (AllowAnyHostKey) для тестовых сред.");
            }

            client.HostKeyReceived += (sender, e) =>
            {
                var actual = NormalizeFingerprint(e.FingerPrintSHA256);
                e.CanTrust = string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
            };
        }

        /// <summary>
        /// Нормализует отпечаток к виду "sha256:base64-без-паддинга-в-нижнем-регистре".
        /// Принимает "SHA256:abc..." и "abc..." (без префикса и/или с "=" в конце).
        /// </summary>
        public string NormalizeFingerprint(string fingerprint)
        {
            if (string.IsNullOrWhiteSpace(fingerprint))
                return null;

            var value = fingerprint.Trim();
            var colonIndex = value.IndexOf(':');
            if (value.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) || colonIndex > 0)
                value = value.Substring(colonIndex + 1);

            value = value.TrimEnd('=').ToLowerInvariant();
            return "sha256:" + value;
        }

        /// <summary>
        /// Сверяет отпечатки host key (используется в тестах и при ручной проверке).
        /// </summary>
        public bool HostKeyMatches(string expected, string actualSha256)
        {
            var expectedNorm = NormalizeFingerprint(expected);
            var actualNorm = NormalizeFingerprint(actualSha256);
            return !string.IsNullOrEmpty(expectedNorm)
                && !string.IsNullOrEmpty(actualNorm)
                && string.Equals(expectedNorm, actualNorm, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Нормализует удалённый путь: заменяет обратные слэши, схлопывает
        /// двойные слэши, разворачивает "~" в путь домашней директории.
        /// </summary>
        public string NormalizeRemotePath(string path, string homeDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                return homeDirectory ?? "/";

            var normalized = path.Trim().Replace('\\', '/');
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, "/{2,}", "/");

            if (normalized == "~" || normalized.StartsWith("~/"))
            {
                var home = (homeDirectory ?? "").TrimEnd('/');
                var rest = normalized == "~" ? string.Empty : normalized.Substring(1);
                normalized = (home + rest);
                if (!normalized.StartsWith("/"))
                    normalized = "/" + normalized;
            }

            if (!normalized.StartsWith("/"))
                normalized = "/" + normalized;

            return normalized;
        }

        /// <summary>
        /// Маппинг ISftpFile → SftpEntry.
        /// </summary>
        public SftpEntry MapEntry(ISftpFile file)
        {
            if (file == null)
                return null;

            return new SftpEntry
            {
                Name = file.Name,
                FullPath = file.FullName,
                IsDirectory = file.IsDirectory,
                SizeBytes = file.Length,
                ModifiedAt = file.LastWriteTime
            };
        }

        /// <summary>
        /// Маппинг листинга SFTP-сервера в список SftpEntry.
        /// </summary>
        public List<SftpEntry> MapListing(IEnumerable<ISftpFile> files)
        {
            if (files == null)
                throw new ArgumentNullException(nameof(files));

            var result = new List<SftpEntry>();
            foreach (var file in files)
            {
                var entry = MapEntry(file);
                if (entry != null)
                    result.Add(entry);
            }
            return result;
        }

        /// <summary>
        /// Фильтрует и сортирует записи листинга: каталоги первыми, затем по имени
        /// (без учёта регистра); при нужно — только имена.
        /// </summary>
        public List<SftpEntry> OrderEntries(IEnumerable<SftpEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            return entries
                .Where(e => e != null && e.Name != "." && e.Name != "..")
                .OrderBy(e => e.IsDirectory ? 0 : 1)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
