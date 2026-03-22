using FluentAssertions;
using Primo.MIA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using Xunit;

namespace Primo.MIA.Tests.HttpWeb
{
    public class HttpHelperTests
    {
        #region ParseHeaders

        [Fact]
        public void ParseHeaders_ValidJson_ReturnsDictionary()
        {
            var result = HttpHelper.ParseHeaders("{\"Content-Type\":\"application/json\",\"Authorization\":\"Bearer token\"}");

            result.Should().HaveCount(2);
            result["Content-Type"].Should().Be("application/json");
            result["Authorization"].Should().Be("Bearer token");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("{}")]
        [InlineData("{invalid json}")]
        public void ParseHeaders_EmptyOrInvalidInput_ReturnsEmptyDictionary(string input)
        {
            var result = HttpHelper.ParseHeaders(input);

            result.Should().BeEmpty();
        }

        [Fact]
        public void ParseHeaders_ValuesWithSpaces_ParsesCorrectly()
        {
            var result = HttpHelper.ParseHeaders("{\"X-Custom-Header\":\"value with spaces\",\"Accept-Language\":\"ru-RU,en-US\"}");

            result.Should().ContainKey("X-Custom-Header");
            result["X-Custom-Header"].Should().Be("value with spaces");
            result["Accept-Language"].Should().Be("ru-RU,en-US");
        }

        #endregion

        #region NormalizeHeaders

        [Fact]
        public void NormalizeHeaders_TrimsKeys_AndFiltersEmptyValues()
        {
            var headers = new Dictionary<string, string>
            {
                { "  Content-Type  ", "application/json" },
                { "", "skip" },
                { "   ", "skip" },
                { "Null-Header", null }
            };

            var result = HttpHelper.NormalizeHeaders(headers);

            result.Should().HaveCount(1);
            result.Should().ContainKey("Content-Type");
            result["Content-Type"].Should().Be("application/json");
        }

        [Fact]
        public void NormalizeHeaders_NullInput_ReturnsEmptyDictionary()
        {
            var result = HttpHelper.NormalizeHeaders(null);

            result.Should().BeEmpty();
        }

        #endregion

        #region SerializeResponseHeaders

        [Fact]
        public void SerializeResponseHeaders_IncludesResponseAndContentHeaders()
        {
            var response = new HttpResponseMessage();
            response.Headers.Add("Server", "nginx");
            response.Headers.Add("X-Request-Id", "12345");
            response.Content = new StringContent("test");
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            response.Content.Headers.Add("Content-Language", "ru-RU");

            var result = HttpHelper.SerializeResponseHeaders(response.Headers, response.Content.Headers);

            result.Should().StartWith("{");
            result.Should().EndWith("}");
            result.Should().Contain("\"Server\":\"nginx\"");
            result.Should().Contain("\"X-Request-Id\":\"12345\"");
            result.Should().Contain("\"Content-Type\":\"application/json\"");
            result.Should().Contain("\"Content-Language\":\"ru-RU\"");
        }

        [Fact]
        public void SerializeResponseHeaders_NullHeaders_ReturnsEmptyJson()
        {
            var result = HttpHelper.SerializeResponseHeaders(null, null);

            result.Should().Be("{}");
        }

        [Fact]
        public void SerializeResponseHeaders_JoinsMultipleValuesWithComma()
        {
            var response = new HttpResponseMessage();
            response.Headers.Add("Accept", new[] { "application/json", "text/html" });

            var result = HttpHelper.SerializeResponseHeaders(response.Headers, null);

            result.Should().Contain("\"Accept\":\"application/json, text/html\"");
        }

        #endregion

        #region ValidateUrl

        [Theory]
        [InlineData("https://api.example.com", true)]
        [InlineData("http://localhost:8080/path", true)]
        [InlineData("ftp://example.com", false)]
        [InlineData("file:///C:/test.txt", false)]
        [InlineData("/api/users", false)]
        [InlineData(null, false)]
        [InlineData("   ", false)]
        public void ValidateUrl_ReturnsExpectedResult(string url, bool expected)
        {
            HttpHelper.ValidateUrl(url).Should().Be(expected);
        }

        #endregion

