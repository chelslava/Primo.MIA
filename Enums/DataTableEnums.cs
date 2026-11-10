namespace Primo.MIA
{
    /// <summary>Тип соединения таблиц (issue #36).</summary>
    public enum DataTableJoinKind
    {
        /// <summary>Только совпадающие строки обеих таблиц.</summary>
        [RuName("Inner (пересечение)")]
        Inner = 0,

        /// <summary>Все левые + совпавшие правые (без совпадения — DBNull).</summary>
        [RuName("Left (все левые)")]
        Left = 1,

        /// <summary>Все правые + совпавшие левые (без совпадения — DBNull).</summary>
        [RuName("Right (все правые)")]
        Right = 2,

        /// <summary>Все строки обеих таблиц (без совпадения — DBNull).</summary>
        [RuName("Full (объединение)")]
        Full = 3
    }

    /// <summary>Агрегат для Pivot (issue #36).</summary>
    public enum DataTableAggregation
    {
        /// <summary>Сумма числовых значений.</summary>
        [RuName("Сумма")]
        Sum = 0,

        /// <summary>Количество значений.</summary>
        [RuName("Количество")]
        Count = 1,

        /// <summary>Минимум (числа и даты).</summary>
        [RuName("Минимум")]
        Min = 2,

        /// <summary>Максимум (числа и даты).</summary>
        [RuName("Максимум")]
        Max = 3,

        /// <summary>Первое встреченное значение (любой тип).</summary>
        [RuName("Первое")]
        First = 4
    }

    /// <summary>Режим активности «DataTable: Pivot / Unpivot» (issue #36).</summary>
    public enum DataTablePivotMode
    {
        /// <summary>Сводная таблица: строки × значения pivot-колонки → агрегат.</summary>
        [RuName("Pivot (сводная)")]
        Pivot = 0,

        /// <summary>Разбор сводной: широкая форма → длинная (столбец-столбец-значение).</summary>
        [RuName("Unpivot (разбор)")]
        Unpivot = 1
    }
}
