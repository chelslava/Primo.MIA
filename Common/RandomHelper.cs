using System;
using System.Threading;

namespace Primo.MIA
{
    /// <summary>
    /// Потокобезопасный источник псевдослучайных чисел.
    /// <para>
    /// <see cref="Random"/> не потокобезопасен, а создание нового экземпляра на каждый
    /// вызов под .NET Framework при быстрых последовательных вызовах даёт совпадающие
    /// seed'ы (Environment.TickCount) и, как следствие, одинаковые «случайные» значения
    /// (issue #12). Здесь используется <see cref="ThreadLocal{Random}"/> с уникальным
    /// seed'ом на поток (счётчик ^ TickCount).
    /// </para>
    /// Для воспроизводимых последовательностей (детерминированные тесты, seed-режимы
    /// активностей) передавайте seed явно в <see cref="Random"/> — этот хелпер для того
    /// не предназначен. Для криптографической случайности используйте
    /// <see cref="System.Security.Cryptography.RandomNumberGenerator"/>.
    /// </summary>
    public static class RandomHelper
    {
        private static int _seedCounter = Environment.TickCount;

        private static readonly ThreadLocal<Random> Rng = new ThreadLocal<Random>(
            () => new Random(
                Interlocked.Increment(ref _seedCounter).GetHashCode()
                ^ Environment.TickCount.GetHashCode()),
            trackAllValues: false);

        /// <summary>Возвращает случайное неотрицательное целое, меньшее <paramref name="maxValue"/>.</summary>
        public static int Next(int maxValue) => Rng.Value.Next(maxValue);

        /// <summary>Возвращает случайное целое в диапазоне [<paramref name="minValue"/>, <paramref name="maxValue"/>)..</summary>
        public static int Next(int minValue, int maxValue) => Rng.Value.Next(minValue, maxValue);

        /// <summary>Возвращает случайное число с плавающей запятой в диапазоне [0.0; 1.0).</summary>
        public static double NextDouble() => Rng.Value.NextDouble();

        /// <summary>Заполняет элементы массива случайными байтами.</summary>
        public static void NextBytes(byte[] buffer) => Rng.Value.NextBytes(buffer);
    }
}
