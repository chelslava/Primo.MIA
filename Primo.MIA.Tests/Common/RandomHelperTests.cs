using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests
{
    /// <summary>
    /// Тесты потокобезопасного генератора случайных чисел (issue #12).
    /// </summary>
    public class RandomHelperTests
    {
        [Fact]
        [Trait("Category", "Fast")]
        public void Next_InTightLoop_ReturnsDistinctValues()
        {
            // Быстрые последовательные вызовы — раньше new Random() на каждый
            // вызов давал совпадающие seed'ы и дубли значений.
            var values = Enumerable.Range(0, 1000)
                .Select(_ => RandomHelper.Next(0, int.MaxValue))
                .ToList();

            values.Distinct().Count().Should().BeGreaterThan(900,
                "совпадающие seed'ы в цикле недопустимы");
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void Next_ParallelThreads_NoExceptions_AllInRange()
        {
            // Потокобезопасность: Random не потокобезопасен, ThreadLocal — да.
            const int threads = 8;
            const int perThread = 500;

            var results = new int[threads][];
            Parallel.For(0, threads, i =>
            {
                results[i] = Enumerable.Range(0, perThread)
                    .Select(_ => RandomHelper.Next(0, 100))
                    .ToArray();
            });

            results.Should().OnlyContain(arr => arr.Length == perThread);
            results.SelectMany(v => v).Should().OnlyContain(v => v >= 0 && v < 100);
            results.SelectMany(v => v).Distinct().Count().Should().BeGreaterThan(10);
        }

        [Fact]
        [Trait("Category", "Fast")]
        public void Next_MinMaxRange_Respected()
        {
            Enumerable.Range(0, 200).Select(_ => RandomHelper.Next(5, 10))
                .Should().OnlyContain(v => v >= 5 && v <= 9);
        }
    }
}
