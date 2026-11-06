namespace Primo.MIA
{
    /// <summary>
    /// Режимы работы активности «Календарь: Операции».
    /// </summary>
    public enum BusinessCalendarMode
    {
        /// <summary>Прибавить рабочие дни к входной дате.</summary>
        [RuName("Прибавить рабочие дни")]
        AddWorkDays,

        /// <summary>Вычесть рабочие дни из входной даты.</summary>
        [RuName("Вычесть рабочие дни")]
        SubtractWorkDays,

        /// <summary>Проверить тип дня (рабочий/выходной/праздничный/сокращённый).</summary>
        [RuName("Проверить тип дня")]
        CheckDayType,

        /// <summary>
        /// [П2] Подсчитать количество рабочих дней между InputDate и EndDate включительно.
        /// Требует заполненного поля Prop_EndDate.
        /// Результат записывается в Prop_WorkdaysBetween.
        /// </summary>
        [RuName("Подсчитать рабочие дни")]
        CountWorkdays,

        /// <summary>
        /// [П3] Найти следующий рабочий день после InputDate.
        /// Если InputDate — рабочий, возвращается следующий рабочий день (не сам InputDate).
        /// </summary>
        [RuName("Следующий рабочий день")]
        NextWorkday,

        /// <summary>
        /// [П3] Найти предыдущий рабочий день до InputDate.
        /// Если InputDate — рабочий, возвращается предыдущий рабочий день (не сам InputDate).
        /// </summary>
        [RuName("Предыдущий рабочий день")]
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
        [RuName("XML")]
        Xml,

        /// <summary>
        /// JSON формат (calendar.json).
        /// </summary>
        [RuName("JSON")]
        Json,

        /// <summary>
        /// CSV формат (calendar.csv).
        /// </summary>
        [RuName("CSV")]
        Csv,

        /// <summary>
        /// TXT формат (calendar.txt).
        /// </summary>
        [RuName("TXT")]
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
        [RuName("Россия")]
        Russia,

        /// <summary>
        /// Казахстан (kz).
        /// </summary>
        [RuName("Казахстан")]
        Kazakhstan,

        /// <summary>
        /// Беларусь (by).
        /// </summary>
        [RuName("Беларусь")]
        Belarus,

        /// <summary>
        /// Узбекистан (uz).
        /// </summary>
        [RuName("Узбекистан")]
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
        [RuName("Рабочий")]
        Workday,

        /// <summary>
        /// Выходной день.
        /// </summary>
        [RuName("Выходной")]
        Weekend,

        /// <summary>
        /// Праздничный день.
        /// </summary>
        [RuName("Праздничный")]
        Holiday,

        /// <summary>
        /// Сокращённый рабочий день.
        /// </summary>
        [RuName("Сокращённый")]
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
        [RuName("Файл")]
        File,

        /// <summary>
        /// Загружен из интернета.
        /// </summary>
        [RuName("Интернет")]
        Internet
    }

    /// <summary>
    /// Фильтр по дням недели для активности «Календарь: Рабочие дни списком».
    /// </summary>
    public enum DayOfWeekFilter
    {
        /// <summary>Все рабочие дни без фильтра по дню недели (по умолчанию).</summary>
        [RuName("Все")]
        All,

        /// <summary>Только понедельники.</summary>
        [RuName("Только понедельник")]
        MondayOnly,

        /// <summary>Только вторники.</summary>
        [RuName("Только вторник")]
        TuesdayOnly,

        /// <summary>Только среды.</summary>
        [RuName("Только среда")]
        WednesdayOnly,

        /// <summary>Только четверги.</summary>
        [RuName("Только четверг")]
        ThursdayOnly,

        /// <summary>Только пятницы.</summary>
        [RuName("Только пятница")]
        FridayOnly,

        /// <summary>Только понедельники и пятницы.</summary>
        [RuName("Понедельник и пятница")]
        MondayAndFriday,

        /// <summary>
        /// Пользовательская маска через Prop_DayOfWeekMask.
        /// Формат строки: номера дней через запятую, Пн=1 ... Вс=7.
        /// Например: "1,3,5" — понедельник, среда, пятница.
        /// </summary>
        [RuName("Своя маска")]
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
        [RuName("Месяц")]
        Month,

        /// <summary>
        /// Квартал (1–4). Используются параметры Prop_Quarter + Prop_Year.
        /// Q1 = янв–мар, Q2 = апр–июн, Q3 = июл–сен, Q4 = окт–дек.
        /// </summary>
        [RuName("Квартал")]
        Quarter,

        /// <summary>
        /// Полугодие. Prop_Quarter используется как номер полугодия (1 или 2).
        /// 1 = янв–июн, 2 = июл–дек.
        /// </summary>
        [RuName("Полугодие")]
        HalfYear,

        /// <summary>
        /// Весь год. Используется только Prop_Year.
        /// Например: последний рабочий день года.
        /// </summary>
        [RuName("Год")]
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
        [RuName("Понедельник")]
        Monday,

        /// <summary>Вторник.</summary>
        [RuName("Вторник")]
        Tuesday,

        /// <summary>Среда.</summary>
        [RuName("Среда")]
        Wednesday,

        /// <summary>Четверг.</summary>
        [RuName("Четверг")]
        Thursday,

        /// <summary>Пятница.</summary>
        [RuName("Пятница")]
        Friday,

        /// <summary>Суббота.</summary>
        [RuName("Суббота")]
        Saturday,

        /// <summary>Воскресенье.</summary>
        [RuName("Воскресенье")]
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
        [RuName("Следующее вхождение")]
        NextOccurrence,

        /// <summary>
        /// Следующее вхождение, которое является рабочим днём по производственному календарю.
        /// Пропускает выходные понедельники, праздничные пятницы и т.д.
        /// Продолжает поиск дальше пока не найдёт рабочее вхождение или не исчерпает MaxLookAhead.
        /// </summary>
        [RuName("Следующее рабочее вхождение")]
        NextWorkingOccurrence,

        /// <summary>
        /// Находит следующее вхождение дня недели.
        /// Если оно оказывается выходным — сдвигает на ближайший рабочий день
        /// согласно Prop_IfWeekend (NextWorkday или PrevWorkday).
        /// Гарантирует что результат всегда рабочий день.
        /// </summary>
        [RuName("Ближайший рабочий день")]
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
        [RuName("Следующий рабочий день")]
        NextWorkday,

        /// <summary>
        /// Сдвинуть на предыдущий рабочий день.
        /// Стандарт для платёжных дедлайнов — нельзя платить позже нормы.
        /// </summary>
        [RuName("Предыдущий рабочий день")]
        PrevWorkday,

        /// <summary>
        /// Не сдвигать — вернуть дату как есть, даже если выходной.
        /// Используется в режиме NextOccurrence когда тип дня не важен.
        /// </summary>
        [RuName("Без сдвига")]
        AsIs
    }
}
