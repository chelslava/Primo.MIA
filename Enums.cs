using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
