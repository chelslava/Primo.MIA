// =============================================================================
// ProductionCalendar.cs — модель производственного календаря.
//
// ИСПРАВЛЕНИЯ БАГОВ:
//   [БАГ-1] IsWorkday: GetDayType вызывался дважды — результат теперь кэшируется.
//   [БАГ-2] IsTransferredWorkday: Transitions.Any(O(n)) заменён на словарь
//           TransferWorkdays с O(1) поиском. AddTransition теперь заполняет оба словаря.
//   [БАГ-3] GetDayType: логика переноса унифицирована — сначала проверяется TransferWorkdays,
//           затем SpecialDays без дублирующей ветки IsTransferredWorkday внутри SpecialDays.
//   [БАГ-5] AddWorkdays/SubtractWorkdays: при days=0 возвращается входной день явно и
//           быстро без входа в цикл. Поведение задокументировано.
//
// НОВЫЕ ВОЗМОЖНОСТИ:
//   [П3] GetNextWorkday(date) / GetPrevWorkday(date) — найти ближайший рабочий день.
//   [П3] GetWorkdaysBetween уже был, добавлен XML-комментарий уточнения.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA.Calendar.Models
{
    /// <summary>
    /// Модель производственного календаря.
    /// Содержит информацию о праздниках, выходных, переносах и статистике за год.
    /// </summary>
    public class ProductionCalendar
    {
        /// <summary>Год календаря.</summary>
        public int Year { get; set; }

        /// <summary>Код страны (ru, kz, by, uz).</summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Словарь праздников: ключ — ID праздника (строка), значение — название.
        /// Например: { "1": "Новогодние каникулы", "2": "Рождество Христово" }
        /// </summary>
        public Dictionary<string, string> Holidays { get; set; }

        /// <summary>
        /// Специальные дни: ключ — дата в формате dd.MM, значение — информация о дне.
        /// Содержит праздники, выходные, сокращённые дни и переносы.
        /// </summary>
        public Dictionary<string, CalendarDay> SpecialDays { get; set; }

        /// <summary>
        /// Переносы: ключ — дата выходного (dd.MM), значение — дата рабочего дня.
        /// Например: { "01.03": "09.01" } — 1 марта стало выходным, 9 января стало рабочим.
        /// Используется для быстрого определения, стал ли день выходным из-за переноса.
        /// </summary>
        public Dictionary<string, string> Transitions { get; set; }

        /// <summary>
        /// [БАГ-2] Обратный словарь переносов: ключ — дата рабочего дня (dd.MM),
        /// значение — дата выходного, откуда был перенесён этот рабочий день.
        /// Обеспечивает O(1) поиск вместо O(n) перебора Transitions.
        /// Заполняется автоматически в методе AddTransition.
        /// </summary>
        public Dictionary<string, string> TransferWorkdays { get; set; }

        /// <summary>Статистика по году: рабочие дни, праздники, рабочие часы.</summary>
        public CalendarStatistics Statistics { get; set; }

        /// <summary>Конструктор по умолчанию.</summary>
        public ProductionCalendar()
        {
            Year            = DateTime.Now.Year;
            CountryCode     = "ru";
            Holidays        = new Dictionary<string, string>();
            SpecialDays     = new Dictionary<string, CalendarDay>();
            Transitions     = new Dictionary<string, string>();
            TransferWorkdays = new Dictionary<string, string>(); // [БАГ-2]
            Statistics      = new CalendarStatistics();
        }

        // =========================================================================
        // МЕТОДЫ ПРОВЕРКИ ТИПА ДНЯ
        // =========================================================================

        /// <summary>
        /// Проверяет, является ли указанная дата рабочим днём.
        /// Рабочим считается как полный рабочий день, так и сокращённый.
        /// [БАГ-1] GetDayType больше не вызывается дважды.
        /// </summary>
        public bool IsWorkday(DateTime date)
        {
            // [БАГ-1] Кэшируем результат в локальную переменную — один вызов вместо двух
            DayType dayType = GetDayType(date);
            return dayType == DayType.Workday || dayType == DayType.ShortWorkday;
        }

        /// <summary>
        /// Возвращает тип дня для указанной даты.
        /// [БАГ-3] Логика переноса унифицирована: сначала TransferWorkdays (O(1)),
        ///         затем SpecialDays без дублирующей проверки IsTransferredWorkday внутри.
        /// </summary>
        public DayType GetDayType(DateTime date)
        {
            string dateKey = date.ToString("dd.MM");

            // [БАГ-3] Шаг 1: проверяем, является ли день рабочим из-за переноса — O(1)
            // Этот шаг должен быть ПЕРВЫМ, т.к. XmlParser добавляет такой день и в SpecialDays,
            // и в TransferWorkdays. Если проверять SpecialDays первым — тип будет определён
            // по IsTransferredWorkday внутри CalendarDay, а не по единому источнику истины.
            if (TransferWorkdays.ContainsKey(dateKey))
                return DayType.Workday;

            // Шаг 2: проверяем специальные дни (праздники, выходные, сокращённые)
            if (SpecialDays.TryGetValue(dateKey, out CalendarDay calendarDay))
            {
                // Сокращённый рабочий день (type=2)
                if (calendarDay.IsShortWorkday) return DayType.ShortWorkday;

                // Праздничный день (есть HolidayId)
                if (calendarDay.IsHoliday) return DayType.Holiday;

                // Обычный выходной (type=1 без holidayId)
                return DayType.Weekend;
            }

            // Шаг 3: fallback — определяем по дню недели
            DayOfWeek dow = date.DayOfWeek;
            return (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday)
                ? DayType.Weekend
                : DayType.Workday;
        }

        /// <summary>
        /// Возвращает название праздника для указанной даты.
        /// </summary>
        /// <returns>Название праздника или null если день не праздничный.</returns>
        public string GetHolidayName(DateTime date)
        {
            string dateKey = date.ToString("dd.MM");

            if (SpecialDays.TryGetValue(dateKey, out CalendarDay calendarDay)
                && calendarDay.HolidayId.HasValue)
            {
                string holidayId = calendarDay.HolidayId.Value.ToString();
                if (Holidays.TryGetValue(holidayId, out string holidayName))
                    return holidayName;
            }

            return null;
        }

        // =========================================================================
        // МЕТОДЫ ДЛЯ РАБОТЫ С ДАТАМИ
        // =========================================================================

        /// <summary>
        /// Добавляет указанное количество рабочих дней к дате.
        /// [БАГ-5] При days=0 возвращает исходный день немедленно (без входа в цикл).
        /// При отрицательном значении делегирует в SubtractWorkdays.
        /// </summary>
        /// <param name="date">Исходная дата.</param>
        /// <param name="days">Количество рабочих дней для добавления (>= 0).</param>
        public DateTime AddWorkdays(DateTime date, int days)
        {
            // [БАГ-5] Явный выход для days=0 — входной день возвращается как есть
            if (days == 0) return date;
            if (days < 0)  return SubtractWorkdays(date, -days);

            DateTime result   = date;
            int      added    = 0;

            while (added < days)
            {
                result = result.AddDays(1);
                if (IsWorkday(result))
                    added++;
            }

            return result;
        }

        /// <summary>
        /// Вычитает указанное количество рабочих дней из даты.
        /// [БАГ-5] При days=0 возвращает исходный день немедленно.
        /// При отрицательном значении делегирует в AddWorkdays.
        /// </summary>
        /// <param name="date">Исходная дата.</param>
        /// <param name="days">Количество рабочих дней для вычитания (>= 0).</param>
        public DateTime SubtractWorkdays(DateTime date, int days)
        {
            // [БАГ-5] Явный выход для days=0
            if (days == 0) return date;
            if (days < 0)  return AddWorkdays(date, -days);

            DateTime result    = date;
            int      subtracted = 0;

            while (subtracted < days)
            {
                result = result.AddDays(-1);
                if (IsWorkday(result))
                    subtracted++;
            }

            return result;
        }

        /// <summary>
        /// Возвращает количество рабочих дней между двумя датами включительно.
        /// Порядок дат не важен — метод всегда считает от меньшей к большей.
        /// </summary>
        public int GetWorkdaysBetween(DateTime startDate, DateTime endDate)
        {
            // Нормализуем порядок дат
            if (startDate > endDate)
            {
                DateTime tmp = startDate;
                startDate    = endDate;
                endDate      = tmp;
            }

            // Используем LINQ для компактного подсчёта через перечисление дат
            return Enumerable
                .Range(0, (endDate - startDate).Days + 1)
                .Select(offset => startDate.AddDays(offset))
                .Count(d => IsWorkday(d));
        }

        /// <summary>
        /// [П3] Возвращает следующий рабочий день после указанной даты.
        /// Если сама дата является рабочей — возвращает следующий рабочий день (не саму дату).
        /// </summary>
        /// <param name="date">Опорная дата.</param>
        public DateTime GetNextWorkday(DateTime date)
        {
            DateTime result = date.AddDays(1);
            // Ищем следующий рабочий день, пропуская выходные и праздники
            while (!IsWorkday(result))
                result = result.AddDays(1);
            return result;
        }

        /// <summary>
        /// [П3] Возвращает предыдущий рабочий день до указанной даты.
        /// Если сама дата является рабочей — возвращает предыдущий рабочий день (не саму дату).
        /// </summary>
        /// <param name="date">Опорная дата.</param>
        public DateTime GetPrevWorkday(DateTime date)
        {
            DateTime result = date.AddDays(-1);
            while (!IsWorkday(result))
                result = result.AddDays(-1);
            return result;
        }

        // =========================================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // =========================================================================

        /// <summary>Добавляет праздник в словарь праздников.</summary>
        public void AddHoliday(string id, string title)
        {
            Holidays[id] = title;
        }

        /// <summary>Добавляет специальный день в словарь специальных дней.</summary>
        public void AddSpecialDay(CalendarDay day)
        {
            SpecialDays[day.Date] = day;
        }

        /// <summary>
        /// Добавляет перенос рабочего дня.
        /// [БАГ-2] Теперь заполняет оба словаря: Transitions (выходной→рабочий)
        ///         и TransferWorkdays (рабочий→выходной) для O(1) поиска.
        /// </summary>
        /// <param name="fromDate">Дата, которая стала выходным (dd.MM).</param>
        /// <param name="toDate">Дата, которая стала рабочей вместо fromDate (dd.MM).</param>
        public void AddTransition(string fromDate, string toDate)
        {
            // Прямой словарь: выходной → рабочий (для отображения "откуда перенесли")
            Transitions[fromDate] = toDate;
            // [БАГ-2] Обратный словарь: рабочий → выходной (O(1) поиск в GetDayType)
            TransferWorkdays[toDate] = fromDate;
        }

        /// <summary>Возвращает строковое представление календаря.</summary>
        public override string ToString() =>
            $"Производственный календарь {Year} ({CountryCode}): " +
            $"{SpecialDays.Count} спец. дней, {Holidays.Count} праздников, " +
            $"{Transitions.Count} переносов";
    }
}
