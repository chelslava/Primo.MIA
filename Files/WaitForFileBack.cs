using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для ожидания появления файла в директории:
    /// - Два режима: ожидание нового файла или любого файла по маске
    /// - Три типа фильтрации: точное совпадение, wildcard, regex
    /// - Ожидание стабильности размера файла (завершение записи)
    /// - Возврат информации о найденном файле
    /// </summary>
    public class WaitForFileBack : PrimoComponentSimple<WaitForFile>
    {
        public override string GroupName
        {
            get => ActivityCategories.Utilities;
            protected set { }
        }

        // ============== INPUT PROPERTIES ==============

        private WaitFileMode _waitMode = WaitFileMode.WaitForNewFile;
        /// <summary>
        /// Режим работы активности
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_WaitMode)]
        public WaitFileMode WaitMode
        {
            get => this._waitMode;
            set { this._waitMode = value; this.InvokePropertyChanged(this, "WaitMode"); }
        }

        private string prop_DirectoryPath;
        /// <summary>
        /// Путь к директории для мониторинга
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_DirectoryPath)]
        public string Prop_DirectoryPath
        {
            get { return this.prop_DirectoryPath; }
            set { this.prop_DirectoryPath = value; this.InvokePropertyChanged(this, "Prop_DirectoryPath"); }
        }

        private FileFilterType _filterType = FileFilterType.Wildcard;
        /// <summary>
        /// Тип фильтрации файлов
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_FilterType)]
        public FileFilterType FilterType
        {
            get => this._filterType;
            set { this._filterType = value; this.InvokePropertyChanged(this, "FilterType"); }
        }

        private string prop_FilePattern;
        /// <summary>
        /// Маска/паттерн файла
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_FilePattern)]
        public string Prop_FilePattern
        {
            get { return this.prop_FilePattern; }
            set { this.prop_FilePattern = value; this.InvokePropertyChanged(this, "Prop_FilePattern"); }
        }

        private string prop_Timeout;
        /// <summary>
        /// Таймаут ожидания в секундах
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_Timeout)]
        public string Prop_Timeout
        {
            get { return this.prop_Timeout; }
            set { this.prop_Timeout = value; this.InvokePropertyChanged(this, "Prop_Timeout"); }
        }

        private string prop_CheckInterval;
        /// <summary>
        /// Интервал проверки в миллисекундах
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CheckInterval)]
        public string Prop_CheckInterval
        {
            get { return this.prop_CheckInterval; }
            set { this.prop_CheckInterval = value; this.InvokePropertyChanged(this, "Prop_CheckInterval"); }
        }

        private bool _waitForStability = false;
        /// <summary>
        /// Ожидать стабильности размера файла (файл полностью записан)
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_WaitForStability)]
        public bool Prop_WaitForStability
        {
            get { return this._waitForStability; }
            set { this._waitForStability = value; this.InvokePropertyChanged(this, "Prop_WaitForStability"); }
        }

        private string prop_StabilityTimeout;
        /// <summary>
        /// Время стабильности размера файла в миллисекундах
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_StabilityTimeout)]
        public string Prop_StabilityTimeout
        {
            get { return this.prop_StabilityTimeout; }
            set { this.prop_StabilityTimeout = value; this.InvokePropertyChanged(this, "Prop_StabilityTimeout"); }
        }

        private bool _throwOnTimeout = false;
        /// <summary>
        /// Выбрасывать исключение при таймауте
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_ThrowOnTimeout)]
        public bool Prop_ThrowOnTimeout
        {
            get { return this._throwOnTimeout; }
            set { this._throwOnTimeout = value; this.InvokePropertyChanged(this, "Prop_ThrowOnTimeout"); }
        }

        // ============== OUTPUT PROPERTIES ==============

        private string prop_FileFound;
        /// <summary>
        /// Флаг обнаружения файла
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_FileFound)]
        public string Prop_FileFound
        {
            get => prop_FileFound;
            set
            {
                prop_FileFound = value;
                this.InvokePropertyChanged(this, "Prop_FileFound");
            }
        }

        private string prop_FilePath;
        /// <summary>
        /// Полный путь к найденному файлу
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_FilePath)]
        public string Prop_FilePath
        {
            get => prop_FilePath;
            set
            {
                prop_FilePath = value;
                this.InvokePropertyChanged(this, "Prop_FilePath");
            }
        }

        private string prop_FileName;
        /// <summary>
        /// Имя найденного файла
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_FileName)]
        public string Prop_FileName
        {
            get => prop_FileName;
            set
            {
                prop_FileName = value;
                this.InvokePropertyChanged(this, "Prop_FileName");
            }
        }

        private string prop_FileSize;
        /// <summary>
        /// Размер найденного файла в байтах
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(long))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_FileSize)]
        public string Prop_FileSize
        {
            get => prop_FileSize;
            set
            {
                prop_FileSize = value;
                this.InvokePropertyChanged(this, "Prop_FileSize");
            }
        }

        private string prop_WaitTime;
        /// <summary>
        /// Время ожидания в миллисекундах
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(long))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_WaitTime)]
        public string Prop_WaitTime
        {
            get => prop_WaitTime;
            set
            {
                prop_WaitTime = value;
                this.InvokePropertyChanged(this, "Prop_WaitTime");
            }
        }

        /// <summary>
        /// Конструктор компонента
        /// </summary>
        public WaitForFileBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Ожидание файла";
            sdkComponentHelp = @"Компонент ""Ожидание файла""
