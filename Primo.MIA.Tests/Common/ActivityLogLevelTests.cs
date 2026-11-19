using FluentAssertions;
using Primo.MIA.Common;
using System;
using System.IO;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    /// <summary>
    /// Фильтр уровней ActivityFileLogger.MinLevel (Debug/Info/None).
    /// Классы мутируют статику логгера — общий последовательный collection.
    /// </summary>
    [Collection("ActivityLoggerStatic")]
    [Trait("Category", "Fast")]
    public class ActivityLogLevelTests : IDisposable
    {
        private readonly string _tmpDir;
        private readonly bool _oldEnabled;
        private readonly ActivityLogLevel _oldLevel;
        private readonly string _oldDir;

        public ActivityLogLevelTests()
        {
            _tmpDir = Path.Combine(Path.GetTempPath(), "mia-loglevel-tests", Guid.NewGuid().ToString("N"));
            _oldEnabled = ActivityFileLogger.Enabled;
            _oldLevel = ActivityFileLogger.MinLevel;
            _oldDir = ActivityFileLogger.LogDirectory;
            ActivityFileLogger.LogDirectory = _tmpDir;
            ActivityFileLogger.Enabled = true;
        }

        public void Dispose()
        {
            ActivityFileLogger.Enabled = _oldEnabled;
            ActivityFileLogger.MinLevel = _oldLevel;
            ActivityFileLogger.LogDirectory = _oldDir;
            try { if (Directory.Exists(_tmpDir)) Directory.Delete(_tmpDir, recursive: true); } catch { }
        }

        private string LogFile => Path.Combine(_tmpDir, $"Primo.MIA.{DateTime.Now:yyyyMMdd}.log");

        private string ReadLog() => File.Exists(LogFile) ? File.ReadAllText(LogFile) : string.Empty;

        #region AAA

        [Fact]
        public void InfoLevel_HidesDebugMessages()
        {
            // Arrange
            ActivityFileLogger.MinLevel = ActivityLogLevel.Info;

            // Act
            ActivityFileLogger.Debug("тест", "MARKER-hidden-debug-01");

            // Assert
            ReadLog().Should().NotContain("MARKER-hidden-debug-01");
        }

        [Fact]
        public void InfoLevel_PassesInfoMessages()
        {
            // Arrange
            ActivityFileLogger.MinLevel = ActivityLogLevel.Info;

            // Act
            ActivityFileLogger.Info("тест", "MARKER-visible-info-02");

            // Assert
            ReadLog().Should().Contain("MARKER-visible-info-02");
        }

        [Fact]
        public void DebugLevel_PassesDebugMessages()
        {
            // Arrange
            ActivityFileLogger.MinLevel = ActivityLogLevel.Debug;

            // Act
            ActivityFileLogger.Debug("тест", "MARKER-visible-debug-03");

            // Assert
            ReadLog().Should().Contain("MARKER-visible-debug-03");
        }

        [Fact]
        public void NoneLevel_HidesAllMessages_IncludingErrors()
        {
            // Arrange
            ActivityFileLogger.MinLevel = ActivityLogLevel.None;

            // Act
            ActivityFileLogger.Error("тест", "MARKER-hidden-error-04");
            ActivityFileLogger.Info("тест", "MARKER-hidden-info-04");
            ActivityFileLogger.Debug("тест", "MARKER-hidden-debug-04");

            // Assert
            ReadLog().Should().NotContain("MARKER-hidden");
        }

        [Fact]
        public void Warning_PassesAtInfoLevel()
        {
            // Arrange
            ActivityFileLogger.MinLevel = ActivityLogLevel.Info;

            // Act
            ActivityFileLogger.Warning("тест", "MARKER-visible-warn-05");

            // Assert
            ReadLog().Should().Contain("MARKER-visible-warn-05");
        }

        #endregion
    }

    /// <summary>Применение настроек логирования (LogSettingsLogic).</summary>
    [Collection("ActivityLoggerStatic")]
    [Trait("Category", "Fast")]
    public class LogSettingsLogicTests : IDisposable
    {
        private readonly bool _oldEnabled;
        private readonly ActivityLogLevel _oldLevel;
        private readonly string _oldDir;

        public LogSettingsLogicTests()
        {
            _oldEnabled = ActivityFileLogger.Enabled;
            _oldLevel = ActivityFileLogger.MinLevel;
            _oldDir = ActivityFileLogger.LogDirectory;
        }

        public void Dispose()
        {
            ActivityFileLogger.Enabled = _oldEnabled;
            ActivityFileLogger.MinLevel = _oldLevel;
            ActivityFileLogger.LogDirectory = _oldDir;
        }

        #region AAA

        [Fact]
        public void Apply_SetsLevelAndFileLogFlag()
        {
            // Act
            LogSettingsLogic.Apply(ActivityLogLevel.Debug, true, null);

            // Assert
            ActivityFileLogger.MinLevel.Should().Be(ActivityLogLevel.Debug);
            ActivityFileLogger.Enabled.Should().BeTrue();
        }

        [Fact]
        public void Apply_EmptyDirectory_KeepsCurrent()
        {
            // Arrange
            var before = ActivityFileLogger.LogDirectory;

            // Act
            var effective = LogSettingsLogic.Apply(ActivityLogLevel.Info, false, "   ");

            // Assert
            effective.Should().Be(before);
            ActivityFileLogger.LogDirectory.Should().Be(before);
        }

        [Fact]
        public void Apply_QuotedDirectory_TrimsQuotesAndSpaces()
        {
            // Act
            var effective = LogSettingsLogic.Apply(ActivityLogLevel.None, false, "  \"C:\\logs\\mia\"  ");

            // Assert
            effective.Should().Be("C:\\logs\\mia");
            ActivityFileLogger.LogDirectory.Should().Be("C:\\logs\\mia");
        }

        [Fact]
        public void Apply_ReturnsEffectiveDirectory()
        {
            // Act
            var effective = LogSettingsLogic.Apply(ActivityLogLevel.None, true, @"D:\custom\logs");

            // Assert
            effective.Should().Be(@"D:\custom\logs");
        }

        #endregion
    }
}
