using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Тонкий слой бизнес-логики над TupleHelper для unit-тестов и повторного использования.
    /// </summary>
    public class TupleOperationsLogic
    {
        public object CreateTuple(List<object> values, TupleKind kind)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (values.Count < 1 || values.Count > 7)
                throw new ArgumentException("Кортеж должен содержать от 1 до 7 элементов");

            return TupleHelper.CreateFromArray(values.ToArray(), values.Count, useValueTuple: kind == TupleKind.ValueTuple);
        }

        public object GetTupleItem(object tuple, TupleItemIndex index)
        {
            if (tuple == null)
                throw new ArgumentNullException(nameof(tuple));

            int arity = TupleHelper.GetArity(tuple);
            if (arity < 0)
                throw new ArgumentException("Объект не является кортежем");

            int itemNumber = (int)index;
            if (itemNumber < 1 || itemNumber > arity)
                throw new ArgumentException($"Кортеж не содержит элемент Item{itemNumber}");

            return TupleHelper.GetItem(tuple, itemNumber);
        }

        public List<object> DestructureTuple(object tuple)
        {
            if (tuple == null)
                throw new ArgumentNullException(nameof(tuple));

            int arity = TupleHelper.GetArity(tuple);
            if (arity < 0)
                throw new ArgumentException("Объект не является кортежем");

            var result = new List<object>(arity);
            for (int itemNumber = 1; itemNumber <= arity; itemNumber++)
                result.Add(TupleHelper.GetItem(tuple, itemNumber));

            return result;
        }

        public Dictionary<string, object> TupleToDictionary(object tuple)
        {
            var items = DestructureTuple(tuple);
            return items
                .Select((value, index) => new { Key = $"Item{index + 1}", Value = value })
                .ToDictionary(item => item.Key, item => item.Value);
        }

        public object SetTupleItem(object tuple, TupleItemIndex index, object newValue)
        {
            if (tuple == null)
                throw new ArgumentNullException(nameof(tuple));

            int arity = TupleHelper.GetArity(tuple);
            if (arity < 0)
                throw new ArgumentException("Объект не является кортежем");

            int itemNumber = (int)index;
            if (itemNumber < 1 || itemNumber > arity)
                throw new ArgumentException($"Кортеж не содержит элемент Item{itemNumber}");

            return TupleHelper.WithItem(tuple, itemNumber, newValue);
        }

        public int GetArity(object tuple)
        {
            return TupleHelper.GetArity(tuple);
        }
    }
}
