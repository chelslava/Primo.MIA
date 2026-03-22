using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    public class DictionaryCreateResult
    {
        public Dictionary<string, string> Dictionary { get; set; }

        public int Count { get; set; }

        public int DuplicatesSkipped { get; set; }
    }

    public class DictionaryCreateLogic
    {
        public DictionaryCreateResult CreateEmpty()
        {
            return new DictionaryCreateResult
            {
                Dictionary = new Dictionary<string, string>(),
                Count = 0,
                DuplicatesSkipped = 0
            };
        }

        public DictionaryCreateResult FromLists(List<string> keysList, List<string> valuesList)
        {
            if (keysList == null)
                throw new ArgumentNullException(nameof(keysList), "Список ключей не может быть null");
            if (valuesList == null)
                throw new ArgumentNullException(nameof(valuesList), "Список значений не может быть null");
            if (keysList.Count != valuesList.Count)
            {
                throw new ArgumentException(
                    $"Длина списка ключей ({keysList.Count}) " +
                    $"не совпадает с длиной списка значений ({valuesList.Count})");
            }

            var result = keysList
                .Zip(valuesList, (key, value) => new { Key = key, Value = value })
                .GroupBy(pair => pair.Key)
                .ToDictionary(group => group.Key, group => group.Last().Value);

            return new DictionaryCreateResult
            {
                Dictionary = result,
                Count = result.Count,
                DuplicatesSkipped = 0
            };
        }

        public DictionaryCreateResult Clone(Dictionary<string, string> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Словарь не может быть null для режима Clone");

            var result = source.ToDictionary(pair => pair.Key, pair => pair.Value);
            return new DictionaryCreateResult
            {
                Dictionary = result,
                Count = result.Count,
                DuplicatesSkipped = 0
            };
        }

        public DictionaryCreateResult Invert(Dictionary<string, string> source, bool throwOnDuplicates = false)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Словарь не может быть null для режима Invert");

            var duplicates = source.Values
                .GroupBy(value => value)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicates.Any() && throwOnDuplicates)
            {
                throw new InvalidOperationException(
                    $"Инверсия невозможна — дублирующиеся значения: {string.Join(", ", duplicates)}. " +
                    "Отключите 'Ошибка при дублях' чтобы сохранить первое вхождение.");
            }

            var result = source
                .GroupBy(pair => pair.Value)
                .ToDictionary(group => group.Key, group => group.First().Key);

            return new DictionaryCreateResult
            {
                Dictionary = result,
                Count = result.Count,
                DuplicatesSkipped = source.Count - result.Count
            };
        }
    }
}
