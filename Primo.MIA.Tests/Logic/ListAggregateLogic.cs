using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>
    /// Бизнес-логика агрегации списков
    /// </summary>
    public class ListAggregateLogic
    {
        public object Aggregate(
            List<string> source,
            ListAggregateMode mode,
            string separator = ", ")
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            switch (mode)
            {
                case ListAggregateMode.Count:
                    return source.Count;

                case ListAggregateMode.CountDistinct:
                    return source.Where(s => !string.IsNullOrWhiteSpace(s))
                                 .Distinct()
                                 .Count();

                case ListAggregateMode.CountNonEmpty:
                    return source.Count(s => !string.IsNullOrWhiteSpace(s));

                case ListAggregateMode.Sum:
                    return source.Where(s => double.TryParse(s, 
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture, 
                                    out _))
                                 .Sum(s => double.Parse(s, 
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture));

                case ListAggregateMode.Min:
                    var minValues = source.Where(s => double.TryParse(s,
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out _))
                                          .Select(s => double.Parse(s,
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture));
                    return minValues.Any() ? minValues.Min() : 0.0;

                case ListAggregateMode.Max:
                    var maxValues = source.Where(s => double.TryParse(s,
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out _))
                                          .Select(s => double.Parse(s,
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture));
                    return maxValues.Any() ? maxValues.Max() : 0.0;

                case ListAggregateMode.Average:
                    var avgValues = source.Where(s => double.TryParse(s,
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out _))
                                          .Select(s => double.Parse(s,
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture));
                    return avgValues.Any() ? avgValues.Average() : 0.0;

                case ListAggregateMode.ShortestString:
                    var nonEmpty = source.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                    return nonEmpty.Any() ? nonEmpty.OrderBy(s => s.Length).First() : string.Empty;

                case ListAggregateMode.LongestString:
                    var nonEmptyLong = source.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                    return nonEmptyLong.Any() ? nonEmptyLong.OrderByDescending(s => s.Length).First() : string.Empty;

                case ListAggregateMode.Join:
                    return string.Join(separator, source.Where(s => s != null));

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {mode}");
            }
        }
    }
}
