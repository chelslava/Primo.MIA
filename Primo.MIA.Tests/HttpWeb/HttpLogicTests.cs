using System;
using System.Collections.Generic;
using System.Net.Http;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.HttpWeb
{
    /// <summary>
    /// Тесты для логики HTTP активности
    /// </summary>
    public class HttpLogicTests
    {
        #region ParseHeaders Tests

        [Fact]
        public void ParseHeaders_ValidJson_ReturnsCorrectDictionary()
        {
            // Arrange
            string json = "{\"Content-Type\": \"application/json\", \"Authorization\": \"Bearer token123\"}";

            // Act
            var result = HttpLogic.ParseHeaders(json);

            // Assert
            result.Should().HaveCount(2);
            result["Content-Type"].Should().Be("application/json");
            result["Authorization"].Should().Be("Bearer token123");
        }

        [Fact]
        public void ParseHeaders_EmptyJson_ReturnsEmptyDictionary()
        {
            // Arrange
            string json = "{}";

            // Act
            var result = HttpLogic.ParseHeaders(json);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ParseHeaders_NullString_ReturnsEmptyDictionary()
        {
            // Act
            var result = HttpLogic.ParseHeaders(null);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ParseHeaders_WhitespaceString_ReturnsEmptyDictionary()
        {
            // Act
            var result = HttpLogic.ParseHeaders("   ");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ParseHeaders_InvalidJson_ReturnsEmptyDictionary()
        {
            // Arrange
            string invalidJson = "{invalid json}";

            // Act
            var result = HttpLogic.ParseHeaders(invalidJson);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ParseHeaders_JsonWithSpecialCharacters_ParsesCorrectly()
        {
            // Arrange
            string json = "{\"X-Custom-Header\": \"value with spaces\", \"Accept-Language\": \"ru-RU,en-US\"}";

            // Act
            var result = HttpLogic.ParseHeaders(json);

            // Assert
            result.Should().HaveCount(2);
            result["X-Custom-Header"].Should().Be("value with spaces");
            result["Accept-Language"].Should().Be("ru-RU,en-US");
        }

        #endregion

        #region SerializeResponseHeaders Tests

        [Fact]
        public void SerializeResponseHeaders_WithBothHeaders_ReturnsValidJson()
        {
            // Arrange
            var response = new HttpResponseMessage();
            response.Headers.Add("Server", "nginx");
            response.Headers.Add("X-Request-Id", "12345");
            response.Content = new StringContent("test");
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            // Act
            var result = HttpLogic.SerializeResponseHeaders(response.Headers, response.Content.Headers);

            // Assert
            result.Should().Contain("\"Server\"");
            result.Should().Contain("nginx");
            result.Should().Contain("\"Content-Type\"");
            result.Should().Contain("application/json");
        }

        [Fact]
        public void SerializeResponseHeaders_NullHeaders_ReturnsEmptyJson()
        {
            // Act
            var result = HttpLogic.SerializeResponseHeaders(null, null);

            // Assert
            result.Should().Be("{}");
        }

        [Fact]
        public void SerializeResponseHeaders_MultipleValues_JoinsWithComma()
        {
            // Arrange
            var response = new HttpResponseMessage();
            response.Headers.Add("Accept", new[] { "application/json", "text/html" });

            // Act
            var result = HttpLogic.SerializeResponseHeaders(response.Headers, null);

            // Assert
            result.Should().Contain("application/json, text/html");
        }

        #endregion

        #region ValidateUrl Tests

        [Theory]
        [InlineData("https://api.example.com", true)]
        [InlineData("http://localhost:8080", true)]
        [InlineData("https://example.com/api/v1/users", true)]
        [InlineData("http://192.168.1.1:3000/endpoint", true)]
        public void ValidateUrl_ValidUrls_ReturnsTrue(string url, bool expected)
        {
            // Act
            var result = HttpLogic.ValidateUrl(url);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("   ", false)]
        [InlineData("not a url", false)]
        [InlineData("ftp://example.com", false)]
        [InlineData("file:///C:/test.txt", false)]
        public void ValidateUrl_InvalidUrls_ReturnsFalse(string url, bool expected)
        {
            // Act
            var result = HttpLogic.ValidateUrl(url);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ValidateUrl_RelativeUrl_ReturnsFalse()
        {
            // Arrange
            string url = "/api/users";

            // Act
            var result = HttpLogic.ValidateUrl(url);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region ParseTimeout Tests

        [Theory]
        [InlineData("30", 30)]
        [InlineData("100", 100)]
        [InlineData("1", 1)]
        [InlineData("999", 999)]
        public void ParseTimeout_ValidNumbers_ReturnsCorrectValue(string input, int expected)
        {
            // Act
            var result = HttpLogic.ParseTimeout(input);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("", 100)]
        [InlineData(null, 100)]
        [InlineData("   ", 100)]
        [InlineData("abc", 100)]
        [InlineData("12.5", 100)]
        public void ParseTimeout_InvalidInput_ReturnsDefaultValue(string input, int expected)
        {
            // Act
            var result = HttpLogic.ParseTimeout(input);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ParseTimeout_NegativeNumber_ReturnsDefaultValue()
        {
            // Act
            var result = HttpLogic.ParseTimeout("-10");

            // Assert
            result.Should().Be(100);
        }

        [Fact]
        public void ParseTimeout_Zero_ReturnsDefaultValue()
        {
            // Act
            var result = HttpLogic.ParseTimeout("0");

            // Assert
            result.Should().Be(100);
        }

        [Fact]
        public void ParseTimeout_CustomDefault_ReturnsCustomValue()
        {
            // Act
            var result = HttpLogic.ParseTimeout("invalid", 60);

            // Assert
            result.Should().Be(60);
        }

        #endregion

        #region ValidateCertificatePath Tests

        [Fact]
        public void ValidateCertificatePath_NullPath_ReturnsFalse()
        {
            // Act
            var result = HttpLogic.ValidateCertificatePath(null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateCertificatePath_EmptyPath_ReturnsFalse()
        {
            // Act
            var result = HttpLogic.ValidateCertificatePath("");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateCertificatePath_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            string path = "C:\\NonExistent\\certificate.pfx";

            // Act
            var result = HttpLogic.ValidateCertificatePath(path);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateCertificatePath_WrongExtension_ReturnsFalse()
        {
            // Arrange - создаем временный файл с неправильным расширением
            string tempFile = System.IO.Path.GetTempFileName(); // создает .tmp файл

            try
            {
                // Act
                var result = HttpLogic.ValidateCertificatePath(tempFile);

                // Assert
                result.Should().BeFalse();
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);
            }
        }

        [Fact]
        public void ValidateCertificatePath_ValidPfxFile_ReturnsTrue()
        {
            // Arrange - создаем временный .pfx файл
            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_cert.pfx");
            System.IO.File.WriteAllText(tempFile, "dummy content");

            try
            {
                // Act
                var result = HttpLogic.ValidateCertificatePath(tempFile);

                // Assert
                result.Should().BeTrue();
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);
            }
        }

        [Fact]
        public void ValidateCertificatePath_CaseInsensitive_ReturnsTrue()
        {
            // Arrange - создаем файл с расширением .PFX (заглавные буквы)
            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_cert.PFX");
            System.IO.File.WriteAllText(tempFile, "dummy content");

            try
            {
                // Act
                var result = HttpLogic.ValidateCertificatePath(tempFile);

                // Assert
                result.Should().BeTrue();
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);
            }
        }

        #endregion

        #region NormalizeHeaders Tests

        [Fact]
        public void NormalizeHeaders_ValidHeaders_ReturnsNormalizedDictionary()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "  Authorization  ", "Bearer token" },
                { "X-Custom", "value" }
            };

            // Act
            var result = HttpLogic.NormalizeHeaders(headers);

            // Assert
            result.Should().HaveCount(3);
            result["Content-Type"].Should().Be("application/json");
            result["Authorization"].Should().Be("Bearer token");
            result["X-Custom"].Should().Be("value");
        }

        [Fact]
        public void NormalizeHeaders_NullDictionary_ReturnsEmptyDictionary()
        {
            // Act
            var result = HttpLogic.NormalizeHeaders(null);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void NormalizeHeaders_EmptyKeys_FiltersOut()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "Valid-Header", "value" },
                { "", "empty key" },
                { "   ", "whitespace key" }
            };

            // Act
            var result = HttpLogic.NormalizeHeaders(headers);

            // Assert
            result.Should().HaveCount(1);
            result.Should().ContainKey("Valid-Header");
        }

        [Fact]
        public void NormalizeHeaders_NullValues_FiltersOut()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "Valid-Header", "value" },
                { "Null-Header", null }
            };

            // Act
            var result = HttpLogic.NormalizeHeaders(headers);

            // Assert
            result.Should().HaveCount(1);
            result.Should().ContainKey("Valid-Header");
        }

        [Fact]
        public void NormalizeHeaders_TrimsKeys_PreservesValues()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "  Trimmed-Key  ", "  value with spaces  " }
            };

            // Act
            var result = HttpLogic.NormalizeHeaders(headers);

            // Assert
            result.Should().ContainKey("Trimmed-Key");
            result["Trimmed-Key"].Should().Be("  value with spaces  ");
        }

        #endregion
    }
}
