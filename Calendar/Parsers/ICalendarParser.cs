// =============================================================================
// ICalendarParser.cs — интерфейс парсера производственного календаря.
//
// Определяет контракт для парсеров различных форматов:
// - XML, JSON, CSV, TXT
// - Каждый парсер преобразует содержимое файла в ProductionCalendar
// =============================================================================

using Primo.MIA.Calendar.Models;

namespace Primo.MIA.Calendar.Parsers
{
    /// <summary>
    /// Интерфейс парсера производственного календаря.
    /// Реализуется для каждого поддерживаемого формата файла.
    /// </summary>
    public interface ICalendarParser
    {
        /// <summary>
        /// Парсит содержимое файла календаря и возвращает модель ProductionCalendar.
        /// </summary>
        /// <param name="content">Содержимое файла календаря</param>
        /// <returns>Модель производственного календаря</returns>
        /// <exception cref="CalendarParseException">
        /// Выбрасывается при ошибке парсинга файла
        /// </exception>
        ProductionCalendar Parse(string content);

        /// <summary>
        /// Поддерживаемый формат файла.
        /// </summary>
        global::Primo.MIA.CalendarFormat SupportedFormat { get; }
    }
}