Компонент для ожидания появления файла в директории с поддержкой различных режимов фильтрации и мониторинга.
                
Основные:
Режим ожидания: [WaitFileMode] Режим работы компонента
WaitForNewFile - Ожидать появления нового файла (которого не было при запуске активности)
WaitForAnyFile - Ожидать файл независимо от того, существовал он ранее или нет
Путь к директории*: [String] Полный путь к директории для мониторинга (например: C:\Downloads)
Тип фильтрации: [FileFilterType] Способ фильтрации файлов по имени
Exact - Точное совпадение имени файла
Wildcard - Использование wildcard символов (* и ?). Например: *.xlsx, report_*.pdf
Regex - Использование регулярных выражений. Например: ^report_\d{8}\.xlsx$
Маска файла*: [String] Маска или паттерн для поиска файлов в зависимости от выбранного типа фильтрации
Для Exact: report.xlsx
Для Wildcard: *.txt, data_*.csv
Для Regex: ^file_\d{4}\.log$

Настройки:
Таймаут (сек)*: [Int32] Максимальное время ожидания файла в секундах. По умолчанию: 30
Интервал проверки (мс)*: [Int32] Интервал между проверками наличия файла в миллисекундах. По умолчанию: 500
Ожидать завершения записи: [Boolean] Если включено, компонент будет ждать стабилизации размера файла (файл полностью записан). По умолчанию: false
Время стабильности (мс)*: [Int32] Время в миллисекундах, в течение которого размер файла должен оставаться неизменным для считывания файла полностью записанным. По умолчанию: 2000
Ошибка при таймауте: [Boolean] Если включено, при достижении таймаута будет выброшено исключение TimeoutException. По умолчанию: false

