// =============================================================================
// XmlCalendarParser.cs — парсер XML формата производственного календаря.
//
// Парсит XML файлы с сайта xmlcalendar.ru:
// - <holidays> — список праздников с названиями
// - <days> — специальные дни (праздники, выходные, переносы)
//
// Пример структуры:
// <calendar year="2026" country="ru">
//     <holidays>
//         <holiday id="1" title="Новогодние каникулы"/>
//     </holidays>
//     <days>
//         <day d="01.01" t="1" h="1"/>      — праздник
//         <day d="01.09" t="1" f="01.03"/>  — перенос
//         <day d="04.30" t="2"/>            — сокращённый
//     </days>
// </calendar>
// =============================================================================

using System;
using System.Xml.Linq;
using Primo.MIA.Calendar.Models;

namespace Primo.MIA.Calendar.Parsers
{
    /// <summary>
    /// Парсер XML формата производственного календаря.
    /// </summary>
    public class XmlCalendarParser : ICalendarParser
    {
        /// <summary>
        /// Поддерживаемый формат файла.
        /// </summary>
        public global::Primo.MIA.CalendarFormat SupportedFormat => global::Primo.MIA.CalendarFormat.Xml;

        /// <summary>
        /// Парсит содержимое XML файла календаря.
        /// </summary>
        /// <param name="content">Содержимое XML файла</param>
        /// <returns>Модель производственного календаря</returns>
        /// <exception cref="CalendarParseException">
        /// Выбрасывается при ошибке парсинга XML
        /// </exception>
        public ProductionCalendar Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new CalendarParseException("Содержимое файла календаря пустое", global::Primo.MIA.CalendarFormat.Xml);
            }

            try
            {
                XDocument doc = XDocument.Parse(content);
                XElement root = doc.Root;

                if (root == null || root.Name.LocalName != "calendar")
                {
                    throw new CalendarParseException(
                        "Некорректная структура XML: корневой элемент должен быть 'calendar'",
                        global::Primo.MIA.CalendarFormat.Xml);
                }

                ProductionCalendar calendar = new ProductionCalendar();

                // Читаем атрибуты корневого элемента
                ParseAttributes(root, calendar);

                // Парсим праздники
                ParseHolidays(root, calendar);

                // Парсим дни
                ParseDays(root, calendar);

                return calendar;
            }
            catch (CalendarParseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CalendarParseException(
                    $"Ошибка парсинга XML: {ex.Message}",
                    global::Primo.MIA.CalendarFormat.Xml,
                    ex);
            }
        }

        /// <summary>
        /// Парсит атрибуты корневого элемента calendar.
        /// </summary>
        private void ParseAttributes(XElement root, ProductionCalendar calendar)
        {
            // Год
            XAttribute yearAttr = root.Attribute("year");
            if (yearAttr != null && int.TryParse(yearAttr.Value, out int year))
            {
                calendar.Year = year;
            }

            // Код страны
            XAttribute countryAttr = root.Attribute("country");
            if (countryAttr != null)
            {
                calendar.CountryCode = countryAttr.Value;
            }
        }

        /// <summary>
        /// Парсит секцию holidays — список праздников с названиями.
        /// </summary>
        private void ParseHolidays(XElement root, ProductionCalendar calendar)
        {
            XElement holidaysElement = root.Element("holidays");
            if (holidaysElement == null)
            {
                return;
            }

            foreach (XElement holidayElement in holidaysElement.Elements("holiday"))
            {
                XAttribute idAttr = holidayElement.Attribute("id");
                XAttribute titleAttr = holidayElement.Attribute("title");

                if (idAttr != null && titleAttr != null)
                {
                    calendar.AddHoliday(idAttr.Value, titleAttr.Value);
                }
            }
        }

        /// <summary>
        /// Парсит секцию days — специальные дни.
        /// </summary>
        private void ParseDays(XElement root, ProductionCalendar calendar)
        {
            XElement daysElement = root.Element("days");
            if (daysElement == null)
            {
                return;
            }

            foreach (XElement dayElement in daysElement.Elements("day"))
            {
                ParseDayElement(dayElement, calendar);
            }
        }

        /// <summary>
        /// Парсит отдельный элемент day.
        /// </summary>
        private void ParseDayElement(XElement dayElement, ProductionCalendar calendar)
        {
            // Атрибут d — дата в формате dd.MM
            XAttribute dateAttr = dayElement.Attribute("d");
            if (dateAttr == null)
            {
                return;
            }

            string date = dateAttr.Value;
            int type = 1; // По умолчанию — праздник/выходной
            int? holidayId = null;
            string transferFrom = null;

            // Атрибут t — тип дня (1=праздник/выходной, 2=сокращённый)
            XAttribute typeAttr = dayElement.Attribute("t");
            if (typeAttr != null && int.TryParse(typeAttr.Value, out int t))
            {
                type = t;
            }

            // Атрибут h — ID праздника
            XAttribute holidayAttr = dayElement.Attribute("h");
            if (holidayAttr != null && int.TryParse(holidayAttr.Value, out int h))
            {
                holidayId = h;
            }

            // Атрибут f — дата переноса (какой выходной перенесён на этот день)
            XAttribute transferAttr = dayElement.Attribute("f");
            if (transferAttr != null)
            {
                transferFrom = transferAttr.Value;
                // Добавляем перенос в словарь
                calendar.AddTransition(transferFrom, date);
            }

            CalendarDay calendarDay = new CalendarDay(date, type, holidayId, transferFrom);
            calendar.AddSpecialDay(calendarDay);
        }
    }
}
