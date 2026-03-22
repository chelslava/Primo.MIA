using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    public class ListSliceResult
    {
        public List<string> Result { get; set; }

        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Бизнес-логика получения срезов списков.
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
            return SliceDetailed(source, mode, count, page, pageSize, fromIndex, toIndex, step).Result;
        }

        public ListSliceResult SliceDetailed(
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

            var result = new ListSliceResult
            {
                Result = new List<string>(),
                TotalPages = 0
            };

            switch (mode)
            {
                case ListSliceMode.FirstN:
                    result.Result = source.Take(count).ToList();
                    break;

                case ListSliceMode.LastN:
                    result.Result = source.Skip(Math.Max(0, source.Count - count)).ToList();
                    break;

                case ListSliceMode.SkipFirst:
                    result.Result = source.Skip(count).ToList();
                    break;

                case ListSliceMode.SkipLast:
                    result.Result = source.Take(Math.Max(0, source.Count - count)).ToList();
                    break;

                case ListSliceMode.Page:
                    if (page < 1)
                        throw new ArgumentException("Номер страницы должен быть >= 1");
                    if (pageSize < 1)
                        throw new ArgumentException("Размер страницы должен быть >= 1");
                    result.TotalPages = (int)Math.Ceiling((double)source.Count / pageSize);
                    result.Result = source.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                    break;

                case ListSliceMode.Range:
                    fromIndex = Math.Max(0, fromIndex);
                    toIndex = Math.Min(source.Count - 1, toIndex);
                    if (fromIndex > toIndex)
                        break;
                    result.Result = source.Skip(fromIndex).Take(toIndex - fromIndex + 1).ToList();
                    break;

                case ListSliceMode.EveryNth:
                    if (step < 1)
                        throw new ArgumentException("Шаг должен быть >= 1");
                    result.Result = source
                        .Where((item, index) => index % step == 0)
                        .ToList();
                    break;

                default:
                    throw new InvalidOperationException($"Неизвестный режим: {mode}");
            }

            return result;
        }
    }
}
