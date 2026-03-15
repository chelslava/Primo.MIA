// =============================================================================
// JsonCalendarParser.cs — парсер JSON формата производственного календаря.
//
// Парсит JSON файлы с сайта xmlcalendar.ru:
// - year — год календаря
// - months — месяцы с выходными днями
// - transitions — переносы рабочих дней
// - statistic — статистика по году
//
// Пример структуры:
// {
//   "year": 2026,
//   "months": [
//     { "month": 1, "days": "1,2,3,4,5,6,7,8,9+,10,11,17,18,24,25,31" }
//   ],
//   "transitions": [
//     { "from": "01.03", "to": "01.09" }
//   ],
//   "statistic": { "workdays": 247, "holidays": 118 }
// }
//
// Суффиксы дней:
//   + — сокращённый рабочий день
//   * — перенос выходного на этот день
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Primo.MIA.Calendar.Models;

namespace Primo.MIA.Calendar.Parsers
{
    /// <summary>
    /// Парсер JSON формата производственного календаря.
    /// </summary>
    public class JsonCalendarParser : ICalendarParser
    {
        /// <summary>
        /// Поддерживаемый формат файла.
        /// </summary>
        public global::Primo.MIA.CalendarFormat SupportedFormat => global::Primo.MIA.CalendarFormat.Json;

        /// <summary>
        /// Парсит содержимое JSON файла календаря.
        /// </summary>
        /// <param name="content">Содержимое JSON файла</param>
        /// <returns>Модель производственного календаря</returns>
        /// <exception cref="CalendarParseException">
        /// Выбрасывается при ошибке парсинга JSON
        /// </exception>
        public ProductionCalendar Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new CalendarParseException("Содержимое файла календаря пустое", global::Primo.MIA.CalendarFormat.Json);
            }

            try
            {
                // Используем DataContractJsonSerializer для совместимости с .NET Framework
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(JsonCalendarData));
                
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
                {
                    JsonCalendarData data = (JsonCalendarData)serializer.ReadObject(stream);
                    
                    if (data == null)
                    {
                        throw new CalendarParseException("Не удалось десериализовать JSON", global::Primo.MIA.CalendarFormat.Json);
                    }

                    return ConvertToCalendar(data);
                }
            }
            catch (CalendarParseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CalendarParseException(
                    $"Ошибка парсинга JSON: {ex.Message}",
                    global::Primo.MIA.CalendarFormat.Json,
                    ex);
            }
        }

        /// <summary>
        /// Преобразует JSON-модель в ProductionCalendar.
        /// </summary>
        private ProductionCalendar ConvertToCalendar(JsonCalendarData data)
        {
            ProductionCalendar calendar = new ProductionCalendar
            {
                Year = data.Year,
                CountryCode = "ru" // JSON не содержит код страны
            };

            // Парсим месяцы
            if (data.Months != null)
            {
                foreach (JsonMonth month in data.Months)
                {
                    ParseMonthDays(calendar, month.Month, month.Days);
                }
            }

            // Парсим переносы
            if (data.Transitions != null)
            {
                foreach (JsonTransition transition in data.Transitions)
                {
                    calendar.AddTransition(transition.From, transition.To);
                }
            }

            // Парсим статистику
            if (data.Statistic != null)
            {
                calendar.Statistics = new CalendarStatistics
                {
                    Workdays = data.Statistic.Workdays,
                    Holidays = data.Statistic.Holidays,
                    Hours40 = data.Statistic.Hours40,
                    Hours36 = data.Statistic.Hours36,
                    Hours24 = data.Statistic.Hours24
                };
            }

            return calendar;
        }

        /// <summary>
        /// Парсит строку дней месяца и добавляет в календарь.
        /// </summary>
        private void ParseMonthDays(ProductionCalendar calendar, int month, string daysString)
        {
            if (string.IsNullOrWhiteSpace(daysString))
            {
                return;
            }

            string[] dayParts = daysString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string dayPart in dayParts)
            {
                string dayStr = dayPart.Trim();
                if (string.IsNullOrEmpty(dayStr))
                {
                    continue;
                }

                // Определяем суффикс
                int type = 1; // По умолчанию — выходной
                string transferFrom = null;

                // Убираем суффиксы для получения номера дня
                string dayNumberStr = dayStr;

                if (dayStr.EndsWith("+"))
                {
                    // Сокращённый рабочий день
                    type = 2;
                    dayNumberStr = dayStr.Substring(0, dayStr.Length - 1);
                }
                else if (dayStr.EndsWith("*"))
                {
                    // Перенос выходного на этот день
                    dayNumberStr = dayStr.Substring(0, dayStr.Length - 1);
                    // В JSON формате * означает перенос, но не указано откуда
                    // Устанавливаем маркер переноса для корректной работы IsTransferredWorkday
                    transferFrom = "*";
                }

                if (int.TryParse(dayNumberStr, out int dayNumber))
                {
                    // Формируем дату в формате dd.MM
                    string dateKey = $"{dayNumber:D2}.{month:D2}";

                    CalendarDay calendarDay = new CalendarDay(dateKey, type, null, transferFrom);
                    calendar.AddSpecialDay(calendarDay);
                }
            }
        }

        #region JSON Data Contracts

        /// <summary>
        /// Корневой объект JSON календаря.
        /// </summary>
        [System.Runtime.Serialization.DataContract]
        private class JsonCalendarData
        {
            [System.Runtime.Serialization.DataMember(Name = "year")]
            public int Year { get; set; }

            [System.Runtime.Serialization.DataMember(Name = "months")]
            public List<JsonMonth> Months { get; set; }

            [System.Runtime.Serialization.DataMember(Name = "transitions")]
            public List<JsonTransition> Transitions { get; set; }

            [System.Runtime.Serialization.DataMember(Name = "statistic")]
            public JsonStatistic Statistic { get; set; }
        }

        /// <summary>
        /// Месяц с выходными днями.
        /// </summary>
        [System.Runtime.Serialization.DataContract]
        private class JsonMonth
        {
            [System.Runtime.Serialization.DataMember(Name = "month")]
            public int Month { get; set; }

            [System.Runtime.Serialization.DataMember(Name = "days")]
            public string Days { get; set; }
        }

        /// <summary>
        /// Перенос рабочего дня.
        /// </summary>
        [System.Runtime.Serialization.DataContract]
        private class JsonTransition
        {
            [System.Runtime.Serialization.DataMember(Name = "from")]
            public string From { get; set; }

            [System.Runtime.Serialization.DataMember(Name = "to")]
            public string To { get; set; }
        }

        /// <summary>
        /// Статистика по году.
        /// </summary>
        [System.Runtime.Serialization.DataContract]
        private class JsonStatistic
        {
            [System.Runtime.Serialization.DataMember(Name = "workdays")]
            public int Workdays { get; set; }

            [System.Runtime.Serialization.DataMember(Name = "holidays")]
            public int Holidays { get; set; }

            [System.Runtime.Serialization.DataMember(Name = "hours40")]
            public double Hours40 { get; set; }

            [System.Runtime.Serialization.DataMember(Name = "hours36")]
            public double Hours36 { get; set; }

            [System.Runtime.Serialization.DataMember(Name = "hours24")]
            public double Hours24 { get; set; }
        }

        #endregion
    }
}
