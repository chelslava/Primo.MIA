// =============================================================================
// FileCleanupBack.cs — активность «Файл: Очистка по времени».
//
// Удаляет файлы и/или папки по временному критерию.
//
// Критерии времени (FileTimeAttribute):
//   CreationTime   — дата создания
//   LastWriteTime  — дата последнего изменения
//   LastAccessTime — дата последнего доступа
//
// Способы задания порога (CleanupThresholdMode):
//   OlderThanDays — старше N дней от текущего момента
//   BeforeDate    — временной атрибут раньше указанной даты
//   Both          — оба условия одновременно (И)
//
// Что удалять (CleanupTargetType):
//   FilesOnly             — только файлы
//   EmptyFoldersOnly      — только пустые папки
//   FilesAndEmptyFolders  — файлы + освободившиеся пустые папки
//   FoldersWithContent    — папки целиком (рекурсивно)
//
// Безопасность:
//   Prop_DryRun = true — только показывает что будет удалено, ничего не трогает
//   Prop_FilePattern    — маска имён файлов (*.tmp, *.log, report_*.pdf)
//   Prop_Recursive      — обходить подпапки
//   Prop_MinSizeBytes   — не удалять файлы меньше N байт (0 = без ограничения)
//   Prop_MaxItems       — ограничить количество удалений за один вызов
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LTools.Common.Model;
using LTools.Common.Model.Serialization;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «Файл: Очистка по времени».
    /// Удаляет файлы и папки по временному критерию с поддержкой режима DryRun.
    /// </summary>
    public class FileCleanupBack : PrimoComponentTO<FileCleanup>
    {
        // =====================================================================
        // Статические словари: FileTimeAttribute → геттер времени
        // Используется вместо switch-case для выбора атрибута времени
        // =====================================================================

        /// <summary>
        /// Маппинг атрибута времени на функцию получения значения из FileSystemInfo.
        /// Позволяет выбирать нужный атрибут через словарь без switch-case.
        /// </summary>
        private static readonly Dictionary<FileTimeAttribute, Func<FileSystemInfo, DateTime>>
            TimeGetters = new Dictionary<FileTimeAttribute, Func<FileSystemInfo, DateTime>>
            {
                { FileTimeAttribute.CreationTime,   fsi => fsi.CreationTime   },
                { FileTimeAttribute.LastWriteTime,  fsi => fsi.LastWriteTime  },
                { FileTimeAttribute.LastAccessTime, fsi => fsi.LastAccessTime }
            };

        // =====================================================================
        // Свойства
        // =====================================================================

        #region Prop_FolderPath
        private string _propFolderPath;
        /// <summary>Путь к папке для очистки.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupFolder)]
        public string Prop_FolderPath
        {
            get => _propFolderPath;
            set { _propFolderPath = value; InvokePropertyChanged(this, nameof(Prop_FolderPath)); }
        }
        #endregion

        #region Prop_DeleteFiles
        private bool _propDeleteFiles = true;
        /// <summary>Удалять файлы, соответствующие временному критерию и маске.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Удалять файлы")]
        public bool Prop_DeleteFiles
        {
            get => _propDeleteFiles;
            set { _propDeleteFiles = value; InvokePropertyChanged(this, nameof(Prop_DeleteFiles)); }
        }
        #endregion

        #region Prop_DeleteEmptyFolders
        private bool _propDeleteEmptyFolders = false;
        /// <summary>
        /// Удалять пустые папки, соответствующие временному критерию.
        /// Если включено вместе с DeleteFiles — сначала удаляются файлы,
        /// затем освободившиеся пустые папки.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Удалять пустые папки")]
        public bool Prop_DeleteEmptyFolders
        {
            get => _propDeleteEmptyFolders;
            set { _propDeleteEmptyFolders = value; InvokePropertyChanged(this, nameof(Prop_DeleteEmptyFolders)); }
        }
        #endregion

        #region Prop_DeleteFoldersWithContent
        private bool _propDeleteFoldersWithContent = false;
        /// <summary>
        /// Удалять папки целиком со всем содержимым (рекурсивно).
        /// Временной критерий применяется к самой папке.
        /// ВНИМАНИЕ: необратимая операция, рекомендуется DryRun перед включением.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Удалять папки целиком")]
        public bool Prop_DeleteFoldersWithContent
        {
            get => _propDeleteFoldersWithContent;
            set { _propDeleteFoldersWithContent = value; InvokePropertyChanged(this, nameof(Prop_DeleteFoldersWithContent)); }
        }
        #endregion

        #region Prop_TimeAttribute
        private FileTimeAttribute _propTimeAttribute = FileTimeAttribute.LastWriteTime;
        /// <summary>Временной атрибут для сравнения: CreationTime / LastWriteTime / LastAccessTime.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupTimeAttribute)]
        public FileTimeAttribute Prop_TimeAttribute
        {
            get => _propTimeAttribute;
            set { _propTimeAttribute = value; InvokePropertyChanged(this, nameof(Prop_TimeAttribute)); }
        }
        #endregion

        #region Prop_ThresholdMode
        private CleanupThresholdMode _propThresholdMode = CleanupThresholdMode.OlderThanDays;
        /// <summary>Способ задания порога: количество дней, конкретная дата или оба.</summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupThresholdMode)]
        public CleanupThresholdMode Prop_ThresholdMode
        {
            get => _propThresholdMode;
            set { _propThresholdMode = value; InvokePropertyChanged(this, nameof(Prop_ThresholdMode)); }
        }
        #endregion

        #region Prop_OlderThanDays
        private string _propOlderThanDays;
        /// <summary>
        /// Удалять объекты старше N дней.
        /// Порог = DateTime.Now.AddDays(-N).
        /// Используется при ThresholdMode = OlderThanDays или Both.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupOlderThanDays)]
        public string Prop_OlderThanDays
        {
            get => _propOlderThanDays;
            set { _propOlderThanDays = value; InvokePropertyChanged(this, nameof(Prop_OlderThanDays)); }
        }
        #endregion

        #region Prop_ThresholdDate
        private string _propThresholdDate;
        /// <summary>
        /// Удалять объекты с датой раньше указанной.
        /// Используется при ThresholdMode = BeforeDate или Both.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DateTime))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupThresholdDate)]
        public string Prop_ThresholdDate
        {
            get => _propThresholdDate;
            set { _propThresholdDate = value; InvokePropertyChanged(this, nameof(Prop_ThresholdDate)); }
        }
        #endregion

        #region Prop_FilePattern
        private string _propFilePattern;
        /// <summary>
        /// Маска имён файлов. Поддерживает несколько масок через запятую или точку с запятой.
        /// Wildcard: * и ?.
        /// Примеры: *.tmp   |   *.log;*.tmp   |   report_*.pdf, temp_??.xlsx
        /// Пусто = все файлы (эквивалентно *).
        /// Применяется только при Prop_DeleteFiles = true.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupFilePattern)]
        public string Prop_FilePattern
        {
            get => _propFilePattern;
            set { _propFilePattern = value; InvokePropertyChanged(this, nameof(Prop_FilePattern)); }
        }
        #endregion

        #region Prop_Recursive
        private bool _propRecursive = false;
        /// <summary>
        /// Обходить подпапки рекурсивно.
        /// При false — обрабатывается только корневая папка.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupRecursive)]
        public bool Prop_Recursive
        {
            get => _propRecursive;
            set { _propRecursive = value; InvokePropertyChanged(this, nameof(Prop_Recursive)); }
        }
        #endregion

        #region Prop_MinSizeBytes
        private string _propMinSizeBytes;
        /// <summary>
        /// Не удалять файлы меньше N байт. 0 = без ограничения.
        /// Полезно для сохранения конфигурационных файлов малого размера.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(long))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupMinSize)]
        public string Prop_MinSizeBytes
        {
            get => _propMinSizeBytes;
            set { _propMinSizeBytes = value; InvokePropertyChanged(this, nameof(Prop_MinSizeBytes)); }
        }
        #endregion

        #region Prop_MaxItems
        private string _propMaxItems;
        /// <summary>
        /// Ограничить количество удалений за один вызов. 0 = без ограничения.
        /// Полезно для постепенной очистки больших папок.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupMaxItems)]
        public string Prop_MaxItems
        {
            get => _propMaxItems;
            set { _propMaxItems = value; InvokePropertyChanged(this, nameof(Prop_MaxItems)); }
        }
        #endregion

        #region Prop_DryRun
        private bool _propDryRun = false;
        /// <summary>
        /// Режим пробного запуска — только собирает список кандидатов на удаление,
        /// ничего не удаляет. Результат в Prop_DeletedPaths для проверки.
        /// Рекомендуется включить при первом запуске.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupDryRun)]
        public bool Prop_DryRun
        {
            get => _propDryRun;
            set { _propDryRun = value; InvokePropertyChanged(this, nameof(Prop_DryRun)); }
        }
        #endregion

        #region Prop_IgnoreErrors
        private bool _propIgnoreErrors = true;
        /// <summary>
        /// Продолжать работу при ошибке удаления отдельного файла/папки.
        /// true — ошибка записывается в Prop_Errors, работа продолжается.
        /// false — первая ошибка останавливает активность.
        /// </summary>
        [StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupIgnoreErrors)]
        public bool Prop_IgnoreErrors
        {
            get => _propIgnoreErrors;
            set { _propIgnoreErrors = value; InvokePropertyChanged(this, nameof(Prop_IgnoreErrors)); }
        }
        #endregion

        #region Prop_DeletedCount (Выходной)
        private string _propDeletedCount;
        /// <summary>Количество удалённых (или найденных в DryRun) объектов.</summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupDeletedCount)]
        public string Prop_DeletedCount
        {
            get => _propDeletedCount;
            set { _propDeletedCount = value; InvokePropertyChanged(this, nameof(Prop_DeletedCount)); }
        }
        #endregion

        #region Prop_DeletedPaths (Выходной)
        private string _propDeletedPaths;
        /// <summary>
        /// Список путей удалённых объектов (или кандидатов при DryRun).
        /// Тип: List&lt;string&gt;.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupDeletedPaths)]
        public string Prop_DeletedPaths
        {
            get => _propDeletedPaths;
            set { _propDeletedPaths = value; InvokePropertyChanged(this, nameof(Prop_DeletedPaths)); }
        }
        #endregion

        #region Prop_FreedBytes (Выходной)
        private string _propFreedBytes;
        /// <summary>
        /// Суммарный размер удалённых файлов в байтах.
        /// Для папок — суммарный размер всего удалённого содержимого.
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(long))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupFreedBytes)]
        public string Prop_FreedBytes
        {
            get => _propFreedBytes;
            set { _propFreedBytes = value; InvokePropertyChanged(this, nameof(Prop_FreedBytes)); }
        }
        #endregion

        #region Prop_Errors (Выходной)
        private string _propErrors;
        /// <summary>
        /// Список ошибок удаления (при IgnoreErrors = true).
        /// Тип: List&lt;string&gt;. Формат: "путь: сообщение об ошибке".
        /// </summary>
        [StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName(ActivityStrings.Field_CleanupErrors)]
        public string Prop_Errors
        {
            get => _propErrors;
            set { _propErrors = value; InvokePropertyChanged(this, nameof(Prop_Errors)); }
        }
        #endregion

        // =====================================================================
        // Служебные свойства
        // =====================================================================

        public override string GroupName
        {
            get => ActivityCategories.Utilities;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 300000; // 5 минут — очистка больших папок может занять время
            set { }
        }

        // =====================================================================
        // Конструктор
        // =====================================================================

        public FileCleanupBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_FileCleanup;
            sdkComponentHelp =
                "Удаляет файлы и/или папки по временному критерию.\n" +
                "\n" +
                "── Временные атрибуты ─────────────────────────\n" +
                "CreationTime   — дата создания\n" +
                "LastWriteTime  — дата последнего изменения (рекомендуется)\n" +
                "LastAccessTime — дата последнего доступа\n" +
                "\n" +
                "── Пороги ─────────────────────────────────────\n" +
                "OlderThanDays — старше N дней от сейчас\n" +
                "BeforeDate    — раньше указанной даты\n" +
                "Both          — оба условия одновременно (И)\n" +
                "\n" +
                "── Что удалять ────────────────────────────────\n" +
                "FilesOnly            — только файлы\n" +
                "EmptyFoldersOnly     — только пустые папки\n" +
                "FilesAndEmptyFolders — файлы + освободившиеся пустые папки\n" +
                "FoldersWithContent   — папки целиком рекурсивно\n" +
                "\n" +
                "── Безопасность ───────────────────────────────\n" +
                "DryRun = true — ничего не удаляет, только показывает список\n" +
                "FilePattern   — маска: *.tmp, *.log, report_*.pdf\n" +
                "MinSizeBytes  — не удалять файлы меньше N байт\n" +
                "MaxItems      — ограничить количество удалений\n" +
                "IgnoreErrors  — продолжать при ошибке удаления";

            sdkComponentIcon = ActivityIcons.Clean;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Основные
                PropertyBuilder.FolderSelector("Prop_FolderPath",          ActivityStrings.Field_CleanupFolder),
                // Что удалять — три независимых чекбокса
                PropertyBuilder.BooleanObject("Prop_DeleteFiles",           "Удалять файлы"),
                PropertyBuilder.BooleanObject("Prop_DeleteEmptyFolders",    "Удалять пустые папки"),
                PropertyBuilder.BooleanObject("Prop_DeleteFoldersWithContent","Удалять папки целиком"),
                // Временной критерий
                PropertyBuilder.Enum<FileTimeAttribute>("Prop_TimeAttribute", ActivityStrings.Field_CleanupTimeAttribute),
                PropertyBuilder.Enum<CleanupThresholdMode>("Prop_ThresholdMode", ActivityStrings.Field_CleanupThresholdMode),
                PropertyBuilder.Script<int>("Prop_OlderThanDays",          ActivityStrings.Field_CleanupOlderThanDays),
                PropertyBuilder.Script<DateTime>("Prop_ThresholdDate",     ActivityStrings.Field_CleanupThresholdDate),
                // Фильтры файлов
                PropertyBuilder.Script<string>("Prop_FilePattern",         ActivityStrings.Field_CleanupFilePattern),
                PropertyBuilder.BooleanObject("Prop_Recursive",            ActivityStrings.Field_CleanupRecursive),
                PropertyBuilder.Script<long>("Prop_MinSizeBytes",          ActivityStrings.Field_CleanupMinSize),
                PropertyBuilder.Script<int>("Prop_MaxItems",               ActivityStrings.Field_CleanupMaxItems),
                // Безопасность
                PropertyBuilder.BooleanObject("Prop_DryRun",              ActivityStrings.Field_CleanupDryRun),
                PropertyBuilder.BooleanObject("Prop_IgnoreErrors",        ActivityStrings.Field_CleanupIgnoreErrors),
                // Выходные
                PropertyBuilder.Variable<int>("Prop_DeletedCount",        ActivityStrings.Field_CleanupDeletedCount),
                PropertyBuilder.Variable<List<string>>("Prop_DeletedPaths", ActivityStrings.Field_CleanupDeletedPaths),
                PropertyBuilder.Variable<long>("Prop_FreedBytes",         ActivityStrings.Field_CleanupFreedBytes),
                PropertyBuilder.Variable<List<string>>("Prop_Errors",     ActivityStrings.Field_CleanupErrors)
            };

            InitClass(container);

            this.Prop_DeleteFiles              = true;
            this.Prop_DeleteEmptyFolders       = false;
            this.Prop_DeleteFoldersWithContent = false;
            this.Prop_TimeAttribute            = FileTimeAttribute.LastWriteTime;
            this.Prop_ThresholdMode            = CleanupThresholdMode.OlderThanDays;
            this.Prop_OlderThanDays            = "30";
            this.Prop_FilePattern              = string.Empty;
            this.Prop_Recursive                = false;
            this.Prop_MinSizeBytes             = "0";
            this.Prop_MaxItems                 = "0";
            this.Prop_DryRun                   = false;
            this.Prop_IgnoreErrors             = true;
        }

        // =====================================================================
        // Выполнение
        // =====================================================================

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string folderPath = GetPropertyValue<string>(
                    this.Prop_FolderPath, nameof(Prop_FolderPath), sd);

                if (string.IsNullOrWhiteSpace(folderPath))
                    return Fail(ActivityStrings.Error_CleanupFolderRequired);

                if (!Directory.Exists(folderPath))
                    return Fail($"{ActivityStrings.Error_CleanupFolderNotFound}: {folderPath}");
                
                DateTime? thresholdDate = null;
                int olderThanDays = 0;

                if (this.Prop_ThresholdMode == CleanupThresholdMode.OlderThanDays
                 || this.Prop_ThresholdMode == CleanupThresholdMode.Both)
                {
                    object daysObj = GetPropertyValue(
                        this.Prop_OlderThanDays, nameof(Prop_OlderThanDays), sd);
                    if (!int.TryParse(daysObj?.ToString(), out olderThanDays) || olderThanDays < 0)
                        return Fail(ActivityStrings.Error_CleanupDaysRequired);
                }

                if (this.Prop_ThresholdMode == CleanupThresholdMode.BeforeDate
                 || this.Prop_ThresholdMode == CleanupThresholdMode.Both)
                {
                    object dateObj = GetPropertyValue(
                        this.Prop_ThresholdDate, nameof(Prop_ThresholdDate), sd);
                    if (!DateTime.TryParse(dateObj?.ToString(), out DateTime date))
                        return Fail(ActivityStrings.Error_CleanupDateRequired);
                    thresholdDate = date;
                }

                object minSizeObj = GetPropertyValue(
                    this.Prop_MinSizeBytes, nameof(Prop_MinSizeBytes), sd);
                long.TryParse(minSizeObj?.ToString(), out long minSizeBytes);

                object maxItemsObj = GetPropertyValue(
                    this.Prop_MaxItems, nameof(Prop_MaxItems), sd);
                int.TryParse(maxItemsObj?.ToString(), out int maxItems);

                string filePattern = GetPropertyValue<string>(
                    this.Prop_FilePattern, nameof(Prop_FilePattern), sd);

                var result = FileCleanupLogic.Cleanup(
                    folderPath,
                    deleteFiles: this.Prop_DeleteFiles,
                    deleteEmptyFolders: this.Prop_DeleteEmptyFolders,
                    deleteFoldersWithContent: this.Prop_DeleteFoldersWithContent,
                    timeAttribute: this.Prop_TimeAttribute,
                    thresholdMode: this.Prop_ThresholdMode,
                    olderThanDays: olderThanDays,
                    thresholdDate: thresholdDate,
                    filePattern: filePattern,
                    recursive: this.Prop_Recursive,
                    minSizeBytes: minSizeBytes,
                    maxItems: maxItems,
                    dryRun: this.Prop_DryRun,
                    ignoreErrors: this.Prop_IgnoreErrors);

                if (!result.IsSuccess)
                    return Fail(result.ErrorMessage);

                SetVariableValue(this.Prop_DeletedCount, result.DeletedCount, sd);
                SetVariableValue(this.Prop_DeletedPaths, result.DeletedPaths, sd);
                SetVariableValue(this.Prop_FreedBytes, result.FreedBytes, sd);
                SetVariableValue(this.Prop_Errors, result.Errors, sd);

                string dryRunPrefix = this.Prop_DryRun ? "[DryRun] " : string.Empty;
                string freedStr = FileCleanupLogic.FormatBytes(result.FreedBytes);
                string errStr = result.Errors.Count > 0
                    ? $", ошибок: {result.Errors.Count}"
                    : string.Empty;

                return new ExecutionResult
                {
                    IsSuccess      = true,
                    SuccessMessage = $"{dryRunPrefix}Удалено: {result.DeletedCount} объектов, " +
                                     $"освобождено: {freedStr}{errStr}"
                };
            }
            catch (System.IO.IOException ex)
            {
                return Fail($"Ошибка ввода-вывода: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Fail($"Нет доступа: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка очистки: {ex.Message}");
            }
        }

        // =====================================================================
        // Методы обработки файлов и папок
        // =====================================================================

        /// <summary>
        /// Обрабатывает файлы по списку масок: проверяет критерии и удаляет.
        /// Поддерживает несколько масок — файл включается если подходит хотя бы под одну.
        /// </summary>
        private static void ProcessFiles(
            string folderPath, List<string> patterns, SearchOption searchOption,
            Func<FileSystemInfo, DateTime> getTime,
            DateTime? thresholdDays, DateTime? thresholdDate,
            long minSizeBytes, int maxItems,
            bool dryRun, bool ignoreErrors,
            List<string> deletedPaths, List<string> errors, ref long freedBytes)
        {
            // Собираем файлы по всем маскам через LINQ SelectMany + Distinct
            // Distinct нужен если один файл подходит под несколько масок (*.log и log*.*)
            IEnumerable<string> files;
            try
            {
                files = patterns
                    .SelectMany(p =>
                    {
                        try { return Directory.GetFiles(folderPath, p, searchOption); }
                        catch { return Array.Empty<string>(); }
                    })
                    .Distinct(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                errors.Add($"{folderPath}: {ex.Message}");
                return;
            }

            // Фильтруем через LINQ по временному критерию и размеру
            var candidates = files
                .Select(f => new FileInfo(f))
                .Where(fi => { try { return fi.Exists; } catch { return false; } })
                .Where(fi => MeetsTimeThreshold(fi, getTime, thresholdDays, thresholdDate))
                .Where(fi => minSizeBytes <= 0 || fi.Length >= minSizeBytes)
                .OrderBy(fi => getTime(fi)) // сначала самые старые
                .AsEnumerable();

            if (maxItems > 0)
                candidates = candidates.Take(maxItems);

            foreach (FileInfo fi in candidates)
            {
                try
                {
                    long size = fi.Length;
                    if (!dryRun)
                        fi.Delete();
                    deletedPaths.Add(fi.FullName);
                    freedBytes += size;
                }
                catch (Exception ex)
                {
                    if (!ignoreErrors) throw;
                    errors.Add($"{fi.FullName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Удаляет пустые папки удовлетворяющие временному критерию.
        /// Обходит снизу вверх — сначала вложенные, потом родительские.
        /// </summary>
        private static void ProcessEmptyFolders(
            string rootPath, Func<FileSystemInfo, DateTime> getTime,
            DateTime? thresholdDays, DateTime? thresholdDate,
            bool ignoreErrors,
            List<string> deletedPaths, List<string> errors)
        {
            // Получаем все подпапки — снизу вверх через OrderByDescending по глубине
            // Это гарантирует что вложенные пустые папки удаляются первыми,
            // после чего их родители тоже могут стать пустыми
            var subDirs = Directory
                .GetDirectories(rootPath, "*", SearchOption.AllDirectories)
                .Select(d => new DirectoryInfo(d))
                .Where(di => MeetsTimeThreshold(di, getTime, thresholdDays, thresholdDate))
                // Сортируем по убыванию глубины — сначала самые вложенные
                .OrderByDescending(di => di.FullName.Count(c => c == Path.DirectorySeparatorChar))
                .ToList();

            foreach (DirectoryInfo di in subDirs)
            {
                try
                {
                    // Проверяем что папка пуста после возможного удаления вложенных
                    if (di.Exists && !di.GetFileSystemInfos().Any())
                    {
                        di.Delete(false);
                        deletedPaths.Add(di.FullName);
                    }
                }
                catch (Exception ex)
                {
                    if (!ignoreErrors) throw;
                    errors.Add($"{di.FullName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Удаляет папки целиком с содержимым.
        /// Временной критерий применяется к самой папке, а не к её содержимому.
        /// </summary>
        private static void ProcessFolders(
            string rootPath, Func<FileSystemInfo, DateTime> getTime,
            DateTime? thresholdDays, DateTime? thresholdDate,
            int maxItems, bool dryRun, bool ignoreErrors,
            List<string> deletedPaths, List<string> errors, ref long freedBytes)
        {
            IEnumerable<DirectoryInfo> candidates = Directory
                .GetDirectories(rootPath, "*", SearchOption.TopDirectoryOnly)
                .Select(d => new DirectoryInfo(d))
                .Where(di => MeetsTimeThreshold(di, getTime, thresholdDays, thresholdDate))
                .OrderBy(di => getTime(di)); // сначала самые старые

            if (maxItems > 0)
                candidates = candidates.Take(maxItems);

            foreach (DirectoryInfo di in candidates)
            {
                try
                {
                    // Считаем размер содержимого до удаления
                    long folderSize = GetDirectorySize(di);

                    if (!dryRun)
                        di.Delete(recursive: true);

                    deletedPaths.Add(di.FullName);
                    freedBytes += folderSize;
                }
                catch (Exception ex)
                {
                    if (!ignoreErrors) throw;
                    errors.Add($"{di.FullName}: {ex.Message}");
                }
            }
        }

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>
        /// Проверяет соответствие объекта временному критерию.
        /// ThresholdMode.Both требует выполнения обоих условий одновременно.
        /// </summary>
        private static bool MeetsTimeThreshold(
            FileSystemInfo fsi,
            Func<FileSystemInfo, DateTime> getTime,
            DateTime? thresholdDays,
            DateTime? thresholdDate)
        {
            DateTime fileTime = getTime(fsi);

            bool meetsdays = thresholdDays == null || fileTime < thresholdDays.Value;
            bool meetsDate = thresholdDate == null || fileTime < thresholdDate.Value;

            // Если оба порога заданы — требуем выполнения обоих (режим Both)
            // Если задан только один — проверяем только его
            if (thresholdDays != null && thresholdDate != null)
                return meetsdays && meetsDate; // Both: И

            return meetsdays && meetsDate; // одно из условий всегда true при null
        }

        /// <summary>
        /// Рекурсивно вычисляет суммарный размер содержимого папки в байтах.
        /// Использует LINQ для компактного суммирования.
        /// </summary>
        private static long GetDirectorySize(DirectoryInfo di)
        {
            try
            {
                return di.GetFiles("*", SearchOption.AllDirectories)
                    .Sum(fi => fi.Length);
            }
            catch
            {
                return 0; // нет доступа — размер неизвестен
            }
        }

        /// <summary>
        /// Форматирует размер в байтах в читаемый вид (KB, MB, GB).
        /// Использует словарь порогов вместо вложенных if.
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            var thresholds = new Dictionary<long, string>
            {
                { 1024L * 1024 * 1024, "ГБ" },
                { 1024L * 1024,        "МБ" },
                { 1024L,               "КБ" }
            };

            foreach (var kv in thresholds)
            {
                if (bytes >= kv.Key)
                    return $"{bytes / (double)kv.Key:F1} {kv.Value}";
            }

            return $"{bytes} байт";
        }

        /// <summary>
        /// Разбирает строку масок файлов "*.tmp, *.log; report_*.pdf" в список.
        /// Разделители: запятая, точка с запятой, пробел.
        /// Пустая строка или только пробелы → ["*"] (все файлы).
        /// </summary>
        private static List<string> ParseFilePatterns(string patternsString)
        {
            if (string.IsNullOrWhiteSpace(patternsString))
                return new List<string> { "*" };

            var result = patternsString
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return result.Count > 0 ? result : new List<string> { "*" };
        }

        private static ExecutionResult Fail(string msg) =>
            new ExecutionResult { IsSuccess = false, ErrorMessage = msg };

        // =====================================================================
        // Валидация
        // =====================================================================

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_FolderPath))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_FolderPath),
                    Error        = ActivityStrings.Error_CleanupFolderRequired
                });

            // Хотя бы один тип объектов должен быть выбран
            if (!this.Prop_DeleteFiles
                && !this.Prop_DeleteEmptyFolders
                && !this.Prop_DeleteFoldersWithContent)
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_DeleteFiles),
                    Error        = "Выберите хотя бы один тип объектов для удаления"
                });

            bool needDays = this.Prop_ThresholdMode == CleanupThresholdMode.OlderThanDays
                         || this.Prop_ThresholdMode == CleanupThresholdMode.Both;
            if (needDays && string.IsNullOrWhiteSpace(this.Prop_OlderThanDays))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_OlderThanDays),
                    Error        = ActivityStrings.Error_CleanupDaysRequired
                });

            bool needDate = this.Prop_ThresholdMode == CleanupThresholdMode.BeforeDate
                         || this.Prop_ThresholdMode == CleanupThresholdMode.Both;
            if (needDate && string.IsNullOrWhiteSpace(this.Prop_ThresholdDate))
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_ThresholdDate),
                    Error        = ActivityStrings.Error_CleanupDateRequired
                });

            return ret;
        }
    }
}
