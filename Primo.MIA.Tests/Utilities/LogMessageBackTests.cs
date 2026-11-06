using FluentAssertions;
using System;
using System.Reflection;
using Xunit;

namespace Primo.MIA.Tests.Utilities
{
    /// <summary>
    /// Тесты чистых функций форматирования LogMessageBack (issue #16):
    ///   - GetLevelString — выравнивание уровней логирования;
    ///   - FormatMessage — сборка строки сообщения (формат, не значение Now);
    ///   - ApplyCustomPattern — плейсхолдеры имени файла ({base}, {date}, {time},
    ///     {week}, {day_of_year}, {guid}, ...) на фиксированной дате;
    ///   - GenerateFileName (Fixed) и CleanFileName.
    ///
    /// Конструктор Back-класса требует реальный IWFContainer от Primo Studio,
    /// поэтому экземпляр создаётся через FormatterServices.GetUninitializedObject
    /// (без вызова конструктора), а приватные методы и вложенный класс
    /// LogParameters вызываются через отражение. Тестируемые методы не обращаются
    /// к SDK и к файловой системе.
    /// </summary>
        [Trait("Category", "Fast")]
    public class LogMessageBackTests
    {
        private static readonly Type BackType = typeof(LogMessageBack);
        private static readonly Type ParametersType = GetNestedLogParameters();

        /// <summary>Фиксированный момент для проверок плейсхолдеров: 15.02.2025 14:30:45.123.</summary>
        private static readonly DateTime FixedNow = new DateTime(2025, 2, 15, 14, 30, 45, 123);

        // =====================================================================
        // GetLevelString
        // =====================================================================

        #region GetLevelString

        [Theory]
        [InlineData(LogLevel.Debug, "DEBUG   ")]
        [InlineData(LogLevel.Info, "INFO    ")]
        [InlineData(LogLevel.Warning, "WARNING ")]
        [InlineData(LogLevel.Error, "ERROR   ")]
        [InlineData(LogLevel.Critical, "CRITICAL")]
        public void GetLevelString_AllLevels_PaddedToEightChars(LogLevel level, string expected)
        {
            InvokeInstanceString("GetLevelString", level).Should().Be(expected);
        }

        #endregion

        // =====================================================================
        // FormatMessage — детерминированные комбинации флагов
        // (timestamp завязан на Now — при его включении проверяем формат, не значение)
        // =====================================================================

        #region FormatMessage

        [Fact]
        public void FormatMessage_WithoutTimestamp_LevelAndMessage_ExactString()
        {
            var parameters = CreateParameters(message: "hello", includeTimestamp: false,
                includeLevel: true, appendNewLine: false, level: LogLevel.Error);

            var result = InvokeInstanceString("FormatMessage", parameters);

            result.Should().Be("[ERROR   ] hello");
        }

        [Fact]
        public void FormatMessage_WithoutTimestampAndLevel_ReturnsOnlyMessage()
        {
            var parameters = CreateParameters(message: "hello", includeTimestamp: false,
                includeLevel: false, appendNewLine: false);

            var result = InvokeInstanceString("FormatMessage", parameters);

            result.Should().Be("hello");
        }

        [Fact]
        public void FormatMessage_WithTimestamp_UsesConfiguredTimestampFormat()
        {
            var parameters = CreateParameters(message: "hello", includeTimestamp: true,
                includeLevel: true, appendNewLine: false, timestampFormat: "yyyy-MM-dd HH:mm:ss.fff");

            var result = InvokeInstanceString("FormatMessage", parameters);

            // Значение времени зависит от Now — проверяем структуру строки
            result.Should().MatchRegex(
                @"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\] \[ERROR   \] hello$");
        }

        [Fact]
        public void FormatMessage_AppendNewLine_AddsLineBreak()
        {
            var parameters = CreateParameters(message: "hello", includeTimestamp: false,
                includeLevel: false, appendNewLine: true);

            var result = InvokeInstanceString("FormatMessage", parameters);

            result.Should().Be("hello" + Environment.NewLine);
        }

        #endregion

        // =====================================================================
        // ApplyCustomPattern — плейсхолдеры имени файла на фиксированной дате
        // =====================================================================

        #region ApplyCustomPattern

        [Fact]
        public void ApplyCustomPattern_BaseDateAndTimeComponents_AllReplaced()
        {
            var parameters = CreateParameters(baseFileName: "app", fileExtension: ".log");

            var result = InvokeInstanceString("ApplyCustomPattern",
                "{base}_{yyyy}-{MM}-{dd}_{HH}-{mm}{ext}", parameters, FixedNow);

            result.Should().Be("app_2025-02-15_14-30.log");
        }

