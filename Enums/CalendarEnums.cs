namespace Primo.MIA
{
    /// <summary>
    /// Режимы работы активности «Календарь: Операции».
    /// </summary>
    public enum BusinessCalendarMode
    {
        /// <summary>Прибавить рабочие дни к входной дате.</summary>
        AddWorkDays,

        /// <summary>Вычесть рабочие дни из входной даты.</summary>
        SubtractWorkDays,

        /// <summary>Проверить тип дня (рабочий/выходной/праздничный/сокращённый).</summary>
        CheckDayType,

        /// <summary>
        /// [П2] Подсчитать количество рабочих дней между InputDate и EndDate включительно.
        /// Требует заполненного поля Prop_EndDate.
        /// Результат записывается в Prop_WorkdaysBetween.
        /// </summary>
        CountWorkdays,

        /// <summary>
        /// [П3] Найти следующий рабочий день после InputDate.
        /// Если InputDate — рабочий, возвращается следующий рабочий день (не сам InputDate).
        /// </summary>
        NextWorkday,

        /// <summary>
        /// [П3] Найти предыдущий рабочий день до InputDate.
        /// Если InputDate — рабочий, возвращается предыдущий рабочий день (не сам InputDate).
        /// </summary>
        PrevWorkday
    }

    /// <summary>
    /// Формат файла календаря.
    /// </summary>
    public enum CalendarFormat
    {
        /// <summary>
        /// XML формат (calendar.xml).
        /// </summary>
        Xml,

        /// <summary>
        /// JSON формат (calendar.json).
        /// </summary>
        Json,

        /// <summary>
        /// CSV формат (calendar.csv).
        /// </summary>
        Csv,

        /// <summary>
        /// TXT формат (calendar.txt).
        /// </summary>
        Txt
    }

    /// <summary>
    /// Регион производственного календаря.
    /// </summary>
    public enum CalendarRegion
    {
        /// <summary>
        /// Россия (ru).
        /// </summary>
        Russia,

        /// <summary>
        /// Казахстан (kz).
        /// </summary>
        Kazakhstan,

        /// <summary>
        /// Беларусь (by).
        /// </summary>
        Belarus,

        /// <summary>
        /// Узбекистан (uz).
        /// </summary>
        Uzbekistan
    }

    /// <summary>
    /// Тип дня в производственном календаре.
    /// </summary>
    public enum DayType
    {
        /// <summary>
        /// Рабочий день.
        /// </summary>
        Workday,

        /// <summary>
        /// Выходной день.
        /// </summary>
        Weekend,

        /// <summary>
        /// Праздничный день.
        /// </summary>
        Holiday,

        /// <summary>
        /// Сокращённый рабочий день.
        /// </summary>
        ShortWorkday
    }

    /// <summary>
    /// Источник загрузки календаря.
    /// </summary>
    public enum CalendarSource
    {
        /// <summary>
        /// Загружен из файла.
        /// </summary>
        File,

        /// <summary>
        /// Загружен из интернета.
        /// </summary>
        Internet
    }

    /// <summary>
    /// Фильтр по дням недели для активности «Календарь: Рабочие дни списком».
    /// </summary>
    public enum DayOfWeekFilter
    {
        /// <summary>Все рабочие дни без фильтра по дню недели (по умолчанию).</summary>
        All,

        /// <summary>Только понедельники.</summary>
        MondayOnly,

        /// <summary>Только вторники.</summary>
        TuesdayOnly,

        /// <summary>Только среды.</summary>
        WednesdayOnly,

        /// <summary>Только четверги.</summary>
        ThursdayOnly,

        /// <summary>Только пятницы.</summary>
        FridayOnly,

        /// <summary>Только понедельники и пятницы.</summary>
        MondayAndFriday,

        /// <summary>
        /// Пользовательская маска через Prop_DayOfWeekMask.
        /// Формат строки: номера дней через запятую, Пн=1 ... Вс=7.
        /// Например: "1,3,5" — понедельник, среда, пятница.
        /// </summary>
        CustomMask
    }

    /// <summary>
    /// Период для поиска N-го рабочего дня.
    /// </summary>
    public enum NthWorkdayPeriod
    {
        /// <summary>
        /// Месяц. Используются параметры Prop_Month + Prop_Year.
        /// Например: 3-й рабочий день января 2026.
        /// </summary>
        Month,

        /// <summary>
        /// Квартал (1–4). Используются параметры Prop_Quarter + Prop_Year.
        /// Q1 = янв–мар, Q2 = апр–июн, Q3 = июл–сен, Q4 = окт–дек.
        /// </summary>
        Quarter,

        /// <summary>
        /// Полугодие. Prop_Quarter используется как номер полугодия (1 или 2).
        /// 1 = янв–июн, 2 = июл–дек.
        /// </summary>
        HalfYear,

        /// <summary>
        /// Весь год. Используется только Prop_Year.
        /// Например: последний рабочий день года.
        /// </summary>
        Year
    }

    /// <summary>
    /// Конкретный день недели — цель поиска в активности
    /// «Календарь: Следующий день недели».
    /// Отдельный enum, не связанный с DayOfWeekFilter (тот — для фильтрации множеств).
    /// </summary>
    public enum TargetDayOfWeek
    {
        /// <summary>Понедельник.</summary>
        Monday,

        /// <summary>Вторник.</summary>
        Tuesday,

        /// <summary>Среда.</summary>
        Wednesday,

        /// <summary>Четверг.</summary>
        Thursday,

        /// <summary>Пятница.</summary>
        Friday,

        /// <summary>Суббота.</summary>
        Saturday,

        /// <summary>Воскресенье.</summary>
        Sunday
    }

    /// <summary>
    /// Режим поиска ближайшего дня недели.
    /// </summary>
    public enum DayOfWeekSearchMode
    {
        /// <summary>
        /// Просто следующее вхождение указанного дня недели.
        /// Не проверяет рабочий ли день по производственному календарю.
        /// Используется когда важен именно день недели, а не его тип.
        /// </summary>
        NextOccurrence,

        /// <summary>
        /// Следующее вхождение, которое является рабочим днём по производственному календарю.
        /// Пропускает выходные понедельники, праздничные пятницы и т.д.
        /// Продолжает поиск дальше пока не найдёт рабочее вхождение или не исчерпает MaxLookAhead.
        /// </summary>
        NextWorkingOccurrence,

        /// <summary>
        /// Находит следующее вхождение дня недели.
        /// Если оно оказывается выходным — сдвигает на ближайший рабочий день
        /// согласно Prop_IfWeekend (NextWorkday или PrevWorkday).
        /// Гарантирует что результат всегда рабочий день.
        /// </summary>
        NearestWorkday
    }

    /// <summary>
    /// Поведение при попадании результата на выходной или праздничный день.
    /// Используется в активностях «Календарь: Следующий день недели»
    /// и «Календарь: Дедлайн».
    /// </summary>
    public enum DeadlineWeekendBehavior
    {
        /// <summary>
        /// Сдвинуть на следующий рабочий день.
        /// Стандарт для документооборота и назначения задач.
        /// </summary>
        NextWorkday,

        /// <summary>
        /// Сдвинуть на предыдущий рабочий день.
        /// Стандарт для платёжных дедлайнов — нельзя платить позже нормы.
        /// </summary>
        PrevWorkday,

        /// <summary>
        /// Не сдвигать — вернуть дату как есть, даже если выходной.
        /// Используется в режиме NextOccurrence когда тип дня не важен.
        /// </summary>
        AsIs
    }
}
