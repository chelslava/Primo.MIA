// =============================================================================
// CalendarWorkdaysListBack.cs — активность «Календарь: Рабочие дни списком».
//
// Возвращает List<DateTime> всех рабочих дней в заданном диапазоне дат.
//
// Возможности:
//   - Фильтр по типу дня: включать или исключать сокращённые рабочие дни
//   - Фильтр по дню недели: All / MondayOnly / FridayOnly / CustomMask и т.д.
//   - Пользовательская маска дней: "1,3,5" (Пн=1 .. Вс=7)
//   - Выходные параметры: список, количество, первая и последняя дата
//
// Требует предварительно загруженный календарь от активности «Календарь: Загрузить».
// Диапазон дат должен находиться внутри года загруженного календаря.
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
    /// Активность «Календарь: Рабочие дни списком».
    /// Возвращает List&lt;DateTime&gt; всех рабочих дней в диапазоне с опциональной фильтрацией.
    /// </summary>
    public class CalendarWorkdaysListBack : PrimoComponentTO<CalendarWorkdaysList>
    {
        // =====================================================================
        // Статический словарь: DayOfWeekFilter → набор DayOfWeek для проверки
        // Используется в MatchesDayOfWeekFilter вместо switch-case
        // =====================================================================

        /// <summary>
        /// Маппинг предустановленных фильтров на наборы дней недели.
        /// Ключ — фильтр, значение — HashSet для O(1) Contains.
        /// </summary>
        private static readonly Dictionary<DayOfWeekFilter, HashSet<DayOfWeek>> FilterMap =
            new Dictionary<DayOfWeekFilter, HashSet<DayOfWeek>>
            {
                {
                    DayOfWeekFilter.MondayOnly,
                    new HashSet<DayOfWeek> { DayOfWeek.Monday }
                },
                {
                    DayOfWeekFilter.TuesdayOnly,
                    new HashSet<DayOfWeek> { DayOfWeek.Tuesday }
                },
                {
                    DayOfWeekFilter.WednesdayOnly,
                    new HashSet<DayOfWeek> { DayOfWeek.Wednesday }
                },
                {
                    DayOfWeekFilter.ThursdayOnly,
                    new HashSet<DayOfWeek> { DayOfWeek.Thursday }
                },
                {
                    DayOfWeekFilter.FridayOnly,
                    new HashSet<DayOfWeek> { DayOfWeek.Friday }
                },
                {
                    DayOfWeekFilter.MondayAndFriday,
                    new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Friday }
                }
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

        #region Prop_StartDate

        private string _propStartDate;

        /// <summary>Начало диапазона дат включительно.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DateTime))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_StartDate)]
        public string Prop_StartDate
        {
            get => _propStartDate;
            set { _propStartDate = value; InvokePropertyChanged(this, nameof(Prop_StartDate)); }
        }

        #endregion

        #region Prop_EndDate

        private string _propEndDate;

        /// <summary>Конец диапазона дат включительно.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DateTime))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_EndDate)]
        public string Prop_EndDate
        {
            get => _propEndDate;
            set { _propEndDate = value; InvokePropertyChanged(this, nameof(Prop_EndDate)); }
        }

        #endregion

        #region Prop_IncludeShortDays

        private bool _propIncludeShortDays = true;

        /// <summary>
        /// Включать сокращённые рабочие дни в результат (по умолчанию true).
        /// Сокращённые дни — например, 31 декабря (7 часов вместо 8).
        /// Если false — такие дни исключаются из списка.
        /// </summary>
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
        /// Фильтр по дню недели.
        /// All — все рабочие дни без фильтра.
        /// MondayOnly, FridayOnly и т.д. — только указанный день.
        /// CustomMask — маска из Prop_DayOfWeekMask (например "1,3,5").
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
        /// Пользовательская маска дней недели для режима CustomMask.
        /// Формат: номера дней через запятую, где Пн=1, Вт=2, Ср=3, Чт=4, Пт=5, Сб=6, Вс=7.
        /// Например: "1,3,5" — понедельник, среда, пятница.
        /// Игнорируется если Prop_DayOfWeekFilter != CustomMask.
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

        #region Prop_ResultList (Выходной)

        private string _propResultList;

        /// <summary>
        /// Имя переменной скрипта для записи List&lt;DateTime&gt; рабочих дней диапазона.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<DateTime>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_WorkdaysList)]
        public string Prop_ResultList
        {
            get => _propResultList;
            set { _propResultList = value; InvokePropertyChanged(this, nameof(Prop_ResultList)); }
        }

        #endregion

        #region Prop_Count (Выходной)

        private string _propCount;

        /// <summary>Имя переменной скрипта для записи количества найденных рабочих дней (int).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_WorkdaysCount)]
        public string Prop_Count
        {
            get => _propCount;
            set { _propCount = value; InvokePropertyChanged(this, nameof(Prop_Count)); }
        }

        #endregion

        #region Prop_FirstDate (Выходной)

        private string _propFirstDate;

        /// <summary>
        /// Имя переменной скрипта для записи первого рабочего дня в диапазоне (DateTime).
        /// DateTime.MinValue если рабочих дней не найдено.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DateTime))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_FirstWorkday)]
        public string Prop_FirstDate
        {
            get => _propFirstDate;
            set { _propFirstDate = value; InvokePropertyChanged(this, nameof(Prop_FirstDate)); }
        }

        #endregion

        #region Prop_LastDate (Выходной)

        private string _propLastDate;

        /// <summary>
        /// Имя переменной скрипта для записи последнего рабочего дня в диапазоне (DateTime).
        /// DateTime.MinValue если рабочих дней не найдено.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DateTime))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LastWorkday)]
        public string Prop_LastDate
        {
            get => _propLastDate;
            set { _propLastDate = value; InvokePropertyChanged(this, nameof(Prop_LastDate)); }
        }

        #endregion

        // =====================================================================
        // Служебные свойства
        // =====================================================================

        /// <summary>Категория группы активности в дизайнере Primo.</summary>
        public override string GroupName
        {
            get => ActivityCategories.Calendar;
            protected set { }
        }

        /// <summary>Максимальное время выполнения (мс).</summary>
        protected override int sdkTimeOut
        {
            get => 30000;
            set { }
        }

        // =====================================================================
        // Конструктор
        // =====================================================================

        /// <summary>Конструктор активности «Календарь: Рабочие дни списком».</summary>
        public CalendarWorkdaysListBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_CalendarWorkdaysList;
            sdkComponentHelp =
                "Возвращает список рабочих дней в заданном диапазоне дат.\n" +
                "\n" +
                "── Параметры ──────────────────────────────────\n" +
                "ID календаря — GUID из активности «Календарь: Загрузить»\n" +
                "Начальная дата — начало диапазона (включительно)\n" +
                "Конечная дата — конец диапазона (включительно)\n" +
                "Включать сокращённые дни — добавлять ли в список дни типа ShortWorkday\n" +
                "Фильтр дней недели — ограничить выборку конкретными днями (All / MondayOnly / ...)\n" +
                "Маска дней недели — для CustomMask: \"1,3,5\" (Пн=1..Вс=7)\n" +
                "\n" +
                "── Выходные данные ────────────────────────────\n" +
                "Список рабочих дней — List<DateTime> всех рабочих дней диапазона\n" +
                "Количество — int число найденных дней\n" +
                "Первый рабочий день — DateTime первого дня (MinValue если список пустой)\n" +
                "Последний рабочий день — DateTime последнего дня (MinValue если список пустой)\n" +
                "\n" +
                "── Ограничения ────────────────────────────────\n" +
                "Диапазон должен находиться внутри года загруженного календаря.\n" +
                "Для многолетних диапазонов загрузите каждый год отдельно и объедините списки.";

            sdkComponentIcon = ActivityIcons.Calendar;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Основные параметры
                PropertyBuilder.Script<string>("Prop_CalendarId",         ActivityStrings.Field_CalendarID),
                PropertyBuilder.Script<DateTime>("Prop_StartDate",        ActivityStrings.Field_StartDate),
                PropertyBuilder.Script<DateTime>("Prop_EndDate",          ActivityStrings.Field_EndDate),
                // Настройки фильтрации
                PropertyBuilder.BooleanObject("Prop_IncludeShortDays",    ActivityStrings.Field_IncludeShortDays),
                PropertyBuilder.Enum<DayOfWeekFilter>("Prop_DayOfWeekFilter", ActivityStrings.Field_DayOfWeekFilter),
                PropertyBuilder.Script<string>("Prop_DayOfWeekMask",      ActivityStrings.Field_DayOfWeekMask),
                // Выходные параметры
                PropertyBuilder.Variable<List<DateTime>>("Prop_ResultList", ActivityStrings.Field_WorkdaysList),
                PropertyBuilder.Variable<int>("Prop_Count",               ActivityStrings.Field_WorkdaysCount),
                PropertyBuilder.Variable<DateTime>("Prop_FirstDate",      ActivityStrings.Field_FirstWorkday),
                PropertyBuilder.Variable<DateTime>("Prop_LastDate",       ActivityStrings.Field_LastWorkday)
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_CalendarId        = string.Empty;
            this.Prop_IncludeShortDays  = true;
            this.Prop_DayOfWeekFilter   = DayOfWeekFilter.All;
            this.Prop_DayOfWeekMask     = string.Empty;
        }

        // =====================================================================
        // Выполнение
        // =====================================================================

        /// <summary>Точка входа выполнения активности.</summary>
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

                // Разбираем начальную дату
                object startObj = GetPropertyValue(this.Prop_StartDate, nameof(Prop_StartDate), sd);
                if (!DateTime.TryParse(startObj?.ToString(), out DateTime startDate))
                    return Fail(ActivityStrings.Error_InvalidStartDate);

                // Разбираем конечную дату
                object endObj = GetPropertyValue(this.Prop_EndDate, nameof(Prop_EndDate), sd);
                if (!DateTime.TryParse(endObj?.ToString(), out DateTime endDate))
                    return Fail(ActivityStrings.Error_InvalidEndDate);

                // Нормализуем порядок — допускаем ввод "наоборот"
                if (startDate > endDate)
                {
                    DateTime tmp = startDate;
                    startDate    = endDate;
                    endDate      = tmp;
                }

                // Проверяем что весь диапазон внутри года календаря
                if (startDate.Year != calendar.Year || endDate.Year != calendar.Year)
                    return Fail(
                        $"Диапазон {startDate:dd.MM.yyyy}–{endDate:dd.MM.yyyy} выходит за пределы " +
                        $"загруженного календаря ({calendar.Year}). " +
                        "Загрузите все нужные годы и используйте активность для каждого года отдельно, " +
                        "затем объедините списки.");

                // Разбираем маску дней недели если нужна
                HashSet<DayOfWeek> customMask = null;
                if (this.Prop_DayOfWeekFilter == DayOfWeekFilter.CustomMask)
                {
                    string maskStr = GetPropertyValue<string>(
                        this.Prop_DayOfWeekMask, nameof(Prop_DayOfWeekMask), sd);

                    if (!TryParseDayOfWeekMask(maskStr, out customMask))
                        return Fail(ActivityStrings.Error_InvalidDayOfWeekMask);
                }

                // ── Строим список рабочих дней через LINQ ──────────────────

                // Флаги фильтрации — кэшируем чтобы не читать свойства в каждой итерации
                bool           includeShort = this.Prop_IncludeShortDays;
                DayOfWeekFilter dowFilter   = this.Prop_DayOfWeekFilter;

                List<DateTime> workdays = Enumerable
                    // Генерируем все даты диапазона как смещения от startDate
                    .Range(0, (endDate - startDate).Days + 1)
                    .Select(offset => startDate.AddDays(offset))
                    // Фильтр 1: тип дня по производственному календарю
                    .Where(date =>
                    {
                        DayType type = calendar.GetDayType(date);
                        if (type == DayType.Workday)      return true;
                        if (type == DayType.ShortWorkday) return includeShort;
                        // Weekend и Holiday не включаем
                        return false;
                    })
                    // Фильтр 2: день недели
                    .Where(date => MatchesDayOfWeekFilter(date, dowFilter, customMask))
                    .ToList();

                // ── Записываем выходные параметры ──────────────────────────

                SetVariableValue(this.Prop_ResultList, workdays,       sd);
                SetVariableValue(this.Prop_Count,      workdays.Count, sd);

                // Первый и последний день — MinValue если список пустой
                SetVariableValue(this.Prop_FirstDate,
                    workdays.Count > 0 ? workdays.First() : DateTime.MinValue, sd);
                SetVariableValue(this.Prop_LastDate,
                    workdays.Count > 0 ? workdays.Last()  : DateTime.MinValue, sd);

                // ── Формируем сообщение об успехе ──────────────────────────

                string filterMsg = BuildFilterDescription(dowFilter, includeShort);
                string rangeMsg  = $"{startDate:dd.MM.yyyy}–{endDate:dd.MM.yyyy}";

                return new ExecutionResult
                {
                    IsSuccess      = true,
                    SuccessMessage = workdays.Count > 0
                        ? $"Найдено {workdays.Count} рабочих дней в диапазоне {rangeMsg}{filterMsg}. " +
                          $"Первый: {workdays.First():dd.MM.yyyy}, последний: {workdays.Last():dd.MM.yyyy}"
                        : $"Рабочих дней в диапазоне {rangeMsg}{filterMsg} не найдено"
                };
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка построения списка рабочих дней: {ex.Message}");
            }
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>
        /// Проверяет соответствие даты фильтру по дню недели.
        /// Для All — всегда true.
        /// Для предустановленных фильтров — O(1) поиск в HashSet через FilterMap.
        /// Для CustomMask — O(1) поиск в переданном customMask.
        /// </summary>
        /// <param name="date">Проверяемая дата.</param>
        /// <param name="filter">Выбранный фильтр.</param>
        /// <param name="customMask">Набор дней для CustomMask (null для остальных).</param>
        private static bool MatchesDayOfWeekFilter(
            DateTime date, DayOfWeekFilter filter, HashSet<DayOfWeek> customMask)
        {
            // All — фильтр не применяется
            if (filter == DayOfWeekFilter.All)
                return true;

            // CustomMask — используем переданный набор
            if (filter == DayOfWeekFilter.CustomMask)
                return customMask != null && customMask.Contains(date.DayOfWeek);

            // Предустановленный фильтр — ищем в статическом словаре
            return FilterMap.TryGetValue(filter, out HashSet<DayOfWeek> days)
                && days.Contains(date.DayOfWeek);
        }

        /// <summary>
        /// Разбирает маску дней недели из строки "1,3,5" в HashSet&lt;DayOfWeek&gt;.
        /// Пн=1, Вт=2, Ср=3, Чт=4, Пт=5, Сб=6, Вс=7.
        /// Возвращает false если строка некорректна или пуста.
        /// </summary>
        private static bool TryParseDayOfWeekMask(string mask, out HashSet<DayOfWeek> result)
        {
            result = new HashSet<DayOfWeek>();

            if (string.IsNullOrWhiteSpace(mask))
                return false;

            // Словарь: номер дня (1–7) → DayOfWeek
            // Используем русскую нумерацию: Пн=1 (ISO 8601)
            var dayMap = new Dictionary<int, DayOfWeek>
            {
                { 1, DayOfWeek.Monday    },
                { 2, DayOfWeek.Tuesday   },
                { 3, DayOfWeek.Wednesday },
                { 4, DayOfWeek.Thursday  },
                { 5, DayOfWeek.Friday    },
                { 6, DayOfWeek.Saturday  },
                { 7, DayOfWeek.Sunday    }
            };

            // [FIX CS1628] out-параметр нельзя захватывать в лямбде — используем foreach.
            // Разбиваем строку, парсим каждый токен и добавляем в result напрямую.
            string[] tokens = mask.Split(
                new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string token in tokens)
            {
                string s = token.Trim();
                if (string.IsNullOrEmpty(s))
                    continue;

                // Токен должен быть целым числом 1–7
                if (!int.TryParse(s, out int num))
                    return false;

                // Число должно соответствовать известному дню недели
                if (!dayMap.TryGetValue(num, out DayOfWeek dow))
                    return false;

                result.Add(dow);
            }

            return result.Count > 0;
        }

        /// <summary>
        /// Формирует описание активных фильтров для сообщения об успехе.
        /// </summary>
        private static string BuildFilterDescription(DayOfWeekFilter filter, bool includeShort)
        {
            var parts = new List<string>();

            if (filter != DayOfWeekFilter.All)
                parts.Add($"фильтр: {filter}");

            if (!includeShort)
                parts.Add("без сокращённых дней");

            return parts.Count > 0 ? $" ({string.Join(", ", parts)})" : string.Empty;
        }

        private static ExecutionResult Fail(string msg) =>
            new ExecutionResult { IsSuccess = false, ErrorMessage = msg };

        // =====================================================================
        // Валидация
        // =====================================================================

        /// <summary>Валидация параметров активности на стороне дизайнера.</summary>
        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_CalendarId))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_CalendarId),
                    Error        = ActivityStrings.Error_CalendarIdRequired
                });

            if (string.IsNullOrWhiteSpace(this.Prop_StartDate))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_StartDate),
                    Error        = ActivityStrings.Error_StartDateRequired
                });

            if (string.IsNullOrWhiteSpace(this.Prop_EndDate))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_EndDate),
                    Error        = ActivityStrings.Error_EndDateRequired
                });

            // Маска нужна только в режиме CustomMask
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
}
