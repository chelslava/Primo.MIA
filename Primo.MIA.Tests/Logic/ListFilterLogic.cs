using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>
    /// Бизнес-логика фильтрации списков, извлеченная для тестирования.
    /// Не зависит от инфраструктуры Primo RPA.
    /// </summary>
    public class ListFilterLogic
    {
        /// <summary>
        /// Фильтрует список строк по заданному условию
        /// </summary>
        public FilterResult Filter(
            List<string> source,
            string pattern,
            ListFilterMode mode,
            bool caseSensitive = false,
            int minLength = 0,
            int maxLength = int.MaxValue)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var predicate = BuildPredicate(pattern, mode, caseSensitive, minLength, maxLength);
            var lookup = source.ToLookup(predicate);

            return new FilterResult
            {
                Matched = lookup[true].ToList(),
                Rejected = lookup[false].ToList()
            };
        }

        private Func<string, bool> BuildPredicate(
            string pattern,
            ListFilterMode mode,
            bool caseSensitive,
            int minLength,
            int maxLength)
        {
            StringComparison sc = caseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            switch (mode)
            {
                case ListFilterMode.Contains:
                    return s => s != null && s.IndexOf(pattern, sc) >= 0;

                case ListFilterMode.NotContains:
                    return s => s == null || s.IndexOf(pattern, sc) < 0;

                case ListFilterMode.StartsWith:
                    return s => s != null && s.StartsWith(pattern, sc);

                case ListFilterMode.EndsWith:
                    return s => s != null && s.EndsWith(pattern, sc);

                case ListFilterMode.ExactMatch:
                    return s => string.Equals(s, pattern, sc);

                case ListFilterMode.Regex:
                {
                    var opts = caseSensitive
                        ? RegexOptions.Compiled
                        : RegexOptions.Compiled | RegexOptions.IgnoreCase;
                    var rx = new Regex(pattern, opts, TimeSpan.FromSeconds(1));
                    return s => s != null && rx.IsMatch(s);
                }

                case ListFilterMode.NotRegex:
                {
                    var opts = caseSensitive
                        ? RegexOptions.Compiled
                        : RegexOptions.Compiled | RegexOptions.IgnoreCase;
                    var rx = new Regex(pattern, opts, TimeSpan.FromSeconds(1));
                    return s => s == null || !rx.IsMatch(s);
                }

                case ListFilterMode.NotEmpty:
                    return s => !string.IsNullOrWhiteSpace(s);

                case ListFilterMode.EmptyOnly:
                    return s => string.IsNullOrWhiteSpace(s);

                case ListFilterMode.LengthRange:
                    return s => s != null && s.Length >= minLength && s.Length <= maxLength;

                case ListFilterMode.NumericOnly:
                    return s => !string.IsNullOrWhiteSpace(s)
                             && double.TryParse(s,
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out _);

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {mode}");
            }
        }

        public class FilterResult
        {
            public List<string> Matched { get; set; }
            public List<string> Rejected { get; set; }
        }
    }
}
