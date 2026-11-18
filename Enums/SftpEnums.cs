namespace Primo.MIA
{
    /// <summary>
    /// Направление синхронизации каталогов (issue #48).
    /// </summary>
    public enum SftpSyncDirection
    {
        /// <summary>Push: локальный каталог → удалённый (деплой, выгрузка).</summary>
        [RuName("Push (локальный → удалённый)")]
        Push,

        /// <summary>Pull: удалённый каталог → локальный (забор выгрузок, бэкапов).</summary>
        [RuName("Pull (удалённый → локальный)")]
        Pull
    }

    /// <summary>
    /// Критерий «файл изменился» при сравнении деревьев (issue #48).
    /// </summary>
    public enum SftpSyncCompareMode
    {
        /// <summary>Размер + время изменения (rsync-быстрая проверка; по умолчанию).</summary>
        [RuName("Размер + время")]
        SizeAndTime,

        /// <summary>Только размер — когда время на сторонах ненадёжно (часовые пояса, FAT).</summary>
        [RuName("Только размер")]
        SizeOnly
    }
}
