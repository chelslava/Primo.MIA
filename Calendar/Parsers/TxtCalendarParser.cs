// =============================================================================
// TxtCalendarParser.cs — парсер TXT формата производственного календаря.
//
// Парсит TXT файлы с сайта xmlcalendar.ru:
// - Каждая строка — одна дата выходного/праздничного дня
// - Формат даты: yyyy.MM.dd
// - Только выходные и праздничные дни (рабочие дни не указаны)
//
// Пример структуры:
// 2026.01.01
// 2026.01.03
// 2026.01.04
// ...
//
// Ограничения TXT формата:
// - Не содержит информации о переносах рабочих дней
// - Не содержит названий праздников
// - Не различает праздники и обычные выходные
// =============================================================================

using System;
using System.Globalization;
using Primo.MIA.Calendar.Models;

namespace Primo.MIA.Calendar.Parsers
{
    /// <summary>
    /// Парсер TXT формата производственного календаря.
    /// </summary>
    public class TxtCalendarParser : ICalendarParser
    {
        /// <summary>
        /// Поддерживаемый формат файла.
        /// </summary>
        public global::Primo.MIA.CalendarFormat SupportedFormat => global::Primo.MIA.CalendarFormat.Txt;

        /// <summary>
        /// Парсит содержимое TXT файла календаря.
        /// </summary>
        /// <param name="content">Содержимое TXT файла</param>
        /// <returns>Модель производственного календаря</returns>
        /// <exception cref="CalendarParseException">
        /// Выбрасывается при ошибке парсинга TXT
        /// </exception>
        public ProductionCalendar Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new CalendarParseException("Содержимое файла календаря пустое", global::Primo.MIA.CalendarFormat.Txt);
            }

            try
            {
                ProductionCalendar calendar = new ProductionCalendar();
                string[] lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                int workdaysCount = 0;
                int holidaysCount = 0;

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();

                    if (string.IsNullOrEmpty(trimmedLine))
                    {
                        continue;
                    }

                    // Парсим дату в формате yyyy.MM.dd
                    if (TryParseDate(trimmedLine, out DateTime date))
                    {
                        // Устанавливаем год из первой даты
                        if (calendar.Year == 0)
                        {
                            calendar.Year = date.Year;
                        }

                        // Формируем ключ в формате dd.MM
                        string dateKey = date.ToString("dd.MM");

                        // Проверяем, не добавлен ли уже этот день
                        if (!calendar.SpecialDays.ContainsKey(dateKey))
                        {
                            // Добавляем как выходной день (type=1)
                            CalendarDay calendarDay = new CalendarDay(dateKey, 1, null, null);
                            calendar.AddSpecialDay(calendarDay);
                            holidaysCount++;
                        }
                    }
                }

                // Вычисляем примерное количество рабочих дней.
                // ВАЖНО: TXT формат содержит только список праздничных/выходных дней.
                // Формула totalDays - holidaysCount даёт приблизительное значение,
                // так как не учитывает регулярные выходные (суббота/воскресенье),
                // которые не указаны в файле. Для точного подсчёта используйте XML/JSON/CSV.
                // Поэтому статистика по рабочим дням не вычисляется для TXT формата.
                if (calendar.Year > 0)
                {
                    int totalDays = DateTime.IsLeapYear(calendar.Year) ? 366 : 365;
                    workdaysCount = totalDays - holidaysCount;
                }

                // Устанавливаем статистику.
                // Примечание: Workdays и Hours* не вычисляются для TXT формата,
                // так как файл содержит только праздничные дни без информации
                // о регулярных выходных и переноса рабочих дней.
                calendar.Statistics = new CalendarStatistics
                {
                    Workdays = 0, // Невозможно точно определить из TXT формата
                    Holidays = holidaysCount,
                    Hours40 = 0,
                    Hours36 = 0,
                    Hours24 = 0
                };

                return calendar;
            }
            catch (CalendarParseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CalendarParseException(
                    $"Ошибка парсинга TXT: {ex.Message}",
                    global::Primo.MIA.CalendarFormat.Txt,
                    ex);
            }
        }

        /// <summary>
        /// Пытается распарсить дату в формате yyyy.MM.dd.
        /// </summary>
        /// <param name="dateStr">Строка с датой</param>
        /// <param name="date">Распарсенная дата</param>
        /// <returns>true если парсинг успешен</returns>
        private bool TryParseDate(string dateStr, out DateTime date)
        {
            date = DateTime.MinValue;

            if (string.IsNullOrWhiteSpace(dateStr))
            {
                return false;
            }

            // Пробуем формат yyyy.MM.dd
            if (DateTime.TryParseExact(dateStr, "yyyy.MM.dd", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return true;
            }

            // Пробуем стандартный парсинг как fallback
            return DateTime.TryParse(dateStr, out date);
        }
    }
}
