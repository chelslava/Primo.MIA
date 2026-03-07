using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>
    /// Бизнес-логика сортировки списков
    /// </summary>
    public class ListSortLogic
    {
        private static readonly Random _random = new Random();

        public List<string> Sort(List<string> source, ListSortMode mode)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // Создаем копию чтобы не изменять оригинал
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
                    result = result.OrderBy(s => s, new NaturalStringComparer()).ToList();
                    break;

                case ListSortMode.Reverse:
                    result.Reverse();
                    break;

                case ListSortMode.Random:
                    // Fisher-Yates shuffle
                    for (int i = result.Count - 1; i > 0; i--)
                    {
                        int j = _random.Next(i + 1);
                        var temp = result[i];
                        result[i] = result[j];
                        result[j] = temp;
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {mode}");
            }

            return result;
        }

        /// <summary>
        /// Компаратор для натуральной сортировки (file1, file2, file10 вместо file1, file10, file2)
        /// </summary>
        private class NaturalStringComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                int ix = 0, iy = 0;

                while (ix < x.Length && iy < y.Length)
                {
                    if (char.IsDigit(x[ix]) && char.IsDigit(y[iy]))
                    {
                        // Извлекаем числа
                        var numX = ExtractNumber(x, ref ix);
                        var numY = ExtractNumber(y, ref iy);

                        int cmp = numX.CompareTo(numY);
                        if (cmp != 0) return cmp;
                    }
                    else
                    {
                        int cmp = string.Compare(x[ix].ToString(), y[iy].ToString(), 
                            StringComparison.OrdinalIgnoreCase);
                        if (cmp != 0) return cmp;
                        ix++;
                        iy++;
                    }
                }

                return x.Length.CompareTo(y.Length);
            }

            private long ExtractNumber(string str, ref int index)
            {
                int start = index;
                while (index < str.Length && char.IsDigit(str[index]))
                    index++;

                return long.Parse(str.Substring(start, index - start));
            }
        }
    }
}
