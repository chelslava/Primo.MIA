// =============================================================================
// NaturalComparer.cs — компаратор для натуральной сортировки строк.
//
// Числовые части строк сравниваются как числа, а не как строки:
//   "file2" < "file10" (не "file10" < "file2")
//
// Используется в активностях сортировки списков и кортежей.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Компаратор для натуральной (естественной) сортировки строк.
    /// Числовые части сравниваются как числа: "file2" &lt; "file10".
    /// Используется в List/ListSort и Tuple/TupleSort активностях.
    /// </summary>
    public sealed class NaturalComparer : IComparer<string>
    {
        /// <summary>
        /// Синглтон-экземпляр компаратора.
        /// Используйте NaturalComparer.Instance для доступа.
        /// </summary>
        public static readonly NaturalComparer Instance = new NaturalComparer();

        /// <summary>
        /// Regex для разбиения строки на текстовые и числовые токены.
        /// Захватывает последовательности цифр как отдельные группы.
        /// </summary>
        private static readonly Regex _tokenizer = new Regex(@"(\d+)", RegexOptions.Compiled);

        /// <summary>
        /// Приватный конструктор для паттерна Singleton.
        /// </summary>
        private NaturalComparer() { }

        /// <summary>
        /// Сравнивает две строки с использованием натуральной сортировки.
        /// </summary>
        /// <param name="x">Первая строка для сравнения</param>
        /// <param name="y">Вторая строка для сравнения</param>
        /// <returns>
        /// Отрицательное число если x &lt; y,
        /// ноль если x == y,
        /// положительное число если x &gt; y
        /// </returns>
        public int Compare(string x, string y)
        {
            // Обработка null и равенства
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            // Разбиваем строки на токены: текст и числовые части чередуются
            // Пример: "file10name" → ["file", "10", "name"]
            string[] xTokens = _tokenizer.Split(x);
            string[] yTokens = _tokenizer.Split(y);

            // Сравниваем токены попарно
            int minLength = Math.Min(xTokens.Length, yTokens.Length);

            for (int i = 0; i < minLength; i++)
            {
                string xToken = xTokens[i];
                string yToken = yTokens[i];

                int comparison;

                // Если оба токена — числа, сравниваем их как long
                if (long.TryParse(xToken, out long xNumber) && long.TryParse(yToken, out long yNumber))
                {
                    comparison = xNumber.CompareTo(yNumber);
                }
                else
                {
                    // Иначе сравниваем как строки без учёта регистра
                    comparison = string.Compare(xToken, yToken, StringComparison.OrdinalIgnoreCase);
                }

                // Если токены различаются — возвращаем результат
                if (comparison != 0)
                    return comparison;
            }

            // Если все общие токены равны — более короткая строка идёт первой
            return xTokens.Length.CompareTo(yTokens.Length);
        }
    }
}
