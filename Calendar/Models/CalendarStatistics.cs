// =============================================================================
// CalendarStatistics.cs — статистика производственного календаря.
//
// Содержит агрегированные данные о рабочем времени за год:
// - Количество рабочих и праздничных дней
// - Рабочие часы для разных графиков (40ч, 36ч, 24ч в неделю)
// =============================================================================

namespace Primo.MIA.Calendar.Models
{
    /// <summary>
    /// Статистика производственного календаря за год.
    /// </summary>
    public class CalendarStatistics
    {
        /// <summary>
        /// Количество рабочих дней в году.
        /// </summary>
        public int Workdays { get; set; }

        /// <summary>
        /// Количество праздничных и выходных дней в году.
        /// </summary>
        public int Holidays { get; set; }

        /// <summary>
        /// Количество рабочих часов при 40-часовой рабочей неделе.
        /// </summary>
        public double Hours40 { get; set; }

        /// <summary>
        /// Количество рабочих часов при 36-часовой рабочей неделе.
        /// </summary>
        public double Hours36 { get; set; }

        /// <summary>
        /// Количество рабочих часов при 24-часовой рабочей неделе.
        /// </summary>
        public double Hours24 { get; set; }

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public CalendarStatistics()
        {
            Workdays = 0;
            Holidays = 0;
            Hours40 = 0;
            Hours36 = 0;
            Hours24 = 0;
        }

        /// <summary>
        /// Возвращает строковое представление статистики.
        /// </summary>
        public override string ToString()
        {
            return $"Рабочих дней: {Workdays}, Праздничных/выходных: {Holidays}, " +
                   $"Часы (40ч): {Hours40}, (36ч): {Hours36}, (24ч): {Hours24}";
        }
    }
}
