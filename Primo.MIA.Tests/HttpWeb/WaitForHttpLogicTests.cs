using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.HttpWeb
{
    /// <summary>
    /// Тесты WaitForHttpLogic (issue #32): условие остановки, poll-цикл
    /// с инъекцией fetch-делегата и нулевой задержки, таймаут, сетевые ошибки.
    /// </summary>
    [Trait("Category", "Fast")]
    public class WaitForHttpLogicTests
    {
        private readonly WaitForHttpLogic _logic = new WaitForHttpLogic();

        /// <summary>Задержка без реального ожидания — фиксирует затребованные паузы.</summary>
        private readonly List<TimeSpan> _recordedDelays = new List<TimeSpan>();

        private Action<TimeSpan> NoDelay() => wait => _recordedDelays.Add(wait);

        // ── IsConditionMet ─────────────────────────────────────────────

        [Fact(DisplayName = "Условие: ожидаемый статус 200 выполнен")]
        public void Condition_ExpectedStatus_Matches()
        {
            _logic.IsConditionMet(200, 200, null, null).Should().BeTrue();
            _logic.IsConditionMet(404, 200, "body", null).Should().BeFalse();
        }

        [Fact(DisplayName = "Условие: 0/без статуса — любой 2xx")]
        public void Condition_NullOrZero_Any2xx()
        {
            _logic.IsConditionMet(204, 0, null, null).Should().BeTrue();
            _logic.IsConditionMet(200, null, null, null).Should().BeTrue();
            _logic.IsConditionMet(302, 0, null, null).Should().BeFalse();
            _logic.IsConditionMet(500, 0, null, null).Should().BeFalse();
        }

        [Fact(DisplayName = "Условие: подстрока тела обязательна")]
        public void Condition_BodyContains()
        {
            _logic.IsConditionMet(200, 200, "{\"s\":\"done\"}", "done").Should().BeTrue();
            _logic.IsConditionMet(200, 200, "{\"s\":\"running\"}", "done").Should().BeFalse();
            _logic.IsConditionMet(200, 200, null, "done").Should().BeFalse("тела нет — подстроки нет");
        }

        [Fact(DisplayName = "Условие: нет ответа — не выполнено")]
        public void Condition_NoResponse_False()
        {
            _logic.IsConditionMet(null, 200, null, null).Should().BeFalse();
        }

        // ── Нормализация ───────────────────────────────────────────────

        [Fact(DisplayName = "Интервал: ниже 100 мс поднимается до 100")]
        public void NormalizeInterval_Minimum()
        {
            _logic.NormalizeInterval(0).Should().Be(100);
            _logic.NormalizeInterval(1000).Should().Be(1000);
        }

        [Fact(DisplayName = "Таймаут: 0 → по умолчанию; сверх часа → час")]
        public void NormalizeTimeout_Clamps()
        {
            _logic.NormalizeTimeout(0).Should().Be(WaitForHttpLogic.DefaultTimeoutSeconds);
            _logic.NormalizeTimeout(-5).Should().Be(WaitForHttpLogic.DefaultTimeoutSeconds);
            _logic.NormalizeTimeout(99999).Should().Be(WaitForHttpLogic.MaxTimeoutSeconds);
            _logic.NormalizeTimeout(30).Should().Be(30);
        }

        // ── PollUntil ──────────────────────────────────────────────────

        [Fact(DisplayName = "Poll: успех с первой попытки — без задержек")]
        public void Poll_ImmediateSuccess()
        {
            var result = _logic.PollUntil(
                () => (200, "ok"), 200, null,
                TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(500), NoDelay());

            result.ConditionMet.Should().BeTrue();
            result.Attempts.Should().Be(1);
            result.LastBody.Should().Be("ok");
            _recordedDelays.Should().BeEmpty("успех с первой попытки — пауза не нужна");
        }

        [Fact(DisplayName = "Poll: успех после N неудачных попыток")]
        public void Poll_SuccessAfterRetries()
        {
            var responses = new Queue<(int, string)>(new[] { (202, "queued"), (202, "running"), (200, "done") });

            var result = _logic.PollUntil(
                () => responses.Dequeue(), 200, null,
                TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(100), NoDelay());

            result.ConditionMet.Should().BeTrue();
            result.Attempts.Should().Be(3);
            result.LastBody.Should().Be("done");
            _recordedDelays.Should().HaveCount(2, "паузы между попытками");
        }

        [Fact(DisplayName = "Poll: таймаут — не успех, последний статус сохранён")]
        public void Poll_Timeout_ReturnsNotMet()
        {
            var result = _logic.PollUntil(
                () => (202, "running"), 200, null,
                TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(1), NoDelay());

            result.ConditionMet.Should().BeFalse();
            result.Attempts.Should().BeGreaterThan(0);
            result.LastStatus.Should().Be(202);
        }

        [Fact(DisplayName = "Poll: сетевые ошибки не прерывают ожидание")]
        public void Poll_NetworkErrors_Continue()
        {
            var attempts = 0;
            var result = _logic.PollUntil(
                () =>
                {
                    attempts++;
                    if (attempts < 3)
                        throw new InvalidOperationException("соединение разорвано");
                    return (200, "recovered");
                },
                200, null,
                TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(10), NoDelay());

            result.ConditionMet.Should().BeTrue("сервер «поднялся» на 3-й попытке");
            result.Attempts.Should().Be(3);
            result.LastError.Should().BeNull("последняя попытка успешна");
        }

        [Fact(DisplayName = "Poll: все попытки сетевые ошибки — не успех")]
        public void Poll_AllNetworkErrors_NotMet()
        {
            var result = _logic.PollUntil(
                () => throw new InvalidOperationException("host unreachable"),
                200, null,
                TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(1), NoDelay());

            result.ConditionMet.Should().BeFalse();
            result.LastStatus.Should().BeNull();
            result.LastError.Should().NotBeNull();
        }

        [Fact(DisplayName = "Poll: Guard на null-делегаты")]
        public void Poll_NullDelegates_Throw()
        {
            Action noFetch = () => _logic.PollUntil(null, 200, null, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1), NoDelay());
            Action noDelay = () => _logic.PollUntil(() => (200, "x"), 200, null, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1), null);

            noFetch.Should().Throw<ArgumentException>();
            noDelay.Should().Throw<ArgumentException>();
        }
    }
}
