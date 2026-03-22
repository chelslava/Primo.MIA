using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Бизнес-логика фильтрации списков.
    /// </summary>
    public class ListFilterLogic
    {
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
            var comparison = ComparisonHelper.GetStringComparison(caseSensitive);

            switch (mode)
            {
                case ListFilterMode.Contains:
                    return value => value != null && value.IndexOf(pattern, comparison) >= 0;

                case ListFilterMode.NotContains:
                    return value => value == null || value.IndexOf(pattern, comparison) < 0;

                case ListFilterMode.StartsWith:
                    return value => value != null && value.StartsWith(pattern, comparison);

                case ListFilterMode.EndsWith:
                    return value => value != null && value.EndsWith(pattern, comparison);

                case ListFilterMode.ExactMatch:
                    return value => string.Equals(value, pattern, comparison);

                case ListFilterMode.Regex:
                {
                    var options = caseSensitive
                        ? RegexOptions.Compiled
                        : RegexOptions.Compiled | RegexOptions.IgnoreCase;
                    var regex = new Regex(pattern, options, TimeSpan.FromSeconds(1));
                    return value => value != null && regex.IsMatch(value);
                }

                case ListFilterMode.NotRegex:
                {
                    var options = caseSensitive
                        ? RegexOptions.Compiled
                        : RegexOptions.Compiled | RegexOptions.IgnoreCase;
                    var regex = new Regex(pattern, options, TimeSpan.FromSeconds(1));
                    return value => value == null || !regex.IsMatch(value);
                }

                case ListFilterMode.NotEmpty:
                    return value => !string.IsNullOrWhiteSpace(value);

                case ListFilterMode.EmptyOnly:
                    return value => string.IsNullOrWhiteSpace(value);

                case ListFilterMode.LengthRange:
                    return value => value != null && value.Length >= minLength && value.Length <= maxLength;

                case ListFilterMode.NumericOnly:
                    return value => !string.IsNullOrWhiteSpace(value)
                        && double.TryParse(
                            value,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out _);

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {mode}");
            }
        }
    }

    public class FilterResult
    {
        public List<string> Matched { get; set; }

        public List<string> Rejected { get; set; }
    }
}
