namespace Primo.MIA
{
    /// <summary>
    /// Тип выполняемой команды ADO.NET.
    /// </summary>
    public enum DatabaseCommandType
    {
        /// <summary>Обычный SQL-текст.</summary>
        Text,

        /// <summary>Хранимая процедура.</summary>
        StoredProcedure
    }

    /// <summary>
    /// Уровень изоляции транзакции БД.
    /// </summary>
    public enum DatabaseIsolationLevel
    {
        ReadCommitted,
        ReadUncommitted,
        RepeatableRead,
        Serializable,
        Snapshot,
        Unspecified
    }

    /// <summary>
    /// Предварительная очистка таблицы перед bulk insert.
    /// </summary>
    public enum DatabaseBulkPreloadMode
    {
        None,
        DeleteAll,
        Truncate
    }
}
