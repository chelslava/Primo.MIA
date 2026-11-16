using System;
using System.Collections.Generic;
using FluentAssertions;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    /// <summary>
    /// Тесты Common/RetryPolicy (issue #43): попытки, предикаты повтора,
    /// fail-fast на неповторяемом исключении, экспоненциальный backoff.
    /// </summary>
    [Trait("Category", "Fast")]
    public class RetryPolicyTests
    {
        private sealed class NonRetryableException : Exception
        {
            public NonRetryableException(string message) : base(message) { }
        }

        #region Базовое выполнение

        [Fact(DisplayName = "Execute: успех с первой попытки")]
        public void Execute_SuccessFirstAttempt()
        {
            var outcome = RetryPolicy.Execute(
                attempt => attempt * 10,
                maxAttempts: 3,
                delayMs: 0);

            outcome.Attempts.Should().Be(1);
            outcome.HasResult.Should().BeTrue();
            outcome.LastResult.Should().Be(10);
            outcome.LastException.Should().BeNull();
        }

        [Fact(DisplayName = "Execute: максимальное число попыток ≥ 1")]
        public void Execute_MaxAttemptsFlooredToOne()
        {
            int calls = 0;
            var outcome = RetryPolicy.Execute(
                attempt => { calls++; return attempt; },
                maxAttempts: 0,
                delayMs: 0);

            calls.Should().Be(1);
            outcome.Attempts.Should().Be(1);
        }

        #endregion

        #region Повторы по исключению

        [Fact(DisplayName = "Execute: повтор по исключению до успеха")]
        public void Execute_RetriesUntilSuccess()
        {
            int calls = 0;

            var outcome = RetryPolicy.Execute<string>(
                attempt =>
                {
                    calls++;
                    if (calls < 3)
                        throw new InvalidOperationException($"попытка {calls}");
                    return "готово";
                },
                maxAttempts: 3,
                delayMs: 0);

            calls.Should().Be(3);
            outcome.Attempts.Should().Be(3);
            outcome.HasResult.Should().BeTrue();
            outcome.LastResult.Should().Be("готово");
            outcome.LastException.Should().BeNull();
        }

        [Fact(DisplayName = "Execute: исчерпание попыток — Outcome с последним исключением")]
        public void Execute_ExhaustionReturnsLastException()
        {
            var outcome = RetryPolicy.Execute<string>(
                attempt => throw new InvalidOperationException($"упала попытка {attempt}"),
                maxAttempts: 3,
                delayMs: 0);

            outcome.Attempts.Should().Be(3);
            outcome.HasResult.Should().BeFalse();
            outcome.LastException.Should().BeOfType<InvalidOperationException>();
            outcome.LastException.Message.Should().Contain("3");
        }

        [Fact(DisplayName = "Execute: неповторяемое исключение пробрасывается сразу")]
        public void Execute_NonRetryableRethrownImmediately()
        {
            int calls = 0;
            var expected = new NonRetryableException("не повторяем");

            Action act = () => RetryPolicy.Execute<string>(
                attempt => { calls++; throw expected; },
                maxAttempts: 5,
                shouldRetryException: (ex, attempt) => !(ex is NonRetryableException),
                delayMs: 0);

            act.Should().Throw<NonRetryableException>().Which.Should().BeSameAs(expected);
            calls.Should().Be(1);
        }

        #endregion

        #region Повторы по результату

        [Fact(DisplayName = "Execute: повтор по «плохому» результату (5xx-паттерн)")]
        public void Execute_RetriesByResult()
        {
            int calls = 0;

            var outcome = RetryPolicy.Execute(
                attempt => { calls++; return calls == 1 ? 500 : 200; },
                maxAttempts: 3,
                shouldRetryResult: (code, attempt) => code >= 500,
                delayMs: 0);

            calls.Should().Be(2);
            outcome.Attempts.Should().Be(2);
            outcome.HasResult.Should().BeTrue();
            outcome.LastResult.Should().Be(200);
            outcome.LastException.Should().BeNull();
        }

        [Fact(DisplayName = "Execute: результат сохраняется при последующем исключении")]
        public void Execute_ResultKeptWhenLaterAttemptThrows()
        {
            int calls = 0;

            var outcome = RetryPolicy.Execute(
                attempt =>
                {
                    calls++;
                    if (calls == 1) return 500;           // повторяемый результат
                    throw new TimeoutException("сеть легла"); // повторяемое исключение
                },
                maxAttempts: 2,
                shouldRetryResult: (code, attempt) => code >= 500,
                delayMs: 0);

            outcome.HasResult.Should().BeTrue();
            outcome.LastResult.Should().Be(500);
            outcome.LastException.Should().BeOfType<TimeoutException>();
        }

        #endregion

        #region Backoff

        [Fact(DisplayName = "Execute: экспоненциальная задержка 1×, 2×, 4×")]
        public void Execute_ExponentialBackoffDelays()
        {
            var delays = new List<int>();

            RetryPolicy.Execute<string>(
                attempt => throw new InvalidOperationException("всё падает"),
                maxAttempts: 4,
                delayMs: 10,
                exponentialBackoff: true,
                delayCallback: (attempt, delay) => delays.Add(delay));

            delays.Should().Equal(10, 20, 40);
        }

        [Fact(DisplayName = "Execute: потолок экспоненциальной задержки")]
        public void Execute_ExponentialBackoffCapped()
        {
            var delays = new List<int>();

            RetryPolicy.Execute<string>(
                attempt => throw new InvalidOperationException("всё падает"),
                maxAttempts: 3,
                delayMs: 100,
                exponentialBackoff: true,
                maxDelayMs: 150,
                delayCallback: (attempt, delay) => delays.Add(delay));

            delays.Should().Equal(100, 150);
        }

        #endregion
    }
}
