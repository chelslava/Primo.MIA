// =============================================================================
// CalendarDay.cs — информация о дне в производственном календаре.
//
// Хранит данные о специальном дне (праздник, перенос, сокращённый день):
// - Дата в формате dd.MM
// - Тип дня (праздник/выходной/сокращённый)
// - ID праздника для получения названия
// - Информация о переносе рабочего дня
// =============================================================================

namespace Primo.MIA.Calendar.Models
{
    /// <summary>
    /// Информация о дне в производственном календаре.
    /// Используется для хранения данных о праздниках, выходных и переносах.
    /// </summary>
    public class CalendarDay
    {
        /// <summary>
        /// Дата в формате dd.MM (например, "01.01" для 1 января).
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// Тип дня:
        ///   1 — праздник/выходной день
        ///   2 — сокращённый рабочий день
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// ID праздника для ссылки на название в словаре Holidays.
        /// null для обычных выходных без названия праздника.
        /// </summary>
        public int? HolidayId { get; set; }

        /// <summary>
        /// Дата переноса в формате dd.MM.
        /// Указывает, какой выходной день был перенесён на этот рабочий день.
        /// Например, если 01.09 (1 сентября) рабочий вместо 01.03 (1 марта),
        /// то TransferFrom = "01.03".
        /// </summary>
        public string TransferFrom { get; set; }

        /// <summary>
        /// Признак того, что день является праздничным (имеет HolidayId).
        /// </summary>
        public bool IsHoliday => HolidayId.HasValue;

        /// <summary>
        /// Признак того, что день является сокращённым рабочим днём.
        /// </summary>
        public bool IsShortWorkday => Type == 2;

        /// <summary>
        /// Признак того, что это рабочий день, на который перенесён выходной.
        /// </summary>
        public bool IsTransferredWorkday => !string.IsNullOrEmpty(TransferFrom);

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public CalendarDay()
        {
            Date = string.Empty;
            Type = 0;
            HolidayId = null;
            TransferFrom = null;
        }

        /// <summary>
        /// Конструктор с параметрами.
        /// </summary>
        /// <param name="date">Дата в формате dd.MM</param>
        /// <param name="type">Тип дня (1=праздник/выходной, 2=сокращённый)</param>
        /// <param name="holidayId">ID праздника (опционально)</param>
        /// <param name="transferFrom">Дата переноса (опционально)</param>
        public CalendarDay(string date, int type, int? holidayId = null, string transferFrom = null)
        {
            Date = date;
            Type = type;
            HolidayId = holidayId;
            TransferFrom = transferFrom;
        }

        /// <summary>
        /// Возвращает строковое представление дня.
        /// </summary>
        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string> { $"Дата: {Date}" };

            if (Type == 1)
                parts.Add("Тип: Праздник/выходной");
            else if (Type == 2)
                parts.Add("Тип: Сокращённый");

            if (HolidayId.HasValue)
                parts.Add($"Праздник ID: {HolidayId}");

            if (!string.IsNullOrEmpty(TransferFrom))
                parts.Add($"Перенос с: {TransferFrom}");

            return string.Join(", ", parts);
        }
    }
}
