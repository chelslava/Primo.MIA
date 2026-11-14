// =============================================================================
// GeneratorsLogic.cs — чистая логика генераторов (без SDK-зависимостей).
//
// Выделено из Utilities/GeneratorsBack.cs для тестируемости:
//   - FormatGuid        — форматирование GUID (N/D/B/P/X)
//   - NormalizeExtension — точка перед расширением (txt → .txt)
//   - BuildFilePath     — сборка полного пути файла
//   - ParseRange        — разбор Min/Max с дефолтами и валидацией
//   - FormatTimestamp   — все форматы TimestampFormat (+Quarter/Week/Unix/Custom)
// =============================================================================

using System;
using System.Globalization;

using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Чистые функции генераторов данных. Никакого SDK и глобального состояния.
    /// </summary>
    public static class GeneratorsLogic
    {
        /// <summary>
        /// Форматирует GUID в выбранном формате (N/D/B/P/X).
        /// </summary>
        public static string FormatGuid(Guid guid, GuidFormat format)
        {
            switch (format)
            {
                case GuidFormat.N: return guid.ToString("N"); // 32 цифры без дефисов
                case GuidFormat.B: return guid.ToString("B"); // {с фигурными скобками}
                case GuidFormat.P: return guid.ToString("P"); // (с круглыми скобками)
                case GuidFormat.X: return guid.ToString("X"); // {0x...,0x...,{0x...}}
                case GuidFormat.D:
                default: return guid.ToString("D"); // стандарт с дефисами
            }
        }

        /// <summary>
        /// Нормализует расширение: добавляет ведущую точку, если её нет ("txt" → ".txt").
        /// null/пустая строка → пустая строка.
        /// </summary>
        public static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return string.Empty;

            return extension.StartsWith(".") ? extension : "." + extension;
        }

        /// <summary>
        /// Собирает полный путь файла из директории, базового имени и расширения.
        /// Пустая директория → только имя + расширение.
        /// </summary>
        public static string BuildFilePath(string directory, string baseName, string extension)
        {
            string normalizedExt = NormalizeExtension(extension);
            string fileName = (baseName ?? "file") + normalizedExt;

            return string.IsNullOrEmpty(directory)
                ? fileName
                : System.IO.Path.Combine(directory, fileName);
        }

        /// <summary>
        /// Разбирает Min/Max из строк; при ошибке парсинга — дефолты (0 и 100).
        /// Мин больше макса → ArgumentException.
        /// </summary>
        public static void ParseRange(string minStr, string maxStr, out int min, out int max)
        {
            min = int.TryParse(minStr, out int parsedMin) ? parsedMin : 0;
            max = int.TryParse(maxStr, out int parsedMax) ? parsedMax : 100;

            Guard.That(min <= max, $"Минимальное ({min}) не может быть больше максимального ({max})");
        }

        /// <summary>
        /// Форматирует дату/время по формату TimestampFormat.
        /// Special-форматы (Quarter, WeekNumber — ISO 8601, UnixTimestamp) вычисляются вручную.
        /// FullDate — по культуре машины (день недели на языке пользователя).
        /// Custom требует непустой customFormat.
        /// Числовые/буквенные шаблоны — инвариантная культура (детерминированный вывод).
        /// </summary>
        public static string FormatTimestamp(DateTime now, TimestampFormat format, string customFormat = null)
        {
            switch (format)
            {
                // ── Компактные (для имён файлов) ──────────────────────────────────
                case TimestampFormat.yyyyMMdd_HHmmss: return now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                case TimestampFormat.yyyyMMdd_HHmm: return now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);
                case TimestampFormat.yyyyMMdd: return now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                case TimestampFormat.yyyyMM: return now.ToString("yyyyMM", CultureInfo.InvariantCulture);
                case TimestampFormat.yyyyMMddHHmmssfff: return now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);

                // ── ISO 8601 (для БД и API) ────────────────────────────────────────
                case TimestampFormat.yyyy_MM_dd: return now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                case TimestampFormat.yyyy_MM_dd_HH_mm_ss: return now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                case TimestampFormat.ISO8601: return now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                case TimestampFormat.ISO8601_ms: return now.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);

                // ── Российские (для отчётов) ───────────────────────────────────────
                case TimestampFormat.dd_MM_yyyy: return now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
                case TimestampFormat.dd_MM_yyyy_HH_mm_ss: return now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                case TimestampFormat.dd_MM_yyyy_HH_mm: return now.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
                case TimestampFormat.dd_MM_yyyy_slash: return now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

                // ── Американские ──────────────────────────────────────────────────
                case TimestampFormat.MM_dd_yyyy: return now.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                case TimestampFormat.MM_dd_yyyy_HH_mm_ss: return now.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                // ── Только время ──────────────────────────────────────────────────
                case TimestampFormat.HHmmss: return now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                case TimestampFormat.HHmm: return now.ToString("HH:mm", CultureInfo.InvariantCulture);
                case TimestampFormat.HHmmss_fff: return now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

                // ── Специальные ───────────────────────────────────────────────────
                case TimestampFormat.FullDate:
                    // Полная дата с днём недели — зависит от культуры системы
                    return now.ToString("D");

                case TimestampFormat.Quarter:
                    // Q1-2025, Q2-2025, Q3-2025, Q4-2025
                    int quarter = (now.Month - 1) / 3 + 1;
                    return $"Q{quarter}-{now.Year}";

                case TimestampFormat.WeekNumber:
                    // W07-2025 — номер недели по ISO 8601 (понедельник = начало недели)
                    int weekNum = CultureInfo.InvariantCulture.Calendar
                        .GetWeekOfYear(now,
                            CalendarWeekRule.FirstFourDayWeek,
                            DayOfWeek.Monday);
                    return $"W{weekNum:D2}-{now.Year}";

                case TimestampFormat.UnixTimestamp:
                    // Секунды с 01.01.1970 00:00:00 UTC
                    long unixSeconds = (long)(now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                    return unixSeconds.ToString(CultureInfo.InvariantCulture);

                case TimestampFormat.Custom:
                    Guard.That(!string.IsNullOrWhiteSpace(customFormat),
                        "Кастомный формат даты не может быть пустым при выборе Custom");
                    return now.ToString(customFormat);

                default:
                    return now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            }
        }
    }
}
