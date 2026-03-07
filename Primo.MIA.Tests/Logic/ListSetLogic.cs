using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>
    /// Бизнес-логика операций с множествами (списками)
    /// </summary>
    public class ListSetLogic
    {
        public List<string> PerformSetOperation(
            List<string> listA,
            List<string> listB,
            ListSetOperation operation)
        {
            if (listA == null)
                throw new ArgumentNullException(nameof(listA));

            switch (operation)
            {
                case ListSetOperation.Union:
                    if (listB == null) return listA.Distinct().ToList();
                    return listA.Union(listB).ToList();

                case ListSetOperation.Intersect:
                    if (listB == null) return new List<string>();
                    return listA.Intersect(listB).ToList();

                case ListSetOperation.Except:
                    if (listB == null) return listA.Distinct().ToList();
                    return listA.Except(listB).ToList();

                case ListSetOperation.ExceptReverse:
                    if (listB == null) return new List<string>();
                    return listB.Except(listA).ToList();

                case ListSetOperation.SymmetricDiff:
                    if (listB == null) return listA.Distinct().ToList();
                    var aExceptB = listA.Except(listB);
                    var bExceptA = listB.Except(listA);
                    return aExceptB.Union(bExceptA).ToList();

                case ListSetOperation.Distinct:
                    return listA.Distinct().ToList();

                default:
                    throw new InvalidOperationException($"Неизвестная операция: {operation}");
            }
        }
    }
}
