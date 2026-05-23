namespace Primo.MIA
{
    /// <summary>Тип конвертации списка в другую структуру данных</summary>
    public enum ListConvertMode
    {
        /// <summary>List&lt;"key=value"&gt; → Dictionary&lt;string,string&gt;</summary>
        ToDict,
        /// <summary>List&lt;string&gt; → Dictionary&lt;string(индекс),string(значение)&gt;</summary>
        ToDictIndexed,
        /// <summary>List&lt;string&gt; → одна CSV-строка ("a","b","c")</summary>
        ToCSVRow,
        /// <summary>CSV-строка → List&lt;string&gt;</summary>
        FromCSVRow,
        /// <summary>Zip двух List → Dictionary: listA[i] → listB[i]</summary>
        ZipToDict,
        /// <summary>Каждый элемент разбить по разделителю → плоский список</summary>
        Flatten,
        /// <summary>Разбить список на подсписки по N элементов (батчи)</summary>
        Chunk
    }

    /// <summary>Режим фильтрации списка строк</summary>
    public enum ListFilterMode
    {
        /// <summary>Строка содержит указанную подстроку</summary>
        Contains,
        /// <summary>Строка НЕ содержит указанную подстроку</summary>
        NotContains,
        /// <summary>Строка начинается с указанной подстроки</summary>
        StartsWith,
        /// <summary>Строка заканчивается указанной подстрокой</summary>
        EndsWith,
        /// <summary>Точное совпадение строки</summary>
        ExactMatch,
        /// <summary>Строка соответствует регулярному выражению</summary>
        Regex,
        /// <summary>Строка НЕ соответствует регулярному выражению</summary>
        NotRegex,
        /// <summary>Непустые строки (не null, не пустая, не только пробелы)</summary>
        NotEmpty,
        /// <summary>Только пустые строки (null, пустая или только пробелы)</summary>
        EmptyOnly,
        /// <summary>Длина строки в диапазоне [Prop_MinLength, Prop_MaxLength]</summary>
        LengthRange,
        /// <summary>Строка является числом (целым или дробным)</summary>
        NumericOnly
    }

    /// <summary>Способ формирования ключа группировки</summary>
    public enum ListGroupMode
    {
        /// <summary>Группировка по первому символу строки</summary>
        ByFirstChar,
        /// <summary>Группировка по длине строки</summary>
        ByLength,
        /// <summary>Группировка по первым N символам (префикс)</summary>
        ByPrefix,
        /// <summary>Группировка по значению первой capture-группы regex</summary>
        ByRegexGroup,
        /// <summary>Топ-N самых часто встречающихся значений с частотой</summary>
        TopFrequent
    }

    /// <summary>Операция над двумя множествами-списками</summary>
    public enum ListSetOperation
    {
        /// <summary>Объединение A∪B — все уникальные элементы обоих списков</summary>
        Union,
        /// <summary>Пересечение A∩B — только элементы есть в обоих</summary>
        Intersect,
        /// <summary>Разность A∖B — элементы из A которых нет в B</summary>
        Except,
        /// <summary>Разность B∖A — элементы из B которых нет в A</summary>
        ExceptReverse,
        /// <summary>Симметричная разность (A∪B)∖(A∩B) — элементы только в одном из списков</summary>
        SymmetricDiff,
        /// <summary>Уникальные элементы первого списка (без дублей внутри A)</summary>
        Distinct
    }

    /// <summary>Тип агрегатной функции над списком строк</summary>
    public enum ListAggregateMode
    {
        /// <summary>Количество элементов (включая пустые)</summary>
        Count,
        /// <summary>Количество уникальных непустых элементов</summary>
        CountDistinct,
        /// <summary>Количество непустых элементов</summary>
        CountNonEmpty,
        /// <summary>Сумма числовых значений (нечисловые игнорируются)</summary>
        Sum,
        /// <summary>Минимальное числовое значение</summary>
        Min,
        /// <summary>Максимальное числовое значение</summary>
        Max,
        /// <summary>Среднее числовое значение</summary>
        Average,
        /// <summary>Кратчайшая строка по длине</summary>
        ShortestString,
        /// <summary>Длиннейшая строка по длине</summary>
        LongestString,
        /// <summary>Конкатенация всех элементов через разделитель</summary>
        Join
    }

    /// <summary>Способ получения среза списка</summary>
    public enum ListSliceMode
    {
        /// <summary>Первые N элементов</summary>
        FirstN,
        /// <summary>Последние N элементов</summary>
        LastN,
        /// <summary>Пропустить первые N, вернуть остаток</summary>
        SkipFirst,
        /// <summary>Пропустить последние N, вернуть начало</summary>
        SkipLast,
        /// <summary>Страница: пропустить (Page-1)*PageSize, взять PageSize</summary>
        Page,
        /// <summary>Срез по индексам [FromIndex, ToIndex) — нумерация с 0</summary>
        Range,
        /// <summary>Каждый N-й элемент (Step=2 → чётные индексы, Step=3 → каждый третий)</summary>
        EveryNth
    }

    /// <summary>
    /// Способ сортировки списка строк
    /// </summary>
    public enum ListSortMode
    {
        /// <summary>Алфавитная A→Z с учётом регистра (A до a)</summary>
        Alphabetical,
        /// <summary>Алфавитная Z→A с учётом регистра</summary>
        AlphabeticalDesc,
        /// <summary>Алфавитная без учёта регистра (смешанный регистр рядом)</summary>
        CaseInsensitive,
        /// <summary>Алфавитная без учёта регистра, обратная</summary>
        CaseInsensitiveDesc,
        /// <summary>По длине строки — сначала короткие</summary>
        ByLength,
        /// <summary>По длине строки — сначала длинные</summary>
        ByLengthDesc,
        /// <summary>Натуральная: file1, file2, file10 (не file1, file10, file2)</summary>
        Natural,
        /// <summary>Обратный порядок текущего списка без пересортировки</summary>
        Reverse,
        /// <summary>Случайное перемешивание (Fisher–Yates)</summary>
        Random
    }

    /// <summary>Способ преобразования каждого элемента списка</summary>
    public enum ListTransformMode
    {
        /// <summary>Весь текст в верхний регистр (HELLO)</summary>
        ToUpper,
        /// <summary>Весь текст в нижний регистр (hello)</summary>
        ToLower,
        /// <summary>Удалить пробелы с обоих краёв</summary>
        Trim,
        /// <summary>Удалить пробелы только слева</summary>
        TrimStart,
        /// <summary>Удалить пробелы только справа</summary>
        TrimEnd,
        /// <summary>Заменить подстроку Prop_Find на Prop_Replacement</summary>
        Replace,
        /// <summary>Заменить совпадения Prop_Find (regex) на Prop_Replacement</summary>
        RegexReplace,
        /// <summary>Добавить Prop_Prefix перед каждым элементом</summary>
        Prefix,
        /// <summary>Добавить Prop_Suffix после каждого элемента</summary>
        Suffix,
        /// <summary>Обернуть элемент: Prop_Prefix + элемент + Prop_Suffix</summary>
        Wrap,
        /// <summary>Дополнить элемент слева символом Prop_PadChar до Prop_PadWidth</summary>
        PadLeft,
        /// <summary>Дополнить элемент справа символом Prop_PadChar до Prop_PadWidth</summary>
        PadRight,
        /// <summary>Обрезать до Prop_MaxLength символов</summary>
        Truncate,
        /// <summary>Удалить все цифры (0-9)</summary>
        RemoveNumbers,
        /// <summary>Оставить только буквы, цифры и пробелы</summary>
        RemoveNonAlpha
    }
}
