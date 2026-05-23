// =============================================================================
// CalendarLoadBack.cs — активность загрузки производственного календаря.
//
// ИСПРАВЛЕНИЯ БАГОВ:
//   [БАГ-6] calendar.Year больше не перезаписывается из Prop_Year после парсинга.
//           Год из Prop_Year используется только как fallback если парсер не нашёл год.
//
// НОВЫЕ ВОЗМОЖНОСТИ:
//   [П1]  Prop_ForceReload (bool) — скачать заново даже если файл уже есть.
//   [П5]  Prop_CacheAgeDays (int) — не скачивать если файл свежее N дней.
//   [П6]  Prop_DownloadTimeoutSeconds (int) — настраиваемый таймаут вместо хардкода 30 с.
//         LoadedFrom теперь возвращает три значения: File / Internet / Cache.
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Calendar.Models;
using Primo.MIA.Calendar.Parsers;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Активность загрузки производственного календаря.
    /// Загружает календарь из файла или из интернета (xmlcalendar.ru).
    /// </summary>
    public class CalendarLoadBack : PrimoComponentTO<CalendarLoad>
    {
        // =====================================================================
        // Статические словари маппинга
        // =====================================================================

        private static readonly Dictionary<CalendarFormat, string> ExtensionMap =
            new Dictionary<CalendarFormat, string>
            {
                { CalendarFormat.Xml,  "xml"  },
                { CalendarFormat.Json, "json" },
                { CalendarFormat.Csv,  "csv"  },
                { CalendarFormat.Txt,  "txt"  }
            };

        private static readonly Dictionary<CalendarRegion, string> RegionCodeMap =
            new Dictionary<CalendarRegion, string>
            {
                { CalendarRegion.Russia,     "ru" },
                { CalendarRegion.Kazakhstan, "kz" },
                { CalendarRegion.Belarus,    "by" },
                { CalendarRegion.Uzbekistan, "uz" }
            };

        private static readonly Dictionary<CalendarRegion, string> RegionDisplayMap =
            new Dictionary<CalendarRegion, string>
            {
                { CalendarRegion.Russia,     "Россия"     },
                { CalendarRegion.Kazakhstan, "Казахстан"  },
                { CalendarRegion.Belarus,    "Беларусь"   },
                { CalendarRegion.Uzbekistan, "Узбекистан" }
            };

        // =====================================================================
        // Свойства
        // =====================================================================

        #region Prop_CalendarPath
        private string _propCalendarPath;
        /// <summary>Путь к папке для хранения файлов производственного календаря.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CalendarPath)]
        public string Prop_CalendarPath
        {
            get => _propCalendarPath;
            set { _propCalendarPath = value; InvokePropertyChanged(this, nameof(Prop_CalendarPath)); }
        }
        #endregion

        #region Prop_Year
        private string _propYear;
        /// <summary>Год календаря — fallback если парсер не нашёл год в файле.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CalendarYear)]
        public string Prop_Year
        {
            get => _propYear;
            set { _propYear = value; InvokePropertyChanged(this, nameof(Prop_Year)); }
        }
        #endregion

        #region Prop_Format
        private CalendarFormat _propFormat = CalendarFormat.Xml;
        /// <summary>Формат файла календаря: Xml, Json, Csv или Txt.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Format),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CalendarFormat)]
        public CalendarFormat Prop_Format
        {
            get => _propFormat;
            set { _propFormat = value; InvokePropertyChanged(this, nameof(Prop_Format)); }
        }
        #endregion

        #region Prop_Region
        private CalendarRegion _propRegion = CalendarRegion.Russia;
        /// <summary>Регион производственного календаря: Россия, Казахстан, Беларусь или Узбекистан.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CalendarRegion)]
        public CalendarRegion Prop_Region
        {
            get => _propRegion;
            set { _propRegion = value; InvokePropertyChanged(this, nameof(Prop_Region)); }
        }
        #endregion

        #region Prop_DownloadIfMissing
        private bool _propDownloadIfMissing = true;
        /// <summary>Скачать файл календаря с xmlcalendar.ru если он отсутствует локально (по умолчанию true).</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_DownloadIfMissing)]
        public bool Prop_DownloadIfMissing
        {
            get => _propDownloadIfMissing;
            set { _propDownloadIfMissing = value; InvokePropertyChanged(this, nameof(Prop_DownloadIfMissing)); }
        }
        #endregion

        #region Prop_ForceReload [П1]
        private bool _propForceReload = false;
        /// <summary>
        /// [П1] Скачать заново даже если файл уже существует.
        /// Имеет приоритет над Prop_CacheAgeDays.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_ForceReload)]
        public bool Prop_ForceReload
        {
            get => _propForceReload;
            set { _propForceReload = value; InvokePropertyChanged(this, nameof(Prop_ForceReload)); }
        }
        #endregion

        #region Prop_CacheAgeDays [П5]
        private int _propCacheAgeDays = 30;
        /// <summary>
        /// [П5] Максимальный возраст локального файла в днях.
        /// 0 = всегда считать устаревшим (аналог ForceReload).
        /// Игнорируется если DownloadIfMissing = false.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CacheAgeDays)]
        public int Prop_CacheAgeDays
        {
            get => _propCacheAgeDays;
            set { _propCacheAgeDays = value; InvokePropertyChanged(this, nameof(Prop_CacheAgeDays)); }
        }
        #endregion

        #region Prop_DownloadTimeoutSeconds [П6]
        private int _propDownloadTimeoutSeconds = 30;
        /// <summary>[П6] Таймаут HTTP-скачивания в секундах (по умолчанию 30).</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_DownloadTimeoutSeconds)]
        public int Prop_DownloadTimeoutSeconds
        {
            get => _propDownloadTimeoutSeconds;
            set { _propDownloadTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_DownloadTimeoutSeconds)); }
        }
        #endregion

        #region Prop_CalendarId (Выходной)
        private string _propCalendarId;
        /// <summary>Имя переменной скрипта для записи GUID загруженного календаря — используется как ID в последующих активностях.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CalendarID)]
        public string Prop_CalendarId
        {
            get => _propCalendarId;
            set { _propCalendarId = value; InvokePropertyChanged(this, nameof(Prop_CalendarId)); }
        }
        #endregion

        #region Prop_CalendarLoaded (Выходной)
        private string _propCalendarLoaded;
        /// <summary>Имя переменной скрипта для записи признака успешной загрузки календаря (bool).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CalendarLoaded)]
        public string Prop_CalendarLoaded
        {
            get => _propCalendarLoaded;
            set { _propCalendarLoaded = value; InvokePropertyChanged(this, nameof(Prop_CalendarLoaded)); }
        }
        #endregion

        #region Prop_FilePath (Выходной)
        private string _propFilePath;
        /// <summary>Имя переменной скрипта для записи полного пути к загруженному файлу календаря (string).</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CalendarFilePath)]
        public string Prop_FilePath
        {
            get => _propFilePath;
            set { _propFilePath = value; InvokePropertyChanged(this, nameof(Prop_FilePath)); }
        }
        #endregion

        #region Prop_LoadedFrom (Выходной)
        private string _propLoadedFrom;
        /// <summary>Источник загрузки: "File" / "Internet" / "Cache".</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_LoadedFrom)]
        public string Prop_LoadedFrom
        {
            get => _propLoadedFrom;
            set { _propLoadedFrom = value; InvokePropertyChanged(this, nameof(Prop_LoadedFrom)); }
        }
        #endregion

        public override string GroupName
        {
            get => ActivityCategories.Calendar;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 120000; // запас для любого пользовательского таймаута
            set { }
        }

        // =====================================================================
        // Конструктор
        // =====================================================================

        public CalendarLoadBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_CalendarLoad;
            sdkComponentHelp =
                "Загружает производственный календарь из файла или из интернета.\n" +
                "\n" +
                "── Параметры ──────────────────────────────────\n" +
                "Путь к папке — директория для хранения файлов календаря\n" +
                "Год — год календаря (fallback если не найден в файле)\n" +
                "Формат — XML, JSON, CSV или TXT\n" +
                "Регион — Россия, Казахстан, Беларусь, Узбекистан\n" +
                "Загрузить из интернета — скачать с xmlcalendar.ru при отсутствии файла\n" +
                "Принудительная перезагрузка — скачать даже если файл есть\n" +
                "Срок кэша (дней) — не скачивать если файл свежее N дней (0 = всегда скачивать)\n" +
                "Таймаут скачивания (сек) — таймаут HTTP-запроса\n" +
                "\n" +
                "── Выходные данные ────────────────────────────\n" +
                "ID календаря — GUID для активности «Календарь: Операции»\n" +
                "Календарь загружен — true если загрузка успешна\n" +
                "Путь к файлу — полный путь к загруженному файлу\n" +
                "Загружен из — File | Internet | Cache";

            sdkComponentIcon = ActivityIcons.Calendar;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.FolderSelector("Prop_CalendarPath",       ActivityStrings.Field_CalendarPath),
                PropertyBuilder.Int("Prop_Year",                           ActivityStrings.Field_CalendarYear),
                PropertyBuilder.Enum<CalendarFormat>("Prop_Format",        ActivityStrings.Field_CalendarFormat),
                PropertyBuilder.Enum<CalendarRegion>("Prop_Region",        ActivityStrings.Field_CalendarRegion),
                PropertyBuilder.BooleanObject("Prop_DownloadIfMissing",    ActivityStrings.Field_DownloadIfMissing),
                PropertyBuilder.BooleanObject("Prop_ForceReload",          ActivityStrings.Field_ForceReload),          // [П1]
                PropertyBuilder.Int("Prop_CacheAgeDays",                   ActivityStrings.Field_CacheAgeDays),          // [П5]
                PropertyBuilder.Int("Prop_DownloadTimeoutSeconds",         ActivityStrings.Field_DownloadTimeoutSeconds), // [П6]
                PropertyBuilder.Variable<string>("Prop_CalendarId",        ActivityStrings.Field_CalendarID),
                PropertyBuilder.Variable<bool>("Prop_CalendarLoaded",      ActivityStrings.Field_CalendarLoaded),
                PropertyBuilder.Variable<string>("Prop_FilePath",          ActivityStrings.Field_CalendarFilePath),
                PropertyBuilder.Variable<string>("Prop_LoadedFrom",        ActivityStrings.Field_LoadedFrom)
            };

            InitClass(container);

            this.Prop_CalendarPath          = string.Empty;
            this.Prop_Year                  = DateTime.Now.Year.ToString();
            this.Prop_Format                = CalendarFormat.Xml;
            this.Prop_Region                = CalendarRegion.Russia;
            this.Prop_DownloadIfMissing      = true;
            this.Prop_ForceReload            = false;
            this.Prop_CacheAgeDays           = 30;
            this.Prop_DownloadTimeoutSeconds = 30;
        }

        // =====================================================================
        // Выполнение
        // =====================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string calendarPath = GetPropertyValue<string>(
                    this.Prop_CalendarPath, nameof(Prop_CalendarPath), sd);

                if (string.IsNullOrWhiteSpace(calendarPath))
                    return Fail(ActivityStrings.Error_CalendarPathRequired);

                object yearObj = GetPropertyValue(this.Prop_Year, nameof(Prop_Year), sd);
                if (!int.TryParse(yearObj?.ToString(), out int year))
                    return Fail(ActivityStrings.Error_YearMustBeInteger);

                string regionCode     = GetRegionCode();
                string fileName       = $"calendar_{regionCode}_{year}.{GetFileExtension()}";
                string filePath       = Path.Combine(calendarPath, fileName);

                bool fileExists = File.Exists(filePath);
                // [П5] Свежесть файла — используем кэш только если файл свежий и нет ForceReload
                bool fileFresh  = fileExists && !this.Prop_ForceReload && IsFileFresh(filePath);

                ProductionCalendar calendar;
                string             loadedFromStr;
                string             actualFilePath;

                if (fileFresh)
                {
                    // [П5] Файл свежий — читаем без скачивания
                    calendar       = LoadFromFile(filePath, year);
                    loadedFromStr  = "Cache";
                    actualFilePath = filePath;
                }
                else if (fileExists && !this.Prop_DownloadIfMissing && !this.Prop_ForceReload)
                {
                    // Файл есть, но скачивание выключено и принудительная перезагрузка не нужна
                    calendar       = LoadFromFile(filePath, year);
                    loadedFromStr  = "File";
                    actualFilePath = filePath;
                }
                else if (this.Prop_DownloadIfMissing || this.Prop_ForceReload)
                {
                    // Скачиваем: нет файла, файл устарел, или ForceReload = true
                    (calendar, actualFilePath) = DownloadFromInternet(calendarPath, year);
                    loadedFromStr = "Internet";
                }
                else
                {
                    return Fail($"{ActivityStrings.Error_CalendarFileNotFound}: {filePath}");
                }

                calendar.CountryCode = regionCode;

                string calendarGuid = Guid.NewGuid().ToString("D");
                RepoDict.Set(calendarGuid, calendar);

                SetVariableValue(this.Prop_CalendarId,     calendarGuid,  sd);
                SetVariableValue(this.Prop_CalendarLoaded, true,          sd);
                SetVariableValue(this.Prop_FilePath,       actualFilePath, sd);
                SetVariableValue(this.Prop_LoadedFrom,     loadedFromStr, sd);

                string regionName = GetRegionDisplayName();
                string sourceMsg  = loadedFromStr == "Internet" ? "из интернета"
                                  : loadedFromStr == "Cache"    ? "из кэша"
                                  :                               "из файла";

                return new ExecutionResult
                {
                    IsSuccess      = true,
                    SuccessMessage = $"Календарь за {calendar.Year} год ({regionName}) загружен {sourceMsg}"
                };
            }
            catch (CalendarParseException ex)
            {
                return Fail($"{ActivityStrings.Error_CalendarParseFailed}: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка загрузки календаря: {ex.Message}");
            }
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>
        /// [П5] Возвращает true если файл свежее Prop_CacheAgeDays дней.
        /// При CacheAgeDays = 0 всегда возвращает false (всегда считать устаревшим).
        /// </summary>
        private bool IsFileFresh(string filePath)
        {
            if (this.Prop_CacheAgeDays <= 0)
                return false;
            double ageInDays = (DateTime.Now - File.GetLastWriteTime(filePath)).TotalDays;
            return ageInDays <= this.Prop_CacheAgeDays;
        }

        private string GetFileExtension() =>
            ExtensionMap.TryGetValue(this.Prop_Format, out string ext) ? ext : "xml";

        private string GetRegionCode() =>
            RegionCodeMap.TryGetValue(this.Prop_Region, out string code) ? code : "ru";

        private string GetRegionDisplayName() =>
            RegionDisplayMap.TryGetValue(this.Prop_Region, out string name) ? name : "Россия";

        /// <summary>
        /// Загружает и парсит календарь из локального файла.
        /// [БАГ-6] Год из файла сохраняется; yearFallback используется только если Year == 0.
        /// </summary>
        private ProductionCalendar LoadFromFile(string filePath, int yearFallback)
        {
            string content             = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            ProductionCalendar calendar = GetParser().Parse(content);

            // [БАГ-6] Не перезаписываем год — только восстанавливаем если парсер не нашёл
            if (calendar.Year == 0)
                calendar.Year = yearFallback;

            return calendar;
        }

        /// <summary>
        /// Скачивает календарь с xmlcalendar.ru и сохраняет файл.
        /// [П6] Таймаут берётся из Prop_DownloadTimeoutSeconds.
        /// [БАГ-6] Год из файла сохраняется; yearFallback только при Year == 0.
        /// </summary>
        private (ProductionCalendar calendar, string actualFilePath) DownloadFromInternet(
            string calendarPath, int yearFallback)
        {
            string regionCode     = GetRegionCode();
            string extension      = GetFileExtension();
            string url            = $"https://xmlcalendar.ru/data/{regionCode}/{yearFallback}/calendar.{extension}";
            string actualFilePath = Path.Combine(calendarPath,
                $"calendar_{regionCode}_{yearFallback}.{extension}");

            if (!Directory.Exists(calendarPath))
                Directory.CreateDirectory(calendarPath);

            string content;
            try
            {
                // [П6] Настраиваемый таймаут; минимум 1 секунда для защиты от нуля
                int timeout = Math.Max(this.Prop_DownloadTimeoutSeconds, 1);
                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(timeout);
                    http.DefaultRequestHeaders.Add("User-Agent", "Primo.MIA/1.0");
                    content = http.GetStringAsync(url).GetAwaiter().GetResult();
                }
                File.WriteAllText(actualFilePath, content, System.Text.Encoding.UTF8);
            }
            catch (TaskCanceledException)
            {
                throw new Exception(
                    $"{ActivityStrings.Error_CalendarDownloadFailed}: " +
                    $"Превышено время ожидания ({this.Prop_DownloadTimeoutSeconds} с)");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"{ActivityStrings.Error_CalendarDownloadFailed}: {ex.Message}");
            }

            ProductionCalendar calendar = GetParser().Parse(content);

            // [БАГ-6] Fallback только если год не определён из файла
            if (calendar.Year == 0)
                calendar.Year = yearFallback;

            return (calendar, actualFilePath);
        }

        /// <summary>Словарь-фабрика парсеров по формату.</summary>
        private ICalendarParser GetParser()
        {
            var factory = new Dictionary<CalendarFormat, Func<ICalendarParser>>
            {
                { CalendarFormat.Xml,  () => new XmlCalendarParser()  },
                { CalendarFormat.Json, () => new JsonCalendarParser() },
                { CalendarFormat.Csv,  () => new CsvCalendarParser()  },
                { CalendarFormat.Txt,  () => new TxtCalendarParser()  }
            };

            if (factory.TryGetValue(this.Prop_Format, out Func<ICalendarParser> create))
                return create();

            throw new ArgumentException(ActivityStrings.Error_CalendarUnsupportedFormat);
        }

        private static ExecutionResult Fail(string msg) =>
            new ExecutionResult { IsSuccess = false, ErrorMessage = msg };

        // =====================================================================
        // Валидация
        // =====================================================================

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_CalendarPath))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_CalendarPath),
                    Error        = ActivityStrings.Error_CalendarPathRequired
                });

            if (string.IsNullOrWhiteSpace(this.Prop_Year))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_Year),
                    Error        = ActivityStrings.Error_CalendarYearRequired
                });

            if (this.Prop_DownloadTimeoutSeconds < 0)
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_DownloadTimeoutSeconds),
                    Error        = "Таймаут скачивания должен быть >= 0"
                });

            return ret;
        }
    }
}
