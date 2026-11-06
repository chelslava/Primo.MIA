using FluentAssertions;
using Primo.MIA;
using System;
using System.Reflection;
using Xunit;

namespace Primo.MIA.Tests.HttpWeb
{
    /// <summary>
    /// Тесты чистой логики OAuth2-активностей (issue #16):
    /// парсинг JSON-ответа токена (ParseTokenResponse) и строковое
    /// представление Grant Type (GetGrantTypeString).
    ///
    /// Валидация плейсхолдера Client Secret уже покрыта в
    /// <see cref="Primo.MIA.Tests.OAuth2SecretValidationTests"/> и здесь не дублируется.
    ///
    /// Методы приватные, а конструкторы Back-классов требуют реальный
    /// IWFContainer от Primo Studio (см. CalendarLoadTests), поэтому:
    ///   - экземпляры создаются через FormatterServices.GetUninitializedObject
    ///     (без вызова конструктора);
    ///   - приватные методы вызываются через отражение;
    ///   - поле _propGrantType заполняется напрямую (сеттер свойства
    ///     дергает InvokePropertyChanged из SDK).
    /// Тестируемые методы не ходят в сеть и не используют состояние SDK.
    /// </summary>
        [Trait("Category", "Fast")]
    public class HttpOAuth2Tests
    {
        // =====================================================================
        // ParseTokenResponse — HttpOAuth2TokenBack
        // =====================================================================

        #region ParseTokenResponse_Token

        [Fact]
        public void ParseTokenResponse_Token_ValidJson_ReturnsAllFields()
        {
            var result = InvokeTokenParse(
                "{\"access_token\":\"abc123\",\"refresh_token\":\"xyz789\"," +
                "\"expires_in\":3600,\"token_type\":\"Bearer\"}");

            GetProperty(result, "AccessToken").Should().Be("abc123");
            GetProperty(result, "RefreshToken").Should().Be("xyz789");
            GetProperty(result, "TokenType").Should().Be("Bearer");
            GetProperty(result, "ExpiresIn").Should().Be(3600);
        }

        [Fact]
        public void ParseTokenResponse_Token_MissingOptionalFields_ReturnsDefaults()
        {
            // Нет refresh_token и token_type — токен доступа не обязан их содержать
            var result = InvokeTokenParse("{\"access_token\":\"abc123\"}");

            GetProperty(result, "AccessToken").Should().Be("abc123");
            GetProperty(result, "RefreshToken").Should().BeNull();
            // token_type по умолчанию — Bearer
            GetProperty(result, "TokenType").Should().Be("Bearer");
            GetProperty(result, "ExpiresIn").Should().Be(0);
        }

        [Fact]
        public void ParseTokenResponse_Token_EmptyJson_ReturnsDefaults()
        {
            var result = InvokeTokenParse("{}");

            GetProperty(result, "AccessToken").Should().BeNull();
            GetProperty(result, "TokenType").Should().Be("Bearer");
            GetProperty(result, "ExpiresIn").Should().Be(0);
        }

        [Fact]
        public void ParseTokenResponse_Token_ExpiresInAsString_ParsesNumber()
        {
            // Некоторые провайдеры возвращают expires_in строкой
            var result = InvokeTokenParse("{\"access_token\":\"a\",\"expires_in\":\"7200\"}");

            GetProperty(result, "ExpiresIn").Should().Be(7200);
        }

        [Fact]
        public void ParseTokenResponse_Token_NonNumericExpiresIn_KeepsDefault()
        {
            var result = InvokeTokenParse("{\"access_token\":\"a\",\"expires_in\":\"soon\"}");

            GetProperty(result, "ExpiresIn").Should().Be(0);
        }

        [Theory]
        [InlineData("not a json at all")]
        [InlineData("{\"access_token\": }")]
        [InlineData("")]
        public void ParseTokenResponse_Token_InvalidJson_ThrowsInvalidOperationException(string json)
        {
            Action act = () => InvokeTokenParse(json);

            // Отражение оборачивает исключение в TargetInvocationException
            var inner = act.Should().Throw<TargetInvocationException>().Which.InnerException;
            inner.Should().BeOfType<InvalidOperationException>();
            inner.Message.Should().Contain("Ошибка парсинга ответа OAuth2");
        }

        #endregion

        // =====================================================================
        // ParseTokenResponse — HttpOAuth2RefreshBack
        // (мягкий вариант: при ошибке парсинга — дефолты, без исключения)
        // =====================================================================

        #region ParseTokenResponse_Refresh

