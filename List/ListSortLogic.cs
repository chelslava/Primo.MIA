using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Бизнес-логика сортировки списков.
    /// </summary>
    public class ListSortLogic
    {
        public List<string> Sort(List<string> source, ListSortMode mode, int? randomSeed = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var result = new List<string>(source);

            switch (mode)
            {
                case ListSortMode.Alphabetical:
                    result.Sort(StringComparer.Ordinal);
                    break;

                case ListSortMode.AlphabeticalDesc:
                    result.Sort(StringComparer.Ordinal);
                    result.Reverse();
                    break;

                case ListSortMode.CaseInsensitive:
                    result.Sort(StringComparer.OrdinalIgnoreCase);
                    break;

                case ListSortMode.CaseInsensitiveDesc:
                    result.Sort(StringComparer.OrdinalIgnoreCase);
                    result.Reverse();
                    break;

                case ListSortMode.ByLength:
                    result = result.OrderBy(s => s?.Length ?? 0).ToList();
                    break;

                case ListSortMode.ByLengthDesc:
                    result = result.OrderByDescending(s => s?.Length ?? 0).ToList();
                    break;

                case ListSortMode.Natural:
                    result = result.OrderBy(s => s, NaturalComparer.Instance).ToList();
                    break;

                case ListSortMode.Reverse:
                    result.Reverse();
                    break;

                case ListSortMode.Random:
                    Shuffle(result, randomSeed);
                    break;

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {mode}");
            }

            return result;
        }

        private static void Shuffle(List<string> list, int? randomSeed)
        {
            var random = randomSeed.HasValue
                ? new Random(randomSeed.Value)
                : new Random();

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
