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

        /// <summary>
        /// Создаёт новый кортеж с заменённым элементом.
        /// Кортежи иммутабельны — возвращается новый экземпляр.
        /// </summary>
        /// <param name="tuple">Исходный кортеж.</param>
        /// <param name="index">Индекс элемента для замены (1-7).</param>
        /// <param name="newValue">Новое значение.</param>
        /// <returns>Новый кортеж с заменённым элементом.</returns>
        public object SetTupleItem(object tuple, TupleItemIndex index, object newValue)
        {
            if (tuple == null)
                throw new ArgumentNullException(nameof(tuple));

            var tupleType = tuple.GetType();
            if (!tupleType.Name.StartsWith("Tuple"))
                throw new ArgumentException("Объект не является кортежем");

            int itemNumber = (int)index;
            var items = DestructureTuple(tuple);

            if (itemNumber < 1 || itemNumber > items.Count)
                throw new ArgumentException($"Кортеж не содержит элемент Item{itemNumber}");

            // Заменяем элемент
            items[itemNumber - 1] = newValue;

            // Создаём новый кортеж
            return CreateTuple(items, TupleKind.ClassicTuple);
        }

        /// <summary>
        /// Определяет арность (количество элементов) кортежа.
        /// </summary>
        /// <param name="tuple">Кортеж.</param>
        /// <returns>Количество элементов или -1 если не кортеж.</returns>
        public int GetArity(object tuple)
        {
            if (tuple == null)
                return -1;

            var tupleType = tuple.GetType();
            if (!tupleType.Name.StartsWith("Tuple"))
                return -1;

            return tupleType.GetGenericArguments().Length;
        }
    }
}
