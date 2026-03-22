using System;
using System.Collections.Generic;
using System.Linq;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Бизнес-логика операций с множествами (списками).
    /// </summary>
    public class ListSetLogic
    {
        public List<string> PerformSetOperation(
            List<string> listA,
            List<string> listB,
            ListSetOperation operation,
            bool caseSensitive = false)
        {
            if (listA == null)
                throw new ArgumentNullException(nameof(listA));

            var comparer = ComparisonHelper.GetStringComparer(caseSensitive);

            switch (operation)
            {
                case ListSetOperation.Union:
                    if (listB == null) return listA.Distinct(comparer).ToList();
                    return listA.Union(listB, comparer).ToList();

                case ListSetOperation.Intersect:
                    if (listB == null) return new List<string>();
                    return listA.Intersect(listB, comparer).ToList();

                case ListSetOperation.Except:
                    if (listB == null) return listA.Distinct(comparer).ToList();
                    return listA.Except(listB, comparer).ToList();

                case ListSetOperation.ExceptReverse:
                    if (listB == null) return new List<string>();
                    return listB.Except(listA, comparer).ToList();

                case ListSetOperation.SymmetricDiff:
                    if (listB == null) return listA.Distinct(comparer).ToList();
                    var aExceptB = listA.Except(listB, comparer);
                    var bExceptA = listB.Except(listA, comparer);
                    return aExceptB.Union(bExceptA, comparer).ToList();

                case ListSetOperation.Distinct:
                    return listA.Distinct(comparer).ToList();

                default:
                    throw new InvalidOperationException($"Неизвестная операция: {operation}");
            }
        }
    }
}
