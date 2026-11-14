using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.HttpWeb
{
    /// <summary>
    /// Тесты WebhookServerCore (issue #23): payload-шаблоны, retry-политика,
    /// нормализация префиксов, разбор методов/заголовков/query, проверка
    /// секретного токена. Только чистая логика — порты и сеть не нужны.
    /// </summary>
    [Trait("Category", "Fast")]
    public class WebhookServerCoreTests
    {
        #region Payload: приоритет и шаблоны

        [Fact(DisplayName = "Payload: Template приоритетнее Payload")]
        public void ResolvePayload_TemplateWins()
        {
            WebhookServerCore.ResolvePayload("{\"t\":1}", "{\"p\":2}")
                .Should().Be("{\"t\":1}");
        }

        [Fact(DisplayName = "Payload: без шаблона возвращается Payload")]
        public void ResolvePayload_PayloadOnly()
        {
            WebhookServerCore.ResolvePayload(null, "{\"p\":2}")
                .Should().Be("{\"p\":2}");
        }

        [Fact(DisplayName = "Payload: оба пустые — null")]
        public void ResolvePayload_BothEmpty()
        {
            WebhookServerCore.ResolvePayload("", "   ").Should().BeNull();
            WebhookServerCore.ResolvePayload(null, null).Should().BeNull();
        }

        [Fact(DisplayName = "Шаблон: {{ключ}} подставляется")]
        public void ApplyTemplate_Substitutes()
        {
            var vars = new Dictionary<string, string> { ["Message"] = "Привет" };
            WebhookServerCore.ApplyTemplate("{\"text\": \"{{Message}}\"}", vars)
                .Should().Be("{\"text\": \"Привет\"}");
        }

        [Fact(DisplayName = "Шаблон: ключ без учёта регистра")]
        public void ApplyTemplate_CaseInsensitive()
        {
            var vars = new Dictionary<string, string> { ["message"] = "привет" };
            WebhookServerCore.ApplyTemplate("{{MESSAGE}}", vars)
                .Should().Be("привет");
        }

        [Fact(DisplayName = "Шаблон: пробелы внутри скобок игнорируются")]
        public void ApplyTemplate_TrimsKey()
        {
            var vars = new Dictionary<string, string> { ["Key"] = "v" };
            WebhookServerCore.ApplyTemplate("{{ Key }}", vars)
                .Should().Be("v");
        }

        [Fact(DisplayName = "Шаблон: не найденный плейсхолдер остаётся")]
        public void ApplyTemplate_UnknownKey_KeptAsIs()
        {
            var vars = new Dictionary<string, string> { ["A"] = "1" };
            WebhookServerCore.ApplyTemplate("{{A}} {{B}}", vars)
                .Should().Be("1 {{B}}");
        }

        [Fact(DisplayName = "Шаблон: пустой/null словарь — шаблон как есть")]
        public void ApplyTemplate_NoVars()
        {
            var template = "static {{X}}";
            WebhookServerCore.ApplyTemplate(template, null).Should().Be(template);
            WebhookServerCore.ApplyTemplate(template, new Dictionary<string, string>()).Should().Be(template);
        }

        [Fact(DisplayName = "Шаблон: несколько вхождений подставляются")]
        public void ApplyTemplate_MultiplePlaceholders()
        {
            var vars = new Dictionary<string, string> { ["X"] = "7" };
            WebhookServerCore.ApplyTemplate("{{X}}-{{X}}", vars)
                .Should().Be("7-7");
        }

        #endregion

        #region Retry-политика

        [Theory(DisplayName = "Retry: 2xx — успех")]
        [InlineData(200)]
        [InlineData(299)]
        public void IsSuccessStatusCode_2xx(int code)
        {
            WebhookServerCore.IsSuccessStatusCode(code).Should().BeTrue();
        }

        [Theory(DisplayName = "Retry: не-2xx — не успех")]
        [InlineData(199)]
        [InlineData(300)]
        [InlineData(404)]
        [InlineData(500)]
        public void IsSuccessStatusCode_Not2xx(int code)
        {
            WebhookServerCore.IsSuccessStatusCode(code).Should().BeFalse();
        }

        [Fact(DisplayName = "Retry: 5xx повторяется, 4xx и успех — нет")]
        public void ShouldRetry_StatusCodes()
        {
            WebhookServerCore.ShouldRetry(500, false, 1, 3).Should().BeTrue();
            WebhookServerCore.ShouldRetry(503, false, 2, 3).Should().BeTrue();
            WebhookServerCore.ShouldRetry(404, false, 1, 3).Should().BeFalse();
            WebhookServerCore.ShouldRetry(200, false, 1, 3).Should().BeFalse();
        }

        [Fact(DisplayName = "Retry: последняя попытка — не повторяем")]
        public void ShouldRetry_LastAttempt()
        {
            WebhookServerCore.ShouldRetry(500, false, 3, 3).Should().BeFalse();
            WebhookServerCore.ShouldRetry(500, true, 3, 3).Should().BeFalse();
        }

        [Fact(DisplayName = "Retry: сетевое исключение повторяется")]
        public void ShouldRetry_Exception()
        {
            WebhookServerCore.ShouldRetry(0, true, 1, 3).Should().BeTrue();
            WebhookServerCore.ShouldRetry(0, true, 2, 3).Should().BeTrue();
        }

        [Fact(DisplayName = "Retry: statusCode 0 без исключения (таймаут) — повтор")]
        public void ShouldRetry_Timeout()
        {
            WebhookServerCore.ShouldRetry(0, false, 1, 2).Should().BeTrue();
        }

        #endregion

        #region Валидация отправки и сообщение результата

        [Fact(DisplayName = "Валидация отправки: пустой URL и отсутствие payload")]
        public void ValidateSendInput_Errors()
        {
            WebhookServerCore.ValidateSendInput("", "{\"p\":1}", null)
                .Should().ContainSingle(e => e.Contains("URL"));
            WebhookServerCore.ValidateSendInput("http://x", "", null)
                .Should().ContainSingle(e => e.Contains("Payload или Template"));
            WebhookServerCore.ValidateSendInput(null, null, null)
                .Should().HaveCount(2);
        }

        [Fact(DisplayName = "Валидация отправки: корректный ввод — без ошибок")]
        public void ValidateSendInput_Valid()
        {
            WebhookServerCore.ValidateSendInput("http://x", "{}", null).Should().BeEmpty();
            WebhookServerCore.ValidateSendInput("http://x", null, "{{T}}").Should().BeEmpty();
        }

        [Fact(DisplayName = "Сообщение результата: успех/ошибка, попытки")]
        public void BuildResultMessage()
        {
            WebhookServerCore.BuildResultMessage(200, true, 1)
                .Should().Be("Вебхук отправлен → 200");
            WebhookServerCore.BuildResultMessage(500, false, 3)
                .Should().Be("Вебхук вернул 500, попыток: 3");
        }

        #endregion

        #region Listener: префикс, методы, код

        [Fact(DisplayName = "Префикс: добавляется завершающий слеш")]
        public void NormalizePrefix_AddsSlash()
        {
            WebhookServerCore.NormalizePrefix("http://localhost:8080/hook")
                .Should().Be("http://localhost:8080/hook/");
        }

        [Fact(DisplayName = "Префикс: слеш не дублируется")]
        public void NormalizePrefix_NoDoubleSlash()
        {
            WebhookServerCore.NormalizePrefix("http://localhost:8080/hook/")
                .Should().Be("http://localhost:8080/hook/");
        }

        [Fact(DisplayName = "Префикс: null/пустой возвращается как есть")]
        public void NormalizePrefix_Empty()
        {
            WebhookServerCore.NormalizePrefix(null).Should().BeNull();
            WebhookServerCore.NormalizePrefix("  ").Should().Be("  ");
        }

        [Fact(DisplayName = "Методы: CSV + пробелы + регистр")]
        public void ParseAllowedMethods()
        {
            var methods = WebhookServerCore.ParseAllowedMethods("get, post;DELETE  put");
            methods.Should().BeEquivalentTo("GET", "POST", "DELETE", "PUT");
            methods.Contains("get").Should().BeTrue("сравнение регистронезависимое");
        }

        [Fact(DisplayName = "Методы: пустая строка — пустое множество")]
        public void ParseAllowedMethods_Empty()
        {
            WebhookServerCore.ParseAllowedMethods("").Should().BeEmpty();
            WebhookServerCore.ParseAllowedMethods(null).Should().BeEmpty();
            WebhookServerCore.ParseAllowedMethods("  ,  ;  ").Should().BeEmpty();
        }

        [Fact(DisplayName = "Код ответа: 0/отрицательный → 200")]
        public void ResolveResponseCode()
        {
            WebhookServerCore.ResolveResponseCode(0).Should().Be(200);
            WebhookServerCore.ResolveResponseCode(-1).Should().Be(200);
            WebhookServerCore.ResolveResponseCode(204).Should().Be(204);
        }

        #endregion

        #region Токен

        [Fact(DisplayName = "Токен: пустой ожидаемый — проверка отключена")]
        public void IsTokenValid_EmptySecret_AlwaysValid()
        {
            WebhookServerCore.IsTokenValid(null, null).Should().BeTrue();
            WebhookServerCore.IsTokenValid(null, "").Should().BeTrue();
        }

        [Fact(DisplayName = "Токен: X-Webhook-Secret совпадает")]
        public void IsTokenValid_SecretHeader()
        {
            var headers = new Dictionary<string, string> { ["X-Webhook-Secret"] = "abc" };
            WebhookServerCore.IsTokenValid(headers, "abc").Should().BeTrue();
        }

        [Fact(DisplayName = "Токен: fallback на Authorization")]
        public void IsTokenValid_AuthorizationFallback()
        {
            var headers = new Dictionary<string, string> { ["Authorization"] = "Bearer abc" };
            WebhookServerCore.IsTokenValid(headers, "Bearer abc").Should().BeTrue();
        }

        [Fact(DisplayName = "Токен: X-Hub-Signature-256 проверяется")]
        public void IsTokenValid_HubSignature()
        {
            var headers = new Dictionary<string, string> { ["X-Hub-Signature-256"] = "sha256=xyz" };
            WebhookServerCore.IsTokenValid(headers, "sha256=xyz").Should().BeTrue();
        }

        [Fact(DisplayName = "Токен: несовпадение или отсутствие заголовка — невалидно")]
        public void IsTokenValid_Invalid()
        {
            new Dictionary<string, string> { ["X-Webhook-Secret"] = "другой" }
                .Apply(h => WebhookServerCore.IsTokenValid(h, "abc").Should().BeFalse());
            new Dictionary<string, string> { ["X-Other"] = "abc" }
                .Apply(h => WebhookServerCore.IsTokenValid(h, "abc").Should().BeFalse());
            WebhookServerCore.IsTokenValid(null, "abc").Should().BeFalse();
        }

        [Fact(DisplayName = "Токен: регистр заголовка не важен (Dictionary.OrdinalIgnoreCase)")]
        public void IsTokenValid_HeaderCaseInsensitive()
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["x-webhook-secret"] = "abc"
            };
            WebhookServerCore.IsTokenValid(headers, "abc").Should().BeTrue();
        }

        #endregion

        #region Разбор NameValueCollection

        [Fact(DisplayName = "Разбор: заголовки/query → словарь, null-ключи отбрасываются")]
        public void ParseNameValues()
        {
            var source = new NameValueCollection
            {
                { "Content-Type", "application/json" },
                { "X-Empty", null }
            };
            source["X-Empty"] = string.Empty;

            var parsed = WebhookServerCore.ParseNameValues(source);

            parsed.Should().HaveCount(2);
            parsed["Content-Type"].Should().Be("application/json");
            parsed["X-Empty"].Should().Be(string.Empty);
            parsed.ContainsKey("content-type").Should().BeTrue("регистр ключей игнорируется");
        }

        [Fact(DisplayName = "Разбор: null-источник — пустой словарь")]
        public void ParseNameValues_Null()
        {
            WebhookServerCore.ParseNameValues(null).Should().BeEmpty();
        }

        [Fact(DisplayName = "Сообщение приёма: метод/адрес/объём/токен")]
        public void BuildReceiveMessage()
        {
            WebhookServerCore.BuildReceiveMessage("GET", "127.0.0.1:5000", 10, true)
                .Should().Be("Получен GET от 127.0.0.1:5000, тело: 10 байт");
            WebhookServerCore.BuildReceiveMessage("POST", "1.2.3.4:80", 0, false)
                .Should().Contain("⚠ токен не совпал");
        }

        #endregion
    }

    internal static class DictionaryTestExtensions
    {
        public static void Apply<T>(this T value, Action<T> action) => action(value);
    }
}
