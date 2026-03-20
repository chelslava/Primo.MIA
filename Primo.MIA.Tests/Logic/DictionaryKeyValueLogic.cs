// =============================================================================
// DictionaryKeyValueLogic.cs — бизнес-логика операций с ключами/значениями.
//
// Извлечена из DictionaryGetValueBack.cs и DictionarySetValueBack.cs
// для unit-тестирования без зависимостей от Primo RPA SDK.
//
// Операции:
//   GetValue    — получить значение по ключу
//   SetValue    — установить значение (добавить или обновить)
//   ContainsKey — проверить наличие ключа
//   ContainsValue — проверить наличие значения
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>
    /// Результат получения значения из словаря.
    /// </summary>
    public class DictionaryGetValueResult
    {
        /// <summary>Найденное значение или значение по умолчанию.</summary>
        public string Value { get; set; }

        /// <summary>True если ключ был найден в словаре.</summary>
        public bool Found { get; set; }
    }

    /// <summary>
    /// Результат установки значения в словарь.
    /// </summary>
    public class DictionarySetValueResult
    {
        /// <summary>Новый словарь с установленным значением.</summary>
        public Dictionary<string, string> Dictionary { get; set; }

        /// <summary>True если ключ уже существовал (обновление), false если добавлен новый.</summary>
        public bool IsUpdate { get; set; }

        /// <summary>Количество элементов в словаре.</summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// Результат проверки наличия ключа/значения.
    /// </summary>
    public class DictionaryContainsResult
    {
        /// <summary>True если ключ/значение найдены.</summary>
        public bool Found { get; set; }

        /// <summary>Количество вхождений (для ContainsValue).</summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// Бизнес-логика операций с ключами и значениями словаря.
    /// </summary>
    public class DictionaryKeyValueLogic
    {
        /// <summary>
        /// Получает значение из словаря по ключу.
        /// </summary>
        /// <param name="dictionary">Исходный словарь.</param>
        /// <param name="key">Ключ для поиска.</param>
        /// <param name="defaultValue">Значение по умолчанию если ключ не найден.</param>
        /// <param name="throwIfNotFound">
        /// true — выбросить KeyNotFoundException при отсутствии ключа.
        /// false — вернуть defaultValue.
        /// </param>
        /// <returns>Результат с найденным значением и флагом Found.</returns>
        /// <exception cref="ArgumentNullException">Если dictionary null.</exception>
        /// <exception cref="ArgumentException">Если ключ пустой.</exception>
        /// <exception cref="KeyNotFoundException">Если throwIfNotFound=true и ключ не найден.</exception>
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

            bool found = dictionary.TryGetValue(key, out string value);

            if (!found)
            {
                if (throwIfNotFound)
                    throw new KeyNotFoundException(
                        $"Ключ '{key}' не найден в словаре. " +
                        $"Доступные ключи: {string.Join(", ", dictionary.Keys.OrderBy(k => k))}");

                value = defaultValue ?? string.Empty;
            }

            return new DictionaryGetValueResult
            {
                Value = value,
                Found = found
            };
        }

        /// <summary>
        /// Устанавливает значение в словаре (добавляет или обновляет).
        /// Возвращает новую копию словаря — оригинал не изменяется.
        /// </summary>
        /// <param name="dictionary">Исходный словарь.</param>
        /// <param name="key">Ключ для установки.</param>
        /// <param name="value">Значение для установки.</param>
        /// <returns>Результат с новым словарём и флагом IsUpdate.</returns>
        /// <exception cref="ArgumentNullException">Если dictionary или value null.</exception>
        /// <exception cref="ArgumentException">Если ключ пустой.</exception>
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

            // Фиксируем: был ли ключ до установки
            bool isUpdate = dictionary.ContainsKey(key);

            // Создаём копию и устанавливаем значение — оригинал не трогаем
            var result = dictionary.ToDictionary(p => p.Key, p => p.Value);
            result[key] = value;

            return new DictionarySetValueResult
            {
                Dictionary = result,
                IsUpdate = isUpdate,
                Count = result.Count
            };
        }

        /// <summary>
        /// Проверяет наличие ключа в словаре.
        /// </summary>
        /// <param name="dictionary">Исходный словарь.</param>
        /// <param name="key">Ключ для поиска.</param>
        /// <returns>Результат с флагом Found.</returns>
        /// <exception cref="ArgumentNullException">Если dictionary null.</exception>
        public DictionaryContainsResult ContainsKey(
            Dictionary<string, string> dictionary,
            string key)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), "Словарь не может быть null");

            bool found = !string.IsNullOrEmpty(key) && dictionary.ContainsKey(key);

            return new DictionaryContainsResult
            {
                Found = found,
                Count = found ? 1 : 0
            };
        }

        /// <summary>
        /// Проверяет наличие значения в словаре.
        /// </summary>
        /// <param name="dictionary">Исходный словарь.</param>
        /// <param name="value">Значение для поиска.</param>
        /// <param name="caseSensitive">Учитывать регистр при поиске.</param>
        /// <returns>Результат с флагом Found и количеством вхождений.</returns>
        /// <exception cref="ArgumentNullException">Если dictionary null.</exception>
        public DictionaryContainsResult ContainsValue(
            Dictionary<string, string> dictionary,
            string value,
            bool caseSensitive = false)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), "Словарь не может быть null");

            if (value == null)
                return new DictionaryContainsResult { Found = false, Count = 0 };

            int count;
            if (caseSensitive)
            {
                count = dictionary.Values.Count(v => v == value);
            }
            else
            {
                count = dictionary.Values.Count(v => 
                    string.Equals(v, value, StringComparison.OrdinalIgnoreCase));
            }

            return new DictionaryContainsResult
            {
                Found = count > 0,
                Count = count
            };
        }

        /// <summary>
        /// Удаляет ключ из словаря.
        /// Возвращает новую копию словаря — оригинал не изменяется.
        /// </summary>
        /// <param name="dictionary">Исходный словарь.</param>
        /// <param name="key">Ключ для удаления.</param>
        /// <returns>Результат с новым словарём и флагом Found.</returns>
        /// <exception cref="ArgumentNullException">Если dictionary null.</exception>
        public DictionarySetValueResult RemoveKey(
            Dictionary<string, string> dictionary,
            string key)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), "Словарь не может быть null");

            // Создаём копию
            var result = dictionary.ToDictionary(p => p.Key, p => p.Value);

            bool found = result.Remove(key);

            return new DictionarySetValueResult
            {
                Dictionary = result,
                IsUpdate = found, // IsUpdate означает "был удалён"
                Count = result.Count
            };
        }

        /// <summary>
        /// Очищает словарь (возвращает новый пустой).
        /// </summary>
        /// <param name="dictionary">Исходный словарь.</param>
        /// <returns>Пустой словарь.</returns>
        /// <exception cref="ArgumentNullException">Если dictionary null.</exception>
        public DictionarySetValueResult Clear(
            Dictionary<string, string> dictionary)
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
