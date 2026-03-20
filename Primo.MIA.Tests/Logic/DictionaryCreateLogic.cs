// =============================================================================
// DictionaryCreateLogic.cs — бизнес-логика создания словарей.
//
// Извлечена из DictionaryCreateBack.cs для unit-тестирования без зависимостей
// от Primo RPA SDK.
//
// Режимы:
//   CreateEmpty — новый пустой Dictionary<string, string>
//   FromLists   — словарь из двух List<string> (ключи + значения)
//   Clone       — полная независимая копия существующего словаря
//   Invert      — инверсия: значения → ключи, ключи → значения
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>
    /// Результат создания словаря.
    /// </summary>
    public class DictionaryCreateResult
    {
        /// <summary>Созданный словарь.</summary>
        public Dictionary<string, string> Dictionary { get; set; }

        /// <summary>Количество элементов в словаре.</summary>
        public int Count { get; set; }

        /// <summary>Количество пропущенных дублей при инверсии.</summary>
        public int DuplicatesSkipped { get; set; }
    }

    /// <summary>
    /// Бизнес-логика создания словарей.
    /// </summary>
    public class DictionaryCreateLogic
    {
        /// <summary>
        /// Создаёт новый пустой словарь.
        /// </summary>
        /// <returns>Пустой Dictionary<string, string>.</returns>
        public DictionaryCreateResult CreateEmpty()
        {
            return new DictionaryCreateResult
            {
                Dictionary = new Dictionary<string, string>(),
                Count = 0,
                DuplicatesSkipped = 0
            };
        }

        /// <summary>
        /// Создаёт словарь из двух списков одинаковой длины.
        /// При дублирующихся ключах побеждает последнее значение.
        /// </summary>
        /// <param name="keysList">Список ключей.</param>
        /// <param name="valuesList">Список значений.</param>
        /// <returns>Созданный словарь.</returns>
        /// <exception cref="ArgumentNullException">Если один из списков null.</exception>
        /// <exception cref="ArgumentException">Если списки разной длины.</exception>
        public DictionaryCreateResult FromLists(List<string> keysList, List<string> valuesList)
        {
            if (keysList == null)
                throw new ArgumentNullException(nameof(keysList), "Список ключей не может быть null");
            if (valuesList == null)
                throw new ArgumentNullException(nameof(valuesList), "Список значений не может быть null");
            if (keysList.Count != valuesList.Count)
                throw new ArgumentException(
                    $"Длина списка ключей ({keysList.Count}) " +
                    $"не совпадает с длиной списка значений ({valuesList.Count})");

            // Zip попарно объединяет списки, GroupBy обрабатывает дубли ключей
            var result = keysList
                .Zip(valuesList, (k, v) => new { Key = k, Value = v })
                .GroupBy(pair => pair.Key)
                .ToDictionary(
                    group => group.Key,
                    group => group.Last().Value   // при дубле побеждает последний
                );

            return new DictionaryCreateResult
            {
                Dictionary = result,
                Count = result.Count,
                DuplicatesSkipped = 0
            };
        }

        /// <summary>
        /// Создаёт полную независимую копию словаря.
        /// </summary>
        /// <param name="source">Исходный словарь.</param>
        /// <returns>Независимая копия словаря.</returns>
        /// <exception cref="ArgumentNullException">Если source null.</exception>
        public DictionaryCreateResult Clone(Dictionary<string, string> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Словарь не может быть null для режима Clone");

            var result = source.ToDictionary(p => p.Key, p => p.Value);

            return new DictionaryCreateResult
            {
                Dictionary = result,
                Count = result.Count,
                DuplicatesSkipped = 0
            };
        }

        /// <summary>
        /// Инвертирует словарь: значения становятся ключами, ключи — значениями.
        /// </summary>
        /// <param name="source">Исходный словарь.</param>
        /// <param name="throwOnDuplicates">
        /// true — выбросить исключение при дублирующихся значениях.
        /// false — сохранить первое вхождение.
        /// </param>
        /// <returns>Инвертированный словарь.</returns>
        /// <exception cref="ArgumentNullException">Если source null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Если throwOnDuplicates=true и есть дублирующиеся значения.
        /// </exception>
        public DictionaryCreateResult Invert(Dictionary<string, string> source, bool throwOnDuplicates = false)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Словарь не может быть null для режима Invert");

            // Ищем значения которые встречаются более одного раза
            var duplicates = source.Values
                .GroupBy(v => v)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any() && throwOnDuplicates)
                throw new InvalidOperationException(
                    $"Инверсия невозможна — дублирующиеся значения: {string.Join(", ", duplicates)}. " +
                    "Отключите 'Ошибка при дублях' чтобы сохранить первое вхождение.");

            // GroupBy по значению, берём первый ключ при дублях
            var result = source
                .GroupBy(p => p.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Key
                );

            // Количество пропущенных = исходное кол-во - результирующее
            int skipped = source.Count - result.Count;

            return new DictionaryCreateResult
            {
                Dictionary = result,
                Count = result.Count,
                DuplicatesSkipped = skipped
            };
        }
    }
}
