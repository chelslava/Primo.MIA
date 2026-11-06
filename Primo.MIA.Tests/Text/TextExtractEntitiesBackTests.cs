using FluentAssertions;
using System;
using System.Collections;
using System.Reflection;
using Xunit;

namespace Primo.MIA.Tests.Text
{
    /// <summary>
    /// Smoke-тесты чистых функций TextExtractEntitiesBack (issue #16):
    ///   - нормализация российских сущностей: телефон, СНИЛС, паспорт, автономер;
    ///   - валидация контрольных цифр ИНН (алгоритм ФНС);
    ///   - реестр встроенных regex-паттернов (BuiltinPatterns / PatternByType).
    ///
    /// Отдельного *Logic.cs у активности нет, поэтому тестируются приватные
    /// СТАТИЧЕСКИЕ методы Back-класса через отражение — они не используют
    /// состояние SDK и не требуют экземпляра. SDK-зависимые части
    /// (TimedAction, Validate, свойства) не покрываются.
    /// </summary>
        [Trait("Category", "Fast")]
    public class TextExtractEntitiesBackTests
    {
        private static readonly Type BackType = typeof(Primo.MIA.TextExtractEntitiesBack);

        // =====================================================================
        // NormalizePhone — формат +7 (XXX) XXX-XX-XX
        // =====================================================================

        #region NormalizePhone

        [Theory]
        [InlineData("+7 (912) 345-67-89", "+7 (912) 345-67-89")]   // уже нормализован
        [InlineData("+79123456789", "+7 (912) 345-67-89")]          // без разделителей
        [InlineData("8-912-345-67-89", "+7 (912) 345-67-89")]       // ведущая 8 → 7
        [InlineData("8 (912) 345 67 89", "+7 (912) 345-67-89")]
        public void NormalizePhone_ValidRussianNumbers_ReturnsCanonicalFormat(string raw, string expected)
        {
            InvokeStaticString("NormalizePhone", raw).Should().Be(expected);
        }

        [Theory]
        [InlineData("12345")]            // слишком коротко
        [InlineData("712345678901")]     // 12 цифр
        [InlineData("+1 (555) 123-45-67")]// не российский код после нормализации
        public void NormalizePhone_UnnormalizableInput_ReturnsRawValue(string raw)
        {
            InvokeStaticString("NormalizePhone", raw).Should().Be(raw);
        }

        #endregion

        // =====================================================================
        // NormalizeSNILS — формат XXX-XXX-XXX XX
        // =====================================================================

        #region NormalizeSNILS

        [Fact]
        public void NormalizeSNILS_ElevenDigits_ReturnsFormattedValue()
        {
            InvokeStaticString("NormalizeSNILS", "112-233-445 95").Should().Be("112-233-445 95");
            InvokeStaticString("NormalizeSNILS", "11223344595").Should().Be("112-233-445 95");
        }

        [Fact]
        public void NormalizeSNILS_WrongDigitCount_ReturnsRawValue()
        {
            InvokeStaticString("NormalizeSNILS", "112-233-44").Should().Be("112-233-44");
            InvokeStaticString("NormalizeSNILS", "123456789012").Should().Be("123456789012");
        }

        #endregion

        // =====================================================================
        // NormalizePassport — формат XXXX XXXXXX
        // =====================================================================

        #region NormalizePassport

        [Fact]
        public void NormalizePassport_TenDigits_ReturnsSeriesAndNumber()
        {
            InvokeStaticString("NormalizePassport", "4605123456").Should().Be("4605 123456");
            InvokeStaticString("NormalizePassport", "4605-123456").Should().Be("4605 123456");
        }

        [Fact]
        public void NormalizePassport_WrongDigitCount_ReturnsRawValue()
        {
            InvokeStaticString("NormalizePassport", "4605 12345").Should().Be("4605 12345");
        }

        #endregion

        // =====================================================================
        // NormalizeCarNumber — латинские омоглифы → кириллица
        // =====================================================================

        #region NormalizeCarNumber

        [Fact]
        public void NormalizeCarNumber_LatinHomoglyphs_ReplacedWithCyrillic()
        {
            // OCR-текст: латинские A/B/C → кириллические А/В/С
            InvokeStaticString("NormalizeCarNumber", "a123bc 77").Should().Be("А123ВС77");
        }

        [Fact]
        public void NormalizeCarNumber_AlreadyCyrillic_OnlyUppercasedAndTrimmed()
        {
            InvokeStaticString("NormalizeCarNumber", "а123вс 77").Should().Be("А123ВС77");
        }

        #endregion

        // =====================================================================
        // ValidateINN — контрольные цифры по алгоритму ФНС
        // =====================================================================

        #region ValidateINN

        [Theory]
        [InlineData("7707083893")]    // 10-значный: валидная контрольная цифра
        [InlineData("500100732259")]  // 12-значный: валидные обе контрольные
        public void ValidateINN_ValidCheckDigits_ReturnsTrue(string inn)
        {
            InvokeStaticBool("ValidateINN", inn).Should().BeTrue();
        }

