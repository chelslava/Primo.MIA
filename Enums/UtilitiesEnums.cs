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
}
