namespace Primo.MIA
{
    /// <summary>
    /// Направление сортировки списка кортежей.
    /// </summary>
    public enum TupleSortDirection
    {
        /// <summary>По возрастанию (A→Z, 1→9)</summary>
        Ascending,
        /// <summary>По убыванию (Z→A, 9→1)</summary>
        Descending
    }

    /// <summary>
    /// Режим сортировки значений — как строки или как числа.
    /// </summary>
    public enum TupleSortType
    {
        /// <summary>Алфавитная сортировка без учёта регистра. Используется по умолчанию.</summary>
        Alphabetical,
        /// <summary>Числовая сортировка. Нечисловые элементы уходят в конец.</summary>
        Numeric,
        /// <summary>Натуральная сортировка: file2 < file10 (числовые части как числа).</summary>
        Natural
    }

    /// <summary>
    /// Номер элемента кортежа — Item1 … Item7.
    /// Используется в TupleGetBack, TupleSetBack, TupleSortBack.
    /// </summary>
    public enum TupleItemIndex
    {
        /// <summary>Первый элемент кортежа — .Item1</summary>
        Item1 = 1,
        /// <summary>Второй элемент кортежа — .Item2</summary>
        Item2 = 2,
        /// <summary>Третий элемент кортежа — .Item3</summary>
        Item3 = 3,
        /// <summary>Четвёртый элемент кортежа — .Item4</summary>
        Item4 = 4,
        /// <summary>Пятый элемент кортежа — .Item5</summary>
        Item5 = 5,
        /// <summary>Шестой элемент кортежа — .Item6</summary>
        Item6 = 6,
        /// <summary>Седьмой элемент кортежа — .Item7</summary>
        Item7 = 7
    }

    /// <summary>
    /// Тип создаваемого или ожидаемого кортежа.
    /// </summary>
    public enum TupleKind
    {
        /// <summary>
        /// System.Tuple&lt;T1..T7&gt; — ссылочный тип, иммутабелен.
        /// Не требует дополнительных NuGet-пакетов.
        /// </summary>
        ClassicTuple,

        /// <summary>
        /// System.ValueTuple&lt;T1..T7&gt; — значимый тип (struct).
        /// Меньше нагружает GC при обработке больших коллекций кортежей.
        /// Требует NuGet: System.ValueTuple 4.5.0.
        /// Через рефлексию ведёт себя аналогично ClassicTuple — мутабельность
        /// ValueTuple доступна только при прямом доступе в C#-коде.
        /// </summary>
        ValueTuple
    }

    /// <summary>
    /// Режим конвертации кортежа в другой тип данных или обратно.
    /// </summary>
    public enum TupleConvertMode
    {
        /// <summary>Tuple → List&lt;object&gt;: каждый элемент становится элементом списка</summary>
        ToList,

        /// <summary>List&lt;object&gt; → Tuple: первые 1–7 элементов списка становятся кортежем</summary>
        FromList,

        /// <summary>
        /// Tuple → Dictionary&lt;string,object&gt;: ключи "Item1","Item2"… — значения элементов.
        /// Удобно для логирования и сериализации.
        /// </summary>
        ToDictionary,

        /// <summary>
        /// Tuple → string: "Item1=значение1; Item2=значение2; …"
        /// Разделитель настраивается через Prop_Separator.
        /// </summary>
        ToString
    }
}
