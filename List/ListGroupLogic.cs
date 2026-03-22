using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    /// <summary>
    /// Бизнес-логика группировки списков.
    /// </summary>
    public class ListGroupLogic
    {
        public Dictionary<string, List<string>> Group(
            List<string> source,
            ListGroupMode mode,
            int prefixLength = 1,
            string regexPattern = null,
            int topN = 10)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            switch (mode)
            {
                case ListGroupMode.ByFirstChar:
                    return source
                        .Where(value => !string.IsNullOrEmpty(value))
                        .GroupBy(value => value[0].ToString().ToUpper())
                        .ToDictionary(group => group.Key, group => group.ToList());

                case ListGroupMode.ByLength:
                    return source
                        .Where(value => value != null)
                        .GroupBy(value => value.Length.ToString())
                        .ToDictionary(group => group.Key, group => group.ToList());

                case ListGroupMode.ByPrefix:
                    return source
                        .Where(value => !string.IsNullOrEmpty(value))
                        .GroupBy(value => value.Length >= prefixLength
                            ? value.Substring(0, prefixLength).ToUpper()
                            : value.ToUpper())
                        .ToDictionary(group => group.Key, group => group.ToList());

                case ListGroupMode.ByRegexGroup:
                    if (string.IsNullOrEmpty(regexPattern))
                        throw new ArgumentException("Regex паттерн обязателен для режима ByRegexGroup");

                    var regex = new Regex(regexPattern);
                    return source
                        .Where(value => !string.IsNullOrEmpty(value))
                        .Select(value => new { Original = value, Match = regex.Match(value) })
                        .Where(item => item.Match.Success && item.Match.Groups.Count > 1)
                        .GroupBy(item => item.Match.Groups[1].Value)
                        .ToDictionary(group => group.Key, group => group.Select(item => item.Original).ToList());

                case ListGroupMode.TopFrequent:
                    return source
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .GroupBy(value => value)
                        .OrderByDescending(group => group.Count())
                        .Take(topN)
                        .ToDictionary(group => $"{group.Key} ({group.Count()})", group => group.ToList());

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {mode}");
            }
        }
    }
}
