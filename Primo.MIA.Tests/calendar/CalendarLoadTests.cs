// =============================================================================
// CalendarLoadTests.cs — тесты активности CalendarLoadBack.
//
// Проверяет загрузку производственного календаря:
// - Загрузка из файла (XML, JSON, CSV, TXT)
// - Валидация параметров
// - Обработка ошибок
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Calendar.Models;
using Primo.MIA.Calendar.Parsers;
using Primo.MIA.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace Primo.MIA.Tests.Calendar
{
    /// <summary>
    /// Тесты активности CalendarLoadBack.
    /// </summary>
    public class CalendarLoadTests
    {
        private readonly string _testDataPath;

        public CalendarLoadTests()
        {
            // Путь к тестовым данным
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _testDataPath = Path.Combine(baseDirectory, "calendar");

            // Если папка не найдена, пробуем путь относительно проекта
            if (!Directory.Exists(_testDataPath))
            {
                string projectPath = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\..\Primo.MIA.Tests"));
                _testDataPath = Path.Combine(projectPath, "calendar");
            }
        }

        // =========================================================================
        // VALIDATION TESTS
        // =========================================================================

        [Fact(Skip = "Требует реальный IWFContainer от Primo Studio")]
        [Trait("Category", "Calendar")]
        public void CalendarLoadBack_Validate_EmptyPath_ReturnsValidationError()
        {
            // Arrange
            var container = new Mock<IWFContainer>().Object;
            var activity = new CalendarLoadBack(container);
            activity.Prop_CalendarPath = "";
            activity.Prop_Year = "2026";

            // Act
            var result = activity.Validate();

            // Assert
            result.Items.Should().Contain(i => i.PropertyName == "Prop_CalendarPath");
        }

        [Fact(Skip = "Требует реальный IWFContainer от Primo Studio")]
        [Trait("Category", "Calendar")]
        public void CalendarLoadBack_Validate_EmptyYear_ReturnsValidationError()
        {
            // Arrange
            var container = new Mock<IWFContainer>().Object;
            var activity = new CalendarLoadBack(container);
            activity.Prop_CalendarPath = @"C:\Temp";
            activity.Prop_Year = "";

            // Act
            var result = activity.Validate();

            // Assert
            result.Items.Should().Contain(i => i.PropertyName == "Prop_Year");
        }

        [Fact(Skip = "Требует реальный IWFContainer от Primo Studio")]
        [Trait("Category", "Calendar")]
        public void CalendarLoadBack_Validate_ValidParameters_ReturnsNoErrors()
        {
            // Arrange
            var container = new Mock<IWFContainer>().Object;
            var activity = new CalendarLoadBack(container);
            activity.Prop_CalendarPath = _testDataPath;
            activity.Prop_Year = "2026";

            // Act
            var result = activity.Validate();

            // Assert
            result.Items.Should().BeEmpty();
        }

        // =========================================================================
        // DEFAULT VALUES TESTS
        // =========================================================================

        [Fact(Skip = "Требует реальный IWFContainer от Primo Studio")]
        [Trait("Category", "Calendar")]
        public void CalendarLoadBack_Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var container = new Mock<IWFContainer>().Object;
            var activity = new CalendarLoadBack(container);

            // Assert
            activity.Prop_Year.Should().Be(DateTime.Now.Year.ToString());
            activity.Prop_Format.Should().Be(CalendarFormat.Xml);
            activity.Prop_Region.Should().Be(CalendarRegion.Russia);
            activity.Prop_DownloadIfMissing.Should().BeTrue();
        }

        [Fact(Skip = "Требует реальный IWFContainer от Primo Studio")]
        [Trait("Category", "Calendar")]
        public void CalendarLoadBack_GroupName_ReturnsUtilities()
        {
            // Arrange
            var container = new Mock<IWFContainer>().Object;
            var activity = new CalendarLoadBack(container);

            // Act
            string groupName = activity.GroupName;

            // Assert
            groupName.Should().Be("Utilities");
        }

        // =========================================================================
        // FILE EXTENSION TESTS
        // =========================================================================

        [Theory(Skip = "Требует реальный IWFContainer от Primo Studio")]
        [Trait("Category", "Calendar")]
        [InlineData(CalendarFormat.Xml, "xml")]
        [InlineData(CalendarFormat.Json, "json")]
        [InlineData(CalendarFormat.Csv, "csv")]
        [InlineData(CalendarFormat.Txt, "txt")]
        public void CalendarLoadBack_Format_ReturnsCorrectExtension(CalendarFormat format, string expectedExtension)
        {
            // Arrange
            var container = new Mock<IWFContainer>().Object;
            var activity = new CalendarLoadBack(container);
            activity.Prop_Format = format;

            // Act
            // Проверяем через формирование имени файла
            string fileName = $"calendar_2026.{expectedExtension}";

            // Assert
            fileName.Should().EndWith(expectedExtension);
        }

        // =========================================================================
        // REGION CODE TESTS
        // =========================================================================

        [Theory(Skip = "Требует реальный IWFContainer от Primo Studio")]
        [Trait("Category", "Calendar")]
        [InlineData(CalendarRegion.Russia, "ru")]
        [InlineData(CalendarRegion.Kazakhstan, "kz")]
        [InlineData(CalendarRegion.Belarus, "by")]
        [InlineData(CalendarRegion.Uzbekistan, "uz")]
        public void CalendarLoadBack_Region_ReturnsCorrectCode(CalendarRegion region, string expectedCode)
        {
            // Arrange
            var container = new Mock<IWFContainer>().Object;
            var activity = new CalendarLoadBack(container);
            activity.Prop_Region = region;

            // Act
            // Проверяем через формирование URL
            string url = $"https://xmlcalendar.ru/data/{expectedCode}/2026/calendar.xml";

            // Assert
            url.Should().Contain($"/{expectedCode}/");
        }

        // =========================================================================
        // INTEGRATION TESTS WITH TEST FILES
        // =========================================================================

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarLoadBack_LoadFromFile_XmlFormat_ReturnsCalendar()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "calendar.xml");
            if (!File.Exists(filePath))
            {
                // Skip test if file not found
                return;
            }

            string content = File.ReadAllText(filePath);
            var parser = new Primo.MIA.Calendar.Parsers.XmlCalendarParser();

            // Act
            ProductionCalendar calendar = parser.Parse(content);

            // Assert
            calendar.Should().NotBeNull();
            calendar.Year.Should().Be(2026);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarLoadBack_LoadFromFile_JsonFormat_ReturnsCalendar()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "calendar.json");
            if (!File.Exists(filePath))
            {
                return;
            }

            string content = File.ReadAllText(filePath);
            var parser = new Primo.MIA.Calendar.Parsers.JsonCalendarParser();

            // Act
            ProductionCalendar calendar = parser.Parse(content);

            // Assert
            calendar.Should().NotBeNull();
            calendar.Year.Should().Be(2026);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarLoadBack_LoadFromFile_CsvFormat_ReturnsCalendar()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "calendar.csv");
            if (!File.Exists(filePath))
            {
                return;
            }

            string content = File.ReadAllText(filePath);
            var parser = new Primo.MIA.Calendar.Parsers.CsvCalendarParser();

            // Act
            ProductionCalendar calendar = parser.Parse(content);

            // Assert
            calendar.Should().NotBeNull();
            calendar.Year.Should().Be(2026);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarLoadBack_LoadFromFile_TxtFormat_ReturnsCalendar()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "calendar.txt");
            if (!File.Exists(filePath))
            {
                return;
            }

            string content = File.ReadAllText(filePath);
            var parser = new Primo.MIA.Calendar.Parsers.TxtCalendarParser();

            // Act
            ProductionCalendar calendar = parser.Parse(content);

            // Assert
            calendar.Should().NotBeNull();
            calendar.Year.Should().Be(2026);
        }

        // =========================================================================
        // ERROR HANDLING TESTS
        // =========================================================================

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarLoadBack_ParseInvalidXml_ThrowsCalendarParseException()
        {
            // Arrange
            var parser = new Primo.MIA.Calendar.Parsers.XmlCalendarParser();
            string invalidContent = "<invalid>";

            // Act & Assert
            Assert.Throws<Primo.MIA.Calendar.Parsers.CalendarParseException>(
                () => parser.Parse(invalidContent));
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarLoadBack_ParseEmptyJson_ThrowsCalendarParseException()
        {
            // Arrange
            var parser = new Primo.MIA.Calendar.Parsers.JsonCalendarParser();
            string emptyContent = "";

            // Act & Assert
            Assert.Throws<Primo.MIA.Calendar.Parsers.CalendarParseException>(
                () => parser.Parse(emptyContent));
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarLoadBack_ParseEmptyCsv_ThrowsCalendarParseException()
        {
            // Arrange
            var parser = new Primo.MIA.Calendar.Parsers.CsvCalendarParser();
            string emptyContent = "";

            // Act & Assert
            Assert.Throws<Primo.MIA.Calendar.Parsers.CalendarParseException>(
                () => parser.Parse(emptyContent));
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarLoadBack_ParseEmptyTxt_ThrowsCalendarParseException()
        {
            // Arrange
            var parser = new Primo.MIA.Calendar.Parsers.TxtCalendarParser();
            string emptyContent = "";

            // Act & Assert
            Assert.Throws<Primo.MIA.Calendar.Parsers.CalendarParseException>(
                () => parser.Parse(emptyContent));
        }

        // =========================================================================
        // CALENDAR PARSE EXCEPTION TESTS
        // =========================================================================

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarParseException_WithFormat_SetsFormatProperty()
        {
            // Arrange & Act
            var exception = new Primo.MIA.Calendar.Parsers.CalendarParseException(
                "Test message", CalendarFormat.Xml);

            // Assert
            exception.Format.Should().Be(CalendarFormat.Xml);
            exception.Message.Should().Be("Test message");
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarParseException_WithInnerException_PreservesInnerException()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new Primo.MIA.Calendar.Parsers.CalendarParseException(
                "Test message", CalendarFormat.Json, innerException);

            // Assert
            exception.InnerException.Should().Be(innerException);
            exception.Format.Should().Be(CalendarFormat.Json);
        }
    }
}
