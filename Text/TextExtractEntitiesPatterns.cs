// =============================================================================
// TextExtractEntitiesPatterns.cs — встроенные паттерны сущностей, их
// нормализаторы и валидатор ИНН (issue #38, разбиение монолита
// TextExtractEntitiesBack 1013 строк).
//
// Только данные и чистые функции: склейка с активностью — в Back.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    /// <summary>
    /// Описание одного типа сущности: regex-паттерн, нормализатор, валидатор.
    /// </summary>
    internal class EntityPattern
    {
        /// <summary>Имя типа — ключ в результирующем словаре.</summary>
        public string TypeName { get; set; }

        /// <summary>Скомпилированный regex для поиска в тексте.</summary>
        public Regex Pattern { get; set; }

        /// <summary>
        /// Уровень уверенности (0–100).
        /// Высокий — у Email, ИНН с валидацией.
        /// Средний — у Phone, Date (много ложных срабатываний возможно).
        /// </summary>
        public int Confidence { get; set; }

        /// <summary>
        /// Функция нормализации найденного значения.
        /// null — нормализация не применяется.
        /// </summary>
        public Func<string, string> Normalize { get; set; }

        /// <summary>
        /// Дополнительная функция валидации.
        /// null — дополнительная проверка не нужна.
        /// Используется для проверки контрольных цифр ИНН.
        /// </summary>
        public Func<string, bool> Validate { get; set; }
    }

    /// <summary>Реестр встроенных паттернов сущностей и их операции.</summary>
    internal static class TextEntityPatterns
    {
        /// <summary>
        /// Все встроенные паттерны сущностей.
        /// Порядок важен: более специфичные паттерны (ИНН, СНИЛС) идут раньше
        /// чем общие (числа), чтобы избежать ложных захватов.
        /// </summary>
        public static readonly List<EntityPattern> BuiltinPatterns =
            new List<EntityPattern>
            {
                // ── Email ──────────────────────────────────────────────────
                new EntityPattern
                {
                    TypeName   = "Email",
                    Confidence = 95,
                    Pattern    = new Regex(
                        @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase),
                    Normalize  = s => s.ToLowerInvariant().Trim()
                },

                // ── URL ────────────────────────────────────────────────────
                new EntityPattern
                {
                    TypeName   = "URL",
                    Confidence = 90,
                    Pattern    = new Regex(
                        @"(?:https?|ftp)://[^\s""'<>]+",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase),
                    Normalize  = s => s.TrimEnd('.', ',', ';', ')')
                },

                // ── GUID / UUID ────────────────────────────────────────────
                new EntityPattern
                {
                    TypeName   = "GUID",
                    Confidence = 99,
                    Pattern    = new Regex(
                        @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
                        RegexOptions.Compiled)
                },

                // ── IP-адрес (IPv4) ────────────────────────────────────────
                new EntityPattern
                {
                    TypeName   = "IP",
                    Confidence = 90,
                    Pattern    = new Regex(
                        @"\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\b",
                        RegexOptions.Compiled),
                    // Исключаем зарезервированные и маловероятные в тексте адреса
                    Validate   = s => s != "0.0.0.0" && s != "255.255.255.255"
                },

                // ── Телефон (Россия: +7 / 8) ───────────────────────────────
                // Охватывает: +7(495)1234567, +7 495 123-45-67, 84951234567 и т.д.
                new EntityPattern
                {
                    TypeName   = "Phone",
                    Confidence = 80,
                    Pattern    = new Regex(
                        @"(?:\+7|8)[\s\-]?\(?(\d{3})\)?[\s\-]?(\d{3})[\s\-]?(\d{2})[\s\-]?(\d{2})",
                        RegexOptions.Compiled),
                    Normalize  = NormalizePhone
                },

                // ── Дата (множество форматов) ──────────────────────────────
                new EntityPattern
                {
                    TypeName   = "Date",
                    Confidence = 75,
                    Pattern    = new Regex(
                        @"\b(?:\d{1,2}[./\-]\d{1,2}[./\-]\d{2,4}|" +       // 15.03.2026 / 15/03/26
                        @"\d{4}[.\-]\d{2}[.\-]\d{2}|" +                      // 2026-03-15
                        @"\d{1,2}\s+(?:января|февраля|марта|апреля|мая|июня|" +
                        @"июля|августа|сентября|октября|ноября|декабря)" +
                        @"(?:\s+\d{4})?)\b",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase)
                },

                // ── Денежная сумма ─────────────────────────────────────────
                // Охватывает: 14 500,00 руб., USD 1 200.50, € 850, 1000 ₽
                new EntityPattern
                {
                    TypeName   = "Money",
                    Confidence = 75,
                    Pattern    = new Regex(
                        @"(?:USD|EUR|GBP|CNY|RUB|руб\.?|р\.)\s*[\d\s]+(?:[.,]\d{1,2})?" +
                        @"|[\d\s]+(?:[.,]\d{1,2})?\s*(?:USD|EUR|GBP|CNY|RUB|руб\.?|р\.)" +
                        @"|[$€£¥₽]\s*[\d\s]+(?:[.,]\d{1,2})?",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase),
                    Normalize  = s => Regex.Replace(s.Trim(), @"\s{2,}", " ")
                },

                // ── СНИЛС ─────────────────────────────────────────────────
                // Формат: XXX-XXX-XXX XX или XXXXXXXXXXX (11 цифр)
                new EntityPattern
                {
                    TypeName   = "SNILS",
                    Confidence = 92,
                    Pattern    = new Regex(
                        @"\b\d{3}[\-\s]\d{3}[\-\s]\d{3}[\s]\d{2}\b|\b\d{11}\b",
                        RegexOptions.Compiled),
                    Normalize  = NormalizeSNILS
                },

                // ── ИНН ───────────────────────────────────────────────────
                // 10 цифр (юрлицо) или 12 цифр (физлицо)
                // Предшествующий текст «ИНН» повышает точность
                new EntityPattern
                {
                    TypeName   = "INN",
                    Confidence = 85,
                    Pattern    = new Regex(
                        @"(?:ИНН\s*[:№]?\s*)?(?<!\d)(\d{10}|\d{12})(?!\d)",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase),
                    Validate   = ValidateINN
                },

                // ── Паспорт РФ ────────────────────────────────────────────
                // Серия 4 цифры + номер 6 цифр, с предшествующим «паспорт» или без
                new EntityPattern
                {
                    TypeName   = "Passport",
                    Confidence = 70,
                    Pattern    = new Regex(
                        @"(?:паспорт\s+)?(?<!\d)(\d{4})\s*(\d{6})(?!\d)",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase),
                    Normalize  = NormalizePassport
                },

                // ── Номер автомобиля (Россия, ГОСТ Р 50577-93) ────────────
                // Стандарт: Б000ББ00(0) — буква-три цифры-две буквы-2/3 цифры региона
                // Поддерживает кириллицу (АВЕКМНОРСТУХ) и латинские замены (для OCR)
                new EntityPattern
                {
                    TypeName   = "CarNumber",
                    Confidence = 88,
                    Pattern    = new Regex(
                        // Кириллические и латинские омоглифы (OCR-дружественный)
                        @"\b[АВЕКМНОРСТУХавекмнорстухABEKMHOPCTYXabekmhopctyx]" +
                        @"\d{3}" +
                        @"[АВЕКМНОРСТУХавекмнорстухABEKMHOPCTYXabekmhopctyx]{2}" +
                        @"\d{2,3}\b",
                        RegexOptions.Compiled),
                    Normalize  = NormalizeCarNumber
                },
            };

        /// <summary>
        /// Словарь: имя типа (upper) → EntityPattern. O(1) поиск по имени.
        /// </summary>
        public static readonly Dictionary<string, EntityPattern> PatternByType =
            BuiltinPatterns.ToDictionary(
                p  => p.TypeName.ToUpperInvariant(),
                p  => p);

        /// <summary>
        /// Нормализует российский номер телефона к формату +7 (XXX) XXX-XX-XX.
        /// Убирает все разделители, добавляет единый формат.
        /// </summary>
        public static string NormalizePhone(string raw)
        {
            // Оставляем только цифры
            string digits = new string(raw.Where(char.IsDigit).ToArray());

            // Заменяем ведущую 8 на 7
            if (digits.StartsWith("8") && digits.Length == 11)
                digits = "7" + digits.Substring(1);

            if (digits.Length != 11 || !digits.StartsWith("7"))
                return raw; // не смогли нормализовать — возвращаем как есть

            return $"+7 ({digits.Substring(1, 3)}) {digits.Substring(4, 3)}-" +
                   $"{digits.Substring(7, 2)}-{digits.Substring(9, 2)}";
        }

        /// <summary>Нормализует СНИЛС к формату XXX-XXX-XXX XX.</summary>
        public static string NormalizeSNILS(string raw)
        {
            string digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length != 11) return raw;
            return $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-" +
                   $"{digits.Substring(6, 3)} {digits.Substring(9, 2)}";
        }

        /// <summary>Нормализует номер паспорта к формату «XXXX XXXXXX».</summary>
        public static string NormalizePassport(string raw)
        {
            string digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length == 10)
                return $"{digits.Substring(0, 4)} {digits.Substring(4, 6)}";
            return raw;
        }

        /// <summary>
        /// Нормализует номер автомобиля РФ:
        /// — приводит латинские омоглифы к кириллице (для OCR-текста)
        /// — убирает пробелы внутри номера
        /// — приводит к верхнему регистру.
        /// Латино-кириллические омоглифы: A→А, B→В, E→Е, K→К, M→М,
        /// H→Н, O→О, P→Р, C→С, T→Т, Y→У, X→Х.
        /// </summary>
        public static string NormalizeCarNumber(string raw)
        {
            // Словарь замены латинских омоглифов на кириллицу
            var latinToCyr = new Dictionary<char, char>
            {
                { 'A', 'А' }, { 'B', 'В' }, { 'E', 'Е' }, { 'K', 'К' },
                { 'M', 'М' }, { 'H', 'Н' }, { 'O', 'О' }, { 'P', 'Р' },
                { 'C', 'С' }, { 'T', 'Т' }, { 'Y', 'У' }, { 'X', 'Х' },
                { 'a', 'а' }, { 'b', 'в' }, { 'e', 'е' }, { 'k', 'к' },
                { 'm', 'м' }, { 'h', 'н' }, { 'o', 'о' }, { 'p', 'р' },
                { 'c', 'с' }, { 't', 'т' }, { 'y', 'у' }, { 'x', 'х' }
            };

            string upper = raw.ToUpperInvariant().Replace(" ", "");

            var sb = new StringBuilder(upper.Length);
            foreach (char c in upper)
                sb.Append(latinToCyr.TryGetValue(c, out char cyr) ? cyr : c);

            return sb.ToString();
        }

        /// <summary>
        /// Проверяет контрольные цифры ИНН по алгоритму ФНС России.
        /// 10-значный ИНН (юрлицо): одна контрольная цифра.
        /// 12-значный ИНН (физлицо): две контрольные цифры.
        /// Использует LINQ для вычисления взвешенных сумм.
        /// </summary>
        public static bool ValidateINN(string inn)
        {
            if (inn == null) return false;

            // Весовые коэффициенты для 10-значного ИНН
            int[] w10 = { 2, 4, 10, 3, 5, 9, 4, 6, 8 };
            // Весовые коэффициенты для 12-значного (11-я и 12-я контрольные)
            int[] w11 = { 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };
            int[] w12 = { 3, 7, 2, 4,  10, 3, 5, 9, 4, 6, 8 };

            if (inn.Length == 10)
            {
                int check = inn.Take(9)
                    .Select((c, i) => (c - '0') * w10[i])
                    .Sum() % 11 % 10;
                return check == (inn[9] - '0');
            }

            if (inn.Length == 12)
            {
                int check11 = inn.Take(10)
                    .Select((c, i) => (c - '0') * w11[i])
                    .Sum() % 11 % 10;
                int check12 = inn.Take(11)
                    .Select((c, i) => (c - '0') * w12[i])
                    .Sum() % 11 % 10;
                return check11 == (inn[10] - '0') && check12 == (inn[11] - '0');
            }

            return false;
        }
    }
}