        [Fact]
        public void ParseTokenResponse_Refresh_ValidJson_ReturnsFields()
        {
            var result = InvokeRefreshParse(
                "{\"access_token\":\"new-a\",\"refresh_token\":\"new-r\",\"expires_in\":1800}");

            GetProperty(result, "AccessToken").Should().Be("new-a");
            GetProperty(result, "RefreshToken").Should().Be("new-r");
            GetProperty(result, "ExpiresIn").Should().Be(1800);
        }

        [Fact]
        public void ParseTokenResponse_Refresh_MissingRefreshToken_ReturnsNull()
        {
            // Многие провайдеры не перевыпускают refresh_token при refresh
            var result = InvokeRefreshParse("{\"access_token\":\"new-a\",\"expires_in\":600}");

            GetProperty(result, "AccessToken").Should().Be("new-a");
            GetProperty(result, "RefreshToken").Should().BeNull();
            GetProperty(result, "ExpiresIn").Should().Be(600);
        }

        [Theory]
        [InlineData("garbage")]
        [InlineData("")]
        [InlineData("null")]
        public void ParseTokenResponse_Refresh_InvalidJson_ReturnsDefaults(string json)
        {
            // В отличие от Token-активности, здесь ошибка парсинга проглатывается
            var result = InvokeRefreshParse(json);

            GetProperty(result, "AccessToken").Should().BeNull();
            GetProperty(result, "RefreshToken").Should().BeNull();
            GetProperty(result, "ExpiresIn").Should().Be(0);
        }

        [Fact]
        public void ParseTokenResponse_Refresh_ExpiresInAsString_ParsesNumber()
        {
            var result = InvokeRefreshParse("{\"access_token\":\"a\",\"expires_in\":\"120\"}");

            GetProperty(result, "ExpiresIn").Should().Be(120);
        }

        #endregion

        // =====================================================================
        // GetGrantTypeString — HttpOAuth2TokenBack
        // =====================================================================

        #region GetGrantTypeString

        [Theory]
        [InlineData(OAuth2GrantType.ClientCredentials, "client_credentials")]
        [InlineData(OAuth2GrantType.Password, "password")]
        [InlineData(OAuth2GrantType.AuthorizationCode, "authorization_code")]
        public void GetGrantTypeString_KnownGrantTypes_ReturnsWireFormat(OAuth2GrantType grantType, string expected)
        {
            var back = CreateUninitializedTokenBack(grantType);

            var result = InvokePrivate(back, "GetGrantTypeString");

            result.Should().Be(expected);
        }

        [Fact]
        public void GetGrantTypeString_UndefinedGrantType_FallsBackToClientCredentials()
        {
            // default-ветка switch: неизвестное значение enum -> client_credentials
            var back = CreateUninitializedTokenBack((OAuth2GrantType)999);

            var result = InvokePrivate(back, "GetGrantTypeString");

            result.Should().Be("client_credentials");
        }

        #endregion

        // =====================================================================
        // Вспомогательные методы (отражение)
        // =====================================================================

        /// <summary>
        /// Создаёт экземпляр HttpOAuth2TokenBack без вызова конструктора
        /// (конструктор требует реальный IWFContainer) и проставляет Grant Type.
        /// </summary>
        private static HttpOAuth2TokenBack CreateUninitializedTokenBack(OAuth2GrantType grantType)
        {
            var back = (HttpOAuth2TokenBack)System.Runtime.Serialization.FormatterServices
                .GetUninitializedObject(typeof(HttpOAuth2TokenBack));

            // Пишем напрямую в поле: сеттер свойства вызывает InvokePropertyChanged из SDK
            typeof(HttpOAuth2TokenBack)
                .GetField("_propGrantType", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(back, grantType);

            return back;
        }

        private static object InvokeTokenParse(string json)
        {
            return typeof(HttpOAuth2TokenBack)
                .GetMethod("ParseTokenResponse", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(CreateUninitializedTokenBack(OAuth2GrantType.ClientCredentials), new object[] { json });
        }

        private static object InvokeRefreshParse(string json)
        {
            var back = System.Runtime.Serialization.FormatterServices
                .GetUninitializedObject(typeof(HttpOAuth2RefreshBack));

            return typeof(HttpOAuth2RefreshBack)
                .GetMethod("ParseTokenResponse", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(back, new object[] { json });
        }

        private static object InvokePrivate(object target, string methodName)
        {
            return target.GetType()
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(target, new object[0]);
        }

        private static object GetProperty(object target, string propertyName)
        {
            return target.GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                .GetValue(target);
        }
    }
}
