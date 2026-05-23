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
    /// Временной атрибут файла/папки для сравнения при очистке.
    /// </summary>
    public enum FileTimeAttribute
    {
        /// <summary>Дата и время создания файла или папки.</summary>
        CreationTime,

        /// <summary>Дата и время последнего изменения содержимого.</summary>
        LastWriteTime,

        /// <summary>Дата и время последнего обращения (чтения).</summary>
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
        OlderThanDays,

        /// <summary>
        /// Удалять объекты, временной атрибут которых раньше указанной даты.
        /// Дата задаётся в Prop_ThresholdDate.
        /// </summary>
        BeforeDate,

        /// <summary>
        /// Оба условия: старше N дней И раньше указанной даты.
        /// Применяется логическое И — удаляется только если выполнены оба условия.
        /// </summary>
        Both
    }

    /// <summary>
    /// Что удалять: файлы, папки или и то и другое.
    /// </summary>
    public enum CleanupTargetType
    {
        /// <summary>Удалять только файлы.</summary>
        FilesOnly,

        /// <summary>
        /// Удалять только пустые папки.
        /// Папка считается пустой если в ней нет файлов и вложенных папок.
        /// </summary>
        EmptyFoldersOnly,

        /// <summary>
        /// Удалять файлы И пустые папки.
        /// Сначала удаляются файлы, затем освободившиеся пустые папки.
        /// </summary>
        FilesAndEmptyFolders,

        /// <summary>
        /// Удалять папки целиком со всем содержимым (рекурсивно).
        /// Временной критерий применяется к самой папке, не к её содержимому.
        /// </summary>
        FoldersWithContent
    }
}
