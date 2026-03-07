// =============================================================================
// TupleHelper.cs — активности «Кортеж: Создать» и «Кортеж: Деструктуризация».
//
// TupleCreateBack      — создаёт Tuple из 1–7 элементов произвольных типов.
// TupleDestructureBack — раскладывает все элементы кортежа в отдельные переменные.
//
// Совместимость: .NET Framework 4.7.2
// Тип кортежа:   System.Tuple<T1..T7> — иммутабельный, доступ через .Item1–.Item7.
//
// ВАЖНО: ValueTuple (C# 7 синтаксис (a, b)) НЕ используется — он требует NuGet
// пакет System.ValueTuple в .NET Framework. Используется классический Tuple.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Primo.MIA
{
    // =========================================================================
    // ВСПОМОГАТЕЛЬНЫЙ КЛАСС — единая работа с Tuple и ValueTuple через рефлексию
    // =========================================================================

    /// <summary>
    /// Вспомогательные методы для работы с System.Tuple и System.ValueTuple через рефлексию.
    /// Прозрачно определяет тип кортежа и использует нужный механизм доступа:
    ///   System.Tuple      → PropertyInfo (свойство, только чтение)
    ///   System.ValueTuple → FieldInfo    (публичное поле, чтение и запись)
    /// </summary>
    internal static class TupleHelper
    {
        // ── Определение типа ──────────────────────────────────────────────────

        /// <summary>Возвращает true если объект является System.ValueTuple&lt;...&gt;.</summary>
        public static bool IsValueTuple(object obj)
        {
            if (obj == null) return false;
            return obj.GetType().FullName.StartsWith("System.ValueTuple`");
        }

        /// <summary>Возвращает true если объект является System.Tuple&lt;...&gt;.</summary>
        public static bool IsClassicTuple(object obj)
        {
            if (obj == null) return false;
            return obj.GetType().FullName.StartsWith("System.Tuple`");
        }

        /// <summary>
        /// Возвращает количество элементов (арность) кортежа.
        /// Поддерживает System.Tuple и System.ValueTuple.
        /// Возвращает -1 если объект не является ни тем ни другим.
        /// </summary>
        public static int GetArity(object obj)
        {
            if (obj == null) return -1;
            Type t = obj.GetType();
            if (!t.FullName.StartsWith("System.Tuple`") &&
                !t.FullName.StartsWith("System.ValueTuple`"))
                return -1;

            // У обоих типов количество generic-аргументов = арность
            return t.GetGenericArguments().Length;
        }

        // ── Чтение элемента ───────────────────────────────────────────────────

        /// <summary>
        /// Извлекает значение элемента ItemN через рефлексию.
        /// Для System.Tuple — через PropertyInfo (свойство).
        /// Для System.ValueTuple — через FieldInfo (публичное поле).
        /// Возвращает null если элемент с таким номером не существует.
        /// </summary>
        public static object GetItem(object obj, int itemNumber)
        {
            if (obj == null) return null;
            string memberName = "Item" + itemNumber;
            Type t = obj.GetType();

            if (IsValueTuple(obj))
            {
                // ValueTuple: Item1..Item7 — публичные поля
                FieldInfo field = t.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
                return field?.GetValue(obj);
            }
            else
            {
                // Tuple: Item1..Item7 — свойства только для чтения
                PropertyInfo prop = t.GetProperty(memberName);
                return prop?.GetValue(obj, null);
            }
        }

        // ── Замена элемента (создание нового кортежа) ─────────────────────────

        /// <summary>
        /// Создаёт новый кортеж того же типа (Tuple или ValueTuple) с заменённым элементом.
        /// Оригинальный кортеж не изменяется.
        ///
        /// Для ValueTuple: несмотря на мутабельность struct, при передаче как object
        /// он упакован (boxed). В .NET Framework SetValue на boxed struct создаёт
        /// временную копию — оригинал остаётся неизменным. Поэтому для обоих типов
        /// мы создаём новый кортеж — это единственно корректное решение через рефлексию.
        /// </summary>
        public static object WithItem(object obj, int itemNumber, object newValue)
        {
            int arity = GetArity(obj);
            if (arity < 1 || itemNumber < 1 || itemNumber > arity)
                throw new InvalidOperationException(
                    $"Невозможно заменить Item{itemNumber} в кортеже арности {arity}.");

            // Читаем все текущие значения через рефлексию
            object[] values = new object[arity];
            for (int i = 0; i < arity; i++)
                values[i] = GetItem(obj, i + 1);

            // Заменяем нужный элемент
            values[itemNumber - 1] = newValue;

            // Создаём новый кортеж того же типа что и оригинал
            return CreateFromArray(values, arity, useValueTuple: IsValueTuple(obj));
        }

        // ── Создание кортежа ──────────────────────────────────────────────────

        /// <summary>
        /// Создаёт кортеж нужной арности из массива значений.
        /// useValueTuple = true → System.ValueTuple, false → System.Tuple.
        /// </summary>
        public static object CreateFromArray(object[] values, int arity, bool useValueTuple = false)
        {
            return useValueTuple
                ? CreateValueTuple(values, arity)
                : CreateClassicTuple(values, arity);
        }

        /// <summary>Создаёт System.Tuple нужной арности.</summary>
        private static object CreateClassicTuple(object[] v, int arity)
        {
            switch (arity)
            {
                case 1: return Tuple.Create(v[0]);
                case 2: return Tuple.Create(v[0], v[1]);
                case 3: return Tuple.Create(v[0], v[1], v[2]);
                case 4: return Tuple.Create(v[0], v[1], v[2], v[3]);
                case 5: return Tuple.Create(v[0], v[1], v[2], v[3], v[4]);
                case 6: return Tuple.Create(v[0], v[1], v[2], v[3], v[4], v[5]);
                case 7: return Tuple.Create(v[0], v[1], v[2], v[3], v[4], v[5], v[6]);
                default: throw new InvalidOperationException($"Арность {arity} не поддерживается (максимум 7).");
            }
        }

        /// <summary>
        /// Создаёт System.ValueTuple нужной арности.
        /// Требует NuGet: System.ValueTuple 4.5.0.
        /// </summary>
        private static object CreateValueTuple(object[] v, int arity)
        {
            switch (arity)
            {
                case 1: return ValueTuple.Create(v[0]);
                case 2: return ValueTuple.Create(v[0], v[1]);
                case 3: return ValueTuple.Create(v[0], v[1], v[2]);
                case 4: return ValueTuple.Create(v[0], v[1], v[2], v[3]);
                case 5: return ValueTuple.Create(v[0], v[1], v[2], v[3], v[4]);
                case 6: return ValueTuple.Create(v[0], v[1], v[2], v[3], v[4], v[5]);
                case 7: return ValueTuple.Create(v[0], v[1], v[2], v[3], v[4], v[5], v[6]);
                default: throw new InvalidOperationException($"Арность {arity} не поддерживается (максимум 7).");
            }
        }
    }

    // =========================================================================
    // ВСПОМОГАТЕЛЬНЫЙ КОМПАРАТОР — натуральная сортировка строк
    // =========================================================================

    /// <summary>
    /// Компаратор строк для натуральной сортировки.
    /// Числовые части сравниваются как числа: "file2" &lt; "file10".
    /// Используется в TupleSortBack для режима Natural.
    /// </summary>
    internal sealed class NaturalTupleComparer : IComparer<string>
    {
        public static readonly NaturalTupleComparer Instance = new NaturalTupleComparer();

        private static readonly System.Text.RegularExpressions.Regex _splitter =
            new System.Text.RegularExpressions.Regex(@"(\d+)", System.Text.RegularExpressions.RegexOptions.Compiled);

        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            string[] xParts = _splitter.Split(x);
            string[] yParts = _splitter.Split(y);

            for (int i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
            {
                string px = xParts[i];
                string py = yParts[i];

                int cmp;
                long nx, ny;
                // Если оба токена — числа, сравниваем числово
                if (long.TryParse(px, out nx) && long.TryParse(py, out ny))
                    cmp = nx.CompareTo(ny);
                else
                    cmp = string.Compare(px, py, StringComparison.OrdinalIgnoreCase);

                if (cmp != 0) return cmp;
            }

            return xParts.Length.CompareTo(yParts.Length);
        }
    }
}