namespace Primo.MIA
{
    // =========================================================================
    // ПЕРЕЧИСЛЕНИЯ
    // =========================================================================

    /// <summary>
    /// Режим чтения TOML-конфигурации
    /// </summary>
    public enum TomlReadMode
    {
        /// <summary>
        /// Прочитать одно значение по ключу.
        /// Поддерживает вложенность через точку: "section.subsection.key"
        /// </summary>
        SingleValue,

        /// <summary>
        /// Прочитать все ключи указанной секции в Dictionary&lt;string, string&gt;.
        /// Поддерживает вложенные секции: "app.database"
        /// </summary>
        SectionToDictionary,

        /// <summary>
        /// Прочитать весь файл в плоский словарь.
        /// Ключи вложенных секций объединяются через точку: "server.host"
        /// </summary>
        FullFileToDictionary,

        /// <summary>
        /// Чтение конфига с профилями окружений.
        /// Мёржит секцию [default] с секцией выбранного профиля ([production], [staging] и т.д.)
        /// по выбранной стратегии слияния.
        /// </summary>
        ReadProfile
    }

    /// <summary>
    /// Стратегия слияния профиля с секцией default
    /// </summary>
    public enum ProfileMergeStrategy
    {
        /// <summary>
        /// Стандартная стратегия: default как база, профиль перекрывает его ключи.
        /// Ключи из default, которых нет в профиле, сохраняются.
        /// Пример: default.timeout=30, production.timeout=60 → итог: timeout=60
        ///         default.retry=3 (нет в production) → итог: retry=3
        /// </summary>
        DefaultThenProfile,

        /// <summary>
        /// Только секция профиля, без слияния с default.
        /// Используется когда профили полностью самодостаточны.
        /// </summary>
        ProfileOnly,

        /// <summary>
        /// Обратный порядок: профиль как база, default добавляет только недостающие ключи.
        /// Ключи профиля никогда не перекрываются default.
        /// </summary>
        ProfileThenDefault
    }

    /// <summary>
    /// Стратегия при слиянии двух словарей с конфликтующими ключами
    /// </summary>
    public enum DictionaryMergeStrategy
    {
        /// <summary>
        /// Ключи первого словаря имеют приоритет.
        /// Ключи второго словаря не перезаписывают уже существующие.
        /// </summary>
        KeepFirst,

        /// <summary>
        /// Ключи второго словаря имеют приоритет.
        /// Перекрывают значения первого при совпадении ключей.
        /// </summary>
        KeepSecond,

        /// <summary>
        /// При конфликте ключей — выбросить исключение со списком дублей.
        /// Используется когда дублирование ключей является ошибкой процесса.
        /// </summary>
        ThrowOnDuplicate
    }

    /// <summary>
    /// Режим создания нового словаря
    /// </summary>
    public enum DictionaryCreateMode
    {
        /// <summary>
        /// Создать новый пустой Dictionary&lt;string, string&gt;.
        /// Не требует входного словаря.
        /// </summary>
        CreateEmpty,

        /// <summary>
        /// Создать словарь из двух List&lt;string&gt; одинаковой длины.
        /// Prop_KeysList[i] → ключ, Prop_ValuesList[i] → значение.
        /// При дублирующихся ключах побеждает последнее значение.
        /// </summary>
        FromLists,

        /// <summary>
        /// Создать полную независимую копию словаря.
        /// Изменения в копии не затронут оригинал.
        /// </summary>
        Clone,

        /// <summary>
        /// Инвертировать словарь: значения становятся ключами, ключи — значениями.
        /// При дублирующихся значениях (будущих ключах):
        ///   Prop_ThrowOnDuplicates = true  → исключение
        ///   Prop_ThrowOnDuplicates = false → первое вхождение сохраняется
        /// </summary>
        Invert
    }

    /// <summary>
    /// Режим работы активности
    /// </summary>
    public enum WaitFileMode
    {
        /// <summary>
        /// Ожидать появления нового файла (которого не было при запуске)
        /// </summary>
        WaitForNewFile,

        /// <summary>
        /// Ожидать файл независимо от того, был он или нет
        /// </summary>
        WaitForAnyFile
    }

    /// <summary>
    /// Тип фильтрации файлов
    /// </summary>
    public enum FileFilterType
    {
        /// <summary>
        /// Точное совпадение имени файла
        /// </summary>
        Exact,

        /// <summary>
        /// Wildcard фильтрация (*, ?)
        /// </summary>
        Wildcard,

        /// <summary>
        /// Regex фильтрация
        /// </summary>
        Regex
    }

    /// <summary>
    /// Тип поиска файлов
    /// </summary>
    public enum SearchType
    {
        /// <summary>
        /// Поиск только файлов
        /// </summary>
        FilesOnly,

        /// <summary>
        /// Поиск только папок
        /// </summary>
        FoldersOnly,

        /// <summary>
        /// Поиск файлов и папок
        /// </summary>
        FilesAndFolders
    }

    /// <summary>
    /// Тип фильтрации при поиске
    /// </summary>
    public enum SearchFilterType
    {
        /// <summary>
        /// Wildcard фильтрация (*, ?)
        /// </summary>
        Wildcard,

        /// <summary>
        /// Regex фильтрация
        /// </summary>
        Regex
    }

    /// <summary>
    /// Уровень логирования
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Отладочная информация
        /// </summary>
        Debug,