        #region ValidateCertificatePath

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateCertificatePath_EmptyInput_ReturnsFalse(string path)
        {
            HttpHelper.ValidateCertificatePath(path).Should().BeFalse();
        }

        [Fact]
        public void ValidateCertificatePath_MissingFile_ReturnsFalse()
        {
            var path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.pfx");

            HttpHelper.ValidateCertificatePath(path).Should().BeFalse();
        }

        [Fact]
        public void ValidateCertificatePath_WrongExtension_ReturnsFalse()
        {
            var path = Path.Combine(Path.GetTempPath(), $"certificate-{Guid.NewGuid():N}.txt");
            File.WriteAllText(path, "dummy");

            try
            {
                HttpHelper.ValidateCertificatePath(path).Should().BeFalse();
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void ValidateCertificatePath_PfxExtension_IsCaseInsensitive()
        {
            var path = Path.Combine(Path.GetTempPath(), $"certificate-{Guid.NewGuid():N}.PFX");
            File.WriteAllText(path, "dummy");

            try
            {
                HttpHelper.ValidateCertificatePath(path).Should().BeTrue();
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        #endregion

        #region ParseTimeout

        [Theory]
        [InlineData("30", 30)]
        [InlineData("1", 1)]
        [InlineData("999", 999)]
        public void ParseTimeout_ValidNumbers_ReturnExpectedValue(string input, int expected)
        {
            HttpHelper.ParseTimeout(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abc")]
        [InlineData("12.5")]
        [InlineData("0")]
        [InlineData("-10")]
        public void ParseTimeout_InvalidOrNonPositiveInput_ReturnsDefaultValue(string input)
        {
            HttpHelper.ParseTimeout(input).Should().Be(100);
        }

        [Fact]
        public void ParseTimeout_CustomDefault_IsUsedForInvalidInput()
        {
            HttpHelper.ParseTimeout("invalid", 60).Should().Be(60);
        }

        #endregion

        #region RetryLogic

        [Theory]
        [InlineData(1, 1000, 1000)]
        [InlineData(2, 1000, 1000)]
        [InlineData(0, 0, 1000)]
        public void CalculateRetryDelay_Fixed_UsesBaseDelay(int attempt, int baseDelayMs, int expected)
        {
            CalculateRetryDelay("Fixed", baseDelayMs, attempt).Should().Be(expected);
        }

        [Theory]
        [InlineData(1, 250, 250)]
        [InlineData(2, 250, 500)]
        [InlineData(3, 250, 750)]
        public void CalculateRetryDelay_Linear_MultipliesByAttempt(int attempt, int baseDelayMs, int expected)
        {
            CalculateRetryDelay("Linear", baseDelayMs, attempt).Should().Be(expected);
        }

        [Theory]
        [InlineData(1, 200, 200)]
        [InlineData(2, 200, 400)]
        [InlineData(3, 200, 800)]
        public void CalculateRetryDelay_Exponential_DoublesPerAttempt(int attempt, int baseDelayMs, int expected)
        {
            CalculateRetryDelay("Exponential", baseDelayMs, attempt).Should().Be(expected);
        }

        [Fact]
        public void CalculateRetryDelay_NormalizesInvalidInputs()
        {
            CalculateRetryDelay("Fixed", -1, 0).Should().Be(1000);
            CalculateRetryDelay("Linear", -1, 0).Should().Be(1000);
            CalculateRetryDelay("Exponential", -1, 0).Should().Be(1000);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("invalid")]
        public void ParseRetryStatusCodes_InvalidInput_ReturnsDefaultCodes(string input)
        {
            HttpHelper.ParseRetryStatusCodes(input).Should().BeEquivalentTo(HttpHelper.DefaultRetryStatusCodes);
        }

        [Fact]
        public void ParseRetryStatusCodes_ParsesMultipleSeparators()
        {
            var result = HttpHelper.ParseRetryStatusCodes("500, 502;503 504");

            result.Should().BeEquivalentTo(new[] { 500, 502, 503, 504 });
        }

        [Fact]
        public void ParseRetryStatusCodes_IgnoresInvalidAndNonPositiveValues()
        {
            var result = HttpHelper.ParseRetryStatusCodes("500, -1, abc, 429");

            result.Should().BeEquivalentTo(new[] { 500, 429 });
        }

        [Fact]
        public void ShouldRetry_UsesDefaultCodesWhenInputIsEmpty()
        {
            HttpHelper.ShouldRetry(500, null).Should().BeTrue();
            HttpHelper.ShouldRetry(418, Array.Empty<int>()).Should().BeFalse();
        }

        [Fact]
        public void ShouldRetry_UsesProvidedCodes()
        {
            HttpHelper.ShouldRetry(429, new[] { 429, 503 }).Should().BeTrue();
            HttpHelper.ShouldRetry(400, new[] { 429, 503 }).Should().BeFalse();
        }

        #endregion

        #region MimeAndFileName

        [Theory]
        [InlineData("document.txt", "text/plain")]
        [InlineData("report.PDF", "application/pdf")]
        [InlineData("image.jpeg", "image/jpeg")]
        [InlineData("data.unknown", "application/octet-stream")]
        [InlineData(null, "application/octet-stream")]
        public void GetMimeType_ReturnsExpectedMimeType(string filePath, string expected)
        {
            HttpHelper.GetMimeType(filePath).Should().Be(expected);
        }

        [Fact]
        public void ExtractFileName_UsesContentDispositionWhenAvailable()
        {
            var result = HttpHelper.ExtractFileName(
                "https://example.com/files/fallback-name.txt",
                "attachment; filename=\"report-final.pdf\"");

            result.Should().Be("report-final.pdf");
        }

        [Fact]
        public void ExtractFileName_FallsBackToUrlPath()
        {
            var result = HttpHelper.ExtractFileName(
                "https://example.com/files/archive.tar.gz?download=1",
                null);

            result.Should().Be("archive.tar.gz");
        }

        [Fact]
        public void ExtractFileName_FilenameStarIsSupported()
        {
            var result = HttpHelper.ExtractFileName(
                null,
                "attachment; filename*=report%20final.pdf");

            result.Should().Be("report%20final.pdf");
        }

        #endregion

        #region JsonHelpers

        [Theory]
        [InlineData("{\"name\":\"Alice\",\"age\":30}")]
        [InlineData("[1,2,3]")]
        public void IsValidJson_ValidJson_ReturnsTrue(string json)
        {
            HttpHelper.IsValidJson(json).Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("{invalid}")]
        [InlineData("{\"name\":\"Alice\",}")]
        public void IsValidJson_InvalidJson_ReturnsFalse(string json)
        {
            HttpHelper.IsValidJson(json).Should().BeFalse();
        }

        [Fact]
        public void FormatJson_FormatsValidJson()
        {
            var result = HttpHelper.FormatJson("{\"name\":\"Alice\",\"age\":30}");

            result.Should().Contain(Environment.NewLine);
            result.Should().Contain("  \"name\": \"Alice\"");
            result.Should().Contain("  \"age\": 30");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("{invalid}")]
        public void FormatJson_InvalidInput_ReturnsOriginalValue(string json)
        {
            HttpHelper.FormatJson(json).Should().Be(json);
        }

        [Fact]
        public void MinifyJson_MinifiesValidJson()
        {
            var result = HttpHelper.MinifyJson("{\"name\": \"Alice\", \"age\": 30}");

            result.Should().Be("{\"name\":\"Alice\",\"age\":30}");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("{invalid}")]
        public void MinifyJson_InvalidInput_ReturnsOriginalValue(string json)
        {
            HttpHelper.MinifyJson(json).Should().Be(json);
        }

        #endregion

        private static int CalculateRetryDelay(string strategyName, int baseDelayMs, int attempt)
        {
            var method = typeof(HttpHelper).GetMethod(nameof(HttpHelper.CalculateRetryDelay));
            var strategyType = typeof(HttpHelper).Assembly.GetTypes().Single(t => t.Name == "HttpRetryDelayStrategy");
            var strategy = Enum.Parse(strategyType, strategyName, ignoreCase: true);

            return (int)method.Invoke(null, new[] { strategy, baseDelayMs, attempt });
        }
    }
}
