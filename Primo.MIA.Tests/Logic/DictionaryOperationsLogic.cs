using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>
    /// Бизнес-логика операций со словарями
    /// </summary>
    public class DictionaryOperationsLogic
    {
        /// <summary>
        /// Объединяет два словаря по заданной стратегии
        /// </summary>
        public Dictionary<string, string> Merge(
            Dictionary<string, string> first,
            Dictionary<string, string> second,
            DictionaryMergeStrategy strategy)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            var result = new Dictionary<string, string>(first);

            switch (strategy)
            {
                case DictionaryMergeStrategy.KeepFirst:
                    foreach (var kvp in second)
                    {
                        if (!result.ContainsKey(kvp.Key))
                            result[kvp.Key] = kvp.Value;
                    }
                    break;

                case DictionaryMergeStrategy.KeepSecond:
                    foreach (var kvp in second)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                    break;

                case DictionaryMergeStrategy.ThrowOnDuplicate:
                    var duplicates = first.Keys.Intersect(second.Keys).ToList();
                    if (duplicates.Any())
                        throw new InvalidOperationException(
                            $"Обнаружены дублирующиеся ключи: {string.Join(", ", duplicates)}");
                    
                    foreach (var kvp in second)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                    break;

                default:
                    throw new ArgumentException($"Неизвестная стратегия: {strategy}");
            }

            return result;
        }

        /// <summary>
        /// Фильтрует словарь по условию
        /// </summary>
        public Dictionary<string, string> Filter(
            Dictionary<string, string> source,
            string pattern,
            DictionaryFilterTarget target,
            DictionaryFilterMethod method,
            bool caseSensitive = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var comparison = caseSensitive 
                ? StringComparison.Ordinal 
                : StringComparison.OrdinalIgnoreCase;

            Func<KeyValuePair<string, string>, bool> predicate = kvp =>
            {
                bool keyMatches = Matches(kvp.Key, pattern, method, comparison);
                bool valueMatches = Matches(kvp.Value, pattern, method, comparison);

                return target switch
                {
                    DictionaryFilterTarget.Keys => keyMatches,
                    DictionaryFilterTarget.Values => valueMatches,
                    DictionaryFilterTarget.KeysAndValues => keyMatches || valueMatches,
                    _ => throw new ArgumentException($"Неизвестная цель: {target}")
                };
            };

            return source.Where(predicate).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private bool Matches(string text, string pattern, DictionaryFilterMethod method, StringComparison comparison)
        {
            if (text == null) return false;

            switch (method)
            {
                case DictionaryFilterMethod.Contains:
                    return text.IndexOf(pattern, comparison) >= 0;

                case DictionaryFilterMethod.Exact:
                    return string.Equals(text, pattern, comparison);

                case DictionaryFilterMethod.Regex:
                    var opts = comparison == StringComparison.Ordinal
                        ? System.Text.RegularExpressions.RegexOptions.None
                        : System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                    return System.Text.RegularExpressions.Regex.IsMatch(text, pattern, opts);

                case DictionaryFilterMethod.Wildcard:
                    return MatchesWildcard(text, pattern, comparison);

                default:
                    throw new ArgumentException($"Неизвестный метод: {method}");
            }
        }

        private bool MatchesWildcard(string text, string pattern, StringComparison comparison)
        {
            // Конвертируем wildcard в regex
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            var opts = comparison == StringComparison.Ordinal
                ? System.Text.RegularExpressions.RegexOptions.None
                : System.Text.RegularExpressions.RegexOptions.IgnoreCase;

            return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern, opts);
        }

        /// <summary>
        /// Инвертирует словарь (ключи становятся значениями и наоборот)
        /// </summary>
        public Dictionary<string, string> Invert(
            Dictionary<string, string> source,
            bool throwOnDuplicates = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var result = new Dictionary<string, string>();
            var duplicates = new List<string>();

            foreach (var kvp in source)
            {
                if (result.ContainsKey(kvp.Value))
                {
                    if (throwOnDuplicates)
                        duplicates.Add(kvp.Value);
                    else
                        continue; // Пропускаем дубликаты, оставляем первое вхождение
                }
                else
                {
                    result[kvp.Value] = kvp.Key;
                }
            }

            if (duplicates.Any())
                throw new InvalidOperationException(
                    $"Обнаружены дублирующиеся значения: {string.Join(", ", duplicates)}");

            return result;
        }
    }
}
