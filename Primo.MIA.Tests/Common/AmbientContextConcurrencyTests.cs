using System;
using System.Threading.Tasks;
using FluentAssertions;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    /// <summary>
    /// Стресс-тесты конкурентности ambient-контекстов.
    /// Проверяют, что параллельные Push/Pop из разных потоков
    /// не искажают стек и не падают (FIX: стеки переведены на ConcurrentStack).
    /// </summary>
    [Trait("Category", "Fast")]
    public class AmbientContextConcurrencyTests
    {
        private static void DrainTransactionContext()
        {
            while (DatabaseTransactionContext.Current != null)
                DatabaseTransactionContext.Pop();
        }

        private static void DrainBrowserContext()
        {
            while (BrowserSessionContext.Current != null)
                BrowserSessionContext.Pop();
        }

        [Fact(DisplayName = "DatabaseTransactionContext: параллельный Push не теряет элементы")]
        public void ParallelPush_AllElementsPresent()
        {
            DrainTransactionContext();

            const int threads = 8;
            const int pushesPerThread = 200;

            Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, t =>
            {
                for (int i = 0; i < pushesPerThread; i++)
                    DatabaseTransactionContext.Push($"tx-{t}-{i}");
            });

            DatabaseTransactionContext.Current.Should().NotBeNull();

            // Все элементы доступны для снятия
            int popped = 0;
            while (DatabaseTransactionContext.Current != null)
            {
                DatabaseTransactionContext.Pop();
                popped++;
            }

            popped.Should().Be(threads * pushesPerThread,
                "каждый из {0} параллельных Push должен попасть в один и тот же стек", threads * pushesPerThread);
        }

        [Fact(DisplayName = "DatabaseTransactionContext: параллельные Push/Pop не падают")]
        public void ParallelPushPop_DoesNotThrow()
        {
            DrainTransactionContext();

            const int threads = 8;
            const int iterations = 300;

            Action act = () => Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, t =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    DatabaseTransactionContext.Push($"tx-{t}-{i}");
                    DatabaseTransactionContext.Current.Should().NotBeNull();
                    DatabaseTransactionContext.Pop();
                }
            });

            // TryPop на пустом стеке возвращает false, но не бросает исключений
            act.Should().NotThrow();
            DrainTransactionContext();
            DatabaseTransactionContext.Current.Should().BeNull();
        }

        [Fact(DisplayName = "BrowserSessionContext: параллельный Push не теряет элементы")]
        public void BrowserContext_ParallelPush_AllElementsPresent()
        {
            DrainBrowserContext();

            const int threads = 8;
            const int pushesPerThread = 200;

            Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, t =>
            {
                for (int i = 0; i < pushesPerThread; i++)
                    BrowserSessionContext.Push($"session-{t}-{i}");
            });

            int popped = 0;
            while (BrowserSessionContext.Current != null)
            {
                BrowserSessionContext.Pop();
                popped++;
            }

            popped.Should().Be(threads * pushesPerThread);
        }

        [Fact(DisplayName = "BrowserSessionContext: параллельные Push/Pop не падают")]
        public void BrowserContext_ParallelPushPop_DoesNotThrow()
        {
            DrainBrowserContext();

            const int threads = 8;
            const int iterations = 300;

            Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, t =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    BrowserSessionContext.Push($"session-{t}-{i}");
                    BrowserSessionContext.Current.Should().NotBeNull();
                    BrowserSessionContext.Pop();
                }
            });

            DrainBrowserContext();
            BrowserSessionContext.Current.Should().BeNull();
        }
    }
}