        [Theory]
        [InlineData("7707083894")]    // испорчена контрольная цифра
        [InlineData("500100732258")]  // испорчена 12-я контрольная
        [InlineData("12345")]         // неверная длина
        [InlineData("")]              // пустая строка
        [InlineData(null)]            // null
        public void ValidateINN_Invalid_ReturnsFalse(string inn)
        {
            InvokeStaticBool("ValidateINN", inn).Should().BeFalse();
        }

        #endregion

        // =====================================================================
        // Реестр встроенных паттернов
        // =====================================================================

        #region BuiltinPatterns

        [Fact]
        public void BuiltinPatterns_Registry_ContainsAllEntityTypes()
        {
            var patterns = (IList)GetStaticField("BuiltinPatterns");

            patterns.Should().NotBeNull();
            patterns.Count.Should().BeGreaterOrEqualTo(11);

            var typeNames = new System.Collections.Generic.HashSet<string>();
            foreach (var pattern in patterns)
                typeNames.Add((string)GetPatternProperty(pattern, "TypeName"));

            typeNames.Should().Contain(
                new[] { "Email", "URL", "GUID", "IP", "Phone", "Date", "Money", "SNILS", "INN", "Passport", "CarNumber" });
        }

        [Theory]
        [InlineData("Email", "Контакты: user.name@example.com, звоните")]
        [InlineData("URL", "Документация на https://example.com/docs?page=1")]
        [InlineData("GUID", "идентификатор 01234567-89ab-cdef-0123-456789abcdef найден")]
        [InlineData("IP", "сервер 192.168.1.100 доступен")]
        [InlineData("Phone", "телефон +7 (912) 345-67-89 для связи")]
        [InlineData("Date", "договор от 15.03.2026 расторгнут")]
        [InlineData("Money", "сумма 14500,50 руб. оплачена")]
        [InlineData("SNILS", "СНИЛС 112-233-445 95")]
        [InlineData("INN", "ИНН: 7707083893")]
        [InlineData("Passport", "паспорт 4605 123456")]
        [InlineData("CarNumber", "авто А123ВС77")]
        public void BuiltinPatterns_Regex_MatchesKnownSample(string typeName, string sample)
        {
            var regex = FindPatternRegex(typeName);

            regex.IsMatch(sample).Should().BeTrue($"паттерн '{typeName}' должен находить сущность в тексте");
        }

        [Theory]
        [InlineData("Email", "нет собаки здесь")]
        [InlineData("IP", "адрес 999.999.999.999 вне диапазона октета")]
        public void BuiltinPatterns_Regex_DoesNotMatchInvalidSample(string typeName, string sample)
        {
            var regex = FindPatternRegex(typeName);

            regex.IsMatch(sample).Should().BeFalse();
        }

        [Fact]
        public void PatternByType_ContainsUpperCaseKeyForEveryBuiltinPattern()
        {
            var patterns = (IList)GetStaticField("BuiltinPatterns");
            var patternByType = (IDictionary)GetStaticField("PatternByType");

            patternByType.Should().NotBeNull();
            patternByType.Count.Should().Be(patterns.Count);

            foreach (var pattern in patterns)
            {
                var typeName = (string)GetPatternProperty(pattern, "TypeName");
                patternByType.Contains(typeName.ToUpperInvariant()).Should().BeTrue(
                    $"PatternByType должен содержать ключ {typeName.ToUpperInvariant()}");
            }
        }

        #endregion

        // =====================================================================
        // Вспомогательные методы (отражение)
        // =====================================================================

        /// <summary>Вызывает приватный статический метод Back-класса, возвращающий строку.</summary>
        private static string InvokeStaticString(string methodName, string arg)
        {
            return (string)BackType
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { arg });
        }

        /// <summary>Вызывает приватный статический метод Back-класса, возвращающий bool.</summary>
        private static bool InvokeStaticBool(string methodName, string arg)
        {
            return (bool)BackType
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { arg });
        }

        private static object GetStaticField(string fieldName)
        {
            return BackType
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);
        }

        private static object GetPatternProperty(object pattern, string propertyName)
        {
            return pattern.GetType().GetProperty(propertyName).GetValue(pattern);
        }

        /// <summary>Находит Regex нужного типа сущности в реестре BuiltinPatterns.</summary>
        private static System.Text.RegularExpressions.Regex FindPatternRegex(string typeName)
        {
            var patterns = (IList)GetStaticField("BuiltinPatterns");
            foreach (var pattern in patterns)
            {
                if (string.Equals((string)GetPatternProperty(pattern, "TypeName"), typeName, StringComparison.OrdinalIgnoreCase))
                    return (System.Text.RegularExpressions.Regex)GetPatternProperty(pattern, "Pattern");
            }

            throw new InvalidOperationException($"Паттерн '{typeName}' не найден в BuiltinPatterns");
        }
    }
}
