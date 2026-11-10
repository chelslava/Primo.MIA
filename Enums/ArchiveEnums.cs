namespace Primo.MIA
{
    /// <summary>Политика перезаписи при распаковке (issue #28).</summary>
    public enum ArchiveOverwritePolicy
    {
        /// <summary>Пропустить существующие файлы.</summary>
        [RuName("Пропускать")]
        Skip = 0,

        /// <summary>Перезаписать существующие файлы.</summary>
        [RuName("Перезаписывать")]
        Overwrite = 1,

        /// <summary>Ошибка при существующем файле.</summary>
        [RuName("Ошибка")]
        Throw = 2
    }

    /// <summary>Уровень сжатия ZIP-архива.</summary>
    /// <remarks>SmallestSize отсутствует в net481 (появился в .NET Core 3.0) —
    /// поэтому только три уровня, доступных System.IO.Compression на .NET Framework.</remarks>
    public enum ArchiveCompressionLevel
    {
        /// <summary>Оптимальное сжатие (по умолчанию).</summary>
        [RuName("Оптимальное")]
        Optimal = 0,

        /// <summary>Быстрое сжатие (крупные объёмы).</summary>
        [RuName("Быстрое")]
        Fastest = 1,

        /// <summary>Без сжатия (только упаковка).</summary>
        [RuName("Без сжатия")]
        NoCompression = 2
    }
}
