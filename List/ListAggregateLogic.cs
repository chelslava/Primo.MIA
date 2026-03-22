using System;
using System.Collections.Generic;
using System.Linq;
using Primo.MIA.Common;

namespace Primo.MIA
{
    public class ListAggregateResult
    {
        public object Value { get; set; }

        public double NumericResult { get; set; }

        public string StringResult { get; set; }

        public int NumericCount { get; set; }
    }

    /// <summary>
    /// Бизнес-логика агрегации списков.
    /// </summary>
    public class ListAggregateLogic
    {
        public object Aggregate(
            List<string> source,
            ListAggregateMode mode,
            string separator = ", ")
        {
            return AggregateDetailed(source, mode, separator).Value;
        }

        public ListAggregateResult AggregateDetailed(
            List<string> source,
            ListAggregateMode mode,
            string separator = ", ")
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var numbers = source
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(StringHelper.TryParseDouble)
                .Where(value => value.HasValue)
                .Select(value => value.Value)
                .ToList();

            var result = new ListAggregateResult
            {
                NumericResult = 0,
                StringResult = string.Empty,
                NumericCount = numbers.Count
            };

            switch (mode)
            {
                case ListAggregateMode.Count:
                    result.NumericResult = source.Count;
                    result.Value = source.Count;
                    break;

                case ListAggregateMode.CountDistinct:
                    result.NumericResult = source
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count();
                    result.Value = (int)result.NumericResult;
                    break;

                case ListAggregateMode.CountNonEmpty:
                    result.NumericResult = source.Count(s => !string.IsNullOrWhiteSpace(s));
                    result.Value = (int)result.NumericResult;
                    break;

                case ListAggregateMode.Sum:
                    result.NumericResult = numbers.Any() ? numbers.Sum() : 0;
                    result.Value = result.NumericResult;
                    break;

                case ListAggregateMode.Min:
                    if (!numbers.Any())
                        throw new InvalidOperationException("Нет числовых элементов для вычисления Min");
                    result.NumericResult = numbers.Min();
                    result.Value = result.NumericResult;
                    break;

                case ListAggregateMode.Max:
                    if (!numbers.Any())
                        throw new InvalidOperationException("Нет числовых элементов для вычисления Max");
                    result.NumericResult = numbers.Max();
                    result.Value = result.NumericResult;
                    break;

                case ListAggregateMode.Average:
                    if (!numbers.Any())
                        throw new InvalidOperationException("Нет числовых элементов для вычисления Average");
                    result.NumericResult = numbers.Average();
                    result.Value = result.NumericResult;
                    break;

                case ListAggregateMode.ShortestString:
                    result.StringResult = source
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .OrderBy(s => s.Length)
                        .ThenBy(s => s)
                        .FirstOrDefault() ?? string.Empty;
                    result.Value = result.StringResult;
                    break;

                case ListAggregateMode.LongestString:
                    result.StringResult = source
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .OrderByDescending(s => s.Length)
                        .ThenBy(s => s)
                        .FirstOrDefault() ?? string.Empty;
                    result.Value = result.StringResult;
                    break;

                case ListAggregateMode.Join:
                    result.StringResult = string.Join(separator, source.Where(s => s != null));
                    result.NumericResult = source.Count;
                    result.Value = result.StringResult;
                    break;

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {mode}");
            }

            return result;
        }
    }
}
