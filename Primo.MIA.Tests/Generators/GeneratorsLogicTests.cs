// =============================================================================
// GeneratorsLogicTests.cs — тесты чистой логики генераторов (GeneratorsLogic).
//
// Покрывает:
//   - FormatGuid: все форматы GUID (N/D/B/P/X) на фиксированном значении
//   - NormalizeExtension: точка перед расширением
//   - BuildFilePath: сборка пути с директорией и без
//   - ParseRange: дефолты при мусорном вводе, ошибка min > max
//   - FormatTimestamp: точный вывод всех групп форматов на фиксированной дате
//     (компактные, ISO 8601, RU/US, время, Quarter, WeekNumber, Unix, Custom)
//
// Все проверки детерминированы: фиксированный DateTime, инвариантная культура.
// =============================================================================

using System;
using System.Globalization;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Generators
{
    /// <summary>Тесты чистой логики генераторов данных.</summary>
        [Trait("Category", "Fast")]
    public class GeneratorsLogicTests
    {
        /// <summary>Фиксированный GUID для точных проверок форматов.</summary>
        private static readonly Guid FixedGuid = new Guid("01234567-89ab-cdef-0123-456789abcdef");

        /// <summary>Фиксированный момент: 15 марта 2024, 09:07:05.123 (локальное время).</summary>
        private static readonly DateTime FixedNow = new DateTime(2024, 3, 15, 9, 7, 5, 123);

        // =====================================================================
        // FormatGuid
        // =====================================================================

        [Theory(DisplayName = "FormatGuid: все форматы на фиксированном GUID")]
        [InlineData("N", "0123456789abcdef0123456789abcdef")]
        [InlineData("D", "01234567-89ab-cdef-0123-456789abcdef")]
        [InlineData("B", "{01234567-89ab-cdef-0123-456789abcdef}")]
        [InlineData("P", "(01234567-89ab-cdef-0123-456789abcdef)")]
        public void FormatGuid_KnownFormats_ReturnsExactString(string formatName, string expected)
        {
            // Arrange
            var format = (GuidFormat)Enum.Parse(typeof(GuidFormat), formatName);

            // Act
            string result = GeneratorsLogic.FormatGuid(FixedGuid, format);

            // Assert
            result.Should().Be(expected);
        }

        [Fact(DisplayName = "FormatGuid: неизвестный формат → D (по умолчанию)")]
        public void FormatGuid_UnknownFormat_FallsBackToD()
        {
            // Act
            string result = GeneratorsLogic.FormatGuid(FixedGuid, (GuidFormat)999);

            // Assert
            result.Should().Be("01234567-89ab-cdef-0123-456789abcdef");
        }

        [Fact(DisplayName = "FormatGuid: X-формат — 4 группы, последняя как массив байтов")]
        public void FormatGuid_XFormat_ExactString()
        {
            // Act
            string result = GeneratorsLogic.FormatGuid(FixedGuid, GuidFormat.X);

            // Assert — int×3 + byte[8] в фигурных скобках
            result.Should().Be("{0x01234567,0x89ab,0xcdef,{0x01,0x23,0x45,0x67,0x89,0xab,0xcd,0xef}}");
        }

        // =====================================================================
        // NormalizeExtension / BuildFilePath
        // =====================================================================

        [Theory(DisplayName = "NormalizeExtension: точка добавляется при отсутствии")]
        [InlineData(".txt", ".txt")]
        [InlineData("txt", ".txt")]
        [InlineData(".csv", ".csv")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void NormalizeExtension_VariousInputs_ReturnsNormalized(string input, string expected)
        {
            // Act + Assert
            GeneratorsLogic.NormalizeExtension(input).Should().Be(expected);
        }

        [Fact(DisplayName = "BuildFilePath: с директорией — Combine")]
        public void BuildFilePath_WithDirectory_Combines()
        {
            // Act
            string result = GeneratorsLogic.BuildFilePath(@"C:\Data", "report", ".xlsx");

            // Assert
            result.Should().Be(System.IO.Path.Combine(@"C:\Data", "report.xlsx"));
        }

        [Theory(DisplayName = "BuildFilePath: без директории — только имя")]
        [InlineData("file", "txt", "file.txt")]
        [InlineData("file", null, "file")]
        [InlineData(null, "txt", "file.txt")]
        public void BuildFilePath_WithoutDirectory_ReturnsFileNameOnly(string baseName, string ext, string expected)
        {
            // Act
            string result = GeneratorsLogic.BuildFilePath("", baseName, ext);

            // Assert
            result.Should().Be(expected);
        }

        // =====================================================================
        // ParseRange
        // =====================================================================

        [Fact(DisplayName = "ParseRange: корректные строки → значения")]
        public void ParseRange_ValidStrings_ReturnsValues()
        {
            // Act
            GeneratorsLogic.ParseRange("10", "50", out int min, out int max);

            // Assert
            min.Should().Be(10);
            max.Should().Be(50);
        }

        [Fact(DisplayName = "ParseRange: мусорный ввод → дефолты 0 и 100")]
        public void ParseRange_GarbageInput_ReturnsDefaults()
        {
            // Act
            GeneratorsLogic.ParseRange("abc", "", out int min, out int max);

            // Assert
            min.Should().Be(0);
            max.Should().Be(100);
        }

        [Fact(DisplayName = "ParseRange: null → дефолты 0 и 100")]
        public void ParseRange_Nulls_ReturnsDefaults()
        {
            // Act
            GeneratorsLogic.ParseRange(null, null, out int min, out int max);

            // Assert
            min.Should().Be(0);
            max.Should().Be(100);
        }

        [Fact(DisplayName = "ParseRange: min > max → ArgumentException")]
        public void ParseRange_MinGreaterThanMax_Throws()
        {
            // Act
            Action act = () => GeneratorsLogic.ParseRange("50", "10", out _, out _);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Минимальное (50)*максимального (10)*");
        }

        // =====================================================================
        // FormatTimestamp — фиксированный момент 2024-03-15 09:07:05.123
        // =====================================================================

        [Theory(DisplayName = "FormatTimestamp: компактные форматы (для имён файлов)")]
        [InlineData(TimestampFormat.yyyyMMdd_HHmmss, "20240315_090705")]
        [InlineData(TimestampFormat.yyyyMMdd_HHmm, "20240315_0907")]
        [InlineData(TimestampFormat.yyyyMMdd, "20240315")]
        [InlineData(TimestampFormat.yyyyMM, "202403")]
        [InlineData(TimestampFormat.yyyyMMddHHmmssfff, "20240315090705123")]
        public void FormatTimestamp_CompactFormats_ReturnsExactString(TimestampFormat format, string expected)
        {
            // Act + Assert
            GeneratorsLogic.FormatTimestamp(FixedNow, format).Should().Be(expected);
        }

        [Theory(DisplayName = "FormatTimestamp: ISO 8601")]
        [InlineData(TimestampFormat.yyyy_MM_dd, "2024-03-15")]
        [InlineData(TimestampFormat.yyyy_MM_dd_HH_mm_ss, "2024-03-15 09:07:05")]
        [InlineData(TimestampFormat.ISO8601, "2024-03-15T09:07:05")]
        [InlineData(TimestampFormat.ISO8601_ms, "2024-03-15T09:07:05.123")]
        public void FormatTimestamp_IsoFormats_ReturnsExactString(TimestampFormat format, string expected)
        {
            // Act + Assert
            GeneratorsLogic.FormatTimestamp(FixedNow, format).Should().Be(expected);
        }

        [Theory(DisplayName = "FormatTimestamp: RU и US форматы дат")]
        [InlineData(TimestampFormat.dd_MM_yyyy, "15.03.2024")]
        [InlineData(TimestampFormat.dd_MM_yyyy_HH_mm_ss, "15.03.2024 09:07:05")]
        [InlineData(TimestampFormat.dd_MM_yyyy_HH_mm, "15.03.2024 09:07")]
        [InlineData(TimestampFormat.dd_MM_yyyy_slash, "15/03/2024")]
        [InlineData(TimestampFormat.MM_dd_yyyy, "03/15/2024")]
        [InlineData(TimestampFormat.MM_dd_yyyy_HH_mm_ss, "03/15/2024 09:07:05")]
        public void FormatTimestamp_RegionalFormats_ReturnsExactString(TimestampFormat format, string expected)
        {
            // Act + Assert
            GeneratorsLogic.FormatTimestamp(FixedNow, format).Should().Be(expected);
        }

        [Theory(DisplayName = "FormatTimestamp: форматы времени")]
        [InlineData(TimestampFormat.HHmmss, "09:07:05")]
        [InlineData(TimestampFormat.HHmm, "09:07")]
        [InlineData(TimestampFormat.HHmmss_fff, "09:07:05.123")]
        public void FormatTimestamp_TimeOnlyFormats_ReturnsExactString(TimestampFormat format, string expected)
        {
            // Act + Assert
            GeneratorsLogic.FormatTimestamp(FixedNow, format).Should().Be(expected);
        }

        [Fact(DisplayName = "FormatTimestamp: Quarter — Q1 для марта")]
        public void FormatTimestamp_Quarter_March_IsQ1()
        {
            // Act + Assert
            GeneratorsLogic.FormatTimestamp(FixedNow, TimestampFormat.Quarter).Should().Be("Q1-2024");
        }

        [Theory(DisplayName = "FormatTimestamp: Quarter — границы кварталов")]
        [InlineData(1, "Q1")]
        [InlineData(3, "Q1")]
        [InlineData(4, "Q2")]
        [InlineData(6, "Q2")]
        [InlineData(7, "Q3")]
        [InlineData(9, "Q3")]
        [InlineData(10, "Q4")]
        [InlineData(12, "Q4")]
        public void FormatTimestamp_Quarter_AllBoundaries(int month, string expectedPrefix)
        {
            // Arrange
            var date = new DateTime(2024, month, 15);

            // Act + Assert
            GeneratorsLogic.FormatTimestamp(date, TimestampFormat.Quarter)
                .Should().StartWith(expectedPrefix).And.EndWith("-2024");
        }

        [Fact(DisplayName = "FormatTimestamp: WeekNumber — ISO-неделя детерминирована")]
        public void FormatTimestamp_WeekNumber_IsoDeterministic()
        {
            // Arrange — 2024-03-15 по ISO 8601 = 11-я неделя (W1 начинается 2024-01-01, пн)
            var isoDate = new DateTime(2024, 3, 15);

            // Act
            string result = GeneratorsLogic.FormatTimestamp(isoDate, TimestampFormat.WeekNumber);

            // Assert — формат W{неделя:00}-{год}, совпадает с ISO
            result.Should().Be("W11-2024");
        }

        [Fact(DisplayName = "FormatTimestamp: UnixTimestamp — UtcKind детерминирован")]
        public void FormatTimestamp_UnixTimestamp_UtcKindDeterministic()
        {
            // Arrange — известная эпоха: 2024-01-01 00:00:00 UTC = 1704067200
            var utcMoment = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Act
            string result = GeneratorsLogic.FormatTimestamp(utcMoment, TimestampFormat.UnixTimestamp);

            // Assert
            result.Should().Be("1704067200");
        }

        [Fact(DisplayName = "FormatTimestamp: Custom — произвольный шаблон")]
        public void FormatTimestamp_CustomFormat_Applies()
        {
            // Act + Assert
            GeneratorsLogic.FormatTimestamp(FixedNow, TimestampFormat.Custom, "yyyy|MM|dd")
                .Should().Be("2024|03|15");
        }

        [Fact(DisplayName = "FormatTimestamp: Custom с пустым шаблоном → ошибка")]
        public void FormatTimestamp_CustomEmptyFormat_Throws()
        {
            // Act
            Action act = () => GeneratorsLogic.FormatTimestamp(FixedNow, TimestampFormat.Custom, " ");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Кастомный формат*");
        }

        [Fact(DisplayName = "FormatTimestamp: инвариантная культура — не зависит от локали ОС")]
        public void FormatTimestamp_NumberPatterns_CultureInvariant()
        {
            // Arrange — "f"/"F"-подобные шаблоны могли бы дать запятую на RU-локали
            string result = GeneratorsLogic.FormatTimestamp(FixedNow, TimestampFormat.Custom, "yyyy.MM.dd HH:mm");

            // Assert
            result.Should().Be("2024.03.15 09:07");
        }

        [Fact(DisplayName = "FormatTimestamp: ISO8601_ms сохраняет миллисекунды")]
        public void FormatTimestamp_Iso8601Ms_IncludesMilliseconds()
        {
            // Act
            string result = GeneratorsLogic.FormatTimestamp(FixedNow, TimestampFormat.ISO8601_ms);

            // Assert
            result.Should().EndWith(".123");
        }
    }
}
