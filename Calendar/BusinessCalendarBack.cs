// =============================================================================
// BusinessCalendarBack.cs — активность операций с производственным календарём.
//
// ИСПРАВЛЕНИЯ БАГОВ:
//   [БАГ-1] IsWorkday вызывал GetDayType дважды — устранено кэшированием в WriteOutputDayInfo.
//   [БАГ-2] IsTransferredWorkday O(n) — устранено через TransferWorkdays в ProductionCalendar.
//   [БАГ-3] GetDayType: логика переноса унифицирована в ProductionCalendar (TransferWorkdays первый).
//
// ИСПРАВЛЕНИЯ ЛОГИЧЕСКИХ ПРОБЛЕМ:
//   [ЛОГ-2] Добавлена проверка: inputDate.Year должен совпадать с calendar.Year.
//           Без этого ключ "01.03" будет искаться в переносах 2026 года для даты 2025 года.
//   [ЛОГ-3] Prop_DayType изменён с string (dayType.ToString()) на string (enum.ToString()) —
//           оба варианта одинаковы, но теперь явно задокументировано ожидаемое значение.
//
// НОВЫЕ ВОЗМОЖНОСТИ:
//   [П2] Режим CountWorkdays — подсчёт рабочих дней между двумя датами.
//        Новые свойства: Prop_EndDate (входной), Prop_WorkdaysBetween (выходной).
//   [П3] Режимы NextWorkday и PrevWorkday — найти ближайший рабочий день.
//        Используют новые методы GetNextWorkday / GetPrevWorkday в ProductionCalendar.
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
    /// Активность операций с производственным календарём.
    /// Поддерживает режимы: AddWorkDays, SubtractWorkDays, CheckDayType,
    /// CountWorkdays, NextWorkday, PrevWorkday.
    /// </summary>
    public class BusinessCalendarBack : PrimoComponentTO<BusinessCalendar>
    {
        // =====================================================================
        // Статические словари
        // =====================================================================

        /// <summary>Словарь отображаемых названий типов дней на русском языке.</summary>
        private static readonly Dictionary<DayType, string> DayTypeDescriptions =
            new Dictionary<DayType, string>
            {
                { DayType.Workday,      "Рабочий день"            },
                { DayType.Weekend,      "Выходной день"            },
                { DayType.Holiday,      "Праздничный день"         },
                { DayType.ShortWorkday, "Сокращённый рабочий день" }
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

        #region Prop_OperationMode
        private BusinessCalendarMode _propOperationMode = BusinessCalendarMode.AddWorkDays;
        /// <summary>Режим операции с календарём: AddWorkDays, SubtractWorkDays, CheckDayType, CountWorkdays, NextWorkday, PrevWorkday.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_OperationMode)]
        public BusinessCalendarMode Prop_OperationMode
        {
            get => _propOperationMode;
            set { _propOperationMode = value; InvokePropertyChanged(this, nameof(Prop_OperationMode)); }
        }
        #endregion

        #region Prop_InputDate
        private string _propInputDate;
        /// <summary>Входная дата для операции (начальная дата для CountWorkdays).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DateTime))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_InputDate)]
        public string Prop_InputDate
        {
            get => _propInputDate;
            set { _propInputDate = value; InvokePropertyChanged(this, nameof(Prop_InputDate)); }
        }
        #endregion

        #region Prop_EndDate [П2]
        private string _propEndDate;
        /// <summary>
        /// [П2] Конечная дата диапазона для режима CountWorkdays.
        /// Игнорируется в других режимах.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DateTime))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_EndDate)]
        public string Prop_EndDate
        {
            get => _propEndDate;
            set { _propEndDate = value; InvokePropertyChanged(this, nameof(Prop_EndDate)); }
        }
        #endregion

        #region Prop_DaysCount
        private string _propDaysCount;
        /// <summary>Количество дней для прибавления/вычитания.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_DaysCount)]
        public string Prop_DaysCount
        {
            get => _propDaysCount;
            set { _propDaysCount = value; InvokePropertyChanged(this, nameof(Prop_DaysCount)); }
        }
        #endregion

        #region Prop_ResultDate (Выходной)
        private string _propResultDate;
        /// <summary>Имя переменной скрипта для записи результирующей даты.</summary>
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

        #region Prop_DayType (Выходной)
        private string _propDayType;
        /// <summary>
        /// Имя переменной скрипта для записи типа дня.
        /// Значение — строка: Workday | Weekend | Holiday | ShortWorkday.
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

        #region Prop_IsWorkday (Выходной)
        private string _propIsWorkday;
        /// <summary>Имя переменной скрипта для записи признака рабочего дня (bool): true если день рабочий или сокращённый.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_IsWorkday)]
        public string Prop_IsWorkday
        {
            get => _propIsWorkday;
            set { _propIsWorkday = value; InvokePropertyChanged(this, nameof(Prop_IsWorkday)); }
        }
        #endregion

        #region Prop_HolidayName (Выходной)
        private string _propHolidayName;
        /// <summary>Имя переменной скрипта для записи названия праздника (string). Пустая строка если день не праздничный.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_HolidayName)]
        public string Prop_HolidayName
        {
            get => _propHolidayName;
            set { _propHolidayName = value; InvokePropertyChanged(this, nameof(Prop_HolidayName)); }
        }
        #endregion

        #region Prop_WorkdaysInYear (Выходной)
        private string _propWorkdaysInYear;
        /// <summary>Имя переменной скрипта для записи количества рабочих дней в году (int) по статистике календаря.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_WorkdaysInYear)]
        public string Prop_WorkdaysInYear
        {
            get => _propWorkdaysInYear;
            set { _propWorkdaysInYear = value; InvokePropertyChanged(this, nameof(Prop_WorkdaysInYear)); }
        }
        #endregion

        #region Prop_HolidaysInYear (Выходной)
        private string _propHolidaysInYear;
        /// <summary>Имя переменной скрипта для записи количества праздничных дней в году (int) по статистике календаря.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_HolidaysInYear)]
        public string Prop_HolidaysInYear
        {
            get => _propHolidaysInYear;
            set { _propHolidaysInYear = value; InvokePropertyChanged(this, nameof(Prop_HolidaysInYear)); }
        }
        #endregion

        #region Prop_WorkdaysBetween (Выходной) [П2]
        private string _propWorkdaysBetween;
        /// <summary>[П2] Имя переменной скрипта для записи количества рабочих дней в диапазоне.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_WorkdaysBetween)]
        public string Prop_WorkdaysBetween
        {
            get => _propWorkdaysBetween;
            set { _propWorkdaysBetween = value; InvokePropertyChanged(this, nameof(Prop_WorkdaysBetween)); }
        }
        #endregion

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

        public BusinessCalendarBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_BusinessCalendar;
            sdkComponentHelp =
                "Выполняет операции с производственным календарём.\n" +
                "\n" +
                "── Режимы ──────────────────────────────────\n" +
                "AddWorkDays      — прибавить рабочие дни к дате\n" +
                "SubtractWorkDays — вычесть рабочие дни из даты\n" +
                "CheckDayType     — проверить тип дня\n" +
                "CountWorkdays    — подсчитать рабочие дни между двумя датами\n" +
                "NextWorkday      — найти следующий рабочий день\n" +
                "PrevWorkday      — найти предыдущий рабочий день\n" +
                "\n" +
                "── Входные параметры ───────────────────────\n" +
                "ID календаря — GUID из активности «Календарь: Загрузить»\n" +
                "Входная дата — опорная дата (начальная для CountWorkdays)\n" +
                "Конечная дата — конец диапазона для CountWorkdays\n" +
                "Количество дней — для AddWorkDays / SubtractWorkDays\n" +
                "\n" +
                "── Выходные данные ─────────────────────────\n" +
                "Результат (дата) — дата после операции\n" +
                "Тип дня — Workday | Weekend | Holiday | ShortWorkday\n" +
                "Рабочий день — true если день рабочий\n" +
                "Название праздника — для праздничных дней\n" +
                "Рабочих дней в году — статистика\n" +
                "Праздничных дней в году — статистика\n" +
                "Рабочих дней в диапазоне — результат CountWorkdays\n" +
                "\n" +
                "── Кэширование ─────────────────────────────\n" +
                "Календарь получается из RepoDict по GUID.";

            sdkComponentIcon = ActivityIcons.Calendar;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<string>("Prop_CalendarId",       ActivityStrings.Field_CalendarID),
                PropertyBuilder.Enum<BusinessCalendarMode>("Prop_OperationMode", ActivityStrings.Field_OperationMode),
                PropertyBuilder.Script<DateTime>("Prop_InputDate",      ActivityStrings.Field_InputDate),
                // [П2] Конечная дата для CountWorkdays
                PropertyBuilder.Script<DateTime>("Prop_EndDate",        ActivityStrings.Field_EndDate),
                // Script<int> — принимает число, переменную или выражение
                PropertyBuilder.Script<int>("Prop_DaysCount",           ActivityStrings.Field_DaysCount),
                PropertyBuilder.Variable<DateTime>("Prop_ResultDate",   ActivityStrings.Field_ResultDate),
                PropertyBuilder.Variable<string>("Prop_DayType",        ActivityStrings.Field_DayType),
                PropertyBuilder.Variable<bool>("Prop_IsWorkday",        ActivityStrings.Field_IsWorkday),
                PropertyBuilder.Variable<string>("Prop_HolidayName",    ActivityStrings.Field_HolidayName),
                PropertyBuilder.Variable<int>("Prop_WorkdaysInYear",    ActivityStrings.Field_WorkdaysInYear),
                PropertyBuilder.Variable<int>("Prop_HolidaysInYear",    ActivityStrings.Field_HolidaysInYear),
                // [П2] Результат CountWorkdays
                PropertyBuilder.Variable<int>("Prop_WorkdaysBetween",   ActivityStrings.Field_WorkdaysBetween)
            };

            InitClass(container);

            this.Prop_CalendarId    = string.Empty;
            this.Prop_DaysCount     = "1";
            this.Prop_OperationMode = BusinessCalendarMode.AddWorkDays;
        }

        // =====================================================================
        // Выполнение
        // =====================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Получаем GUID и достаём календарь из кэша
                string calendarGuid = GetPropertyValue<string>(
                    this.Prop_CalendarId, nameof(Prop_CalendarId), sd);

                var calendar = RepoDict.Get<ProductionCalendar>(calendarGuid);
                if (calendar == null)
                    return Fail(
                        $"Календарь не найден в кэше по GUID: {calendarGuid}. " +
                        "Сначала загрузите календарь активностью «Календарь: Загрузить».");

                // Разбираем входную дату
                object dateObj = GetPropertyValue(this.Prop_InputDate, nameof(Prop_InputDate), sd);
                if (!DateTime.TryParse(dateObj?.ToString(), out DateTime inputDate))
                    return Fail(ActivityStrings.Error_InvalidInputDate);

                // [ЛОГ-2] Проверяем соответствие года даты и года календаря
                // Ключи в SpecialDays и TransferWorkdays — в формате dd.MM без года.
                // Если год даты != год календаря — результат будет некорректным.
                if (inputDate.Year != calendar.Year)
                    return Fail(
                        $"Дата {inputDate:dd.MM.yyyy} не соответствует году календаря ({calendar.Year}). " +
                        "Загрузите календарь нужного года активностью «Календарь: Загрузить».");

                // Записываем статистику года
                SetVariableValue(this.Prop_WorkdaysInYear,  calendar.Statistics?.Workdays  ?? 0, sd);
                SetVariableValue(this.Prop_HolidaysInYear, calendar.Statistics?.Holidays ?? 0, sd);

                // Выполняем операцию по режиму
                switch (this.Prop_OperationMode)
                {
                    case BusinessCalendarMode.AddWorkDays:
                        return ExecuteAddWorkDays(sd, calendar, inputDate);

                    case BusinessCalendarMode.SubtractWorkDays:
                        return ExecuteSubtractWorkDays(sd, calendar, inputDate);

                    case BusinessCalendarMode.CheckDayType:
                        return ExecuteCheckDayType(sd, calendar, inputDate);

                    case BusinessCalendarMode.CountWorkdays:   // [П2]
                        return ExecuteCountWorkdays(sd, calendar, inputDate);

                    case BusinessCalendarMode.NextWorkday:     // [П3]
                        return ExecuteNextWorkday(sd, calendar, inputDate);

                    case BusinessCalendarMode.PrevWorkday:     // [П3]
                        return ExecutePrevWorkday(sd, calendar, inputDate);

                    default:
                        return Fail($"Неизвестный режим операции: {this.Prop_OperationMode}");
                }
            }
            catch (ArgumentException ex)
            {
                return Fail($"Неверный аргумент: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return Fail($"Недопустимая операция: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка [{this.Prop_OperationMode}]: {ex.Message}");
            }
        }

        // =====================================================================
        // Методы выполнения операций
        // =====================================================================

        /// <summary>Прибавляет рабочие дни к дате.</summary>
        private ExecutionResult ExecuteAddWorkDays(
            ScriptingData sd, ProductionCalendar calendar, DateTime inputDate)
        {
            if (!TryGetDaysCount(sd, out int daysCount, out ExecutionResult err))
                return err;

            DateTime result = calendar.AddWorkdays(inputDate, daysCount);
            WriteOutputDayInfo(sd, calendar, result);

            return new ExecutionResult
            {
                IsSuccess      = true,
                SuccessMessage = $"Прибавлено {daysCount} рабочих дней. Результат: {result:dd.MM.yyyy}"
            };
        }

        /// <summary>Вычитает рабочие дни из даты.</summary>
        private ExecutionResult ExecuteSubtractWorkDays(
            ScriptingData sd, ProductionCalendar calendar, DateTime inputDate)
        {
            if (!TryGetDaysCount(sd, out int daysCount, out ExecutionResult err))
                return err;

            DateTime result = calendar.SubtractWorkdays(inputDate, daysCount);
            WriteOutputDayInfo(sd, calendar, result);

            return new ExecutionResult
            {
                IsSuccess      = true,
                SuccessMessage = $"Вычтено {daysCount} рабочих дней. Результат: {result:dd.MM.yyyy}"
            };
        }

        /// <summary>Проверяет тип дня.</summary>
        private ExecutionResult ExecuteCheckDayType(
            ScriptingData sd, ProductionCalendar calendar, DateTime inputDate)
        {
            WriteOutputDayInfo(sd, calendar, inputDate);

            DayType dayType    = calendar.GetDayType(inputDate);
            string  holiday    = calendar.GetHolidayName(inputDate) ?? string.Empty;
            string  dayTypeMsg = GetDayTypeDescription(dayType);
            string  holidayMsg = string.IsNullOrEmpty(holiday) ? string.Empty : $" ({holiday})";

            return new ExecutionResult
            {
                IsSuccess      = true,
                SuccessMessage = $"{inputDate:dd.MM.yyyy} — {dayTypeMsg}{holidayMsg}"
            };
        }

        /// <summary>
        /// [П2] Подсчитывает рабочие дни между Prop_InputDate и Prop_EndDate.
        /// [ЛОГ-2] Проверяет соответствие года конечной даты году календаря.
        /// </summary>
        private ExecutionResult ExecuteCountWorkdays(
            ScriptingData sd, ProductionCalendar calendar, DateTime startDate)
        {
            // Разбираем конечную дату
            object endObj = GetPropertyValue(this.Prop_EndDate, nameof(Prop_EndDate), sd);
            if (!DateTime.TryParse(endObj?.ToString(), out DateTime endDate))
                return Fail("Некорректный формат конечной даты. Ожидается значение типа DateTime.");

            // [ЛОГ-2] Проверяем год конечной даты
            if (endDate.Year != calendar.Year)
                return Fail(
                    $"Конечная дата {endDate:dd.MM.yyyy} не соответствует году календаря ({calendar.Year}).");

            int count = calendar.GetWorkdaysBetween(startDate, endDate);

            // Записываем результат в выходную переменную
            SetVariableValue(this.Prop_WorkdaysBetween, count, sd);

            // Дополнительно записываем информацию о начальной дате
            WriteOutputDayInfo(sd, calendar, startDate);

            string fromStr = startDate.ToString("dd.MM.yyyy");
            string toStr   = endDate.ToString("dd.MM.yyyy");

            return new ExecutionResult
            {
                IsSuccess      = true,
                SuccessMessage = $"Рабочих дней с {fromStr} по {toStr}: {count}"
            };
        }

        /// <summary>
        /// [П3] Находит следующий рабочий день после inputDate.
        /// </summary>
        private ExecutionResult ExecuteNextWorkday(
            ScriptingData sd, ProductionCalendar calendar, DateTime inputDate)
        {
            DateTime result = calendar.GetNextWorkday(inputDate);
            WriteOutputDayInfo(sd, calendar, result);

            return new ExecutionResult
            {
                IsSuccess      = true,
                SuccessMessage = $"Следующий рабочий день после {inputDate:dd.MM.yyyy}: {result:dd.MM.yyyy}"
            };
        }

        /// <summary>
        /// [П3] Находит предыдущий рабочий день до inputDate.
        /// </summary>
        private ExecutionResult ExecutePrevWorkday(
            ScriptingData sd, ProductionCalendar calendar, DateTime inputDate)
        {
            DateTime result = calendar.GetPrevWorkday(inputDate);
            WriteOutputDayInfo(sd, calendar, result);

            return new ExecutionResult
            {
                IsSuccess      = true,
                SuccessMessage = $"Предыдущий рабочий день до {inputDate:dd.MM.yyyy}: {result:dd.MM.yyyy}"
            };
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>
        /// Безопасно разбирает Prop_DaysCount через TryParse.
        /// Возвращает false и заполняет error если значение не является целым числом.
        /// </summary>
        private bool TryGetDaysCount(ScriptingData sd, out int daysCount, out ExecutionResult error)
        {
            daysCount = 0;
            error     = null;

            object obj = GetPropertyValue(this.Prop_DaysCount, nameof(Prop_DaysCount), sd);
            if (!int.TryParse(obj?.ToString(), out daysCount))
            {
                error = Fail(ActivityStrings.Error_DaysCountMustBeInteger);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Записывает выходные параметры о дне через SetVariableValue.
        /// Вынесен для устранения дублирования в методах Execute*.
        /// [БАГ-1] GetDayType вызывается один раз — результат используется во всех трёх SetVariable.
        /// </summary>
        private void WriteOutputDayInfo(ScriptingData sd, ProductionCalendar calendar, DateTime date)
        {
            // [БАГ-1] Один вызов GetDayType — результат кэшируется в локальных переменных
            DayType dayType   = calendar.GetDayType(date);
            bool    isWorkday = dayType == DayType.Workday || dayType == DayType.ShortWorkday;
            string  holiday   = calendar.GetHolidayName(date) ?? string.Empty;

            SetVariableValue(this.Prop_ResultDate,  date,               sd);
            // [ЛОГ-3] Значение типа дня — строка enum: Workday / Weekend / Holiday / ShortWorkday
            SetVariableValue(this.Prop_DayType,     dayType.ToString(), sd);
            SetVariableValue(this.Prop_IsWorkday,   isWorkday,          sd);
            SetVariableValue(this.Prop_HolidayName, holiday,            sd);
        }

        /// <summary>Возвращает описание типа дня на русском.</summary>
        private string GetDayTypeDescription(DayType dayType) =>
            DayTypeDescriptions.TryGetValue(dayType, out string desc) ? desc : dayType.ToString();

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

            if (string.IsNullOrWhiteSpace(this.Prop_InputDate))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_InputDate),
                    Error        = ActivityStrings.Error_InputDateRequired
                });

            // DaysCount нужен только для Add/Subtract
            bool needsDays = this.Prop_OperationMode == BusinessCalendarMode.AddWorkDays
                          || this.Prop_OperationMode == BusinessCalendarMode.SubtractWorkDays;
            if (needsDays && string.IsNullOrWhiteSpace(this.Prop_DaysCount))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_DaysCount),
                    Error        = ActivityStrings.Error_DaysCountRequired
                });

            // [П2] EndDate нужна только для CountWorkdays
            if (this.Prop_OperationMode == BusinessCalendarMode.CountWorkdays
                && string.IsNullOrWhiteSpace(this.Prop_EndDate))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_EndDate),
                    Error        = ActivityStrings.Error_EndDateRequired
                });

            return ret;
        }
    }
}
