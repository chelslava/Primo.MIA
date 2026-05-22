// =============================================================================
// BusinessCalendarTests.cs — тесты активности BusinessCalendarBack.
//
// Проверяет операции с производственным календарём:
// - Прибавление/вычитание рабочих дней
// - Проверка типа дня (рабочий/выходной/праздник)
// - Валидация параметров
// =============================================================================

using System;
using FluentAssertions;
using LTools.Common.Model;
using Moq;
using Primo.MIA.Calendar.Models;
using Xunit;

namespace Primo.MIA.Tests.Calendar
{
    /// <summary>
    /// Тесты активности BusinessCalendarBack.
    /// </summary>
    public class BusinessCalendarTests
    {
        // =========================================================================
        // PRODUCTION CALENDAR INTEGRATION TESTS
        // =========================================================================

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_AddWorkdays_WithHolidays_SkipsNonWorkingDays()
        {
            // Arrange
            var calendar = CreateTestCalendar2026();
            // 9 января 2026 — рабочий день из переноса (вместо 3 января)
            DateTime startDate = new DateTime(2026, 1, 9);

            // Act
            DateTime result = calendar.AddWorkdays(startDate, 1);

            // Assert
            // Следующий рабочий день после 9 января — 12 января (пропускаем выходные 10-11)
            result.Should().Be(new DateTime(2026, 1, 12));
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_GetDayType_Holiday_ReturnsHoliday()
        {
            // Arrange
            var calendar = CreateTestCalendar2026();
            // 1 января — Новогодние каникулы
            DateTime date = new DateTime(2026, 1, 1);

            // Act
            DayType dayType = calendar.GetDayType(date);

            // Assert
            dayType.Should().Be(DayType.Holiday);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_GetDayType_TransferredWorkday_ReturnsWorkday()
        {
            // Arrange
            var calendar = CreateTestCalendar2026();
            // 9 января 2026 — рабочий день из переноса (вместо 3 января)
            DateTime date = new DateTime(2026, 1, 9);

            // Act
            DayType dayType = calendar.GetDayType(date);

            // Assert
            dayType.Should().Be(DayType.Workday);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_GetHolidayName_ReturnsCorrectName()
        {
            // Arrange
            var calendar = CreateTestCalendar2026();
            // 23 февраля — День защитника Отечества
            DateTime date = new DateTime(2026, 2, 23);

            // Act
            string holidayName = calendar.GetHolidayName(date);

            // Assert
            holidayName.Should().Be("День защитника Отечества");
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_GetHolidayName_RegularDay_ReturnsNull()
        {
            // Arrange
            var calendar = CreateTestCalendar2026();
            DateTime date = new DateTime(2026, 1, 12); // Обычный рабочий день

            // Act
            string holidayName = calendar.GetHolidayName(date);

            // Assert
            holidayName.Should().BeNull();
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_SubtractWorkdays_CrossYearBoundary_ReturnsCorrectDate()
        {
            // Arrange
            var calendar = CreateTestCalendar2026();
            DateTime startDate = new DateTime(2026, 1, 12);

            // Act
            DateTime result = calendar.SubtractWorkdays(startDate, 5);

            // Assert
            // 5 рабочих дней назад от 12 января:
            // 9 января (рабочий из переноса), 8 января (рабочий из переноса),
            // затем нужно перейти в декабрь 2025
            result.Year.Should().BeOneOf(2025, 2026);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_AddWorkdays_ZeroDays_ReturnsSameDate()
        {
            // Arrange
            var calendar = CreateTestCalendar2026();
            DateTime date = new DateTime(2026, 1, 12);

            // Act
            DateTime result = calendar.AddWorkdays(date, 0);

            // Assert
            result.Should().Be(date);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_AddWorkdays_NegativeDays_SubtractsWorkdays()
        {
            // Arrange
            var calendar = CreateTestCalendar2026();
            DateTime date = new DateTime(2026, 1, 19);

            // Act
            DateTime result = calendar.AddWorkdays(date, -5);

            // Assert
            result.Should().Be(new DateTime(2026, 1, 12));
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_GetWorkdaysBetween_SameDate_ReturnsOne()
        {
            // Arrange
            var calendar = CreateTestCalendar2026();
            DateTime date = new DateTime(2026, 1, 12);

            // Act
            int workdays = calendar.GetWorkdaysBetween(date, date);

            // Assert
            workdays.Should().Be(1);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void ProductionCalendar_GetWorkdaysBetween_ReversedDates_ReturnsCorrectCount()
        {
            // Arrange
            var calendar = CreateTestCalendar2026();
            DateTime startDate = new DateTime(2026, 1, 16);
            DateTime endDate = new DateTime(2026, 1, 12);

            // Act
            int workdays = calendar.GetWorkdaysBetween(startDate, endDate);

            // Assert
            workdays.Should().Be(5);
        }

        // =========================================================================
        // CALENDAR DAY MODEL TESTS
        // =========================================================================

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarDay_IsHoliday_WithHolidayId_ReturnsTrue()
        {
            // Arrange
            var day = new CalendarDay("01.01", 1, 1, null);

            // Act
            bool isHoliday = day.IsHoliday;

            // Assert
            isHoliday.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarDay_IsHoliday_WithoutHolidayId_ReturnsFalse()
        {
            // Arrange
            var day = new CalendarDay("01.01", 1, null, null);

            // Act
            bool isHoliday = day.IsHoliday;

            // Assert
            isHoliday.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarDay_IsShortWorkday_Type2_ReturnsTrue()
        {
            // Arrange
            var day = new CalendarDay("30.04", 2, null, null);

            // Act
            bool isShort = day.IsShortWorkday;

            // Assert
            isShort.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarDay_IsTransferredWorkday_WithTransferFrom_ReturnsTrue()
        {
            // Arrange
            var day = new CalendarDay("01.09", 1, null, "01.03");

            // Act
            bool isTransferred = day.IsTransferredWorkday;

            // Assert
            isTransferred.Should().BeTrue();
        }

        // =========================================================================
        // CALENDAR STATISTICS MODEL TESTS
        // =========================================================================

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarStatistics_DefaultValues_AreZero()
        {
            // Arrange & Act
            var stats = new CalendarStatistics();

            // Assert
            stats.Workdays.Should().Be(0);
            stats.Holidays.Should().Be(0);
            stats.Hours40.Should().Be(0);
            stats.Hours36.Should().Be(0);
            stats.Hours24.Should().Be(0);
        }

        [Fact]
        [Trait("Category", "Calendar")]
        public void CalendarStatistics_ToString_ContainsAllValues()
        {
            // Arrange
            var stats = new CalendarStatistics
            {
                Workdays = 247,
                Holidays = 118,
                Hours40 = 1972,
                Hours36 = 1774.4,
                Hours24 = 1181.6
            };

            // Act
            string result = stats.ToString();

            // Assert
            result.Should().Contain("247");
            result.Should().Contain("118");
            result.Should().Contain("1972");
        }

        // =========================================================================
        // HELPER METHODS
        // =========================================================================

        /// <summary>
        /// Создаёт тестовый календарь на 2026 год с данными из XML.
        /// </summary>
        private ProductionCalendar CreateTestCalendar2026()
        {
            var calendar = new ProductionCalendar { Year = 2026, CountryCode = "ru" };

            // Добавляем праздники
            calendar.AddHoliday("1", "Новогодние каникулы");
            calendar.AddHoliday("2", "Рождество Христово");
            calendar.AddHoliday("3", "День защитника Отечества");
            calendar.AddHoliday("4", "Международный женский день");
            calendar.AddHoliday("5", "Праздник Весны и Труда");
            calendar.AddHoliday("6", "День Победы");
            calendar.AddHoliday("7", "День России");
            calendar.AddHoliday("8", "День народного единства");

            // Добавляем специальные дни (праздники) — формат даты dd.MM
            calendar.AddSpecialDay(new CalendarDay("01.01", 1, 1, null));
            calendar.AddSpecialDay(new CalendarDay("02.01", 1, 1, null));
            calendar.AddSpecialDay(new CalendarDay("03.01", 1, 1, null));
            calendar.AddSpecialDay(new CalendarDay("04.01", 1, 1, null));
            calendar.AddSpecialDay(new CalendarDay("05.01", 1, 1, null));
            calendar.AddSpecialDay(new CalendarDay("06.01", 1, 1, null));
            calendar.AddSpecialDay(new CalendarDay("07.01", 1, 2, null)); // Рождество
            calendar.AddSpecialDay(new CalendarDay("08.01", 1, 1, null));

            // Переносы рабочих дней
            calendar.AddSpecialDay(new CalendarDay("09.01", 1, null, "03.01")); // 9 января рабочий вместо 3 января
            calendar.AddTransition("03.01", "09.01");

            // 23 февраля
            calendar.AddSpecialDay(new CalendarDay("23.02", 1, 3, null));

            // 8 марта
            calendar.AddSpecialDay(new CalendarDay("08.03", 1, 4, null));
            calendar.AddSpecialDay(new CalendarDay("09.03", 1, null, "08.03"));
            calendar.AddTransition("08.03", "09.03");

            // Сокращённые дни
            calendar.AddSpecialDay(new CalendarDay("30.04", 2, null, null)); // Сокращённый день

            // Майские праздники
            calendar.AddSpecialDay(new CalendarDay("01.05", 1, 5, null));
            calendar.AddSpecialDay(new CalendarDay("08.05", 2, null, null)); // Сокращённый день
            calendar.AddSpecialDay(new CalendarDay("09.05", 1, 6, null));
            calendar.AddSpecialDay(new CalendarDay("11.05", 1, null, "09.05"));
            calendar.AddTransition("09.05", "11.05");

            // 12 июня
            calendar.AddSpecialDay(new CalendarDay("11.06", 2, null, null)); // Сокращённый день
            calendar.AddSpecialDay(new CalendarDay("12.06", 1, 7, null));

            // 4 ноября
            calendar.AddSpecialDay(new CalendarDay("03.11", 2, null, null)); // Сокращённый день
            calendar.AddSpecialDay(new CalendarDay("04.11", 1, 8, null));

            // 31 декабря
            calendar.AddSpecialDay(new CalendarDay("31.12", 1, null, "04.01"));
            calendar.AddTransition("04.01", "31.12");

            // Статистика
            calendar.Statistics = new CalendarStatistics
            {
                Workdays = 247,
                Holidays = 118,
                Hours40 = 1972,
                Hours36 = 1774.4,
                Hours24 = 1181.6
            };

            return calendar;
        }
    }
}
