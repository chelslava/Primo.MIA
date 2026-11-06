using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests
{
    /// <summary>
    /// Тесты валидации OAuth2 Client Secret (issue #11):
    /// незамещённый плейсхолдер «client_secret» не должен уходить в запрос токена.
    /// </summary>
    public class OAuth2SecretValidationTests
    {
        [Fact]
        [Trait("Category", "Fast")]
        public void IsOAuth2PlaceholderSecret_ExactPlaceholder_True()
        {
            HttpHelper.IsOAuth2PlaceholderSecret("client_secret").Should().BeTrue();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void IsOAuth2PlaceholderSecret_WithWhitespace_True()
        {
            HttpHelper.IsOAuth2PlaceholderSecret("  client_secret  ").Should().BeTrue();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void IsOAuth2PlaceholderSecret_CaseInsensitive_True()
        {
            HttpHelper.IsOAuth2PlaceholderSecret("Client_Secret").Should().BeTrue();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void IsOAuth2PlaceholderSecret_RealSecret_False()
        {
            HttpHelper.IsOAuth2PlaceholderSecret("s3cr3t-4a2b").Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void IsOAuth2PlaceholderSecret_EmptyOrNull_False()
        {
            // Пустое значение — легитимный кейс (публичные клиенты без секрета);
            // отсекается только плейсхолдер.
            HttpHelper.IsOAuth2PlaceholderSecret(null).Should().BeFalse();
            HttpHelper.IsOAuth2PlaceholderSecret("").Should().BeFalse();
            HttpHelper.IsOAuth2PlaceholderSecret("   ").Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void IsOAuth2PlaceholderSecret_SecretContainingWord_False()
        {
            // Секрет, содержащий подстроку, — не плейсхолдер
            HttpHelper.IsOAuth2PlaceholderSecret("my-client_secret-value").Should().BeFalse();
        }
    }
}
