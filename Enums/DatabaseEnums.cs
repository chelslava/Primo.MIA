namespace Primo.MIA
{
    /// <summary>
    /// Тип выполняемой команды ADO.NET.
    /// </summary>
    public enum DatabaseCommandType
    {
        /// <summary>Обычный SQL-текст.</summary>
        [RuName("SQL-текст")]
        Text,

        /// <summary>Хранимая процедура.</summary>
        [RuName("Хранимая процедура")]
        StoredProcedure
    }

    /// <summary>
    /// Уровень изоляции транзакции БД.
    /// </summary>
    public enum DatabaseIsolationLevel
    {
        [RuName("ReadCommitted")]
        ReadCommitted,
        [RuName("ReadUncommitted")]
        ReadUncommitted,
        [RuName("RepeatableRead")]
        RepeatableRead,
        [RuName("Serializable")]
        Serializable,
        [RuName("Snapshot")]
        Snapshot,
        [RuName("Unspecified")]
        Unspecified
    }

    /// <summary>
    /// Предварительная очистка таблицы перед bulk insert.
    /// </summary>
    public enum DatabaseBulkPreloadMode
    {
        [RuName("Без очистки")]
        None,
        [RuName("Удалить все строки")]
        DeleteAll,
        [RuName("TRUNCATE")]
        Truncate
    }
}
