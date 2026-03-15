// =============================================================================
// CalendarNextDayOfWeekBack.cs — активность «Календарь: Следующий день недели».
//
// Находит ближайшую дату указанного дня недели с учётом производственного
// календаря. Поддерживает три режима поиска:
//
//   NextOccurrence        — просто следующий понедельник/вторник/... без проверки
//   NextWorkingOccurrence — следующее вхождение, которое является рабочим днём
//   NearestWorkday        — следующее вхождение + коррекция если выходной
//
// Примеры:
//   Monday / NextWorkingOccurrence → первый рабочий понедельник
//   Friday / NearestWorkday / PrevWorkday → пятница, если праздник — четверг
//   Wednesday / NextOccurrence → просто следующая среда (без проверки)
//
// Требует предварительно загруженный календарь от «Календарь: Загрузить».
// =============================================================================

using System;
using System.Collections.Generic;
using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Calendar.Models;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Календарь: Следующий день недели».
    /// Ищет ближайшую дату указанного дня недели с учётом производственного календаря.
    /// </summary>
    public class CalendarNextDayOfWeekBack : PrimoComponentTO<CalendarNextDayOfWeek>
    {
        // =====================================================================
        // Статические словари маппинга
        // =====================================================================

        /// <summary>
        /// Маппинг TargetDayOfWeek → DayOfWeek.
        /// Используется в ConvertToDayOfWeek вместо switch-case.
        /// </summary>
        private static readonly Dictionary<TargetDayOfWeek, DayOfWeek> TargetToDayOfWeek =
            new Dictionary<TargetDayOfWeek, DayOfWeek>
            {
                { TargetDayOfWeek.Monday,    DayOfWeek.Monday    },
                { TargetDayOfWeek.Tuesday,   DayOfWeek.Tuesday   },
                { TargetDayOfWeek.Wednesday, DayOfWeek.Wednesday },
                { TargetDayOfWeek.Thursday,  DayOfWeek.Thursday  },
                { TargetDayOfWeek.Friday,    DayOfWeek.Friday    },
                { TargetDayOfWeek.Saturday,  DayOfWeek.Saturday  },
                { TargetDayOfWeek.Sunday,    DayOfWeek.Sunday    }
            };

        /// <summary>
        /// Названия дней недели на русском — именительный падеж.
        /// Используется в SuccessMessage.
        /// </summary>
        private static readonly Dictionary<DayOfWeek, string> RussianDayNames =
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
        /// <summary>Дата отсчёта поиска.</summary>
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

        #region Prop_TargetDayOfWeek
        private TargetDayOfWeek _propTargetDayOfWeek = TargetDayOfWeek.Monday;
        /// <summary>Искомый день недели.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_TargetDayOfWeek)]
        public TargetDayOfWeek Prop_TargetDayOfWeek
        {
            get => _propTargetDayOfWeek;
            set { _propTargetDayOfWeek = value; InvokePropertyChanged(this, nameof(Prop_TargetDayOfWeek)); }
        }
        #endregion

        #region Prop_Mode
        private DayOfWeekSearchMode _propMode = DayOfWeekSearchMode.NextWorkingOccurrence;
        /// <summary>
        /// Режим поиска:
        /// NextOccurrence — просто следующее вхождение дня недели.
        /// NextWorkingOccurrence — следующее рабочее вхождение.
        /// NearestWorkday — следующее вхождение + коррекция если выходной.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_DayOfWeekSearchMode)]
        public DayOfWeekSearchMode Prop_Mode
        {
            get => _propMode;
            set { _propMode = value; InvokePropertyChanged(this, nameof(Prop_Mode)); }
        }
        #endregion

        #region Prop_IfWeekend
        private DeadlineWeekendBehavior _propIfWeekend = DeadlineWeekendBehavior.NextWorkday;
        /// <summary>
        /// Поведение при попадании найденного дня на выходной (для режима NearestWorkday).
        /// NextWorkday — сдвинуть вперёд. PrevWorkday — назад. AsIs — не сдвигать.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_IfWeekend)]
        public DeadlineWeekendBehavior Prop_IfWeekend
        {
            get => _propIfWeekend;
            set { _propIfWeekend = value; InvokePropertyChanged(this, nameof(Prop_IfWeekend)); }
        }
        #endregion

        #region Prop_IncludeStartDate
        private bool _propIncludeStartDate = false;
        /// <summary>
        /// Учитывать стартовую дату если она совпадает с искомым днём недели.
        /// false (по умолчанию) — поиск начинается со следующего дня после startDate.
        /// true — если startDate уже нужный день, вернуть её.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_IncludeStartDate)]
        public bool Prop_IncludeStartDate
        {
            get => _propIncludeStartDate;
            set { _propIncludeStartDate = value; InvokePropertyChanged(this, nameof(Prop_IncludeStartDate)); }
        }
        #endregion

        #region Prop_MaxLookAhead
        private string _propMaxLookAhead;
        /// <summary>
        /// Максимальное количество дней поиска вперёд (по умолчанию 30).
        /// Защита от бесконечного цикла в режиме NextWorkingOccurrence —
        /// если все вхождения дня недели в периоде выходные (январские каникулы).
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_MaxLookAhead)]
        public string Prop_MaxLookAhead
        {
            get => _propMaxLookAhead;
            set { _propMaxLookAhead = value; InvokePropertyChanged(this, nameof(Prop_MaxLookAhead)); }
        }
        #endregion

        #region Prop_ResultDate (Выходной)
        private string _propResultDate;
        /// <summary>Имя переменной скрипта для записи найденной даты (DateTime).</summary>
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

        #region Prop_DaysAhead (Выходной)
        private string _propDaysAhead;
        /// <summary>Имя переменной скрипта для записи количества дней от StartDate до результата (int).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_DaysAhead)]
        public string Prop_DaysAhead
        {
            get => _propDaysAhead;
            set { _propDaysAhead = value; InvokePropertyChanged(this, nameof(Prop_DaysAhead)); }
        }
        #endregion

        #region Prop_DayType (Выходной)
        private string _propDayType;
        /// <summary>
        /// Имя переменной скрипта для записи типа найденного дня.
        /// Значение: Workday | Weekend | Holiday | ShortWorkday.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_DayType)]
        public string Prop_DayType
        {
            get => _propDayType;
            set { _propDayType = value; InvokePropertyChanged(this, nameof(Prop_DayType)); }
        }
        #endregion

        #region Prop_IsFound (Выходной)
        private string _propIsFound;
        /// <summary>
        /// Имя переменной скрипта для записи флага успеха (bool).
        /// false если день не найден в пределах MaxLookAhead дней.
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

        public CalendarNextDayOfWeekBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_CalendarNextDayOfWeek;
            sdkComponentHelp =
                "Находит ближайшую дату указанного дня недели с учётом производственного календаря.\n" +
                "\n" +
                "── Режимы поиска ──────────────────────────────\n" +
                "NextOccurrence        — просто следующее вхождение без проверки типа дня\n" +
                "NextWorkingOccurrence — следующее вхождение, которое является рабочим днём\n" +
                "NearestWorkday        — следующее вхождение + коррекция если выходной\n" +
                "\n" +
                "── Параметры ──────────────────────────────────\n" +
                "ID календаря — GUID из активности «Календарь: Загрузить»\n" +
                "Начальная дата — дата отсчёта поиска\n" +
                "День недели — Monday / Tuesday / ... / Sunday\n" +
                "Режим поиска — см. выше\n" +
                "При выходном — NextWorkday / PrevWorkday / AsIs (для NearestWorkday)\n" +
                "Включать стартовую дату — вернуть startDate если она уже нужный день\n" +
                "Макс. дней поиска — защита от бесконечного цикла (по умолчанию 30)\n" +
                "\n" +
                "── Выходные данные ────────────────────────────\n" +
                "Результат (дата) — найденная дата\n" +
                "Дней вперёд — количество дней от StartDate до результата\n" +
                "Тип дня — Workday / Weekend / Holiday / ShortWorkday\n" +
                "Найден — false если не найдено за MaxLookAhead дней\n" +
                "\n" +
                "── Примеры ────────────────────────────────────\n" +
                "Monday / NextWorkingOccurrence → первый рабочий понедельник\n" +
                "Friday / NearestWorkday / PrevWorkday → пятница или четверг если праздник\n" +
                "Wednesday / NextOccurrence → следующая среда без проверки типа дня";

            sdkComponentIcon = ActivityIcons.Calendar;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Основные
                PropertyBuilder.Script<string>("Prop_CalendarId",         ActivityStrings.Field_CalendarID),
                PropertyBuilder.Script<DateTime>("Prop_StartDate",        ActivityStrings.Field_StartDate),
                PropertyBuilder.Enum<TargetDayOfWeek>("Prop_TargetDayOfWeek", ActivityStrings.Field_TargetDayOfWeek),
                PropertyBuilder.Enum<DayOfWeekSearchMode>("Prop_Mode",    ActivityStrings.Field_DayOfWeekSearchMode),
                // Настройки
                PropertyBuilder.Enum<DeadlineWeekendBehavior>("Prop_IfWeekend", ActivityStrings.Field_IfWeekend),
                PropertyBuilder.BooleanObject("Prop_IncludeStartDate",    ActivityStrings.Field_IncludeStartDate),
                PropertyBuilder.Script<int>("Prop_MaxLookAhead",          ActivityStrings.Field_MaxLookAhead),
                // Выходные
                PropertyBuilder.Variable<DateTime>("Prop_ResultDate",     ActivityStrings.Field_ResultDate),
                PropertyBuilder.Variable<int>("Prop_DaysAhead",           ActivityStrings.Field_DaysAhead),
                PropertyBuilder.Variable<string>("Prop_DayType",          ActivityStrings.Field_DayType),
                PropertyBuilder.Variable<bool>("Prop_IsFound",            ActivityStrings.Field_NthIsFound)
            };

            InitClass(container);

            this.Prop_CalendarId       = string.Empty;
            this.Prop_TargetDayOfWeek  = TargetDayOfWeek.Monday;
            this.Prop_Mode             = DayOfWeekSearchMode.NextWorkingOccurrence;
            this.Prop_IfWeekend        = DeadlineWeekendBehavior.NextWorkday;
            this.Prop_IncludeStartDate = false;
            this.Prop_MaxLookAhead     = "30";
        }

        // =====================================================================
        // Выполнение
        // =====================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // ── Читаем и проверяем входные параметры ───────────────────

                string calendarGuid = GetPropertyValue<string>(
                    this.Prop_CalendarId, nameof(Prop_CalendarId), sd);

                var calendar = RepoDict.Get<ProductionCalendar>(calendarGuid);
                if (calendar == null)
                    return Fail(
                        $"Календарь не найден в кэше по GUID: {calendarGuid}. " +
                        "Сначала загрузите календарь активностью «Календарь: Загрузить».");

                // Разбираем стартовую дату
                object startObj = GetPropertyValue(this.Prop_StartDate, nameof(Prop_StartDate), sd);
                if (!DateTime.TryParse(startObj?.ToString(), out DateTime startDate))
                    return Fail(ActivityStrings.Error_InvalidStartDate);

                // Разбираем MaxLookAhead — с fallback на 30
                int maxLookAhead = 30;
                if (!string.IsNullOrWhiteSpace(this.Prop_MaxLookAhead))
                {
                    object lookObj = GetPropertyValue(
                        this.Prop_MaxLookAhead, nameof(Prop_MaxLookAhead), sd);
                    if (int.TryParse(lookObj?.ToString(), out int parsed) && parsed > 0)
                        maxLookAhead = parsed;
                }

                // Преобразуем TargetDayOfWeek → DayOfWeek
                DayOfWeek targetDow = ConvertToDayOfWeek(this.Prop_TargetDayOfWeek);

                // ── Поиск ──────────────────────────────────────────────────

                // Стартовая точка поиска: следующий день или сама startDate
                DateTime searchFrom = this.Prop_IncludeStartDate
                    ? startDate
                    : startDate.AddDays(1);

                // Граница поиска по MaxLookAhead
                DateTime searchLimit = startDate.AddDays(maxLookAhead);

                DateTime? found = Search(
                    calendar, searchFrom, searchLimit, targetDow,
                    this.Prop_Mode, this.Prop_IfWeekend);

                // ── Обрабатываем результат поиска ─────────────────────────

                if (!found.HasValue)
                {
                    // Не найдено — IsFound = false, это не ошибка выполнения
                    SetVariableValue(this.Prop_IsFound,    false,          sd);
                    SetVariableValue(this.Prop_ResultDate, DateTime.MinValue, sd);
                    SetVariableValue(this.Prop_DaysAhead,  0,              sd);
                    SetVariableValue(this.Prop_DayType,    string.Empty,   sd);

                    // [FIX CS1503] GetRussianDayName принимает TargetDayOfWeek, не DayOfWeek
                    string dowName = GetRussianDayName(this.Prop_TargetDayOfWeek);
                    return new ExecutionResult
                    {
                        IsSuccess      = true,
                        SuccessMessage =
                            $"Рабочий {dowName} не найден в пределах {maxLookAhead} дней " +
                            $"от {startDate:dd.MM.yyyy}. Увеличьте Prop_MaxLookAhead."
                    };
                }

                DateTime result   = found.Value;
                DayType  dayType  = calendar.GetDayType(result);
                int      daysAhead = (result - startDate).Days;

                // Записываем выходные параметры
                SetVariableValue(this.Prop_ResultDate, result,             sd);
                SetVariableValue(this.Prop_DaysAhead,  daysAhead,          sd);
                SetVariableValue(this.Prop_DayType,    dayType.ToString(), sd);
                SetVariableValue(this.Prop_IsFound,    true,               sd);

                // ── Формируем сообщение ────────────────────────────────────

                // [FIX CS1503] GetRussianDayName и BuildModeNote принимают TargetDayOfWeek, не DayOfWeek
                string dowRu      = GetRussianDayName(this.Prop_TargetDayOfWeek);
                string modeNote   = BuildModeNote(this.Prop_Mode, this.Prop_IfWeekend,
                    dayType, result, this.Prop_TargetDayOfWeek);

                return new ExecutionResult
                {
                    IsSuccess      = true,
                    SuccessMessage =
                        $"Следующий {dowRu}: {result:dd.MM.yyyy} " +
                        $"(через {daysAhead} дн., {dayType}){modeNote}"
                };
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка поиска дня недели: {ex.Message}");
            }
        }

        // =====================================================================
        // Основной метод поиска
        // =====================================================================

        /// <summary>
        /// Ищет ближайшую дату указанного дня недели в диапазоне [searchFrom, searchLimit].
        /// Возвращает null если день не найден (режим NextWorkingOccurrence без рабочих вхождений).
        /// </summary>
        /// <param name="calendar">Производственный календарь.</param>
        /// <param name="searchFrom">Начало поиска включительно.</param>
        /// <param name="searchLimit">Граница поиска включительно.</param>
        /// <param name="targetDow">Искомый день недели.</param>
        /// <param name="mode">Режим поиска.</param>
        /// <param name="ifWeekend">Поведение при выходном (для NearestWorkday).</param>
        private static DateTime? Search(
            ProductionCalendar     calendar,
            DateTime               searchFrom,
            DateTime               searchLimit,
            DayOfWeek              targetDow,
            DayOfWeekSearchMode    mode,
            DeadlineWeekendBehavior ifWeekend)
        {
            DateTime current = searchFrom;

            while (current <= searchLimit)
            {
                // Нашли нужный день недели
                if (current.DayOfWeek == targetDow)
                {
                    switch (mode)
                    {
                        case DayOfWeekSearchMode.NextOccurrence:
                            // Берём сразу — тип дня не важен
                            return current;

                        case DayOfWeekSearchMode.NextWorkingOccurrence:
                            // Берём только если рабочий по календарю
                            // Если не рабочий — продолжаем поиск на следующей неделе
                            if (calendar.IsWorkday(current))
                                return current;
                            // Перепрыгиваем на 7 дней — следующее вхождение того же дня недели
                            current = current.AddDays(7);
                            continue;

                        case DayOfWeekSearchMode.NearestWorkday:
                            // Берём в любом случае, потом скорректируем если выходной
                            if (calendar.IsWorkday(current))
                                return current;
                            // День выходной — корректируем по правилу IfWeekend
                            return AdjustToNearestWorkday(calendar, current, ifWeekend);
                    }
                }

                current = current.AddDays(1);
            }

            // Не найдено в пределах MaxLookAhead
            return null;
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>
        /// Сдвигает дату на ближайший рабочий день согласно правилу поведения при выходном.
        /// AsIs — возвращает дату без изменений.
        /// NextWorkday — ищет вперёд.
        /// PrevWorkday — ищет назад.
        /// </summary>
        private static DateTime AdjustToNearestWorkday(
            ProductionCalendar      calendar,
            DateTime                date,
            DeadlineWeekendBehavior behavior)
        {
            switch (behavior)
            {
                case DeadlineWeekendBehavior.AsIs:
                    // Не сдвигаем — возвращаем как есть
                    return date;

                case DeadlineWeekendBehavior.NextWorkday:
                    // Идём вперёд пока не найдём рабочий день
                    return calendar.GetNextWorkday(date);

                case DeadlineWeekendBehavior.PrevWorkday:
                    // Идём назад пока не найдём рабочий день
                    return calendar.GetPrevWorkday(date);

                default:
                    return date;
            }
        }

        /// <summary>
        /// Конвертирует TargetDayOfWeek → DayOfWeek через статический словарь.
        /// </summary>
        private static DayOfWeek ConvertToDayOfWeek(TargetDayOfWeek target) =>
            TargetToDayOfWeek.TryGetValue(target, out DayOfWeek dow) ? dow : DayOfWeek.Monday;

        /// <summary>
        /// Возвращает русское название дня недели в именительном падеже.
        /// </summary>
        private static string GetRussianDayName(TargetDayOfWeek target)
        {
            DayOfWeek dow = ConvertToDayOfWeek(target);
            return RussianDayNames.TryGetValue(dow, out string name) ? name : dow.ToString();
        }

        /// <summary>
        /// Строит дополнительную заметку для SuccessMessage —
        /// поясняет если результат был скорректирован относительно исходного дня недели.
        /// </summary>
        private static string BuildModeNote(
            DayOfWeekSearchMode    mode,
            DeadlineWeekendBehavior ifWeekend,
            DayType                resultDayType,
            DateTime               result,
            TargetDayOfWeek        target)
        {
            // Для NearestWorkday сообщаем если произошла коррекция
            if (mode == DayOfWeekSearchMode.NearestWorkday
                && result.DayOfWeek != ConvertToDayOfWeek(target))
            {
                string direction = ifWeekend == DeadlineWeekendBehavior.NextWorkday
                    ? "перенесён вперёд"
                    : "перенесён назад";
                return $" [{direction} из-за выходного]";
            }

            return string.Empty;
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

            if (string.IsNullOrWhiteSpace(this.Prop_StartDate))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_StartDate),
                    Error        = ActivityStrings.Error_StartDateRequired
                });

            return ret;
        }
    }
}
