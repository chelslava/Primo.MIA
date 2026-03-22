using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    public class DictionaryGetValueResult
    {
        public string Value { get; set; }

        public bool Found { get; set; }
    }

    public class DictionarySetValueResult
    {
        public Dictionary<string, string> Dictionary { get; set; }

        public bool IsUpdate { get; set; }

        public int Count { get; set; }
    }

    public class DictionaryContainsResult
    {
        public bool Found { get; set; }

        public int Count { get; set; }

        public List<string> Keys { get; set; }
    }

    /// <summary>
    /// Бизнес-логика операций с ключами и значениями словаря.
    /// </summary>
    public class DictionaryKeyValueLogic
    {
        public DictionaryGetValueResult GetValue(
            Dictionary<string, string> dictionary,
            string key,
            string defaultValue = "",
            bool throwIfNotFound = false)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), "Словарь не может быть null");
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Ключ не может быть пустым", nameof(key));

            var found = dictionary.TryGetValue(key, out var value);

            if (!found)
            {
                if (throwIfNotFound)
                {
                    throw new KeyNotFoundException(
                        $"Ключ '{key}' не найден в словаре. " +
                        $"Доступные ключи: {string.Join(", ", dictionary.Keys.OrderBy(k => k))}");
                }

                value = defaultValue ?? string.Empty;
            }

            return new DictionaryGetValueResult
            {
                Value = value,
                Found = found
            };
        }

        public DictionarySetValueResult SetValue(
            Dictionary<string, string> dictionary,
            string key,
            string value)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), "Словарь не может быть null");
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Ключ не может быть пустым", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Значение не может быть null");

            var isUpdate = dictionary.ContainsKey(key);
            var result = dictionary.ToDictionary(pair => pair.Key, pair => pair.Value);
            result[key] = value;

            return new DictionarySetValueResult
            {
                Dictionary = result,
                IsUpdate = isUpdate,
                Count = result.Count
            };
        }

        public DictionaryContainsResult ContainsKey(
            Dictionary<string, string> dictionary,
            string key)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), "Словарь не может быть null");

            var found = !string.IsNullOrEmpty(key) && dictionary.ContainsKey(key);

            return new DictionaryContainsResult
            {
                Found = found,
                Count = found ? 1 : 0,
                Keys = found ? new List<string> { key } : new List<string>()
            };
        }

        public DictionaryContainsResult ContainsValue(
            Dictionary<string, string> dictionary,
            string value,
            bool caseSensitive = false)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), "Словарь не может быть null");
            if (value == null)
            {
                return new DictionaryContainsResult
                {
                    Found = false,
                    Count = 0,
                    Keys = new List<string>()
                };
            }

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var keys = dictionary
                .Where(pair => string.Equals(pair.Value, value, comparison))
                .Select(pair => pair.Key)
                .OrderBy(key => key)
                .ToList();

            return new DictionaryContainsResult
            {
                Found = keys.Count > 0,
                Count = keys.Count,
                Keys = keys
            };
        }

        public DictionarySetValueResult RemoveKey(
            Dictionary<string, string> dictionary,
            string key)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), "Словарь не может быть null");

            var result = dictionary.ToDictionary(pair => pair.Key, pair => pair.Value);
            var found = result.Remove(key);

            return new DictionarySetValueResult
            {
                Dictionary = result,
                IsUpdate = found,
                Count = result.Count
            };
        }

        public DictionarySetValueResult Clear(Dictionary<string, string> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), "Словарь не может быть null");

            return new DictionarySetValueResult
            {
                Dictionary = new Dictionary<string, string>(),
                IsUpdate = dictionary.Count > 0,
                Count = 0
            };
        }
    }
}
