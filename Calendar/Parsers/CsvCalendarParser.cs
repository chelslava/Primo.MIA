// =============================================================================
// CsvCalendarParser.cs — парсер CSV формата производственного календаря.
//
// Парсит CSV файлы с сайта xmlcalendar.ru:
// - Первая строка — заголовки месяцев
// - Вторая строка — год и дни через запятую
//
// Пример структуры:
// "Год/Месяц","Январь","Февраль","Март",...
// 2026,"1,2,3,4,5,6,7,8,9+,10,11,17,18,24,25,31","1,7,8,14,15,21,22,23,28",...
//
// Суффиксы дней:
//   + — сокращённый рабочий день
//   * — перенос выходного на этот день
//
// ИСПРАВЛЕНИЯ:
//   [БАГ-4] Условие if (lines.Length >= 2) для ParseStatistics удалено — оно всегда
//           было true (выше уже проверено lines.Length < 2 с выбросом исключения).
//           ParseStatistics теперь вызывается безусловно.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using Primo.MIA.Calendar.Models;

namespace Primo.MIA.Calendar.Parsers
{
    /// <summary>Парсер CSV формата производственного календаря.</summary>
    public class CsvCalendarParser : ICalendarParser
    {
        /// <summary>Поддерживаемый формат файла.</summary>
        public global::Primo.MIA.CalendarFormat SupportedFormat =>
            global::Primo.MIA.CalendarFormat.Csv;

        /// <summary>Названия месяцев на русском для определения индекса колонки.</summary>
        private static readonly string[] RussianMonthNames =
        {
            "Январь",  "Февраль", "Март",    "Апрель",
            "Май",     "Июнь",    "Июль",    "Август",
            "Сентябрь","Октябрь", "Ноябрь",  "Декабрь"
        };

        /// <summary>
        /// Парсит содержимое CSV файла календаря.
        /// </summary>
        /// <param name="content">Содержимое CSV файла.</param>
        /// <returns>Модель производственного календаря.</returns>
        /// <exception cref="CalendarParseException">При ошибке парсинга.</exception>
        public ProductionCalendar Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new CalendarParseException(
                    "Содержимое файла календаря пустое",
                    global::Primo.MIA.CalendarFormat.Csv);

            try
            {
                string[] lines = content.Split(
                    new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length < 2)
                    throw new CalendarParseException(
                        "CSV файл должен содержать минимум 2 строки (заголовок и данные)",
                        global::Primo.MIA.CalendarFormat.Csv);

                // Парсим заголовок для определения соответствия колонок месяцам
                List<int> monthColumns = ParseHeader(lines[0]);

                // Парсим строку с данными (год + дни по месяцам)
                ProductionCalendar calendar = ParseDataLine(lines[1], monthColumns);

                // [БАГ-4] Условие lines.Length >= 2 удалено — оно всегда было истинным,
                //         т.к. выше уже проверено lines.Length < 2 с выбросом исключения.
                //         ParseStatistics вызывается безусловно.
                ParseStatistics(lines[1], calendar);

                return calendar;
            }
            catch (CalendarParseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CalendarParseException(
                    $"Ошибка парсинга CSV: {ex.Message}",
                    global::Primo.MIA.CalendarFormat.Csv,
                    ex);
            }
        }

        /// <summary>
        /// Парсит заголовок CSV и возвращает список индексов месяцев для каждой колонки.
        /// Использует LINQ для поиска названия месяца.
        /// </summary>
        private List<int> ParseHeader(string headerLine)
        {
            var    result  = new List<int>();
            string[] headers = ParseCsvLine(headerLine);

            foreach (string raw in headers)
            {
                string header = raw.Trim().Trim('"');

                // LINQ-поиск: ищем название месяца по началу заголовка
                int monthIndex = Array.FindIndex(RussianMonthNames, m =>
                    header.StartsWith(m, StringComparison.OrdinalIgnoreCase));

                // 1–12 для месяцев, 0 для не-месяцев (год, статистика и т.п.)
                result.Add(monthIndex >= 0 ? monthIndex + 1 : 0);
            }

            return result;
        }

