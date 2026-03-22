using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Бизнес-логика операций со словарями.
    /// </summary>
    public class DictionaryOperationsLogic
    {
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
                    foreach (var pair in second)
                    {
                        if (!result.ContainsKey(pair.Key))
                            result[pair.Key] = pair.Value;
                    }
                    break;

                case DictionaryMergeStrategy.KeepSecond:
                    foreach (var pair in second)
                    {
                        result[pair.Key] = pair.Value;
                    }
                    break;

                case DictionaryMergeStrategy.ThrowOnDuplicate:
                    var duplicates = first.Keys.Intersect(second.Keys).OrderBy(key => key).ToList();
                    if (duplicates.Any())
                    {
                        throw new InvalidOperationException(
                            $"Обнаружены дублирующиеся ключи: {string.Join(", ", duplicates)}");
                    }

                    foreach (var pair in second)
                    {
                        result[pair.Key] = pair.Value;
                    }
                    break;

                default:
                    throw new ArgumentException($"Неизвестная стратегия: {strategy}");
            }

            return result;
        }

        public Dictionary<string, string> Filter(
            Dictionary<string, string> source,
            string pattern,
            DictionaryFilterTarget target,
            DictionaryFilterMethod method,
            bool caseSensitive = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var comparison = ComparisonHelper.GetStringComparison(caseSensitive);

            bool Predicate(KeyValuePair<string, string> pair)
            {
                var keyMatches = Matches(pair.Key, pattern, method, comparison);
                var valueMatches = Matches(pair.Value, pattern, method, comparison);

                switch (target)
                {
                    case DictionaryFilterTarget.Keys:
                        return keyMatches;
                    case DictionaryFilterTarget.Values:
                        return valueMatches;
                    case DictionaryFilterTarget.KeysAndValues:
                        return keyMatches || valueMatches;
                    default:
                        throw new ArgumentException($"Неизвестная цель: {target}");
                }
            }

            return source.Where(Predicate).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public Dictionary<string, string> Invert(
            Dictionary<string, string> source,
            bool throwOnDuplicates = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var result = new Dictionary<string, string>();
            var duplicates = new List<string>();

            foreach (var pair in source)
            {
                if (result.ContainsKey(pair.Value))
                {
                    if (throwOnDuplicates)
                        duplicates.Add(pair.Value);
                    continue;
                }

                result[pair.Value] = pair.Key;
            }

            if (duplicates.Any())
            {
                throw new InvalidOperationException(
                    $"Обнаружены дублирующиеся значения: {string.Join(", ", duplicates)}");
            }

            return result;
        }

        private bool Matches(
            string text,
            string pattern,
            DictionaryFilterMethod method,
            StringComparison comparison)
        {
            if (text == null) return false;

            switch (method)
            {
                case DictionaryFilterMethod.Contains:
                    return text.IndexOf(pattern, comparison) >= 0;
                case DictionaryFilterMethod.Exact:
                    return string.Equals(text, pattern, comparison);
                case DictionaryFilterMethod.Regex:
                    var regexOptions = comparison == StringComparison.Ordinal
                        ? RegexOptions.None
                        : RegexOptions.IgnoreCase;
                    return Regex.IsMatch(text, pattern, regexOptions);
                case DictionaryFilterMethod.Wildcard:
                    return MatchesWildcard(text, pattern, comparison);
                default:
                    throw new ArgumentException($"Неизвестный метод: {method}");
            }
        }

        private bool MatchesWildcard(string text, string pattern, StringComparison comparison)
        {
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            var options = comparison == StringComparison.Ordinal
                ? RegexOptions.None
                : RegexOptions.IgnoreCase;

            return Regex.IsMatch(text, regexPattern, options);
        }
    }
}
