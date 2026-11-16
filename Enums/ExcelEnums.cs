namespace Primo.MIA
{
    /// <summary>Способ записи диапазона в Excel (issue #47).</summary>
    public enum ExcelWriteMode
    {
        /// <summary>Перезаписать данные начиная с начальной ячейки.</summary>
        [RuName("Перезаписать (от начальной ячейки)")]
        Overwrite = 0,

        /// <summary>Дописать строки после последней занятой строки листа.</summary>
        [RuName("Дописать после последней строки")]
        Append = 1
    }
}
