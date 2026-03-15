// =============================================================================
// CalendarNthWorkdayBack.cs — активность «Календарь: Nth рабочий день».
//
// Возвращает дату N-го рабочего дня в заданном периоде (месяц, квартал,
// полугодие или год). Поддерживает отсчёт с конца (N < 0) и фильтрацию
// по дням недели.
//
// Примеры:
//   N=1,  Period=Month,   Month=1  → первый  рабочий день января
//   N=-1, Period=Month,   Month=1  → последний рабочий день января
//   N=3,  Period=Quarter, Quarter=1 → 3-й рабочий день Q1
//   N=1,  Period=Month,   DayOfWeek=MondayOnly → первый рабочий понедельник
//
// Требует предварительно загруженный календарь от «Календарь: Загрузить».
// Период должен находиться внутри года загруженного календаря.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Calendar.Models;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Календарь: Nth рабочий день».
    /// Ищет N-й рабочий день в периоде с опциональным фильтром по дню недели.
    /// </summary>
    public class CalendarNthWorkdayBack : PrimoComponentTO<CalendarNthWorkday>
    {
        // =====================================================================
        // Статические словари маппинга — переиспользуем паттерн из CalendarWorkdaysList
        // =====================================================================

        /// <summary>
        /// Маппинг предустановленных фильтров дней недели на HashSet&lt;DayOfWeek&gt;.
        /// O(1) поиск в MatchesDayOfWeekFilter.
        /// </summary>
        private static readonly Dictionary<DayOfWeekFilter, HashSet<DayOfWeek>> FilterMap =
            new Dictionary<DayOfWeekFilter, HashSet<DayOfWeek>>
            {
                { DayOfWeekFilter.MondayOnly,     new HashSet<DayOfWeek> { DayOfWeek.Monday } },
                { DayOfWeekFilter.TuesdayOnly,    new HashSet<DayOfWeek> { DayOfWeek.Tuesday } },
                { DayOfWeekFilter.WednesdayOnly,  new HashSet<DayOfWeek> { DayOfWeek.Wednesday } },
                { DayOfWeekFilter.ThursdayOnly,   new HashSet<DayOfWeek> { DayOfWeek.Thursday } },
                { DayOfWeekFilter.FridayOnly,     new HashSet<DayOfWeek> { DayOfWeek.Friday } },
                { DayOfWeekFilter.MondayAndFriday,new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Friday } }
            };

        /// <summary>
        /// Словарь: номер дня по ISO 8601 (Пн=1..Вс=7) → DayOfWeek.
        /// Используется в TryParseDayOfWeekMask.
        /// </summary>
        private static readonly Dictionary<int, DayOfWeek> IsoToDayOfWeek =
            new Dictionary<int, DayOfWeek>
            {
                { 1, DayOfWeek.Monday    },
                { 2, DayOfWeek.Tuesday   },
                { 3, DayOfWeek.Wednesday },
                { 4, DayOfWeek.Thursday  },
                { 5, DayOfWeek.Friday    },
                { 6, DayOfWeek.Saturday  },
                { 7, DayOfWeek.Sunday    }
            };

        // =====================================================================
        // Свойства
        // =====================================================================

        #region Prop_CalendarId
        private string _propCalendarId;
        /// <summary>ID календаря (GUID) от активности «Календарь: Загрузить».</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CalendarID)]
        public string Prop_CalendarId
        {
            get => _propCalendarId;
            set { _propCalendarId = value; InvokePropertyChanged(this, nameof(Prop_CalendarId)); }
        }
        #endregion

        #region Prop_N
        private string _propN;
        /// <summary>
        /// Номер рабочего дня в периоде.
        /// Положительный — отсчёт с начала (1 = первый рабочий день).
        /// Отрицательный — отсчёт с конца (-1 = последний, -2 = предпоследний).
        /// Ноль недопустим.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_NthNumber)]
        public string Prop_N
        {
            get => _propN;
            set { _propN = value; InvokePropertyChanged(this, nameof(Prop_N)); }
        }
        #endregion

        #region Prop_Period
        private NthWorkdayPeriod _propPeriod = NthWorkdayPeriod.Month;
        /// <summary>Тип периода: Month, Quarter, HalfYear или Year.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_NthPeriod)]
        public NthWorkdayPeriod Prop_Period
        {
            get => _propPeriod;
            set { _propPeriod = value; InvokePropertyChanged(this, nameof(Prop_Period)); }
        }
        #endregion

        #region Prop_Month
        private string _propMonth;
        /// <summary>
        /// Месяц (1–12). Используется при Period = Month.
        /// Если пусто — берётся текущий месяц.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_NthMonth)]
        public string Prop_Month
        {
            get => _propMonth;
            set { _propMonth = value; InvokePropertyChanged(this, nameof(Prop_Month)); }
        }
        #endregion

        #region Prop_Quarter
        private string _propQuarter;
        /// <summary>
        /// Квартал (1–4) при Period = Quarter.
        /// Номер полугодия (1–2) при Period = HalfYear.
        /// Если пусто — определяется из текущей даты.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_NthQuarter)]
        public string Prop_Quarter
        {
            get => _propQuarter;
            set { _propQuarter = value; InvokePropertyChanged(this, nameof(Prop_Quarter)); }
        }
        #endregion

        #region Prop_Year
        private string _propYear;
        /// <summary>
        /// Год периода. Если пусто — берётся из загруженного календаря.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CalendarYear)]
        public string Prop_Year
        {
            get => _propYear;
            set { _propYear = value; InvokePropertyChanged(this, nameof(Prop_Year)); }
        }
        #endregion

        #region Prop_IncludeShortDays
        private bool _propIncludeShortDays = true;
        /// <summary>Включать сокращённые рабочие дни (ShortWorkday) в отсчёт.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_IncludeShortDays)]
        public bool Prop_IncludeShortDays
        {
            get => _propIncludeShortDays;
            set { _propIncludeShortDays = value; InvokePropertyChanged(this, nameof(Prop_IncludeShortDays)); }
        }
        #endregion

        #region Prop_DayOfWeekFilter
        private DayOfWeekFilter _propDayOfWeekFilter = DayOfWeekFilter.All;
        /// <summary>
        /// Опциональный фильтр по дню недели.
        /// Например, MondayOnly — искать только среди рабочих понедельников.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_DayOfWeekFilter)]
        public DayOfWeekFilter Prop_DayOfWeekFilter
        {
            get => _propDayOfWeekFilter;
            set { _propDayOfWeekFilter = value; InvokePropertyChanged(this, nameof(Prop_DayOfWeekFilter)); }
        }
        #endregion

        #region Prop_DayOfWeekMask
        private string _propDayOfWeekMask;
        /// <summary>
        /// Пользовательская маска для CustomMask: "1,3,5" (Пн=1..Вс=7).
        /// Игнорируется если DayOfWeekFilter != CustomMask.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_DayOfWeekMask)]
        public string Prop_DayOfWeekMask
        {
            get => _propDayOfWeekMask;
            set { _propDayOfWeekMask = value; InvokePropertyChanged(this, nameof(Prop_DayOfWeekMask)); }
        }
        #endregion

        #region Prop_ResultDate (Выходной)
        private string _propResultDate;
        /// <summary>
        /// Имя переменной скрипта для записи найденной даты (DateTime).
        /// DateTime.MinValue если IsFound = false.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DateTime))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ResultDate)]
        public string Prop_ResultDate
        {
            get => _propResultDate;
            set { _propResultDate = value; InvokePropertyChanged(this, nameof(Prop_ResultDate)); }
        }
        #endregion

        #region Prop_TotalWorkdays (Выходной)
        private string _propTotalWorkdays;
        /// <summary>Имя переменной скрипта для записи общего числа рабочих дней в периоде (int).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_TotalWorkdays)]
        public string Prop_TotalWorkdays
        {
            get => _propTotalWorkdays;
            set { _propTotalWorkdays = value; InvokePropertyChanged(this, nameof(Prop_TotalWorkdays)); }
        }
        #endregion

        #region Prop_IsFound (Выходной)
        private string _propIsFound;
        /// <summary>
        /// Имя переменной скрипта для записи флага успеха (bool).
        /// false если N выходит за пределы числа рабочих дней в периоде.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_NthIsFound)]
        public string Prop_IsFound
        {
            get => _propIsFound;
            set { _propIsFound = value; InvokePropertyChanged(this, nameof(Prop_IsFound)); }
        }
        #endregion

        // =====================================================================
        // Служебные свойства
        // =====================================================================

        public override string GroupName
        {
            get => ActivityCategories.Calendar;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 30000;
            set { }
        }

        // =====================================================================
        // Конструктор
        // =====================================================================

        public CalendarNthWorkdayBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_CalendarNthWorkday;
            sdkComponentHelp =
                "Возвращает дату N-го рабочего дня в заданном периоде.\n" +
                "\n" +
                "── Параметры ──────────────────────────────────\n" +
                "ID календаря — GUID из активности «Календарь: Загрузить»\n" +
                "N — номер рабочего дня (1=первый, -1=последний, -2=предпоследний)\n" +
                "Период — Month / Quarter / HalfYear / Year\n" +
                "Месяц — номер месяца 1–12 (для Period=Month)\n" +
                "Квартал / Полугодие — номер 1–4 / 1–2 (для Period=Quarter или HalfYear)\n" +
                "Год — год периода (пусто = год из календаря)\n" +
                "Включать сокращённые дни — учитывать ShortWorkday в отсчёте\n" +
                "Фильтр дней недели — искать только среди указанных дней (напр. MondayOnly)\n" +
                "Маска дней — для CustomMask: \"1,3,5\" (Пн=1..Вс=7)\n" +
                "\n" +
                "── Выходные данные ────────────────────────────\n" +
                "Результат (дата) — найденная дата (MinValue если IsFound=false)\n" +
                "Рабочих дней в периоде — общее число рабочих дней (с учётом фильтра)\n" +
                "Найден — false если N выходит за пределы периода\n" +
                "\n" +
                "── Примеры ────────────────────────────────────\n" +
                "N=1,  Month=1    → первый  рабочий день января\n" +
                "N=-1, Month=12   → последний рабочий день декабря\n" +
                "N=3,  Quarter=1  → 3-й рабочий день I квартала\n" +
                "N=1,  MondayOnly → первый рабочий понедельник периода";

            sdkComponentIcon = ActivityIcons.Calendar;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Основные
                PropertyBuilder.Script<string>("Prop_CalendarId",          ActivityStrings.Field_CalendarID),
                PropertyBuilder.Script<int>("Prop_N",                      ActivityStrings.Field_NthNumber),
                PropertyBuilder.Enum<NthWorkdayPeriod>("Prop_Period",      ActivityStrings.Field_NthPeriod),
                // Параметры периода
                PropertyBuilder.Script<int>("Prop_Month",                  ActivityStrings.Field_NthMonth),
                PropertyBuilder.Script<int>("Prop_Quarter",                ActivityStrings.Field_NthQuarter),
                PropertyBuilder.Script<int>("Prop_Year",                   ActivityStrings.Field_CalendarYear),
                // Настройки
                PropertyBuilder.BooleanObject("Prop_IncludeShortDays",     ActivityStrings.Field_IncludeShortDays),
                PropertyBuilder.Enum<DayOfWeekFilter>("Prop_DayOfWeekFilter", ActivityStrings.Field_DayOfWeekFilter),
                PropertyBuilder.Script<string>("Prop_DayOfWeekMask",       ActivityStrings.Field_DayOfWeekMask),
                // Выходные
                PropertyBuilder.Variable<DateTime>("Prop_ResultDate",      ActivityStrings.Field_ResultDate),
                PropertyBuilder.Variable<int>("Prop_TotalWorkdays",        ActivityStrings.Field_TotalWorkdays),
                PropertyBuilder.Variable<bool>("Prop_IsFound",             ActivityStrings.Field_NthIsFound)
            };

            InitClass(container);

            this.Prop_CalendarId      = string.Empty;
            this.Prop_N               = "1";
            this.Prop_Period          = NthWorkdayPeriod.Month;
            this.Prop_Month           = DateTime.Now.Month.ToString();
            this.Prop_Quarter         = ((DateTime.Now.Month - 1) / 3 + 1).ToString();
            this.Prop_Year            = string.Empty; // пусто = берётся из календаря
            this.Prop_IncludeShortDays = true;
            this.Prop_DayOfWeekFilter  = DayOfWeekFilter.All;
            this.Prop_DayOfWeekMask    = string.Empty;
        }

        // =====================================================================
        // Выполнение
        // =====================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Получаем и проверяем входные параметры ─────────────────

                string calendarGuid = GetPropertyValue<string>(
                    this.Prop_CalendarId, nameof(Prop_CalendarId), sd);

                var calendar = RepoDict.Get<ProductionCalendar>(calendarGuid);
                if (calendar == null)
                    return Fail(
                        $"Календарь не найден в кэше по GUID: {calendarGuid}. " +
                        "Сначала загрузите календарь активностью «Календарь: Загрузить».");

                // Разбираем N — ноль недопустим
                object nObj = GetPropertyValue(this.Prop_N, nameof(Prop_N), sd);
                if (!int.TryParse(nObj?.ToString(), out int n) || n == 0)
                    return Fail(ActivityStrings.Error_NthNRequired);

                // Год периода: из свойства или из календаря как fallback
                int year = calendar.Year;
                if (!string.IsNullOrWhiteSpace(this.Prop_Year))
                {
                    object yearObj = GetPropertyValue(this.Prop_Year, nameof(Prop_Year), sd);
                    if (int.TryParse(yearObj?.ToString(), out int parsedYear))
                        year = parsedYear;
                }

                // Разбираем месяц и квартал (оба опциональны)
                int month   = ResolveOptionalInt(sd, this.Prop_Month,   nameof(Prop_Month),   DateTime.Now.Month);
                int quarter = ResolveOptionalInt(sd, this.Prop_Quarter, nameof(Prop_Quarter),
                    (DateTime.Now.Month - 1) / 3 + 1);

                // Проверяем диапазоны
                if (this.Prop_Period == NthWorkdayPeriod.Month && (month < 1 || month > 12))
                    return Fail("Месяц должен быть от 1 до 12.");

                if (this.Prop_Period == NthWorkdayPeriod.Quarter && (quarter < 1 || quarter > 4))
                    return Fail("Квартал должен быть от 1 до 4.");

                if (this.Prop_Period == NthWorkdayPeriod.HalfYear && (quarter < 1 || quarter > 2))
                    return Fail("Номер полугодия должен быть 1 или 2.");

                // Проверяем что период внутри года календаря
                if (year != calendar.Year)
                    return Fail(
                        $"Год {year} не соответствует загруженному календарю ({calendar.Year}). " +
                        "Загрузите нужный год активностью «Календарь: Загрузить».");

                // Разбираем маску дней недели для CustomMask
                HashSet<DayOfWeek> customMask = null;
                if (this.Prop_DayOfWeekFilter == DayOfWeekFilter.CustomMask)
                {
                    string maskStr = GetPropertyValue<string>(
                        this.Prop_DayOfWeekMask, nameof(Prop_DayOfWeekMask), sd);
                    if (!TryParseDayOfWeekMask(maskStr, out customMask))
                        return Fail(ActivityStrings.Error_InvalidDayOfWeekMask);
                }

                // ── Вычисляем границы периода ───────────────────────────────

                (DateTime periodStart, DateTime periodEnd) =
                    GetPeriodBounds(this.Prop_Period, month, quarter, year);

                // ── Строим список рабочих дней через LINQ ───────────────────

                bool           includeShort = this.Prop_IncludeShortDays;
                DayOfWeekFilter dowFilter   = this.Prop_DayOfWeekFilter;

                List<DateTime> workdays = Enumerable
                    .Range(0, (periodEnd - periodStart).Days + 1)
                    .Select(offset => periodStart.AddDays(offset))
                    // Фильтр 1: тип дня по производственному календарю
                    .Where(date =>
                    {
                        DayType type = calendar.GetDayType(date);
                        if (type == DayType.Workday)      return true;
                        if (type == DayType.ShortWorkday) return includeShort;
                        return false;
                    })
                    // Фильтр 2: день недели
                    .Where(date => MatchesDayOfWeekFilter(date, dowFilter, customMask))
                    .ToList();

                // Записываем общее число рабочих дней периода
                SetVariableValue(this.Prop_TotalWorkdays, workdays.Count, sd);

                // ── Выбираем N-й элемент ────────────────────────────────────

                // Положительный N: отсчёт с начала, 1-based → index = N - 1
                // Отрицательный N: отсчёт с конца, -1=последний → index = Count + N
                int index = n > 0
                    ? n - 1
                    : workdays.Count + n;

                // N выходит за пределы — возвращаем IsFound = false (не ошибка)
                if (index < 0 || index >= workdays.Count)
                {
                    SetVariableValue(this.Prop_IsFound,    false,          sd);
                    SetVariableValue(this.Prop_ResultDate, DateTime.MinValue, sd);

                    string direction = n > 0 ? $"{n}-й" : $"{Math.Abs(n)}-й с конца";
                    return new ExecutionResult
                    {
                        IsSuccess      = true,
                        SuccessMessage = $"В периоде {GetPeriodDescription(this.Prop_Period, month, quarter, year)} " +
                                         $"всего {workdays.Count} рабочих дней — {direction} не существует."
                    };
                }

                DateTime result = workdays[index];

                // Записываем результат
                SetVariableValue(this.Prop_ResultDate, result, sd);
                SetVariableValue(this.Prop_IsFound,    true,   sd);

                // ── Формируем сообщение об успехе ───────────────────────────

                string periodDesc  = GetPeriodDescription(this.Prop_Period, month, quarter, year);
                string nDesc       = n > 0 ? $"{n}-й" : $"{Math.Abs(n)}-й с конца";
                string dowDesc     = dowFilter != DayOfWeekFilter.All ? $" ({dowFilter})" : string.Empty;

                return new ExecutionResult
                {
                    IsSuccess      = true,
                    SuccessMessage = $"{nDesc} рабочий день{dowDesc} {periodDesc}: {result:dd.MM.yyyy} " +
                                     $"({result.DayOfWeek.ToRussianName()})"
                };
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка поиска N-го рабочего дня: {ex.Message}");
            }
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>
        /// Вычисляет границы периода (начало и конец включительно).
        /// </summary>
        /// <param name="period">Тип периода.</param>
        /// <param name="month">Месяц (для Month).</param>
        /// <param name="quarterOrHalf">Квартал (1–4) или полугодие (1–2).</param>
        /// <param name="year">Год.</param>
        private static (DateTime start, DateTime end) GetPeriodBounds(
            NthWorkdayPeriod period, int month, int quarterOrHalf, int year)
        {
            switch (period)
            {
                case NthWorkdayPeriod.Month:
                    return (
                        new DateTime(year, month, 1),
                        new DateTime(year, month, DateTime.DaysInMonth(year, month))
                    );

                case NthWorkdayPeriod.Quarter:
                    // Q1 → месяцы 1–3, Q2 → 4–6, Q3 → 7–9, Q4 → 10–12
                    int qStartMonth = (quarterOrHalf - 1) * 3 + 1;
                    int qEndMonth   = qStartMonth + 2;
                    return (
                        new DateTime(year, qStartMonth, 1),
                        new DateTime(year, qEndMonth, DateTime.DaysInMonth(year, qEndMonth))
                    );

                case NthWorkdayPeriod.HalfYear:
                    // 1 = янв–июн, 2 = июл–дек
                    return quarterOrHalf == 1
                        ? (new DateTime(year, 1, 1),  new DateTime(year, 6, 30))
                        : (new DateTime(year, 7, 1),  new DateTime(year, 12, 31));

                case NthWorkdayPeriod.Year:
                    return (new DateTime(year, 1, 1), new DateTime(year, 12, 31));

                default:
                    throw new ArgumentException($"Неизвестный тип периода: {period}");
            }
        }

        /// <summary>
        /// Формирует читаемое описание периода для сообщений.
        /// </summary>
        private static string GetPeriodDescription(
            NthWorkdayPeriod period, int month, int quarterOrHalf, int year)
        {
            // Словарь названий месяцев — родительный падеж
            var monthNames = new Dictionary<int, string>
            {
                { 1,"января" }, { 2,"февраля" }, { 3,"марта" },   { 4,"апреля" },
                { 5,"мая" },   { 6,"июня" },    { 7,"июля" },     { 8,"августа" },
                { 9,"сентября" },{ 10,"октября" },{ 11,"ноября" },{ 12,"декабря" }
            };

            switch (period)
            {
                case NthWorkdayPeriod.Month:
                    return $"{monthNames[month]} {year}";

                case NthWorkdayPeriod.Quarter:
                    return $"Q{quarterOrHalf} {year}";

                case NthWorkdayPeriod.HalfYear:
                    return quarterOrHalf == 1
                        ? $"I полугодия {year}"
                        : $"II полугодия {year}";

                case NthWorkdayPeriod.Year:
                    return $"{year} года";

                default:
                    return $"{period} {year}";
            }
        }

        /// <summary>
        /// Читает int-свойство из скрипта, возвращает fallback если свойство пусто.
        /// Используется для опциональных параметров Month, Quarter, Year.
        /// </summary>
        private int ResolveOptionalInt(
            ScriptingData sd, string propValue, string propName, int fallback)
        {
            if (string.IsNullOrWhiteSpace(propValue))
                return fallback;

            object obj = GetPropertyValue(propValue, propName, sd);
            return int.TryParse(obj?.ToString(), out int val) ? val : fallback;
        }

        /// <summary>
        /// Проверяет соответствие даты фильтру по дню недели.
        /// All — true. Предустановленные — O(1) через FilterMap. CustomMask — O(1) через customMask.
        /// </summary>
        private static bool MatchesDayOfWeekFilter(
            DateTime date, DayOfWeekFilter filter, HashSet<DayOfWeek> customMask)
        {
            if (filter == DayOfWeekFilter.All)
                return true;

            if (filter == DayOfWeekFilter.CustomMask)
                return customMask != null && customMask.Contains(date.DayOfWeek);

            return FilterMap.TryGetValue(filter, out HashSet<DayOfWeek> days)
                && days.Contains(date.DayOfWeek);
        }

        /// <summary>
        /// Разбирает маску дней недели "1,3,5" в HashSet&lt;DayOfWeek&gt;.
        /// Использует foreach вместо лямбды во избежание CS1628 (захват out-параметра).
        /// </summary>
        private static bool TryParseDayOfWeekMask(string mask, out HashSet<DayOfWeek> result)
        {
            result = new HashSet<DayOfWeek>();

            if (string.IsNullOrWhiteSpace(mask))
                return false;

            string[] tokens = mask.Split(
                new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string token in tokens)
            {
                string s = token.Trim();
                if (string.IsNullOrEmpty(s)) continue;

                if (!int.TryParse(s, out int num))                  return false;
                if (!IsoToDayOfWeek.TryGetValue(num, out DayOfWeek dow)) return false;

                result.Add(dow);
            }

            return result.Count > 0;
        }

        private static ExecutionResult Fail(string msg) =>
            new ExecutionResult { IsSuccess = false, ErrorMessage = msg };

        // =====================================================================
        // Валидация
        // =====================================================================

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_CalendarId))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_CalendarId),
                    Error        = ActivityStrings.Error_CalendarIdRequired
                });

            if (string.IsNullOrWhiteSpace(this.Prop_N))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_N),
                    Error        = ActivityStrings.Error_NthNRequired
                });

            // Месяц нужен только для Month
            if (this.Prop_Period == NthWorkdayPeriod.Month
                && string.IsNullOrWhiteSpace(this.Prop_Month))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_Month),
                    Error        = ActivityStrings.Error_NthMonthRequired
                });

            // Квартал или полугодие нужны для Quarter / HalfYear
            if ((this.Prop_Period == NthWorkdayPeriod.Quarter
                 || this.Prop_Period == NthWorkdayPeriod.HalfYear)
                && string.IsNullOrWhiteSpace(this.Prop_Quarter))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_Quarter),
                    Error        = ActivityStrings.Error_NthQuarterRequired
                });

            // Маска нужна только для CustomMask
            if (this.Prop_DayOfWeekFilter == DayOfWeekFilter.CustomMask
                && string.IsNullOrWhiteSpace(this.Prop_DayOfWeekMask))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_DayOfWeekMask),
                    Error        = ActivityStrings.Error_DayOfWeekMaskRequired
                });

            return ret;
        }
    }

    // =========================================================================
    // Extension-метод для русского названия дня недели в SuccessMessage
    // =========================================================================

    /// <summary>
    /// Расширения для DayOfWeek — локализованные названия на русском языке.
    /// </summary>
    internal static class DayOfWeekExtensions
    {
        private static readonly Dictionary<DayOfWeek, string> RussianNames =
            new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday,    "понедельник" },
                { DayOfWeek.Tuesday,   "вторник"     },
                { DayOfWeek.Wednesday, "среда"        },
                { DayOfWeek.Thursday,  "четверг"      },
                { DayOfWeek.Friday,    "пятница"      },
                { DayOfWeek.Saturday,  "суббота"      },
                { DayOfWeek.Sunday,    "воскресенье"  }
            };

        /// <summary>Возвращает название дня недели на русском языке.</summary>
        public static string ToRussianName(this DayOfWeek dow) =>
            RussianNames.TryGetValue(dow, out string name) ? name : dow.ToString();
    }
}