        /// <summary>
        /// Парсит строку с данными и заполняет календарь.
        /// </summary>
        private ProductionCalendar ParseDataLine(string dataLine, List<int> monthColumns)
        {
            var      calendar = new ProductionCalendar();
            string[] values   = ParseCsvLine(dataLine);

            for (int i = 0; i < values.Length && i < monthColumns.Count; i++)
            {
                int    month = monthColumns[i];
                string value = values[i].Trim().Trim('"');

                if (month == 0)
                {
                    // Не колонка месяца — проверяем на год (первая колонка)
                    if (i == 0 && int.TryParse(value, out int year))
                        calendar.Year = year;
                    continue;
                }

                // Парсим дни текущего месяца
                ParseMonthDays(calendar, month, value);
            }

            return calendar;
        }

        /// <summary>
        /// Парсит строку дней месяца и добавляет специальные дни в календарь.
        /// Суффикс + — сокращённый рабочий, суффикс * — перенесённый рабочий.
        /// </summary>
        private void ParseMonthDays(ProductionCalendar calendar, int month, string daysString)
        {
            if (string.IsNullOrWhiteSpace(daysString))
                return;

            string[] dayParts = daysString.Split(
                new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string dayPart in dayParts)
            {
                string dayStr = dayPart.Trim();
                if (string.IsNullOrEmpty(dayStr))
                    continue;

                int    type          = 1;      // По умолчанию — выходной
                string dayNumberStr  = dayStr;
                string transferFrom  = null;

                if (dayStr.EndsWith("+"))
                {
                    // Сокращённый рабочий день
                    type         = 2;
                    dayNumberStr = dayStr.Substring(0, dayStr.Length - 1);
                }
                else if (dayStr.EndsWith("*"))
                {
                    // Перенесённый рабочий день (маркер — конкретной даты нет в CSV)
                    dayNumberStr = dayStr.Substring(0, dayStr.Length - 1);
                    transferFrom = "*"; // маркер наличия переноса
                }

                if (!int.TryParse(dayNumberStr, out int dayNumber))
                    continue;

                string dateKey    = $"{dayNumber:D2}.{month:D2}";
                var    calendarDay = new CalendarDay(dateKey, type, null, transferFrom);
                calendar.AddSpecialDay(calendarDay);

                // Для переноса со звёздочкой регистрируем также в обратном словаре
                if (transferFrom == "*")
                    calendar.TransferWorkdays[dateKey] = "*";
            }
        }

        /// <summary>
        /// Парсит статистику из строки данных (последние колонки CSV).
        /// Ожидаемый порядок последних 5 колонок:
        ///   -5: рабочих дней, -4: праздничных/выходных, -3: часов 40ч, -2: 36ч, -1: 24ч
        /// </summary>
        private void ParseStatistics(string dataLine, ProductionCalendar calendar)
        {
            string[] values = ParseCsvLine(dataLine);

            // Минимум: 12 месяцев + 1 колонка года + 4 колонки статистики = 17
            if (values.Length < 17)
                return;

            try
            {
                int len = values.Length;
                calendar.Statistics = new CalendarStatistics
                {
                    Workdays = ParseIntValue(values,    len - 5),
                    Holidays = ParseIntValue(values,    len - 4),
                    Hours40  = ParseDoubleValue(values, len - 3),
                    Hours36  = ParseDoubleValue(values, len - 2),
                    Hours24  = ParseDoubleValue(values, len - 1)
                };
            }
            catch
            {
                // Статистика опциональна — игнорируем ошибки парсинга.
                // Файл может не содержать колонок со статистикой.
            }
        }

        /// <summary>Безопасно парсит целое число из массива значений по индексу.</summary>
        private int ParseIntValue(string[] values, int index)
        {
            if (index < 0 || index >= values.Length)
                return 0;
            string v = values[index].Trim().Trim('"');
            return int.TryParse(v, out int result) ? result : 0;
        }

        /// <summary>Безопасно парсит дробное число из массива значений по индексу.</summary>
        private double ParseDoubleValue(string[] values, int index)
        {
            if (index < 0 || index >= values.Length)
                return 0;
            string v = values[index].Trim().Trim('"');
            return double.TryParse(v, NumberStyles.Any,
                CultureInfo.InvariantCulture, out double result) ? result : 0;
        }

        /// <summary>
        /// Парсит строку CSV с учётом кавычек.
        /// Поля внутри кавычек могут содержать запятые — они не считаются разделителями.
        /// </summary>
        private string[] ParseCsvLine(string line)
        {
            var result     = new List<string>();
            bool inQuotes  = false;
            int  startIdx  = 0;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(line.Substring(startIdx, i - startIdx));
                    startIdx = i + 1;
                }
            }

            // Последняя колонка
            result.Add(line.Substring(startIdx));
            return result.ToArray();
        }
    }
}
