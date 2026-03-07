using System;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.Generators
{
    public class GeneratorLogicTests
    {
        #region GUID Tests

        [Fact]
        public void GenerateGuid_ReturnsValidGuid()
        {
            // Act
            var guid = Guid.NewGuid();

            // Assert
            guid.Should().NotBe(Guid.Empty);
        }

        [Theory]
        [InlineData("N", 32)] // без дефисов
        [InlineData("D", 36)] // с дефисами
        [InlineData("B", 38)] // с фигурными скобками
        [InlineData("P", 38)] // с круглыми скобками
        public void GenerateGuid_DifferentFormats_ReturnsCorrectLength(string format, int expectedLength)
        {
            // Arrange
            var guid = Guid.NewGuid();

            // Act
            var result = guid.ToString(format);

            // Assert
            result.Should().HaveLength(expectedLength);
        }

        [Fact]
        public void GenerateGuid_FormatN_ContainsOnlyHexDigits()
        {
            // Arrange
            var guid = Guid.NewGuid();

            // Act
            var result = guid.ToString("N");

            // Assert
            result.Should().MatchRegex("^[0-9a-f]{32}$");
        }

        #endregion

        #region RandomNumber Tests

        [Fact]
        public void GenerateRandomNumber_InRange_ReturnsValueWithinBounds()
        {
            // Arrange
            var random = new Random();
            int min = 10;
            int max = 20;

            // Act
            var result = random.Next(min, max + 1);

            // Assert
            result.Should().BeInRange(min, max);
        }

        [Fact]
        public void GenerateRandomNumber_MultipleGenerations_ProducesDifferentValues()
        {
            // Arrange
            var random = new Random();
            var results = new System.Collections.Generic.HashSet<int>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                results.Add(random.Next(1, 1000));
            }

            // Assert
            results.Should().HaveCountGreaterThan(50, "должны быть разные значения");
        }

        #endregion

        #region Timestamp Tests

        [Fact]
        public void GenerateTimestamp_yyyyMMdd_HHmmss_MatchesFormat()
        {
            // Arrange
            var now = DateTime.Now;

            // Act
            var result = now.ToString("yyyyMMdd_HHmmss");

            // Assert
            result.Should().MatchRegex(@"^\d{8}_\d{6}$");
        }

        [Fact]
        public void GenerateTimestamp_ISO8601_MatchesFormat()
        {
            // Arrange
            var now = DateTime.Now;

            // Act
            var result = now.ToString("yyyy-MM-ddTHH:mm:ss");

            // Assert
            result.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$");
        }

        [Fact]
        public void GenerateTimestamp_CustomFormat_WorksCorrectly()
        {
            // Arrange
            var now = new DateTime(2025, 2, 15, 14, 30, 45);

            // Act
            var result = now.ToString("dd.MM.yyyy HH:mm:ss");

            // Assert
            result.Should().Be("15.02.2025 14:30:45");
        }

        #endregion

        #region HashId Tests

        [Fact]
        public void GenerateHashId_SameInput_ProducesSameHash()
        {
            // Arrange
            string input = "test input";

            // Act
            var hash1 = ComputeSHA256(input);
            var hash2 = ComputeSHA256(input);

            // Assert
            hash1.Should().Be(hash2);
        }

        [Fact]
        public void GenerateHashId_DifferentInputs_ProduceDifferentHashes()
        {
            // Arrange
            string input1 = "test input 1";
            string input2 = "test input 2";

            // Act
            var hash1 = ComputeSHA256(input1);
            var hash2 = ComputeSHA256(input2);

            // Assert
            hash1.Should().NotBe(hash2);
        }

        [Fact]
        public void GenerateHashId_ReturnsHexString()
        {
            // Arrange
            string input = "test";

            // Act
            var hash = ComputeSHA256(input);

            // Assert
            hash.Should().HaveLength(64); // SHA-256 = 32 bytes = 64 hex chars
            hash.Should().MatchRegex("^[0-9a-f]{64}$");
        }

        private string ComputeSHA256(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                var sb = new System.Text.StringBuilder(hashBytes.Length * 2);
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        #endregion

        #region Username Tests

        [Theory]
        [InlineData("John", "Doe", "example.com", "john.doe@example.com")]
        [InlineData("JOHN", "DOE", "EXAMPLE.COM", "john.doe@example.com")]
        [InlineData("Jane", "Smith", "company.ru", "jane.smith@company.ru")]
        public void GenerateUsername_FormatsCorrectly(string firstName, string lastName, string domain, string expected)
        {
            // Act
            var result = $"{firstName.ToLower()}.{lastName.ToLower()}@{domain.ToLower()}";

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void GenerateUsername_ValidEmail_MatchesEmailPattern()
        {
            // Arrange
            string firstName = "John";
            string lastName = "Doe";
            string domain = "example.com";

            // Act
            var result = $"{firstName.ToLower()}.{lastName.ToLower()}@{domain.ToLower()}";

            // Assert
            result.Should().MatchRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        #endregion

        #region Template Tests

        [Fact]
        public void GenerateTemplate_ReplacesPlaceholders()
        {
            // Arrange
            string template = "Hello, {name}! Your order #{orderId} is ready.";
            var variables = new System.Collections.Generic.Dictionary<string, object>
            {
                { "name", "John" },
                { "orderId", "12345" }
            };

            // Act
            var result = Regex.Replace(template, @"\{(\w+)\}", match =>
            {
                string key = match.Groups[1].Value;
                return variables.ContainsKey(key) ? variables[key]?.ToString() ?? string.Empty : match.Value;
            });

            // Assert
            result.Should().Be("Hello, John! Your order #12345 is ready.");
        }

        [Fact]
        public void GenerateTemplate_UnknownPlaceholder_LeavesUnchanged()
        {
            // Arrange
            string template = "Hello, {name}! Your code is {code}.";
            var variables = new System.Collections.Generic.Dictionary<string, object>
            {
                { "name", "John" }
            };

            // Act
            var result = Regex.Replace(template, @"\{(\w+)\}", match =>
            {
                string key = match.Groups[1].Value;
                return variables.ContainsKey(key) ? variables[key]?.ToString() ?? string.Empty : match.Value;
            });

            // Assert
            result.Should().Be("Hello, John! Your code is {code}.");
        }

        #endregion
    }
}
