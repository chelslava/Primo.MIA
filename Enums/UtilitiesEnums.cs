namespace Primo.MIA
{
    /// <summary>
    /// Режим чтения TOML-конфигурации
    /// </summary>
    public enum TomlReadMode
    {
        /// <summary>
        /// Прочитать одно значение по ключу.
        /// Поддерживает вложенность через точку: "section.subsection.key"
        /// </summary>
        [RuName("Одно значение")]
        SingleValue,

        /// <summary>
        /// Прочитать все ключи указанной секции в Dictionary&lt;string, string&gt;.
        /// Поддерживает вложенные секции: "app.database"
        /// </summary>
        [RuName("Секция в словарь")]
        SectionToDictionary,

        /// <summary>
        /// Прочитать весь файл в плоский словарь.
        /// Ключи вложенных секций объединяются через точку: "server.host"
        /// </summary>
        [RuName("Весь файл в словарь")]
        FullFileToDictionary,

        /// <summary>
        /// Чтение конфига с профилями окружений.
        /// Мёржит секцию [default] с секцией выбранного профиля ([production], [staging] и т.д.)
        /// по выбранной стратегии слияния.
        /// </summary>
        [RuName("Профиль окружения")]
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
        [RuName("Default + профиль")]
        DefaultThenProfile,

        /// <summary>
        /// Только секция профиля, без слияния с default.
        /// Используется когда профили полностью самодостаточны.
        /// </summary>
        [RuName("Только профиль")]
        ProfileOnly,

        /// <summary>
        /// Обратный порядок: профиль как база, default добавляет только недостающие ключи.
        /// Ключи профиля никогда не перекрываются default.
        /// </summary>
        [RuName("Профиль + default")]
        ProfileThenDefault
    }

    /// <summary>
    /// Уровень логирования
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Отладочная информация
        /// </summary>
        [RuName("Отладка")]
        Debug,

        /// <summary>
        /// Информационное сообщение
        /// </summary>
        [RuName("Информация")]
        Info,

        /// <summary>
        /// Предупреждение
        /// </summary>
        [RuName("Предупреждение")]
        Warning,

        /// <summary>
        /// Ошибка
        /// </summary>
        [RuName("Ошибка")]
        Error,

        /// <summary>
        /// Критическая ошибка
        /// </summary>
        [RuName("Критическая ошибка")]
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
        [RuName("Только файл")]
        FileOnly,

        /// <summary>
        /// Только в консоль
        /// </summary>
        [RuName("Только консоль")]
        ConsoleOnly,

        /// <summary>
        /// В файл и консоль одновременно
        /// </summary>
        [RuName("Файл и консоль")]
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
        [RuName("Без ротации")]
        None,

        /// <summary>
        /// Новый файл каждый день
        /// </summary>
        [RuName("Ежедневно")]
        Daily,

        /// <summary>
        /// Новый файл каждый час
        /// </summary>
        [RuName("Ежечасно")]
        Hourly,

        /// <summary>
        /// Новый файл при превышении размера
        /// </summary>
        [RuName("По размеру")]
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
        [RuName("Фиксированное имя")]
        Fixed,

        /// <summary>
        /// С датой (app_2025-02-15.log)
        /// </summary>
        [RuName("С датой")]
        WithDate,

        /// <summary>
        /// С датой и временем (app_2025-02-15_14-30.log)
        /// </summary>
        [RuName("С датой и временем")]
        WithDateTime,

        /// <summary>
        /// С timestamp (app_20250215143045.log)
        /// </summary>
        [RuName("С timestamp")]
        WithTimestamp,

        /// <summary>
        /// Пользовательский формат
        /// </summary>
        [RuName("Пользовательский")]
        Custom
    }

    /// <summary>
    /// Тип генерируемого значения.
    /// Определяет какой набор параметров будет использоваться.
    /// </summary>
    public enum GeneratorType
    {
        /// <summary>Генерация GUID (UUID) — глобально уникального идентификатора</summary>
        [RuName("GUID")]
        Guid,
        /// <summary>Генерация уникального имени файла с учётом уже существующих файлов</summary>
        [RuName("Имя файла")]
        FileName,
        /// <summary>Генерация случайного целого числа в заданном диапазоне</summary>
        [RuName("Случайное число")]
        RandomNumber,
        /// <summary>Форматирование текущей даты/времени в строку</summary>
        [RuName("Дата/время")]
        Timestamp,
        /// <summary>Последовательный счётчик с настраиваемым начальным значением и шагом</summary>
        [RuName("Счётчик")]
        Counter,
        /// <summary>Детерминированный хеш-идентификатор на основе SHA-256</summary>
        [RuName("Hash-ID (SHA-256)")]
        HashId,
        /// <summary>Формирование email-адреса из имени, фамилии и домена</summary>
        [RuName("Email из имени")]
        Username,
        /// <summary>Заполнение строки-шаблона значениями из словаря по ключам {key}</summary>
        [RuName("Шаблон")]
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
        [RuName("N (без дефисов)")]
        N,
        /// <summary>
        /// 32 цифры, разделённые дефисами (стандарт RFC 4122).
        /// Пример: 00000000-0000-0000-0000-000000000000
        /// </summary>
        [RuName("D (с дефисами)")]
        D,
        /// <summary>
        /// 32 цифры с дефисами в фигурных скобках.
        /// Пример: {00000000-0000-0000-0000-000000000000}
        /// </summary>
        [RuName("B (в фигурных скобках)")]
        B,
        /// <summary>
        /// 32 цифры с дефисами в круглых скобках.
        /// Пример: (00000000-0000-0000-0000-000000000000)
        /// </summary>
        [RuName("P (в круглых скобках)")]
        P,
        /// <summary>
        /// Четыре шестнадцатеричных значения в фигурных скобках.
        /// Пример: {0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}
        /// </summary>
        [RuName("X (hex-значения)")]
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
        [RuName("20250215_143045")]
        yyyyMMdd_HHmmss,

        /// <summary>20250215_1430 — дата и время до минут, для ежеминутных файлов</summary>
        [RuName("20250215_1430")]
        yyyyMMdd_HHmm,

        /// <summary>20250215 — только дата слитно (для ежедневных файлов)</summary>
        [RuName("20250215")]
        yyyyMMdd,

        /// <summary>202502 — год и месяц (для ежемесячных файлов)</summary>
        [RuName("202502")]
        yyyyMM,

        /// <summary>20250215143045123 — с миллисекундами, максимальная уникальность</summary>
        [RuName("20250215143045123")]
        yyyyMMddHHmmssfff,

        // ── Форматы ISO 8601 (для баз данных и API) ────────────────────────────

        /// <summary>2025-02-15 — дата ISO (стандарт для БД и API)</summary>
        [RuName("2025-02-15")]
        yyyy_MM_dd,

        /// <summary>2025-02-15 14:30:45 — дата и время ISO без временной зоны</summary>
        [RuName("2025-02-15 14:30:45")]
        yyyy_MM_dd_HH_mm_ss,

        /// <summary>2025-02-15T14:30:45 — дата и время ISO с разделителем T</summary>
        [RuName("2025-02-15T14:30:45")]
        ISO8601,

        /// <summary>2025-02-15T14:30:45.123 — ISO с миллисекундами</summary>
        [RuName("2025-02-15T14:30:45.123")]
        ISO8601_ms,

        // ── Русские и европейские форматы (для отчётов и документов) ──────────

        /// <summary>15.02.2025 — российский формат ДД.ММ.ГГГГ</summary>
        [RuName("15.02.2025")]
        dd_MM_yyyy,

        /// <summary>15.02.2025 14:30:45 — российский формат с временем</summary>
        [RuName("15.02.2025 14:30:45")]
        dd_MM_yyyy_HH_mm_ss,

        /// <summary>15.02.2025 14:30 — российский формат до минут</summary>
        [RuName("15.02.2025 14:30")]
        dd_MM_yyyy_HH_mm,

        /// <summary>15/02/2025 — европейский формат ДД/ММ/ГГГГ</summary>
        [RuName("15/02/2025")]
        dd_MM_yyyy_slash,

        // ── Американский формат ────────────────────────────────────────────────

        /// <summary>02/15/2025 — американский формат ММ/ДД/ГГГГ</summary>
        [RuName("02/15/2025")]
        MM_dd_yyyy,

        /// <summary>02/15/2025 14:30:45 — американский формат с временем</summary>
        [RuName("02/15/2025 14:30:45")]
        MM_dd_yyyy_HH_mm_ss,

        // ── Только время ───────────────────────────────────────────────────────

        /// <summary>14:30:45 — только время ЧЧ:ММ:СС</summary>
        [RuName("14:30:45")]
        HHmmss,

        /// <summary>14:30 — только время ЧЧ:ММ</summary>
        [RuName("14:30")]
        HHmm,

        /// <summary>14:30:45.123 — время с миллисекундами</summary>
        [RuName("14:30:45.123")]
        HHmmss_fff,

        // ── Специальные форматы ────────────────────────────────────────────────

        /// <summary>Суббота, 15 февраля 2025 г. — полная дата с днём недели (зависит от культуры)</summary>
        [RuName("Полная дата с днём недели")]
        FullDate,

        /// <summary>Q1-2025, Q2-2025... — квартал и год (вычисляется вручную)</summary>
        [RuName("Квартал (Q1-2025)")]
        Quarter,

        /// <summary>W07-2025 — номер недели ISO и год</summary>
        [RuName("Номер недели (W07-2025)")]
        WeekNumber,

        /// <summary>Unix Timestamp — секунды с 01.01.1970 UTC</summary>
        [RuName("Unix Timestamp")]
        UnixTimestamp,

        /// <summary>Пользовательский формат из поля Prop_CustomTimestampFormat</summary>
        [RuName("Пользовательский")]
        Custom
    }

    /// <summary>
    /// Предустановленные темы оформления HTML таблицы.
    /// </summary>
    public enum HtmlTableTheme
    {
        /// <summary>
        /// Светлая тема: белый фон, тёмный текст, серые границы.
        /// </summary>
        [RuName("Светлая")]
        Light,

        /// <summary>
        /// Тёмная тема: тёмный фон, светлый текст.
        /// </summary>
        [RuName("Тёмная")]
        Dark,

        /// <summary>
        /// Синяя тема: синий заголовок, чередующиеся синие оттенки.
        /// </summary>
        [RuName("Синяя")]
        Blue,

        /// <summary>
        /// Зелёная тема: зелёный заголовок, чередующиеся зелёные оттенки.
        /// </summary>
        [RuName("Зелёная")]
        Green,

        /// <summary>
        /// Пользовательская тема: цвета задаются вручную.
        /// </summary>
        [RuName("Пользовательская")]
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
        [RuName("Только таблица")]
        TableOnly,

        /// <summary>
        /// Полный HTML-документ: html/head/style/body с таблицей.
        /// </summary>
        [RuName("Полный HTML-документ")]
        FullDocument
    }

    /// <summary>
    /// Режим задания условий фильтрации DataTable.
    /// </summary>
    public enum DataTableFilterMode
    {
        /// <summary>
        /// SQL-подобное выражение (аналог WHERE):
        /// Age &gt;= 18 AND Name LIKE 'Ив%' OR (City = 'Москва' AND Age &lt; 30)
        /// </summary>
        [RuName("SQL-выражение (WHERE)")]
        SqlQuery,

        /// <summary>
        /// Конструктор условий: строки вида «Колонка + Оператор + Значение»,
        /// объединённые И/ИЛИ.
        /// </summary>
        [RuName("Конструктор условий")]
        ConditionBuilder
    }

    /// <summary>
    /// Оператор сравнения условия в конструкторе фильтра DataTable.
    /// </summary>
    public enum DataTableConditionOperator
    {
        /// <summary>Равно (=).</summary>
        [RuName("Равно")]
        Equal,

        /// <summary>Не равно (&lt;&gt;).</summary>
        [RuName("Не равно")]
        NotEqual,

        /// <summary>Больше (&gt;).</summary>
        [RuName("Больше")]
        GreaterThan,

        /// <summary>Больше или равно (&gt;=).</summary>
        [RuName("Больше или равно")]
        GreaterOrEqual,

        /// <summary>Меньше (&lt;).</summary>
        [RuName("Меньше")]
        LessThan,

        /// <summary>Меньше или равно (&lt;=).</summary>
        [RuName("Меньше или равно")]
        LessOrEqual,

        /// <summary>Строка содержит подстроку.</summary>
        [RuName("Содержит")]
        Contains,

        /// <summary>Строка начинается с подстроки.</summary>
        [RuName("Начинается с")]
        StartsWith,

        /// <summary>Строка заканчивается подстрокой.</summary>
        [RuName("Заканчивается на")]
        EndsWith,

        /// <summary>Соответствие шаблону LIKE (% — любое число символов, _ — один символ).</summary>
        [RuName("LIKE")]
        Like,

        /// <summary>Значение входит в список (значения через запятую).</summary>
        [RuName("В списке (IN)")]
        In,

        /// <summary>Значение пустое (null или DBNull).</summary>
        [RuName("Пустое (NULL)")]
        IsNull,

        /// <summary>Значение непустое.</summary>
        [RuName("Непустое (NOT NULL)")]
        IsNotNull
    }

    /// <summary>
    /// Логическая связка условия с предыдущим в конструкторе фильтра DataTable.
    /// Условия объединяются слева направо: (c1 ∨/∧ c2) ∨/∧ c3 ...
    /// </summary>
    public enum DataTableConditionJoin
    {
        /// <summary>И (AND) — с первым условием игнорируется.</summary>
        [RuName("И")]
        And,

        /// <summary>ИЛИ (OR) — с первым условием игнорируется.</summary>
        [RuName("ИЛИ")]
        Or
    }
}
