using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>
    /// Бизнес-логика группировки списков
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
                        .Where(s => !string.IsNullOrEmpty(s))
                        .GroupBy(s => s[0].ToString().ToUpper())
                        .ToDictionary(g => g.Key, g => g.ToList());

                case ListGroupMode.ByLength:
                    return source
                        .Where(s => s != null)
                        .GroupBy(s => s.Length.ToString())
                        .ToDictionary(g => g.Key, g => g.ToList());

                case ListGroupMode.ByPrefix:
                    return source
                        .Where(s => !string.IsNullOrEmpty(s))
                        .GroupBy(s => s.Length >= prefixLength 
                            ? s.Substring(0, prefixLength).ToUpper() 
                            : s.ToUpper())
                        .ToDictionary(g => g.Key, g => g.ToList());

                case ListGroupMode.ByRegexGroup:
                    if (string.IsNullOrEmpty(regexPattern))
                        throw new ArgumentException("Regex паттерн обязателен для режима ByRegexGroup");

                    var regex = new System.Text.RegularExpressions.Regex(regexPattern);
                    return source
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => new { Original = s, Match = regex.Match(s) })
                        .Where(x => x.Match.Success && x.Match.Groups.Count > 1)
                        .GroupBy(x => x.Match.Groups[1].Value)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.Original).ToList());

                case ListGroupMode.TopFrequent:
                    return source
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .GroupBy(s => s)
                        .OrderByDescending(g => g.Count())
                        .Take(topN)
                        .ToDictionary(g => $"{g.Key} ({g.Count()})", g => g.ToList());

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {mode}");
            }
        }
    }
}
