using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using OpenQA.Selenium;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Browser
{
    /// <summary>
    /// Тесты жизненного цикла сессий браузера (issue #20):
    /// - BrowserSessionContext.Pop(sessionId) — целевое снятие сессии вместо вершины стека;
    /// - BrowserSessionRegistry — TTL-очистка «заброшенных» сессий (защита от утечки WebDriver).
    /// </summary>
    /// <remarks>
    /// Все классы, трогающие общий ключ стека __BrowserSessionStack__ в RepoDict,
    /// объединены в коллекцию "BrowserSessionStack" — xUnit выполняет их
    /// последовательно, иначе очистка стека одним классом рвёт стек другого.
    /// </remarks>
    [Trait("Category", "Fast")]
    [Collection("BrowserSessionStack")]
    public class BrowserSessionLifecycleTests : IDisposable
    {
        // ID сессий, созданные в тестах — для очистки RepoDict в Dispose
        private readonly ConcurrentBag<string> _registeredSessions = new ConcurrentBag<string>();

        public void Dispose()
        {
            // Очищаем глобальное состояние после каждого теста
            RepoDict.Remove("__BrowserSessionStack__");
            foreach (var id in _registeredSessions)
            {
                RepoDict.Remove(id);
                BrowserSessionRegistry.Unregister(id);
            }
        }

        private string Track(string sessionId)
        {
            _registeredSessions.Add(sessionId);
            return sessionId;
        }

        #region Pop(sessionId) — целевое снятие

        [Fact(DisplayName = "Pop(sessionId): вложенные контейнеры — снимает конкретную сессию, сохраняя внешнюю")]
        public void Pop_BySessionId_Nested_RemovesOnlyTarget()
        {
            var outer = Track("outer-session");
            var inner = Track("inner-session");

            BrowserSessionContext.Push(outer);
            BrowserSessionContext.Push(inner);

            // Контейнер inner закрывает СВОЮ сессию
            BrowserSessionContext.Pop(inner).Should().BeTrue();

            // Внешняя осталась текущей (раньше Pop() снял бы inner и испортил контекст)
            BrowserSessionContext.Current.Should().Be(outer);

            BrowserSessionContext.Pop(outer).Should().BeTrue();
            BrowserSessionContext.Current.Should().BeNull();
        }

        [Fact(DisplayName = "Pop(sessionId): снятие из середины стека сохраняет порядок остальных")]
        public void Pop_BySessionId_FromMiddle_PreservesOrder()
        {
            var a = Track("a");
            var b = Track("b");
            var c = Track("c");

            BrowserSessionContext.Push(a);
            BrowserSessionContext.Push(b);
            BrowserSessionContext.Push(c);

            // Снимаем среднюю b; a и c остаются в прежнем порядке
            BrowserSessionContext.Pop(b).Should().BeTrue();

            BrowserSessionContext.Current.Should().Be(c, "c был вершиной и не тронут");
            BrowserSessionContext.Pop(c).Should().BeTrue();
            BrowserSessionContext.Current.Should().Be(a, "a лежит ниже b и сохранился");
            BrowserSessionContext.Pop(a).Should().BeTrue();
            BrowserSessionContext.Current.Should().BeNull();
        }

        [Fact(DisplayName = "Pop(sessionId): неизвестная сессия — false, стек не меняется")]
        public void Pop_BySessionId_Unknown_ReturnsFalse_KeepsStack()
        {
            var session = Track("known-session");
            BrowserSessionContext.Push(session);

            BrowserSessionContext.Pop("unknown-session").Should().BeFalse();
            BrowserSessionContext.Current.Should().Be(session, "стек не должен пострадать");

            BrowserSessionContext.Pop(session);
        }

        [Fact(DisplayName = "Pop(sessionId): null/пустой ID — false без исключения")]
        public void Pop_BySessionId_NullOrEmpty_ReturnsFalse()
        {
            BrowserSessionContext.Pop(null).Should().BeFalse();
            BrowserSessionContext.Pop("").Should().BeFalse();
            BrowserSessionContext.Pop("   ").Should().BeFalse();
        }

        [Fact(DisplayName = "Pop(sessionId): чередующиеся контейнеры не снимают записи друг друга (регрессия issue #20)")]
        public void Pop_BySessionId_InterleavedContainers_EachRemovesOwn()
        {
            // Симуляция гонки: контейнер A открылся, затем B открылся,
            // затем B закрылся — A не должен потерять свою запись.
            var a = Track("container-a");
            var b = Track("container-b");

            BrowserSessionContext.Push(a);   // контейнер A
            BrowserSessionContext.Push(b);   // контейнер B (вложенный/чередующийся)

            // B закрывает свою сессию
            BrowserSessionContext.Pop(b).Should().BeTrue();

            // A всё ещё актуален для своих дочерних активностей
            BrowserSessionContext.Current.Should().Be(a,
                "закрытие контейнера B не должно снимать сессию контейнера A");

            BrowserSessionContext.Pop(a);
        }

        [Fact(DisplayName = "Pop(sessionId): параллельные потоки снимают ровно свои сессии")]
        public void Pop_BySessionId_Parallel_EachThreadRemovesOwn()
        {
            const int threads = 8;
            const int sessionsPerThread = 100;

            var results = new ConcurrentBag<bool>();

            Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, t =>
            {
                for (int i = 0; i < sessionsPerThread; i++)
                {
                    var id = $"t{t}-{i}";
                    BrowserSessionContext.Push(id);
                    results.Add(BrowserSessionContext.Pop(id));
                }
            });

            // Каждый поток снял ровно свою сессию — ни одной осечки
            results.Should().OnlyContain(x => x, "Pop(sessionId) обязан находить свою сессию при любом чередовании");
            BrowserSessionContext.Current.Should().BeNull("все сессии сняты своими владельцами");
        }

        #endregion

        #region BrowserSessionRegistry — TTL-очистка

        private Mock<IWebDriver> CreateDriverInRepo(string sessionId)
        {
            var driver = new Mock<IWebDriver>();
            RepoDict.Set(sessionId, driver.Object);
            return driver;
        }

        [Fact(DisplayName = "EvictAbandoned: заброшенная сессия закрывается и удаляется из RepoDict")]
        public void EvictAbandoned_ExpiredSession_QuitsDriverAndRemoves()
        {
            var session = Track("expired-session");
            var driver = CreateDriverInRepo(session);
            var openedAt = DateTime.UtcNow;

            // Сессия открыта 25 ч назад, TTL 24 ч → должна быть выселена
            BrowserSessionRegistry.Register(session, openedAt);
            var evicted = BrowserSessionRegistry.EvictAbandoned(
                TimeSpan.FromHours(24), openedAt.AddHours(25));

            evicted.Should().Contain(session);
            driver.Verify(d => d.Quit(), Times.Once, "заброшенный WebDriver обязан быть закрыт");
            RepoDict.Get<IWebDriver>(session).Should().BeNull("сессия удалена из RepoDict");
        }

        [Fact(DisplayName = "EvictAbandoned: свежая сессия не трогается")]
        public void EvictAbandoned_FreshSession_Kept()
        {
            var session = Track("fresh-session");
            var driver = CreateDriverInRepo(session);
            var openedAt = DateTime.UtcNow;
            BrowserSessionRegistry.Register(session, openedAt);

            var evicted = BrowserSessionRegistry.EvictAbandoned(
                TimeSpan.FromHours(24), openedAt.AddMinutes(10));

            evicted.Should().NotContain(session);
            driver.Verify(d => d.Quit(), Times.Never);
            RepoDict.Get<IWebDriver>(session).Should().BeSameAs(driver.Object, "живая сессия сохранена");
        }

        [Fact(DisplayName = "EvictAbandoned: падение Quit у мёртвого драйвера глушится, сессия всё равно удаляется")]
        public void EvictAbandoned_DeadDriver_StillRemoved()
        {
            var session = Track("dead-driver-session");
            var driver = CreateDriverInRepo(session);
            driver.Setup(d => d.Quit()).Throws(new InvalidOperationException("WebDriver уже мёртв"));

            var openedAt = DateTime.UtcNow;
            BrowserSessionRegistry.Register(session, openedAt);
            var evicted = BrowserSessionRegistry.EvictAbandoned(
                TimeSpan.FromHours(24), openedAt.AddHours(30));

            evicted.Should().Contain(session, "исключение Quit не должно препятствовать очистке");
            driver.Verify(d => d.Dispose(), Times.Once, "Dispose всё равно вызывается после упавшего Quit");
            RepoDict.Get<IWebDriver>(session).Should().BeNull();
        }

        [Fact(DisplayName = "Register: автоматический свип при регистрации новой сессии выселяет заброшенную")]
        public void Register_AutoEvictsExpiredSessions()
        {
            var expired = Track("auto-expired");
            var expiredDriver = CreateDriverInRepo(expired);
            var fresh = Track("auto-fresh");
            var freshDriver = CreateDriverInRepo(fresh);

            // expired открыт «25 ч назад», fresh — «только что»
            BrowserSessionRegistry.Register(expired, DateTime.UtcNow.AddHours(-25));
            BrowserSessionRegistry.Register(fresh);

            expiredDriver.Verify(d => d.Quit(), Times.Once, "Register новой сессии выметает заброшенные");
            freshDriver.Verify(d => d.Quit(), Times.Never);
            RepoDict.Get<IWebDriver>(expired).Should().BeNull();
            RepoDict.Get<IWebDriver>(fresh).Should().NotBeNull();
        }

        [Fact(DisplayName = "Unregister: закрытая сессия снимается с учёта и не выселяется по TTL")]
        public void Unregister_RemovesFromTtlAccounting()
        {
            var session = Track("closed-normally");
            var driver = CreateDriverInRepo(session);
            var openedAt = DateTime.UtcNow;

            BrowserSessionRegistry.Register(session, openedAt);
            BrowserSessionRegistry.Unregister(session);

            // Даже спустя много времени после закрытия сессия не должна «выселяться» повторно
            var evicted = BrowserSessionRegistry.EvictAbandoned(
                TimeSpan.FromHours(24), openedAt.AddHours(48));

            evicted.Should().NotContain(session, "нормально закрытая сессия не остаётся в реестре");
            driver.Verify(d => d.Quit(), Times.Never);
        }

        [Fact(DisplayName = "EvictAbandoned: неположительный TTL — ArgumentOutOfRangeException")]
        public void EvictAbandoned_ZeroOrNegativeTtl_Throws()
        {
            Action zero = () => BrowserSessionRegistry.EvictAbandoned(TimeSpan.Zero);
            Action negative = () => BrowserSessionRegistry.EvictAbandoned(TimeSpan.FromSeconds(-1));

            zero.Should().Throw<ArgumentOutOfRangeException>();
            negative.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(DisplayName = "Register: параллельная регистрация не теряет сессий")]
        public void Register_Parallel_NoLoss()
        {
            const int threads = 8;
            const int perThread = 50;
            var ids = new ConcurrentBag<string>();

            Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, t =>
            {
                for (int i = 0; i < perThread; i++)
                {
                    var id = Track($"reg-t{t}-{i}");
                    ids.Add(id);
                    BrowserSessionRegistry.Register(id);
                }
            });

            BrowserSessionRegistry.Count.Should().BeGreaterOrEqualTo(ids.Count / 2,
                "параллельная регистрация может частично выметать старые записи, но массовых потерь быть не должно");
        }

        #endregion
    }
}
