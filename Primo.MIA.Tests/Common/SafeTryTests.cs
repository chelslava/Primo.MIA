using System;
using FluentAssertions;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    /// <summary>
    /// Тесты SafeTry.Result (issue #38): fallback при исключении,
    /// прохождение результата, Guard.
    /// </summary>
    [Trait("Category", "Fast")]
    public class SafeTryTests
    {
        [Fact(DisplayName = "Result: функция выполняется, значение возвращается")]
        public void Result_ActionSucceeds_ReturnsValue()
        {
            SafeTry.Result(() => 42, 0, "тест").Should().Be(42);
        }

        [Fact(DisplayName = "Result: исключение — fallback без броска")]
        public void Result_ActionThrows_ReturnsFallback()
        {
            SafeTry.Result<int>(() => throw new InvalidOperationException("fail"), -1, "тест")
                .Should().Be(-1);
        }

        [Fact(DisplayName = "Result: reference-type fallback — null")]
        public void Result_ReferenceFallback_Null()
        {
            SafeTry.Result<string>(() => throw new Exception("fail"), null, "тест")
                .Should().BeNull();
        }

        [Fact(DisplayName = "Result: fallback по умолчанию для типа")]
        public void Result_DefaultFallback()
        {
            SafeTry.Result<int>(() => throw new Exception("fail"))
                .Should().Be(0, "default(int)");
        }

        [Fact(DisplayName = "Result: Guard — null делегат")]
        public void Result_NullAction_Throws()
        {
            Action act = () => SafeTry.Result<int>(null, 0, "тест");
            act.Should().Throw<ArgumentException>();
        }

        [Fact(DisplayName = "Result: исключение не логируется в файл (Enabled=False по умолчанию)")]
        public void Result_NoFileLogging_WhenDisabled()
        {
            // по умолчанию Enabled=false — вызов не создаёт файлов
            var before = System.IO.Directory.Exists(ActivityFileLogger.LogDirectory)
                ? System.IO.Directory.GetFiles(ActivityFileLogger.LogDirectory).Length
                : 0;

            SafeTry.Result<int>(() => throw new Exception("quiet fail"), -1, "SafeTryTests");

            var after = System.IO.Directory.Exists(ActivityFileLogger.LogDirectory)
                ? System.IO.Directory.GetFiles(ActivityFileLogger.LogDirectory).Length
                : 0;

            after.Should().Be(before, "при выключенном логе исключение глушится без записи");
        }
    }
}
