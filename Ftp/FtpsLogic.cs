// =============================================================================
// FtpsLogic.cs — чистая логика FTPS-активностей (issue #31).
//
// FtpWebRequest (net481 в-box, нулевые зависимости), explicit TLS (EnableSsl).
//
// Сертификаты: только fail-closed — стандартная валидация ОС. Опции
// «принять всё» для FtpWebRequest не существует без глобального
// ServicePointManager.ServerCertificateValidationCallback (глобальное
// состояние — гонки между параллельными сессиями могут принять чужой
// сертификат). Самоподписанные хосты: публичный сертификат или другой
// клиент (issue #31, «Не в v1»).
//
// Отдельные операции (Upload/Download/...) — сетевые, тестируются
// только интеграционно; чистые части (URI, настройка запроса, парсинг
// листинга) покрыты unit-тестами.
// =============================================================================

using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Primo.MIA
{
    /// <summary>Настройки подключения к FTPS-серверу.</summary>
    public class FtpsConnectionSettings
    {
        /// <summary>Хост (без схемы).</summary>
        public string Host { get; set; }

        /// <summary>Порт (по умолчанию 21).</summary>
        public int Port { get; set; } = 21;

        /// <summary>Имя пользователя (пусто = анонимный ftp / anonymous).</summary>
        public string UserName { get; set; }

        /// <summary>Пароль.</summary>
        public string Password { get; set; }

        /// <summary>Таймаут подключения, сек (по умолчанию 30).</summary>
        public int ConnectionTimeoutSeconds { get; set; } = 30;

        /// <summary>Таймаут операции (чтение/запись потока), сек (по умолчанию 300).</summary>
        public int OperationTimeoutSeconds { get; set; } = 300;
    }

    /// <summary>
    /// Логика FTPS: построение URI, настройка запроса, парсинг листинга
    /// и выполнение операций через FtpWebRequest.
    /// </summary>
    public class FtpsLogic
    {
        // ── Чистые части (unit-тестируемые) ────────────────────────────

        /// <summary>
        /// Строит ftp:// URI: нормализует слэши, экранирует пробелы,
        /// добавляет порт при нестандартном.
        /// </summary>
        public string BuildRequestUri(FtpsConnectionSettings settings, string remotePath)
        {
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNullOrWhiteSpace(settings.Host, nameof(settings.Host));

            if (string.IsNullOrWhiteSpace(remotePath))
                throw new ArgumentException("Удалённый путь не может быть пустым", nameof(remotePath));

            var path = remotePath.Trim().Replace('\\', '/').TrimStart('/');
            if (!path.StartsWith("/"))
                path = "/" + path;

            var encoded = Uri.EscapeDataString(path).Replace("%2F", "/");
            var portPart = settings.Port == 21 ? string.Empty : $":{settings.Port}";
            return $"ftp://{settings.Host.Trim()}{portPart}{encoded}";
        }

        /// <summary>
        /// Создаёт настроенный FtpWebRequest: explicit TLS (EnableSsl),
        /// пассивный режим, binary, учётные данные, таймауты.
        /// </summary>
        public FtpWebRequest CreateRequest(
            FtpsConnectionSettings settings,
            string remotePath,
            string method,
            string requestBodyContent = null)
        {
            var uri = BuildRequestUri(settings, remotePath);
            var request = (FtpWebRequest)WebRequest.Create(uri);

            request.Method = method;
            request.UseBinary = true;
            request.UsePassive = true;
            request.EnableSsl = true;
            request.Timeout = settings.ConnectionTimeoutSeconds * 1000;
            request.ReadWriteTimeout = settings.OperationTimeoutSeconds * 1000;

            var user = string.IsNullOrWhiteSpace(settings.UserName) ? "anonymous" : settings.UserName;
            var pass = string.IsNullOrWhiteSpace(settings.Password) ? "anonymous" : settings.Password;
            request.Credentials = new NetworkCredential(user, pass);

            if (requestBodyContent != null)
            {
                var bytes = Encoding.UTF8.GetBytes(requestBodyContent);
                request.ContentLength = bytes.Length;
            }

            return request;
        }

        /// <summary>
        /// Разбирает ответ LIST на имена файлов/папок: не-пустые строки,
        /// без служебных префиксов, без записей «.» и «..».
        /// </summary>
        public List<string> ParseListing(string listResponse)
        {
            var names = new List<string>();
            if (string.IsNullOrEmpty(listResponse))
                return names;

            foreach (var line in listResponse.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var name = line.Trim();
                if (name.Length == 0)
                    continue;
                // FtpWebRequest.ListDirectory обычно возвращает чистые имена,
                // но некоторые серверы добавляют детали — отрезаем 8 полей
                // формата UNIX: "drwxr-xr-x 1 owner group 4096 Jan 1 12:00 name"
                var parts = name.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 8 && IsUnixDetailsLine(name))
                    name = string.Join(" ", parts, 8, parts.Length - 8);

                if (name == "." || name == "..")
                    continue;

                names.Add(name);
            }

            return names;
        }

        /// <summary>Признак строки с деталями UNIX-листинга (права доступа в начале).</summary>
        private static bool IsUnixDetailsLine(string line)
        {
            return line.StartsWith("d") || line.StartsWith("-") || line.StartsWith("l")
                || char.IsLetterOrDigit(line.Length > 1 ? line[1] : ' ');
        }

        // ── Сетевые операции (интеграционные тесты) ────────────────────

        /// <summary>Скачивает файл на диск. Возвращает путь и число байт.</summary>
        public (string LocalPath, long Bytes) DownloadFile(
            FtpsConnectionSettings settings, string remotePath, string localPath, bool overwrite)
        {
            Guard.NotNullOrWhiteSpace(localPath, nameof(localPath));

            if (!overwrite && File.Exists(localPath))
                throw new InvalidOperationException($"Локальный файл уже существует (перезапись выключена): {localPath}");

            var dir = Path.GetDirectoryName(Path.GetFullPath(localPath));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var request = CreateRequest(settings, remotePath, WebRequestMethods.Ftp.DownloadFile);
            using (var response = (FtpWebResponse)request.GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write))
            {
                responseStream.CopyTo(fileStream);
                return (Path.GetFullPath(localPath), fileStream.Length);
            }
        }

        /// <summary>Загружает файл на сервер. Возвращает число байт.</summary>
        public long UploadFile(
            FtpsConnectionSettings settings, string localPath, string remotePath)
        {
            Guard.NotNullOrWhiteSpace(localPath, nameof(localPath));

            if (!File.Exists(localPath))
                throw new FileNotFoundException($"Локальный файл не найден: {localPath}", localPath);

            var request = CreateRequest(settings, remotePath, WebRequestMethods.Ftp.UploadFile);
            using (var requestStream = request.GetRequestStream())
            using (var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read))
            {
                fileStream.CopyTo(requestStream);
            }

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                return new FileInfo(localPath).Length;
            }
        }

        /// <summary>Получает список файлов/папок по удалённому пути.</summary>
        public List<string> ListDirectory(FtpsConnectionSettings settings, string remotePath)
        {
            var request = CreateRequest(settings, remotePath, WebRequestMethods.Ftp.ListDirectory);
            using (var response = (FtpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return ParseListing(reader.ReadToEnd());
            }
        }

        /// <summary>Удаляет файл на сервере.</summary>
        public void DeleteFile(FtpsConnectionSettings settings, string remotePath)
        {
            var request = CreateRequest(settings, remotePath, WebRequestMethods.Ftp.DeleteFile);
            using (var response = (FtpWebResponse)request.GetResponse())
            {
            }
        }

        /// <summary>Создаёт папку на сервере (ошибка при существовании — наружу).</summary>
        public void CreateFolder(FtpsConnectionSettings settings, string remotePath)
        {
            var request = CreateRequest(settings, remotePath, WebRequestMethods.Ftp.MakeDirectory);
            using (var response = (FtpWebResponse)request.GetResponse())
            {
            }
        }
    }
}
