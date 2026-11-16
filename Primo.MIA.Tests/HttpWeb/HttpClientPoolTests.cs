using System;
using System.Net.Http;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.HttpWeb
{
    /// <summary>
    /// Смоук-тесты HttpClientPool (issue #39): создание клиента из пула
    /// и повторное получение — без сети, только поведение фабрики.
    /// </summary>
    [Trait("Category", "Fast")]
    public class HttpClientPoolTests
    {
        #region Фабрика

        [Fact(DisplayName = "CreateClient: таймаут применяется к клиенту")]
        public void CreateClient_AppliesTimeout()
        {
            var timeout = TimeSpan.FromSeconds(7);

            using var client = HttpClientPool.CreateClient(timeout);
            client.Timeout.Should().Be(timeout);
        }

        [Fact(DisplayName = "CreateClient: повторный вызов с той же конфигурацией не бросает")]
        public void CreateClient_RepeatedCallsAreSafe()
        {
            var timeout = TimeSpan.FromSeconds(5);

            using (var first = HttpClientPool.CreateClient(false, null, null, false, timeout))
            {
                first.Timeout.Should().Be(timeout);
            }

            using (var second = HttpClientPool.CreateClient(false, null, null, false, timeout))
            {
                second.Timeout.Should().Be(timeout);
            }
        }

        [Fact(DisplayName = "CreateClient: ignoreSslErrors — scoped-путь, клиент создаётся")]
        public void CreateClient_IgnoreSslErrorsCreatesScopedClient()
        {
            var timeout = TimeSpan.FromSeconds(5);

            using var client = HttpClientPool.CreateClient(false, null, null, true, timeout);
            client.Timeout.Should().Be(timeout);
        }

        #endregion
    }
}
