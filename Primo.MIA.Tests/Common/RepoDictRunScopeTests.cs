using System;
using System.Collections.Generic;
using System.Security;
using FluentAssertions;
using Moq;
using OpenQA.Selenium;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    /// <summary>
    /// Тесты RepoDict.ClearRunScope / BrowserSessionRegistry.CloseAll
    /// (issue #27): очистка пользовательских словарей между прогонами,
    /// закрытие браузерных сессий, безопасность «нескольких контейнеров».
    /// Коллекция BrowserSessionStack — RepoDict глобален, параллель с
    /// другими классами, мутирующими его, запрещена.
    /// </summary>
    [Trait("Category", "Fast")]
    [Collection("BrowserSessionStack")]
    public class RepoDictRunScopeTests : IDisposable
    {
        public RepoDictRunScopeTests()
        {
            // Чистое состояние перед каждым тестом
            RepoDict.Clear();
        }

        public void Dispose()
        {
            RepoDict.Clear();
        }

        private Mock<IWebDriver> RegisterFakeSession(string sessionId)
        {
            var driver = new Mock<IWebDriver>();
            RepoDict.ObjectDict[sessionId] = driver.Object;
            BrowserSessionRegistry.Register(sessionId);
            return driver;
        }

        #region Словари пользовательских данных

        [Fact(DisplayName = "ClearRunScope очищает TableDict/ObjectDict/строки")]
        public void ClearRunScope_ClearsUserData()
        {
            var table = new System.Data.DataTable();
            RepoDict.TableDict["t1"] = table;
            RepoDict.ObjectDict["obj1"] = new object();
            RepoDict.Set("s1", "значение");
            RepoDict.IntDict["i1"] = 42;

            RepoDict.ClearRunScope();

            RepoDict.TableDict.Should().BeEmpty("таблицы не должны переживать прогон");
            RepoDict.ObjectDict.Should().BeEmpty("объекты не должны переживать прогон");
            RepoDict.Get<string>("s1").Should().BeNull();
            RepoDict.IntDict.Should().BeEmpty();
        }

        [Fact(DisplayName = "ClearRunScope очищает стек контекстов браузера (несколько контейнеров)")]
        public void ClearRunScope_ClearsSessionContextStack()
        {
            // Имитация вложенных/чередующихся контейнеров
            BrowserSessionContext.Push("session-outer");
            BrowserSessionContext.Push("session-inner");
            BrowserSessionContext.Current.Should().Be("session-inner");

            RepoDict.ClearRunScope();

            BrowserSessionContext.Current.Should().BeNull(
                "стек контекстов живёт в ObjectDict и уходит вместе с ним");

            // Новый прогон: контейнеры работают заново
            BrowserSessionContext.Push("new-run");
            BrowserSessionContext.Current.Should().Be("new-run");
        }

        [Fact(DisplayName = "ClearRunScope снимает пароли SecDict и диспоузит SecureString")]
        public void ClearRunScope_DisposesSecureStrings()
        {
            var secure = new SecureString();
            foreach (var c in "pwd") secure.AppendChar(c);
            secure.MakeReadOnly();
            RepoDict.SecDict["db-password"] = secure;

            RepoDict.ClearRunScope();

            RepoDict.SecDict.Should().BeEmpty();
            Action read = () => secure.Length.ToString();
            read.Should().Throw<ObjectDisposedException>(
                "Clear() диспоузит SecureString — пароль не читается после очистки");
        }

        [Fact(DisplayName = "ClearRunScope сохраняет системные PathDict/RegexDict")]
        public void ClearRunScope_KeepsSystemDictionaries()
        {
            var pathCountBefore = RepoDict.PathDict.Count;
            var regexCountBefore = RepoDict.RegexDict.Count;
            pathCountBefore.Should().BeGreaterThan(0);
            regexCountBefore.Should().BeGreaterThan(0);

            RepoDict.ClearRunScope();

            RepoDict.PathDict.Count.Should().Be(pathCountBefore, "пути — системные");
            RepoDict.RegexDict.Count.Should().Be(regexCountBefore, "regex — системные");
        }

        [Fact(DisplayName = "ClearRunScope на пустом состоянии — безопасен, возвращает 0")]
        public void ClearRunScope_EmptyState_ReturnsZero()
        {
            Action act = () => RepoDict.ClearRunScope().Should().Be(0);
            act.Should().NotThrow();
        }

        #endregion

        #region BrowserSessionRegistry.CloseAll

        [Fact(DisplayName = "CloseAll закрывает все сессии: Quit+Dispose, снятие с учёта")]
        public void CloseAll_QuitsAndDisposesAllSessions()
        {
            var first = RegisterFakeSession("run-a");
            var second = RegisterFakeSession("run-b");

            var closed = BrowserSessionRegistry.CloseAll("тест");

            closed.Should().BeEquivalentTo("run-a", "run-b");
            BrowserSessionRegistry.Count.Should().Be(0);
            RepoDict.Get<IWebDriver>("run-a").Should().BeNull();
            RepoDict.Get<IWebDriver>("run-b").Should().BeNull();
            // Стек контекстов CloseAll не трогает — его чистит RepoDict.Clear()
            // внутри ClearRunScope (см. ClearRunScope_ClearsSessionContextStack)

            first.Verify(d => d.Quit(), Times.Once);
            first.Verify(d => d.Dispose(), Times.Once);
            second.Verify(d => d.Quit(), Times.Once);
            second.Verify(d => d.Dispose(), Times.Once);
        }

        [Fact(DisplayName = "CloseAll: мёртвый драйвер не роняет очистку")]
        public void CloseAll_DeadDriver_DoesNotThrow()
        {
            var dead = RegisterFakeSession("dead-session");
            dead.Setup(d => d.Quit()).Throws(new InvalidOperationException("драйвер умер"));

            Action act = () => BrowserSessionRegistry.CloseAll("тест");

            act.Should().NotThrow("Quit/Dispose глушатся SafeDispose.Quiet");
            BrowserSessionRegistry.Count.Should().Be(0);
            dead.Verify(d => d.Dispose(), Times.Once,
                "Dispose вызывается даже при упавшем Quit");
        }

        [Fact(DisplayName = "CloseAll на пустом учёте — безопасен")]
        public void CloseAll_EmptyRegistry()
        {
            Action act = () => BrowserSessionRegistry.CloseAll("тест").Should().BeEmpty();
            act.Should().NotThrow();
        }

        [Fact(DisplayName = "ClearRunScope закрывает зарегистрированные сессии")]
        public void ClearRunScope_ClosesRegisteredSessions()
        {
            var driver = RegisterFakeSession("scope-session");
            BrowserSessionContext.Push("scope-session");

            RepoDict.ClearRunScope().Should().Be(1);
            BrowserSessionRegistry.Count.Should().Be(0);
            driver.Verify(d => d.Quit(), Times.Once);
        }

        #endregion
    }
}
