// =============================================================================
// CalendarParsersTests.cs — тесты парсеров производственного календаря.
//
// Проверяет корректность парсинга XML, JSON, CSV и TXT форматов:
// - Чтение тестовых файлов из папки calendar
// - Проверка структуры и данных ProductionCalendar
// - Обработка ошибок парсинга
// =============================================================================

using System;
using System.IO;
using FluentAssertions;
using Primo.MIA.Calendar.Models;
using Primo.MIA.Calendar.Parsers;
using System.Collections.Generic;
using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Calendar
{
    /// <summary>
    /// Тесты парсеров производственного календаря.
    /// </summary>
    public class CalendarParsersTests
    {
        private readonly string _testDataPath;

        public CalendarParsersTests()
        {
            // Путь к тестовым данным — определяем относительно текущей директории
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
        // XML PARSER TESTS
        // =========================================================================

        [Fact]
        [Trait("Category", "Calendar")]
        public void XmlCalendarParser_Parse_ValidFile_ReturnsCalendar()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "calendar.xml");
            string content = File.ReadAllText(filePath);
            var parser = new XmlCalendarParser();

            // Act
            ProductionCalendar calendar = parser.Parse(content);

            // Assert
            calendar.Should().NotBeNull();
            calendar.Year.Should().Be(2026);
            calendar.CountryCode.Should().Be("ru");
            calendar.Holidays.Should().HaveCount(8);
            calendar.SpecialDays.Should().HaveCount(22);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void XmlCalendarParser_Parse_HolidaysContainCorrectNames()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "calendar.xml");
            string content = File.ReadAllText(filePath);
            var parser = new XmlCalendarParser();

            // Act
            ProductionCalendar calendar = parser.Parse(content);

            // Assert
            calendar.Holidays["1"].Should().Be("Новогодние каникулы");
            calendar.Holidays["2"].Should().Be("Рождество Христово");
            calendar.Holidays["3"].Should().Be("День защитника Отечества");
            calendar.Holidays["4"].Should().Be("Международный женский день");
            calendar.Holidays["5"].Should().Be("Праздник Весны и Труда");
            calendar.Holidays["6"].Should().Be("День Победы");
            calendar.Holidays["7"].Should().Be("День России");
            calendar.Holidays["8"].Should().Be("День народного единства");
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void XmlCalendarParser_Parse_SpecialDaysContainTransitions()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "calendar.xml");
            string content = File.ReadAllText(filePath);
            var parser = new XmlCalendarParser();

            // Act
            ProductionCalendar calendar = parser.Parse(content);

            // Assert
            calendar.Transitions.Should().ContainKey("01.03");
            calendar.Transitions["01.03"].Should().Be("01.09");
            calendar.Transitions.Should().ContainKey("03.08");
            calendar.Transitions["03.08"].Should().Be("03.09");
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void XmlCalendarParser_Parse_EmptyContent_ThrowsException()
        {
            // Arrange
            var parser = new XmlCalendarParser();

            // Act
            Action act = () => parser.Parse("");

            // Assert
            act.Should().Throw<CalendarParseException>()
                .Where(e => e.Format == CalendarFormat.Xml);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void XmlCalendarParser_Parse_InvalidXml_ThrowsException()
        {
            // Arrange
            var parser = new XmlCalendarParser();

            // Act
            Action act = () => parser.Parse("<invalid>");

            // Assert
            act.Should().Throw<CalendarParseException>();
        }

        // =========================================================================
        // JSON PARSER TESTS
        // =========================================================================

        [Fact]
        [Trait("Category", "Calendar")]
        public void JsonCalendarParser_Parse_ValidFile_ReturnsCalendar()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "calendar.json");
            string content = File.ReadAllText(filePath);
            var parser = new JsonCalendarParser();

            // Act
            ProductionCalendar calendar = parser.Parse(content);

            // Assert
            calendar.Should().NotBeNull();
            calendar.Year.Should().Be(2026);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void JsonCalendarParser_Parse_ContainsTransitions()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "calendar.json");
            string content = File.ReadAllText(filePath);
            var parser = new JsonCalendarParser();

            // Act
            ProductionCalendar calendar = parser.Parse(content);

            // Assert
            calendar.Transitions.Should().HaveCount(4);
            calendar.Transitions.Should().ContainKey("01.03");
            calendar.Transitions["01.03"].Should().Be("01.09");
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void JsonCalendarParser_Parse_ContainsStatistics()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "calendar.json");
            string content = File.ReadAllText(filePath);
            var parser = new JsonCalendarParser();

            // Act
            ProductionCalendar calendar = parser.Parse(content);

            // Assert
            calendar.Statistics.Should().NotBeNull();
            calendar.Statistics.Workdays.Should().Be(247);
            calendar.Statistics.Holidays.Should().Be(118);
            calendar.Statistics.Hours40.Should().Be(1972);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void JsonCalendarParser_Parse_EmptyContent_ThrowsException()
        {
            // Arrange
            var parser = new JsonCalendarParser();

            // Act
            Action act = () => parser.Parse("");

            // Assert
            act.Should().Throw<CalendarParseException>()
                .Where(e => e.Format == CalendarFormat.Json);
        }

        // =========================================================================
        // CSV PARSER TESTS
        // =========================================================================

        [Fact]
        [Trait("Category", "Calendar")]
        public void CsvCalendarParser_Parse_ValidFile_ReturnsCalendar()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "calendar.csv");
            string content = File.ReadAllText(filePath);
            var parser = new CsvCalendarParser();

            // Act
            ProductionCalendar calendar = parser.Parse(content);

            // Assert
            calendar.Should().NotBeNull();
            calendar.Year.Should().Be(2026);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void CsvCalendarParser_Parse_ContainsStatistics()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "calendar.csv");
            string content = File.ReadAllText(filePath);
            var parser = new CsvCalendarParser();

            // Act
            ProductionCalendar calendar = parser.Parse(content);

            // Assert
            calendar.Statistics.Should().NotBeNull();
            calendar.Statistics.Workdays.Should().Be(247);
            calendar.Statistics.Holidays.Should().Be(118);
            calendar.Statistics.Hours40.Should().Be(1972);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void CsvCalendarParser_Parse_EmptyContent_ThrowsException()
        {
            // Arrange
            var parser = new CsvCalendarParser();

            // Act
            Action act = () => parser.Parse("");

            // Assert
            act.Should().Throw<CalendarParseException>()
                .Where(e => e.Format == CalendarFormat.Csv);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void CsvCalendarParser_Parse_SingleLine_ThrowsException()
        {
            // Arrange
            var parser = new CsvCalendarParser();

            // Act
            Action act = () => parser.Parse("header only");

            // Assert
            act.Should().Throw<CalendarParseException>()
                .Where(e => e.Format == CalendarFormat.Csv);
        }

        // =========================================================================
        // TXT PARSER TESTS
        // =========================================================================

        [Fact]
        [Trait("Category", "Calendar")]
        public void TxtCalendarParser_Parse_ValidFile_ReturnsCalendar()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "calendar.txt");
            string content = File.ReadAllText(filePath);
            var parser = new TxtCalendarParser();

            // Act
            ProductionCalendar calendar = parser.Parse(content);

            // Assert
            calendar.Should().NotBeNull();
            calendar.Year.Should().Be(2026);
            calendar.SpecialDays.Should().NotBeEmpty();
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void TxtCalendarParser_Parse_ContainsStatistics()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "calendar.txt");
            string content = File.ReadAllText(filePath);
            var parser = new TxtCalendarParser();

            // Act
            ProductionCalendar calendar = parser.Parse(content);

            // Assert
            calendar.Statistics.Should().NotBeNull();
            calendar.Statistics.Holidays.Should().Be(108);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void TxtCalendarParser_Parse_EmptyContent_ThrowsException()
        {
            // Arrange
            var parser = new TxtCalendarParser();

            // Act
            Action act = () => parser.Parse("");

            // Assert
            act.Should().Throw<CalendarParseException>()
                .Where(e => e.Format == CalendarFormat.Txt);
        }

        // =========================================================================
        // PRODUCTION CALENDAR MODEL TESTS
        // =========================================================================

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_GetDayType_Workday_ReturnsWorkday()
        {
            // Arrange
            var calendar = new ProductionCalendar { Year = 2026 };
            // 12 января 2026 — понедельник (рабочий день, если не праздник)
            DateTime date = new DateTime(2026, 1, 12);

            // Act
            DayType dayType = calendar.GetDayType(date);

            // Assert
            dayType.Should().Be(DayType.Workday);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_GetDayType_Saturday_ReturnsWeekend()
        {
            // Arrange
            var calendar = new ProductionCalendar { Year = 2026 };
            // 17 января 2026 — суббота
            DateTime date = new DateTime(2026, 1, 17);

            // Act
            DayType dayType = calendar.GetDayType(date);

            // Assert
            dayType.Should().Be(DayType.Weekend);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_AddWorkdays_ReturnsCorrectDate()
        {
            // Arrange
            var calendar = new ProductionCalendar { Year = 2026 };
            DateTime startDate = new DateTime(2026, 1, 12); // Понедельник

            // Act
            DateTime result = calendar.AddWorkdays(startDate, 5);

            // Assert
            // 5 рабочих дней с 12 января: 12, 13, 14, 15, 16
            // Результат — 19 января (понедельник)
            result.Should().Be(new DateTime(2026, 1, 19));
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_SubtractWorkdays_ReturnsCorrectDate()
        {
            // Arrange
            var calendar = new ProductionCalendar { Year = 2026 };
            DateTime startDate = new DateTime(2026, 1, 19); // Понедельник

            // Act
            DateTime result = calendar.SubtractWorkdays(startDate, 5);

            // Assert
            // 5 рабочих дней назад от 19 января: 16, 15, 14, 13, 12
            result.Should().Be(new DateTime(2026, 1, 12));
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_IsWorkday_Workday_ReturnsTrue()
        {
            // Arrange
            var calendar = new ProductionCalendar { Year = 2026 };
            DateTime date = new DateTime(2026, 1, 12); // Понедельник

            // Act
            bool isWorkday = calendar.IsWorkday(date);

            // Assert
            isWorkday.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_IsWorkday_Weekend_ReturnsFalse()
        {
            // Arrange
            var calendar = new ProductionCalendar { Year = 2026 };
            DateTime date = new DateTime(2026, 1, 17); // Суббота

            // Act
            bool isWorkday = calendar.IsWorkday(date);

            // Assert
            isWorkday.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_GetWorkdaysBetween_ReturnsCorrectCount()
        {
            // Arrange
            var calendar = new ProductionCalendar { Year = 2026 };
            DateTime startDate = new DateTime(2026, 1, 12); // Понедельник
            DateTime endDate = new DateTime(2026, 1, 16); // Пятница

            // Act
            int workdays = calendar.GetWorkdaysBetween(startDate, endDate);

            // Assert
            workdays.Should().Be(5); // Пн-Пт = 5 рабочих дней
        }
    }
}
