namespace Primo.MIA
{
    /// <summary>Источник/приёмник CSV-данных (issue #29).</summary>
    public enum CsvLocation
    {
        /// <summary>Строка с CSV-текстом.</summary>
        [RuName("Строка")]
        String = 0,

        /// <summary>Файл на диске.</summary>
        [RuName("Файл")]
        File = 1
    }
}
