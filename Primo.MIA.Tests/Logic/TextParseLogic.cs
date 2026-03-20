// =============================================================================
// TextParseLogic.cs — логика разбора текста по шаблону.
//
// Извлекает именованные данные из строки по читаемой маске.
// Маска компилируется в именованные группы захвата regex.
//
// Поддерживаемые синтаксисы:
//   SingleBrace — {ключ}       (по умолчанию)
//   DoubleBrace — {{ключ}}
//   Percent     — %ключ%
//
// Опции:
//   GreedyMatch  — жадный захват (.+) вместо ленивого (.+?)
//   CaseSensitive — учитывать регистр литеральных частей
//   MultiLine    — маска разбирает многострочный текст
//   AllMatches   — найти все вхождения маски в тексте
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>Синтаксис плейсхолдеров в маске.</summary>
    public enum ParseSyntax
    {
        /// <summary>Одинарные фигурные скобки: {ключ}</summary>
        SingleBrace,
        /// <summary>Двойные фигурные скобки: {{ключ}}</summary>
        DoubleBrace,
        /// <summary>Проценты: %ключ%</summary>
        Percent
    }

    /// <summary>Результат разбора текста по шаблону.</summary>
    public class TextParseResult
    {
        /// <summary>true если маска совпала со строкой.</summary>
        public bool IsMatched { get; set; }

        /// <summary>Словарь извлечённых значений первого совпадения.</summary>
        public Dictionary<string, string> Result { get; set; } = new Dictionary<string, string>();

        /// <summary>Количество извлечённых ключей (или вхождений при AllMatches).</summary>
        public int Count { get; set; }

        /// <summary>Скомпилированный regex-паттерн (для диагностики).</summary>
        public string CompiledPattern { get; set; }

        /// <summary>Все совпадения (только при AllMatches=true).</summary>
        public List<Dictionary<string, string>> AllResults { get; set; } = new List<Dictionary<string, string>>();

        /// <summary>Сообщение об ошибке (null если успешно).</summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Логика разбора текста по шаблону.
    /// Извлекает именованные значения из строки по маске с плейсхолдерами.
    /// </summary>
    public static class TextParseLogic
    {
        // =====================================================================
        // Статические словари — паттерны для поиска плейсхолдеров в маске
        // =====================================================================

        /// <summary>
        /// Regex-паттерны для поиска плейсхолдеров внутри самой маски.
        /// Группа 1: имя ключа.
        /// </summary>
        private static readonly Dictionary<ParseSyntax, string> PlaceholderPatterns =
            new Dictionary<ParseSyntax, string>
            {
                { ParseSyntax.DoubleBrace, @"\{\{([^}]+)\}\}" },
                { ParseSyntax.SingleBrace, @"\{([^{}]+)\}"    },
                { ParseSyntax.Percent,     @"%([^%]+)%"       }
            };

        // =====================================================================
        // Основной метод
        // =====================================================================

        /// <summary>
        /// Разбирает текст по маске с плейсхолдерами.
        /// </summary>
        /// <param name="inputText">Входная строка для разбора.</param>
        /// <param name="mask">Маска с плейсхолдерами.</param>
        /// <param name="syntax">Синтаксис плейсхолдеров.</param>
        /// <param name="caseSensitive">Учитывать регистр.</param>
        /// <param name="greedy">Жадный захват.</param>
        /// <param name="multiLine">Многострочный режим.</param>
        /// <param name="allMatches">Найти все вхождения.</param>
        /// <returns>Результат разбора с извлечёнными значениями.</returns>
        public static TextParseResult Parse(
            string inputText,
            string mask,
            ParseSyntax syntax = ParseSyntax.SingleBrace,
            bool caseSensitive = false,
            bool greedy = false,
            bool multiLine = false,
            bool allMatches = false)
        {
            // Валидация входных данных
            if (inputText == null)
                return new TextParseResult { ErrorMessage = "Входная строка не указана" };

            if (string.IsNullOrEmpty(mask))
                return new TextParseResult { ErrorMessage = "Маска не указана" };

            // Компилируем маску в regex
            string compiledPattern;
            try
            {
                compiledPattern = CompileMask(mask, syntax, greedy, allMatches);
            }
            catch (Exception ex)
            {
                return new TextParseResult { ErrorMessage = $"Ошибка компиляции маски: {ex.Message}" };
            }

            // Настраиваем опции regex
            RegexOptions options = RegexOptions.Compiled;
            if (!caseSensitive) options |= RegexOptions.IgnoreCase;
            if (multiLine) options |= RegexOptions.Singleline;

            Regex regex;
            try
            {
                regex = new Regex(compiledPattern, options);
            }
            catch (Exception ex)
            {
                return new TextParseResult { ErrorMessage = $"Ошибка создания regex: {ex.Message}" };
            }

            // Выполняем поиск
            var result = new TextParseResult { CompiledPattern = compiledPattern };

            if (allMatches)
            {
                MatchCollection matches = regex.Matches(inputText);
                result.AllResults = matches
                    .Cast<Match>()
                    .Select(m => ExtractNamedGroups(regex, m))
                    .ToList();

                result.Result = result.AllResults.Count > 0
                    ? result.AllResults[0]
                    : new Dictionary<string, string>();
                result.IsMatched = result.AllResults.Count > 0;
                result.Count = result.AllResults.Count;
            }
            else
            {
                Match match = regex.Match(inputText);
                result.IsMatched = match.Success;

                if (match.Success)
                {
                    result.Result = ExtractNamedGroups(regex, match);
                    result.Count = result.Result.Count;
                }
                else
                {
                    result.Result = new Dictionary<string, string>();
                    result.Count = 0;
                }
            }

            return result;
        }

        // =====================================================================
        // Компиляция маски
        // =====================================================================

        /// <summary>
        /// Компилирует читаемую маску в regex-паттерн с именованными группами захвата.
        ///
        /// Алгоритм:
        ///   1. Разбиваем маску на чередующиеся части: литерал, плейсхолдер, литерал...
        ///   2. Каждый литерал экранируется через Regex.Escape
        ///   3. Каждый плейсхолдер {Имя} → (?<Имя>.+?) или (?<Имя>.+) при greedy
        /// </summary>
        /// <param name="mask">Исходная маска с плейсхолдерами.</param>
        /// <param name="syntax">Синтаксис плейсхолдеров.</param>
        /// <param name="greedy">true — жадный захват (.+), false — ленивый (.+?).</param>
        public static string CompileMask(string mask, ParseSyntax syntax, bool greedy, bool allMatches = false)
        {
            string placeholderPattern = PlaceholderPatterns[syntax];
            string quantifier = greedy ? ".+" : ".+?";
            var sb = new StringBuilder();
            int lastIndex = 0;
            bool hasPlaceholders = false;

            // Добавляем якорь начала строки только для одиночного совпадения
            if (!allMatches)
                sb.Append("^");

            foreach (Match m in Regex.Matches(mask, placeholderPattern))
            {
                hasPlaceholders = true;
                
                // Экранируем литеральный текст между плейсхолдерами
                string literal = mask.Substring(lastIndex, m.Index - lastIndex);
                if (!string.IsNullOrEmpty(literal))
                    sb.Append(Regex.Escape(literal));

                // Плейсхолдер → именованная группа захвата (?<Имя>.+?)
                string groupName = m.Groups[1].Value.Trim();

                // Проверяем что имя группы является допустимым идентификатором
                if (!IsValidGroupName(groupName))
                    throw new ArgumentException(
                        $"Недопустимое имя плейсхолдера «{groupName}». " +
                        "Имя должно начинаться с буквы или _, содержать только буквы, цифры и _");

                sb.Append($"(?<{groupName}>{quantifier})");
                lastIndex = m.Index + m.Length;
            }

            // Экранируем хвост маски после последнего плейсхолдера
            string tail = mask.Substring(lastIndex);
            if (!string.IsNullOrEmpty(tail))
                sb.Append(Regex.Escape(tail));

            // Добавляем якорь конца строки только для одиночного совпадения
            if (!allMatches)
                sb.Append("$");

            if (!hasPlaceholders)
                throw new ArgumentException(
                    "Маска не содержит ни одного плейсхолдера. " +
                    "Добавьте хотя бы один плейсхолдер, например {Значение}");

            return sb.ToString();
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>
        /// Извлекает именованные группы из совпадения regex.
        /// Числовые группы (0, 1, 2...) исключаются — только явные имена.
        /// </summary>
        private static Dictionary<string, string> ExtractNamedGroups(Regex regex, Match match) =>
            regex.GetGroupNames()
                .Where(name => !int.TryParse(name, out _))
                .ToDictionary(
                    name => name,
                    name => match.Groups[name].Value
                );

        /// <summary>
        /// Проверяет что имя группы является допустимым C#/regex-идентификатором.
        /// Regex именованные группы: начинается с буквы или _, содержит буквы/цифры/_.
        /// </summary>
        public static bool IsValidGroupName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!char.IsLetter(name[0]) && name[0] != '_') return false;

            return name.All(c => char.IsLetterOrDigit(c) || c == '_');
        }
    }
}
