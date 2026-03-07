using System;
using System.Collections.Generic;
using Primo.MIA;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>
    /// Бизнес-логика операций с кортежами
    /// </summary>
    public class TupleOperationsLogic
    {
        /// <summary>
        /// Создает кортеж из списка значений
        /// </summary>
        public object CreateTuple(List<object> values, TupleKind kind)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (values.Count < 1 || values.Count > 7)
                throw new ArgumentException("Кортеж должен содержать от 1 до 7 элементов");

            // Для простоты тестирования используем Tuple (ClassicTuple)
            // ValueTuple требует более сложной работы через рефлексию
            switch (values.Count)
            {
                case 1:
                    return System.Tuple.Create(values[0]);
                case 2:
                    return System.Tuple.Create(values[0], values[1]);
                case 3:
                    return System.Tuple.Create(values[0], values[1], values[2]);
                case 4:
                    return System.Tuple.Create(values[0], values[1], values[2], values[3]);
                case 5:
                    return System.Tuple.Create(values[0], values[1], values[2], values[3], values[4]);
                case 6:
                    return System.Tuple.Create(values[0], values[1], values[2], values[3], values[4], values[5]);
                case 7:
                    return System.Tuple.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6]);
                default:
                    throw new ArgumentException("Неподдерживаемое количество элементов");
            }
        }

        /// <summary>
        /// Извлекает элемент кортежа по индексу
        /// </summary>
        public object GetTupleItem(object tuple, TupleItemIndex index)
        {
            if (tuple == null)
                throw new ArgumentNullException(nameof(tuple));

            var tupleType = tuple.GetType();
            if (!tupleType.Name.StartsWith("Tuple"))
                throw new ArgumentException("Объект не является кортежем");

            var property = tupleType.GetProperty($"Item{(int)index}");
            if (property == null)
                throw new ArgumentException($"Кортеж не содержит элемент Item{(int)index}");

            return property.GetValue(tuple);
        }

        /// <summary>
        /// Деструктурирует кортеж в список
        /// </summary>
        public List<object> DestructureTuple(object tuple)
        {
            if (tuple == null)
                throw new ArgumentNullException(nameof(tuple));

            var tupleType = tuple.GetType();
            if (!tupleType.Name.StartsWith("Tuple"))
                throw new ArgumentException("Объект не является кортежем");

            var result = new List<object>();
            for (int i = 1; i <= 7; i++)
            {
                var property = tupleType.GetProperty($"Item{i}");
                if (property == null) break;
                result.Add(property.GetValue(tuple));
            }

            return result;
        }

        /// <summary>
        /// Конвертирует кортеж в словарь
        /// </summary>
        public Dictionary<string, object> TupleToDictionary(object tuple)
        {
            if (tuple == null)
                throw new ArgumentNullException(nameof(tuple));

            var items = DestructureTuple(tuple);
            var result = new Dictionary<string, object>();

            for (int i = 0; i < items.Count; i++)
            {
                result[$"Item{i + 1}"] = items[i];
            }

            return result;
        }
    }
}
