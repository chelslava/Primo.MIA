// =============================================================================
// TextTemplateLogic.cs — логика шаблонизатора текста.
//
// Подставляет значения из Dictionary<string,string> в текстовый шаблон
// с плейсхолдерами. Поддерживает три синтаксиса плейсхолдеров, три режима
// обработки пропущенных ключей и форматирование значений.
//
// Синтаксисы:
//   DoubleBrace — {{ключ}}     (по умолчанию, не конфликтует с JSON)
//   SingleBrace — {ключ}
//   Percent     — %ключ%       (стиль Windows-переменных окружения)
//
// Форматирование значений (суффикс через двоеточие):
//   {{Сумма:N2}}          → "14 500,00"
//   {{Дата:dd.MM.yyyy}}   → "15.03.2026"
//   {{Процент:P1}}        → "12,5 %"
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>Синтаксис плейсхолдеров в шаблоне.</summary>
    public enum TemplateSyntax
    {
        /// <summary>Двойные фигурные скобки: {{ключ}}</summary>
        DoubleBrace,
        /// <summary>Одинарные фигурные скобки: {ключ}</summary>
        SingleBrace,
        /// <summary>Проценты: %ключ%</summary>
        Percent
    }

    /// <summary>Поведение при отсутствии ключа в словаре.</summary>
    public enum MissingKeyBehavior
    {
        /// <summary>Оставить плейсхолдер как есть.</summary>
        LeaveAsIs,
        /// <summary>Заменить пустой строкой.</summary>
        ReplaceWithEmpty,
        /// <summary>Бросить исключение.</summary>
        ThrowError
    }

    /// <summary>Результат рендеринга шаблона.</summary>
    public class TextTemplateResult
    {
        /// <summary>Результирующая строка.</summary>
        public string Result { get; set; }

        /// <summary>Количество выполненных замен.</summary>
        public int ReplacedCount { get; set; }

        /// <summary>Список незаполненных ключей.</summary>
        public List<string> MissingKeys { get; set; } = new List<string>();

        /// <summary>Сообщение об ошибке (null если успешно).</summary>
        public string ErrorMessage { get; set; }

        /// <summary>true если рендеринг успешен.</summary>
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }

    /// <summary>
    /// Логика шаблонизатора текста.
    /// Подставляет значения словаря в шаблон с плейсхолдерами.
    /// </summary>
    public static class TextTemplateLogic
    {
        // =====================================================================
        // Статические словари — паттерны regex для каждого синтаксиса
        // =====================================================================

        /// <summary>
        /// Regex-паттерны для каждого синтаксиса плейсхолдеров.
        /// Группа 1: имя ключа (может содержать суффикс формата через ':').
        /// </summary>
        private static readonly Dictionary<TemplateSyntax, string> SyntaxPatterns =
            new Dictionary<TemplateSyntax, string>
            {
                { TemplateSyntax.DoubleBrace, @"\{\{([^}]+)\}\}" },
                { TemplateSyntax.SingleBrace, @"\{([^{}]+)\}"   },
                { TemplateSyntax.Percent,     @"%([^%]+)%"      }
            };

        // =====================================================================
        // Основной метод
        // =====================================================================

        /// <summary>
        /// Рендерит шаблон, подставляя значения из словаря.
        /// </summary>
        /// <param name="template">Шаблон с плейсхолдерами.</param>
        /// <param name="variables">Словарь переменных для подстановки.</param>
        /// <param name="syntax">Синтаксис плейсхолдеров.</param>
        /// <param name="missingKeyBehavior">Поведение при отсутствии ключа.</param>
        /// <param name="caseSensitive">Учитывать регистр ключей.</param>
        /// <returns>Результат рендеринга.</returns>
        public static TextTemplateResult Render(
            string template,
            Dictionary<string, string> variables,
            TemplateSyntax syntax = TemplateSyntax.DoubleBrace,
            MissingKeyBehavior missingKeyBehavior = MissingKeyBehavior.LeaveAsIs,
            bool caseSensitive = false)
        {
            // Валидация входных данных
            if (template == null)
                return new TextTemplateResult { ErrorMessage = "Шаблон не указан" };

            if (variables == null)
                return new TextTemplateResult { ErrorMessage = "Словарь переменных не указан" };

            var missingKeys = new List<string>();
            int replacements = 0;

            // Получаем regex-паттерн для выбранного синтаксиса
            string pattern = SyntaxPatterns[syntax];

            // StringComparison для поиска ключей
            StringComparison comparison = caseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            // Рендерим через Regex.Replace с MatchEvaluator
            string result = Regex.Replace(template, pattern, match =>
            {
                // Полное содержимое плейсхолдера: "ключ" или "ключ:формат"
                string fullKey = match.Groups[1].Value.Trim();

                // Разделяем ключ и суффикс формата
                string key = fullKey;
                string formatSpec = null;

                int colonIndex = fullKey.IndexOf(':');
                if (colonIndex > 0)
                {
                    key = fullKey.Substring(0, colonIndex).Trim();
                    formatSpec = fullKey.Substring(colonIndex + 1).Trim();
                }

                // Ищем ключ в словаре с учётом регистра
                string value = variables
                    .Where(kv => string.Compare(kv.Key, key, comparison) == 0)
                    .Select(kv => kv.Value)
                    .FirstOrDefault();

                if (value != null)
                {
                    replacements++;

                    // Применяем форматирование если задан суффикс
                    if (!string.IsNullOrEmpty(formatSpec))
                        value = ApplyFormat(value, formatSpec);

                    return value;
                }

                // Ключ не найден — применяем стратегию MissingKeyBehavior
                missingKeys.Add(key);

                return missingKeyBehavior == MissingKeyBehavior.ReplaceWithEmpty
                    ? string.Empty
                    : match.Value; // LeaveAsIs — возвращаем плейсхолдер как есть
            });

            // Проверяем строгий режим
            if (missingKeyBehavior == MissingKeyBehavior.ThrowError && missingKeys.Count > 0)
            {
                string keyList = string.Join(", ", missingKeys.Distinct());
                return new TextTemplateResult
                {
                    ErrorMessage = $"Незаполненные ключи: {keyList}"
                };
            }

            return new TextTemplateResult
            {
                Result = result,
                ReplacedCount = replacements,
                MissingKeys = missingKeys
            };
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>
        /// Применяет .NET-формат к строковому значению плейсхолдера.
        /// Пытается распарсить как DateTime, затем как double.
        /// Если не удалось — возвращает исходное значение без изменений.
        /// </summary>
        /// <param name="value">Строковое значение из словаря.</param>
        /// <param name="format">.NET-формат (например "N2", "dd.MM.yyyy", "P1").</param>
        public static string ApplyFormat(string value, string format)
        {
            // Пробуем DateTime
            if (DateTime.TryParse(value, out DateTime dateValue))
                return dateValue.ToString(format);

            // Пробуем double (InvariantCulture для парсинга, CurrentCulture для вывода)
            if (double.TryParse(value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double numValue))
                return numValue.ToString(format);

            // Не удалось — возвращаем как есть
            return value;
        }

        /// <summary>
        /// Разбирает строку кодировки в объект Encoding.
        /// Поддерживает UTF-8, UTF-16, Windows-1251, CP866 и другие IANA-имена.
        /// При ошибке возвращает UTF-8.
        /// </summary>
        public static Encoding ParseEncoding(string encodingName)
        {
            if (string.IsNullOrWhiteSpace(encodingName))
                return Encoding.UTF8;

            try
            {
                return Encoding.GetEncoding(encodingName);
            }
            catch
            {
                return Encoding.UTF8;
            }
        }
    }
}
