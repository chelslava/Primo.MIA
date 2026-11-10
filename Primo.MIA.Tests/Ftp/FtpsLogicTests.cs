using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.Ftp
{
    /// <summary>
    /// Тесты чистых частей FtpsLogic (issue #31): построение URI,
    /// настройка запроса (explicit TLS, passive, binary, учётные данные,
    /// таймауты), парсинг листинга. Сетевые операции — интеграционные
    /// (требуют реального FTPS-сервера) и unit-тестами не покрываются.
    /// </summary>
    [Trait("Category", "Fast")]
    public class FtpsLogicTests
    {
        private readonly FtpsLogic _logic = new FtpsLogic();

        private FtpsConnectionSettings Settings(string host = "ftp.example.com", int port = 21) => new FtpsConnectionSettings
        {
            Host = host,
            Port = port,
            UserName = "user",
            Password = "secret",
            ConnectionTimeoutSeconds = 15,
            OperationTimeoutSeconds = 120
        };

        // ── BuildRequestUri ────────────────────────────────────────────

        [Fact(DisplayName = "URI: стандартный порт скрыт, путь нормализован")]
        public void BuildUri_StandardPort_CleanPath()
        {
            var uri = _logic.BuildRequestUri(Settings(), "/uploads/data.csv");
            uri.Should().Be("ftp://ftp.example.com/uploads/data.csv");
        }

        [Fact(DisplayName = "URI: нестандартный порт присутствует")]
        public void BuildUri_CustomPort_Included()
        {
            var uri = _logic.BuildRequestUri(Settings(port: 990), "/dir");
            uri.Should().Be("ftp://ftp.example.com:990/dir");
        }

        [Fact(DisplayName = "URI: обратные слэши и отсутствие ведущего '/' нормализуются")]
        public void BuildUri_NormalizesSlashes()
        {
            var uri = _logic.BuildRequestUri(Settings(), "\\uploads\\2026-11");
            uri.Should().Be("ftp://ftp.example.com/uploads/2026-11");
        }

        [Fact(DisplayName = "URI: пробелы в пути экранируются, '/' внутри пути сохраняются")]
        public void BuildUri_EscapesSpaces_KeepsSlashes()
        {
            var uri = _logic.BuildRequestUri(Settings(), "/my dir/report 1.csv");
            uri.Should().Be("ftp://ftp.example.com/my%20dir/report%201.csv");
        }

        [Fact(DisplayName = "URI: пустой удалённый путь — ArgumentException")]
        public void BuildUri_EmptyPath_Throws()
        {
            Action act = () => _logic.BuildRequestUri(Settings(), " ");
            act.Should().Throw<ArgumentException>();
        }

        [Fact(DisplayName = "URI: пустой хост — ArgumentException")]
        public void BuildUri_EmptyHost_Throws()
        {
            Action act = () => _logic.BuildRequestUri(Settings(host: " "), "/x");
            act.Should().Throw<ArgumentException>();
        }

        // ── CreateRequest: настройка explicit TLS ──────────────────────

        [Fact(DisplayName = "Request: EnableSsl (explicit TLS) + passive + binary включены")]
        public void CreateRequest_SetsFtpsOptions()
        {
            var request = _logic.CreateRequest(Settings(), "/file.txt", WebRequestMethods.Ftp.DownloadFile);

            request.EnableSsl.Should().BeTrue("FTPS = explicit TLS обязателен");
            request.UsePassive.Should().BeTrue();
            request.UseBinary.Should().BeTrue();
        }

        [Fact(DisplayName = "Request: метод, таймауты, учётные данные")]
        public void CreateRequest_SetsMethodTimeoutsCredentials()
        {
            var request = _logic.CreateRequest(Settings(), "/file.txt", WebRequestMethods.Ftp.UploadFile);

            request.Method.Should().Be(WebRequestMethods.Ftp.UploadFile);
            request.Timeout.Should().Be(15_000, "ConnectionTimeoutSeconds × 1000");
            request.ReadWriteTimeout.Should().Be(120_000, "OperationTimeoutSeconds × 1000");

            var credentials = (NetworkCredential)request.Credentials;
            credentials.UserName.Should().Be("user");
            credentials.Password.Should().Be("secret");
        }

        [Fact(DisplayName = "Request: пустой пользователь/пароль → anonymous")]
        public void CreateRequest_EmptyCredentials_Anonymous()
        {
            var s = Settings();
            s.UserName = null;
            s.Password = "";

            var request = _logic.CreateRequest(s, "/file.txt", WebRequestMethods.Ftp.ListDirectory);
            var credentials = (NetworkCredential)request.Credentials;
            credentials.UserName.Should().Be("anonymous");
            credentials.Password.Should().Be("anonymous");
        }

        [Fact(DisplayName = "Request: URI запроса содержит путь")]
        public void CreateRequest_RequestUriMatches()
        {
            var request = _logic.CreateRequest(Settings(), "/uploads/data.csv", WebRequestMethods.Ftp.DownloadFile);
            request.RequestUri.ToString().Should().Be("ftp://ftp.example.com/uploads/data.csv");
        }

        // ── ParseListing ───────────────────────────────────────────────

        [Fact(DisplayName = "Listing: чистые имена по строкам")]
        public void ParseListing_PlainNames()
        {
            var names = _logic.ParseListing("data.csv\r\nreports\r\narchive.zip\r\n");
            names.Should().BeEquivalentTo("data.csv", "reports", "archive.zip");
        }

        [Fact(DisplayName = "Listing: строки-детали UNIX отрезаются до имени")]
        public void ParseListing_UnixDetailsLine()
        {
            var listing = "-rw-r--r-- 1 user group 4096 Jan 15 12:00 data.csv\r\ndrwxr-xr-x 1 user group 4096 Jan 15 12:00 reports\r\n";
            var names = _logic.ParseListing(listing);

            names.Should().Contain("data.csv");
            names.Should().Contain("reports");
            names.Count.Should().Be(2);
        }

        [Fact(DisplayName = "Listing: точки «.» и «..» отбрасываются, пустые строки пропускаются")]
        public void ParseListing_DotsAndEmptyLines()
        {
            var names = _logic.ParseListing(".\r\n..\r\n\r\nfile.txt\r\n");
            names.Should().BeEquivalentTo("file.txt");
        }

        [Fact(DisplayName = "Listing: null/пустой ответ — пустой список")]
        public void ParseListing_EmptyResponse()
        {
            _logic.ParseListing(null).Should().BeEmpty();
            _logic.ParseListing("").Should().BeEmpty();
        }

        // ── Guard сетевых операций без сети ────────────────────────────

        [Fact(DisplayName = "UploadFile: несуществующий локальный файл — FileNotFoundException (без сети)")]
        public void UploadFile_MissingLocalFile_Throws()
        {
            Action act = () => _logic.UploadFile(
                Settings(), @"C:\nonexistent\file.bin", "/remote.bin");
            act.Should().Throw<FileNotFoundException>();
        }
    }
}
