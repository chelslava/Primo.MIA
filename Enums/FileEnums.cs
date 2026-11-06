namespace Primo.MIA
{
    /// <summary>
    /// Режим работы активности
    /// </summary>
    public enum WaitFileMode
    {
        /// <summary>
        /// Ожидать появления нового файла (которого не было при запуске)
        /// </summary>
        [RuName("Новый файл")]
        WaitForNewFile,

        /// <summary>
        /// Ожидать файл независимо от того, был он или нет
        /// </summary>
        [RuName("Любой файл")]
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
        [RuName("Точное совпадение")]
        Exact,

        /// <summary>
        /// Wildcard фильтрация (*, ?)
        /// </summary>
        [RuName("Wildcard")]
        Wildcard,

        /// <summary>
        /// Regex фильтрация
        /// </summary>
        [RuName("Regex")]
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
        [RuName("Только файлы")]
        FilesOnly,

        /// <summary>
        /// Поиск только папок
        /// </summary>
        [RuName("Только папки")]
        FoldersOnly,

        /// <summary>
        /// Поиск файлов и папок
        /// </summary>
        [RuName("Файлы и папки")]
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
        [RuName("Wildcard")]
        Wildcard,

        /// <summary>
        /// Regex фильтрация
        /// </summary>
        [RuName("Regex")]
        Regex
    }

    /// <summary>
    /// Временной атрибут файла/папки для сравнения при очистке.
    /// </summary>
    public enum FileTimeAttribute
    {
        /// <summary>Дата и время создания файла или папки.</summary>
        [RuName("Дата создания")]
        CreationTime,

        /// <summary>Дата и время последнего изменения содержимого.</summary>
        [RuName("Дата изменения")]
        LastWriteTime,

        /// <summary>Дата и время последнего обращения (чтения).</summary>
        [RuName("Дата последнего обращения")]
        LastAccessTime
    }

    /// <summary>
    /// Способ задания порогового значения для очистки.
    /// </summary>
    public enum CleanupThresholdMode
    {
        /// <summary>
        /// Удалять объекты старше N дней от текущего момента.
        /// Порог вычисляется как DateTime.Now.AddDays(-OlderThanDays).
        /// </summary>
        [RuName("Старше N дней")]
        OlderThanDays,

        /// <summary>
        /// Удалять объекты, временной атрибут которых раньше указанной даты.
        /// Дата задаётся в Prop_ThresholdDate.
        /// </summary>
        [RuName("Раньше даты")]
        BeforeDate,

        /// <summary>
        /// Оба условия: старше N дней И раньше указанной даты.
        /// Применяется логическое И — удаляется только если выполнены оба условия.
        /// </summary>
        [RuName("Оба условия")]
        Both
    }

    /// <summary>
    /// Что удалять: файлы, папки или и то и другое.
    /// </summary>
    public enum CleanupTargetType
    {
        /// <summary>Удалять только файлы.</summary>
        [RuName("Только файлы")]
        FilesOnly,

        /// <summary>
        /// Удалять только пустые папки.
        /// Папка считается пустой если в ней нет файлов и вложенных папок.
        /// </summary>
        [RuName("Только пустые папки")]
        EmptyFoldersOnly,

        /// <summary>
        /// Удалять файлы И пустые папки.
        /// Сначала удаляются файлы, затем освободившиеся пустые папки.
        /// </summary>
        [RuName("Файлы и пустые папки")]
        FilesAndEmptyFolders,

        /// <summary>
        /// Удалять папки целиком со всем содержимым (рекурсивно).
        /// Временной критерий применяется к самой папке, не к её содержимому.
        /// </summary>
        [RuName("Папки целиком")]
        FoldersWithContent
    }
}
