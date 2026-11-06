namespace Primo.MIA
{
    /// <summary>
    /// Направление сортировки списка кортежей.
    /// </summary>
    public enum TupleSortDirection
    {
        /// <summary>По возрастанию (A→Z, 1→9)</summary>
        [RuName("По возрастанию")]
        Ascending,
        /// <summary>По убыванию (Z→A, 9→1)</summary>
        [RuName("По убыванию")]
        Descending
    }

    /// <summary>
    /// Режим сортировки значений — как строки или как числа.
    /// </summary>
    public enum TupleSortType
    {
        /// <summary>Алфавитная сортировка без учёта регистра. Используется по умолчанию.</summary>
        [RuName("По алфавиту")]
        Alphabetical,
        /// <summary>Числовая сортировка. Нечисловые элементы уходят в конец.</summary>
        [RuName("Как числа")]
        Numeric,
        /// <summary>Натуральная сортировка: file2 < file10 (числовые части как числа).</summary>
        [RuName("Натуральная")]
        Natural
    }

    /// <summary>
    /// Номер элемента кортежа — Item1 … Item7.
    /// Используется в TupleGetBack, TupleSetBack, TupleSortBack.
    /// </summary>
    public enum TupleItemIndex
    {
        /// <summary>Первый элемент кортежа — .Item1</summary>
        [RuName("Элемент 1 (Item1)")]
        Item1 = 1,
        /// <summary>Второй элемент кортежа — .Item2</summary>
        [RuName("Элемент 2 (Item2)")]
        Item2 = 2,
        /// <summary>Третий элемент кортежа — .Item3</summary>
        [RuName("Элемент 3 (Item3)")]
        Item3 = 3,
        /// <summary>Четвёртый элемент кортежа — .Item4</summary>
        [RuName("Элемент 4 (Item4)")]
        Item4 = 4,
        /// <summary>Пятый элемент кортежа — .Item5</summary>
        [RuName("Элемент 5 (Item5)")]
        Item5 = 5,
        /// <summary>Шестой элемент кортежа — .Item6</summary>
        [RuName("Элемент 6 (Item6)")]
        Item6 = 6,
        /// <summary>Седьмой элемент кортежа — .Item7</summary>
        [RuName("Элемент 7 (Item7)")]
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
        [RuName("Tuple (классический)")]
        ClassicTuple,

        /// <summary>
        /// System.ValueTuple&lt;T1..T7&gt; — значимый тип (struct).
        /// Меньше нагружает GC при обработке больших коллекций кортежей.
        /// Требует NuGet: System.ValueTuple 4.5.0.
        /// Через рефлексию ведёт себя аналогично ClassicTuple — мутабельность
        /// ValueTuple доступна только при прямом доступе в C#-коде.
        /// </summary>
        [RuName("ValueTuple (структура)")]
        ValueTuple
    }

    /// <summary>
    /// Режим конвертации кортежа в другой тип данных или обратно.
    /// </summary>
    public enum TupleConvertMode
    {
        /// <summary>Tuple → List&lt;object&gt;: каждый элемент становится элементом списка</summary>
        [RuName("В список")]
        ToList,

        /// <summary>List&lt;object&gt; → Tuple: первые 1–7 элементов списка становятся кортежем</summary>
        [RuName("Из списка")]
        FromList,

        /// <summary>
        /// Tuple → Dictionary&lt;string,object&gt;: ключи "Item1","Item2"… — значения элементов.
        /// Удобно для логирования и сериализации.
        /// </summary>
        [RuName("В словарь")]
        ToDictionary,

        /// <summary>
        /// Tuple → string: "Item1=значение1; Item2=значение2; …"
        /// Разделитель настраивается через Prop_Separator.
        /// </summary>
        [RuName("В строку")]
        ToString
    }
}