Выходные данные:
Файл найден: [Boolean] Флаг успешного обнаружения файла. true - файл найден, false - таймаут истек
Путь к файлу: [String] Полный путь к найденному файлу (например: C:\Downloads\report_20250215.xlsx). Пустая строка, если файл не найден
Имя файла: [String] Имя найденного файла без пути (например: report_20250215.xlsx). Пустая строка, если файл не найден
Размер файла (байт): [Int64] Размер найденного файла в байтах. 0, если файл не найден
Время ожидания (мс): [Int64] Фактическое время ожидания в миллисекундах";

            sdkComponentIcon = ActivityIcons.FileWait;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<WaitFileMode>("WaitMode", "Режим работы: ожидать новый файл или любой файл по маске"),
                PropertyBuilder.FolderSelector("Prop_DirectoryPath", "Путь к директории для мониторинга появления файлов"),
                PropertyBuilder.Enum<FileFilterType>("FilterType", "Тип фильтрации: точное совпадение, wildcard (*.txt) или regex паттерн"),
                PropertyBuilder.Script<string>("Prop_FilePattern", "Маска/паттерн файла в зависимости от выбранного типа фильтрации"),
                PropertyBuilder.Script<int>("Prop_Timeout", "Таймаут ожидания в секундах (по умолчанию 30)"),
                PropertyBuilder.Script<int>("Prop_CheckInterval", "Интервал проверки в миллисекундах (по умолчанию 500)"),
                PropertyBuilder.Script<int>("Prop_StabilityTimeout", "Время стабильности размера файла в миллисекундах (по умолчанию 2000)"),
                PropertyBuilder.Variable<bool>("Prop_FileFound", "Флаг обнаружения файла"),
                PropertyBuilder.Variable<string>("Prop_FilePath", "Полный путь к найденному файлу"),
                PropertyBuilder.Variable<string>("Prop_FileName", "Имя найденного файла"),
                PropertyBuilder.Variable<long>("Prop_FileSize", "Размер найденного файла в байтах"),
                PropertyBuilder.Variable<long>("Prop_WaitTime", "Фактическое время ожидания в миллисекундах")
            };

            InitClass(container);

            // Установка значений по умолчанию
            this.Prop_FilePattern = "\"*\"";
            this.Prop_Timeout = "30";
            this.Prop_CheckInterval = "500";
            this.Prop_StabilityTimeout = "2000";
        }

        /// <summary>
        /// Основное действие компонента
        /// </summary>
        public override ExecutionResult SimpleAction(ScriptingData sd)
        {
            try
            {
                var parameters = GetAndValidateParameters(sd);
                var result = new WaitForFileLogic().WaitForFile(
                    parameters.DirectoryPath,
                    parameters.FilePattern,
                    parameters.FilterType,
                    parameters.Mode,
                    parameters.TimeoutMs,
                    parameters.CheckInterval,
                    parameters.WaitForStability,
                    parameters.StabilityTimeout);

                SetVariableValue(this.Prop_FileFound, result.FileFound, sd);
                SetVariableValue(this.Prop_FilePath, result.FilePath ?? string.Empty, sd);
                SetVariableValue(this.Prop_FileName, result.FileName ?? string.Empty, sd);
                SetVariableValue(this.Prop_FileSize, result.FileSize, sd);
                SetVariableValue(this.Prop_WaitTime, result.WaitTimeMs, sd);

                if (result.FileFound)
                {
                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = $"Файл обнаружен: {result.FileName}"
                    };
                }

                if (parameters.ThrowOnTimeout)
                {
                    throw new TimeoutException(
                        $"Таймаут ожидания файла истек. " +
                        $"Директория: {parameters.DirectoryPath}, " +
                        $"Маска: {parameters.FilePattern}, " +
                        $"Таймаут: {parameters.TimeoutMs / 1000} сек.");
                }

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Таймаут ожидания файла истек после {parameters.TimeoutMs / 1000} секунд"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult()
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка при выполнении активности: {ex?.Message}"
                };
            }
        }

        #region Вспомогательные классы и методы

        /// <summary>
        /// Параметры для поиска файла
        /// </summary>
        private class SearchParameters
        {
            public string DirectoryPath { get; set; }
            public string FilePattern { get; set; }
            public int TimeoutMs { get; set; }
            public int CheckInterval { get; set; }
            public bool WaitForStability { get; set; }
            public int StabilityTimeout { get; set; }
            public bool ThrowOnTimeout { get; set; }
            public WaitFileMode Mode { get; set; }
            public FileFilterType FilterType { get; set; }
        }

        /// <summary>
        /// Получает и валидирует все входные параметры
        /// </summary>
        private SearchParameters GetAndValidateParameters(ScriptingData sd)
        {
            // Получение параметров
            string directoryPath = GetPropertyValue<string>(this.Prop_DirectoryPath, "Prop_DirectoryPath", sd);
            string filePattern = GetPropertyValue<string>(this.Prop_FilePattern, "Prop_FilePattern", sd);
            int timeoutSec = Convert.ToInt32(GetPropertyValue(this.Prop_Timeout, "Prop_Timeout", sd));
            int checkInterval = Convert.ToInt32(GetPropertyValue(this.Prop_CheckInterval, "Prop_CheckInterval", sd));
            int stabilityTimeout = Convert.ToInt32(GetPropertyValue(this.Prop_StabilityTimeout, "Prop_StabilityTimeout", sd));

            // Валидация строковых параметров
            ValidateStringParameter(directoryPath, "Путь к директории");
            ValidateStringParameter(filePattern, "Маска файла");
            ValidateDirectoryExists(directoryPath);

            // Валидация числовых параметров
            ValidatePositiveNumber(timeoutSec, "Таймаут");
            ValidatePositiveNumber(checkInterval, "Интервал проверки");
            ValidatePositiveNumber(stabilityTimeout, "Время стабильности");

            return new SearchParameters
            {
                DirectoryPath = directoryPath,
                FilePattern = filePattern,
                TimeoutMs = timeoutSec * 1000,
                CheckInterval = checkInterval,
                WaitForStability = this.Prop_WaitForStability,
                StabilityTimeout = stabilityTimeout,
                ThrowOnTimeout = this.Prop_ThrowOnTimeout,
                Mode = this.WaitMode,
                FilterType = this.FilterType
            };
        }

        /// <summary>
        /// Валидация строкового параметра
        /// </summary>
        private void ValidateStringParameter(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{parameterName} не может быть пустым");
            }
        }

        /// <summary>
        /// Проверка существования директории
        /// </summary>
        private void ValidateDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Директория не найдена: {directoryPath}");
            }
        }

        /// <summary>
        /// Валидация положительного числа
        /// </summary>
        private void ValidatePositiveNumber(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentException($"{parameterName} должен быть больше нуля");
            }
        }

        /// <summary>
        /// Поиск файла в зависимости от режима работы
        /// </summary>
        private FileInfo FindFile(SearchParameters parameters)
        {
            if (parameters.Mode == WaitFileMode.WaitForNewFile)
            {
                return FindNewFile(parameters);
            }
            else
            {
                return FindAnyFile(parameters);
            }
        }

        /// <summary>
        /// Поиск нового файла (которого не было при запуске)
        /// </summary>
        private FileInfo FindNewFile(SearchParameters parameters)
        {
            // Получение списка существующих файлов
            var existingFiles = GetExistingFiles(parameters.DirectoryPath);

            // Поиск в зависимости от типа фильтрации
            return ExecuteSearch(
                parameters,
                (pattern, timeout, interval) => SearchForFile(
                    parameters.DirectoryPath,
                    pattern,
                    parameters.FilterType,
                    timeout,
                    interval,
                    file => !existingFiles.Contains(file.FullName)
                )
            );
        }

        /// <summary>
        /// Поиск любого файла по маске
        /// </summary>
        private FileInfo FindAnyFile(SearchParameters parameters)
        {
            return ExecuteSearch(
                parameters,
                (pattern, timeout, interval) => SearchForFile(
                    parameters.DirectoryPath,
                    pattern,
                    parameters.FilterType,
                    timeout,
                    interval,
                    file => true // Любой файл подходит
                )
            );
        }

        /// <summary>
        /// Получает набор существующих файлов в директории
        /// </summary>
        private HashSet<string> GetExistingFiles(string directoryPath)
        {
            var dirInfo = new DirectoryInfo(directoryPath);
            return new HashSet<string>(
                dirInfo.GetFiles().Select(f => f.FullName),
                StringComparer.OrdinalIgnoreCase
            );
        }

        /// <summary>
        /// Выполняет поиск с учетом стабильности файла
        /// </summary>
        private FileInfo ExecuteSearch(
            SearchParameters parameters,
            Func<string, int, int, FileInfo> searchFunc)
        {
            var file = searchFunc(
                parameters.FilePattern,
                parameters.TimeoutMs,
                parameters.CheckInterval
            );

            if (file != null && parameters.WaitForStability)
            {
                int remainingTimeout = parameters.TimeoutMs;
                if (!WaitForFileStability(file, parameters.StabilityTimeout, remainingTimeout, parameters.CheckInterval))
                {
                    return null;
                }
            }

            return file;
        }

        /// <summary>
        /// Универсальный метод поиска файла с фильтром
        /// </summary>
        private FileInfo SearchForFile(
            string directoryPath,
            string pattern,
            FileFilterType filterType,
            int timeoutMs,
            int checkInterval,
            Func<FileInfo, bool> additionalFilter)
        {
            var startTime = DateTime.UtcNow;
            var dirInfo = new DirectoryInfo(directoryPath);

            // Создание фильтра файлов на основе типа
            Func<FileInfo, bool> fileFilter = CreateFileFilter(pattern, filterType);

            while (true)
            {
                // Проверка таймаута
                if ((DateTime.UtcNow - startTime).TotalMilliseconds > timeoutMs)
                {
                    return null;
                }

                // Поиск подходящих файлов
                var matchedFiles = dirInfo.GetFiles()
                    .Where(f => fileFilter(f) && additionalFilter(f))
                    .OrderBy(f => f.CreationTime)
                    .ToList();

                if (matchedFiles.Any())
                {
                    return matchedFiles.First();
                }

                Thread.Sleep(checkInterval);
            }
        }

        /// <summary>
        /// Создает функцию фильтрации файлов на основе типа фильтрации
        /// </summary>
        private Func<FileInfo, bool> CreateFileFilter(string pattern, FileFilterType filterType)
        {
            switch (filterType)
            {
                case FileFilterType.Exact:
                    return file => file.Name.Equals(pattern, StringComparison.OrdinalIgnoreCase);

                case FileFilterType.Wildcard:
                    return CreateWildcardFilter(pattern);

                case FileFilterType.Regex:
                    return CreateRegexFilter(pattern);

                default:
                    throw new ArgumentException($"Неизвестный тип фильтрации: {filterType}");
            }
        }

        /// <summary>
        /// Создает wildcard фильтр
        /// </summary>
        private Func<FileInfo, bool> CreateWildcardFilter(string pattern)
        {
            // Преобразование wildcard паттерна в regex
            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return file => regex.IsMatch(file.Name);
        }

        /// <summary>
        /// Создает regex фильтр
        /// </summary>
        private Func<FileInfo, bool> CreateRegexFilter(string pattern)
        {
            try
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                return file => regex.IsMatch(file.Name);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Некорректный regex паттерн: {pattern}. Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Ожидает стабильности размера файла (завершения записи)
        /// </summary>
        private bool WaitForFileStability(
            FileInfo fileInfo,
            int stabilityTimeout,
            int remainingTimeout,
            int checkInterval)
        {
            var stabilityStart = DateTime.UtcNow;
            long previousSize = -1;

            while (true)
            {
                fileInfo.Refresh();

                // Проверка существования файла
                if (!fileInfo.Exists)
                {
                    return false;
                }

                long currentSize = fileInfo.Length;

                // Проверка общего таймаута
                if ((DateTime.UtcNow - stabilityStart).TotalMilliseconds > remainingTimeout)
                {
                    return false;
                }

                // Проверка стабильности размера
                if (currentSize != previousSize)
                {
                    previousSize = currentSize;
                    stabilityStart = DateTime.UtcNow;
                }
                else if ((DateTime.UtcNow - stabilityStart).TotalMilliseconds >= stabilityTimeout)
                {
                    return true;
                }

                Thread.Sleep(checkInterval);
            }
        }

        /// <summary>
        /// Обрабатывает результат поиска и устанавливает выходные параметры
        /// </summary>
        private ExecutionResult ProcessResult(
            FileInfo foundFile,
            long waitTime,
            SearchParameters parameters,
            ScriptingData sd)
        {
            if (foundFile != null)
            {
                // Файл найден
                foundFile.Refresh();
                SetOutputParameters(foundFile, waitTime, true, sd);

                return new ExecutionResult()
                {
                    IsSuccess = true,
                    SuccessMessage = $"Файл обнаружен: {foundFile.Name}"
                };
            }
            else
            {
                // Таймаут
                SetOutputParameters(null, waitTime, false, sd);

                if (parameters.ThrowOnTimeout)
                {
                    throw new TimeoutException(
                        $"Таймаут ожидания файла истек. " +
                        $"Директория: {parameters.DirectoryPath}, " +
                        $"Маска: {parameters.FilePattern}, " +
                        $"Таймаут: {parameters.TimeoutMs / 1000} сек."
                    );
                }

                return new ExecutionResult()
                {
                    IsSuccess = true,
                    SuccessMessage = $"Таймаут ожидания файла истек после {parameters.TimeoutMs / 1000} секунд"
                };
            }
        }

        /// <summary>
        /// Устанавливает выходные параметры
        /// </summary>
        private void SetOutputParameters(
            FileInfo file,
            long waitTime,
            bool found,
            ScriptingData sd)
        {
            SetVariableValue(this.Prop_FileFound, found, sd);
            SetVariableValue(this.Prop_FilePath, file?.FullName ?? string.Empty, sd);
            SetVariableValue(this.Prop_FileName, file?.Name ?? string.Empty, sd);
            SetVariableValue(this.Prop_FileSize, file?.Length ?? 0L, sd);
            SetVariableValue(this.Prop_WaitTime, waitTime, sd);
        }

        #endregion

        /// <summary>
        /// Проверка корректности введенных данных
        /// </summary>
        public override ValidationResult Validate()
        {
            ValidationResult ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_DirectoryPath, ActivityStrings.Field_DirectoryPath, ActivityStrings.Error_DirectoryRequired);
            ret.ValidateRequired(this.Prop_FilePattern, ActivityStrings.Field_FilePattern, ActivityStrings.Error_FilePatternRequired);
            ret.ValidateRequired(this.Prop_Timeout, ActivityStrings.Field_Timeout, ActivityStrings.Error_TimeoutRequired);
            ret.ValidateRequired(this.Prop_CheckInterval, ActivityStrings.Field_CheckInterval, ActivityStrings.Error_CheckIntervalRequired);
            ret.ValidateRequired(this.Prop_StabilityTimeout, ActivityStrings.Field_StabilityTimeout, ActivityStrings.Error_StabilityTimeoutRequired);
            return ret;
        }
    }
}