        /// <summary>
        /// Информационное сообщение
        /// </summary>
        Info,

        /// <summary>
        /// Предупреждение
        /// </summary>
        Warning,

        /// <summary>
        /// Ошибка
        /// </summary>
        Error,

        /// <summary>
        /// Критическая ошибка
        /// </summary>
        Critical
    }
    /// <summary>
    /// Режим вывода логов
    /// </summary>
    public enum LogOutputMode
    {
        /// <summary>
        /// Только в файл
        /// </summary>
        FileOnly,

        /// <summary>
        /// Только в консоль
        /// </summary>
        ConsoleOnly,

        /// <summary>
        /// В файл и консоль одновременно
        /// </summary>
        FileAndConsole
    }

    /// <summary>
    /// Стратегия ротации файлов логов
    /// </summary>
    public enum LogRotationStrategy
    {
        /// <summary>
        /// Без ротации - весь лог в один файл
        /// </summary>
        None,

        /// <summary>
        /// Новый файл каждый день
        /// </summary>
        Daily,

        /// <summary>
        /// Новый файл каждый час
        /// </summary>
        Hourly,

        /// <summary>
        /// Новый файл при превышении размера
        /// </summary>
        BySize
    }

    /// <summary>
    /// Шаблон имени файла лога
    /// </summary>
    public enum LogFileNameTemplate
    {
        /// <summary>
        /// Фиксированное имя (app.log)
        /// </summary>
        Fixed,

        /// <summary>
        /// С датой (app_2025-02-15.log)
        /// </summary>
        WithDate,

        /// <summary>
        /// С датой и временем (app_2025-02-15_14-30.log)
        /// </summary>
        WithDateTime,

        /// <summary>
        /// С timestamp (app_20250215143045.log)
        /// </summary>
        WithTimestamp,

        /// <summary>
        /// Пользовательский формат
        /// </summary>
        Custom
    }

    /// <summary>
    /// Где искать: в ключах, значениях или и там и там.
    /// </summary>
    public enum DictionaryFilterTarget
    {
        /// <summary>Применять условие к ключам словаря</summary>
        Keys,

        /// <summary>Применять условие к значениям словаря</summary>
        Values,

        /// <summary>Оставить пару если условие выполняется хотя бы для ключа ИЛИ значения</summary>
        KeysAndValues
    }

    /// <summary>
    /// Как искать: подстрока, точное совпадение, regex или wildcard.
    /// </summary>
    public enum DictionaryFilterMethod
    {
        /// <summary>Содержит подстроку (аналог String.Contains)</summary>
        Contains,

        /// <summary>Точное совпадение всей строки</summary>
        Exact,

        /// <summary>Регулярное выражение</summary>
        Regex,

        /// <summary>Wildcard-паттерн (* — любое кол-во символов, ? — один символ)</summary>
        Wildcard
    }

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

    /// <summary>
    /// Тип генерируемого значения.
    /// Определяет какой набор параметров будет использоваться.
    /// </summary>
    public enum GeneratorType
    {
        /// <summary>Генерация GUID (UUID) — глобально уникального идентификатора</summary>
        Guid,
        /// <summary>Генерация уникального имени файла с учётом уже существующих файлов</summary>
        FileName,
        /// <summary>Генерация случайного целого числа в заданном диапазоне</summary>
        RandomNumber,
        /// <summary>Форматирование текущей даты/времени в строку</summary>
        Timestamp,
        /// <summary>Последовательный счётчик с настраиваемым начальным значением и шагом</summary>
        Counter,
        /// <summary>Детерминированный хеш-идентификатор на основе SHA-256</summary>
        HashId,
        /// <summary>Формирование email-адреса из имени, фамилии и домена</summary>
        Username,
        /// <summary>Заполнение строки-шаблона значениями из словаря по ключам {key}</summary>
        Template
    }

    /// <summary>
    /// Формат строкового представления GUID.
    /// Все форматы генерируют один и тот же GUID, отличается только его запись.
    /// </summary>
    public enum GuidFormat
    {
        /// <summary>
        /// 32 шестнадцатеричных цифры без разделителей.
        /// Пример: 00000000000000000000000000000000
        /// </summary>
        N,
        /// <summary>
        /// 32 цифры, разделённые дефисами (стандарт RFC 4122).
        /// Пример: 00000000-0000-0000-0000-000000000000
        /// </summary>
        D,
        /// <summary>
        /// 32 цифры с дефисами в фигурных скобках.
        /// Пример: {00000000-0000-0000-0000-000000000000}
        /// </summary>
        B,
        /// <summary>
        /// 32 цифры с дефисами в круглых скобках.
        /// Пример: (00000000-0000-0000-0000-000000000000)
        /// </summary>
        P,
        /// <summary>
        /// Четыре шестнадцатеричных значения в фигурных скобках.
        /// Пример: {0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}
        /// </summary>
        X
    }

    /// <summary>
    /// Предустановленные форматы даты/времени для режима Timestamp.
    /// При выборе Custom используется поле Prop_CustomTimestampFormat.
    /// </summary>
    public enum TimestampFormat
    {
        // ── Компактные форматы (для имён файлов и ID) ──────────────────────────

        /// <summary>20250215_143045 — дата и время слитно, безопасно для имён файлов</summary>
        yyyyMMdd_HHmmss,

        /// <summary>20250215_1430 — дата и время до минут, для ежеминутных файлов</summary>
        yyyyMMdd_HHmm,

        /// <summary>20250215 — только дата слитно (для ежедневных файлов)</summary>
        yyyyMMdd,

        /// <summary>202502 — год и месяц (для ежемесячных файлов)</summary>
        yyyyMM,

        /// <summary>20250215143045123 — с миллисекундами, максимальная уникальность</summary>
        yyyyMMddHHmmssfff,

        // ── Форматы ISO 8601 (для баз данных и API) ────────────────────────────

        /// <summary>2025-02-15 — дата ISO (стандарт для БД и API)</summary>
        yyyy_MM_dd,

        /// <summary>2025-02-15 14:30:45 — дата и время ISO без временной зоны</summary>
        yyyy_MM_dd_HH_mm_ss,

        /// <summary>2025-02-15T14:30:45 — дата и время ISO с разделителем T</summary>
        ISO8601,

        /// <summary>2025-02-15T14:30:45.123 — ISO с миллисекундами</summary>
        ISO8601_ms,

        // ── Русские и европейские форматы (для отчётов и документов) ──────────

        /// <summary>15.02.2025 — российский формат ДД.ММ.ГГГГ</summary>
        dd_MM_yyyy,

        /// <summary>15.02.2025 14:30:45 — российский формат с временем</summary>
        dd_MM_yyyy_HH_mm_ss,

        /// <summary>15.02.2025 14:30 — российский формат до минут</summary>
        dd_MM_yyyy_HH_mm,

        /// <summary>15/02/2025 — европейский формат ДД/ММ/ГГГГ</summary>
        dd_MM_yyyy_slash,

        // ── Американский формат ────────────────────────────────────────────────

        /// <summary>02/15/2025 — американский формат ММ/ДД/ГГГГ</summary>
        MM_dd_yyyy,

        /// <summary>02/15/2025 14:30:45 — американский формат с временем</summary>
        MM_dd_yyyy_HH_mm_ss,

        // ── Только время ───────────────────────────────────────────────────────

        /// <summary>14:30:45 — только время ЧЧ:ММ:СС</summary>
        HHmmss,

        /// <summary>14:30 — только время ЧЧ:ММ</summary>
        HHmm,

        /// <summary>14:30:45.123 — время с миллисекундами</summary>
        HHmmss_fff,

        // ── Специальные форматы ────────────────────────────────────────────────

        /// <summary>Суббота, 15 февраля 2025 г. — полная дата с днём недели (зависит от культуры)</summary>
        FullDate,

        /// <summary>Q1-2025, Q2-2025... — квартал и год (вычисляется вручную)</summary>
        Quarter,

        /// <summary>W07-2025 — номер недели ISO и год</summary>
        WeekNumber,

        /// <summary>Unix Timestamp — секунды с 01.01.1970 UTC</summary>
        UnixTimestamp,

        /// <summary>Пользовательский формат из поля Prop_CustomTimestampFormat</summary>
        Custom
    }

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

    /// <summary>
    /// HTTP-методы для выполнения запросов.
    /// </summary>
    public enum HttpMethodType
    {
        /// <summary>GET — получение данных</summary>
        GET,
        /// <summary>POST — отправка данных</summary>
        POST,
        /// <summary>PUT — обновление ресурса</summary>
        PUT,
        /// <summary>DELETE — удаление ресурса</summary>
        DELETE,
        /// <summary>PATCH — частичное обновление</summary>
        PATCH
    }

    /// <summary>
    /// Тип браузера для автоматизации
    /// </summary>
    public enum BrowserType
    {
        /// <summary>Google Chrome</summary>
        Chrome,
        /// <summary>Mozilla Firefox</summary>
        Firefox,
        /// <summary>Microsoft Edge</summary>
        Edge
    }

    /// <summary>
    /// Тип локатора для поиска элементов на странице
    /// </summary>
    public enum ElementLocatorType
    {
        /// <summary>Поиск по атрибуту id</summary>
        Id,
        /// <summary>Поиск по атрибуту name</summary>
        Name,
        /// <summary>Поиск по имени CSS-класса</summary>
        ClassName,
        /// <summary>Поиск по имени тега</summary>
        TagName,
        /// <summary>Поиск по полному тексту ссылки</summary>
        LinkText,
        /// <summary>Поиск по частичному тексту ссылки</summary>
        PartialLinkText,
        /// <summary>Поиск по CSS-селектору</summary>
        CssSelector,
        /// <summary>Поиск по XPath-выражению</summary>
        XPath
    }

    /// <summary>
    /// Тип условия ожидания
    /// </summary>
    public enum WaitConditionType
    {
        /// <summary>Элемент существует в DOM</summary>
        ElementExists,
        /// <summary>Элемент видим на странице</summary>
        ElementVisible,
        /// <summary>Элемент доступен для клика</summary>
        ElementClickable,
        /// <summary>Элемент невидим или отсутствует</summary>
        ElementInvisible,
        /// <summary>Текст присутствует в элементе</summary>
        TextPresent,
        /// <summary>Заголовок страницы содержит текст</summary>
        TitleContains,
        /// <summary>URL содержит текст</summary>
        UrlContains,
        /// <summary>Алерт присутствует</summary>
        AlertPresent
    }

    /// <summary>
    /// Режим выбора опции в select элементе
    /// </summary>
    public enum SelectMode
    {
        /// <summary>Выбор по видимому тексту</summary>
        ByText,
        /// <summary>Выбор по значению атрибута value</summary>
        ByValue,
        /// <summary>Выбор по индексу (начиная с 0)</summary>
        ByIndex
    }

    /// <summary>
    /// Выравнивание при прокрутке к элементу
    /// </summary>
    public enum ScrollAlignment
    {
        /// <summary>Прокрутить элемент к верхней части окна</summary>
        Top,
        /// <summary>Прокрутить элемент к центру окна</summary>
        Center,
        /// <summary>Прокрутить элемент к нижней части окна</summary>
        Bottom
    }

    /// <summary>
    /// Операция с cookies
    /// </summary>
    public enum CookieOperation
    {
        /// <summary>Получить cookie по имени</summary>
        Get,
        /// <summary>Получить все cookies</summary>
        GetAll,
        /// <summary>Установить cookie</summary>
        Set,
        /// <summary>Удалить cookie по имени</summary>
        Delete,
        /// <summary>Удалить все cookies</summary>
        DeleteAll
    }

    /// <summary>
    /// Тип переключения контекста браузера
    /// </summary>
    public enum SwitchToType
    {
        /// <summary>Переключиться на iframe по индексу или имени</summary>
        Frame,
        /// <summary>Переключиться на окно по handle</summary>
        Window,
        /// <summary>Переключиться на алерт</summary>
        Alert,
        /// <summary>Вернуться к основному контенту страницы</summary>
        DefaultContent,
        /// <summary>Вернуться к родительскому фрейму</summary>
        ParentFrame
    }

    /// <summary>
    /// Действие с алертом
    /// </summary>
    public enum AlertAction
    {
        /// <summary>Принять алерт (OK)</summary>
        Accept,
        /// <summary>Отклонить алерт (Cancel)</summary>
        Dismiss,
        /// <summary>Получить текст алерта</summary>
        GetText,
        /// <summary>Ввести текст в prompt</summary>
        SendKeys
    }

    /// <summary>
    /// Операция с вкладками браузера
    /// </summary>
    public enum TabOperation
    {
        /// <summary>Открыть новую вкладку</summary>
        OpenNewTab,
        /// <summary>Закрыть текущую вкладку</summary>
        CloseCurrentTab,
        /// <summary>Закрыть вкладку по handle</summary>
        CloseTabByHandle,
        /// <summary>Получить список всех handles</summary>
        GetAllHandles,
        /// <summary>Получить handle текущей вкладки</summary>
        GetCurrentHandle,
        /// <summary>Переключиться на вкладку по индексу</summary>
        SwitchToTab
    }

    /// <summary>
    /// Тип хранилища браузера
    /// </summary>
    public enum StorageType
    {
        /// <summary>localStorage — данные сохраняются между сессиями</summary>
        LocalStorage,
        /// <summary>sessionStorage — данные удаляются при закрытии вкладки</summary>
        SessionStorage
    }

    /// <summary>
    /// Операция с хранилищем браузера
    /// </summary>
    public enum StorageOperation
    {
        /// <summary>Получить значение по ключу</summary>
        GetItem,
        /// <summary>Установить значение</summary>
        SetItem,
        /// <summary>Удалить ключ</summary>
        RemoveItem,
        /// <summary>Очистить всё хранилище</summary>
        Clear,
        /// <summary>Получить все ключи</summary>
        GetAllKeys,
        /// <summary>Получить длину хранилища</summary>
        GetLength
    }

    /// <summary>
    /// Тип логов браузера
    /// </summary>
    public enum BrowserLogType
    {
        /// <summary>Логи браузера</summary>
        Browser,
        /// <summary>Логи драйвера</summary>
        Driver,
        /// <summary>Логи клиента</summary>
        Client,
        /// <summary>Логи сервера</summary>
        Server,
        /// <summary>Логи производительности</summary>
        Performance
    }

    /// <summary>
    /// Операция с multiple select элементом
    /// </summary>
    public enum MultiSelectOperation
    {
        /// <summary>Выбрать опцию по тексту</summary>
        SelectByText,
        /// <summary>Выбрать опцию по value</summary>
        SelectByValue,
        /// <summary>Выбрать опцию по индексу</summary>
        SelectByIndex,
        /// <summary>Снять выбор по тексту</summary>
        DeselectByText,
        /// <summary>Снять выбор по value</summary>
        DeselectByValue,
        /// <summary>Снять выбор по индексу</summary>
        DeselectByIndex,
        /// <summary>Снять все выборы</summary>
        DeselectAll,
        /// <summary>Получить все опции</summary>
        GetAllOptions,
        /// <summary>Получить выбранные опции</summary>
        GetSelectedOptions
    }

    /// <summary>
    /// Операция с окном браузера
    /// </summary>
    public enum WindowOperation
    {
        /// <summary>Развернуть окно на весь экран</summary>
        Maximize,
        /// <summary>Свернуть окно</summary>
        Minimize,
        /// <summary>Полноэкранный режим (F11)</summary>
        FullScreen,
        /// <summary>Установить размер окна</summary>
        SetSize,
        /// <summary>Установить позицию окна</summary>
        SetPosition,
        /// <summary>Получить текущий размер окна</summary>
        GetSize,
        /// <summary>Получить текущую позицию окна</summary>
        GetPosition
    }

    /// <summary>
    /// Специальные клавиши для отправки в элемент
    /// </summary>
    public enum SpecialKeyType
    {
        /// <summary>Enter</summary>
        Enter,
        /// <summary>Tab</summary>
        Tab,
        /// <summary>Escape</summary>
        Escape,
        /// <summary>Backspace</summary>
        Backspace,
        /// <summary>Delete</summary>
        Delete,
        /// <summary>Space (пробел)</summary>
        Space,
        /// <summary>Стрелка вверх</summary>
        ArrowUp,
        /// <summary>Стрелка вниз</summary>
        ArrowDown,
        /// <summary>Стрелка влево</summary>
        ArrowLeft,
        /// <summary>Стрелка вправо</summary>
        ArrowRight,
        /// <summary>Home</summary>
        Home,
        /// <summary>End</summary>
        End,
        /// <summary>Page Up</summary>
        PageUp,
        /// <summary>Page Down</summary>
        PageDown,
        /// <summary>F1</summary>
        F1,
        /// <summary>F2</summary>
        F2,
        /// <summary>F3</summary>
        F3,
        /// <summary>F4</summary>
        F4,
        /// <summary>F5</summary>
        F5,
        /// <summary>F6</summary>
        F6,
        /// <summary>F7</summary>
        F7,
        /// <summary>F8</summary>
        F8,
        /// <summary>F9</summary>
        F9,
        /// <summary>F10</summary>
        F10,
        /// <summary>F11</summary>
        F11,
        /// <summary>F12</summary>
        F12,
        /// <summary>Ctrl+A (выделить всё)</summary>
        CtrlA,
        /// <summary>Ctrl+C (копировать)</summary>
        CtrlC,
        /// <summary>Ctrl+V (вставить)</summary>
        CtrlV,
        /// <summary>Ctrl+X (вырезать)</summary>
        CtrlX,
        /// <summary>Ctrl+Z (отменить)</summary>
        CtrlZ,
        /// <summary>Shift+Tab</summary>
        ShiftTab,
        /// <summary>Alt+F4</summary>
        AltF4
    }

    /// <summary>
    /// Режим выполнения клика по элементу.
    /// </summary>
    public enum ClickMode
    {
        /// <summary>Обычный одиночный клик.</summary>
        Click,

        /// <summary>Двойной клик.</summary>
        DoubleClick,

        /// <summary>Правый клик (контекстное меню).</summary>
        RightClick,

        /// <summary>Клик с удержанием кнопки мыши.</summary>
        ClickAndHold
    }

    /// <summary>
    /// Режим поиска элементов на странице.
    /// </summary>
    public enum FindMode
    {
        /// <summary>Найти первый подходящий элемент и вернуть его ID.</summary>
        FindOne,

        /// <summary>Найти все подходящие элементы и вернуть список их ID.</summary>
        FindAll
    }

    /// <summary>
    /// Режим наведения на элемент.
    /// </summary>
    public enum HoverMode
    {
        /// <summary>Навести на центр элемента.</summary>
        Center,

        /// <summary>Навести с указанием смещения от центра элемента.</summary>
        WithOffset
    }

    /// <summary>
    /// Режим ввода данных в элемент.
    /// </summary>
    public enum InputMode
    {
        /// <summary>Ввести текст (очистить поле и ввести новый текст).</summary>
        TypeText,

        /// <summary>Отправить клавиши (включая специальные клавиши).</summary>
        SendKeys,

        /// <summary>Очистить поле ввода.</summary>
        Clear
    }

    /// <summary>
    /// Режим получения информации об элементе.
    /// </summary>
    public enum ElementInfoMode
    {
        /// <summary>Получить свойство элемента (атрибут, текст, value).</summary>
        Property,

        /// <summary>Получить вычисленный CSS-стиль.</summary>
        ComputedStyle,

        /// <summary>Получить размеры и позицию элемента (Rectangle).</summary>
        Rectangle
    }

    /// <summary>
    /// Режим навигации браузера.
    /// </summary>
    public enum NavigateMode
    {
        /// <summary>Перейти по URL.</summary>
        ToUrl,

        /// <summary>Назад в истории браузера.</summary>
        Back,

        /// <summary>Вперед в истории браузера.</summary>
        Forward,

        /// <summary>Обновить текущую страницу.</summary>
        Refresh
    }

    /// <summary>
    /// Тип информации о браузере для получения.
    /// </summary>
    public enum BrowserInfoType
    {
        /// <summary>Заголовок страницы (Title).</summary>
        Title,

        /// <summary>Текущий URL страницы.</summary>
        CurrentUrl,

        /// <summary>HTML-код страницы (PageSource).</summary>
        PageSource,

        /// <summary>Все три параметра сразу (Title, URL, PageSource).</summary>
        All
    }

    /// <summary>
    /// Режимы ожидания элементов на странице.
    /// </summary>
    public enum ElementWaitMode
    {
        /// <summary>
        /// Ожидать только появления элемента в DOM (может быть невидимым).
        /// </summary>
        Present,

        /// <summary>
        /// Ожидать пока элемент станет видимым (Displayed = true).
        /// </summary>
        Visible,

        /// <summary>
        /// Ожидать пока элемент станет кликабельным (Displayed = true и Enabled = true).
        /// </summary>
        Clickable,

        /// <summary>
        /// Не ожидать - попытаться найти элемент немедленно.
        /// </summary>
        None
    }

    /// <summary>
    /// Режимы поиска и проверки элементов на странице.
    /// </summary>
    public enum ElementSearchMode
    {
        /// <summary>
        /// Проверить существование элемента в DOM (может быть невидимым).
        /// Возвращает: ElementFound (bool)
        /// </summary>
        Exists,

        /// <summary>
        /// Проверить видимость элемента на странице.
        /// Возвращает: ElementFound (bool), IsVisible (bool)
        /// </summary>
        IsVisible,

        /// <summary>
        /// Проверить кликабельность элемента (видим и активен).
        /// Возвращает: ElementFound (bool), IsVisible (bool), IsEnabled (bool), IsClickable (bool)
        /// </summary>
        IsClickable,

        /// <summary>
        /// Ожидать элемент и проверить его полное состояние.
        /// Возвращает: ElementFound (bool), IsVisible (bool), IsEnabled (bool), IsSelected (bool)
        /// </summary>
        WaitAndCheck,

        /// <summary>
        /// Найти все элементы по локатору.
        /// Возвращает: ElementsFound (List&lt;string&gt;), ElementCount (int)
        /// </summary>
        FindAll,

        /// <summary>
        /// Подсчитать количество элементов по локатору.
        /// Возвращает: ElementCount (int)
        /// </summary>
        Count
    }

    // =========================================================================
    // ПЕРЕЧИСЛЕНИЯ ДЛЯ УТИЛИТ
    // =========================================================================

    /// <summary>
    /// Предустановленные темы оформления HTML таблицы.
    /// </summary>
    public enum HtmlTableTheme
    {
        /// <summary>
        /// Светлая тема: белый фон, тёмный текст, серые границы.
        /// </summary>
        Light,

        /// <summary>
        /// Тёмная тема: тёмный фон, светлый текст.
        /// </summary>
        Dark,

        /// <summary>
        /// Синяя тема: синий заголовок, чередующиеся синие оттенки.
        /// </summary>
        Blue,

        /// <summary>
        /// Зелёная тема: зелёный заголовок, чередующиеся зелёные оттенки.
        /// </summary>
        Green,

        /// <summary>
        /// Пользовательская тема: цвета задаются вручную.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Режим вывода HTML.
    /// </summary>
    public enum HtmlOutputMode
    {
        /// <summary>
        /// Только таблица: HTML-строка с тегами table/thead/tbody/tr/td.
        /// </summary>
        TableOnly,

        /// <summary>
        /// Полный HTML-документ: html/head/style/body с таблицей.
        /// </summary>
        FullDocument
    }

    // =========================================================================
    // ПРОИЗВОДСТВЕННЫЙ КАЛЕНДАРЬ
    // =========================================================================

    /// <summary>
    /// Режимы работы активности «Календарь: Операции».
    /// </summary>
    public enum BusinessCalendarMode
    {
        /// <summary>Прибавить рабочие дни к входной дате.</summary>
        AddWorkDays,

        /// <summary>Вычесть рабочие дни из входной даты.</summary>
        SubtractWorkDays,

        /// <summary>Проверить тип дня (рабочий/выходной/праздничный/сокращённый).</summary>
        CheckDayType,

        /// <summary>
        /// [П2] Подсчитать количество рабочих дней между InputDate и EndDate включительно.
        /// Требует заполненного поля Prop_EndDate.
        /// Результат записывается в Prop_WorkdaysBetween.
        /// </summary>
        CountWorkdays,

        /// <summary>
        /// [П3] Найти следующий рабочий день после InputDate.
        /// Если InputDate — рабочий, возвращается следующий рабочий день (не сам InputDate).
        /// </summary>
        NextWorkday,

        /// <summary>
        /// [П3] Найти предыдущий рабочий день до InputDate.
        /// Если InputDate — рабочий, возвращается предыдущий рабочий день (не сам InputDate).
        /// </summary>
        PrevWorkday
    }

    /// <summary>
    /// Формат файла календаря.
    /// </summary>
    public enum CalendarFormat
    {
        /// <summary>
        /// XML формат (calendar.xml).
        /// </summary>
        Xml,

        /// <summary>
        /// JSON формат (calendar.json).
        /// </summary>
        Json,

        /// <summary>
        /// CSV формат (calendar.csv).
        /// </summary>
        Csv,

        /// <summary>
        /// TXT формат (calendar.txt).
        /// </summary>
        Txt
    }

    /// <summary>
    /// Регион производственного календаря.
    /// </summary>
    public enum CalendarRegion
    {
        /// <summary>
        /// Россия (ru).
        /// </summary>
        Russia,

        /// <summary>
        /// Казахстан (kz).
        /// </summary>
        Kazakhstan,

        /// <summary>
        /// Беларусь (by).
        /// </summary>
        Belarus,

        /// <summary>
        /// Узбекистан (uz).
        /// </summary>
        Uzbekistan
    }

    /// <summary>
    /// Тип дня в производственном календаре.
    /// </summary>
    public enum DayType
    {
        /// <summary>
        /// Рабочий день.
        /// </summary>
        Workday,

        /// <summary>
        /// Выходной день.
        /// </summary>
        Weekend,

        /// <summary>
        /// Праздничный день.
        /// </summary>
        Holiday,

        /// <summary>
        /// Сокращённый рабочий день.
        /// </summary>
        ShortWorkday
    }

    /// <summary>
    /// Источник загрузки календаря.
    /// </summary>
    public enum CalendarSource
    {
        /// <summary>
        /// Загружен из файла.
        /// </summary>
        File,

        /// <summary>
        /// Загружен из интернета.
        /// </summary>
        Internet
    }

    /// <summary>
    /// Фильтр по дням недели для активности «Календарь: Рабочие дни списком».
    /// </summary>
    public enum DayOfWeekFilter
    {
        /// <summary>Все рабочие дни без фильтра по дню недели (по умолчанию).</summary>
        All,

        /// <summary>Только понедельники.</summary>
        MondayOnly,

        /// <summary>Только вторники.</summary>
        TuesdayOnly,

        /// <summary>Только среды.</summary>
        WednesdayOnly,

        /// <summary>Только четверги.</summary>
        ThursdayOnly,

        /// <summary>Только пятницы.</summary>
        FridayOnly,

        /// <summary>Только понедельники и пятницы.</summary>
        MondayAndFriday,

        /// <summary>
        /// Пользовательская маска через Prop_DayOfWeekMask.
        /// Формат строки: номера дней через запятую, Пн=1 ... Вс=7.
        /// Например: "1,3,5" — понедельник, среда, пятница.
        /// </summary>
        CustomMask
    }

    /// <summary>
    /// Период для поиска N-го рабочего дня.
    /// </summary>
    public enum NthWorkdayPeriod
    {
        /// <summary>
        /// Месяц. Используются параметры Prop_Month + Prop_Year.
        /// Например: 3-й рабочий день января 2026.
        /// </summary>
        Month,

        /// <summary>
        /// Квартал (1–4). Используются параметры Prop_Quarter + Prop_Year.
        /// Q1 = янв–мар, Q2 = апр–июн, Q3 = июл–сен, Q4 = окт–дек.
        /// </summary>
        Quarter,

        /// <summary>
        /// Полугодие. Prop_Quarter используется как номер полугодия (1 или 2).
        /// 1 = янв–июн, 2 = июл–дек.
        /// </summary>
        HalfYear,

        /// <summary>
        /// Весь год. Используется только Prop_Year.
        /// Например: последний рабочий день года.
        /// </summary>
        Year
    }

    /// <summary>
    /// Конкретный день недели — цель поиска в активности
    /// «Календарь: Следующий день недели».
    /// Отдельный enum, не связанный с DayOfWeekFilter (тот — для фильтрации множеств).
    /// </summary>
    public enum TargetDayOfWeek
    {
        /// <summary>Понедельник.</summary>
        Monday,

        /// <summary>Вторник.</summary>
        Tuesday,

        /// <summary>Среда.</summary>
        Wednesday,

        /// <summary>Четверг.</summary>
        Thursday,

        /// <summary>Пятница.</summary>
        Friday,

        /// <summary>Суббота.</summary>
        Saturday,

        /// <summary>Воскресенье.</summary>
        Sunday
    }

    /// <summary>
    /// Режим поиска ближайшего дня недели.
    /// </summary>
    public enum DayOfWeekSearchMode
    {
        /// <summary>
        /// Просто следующее вхождение указанного дня недели.
        /// Не проверяет рабочий ли день по производственному календарю.
        /// Используется когда важен именно день недели, а не его тип.
        /// </summary>
        NextOccurrence,

        /// <summary>
        /// Следующее вхождение, которое является рабочим днём по производственному календарю.
        /// Пропускает выходные понедельники, праздничные пятницы и т.д.
        /// Продолжает поиск дальше пока не найдёт рабочее вхождение или не исчерпает MaxLookAhead.
        /// </summary>
        NextWorkingOccurrence,

        /// <summary>
        /// Находит следующее вхождение дня недели.
        /// Если оно оказывается выходным — сдвигает на ближайший рабочий день
        /// согласно Prop_IfWeekend (NextWorkday или PrevWorkday).
        /// Гарантирует что результат всегда рабочий день.
        /// </summary>
        NearestWorkday
    }

    /// <summary>
    /// Поведение при попадании результата на выходной или праздничный день.
    /// Используется в активностях «Календарь: Следующий день недели»
    /// и «Календарь: Дедлайн».
    /// </summary>
    public enum DeadlineWeekendBehavior
    {
        /// <summary>
        /// Сдвинуть на следующий рабочий день.
        /// Стандарт для документооборота и назначения задач.
        /// </summary>
        NextWorkday,

        /// <summary>
        /// Сдвинуть на предыдущий рабочий день.
        /// Стандарт для платёжных дедлайнов — нельзя платить позже нормы.
        /// </summary>
        PrevWorkday,

        /// <summary>
        /// Не сдвигать — вернуть дату как есть, даже если выходной.
        /// Используется в режиме NextOccurrence когда тип дня не важен.
        /// </summary>
        AsIs
    }
    // =========================================================================
    // ШАБЛОНИЗАТОР ТЕКСТА
    // =========================================================================

    /// <summary>
    /// Синтаксис плейсхолдеров в шаблоне для активности «Текст: Шаблонизатор».
    /// </summary>
    public enum TemplateSyntax
    {
        /// <summary>
        /// Двойные фигурные скобки: {{ключ}}.
        /// По умолчанию. Не конфликтует с JSON и C#-интерполяцией строк.
        /// Пример: "Уважаемый {{ФИО}}, ваш заказ №{{НомерЗаказа}}"
        /// </summary>
        DoubleBrace,

        /// <summary>
        /// Одинарные фигурные скобки: {ключ}.
        /// Классический C# string.Format стиль.
        /// Пример: "Уважаемый {ФИО}, ваш заказ №{НомерЗаказа}"
        /// </summary>
        SingleBrace,

        /// <summary>
        /// Знаки процента: %ключ%.
        /// Стиль переменных окружения Windows.
        /// Пример: "Уважаемый %ФИО%, ваш заказ №%НомерЗаказа%"
        /// </summary>
        Percent
    }

    /// <summary>
    /// Поведение активности «Текст: Шаблонизатор» при отсутствии значения
    /// для плейсхолдера в переданном словаре.
    /// </summary>
    public enum MissingKeyBehavior
    {
        /// <summary>
        /// Оставить плейсхолдер без изменений.
        /// Удобно для отладки — сразу видно какие ключи не заполнены.
        /// Пример: {{ФИО}} → {{ФИО}}
        /// </summary>
        LeaveAsIs,

        /// <summary>
        /// Заменить плейсхолдер пустой строкой.
        /// Используется когда незаполненные поля допустимы.
        /// Пример: {{ФИО}} → ""
        /// </summary>
        ReplaceWithEmpty,

        /// <summary>
        /// Вернуть ошибку выполнения.
        /// Строгий режим: все плейсхолдеры обязаны иметь значение.
        /// Сообщение об ошибке содержит список незаполненных ключей.
        /// </summary>
        ThrowError
    }

    // =========================================================================
    // ТРАНСЛИТЕРАЦИЯ
    // =========================================================================

    /// <summary>
    /// Направление транслитерации для активности «Текст: Транслитерация».
    /// </summary>
    public enum TranslitDirection
    {
        /// <summary>
        /// Кириллица → Латиница.
        /// Например: "Иванов" → "Ivanov"
        /// </summary>
        CyrillicToLatin,

        /// <summary>
        /// Латиница → Кириллица (обратная транслитерация).
        /// Работает только для схем с однозначным обратным отображением (ISO9).
        /// Для остальных схем — приближённое восстановление.
        /// Например: "Ivanov" → "Иванов"
        /// </summary>
        LatinToCyrillic,

        /// <summary>
        /// Автоопределение по большинству символов в тексте.
        /// Если кириллических букв >= латинских — выбирается CyrillicToLatin.
        /// Иначе — LatinToCyrillic.
        /// </summary>
        AutoDetect
    }

    /// <summary>
    /// Схема (стандарт) транслитерации для активности «Текст: Транслитерация».
    /// </summary>
    public enum TranslitScheme
    {
        /// <summary>
        /// ГОСТ 7.79-2000, система Б (без диакритических знаков).
        /// Официальный российский стандарт для библиографических записей.
        /// Ё→YO, Ж→ZH, Х→KH, Ц→CZ, Ч→CH, Ш→SH, Щ→SHH, Ъ→'', Ы→Y, Ь→', Э→E, Ю→YU, Я→YA
        /// </summary>
        GOST7792000,

        /// <summary>
        /// Схема загранпаспортов РФ (Приказ МВД России от 16.11.2017 №827).
        /// Используется в российских загранпаспортах с 2013 года.
        /// Ё→E, Ж→ZH, Х→KH, Ц→TS, Ч→CH, Ш→SH, Щ→SHCH, Ъ→(удал.), Ы→Y, Ь→(удал.), Ю→YU, Я→YA
        /// </summary>
        Passport2013,

        /// <summary>
        /// Стандарт ИКАО Doc 9303 для машиносчитываемых документов.
        /// Используется в авиабилетах и международных документах.
        /// Ё→E, Й→I, Х→KH, Ц→TS, Ч→CH, Ш→SH, Щ→SHCH, Ъ→IE, Ы→Y, Ь→(удал.)
        /// </summary>
        ICAOPassport,

        /// <summary>
        /// Международный стандарт ISO 9:1995.
        /// Однозначная обратимая транслитерация с диакритическими знаками.
        /// Единственная схема с полной поддержкой LatinToCyrillic без потерь.
        /// </summary>
        ISO9,

        /// <summary>
        /// Схема BGN/PCGN 1947 (Board on Geographic Names / Permanent Committee on Geographical Names).
        /// Используется в английских географических названиях и картах.
        /// Ж→ZH, Х→KH, Ц→TS, Ч→CH, Ш→SH, Щ→SHCH, Ю→YU, Я→YA
        /// </summary>
        BGN_PCGN,

        /// <summary>
        /// Упрощённая ASCII-схема без диакритики.
        /// Результат всегда состоит только из символов [A-Za-z0-9], пробелов и Prop_ReplaceSpaces.
        /// Ъ и Ь удаляются. Ё→YO, Й→Y, Ж→ZH, Х→KH.
        /// Рекомендуется для: логинов, имён файлов, slug-строк, URL.
        /// </summary>
        Simplified
    }
}