        [Fact]
        public void ApplyCustomPattern_FullDateTokens_ReplacedWithFixedValues()
        {
            var parameters = CreateParameters();

            var result = InvokeInstanceString("ApplyCustomPattern",
                "{date}|{time}|{timestamp}|{fff}|{ss}", parameters, FixedNow);

            // {date}=yyyy-MM-dd, {time}=HH-mm-ss, {timestamp}=yyyyMMddHHmmss
            result.Should().Be("2025-02-15_14-30-45_20250215143045_123_45");
        }

        [Fact]
        public void ApplyCustomPattern_WeekAndDayOfYear_ReplacedWithFixedValues()
        {
            var parameters = CreateParameters();

            var result = InvokeInstanceString("ApplyCustomPattern",
                "{week}|{day_of_year}", parameters, FixedNow);

            // 15.02.2025 (суббота): 7-я неделя с понедельника, 46-й день года
            result.Should().Be("07_046");
        }

        [Fact]
        public void ApplyCustomPattern_LevelToken_LowerCased()
        {
            var parameters = CreateParameters(level: LogLevel.Critical);

            var result = InvokeInstanceString("ApplyCustomPattern", "{level}", parameters, FixedNow);

            result.Should().Be("critical");
        }

        [Fact]
        public void ApplyCustomPattern_GuidTokens_ReplacedWithHexStrings()
        {
            var parameters = CreateParameters();

            var result = InvokeInstanceString("ApplyCustomPattern",
                "x{guid}y{guid_short}z", parameters, FixedNow);

            result.Should().MatchRegex("^x[0-9a-f]{32}y[0-9a-f]{8}z$");
        }

        [Fact]
        public void ApplyCustomPattern_UnknownTokenLeftAsIs_AndInvalidCharsCleaned()
        {
            var parameters = CreateParameters();

            var result = InvokeInstanceString("ApplyCustomPattern",
                "{unknown}<token>:", parameters, FixedNow);

            // Неизвестный плейсхолдер остаётся, недопустимые символы имени файла заменяются на _
            result.Should().Be("{unknown}_token__");
        }

        #endregion

        // =====================================================================
        // GenerateFileName / CleanFileName
        // =====================================================================

        #region FileName

        [Fact]
        public void GenerateFileName_FixedTemplate_ConcatenatesBaseAndExtension()
        {
            var parameters = CreateParameters(baseFileName: "app", fileExtension: ".log",
                fileNameTemplate: LogFileNameTemplate.Fixed);

            var result = InvokeInstanceString("GenerateFileName", parameters);

            result.Should().Be("app.log");
        }

        [Theory]
        [InlineData("a<b>c", "a_b_c")]
        [InlineData("d:e*f", "d_e_f")]
        [InlineData("clean_name.log", "clean_name.log")]
        public void CleanFileName_ReplacesInvalidCharsWithUnderscore(string fileName, string expected)
        {
            InvokeInstanceString("CleanFileName", fileName).Should().Be(expected);
        }

        #endregion

        // =====================================================================
        // Вспомогательные методы (отражение)
        // =====================================================================

        private static Type GetNestedLogParameters()
        {
            return typeof(LogMessageBack).GetNestedType("LogParameters", BindingFlags.NonPublic);
        }

        /// <summary>
        /// Создаёт экземпляр Back-класса без вызова конструктора
        /// (конструктор требует реальный IWFContainer).
        /// </summary>
        private static LogMessageBack CreateUninitializedBack()
        {
            return (LogMessageBack)System.Runtime.Serialization.FormatterServices
                .GetUninitializedObject(typeof(LogMessageBack));
        }

        /// <summary>Создаёт private LogParameters с нужными значениями свойств.</summary>
        private static object CreateParameters(
            string message = "msg",
            LogLevel level = LogLevel.Error,
            string baseFileName = "app",
            string fileExtension = ".log",
            string timestampFormat = "yyyy-MM-dd HH:mm:ss.fff",
            bool includeTimestamp = false,
            bool includeLevel = true,
            bool appendNewLine = false,
            LogFileNameTemplate fileNameTemplate = LogFileNameTemplate.Custom)
        {
            var parameters = Activator.CreateInstance(ParametersType, nonPublic: true);
            SetProperty(parameters, "Message", message);
            SetProperty(parameters, "Level", level);
            SetProperty(parameters, "BaseFileName", baseFileName);
            SetProperty(parameters, "FileExtension", fileExtension);
            SetProperty(parameters, "TimestampFormat", timestampFormat);
            SetProperty(parameters, "IncludeTimestamp", includeTimestamp);
            SetProperty(parameters, "IncludeLevel", includeLevel);
            SetProperty(parameters, "AppendNewLine", appendNewLine);
            SetProperty(parameters, "FileNameTemplate", fileNameTemplate);
            return parameters;
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            ParametersType.GetProperty(propertyName).SetValue(target, value);
        }

        private static string InvokeInstanceString(string methodName, params object[] args)
        {
            return (string)BackType
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(CreateUninitializedBack(), args);
        }
    }
}
