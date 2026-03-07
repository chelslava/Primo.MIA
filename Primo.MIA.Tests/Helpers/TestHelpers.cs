using System;
using System.Collections.Generic;

namespace Primo.MIA.Tests.Helpers
{
    /// <summary>
    /// Вспомогательные методы для тестов
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Создает тестовый список строк
        /// </summary>
        public static List<string> CreateTestList(params string[] items)
        {
            return new List<string>(items);
        }

        /// <summary>
        /// Создает тестовый словарь
        /// </summary>
        public static Dictionary<string, string> CreateTestDictionary(params (string key, string value)[] pairs)
        {
            var dict = new Dictionary<string, string>();
            foreach (var (key, value) in pairs)
            {
                dict[key] = value;
            }
            return dict;
        }

        /// <summary>
        /// Проверяет что два списка содержат одинаковые элементы (порядок не важен)
        /// </summary>
        public static bool ListsAreEquivalent<T>(List<T> list1, List<T> list2)
        {
            if (list1 == null && list2 == null) return true;
            if (list1 == null || list2 == null) return false;
            if (list1.Count != list2.Count) return false;

            var set1 = new HashSet<T>(list1);
            var set2 = new HashSet<T>(list2);
            return set1.SetEquals(set2);
        }
    }
}
