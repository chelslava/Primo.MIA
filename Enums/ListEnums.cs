namespace Primo.MIA
{
    /// <summary>Тип конвертации списка в другую структуру данных</summary>
    public enum ListConvertMode
    {
        /// <summary>List&lt;"key=value"&gt; → Dictionary&lt;string,string&gt;</summary>
        [RuName("В словарь (key=value)")]
        ToDict,
        /// <summary>List&lt;string&gt; → Dictionary&lt;string(индекс),string(значение)&gt;</summary>
        [RuName("В словарь по индексам")]
        ToDictIndexed,
        /// <summary>List&lt;string&gt; → одна CSV-строка ("a","b","c")</summary>
        [RuName("В CSV-строку")]
        ToCSVRow,
        /// <summary>CSV-строка → List&lt;string&gt;</summary>
        [RuName("Из CSV-строки")]
        FromCSVRow,
        /// <summary>Zip двух List → Dictionary: listA[i] → listB[i]</summary>
        [RuName("Zip двух списков в словарь")]
        ZipToDict,
        /// <summary>Каждый элемент разбить по разделителю → плоский список</summary>
        [RuName("Развернуть в плоский список")]
        Flatten,
        /// <summary>Разбить список на подсписки по N элементов (батчи)</summary>
        [RuName("Разбить на части (chunk)")]
        Chunk
    }

    /// <summary>Режим фильтрации списка строк</summary>
    public enum ListFilterMode
    {
        /// <summary>Строка содержит указанную подстроку</summary>
        [RuName("Содержит")]
        Contains,
        /// <summary>Строка НЕ содержит указанную подстроку</summary>
        [RuName("Не содержит")]
        NotContains,
        /// <summary>Строка начинается с указанной подстроки</summary>
        [RuName("Начинается с")]
        StartsWith,
        /// <summary>Строка заканчивается указанной подстрокой</summary>
        [RuName("Заканчивается на")]
        EndsWith,
        /// <summary>Точное совпадение строки</summary>
        [RuName("Точное совпадение")]
        ExactMatch,
        /// <summary>Строка соответствует регулярному выражению</summary>
        [RuName("Соответствует regex")]
        Regex,
        /// <summary>Строка НЕ соответствует регулярному выражению</summary>
        [RuName("Не соответствует regex")]
        NotRegex,
        /// <summary>Непустые строки (не null, не пустая, не только пробелы)</summary>
        [RuName("Непустые")]
        NotEmpty,
        /// <summary>Только пустые строки (null, пустая или только пробелы)</summary>
        [RuName("Только пустые")]
        EmptyOnly,
        /// <summary>Длина строки в диапазоне [Prop_MinLength, Prop_MaxLength]</summary>
        [RuName("Длина в диапазоне")]
        LengthRange,
        /// <summary>Строка является числом (целым или дробным)</summary>
        [RuName("Только числа")]
        NumericOnly
    }

    /// <summary>Способ формирования ключа группировки</summary>
    public enum ListGroupMode
    {
        /// <summary>Группировка по первому символу строки</summary>
        [RuName("По первому символу")]
        ByFirstChar,
        /// <summary>Группировка по длине строки</summary>
        [RuName("По длине строки")]
        ByLength,
        /// <summary>Группировка по первым N символам (префикс)</summary>
        [RuName("По префиксу")]
        ByPrefix,
        /// <summary>Группировка по значению первой capture-группы regex</summary>
        [RuName("По regex-группе")]
        ByRegexGroup,
        /// <summary>Топ-N самых часто встречающихся значений с частотой</summary>
        [RuName("Топ частых значений")]
        TopFrequent
    }

    /// <summary>Операция над двумя множествами-списками</summary>
    public enum ListSetOperation
    {
        /// <summary>Объединение A∪B — все уникальные элементы обоих списков</summary>
        [RuName("Объединение (A∪B)")]
        Union,
        /// <summary>Пересечение A∩B — только элементы есть в обоих</summary>
        [RuName("Пересечение (A∩B)")]
        Intersect,
        /// <summary>Разность A∖B — элементы из A которых нет в B</summary>
        [RuName("Разность (A∖B)")]
        Except,
        /// <summary>Разность B∖A — элементы из B которых нет в A</summary>
        [RuName("Обратная разность (B∖A)")]
        ExceptReverse,
        /// <summary>Симметричная разность (A∪B)∖(A∩B) — элементы только в одном из списков</summary>
        [RuName("Симметричная разность")]
        SymmetricDiff,
        /// <summary>Уникальные элементы первого списка (без дублей внутри A)</summary>
        [RuName("Уникальные элементы")]
        Distinct
    }

    /// <summary>Тип агрегатной функции над списком строк</summary>
    public enum ListAggregateMode
    {
        /// <summary>Количество элементов (включая пустые)</summary>
        [RuName("Количество")]
        Count,
        /// <summary>Количество уникальных непустых элементов</summary>
        [RuName("Количество уникальных")]
        CountDistinct,
        /// <summary>Количество непустых элементов</summary>
        [RuName("Количество непустых")]
        CountNonEmpty,
        /// <summary>Сумма числовых значений (нечисловые игнорируются)</summary>
        [RuName("Сумма")]
        Sum,
        /// <summary>Минимальное числовое значение</summary>
        [RuName("Минимум")]
        Min,
        /// <summary>Максимальное числовое значение</summary>
        [RuName("Максимум")]
        Max,
        /// <summary>Среднее числовое значение</summary>
        [RuName("Среднее")]
        Average,
        /// <summary>Кратчайшая строка по длине</summary>
        [RuName("Кратчайшая строка")]
        ShortestString,
        /// <summary>Длиннейшая строка по длине</summary>
        [RuName("Длиннейшая строка")]
        LongestString,
        /// <summary>Конкатенация всех элементов через разделитель</summary>
        [RuName("Объединить в строку")]
        Join
    }

    /// <summary>Способ получения среза списка</summary>
    public enum ListSliceMode
    {
        /// <summary>Первые N элементов</summary>
        [RuName("Первые N")]
        FirstN,
        /// <summary>Последние N элементов</summary>
        [RuName("Последние N")]
        LastN,
        /// <summary>Пропустить первые N, вернуть остаток</summary>
        [RuName("Пропустить первые N")]
        SkipFirst,
        /// <summary>Пропустить последние N, вернуть начало</summary>
        [RuName("Пропустить последние N")]
        SkipLast,
        /// <summary>Страница: пропустить (Page-1)*PageSize, взять PageSize</summary>
        [RuName("Страница")]
        Page,
        /// <summary>Срез по индексам [FromIndex, ToIndex) — нумерация с 0</summary>
        [RuName("Диапазон индексов")]
        Range,
        /// <summary>Каждый N-й элемент (Step=2 → чётные индексы, Step=3 → каждый третий)</summary>
        [RuName("Каждый N-й")]
        EveryNth
    }

    /// <summary>
    /// Способ сортировки списка строк
    /// </summary>
    public enum ListSortMode
    {
        /// <summary>Алфавитная A→Z с учётом регистра (A до a)</summary>
        [RuName("По алфавиту (A→Z)")]
        Alphabetical,
        /// <summary>Алфавитная Z→A с учётом регистра</summary>
        [RuName("По алфавиту (Z→A)")]
        AlphabeticalDesc,
        /// <summary>Алфавитная без учёта регистра (смешанный регистр рядом)</summary>
        [RuName("Без учёта регистра (A→Z)")]
        CaseInsensitive,
        /// <summary>Алфавитная без учёта регистра, обратная</summary>
        [RuName("Без учёта регистра (Z→A)")]
        CaseInsensitiveDesc,
        /// <summary>По длине строки — сначала короткие</summary>
        [RuName("По длине (сначала короткие)")]
        ByLength,
        /// <summary>По длине строки — сначала длинные</summary>
        [RuName("По длине (сначала длинные)")]
        ByLengthDesc,
        /// <summary>Натуральная: file1, file2, file10 (не file1, file10, file2)</summary>
        [RuName("Натуральная")]
        Natural,
        /// <summary>Обратный порядок текущего списка без пересортировки</summary>
        [RuName("Обратный порядок")]
        Reverse,
        /// <summary>Случайное перемешивание (Fisher–Yates)</summary>
        [RuName("Случайный порядок")]
        Random
    }

    /// <summary>Способ преобразования каждого элемента списка</summary>
    public enum ListTransformMode
    {
        /// <summary>Весь текст в верхний регистр (HELLO)</summary>
        [RuName("Верхний регистр")]
        ToUpper,
        /// <summary>Весь текст в нижний регистр (hello)</summary>
        [RuName("Нижний регистр")]
        ToLower,
        /// <summary>Удалить пробелы с обоих краёв</summary>
        [RuName("Убрать пробелы по краям")]
        Trim,
        /// <summary>Удалить пробелы только слева</summary>
        [RuName("Убрать пробелы слева")]
        TrimStart,
        /// <summary>Удалить пробелы только справа</summary>
        [RuName("Убрать пробелы справа")]
        TrimEnd,
        /// <summary>Заменить подстроку Prop_Find на Prop_Replacement</summary>
        [RuName("Заменить подстроку")]
        Replace,
        /// <summary>Заменить совпадения Prop_Find (regex) на Prop_Replacement</summary>
        [RuName("Заменить по regex")]
        RegexReplace,
        /// <summary>Добавить Prop_Prefix перед каждым элементом</summary>
        [RuName("Добавить префикс")]
        Prefix,
        /// <summary>Добавить Prop_Suffix после каждого элемента</summary>
        [RuName("Добавить суффикс")]
        Suffix,
        /// <summary>Обернуть элемент: Prop_Prefix + элемент + Prop_Suffix</summary>
        [RuName("Обернуть (префикс + суффикс)")]
        Wrap,
        /// <summary>Дополнить элемент слева символом Prop_PadChar до Prop_PadWidth</summary>
        [RuName("Дополнить слева")]
        PadLeft,
        /// <summary>Дополнить элемент справа символом Prop_PadChar до Prop_PadWidth</summary>
        [RuName("Дополнить справа")]
        PadRight,
        /// <summary>Обрезать до Prop_MaxLength символов</summary>
        [RuName("Обрезать до длины")]
        Truncate,
        /// <summary>Удалить все цифры (0-9)</summary>
        [RuName("Удалить цифры")]
        RemoveNumbers,
        /// <summary>Оставить только буквы, цифры и пробелы</summary>
        [RuName("Только буквы и цифры")]
        RemoveNonAlpha
    }
}
