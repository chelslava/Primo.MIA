using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>
    /// Бизнес-логика получения срезов списков
    /// </summary>
    public class ListSliceLogic
    {
        public List<string> Slice(
            List<string> source,
            ListSliceMode mode,
            int count = 0,
            int page = 1,
            int pageSize = 10,
            int fromIndex = 0,
            int toIndex = 0,
            int step = 1)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            switch (mode)
            {
                case ListSliceMode.FirstN:
                    return source.Take(Math.Max(0, count)).ToList();

                case ListSliceMode.LastN:
                    return source.Skip(Math.Max(0, source.Count - count)).ToList();

                case ListSliceMode.SkipFirst:
                    return source.Skip(Math.Max(0, count)).ToList();

                case ListSliceMode.SkipLast:
                    return source.Take(Math.Max(0, source.Count - count)).ToList();

                case ListSliceMode.Page:
                    int skip = (Math.Max(1, page) - 1) * Math.Max(1, pageSize);
                    return source.Skip(skip).Take(Math.Max(1, pageSize)).ToList();

                case ListSliceMode.Range:
                    int start = Math.Max(0, Math.Min(fromIndex, source.Count));
                    int end = Math.Max(0, Math.Min(toIndex, source.Count));
                    if (start >= end) return new List<string>();
                    return source.Skip(start).Take(end - start).ToList();

                case ListSliceMode.EveryNth:
                    if (step <= 0)
                        throw new ArgumentException("Step должен быть больше 0");
                    return source.Where((item, index) => index % step == 0).ToList();

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {mode}");
            }
        }
    }
}
