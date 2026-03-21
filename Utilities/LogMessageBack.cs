using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.Enums;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для логирования сообщений с расширенным функционалом:
    /// - Настраиваемые уровни логирования (Debug, Info, Warning, Error, Critical)
    /// - Гибкий вывод: файл, консоль или оба варианта
    /// - Автоматическая ротация логов по дате, времени или размеру
    /// - Настраиваемый формат имени файла с поддержкой плейсхолдеров
    /// - Форматирование сообщений с timestamp и уровнем
    /// - Поддержка структурированного логирования
    /// - Thread-safe запись в файл
    /// </summary>
    public class LogMessageBack : PrimoComponentTO<LogMessage>
    {
        /// <inheritdoc/>
        public override string GroupName
        {
            get => ActivityCategories.Utilities;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 10000;
            set { }
        }

        // Статический объект для синхронизации записи в файл
        private static readonly object _fileLock = new object();

        // ============== INPUT PROPERTIES ==============

        private string prop_Message;
        /// <summary>
        /// Сообщение для логирования
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Message)]
        public string Prop_Message
        {
            get { return this.prop_Message; }
            set { this.prop_Message = value; this.InvokePropertyChanged(this, "Prop_Message"); }
        }

        private LogLevel _logLevel = LogLevel.Info;
        /// <summary>
        /// Уровень логирования
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Level)]
        public LogLevel Level
        {
            get => this._logLevel;
            set { this._logLevel = value; this.InvokePropertyChanged(this, "Level"); }
        }

        private LogOutputMode _outputMode = LogOutputMode.FileAndConsole;
        /// <summary>
        /// Режим вывода логов
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_OutputMode)]
        public LogOutputMode OutputMode
        {
            get => this._outputMode;
            set { this._outputMode = value; this.InvokePropertyChanged(this, "OutputMode"); }
        }

        private string prop_LogDirectory;
        /// <summary>
        /// Путь к директории для файлов логов
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_File), System.ComponentModel.DisplayName(ActivityStrings.Field_LogDirectory)]
        public string Prop_LogDirectory
        {
            get { return this.prop_LogDirectory; }
            set { this.prop_LogDirectory = value; this.InvokePropertyChanged(this, "Prop_LogDirectory"); }
        }

        private LogFileNameTemplate _fileNameTemplate = LogFileNameTemplate.WithDate;
        /// <summary>
        /// Шаблон имени файла
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_File), System.ComponentModel.DisplayName(ActivityStrings.Field_FileNameTemplate)]
        public LogFileNameTemplate FileNameTemplate
        {
            get => this._fileNameTemplate;
            set { this._fileNameTemplate = value; this.InvokePropertyChanged(this, "FileNameTemplate"); }
        }

        private string prop_BaseFileName;
        /// <summary>
        /// Базовое имя файла (без расширения)
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_File), System.ComponentModel.DisplayName(ActivityStrings.Field_BaseFileName)]
        public string Prop_BaseFileName
        {
            get { return this.prop_BaseFileName; }
            set { this.prop_BaseFileName = value; this.InvokePropertyChanged(this, "Prop_BaseFileName"); }
        }

        private string prop_FileExtension;
        /// <summary>
        /// Расширение файла
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_File), System.ComponentModel.DisplayName(ActivityStrings.Field_FileExtension)]
        public string Prop_FileExtension
        {
            get { return this.prop_FileExtension; }
            set { this.prop_FileExtension = value; this.InvokePropertyChanged(this, "Prop_FileExtension"); }
        }

        private string prop_CustomFileNamePattern;
        /// <summary>
        /// Пользовательский паттерн имени файла
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_File), System.ComponentModel.DisplayName(ActivityStrings.Field_CustomFileNamePattern)]
        public string Prop_CustomFileNamePattern
        {
            get { return this.prop_CustomFileNamePattern; }
            set { this.prop_CustomFileNamePattern = value; this.InvokePropertyChanged(this, "Prop_CustomFileNamePattern"); }
        }

        private LogRotationStrategy _rotationStrategy = LogRotationStrategy.Daily;
        /// <summary>
        /// Стратегия ротации файлов логов
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_File), System.ComponentModel.DisplayName(ActivityStrings.Field_RotationStrategy)]
        public LogRotationStrategy RotationStrategy
        {
            get => this._rotationStrategy;
            set { this._rotationStrategy = value; this.InvokePropertyChanged(this, "RotationStrategy"); }
        }

        private string prop_MaxFileSizeMB;
        /// <summary>
        /// Максимальный размер файла в МБ (для ротации по размеру)
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_File), System.ComponentModel.DisplayName(ActivityStrings.Field_MaxFileSizeMB)]
        public string Prop_MaxFileSizeMB
        {
            get { return this.prop_MaxFileSizeMB; }
            set { this.prop_MaxFileSizeMB = value; this.InvokePropertyChanged(this, "Prop_MaxFileSizeMB"); }
        }

        private bool _includeTimestamp = true;
        /// <summary>
        /// Включать timestamp в сообщение
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Format), System.ComponentModel.DisplayName(ActivityStrings.Field_IncludeTimestamp)]
        public bool Prop_IncludeTimestamp
        {
            get { return this._includeTimestamp; }
            set { this._includeTimestamp = value; this.InvokePropertyChanged(this, "Prop_IncludeTimestamp"); }
        }

        private bool _includeLevel = true;
        /// <summary>
        /// Включать уровень в сообщение
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Format), System.ComponentModel.DisplayName(ActivityStrings.Field_IncludeLevel)]
        public bool Prop_IncludeLevel
        {
            get { return this._includeLevel; }
            set { this._includeLevel = value; this.InvokePropertyChanged(this, "Prop_IncludeLevel"); }
        }

        private string prop_TimestampFormat;
        /// <summary>
        /// Формат timestamp
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Format), System.ComponentModel.DisplayName(ActivityStrings.Field_TimestampFormat)]
        public string Prop_TimestampFormat
        {
            get { return this.prop_TimestampFormat; }
            set { this.prop_TimestampFormat = value; this.InvokePropertyChanged(this, "Prop_TimestampFormat"); }
        }

        private bool _appendNewLine = true;
        /// <summary>
        /// Добавлять перенос строки после сообщения
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Format), System.ComponentModel.DisplayName(ActivityStrings.Field_AppendNewLine)]
        public bool Prop_AppendNewLine
        {
            get { return this._appendNewLine; }
            set { this._appendNewLine = value; this.InvokePropertyChanged(this, "Prop_AppendNewLine"); }
        }

        // ============== OUTPUT PROPERTIES ==============

        private string prop_ActualLogFile;
        /// <summary>
        /// Фактический путь к записанному файлу лога
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ActualLogFile)]
        public string Prop_ActualLogFile
        {
            get => prop_ActualLogFile;
            set
            {
                prop_ActualLogFile = value;
                this.InvokePropertyChanged(this, "Prop_ActualLogFile");
            }
        }

        private string prop_FormattedMessage;
        /// <summary>
        /// Отформатированное сообщение
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_FormattedMessage)]
        public string Prop_FormattedMessage
        {
            get => prop_FormattedMessage;
            set
            {
                prop_FormattedMessage = value;
                this.InvokePropertyChanged(this, "Prop_FormattedMessage");
            }
        }

        /// <summary>
        /// Конструктор компонента
        /// </summary>
        public LogMessageBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Логирование";
            sdkComponentHelp = @"Компонент ""Логирование"" (LogMessageBack)
Активность для расширенного логирования сообщений в процессах Primo RPA. Поддерживает настраиваемые уровни важности, гибкий вывод (в отдельный файл, в лог Primo или одновременно), автоматическую ротацию файлов, а также детальное форматирование как самого сообщения, так и имени файла лога.

Основные
Сообщение*: [String] Текст сообщения, который будет записан в лог.
Уровень: [LogLevel] Степень важности сообщения. Определяет цвет в логе Primo и может использоваться для фильтрации.
Варианты: Debug, Info (по умолчанию), Warning, Error, Critical.
Режим вывода: [LogOutputMode] Куда направлять лог.
Варианты: FileOnly (только в файл), ConsoleOnly (только в лог Primo), FileAndConsole (в оба места, по умолчанию).

Настройки файла
Директория логов: [String] Путь к папке для сохранения файлов логов. Обязателен, если выбран вывод в файл.
Базовое имя файла: [String] Основная часть имени файла без расширения (например, app). По умолчанию ""app"". Обязателен при выводе в файл.
Расширение: [String] Расширение файла (например, .log). По умолчанию "".log"". Обязателен при выводе в файл.
Шаблон имени файла: [LogFileNameTemplate] Определяет формат имени.
Варианты:

Fixed – фиксированное имя (app.log)
WithDate – с датой (app_2025-02-15.log)
WithDateTime – с датой и временем (app_2025-02-15_14-30.log)
WithTimestamp – с меткой времени (app_20250215143045.log)
Custom – пользовательский паттерн (по умолчанию WithDate)

Пользовательский паттерн: [String] Шаблон для варианта Custom. Поддерживаются плейсхолдеры:
{base}, {ext}, {level}, {date}, {time}, {timestamp}, {yyyy}, {MM}, {dd}, {HH}, {mm}, {ss}, {fff}, {weekday}, {week}, {day_of_year}, {guid}, {guid_short}.
Пример: ""{base}_{yyyy}-{MM}-{dd}_{HH}-{mm}{ext}"" → app_2025-02-15_14-30.log. Обязателен при выборе Custom.

Ротация файлов: [LogRotationStrategy] Стратегия создания новых файлов.
Варианты: None (один файл), Daily (ежедневно, по умолчанию), Hourly (ежечасно), BySize (по достижении размера).

Макс. размер (МБ): [Int32] Предельный размер файла в мегабайтах для ротации BySize. По умолчанию 10. Обязателен при ротации BySize.

Формат сообщения
Добавлять время: [Boolean] Если true, к сообщению добавляется временная метка. По умолчанию true.
Добавлять уровень: [Boolean] Если true, к сообщению добавляется уровень логирования. По умолчанию true.
Формат времени: [String] Строка формата DateTime для временной метки, например ""yyyy-MM-dd HH:mm:ss.fff"". По умолчанию ""yyyy-MM-dd HH:mm:ss.fff"".
Перенос строки: [Boolean] Добавлять перевод строки после сообщения. По умолчанию true.

Выходные данные
Путь к файлу лога: [String] Фактический полный путь к файлу, в который была произведена запись (с учётом ротации).
Форматированное сообщение: [String] Итоговое сообщение после применения формата (с временем и уровнем, если они включены).";

            sdkComponentIcon = ActivityIcons.Log;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<string>("Prop_Message", "Текст сообщения для логирования"),
                PropertyBuilder.Enum<LogLevel>("Level", "Уровень важности сообщения: Debug, Info, Warning, Error, Critical"),
                PropertyBuilder.Enum<LogOutputMode>("OutputMode", "Куда выводить лог: в файл, консоль или оба варианта"),
                PropertyBuilder.FolderSelector("Prop_LogDirectory", "Путь к директории для сохранения файлов логов"),
                PropertyBuilder.Enum<LogFileNameTemplate>("FileNameTemplate", "Шаблон формирования имени файла: фиксированное, с датой, с временем или пользовательское"),
                PropertyBuilder.Script<string>("Prop_BaseFileName", "Базовое имя файла без расширения (например: app, mylog, process)"),
                PropertyBuilder.Script<string>("Prop_FileExtension", "Расширение файла (например: .log, .txt)"),
                PropertyBuilder.Script<string>("Prop_CustomFileNamePattern", "Пользовательский паттерн имени файла. Плейсхолдеры: {base}, {date}, {time}, {timestamp}, {yyyy}, {MM}, {dd}, {HH}, {mm}, {ss}, {level}"),
                PropertyBuilder.Enum<LogRotationStrategy>("RotationStrategy", "Стратегия создания новых файлов: нет, ежедневно, ежечасно, по размеру"),
                PropertyBuilder.Script<int>("Prop_MaxFileSizeMB", "Максимальный размер файла в МБ (для ротации по размеру)"),
                PropertyBuilder.Script<string>("Prop_TimestampFormat", "Формат даты/времени в сообщении (например: yyyy-MM-dd HH:mm:ss.fff)"),
                PropertyBuilder.Variable<string>("Prop_ActualLogFile", "Фактический путь к файлу, в который была произведена запись"),
                PropertyBuilder.Variable<string>("Prop_FormattedMessage", "Полностью отформатированное сообщение с timestamp и уровнем")
            };

            InitClass(container);

            // Установка значений по умолчанию
            this.Prop_BaseFileName = "\"app\"";
            this.Prop_FileExtension = "\".log\"";
            this.Prop_CustomFileNamePattern = "\"{base}_{yyyy}-{MM}-{dd}_{HH}-{mm}{ext}\"";
            this.Prop_MaxFileSizeMB = "10";
            this.Prop_TimestampFormat = "\"yyyy-MM-dd HH:mm:ss.fff\"";
        }

        /// <summary>
        /// Основное действие компонента
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var parameters = GetAndValidateParameters(sd);
                string formattedMessage = FormatMessage(parameters);
                string actualLogFile = null;

                switch (parameters.OutputMode)
                {
                    case LogOutputMode.FileOnly:
                        actualLogFile = WriteToFile(formattedMessage, parameters);
                        break;

                    case LogOutputMode.ConsoleOnly:
                        WriteToPrimoLog(sd, parameters.Message, parameters.Level);   // используем sd
                        break;

                    case LogOutputMode.FileAndConsole:
                        actualLogFile = WriteToFile(formattedMessage, parameters);
                        WriteToPrimoLog(sd, parameters.Message, parameters.Level);   // используем sd
                        break;
                }

                SetVariableValue(this.Prop_ActualLogFile, actualLogFile ?? string.Empty, sd);
                SetVariableValue(this.Prop_FormattedMessage, formattedMessage, sd);

                return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Сообщение залогировано: {parameters.Level}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка при логировании: {ex.Message}" };
            }
        }

        #region Вспомогательные классы и методы

        /// <summary>
        /// Параметры логирования
        /// </summary>
        private class LogParameters
        {
            public string Message { get; set; }
            public LogLevel Level { get; set; }
            public LogOutputMode OutputMode { get; set; }
            public string LogDirectory { get; set; }
            public LogFileNameTemplate FileNameTemplate { get; set; }
            public string BaseFileName { get; set; }
            public string FileExtension { get; set; }
            public string CustomFileNamePattern { get; set; }
            public LogRotationStrategy RotationStrategy { get; set; }
            public int MaxFileSizeMB { get; set; }
            public bool IncludeTimestamp { get; set; }
            public bool IncludeLevel { get; set; }
            public string TimestampFormat { get; set; }
            public bool AppendNewLine { get; set; }
        }

        /// <summary>
        /// Получает и валидирует параметры логирования
        /// </summary>
        private LogParameters GetAndValidateParameters(ScriptingData sd)
        {
            // Получение параметров
            string message = GetPropertyValue<string>(this.Prop_Message, "Prop_Message", sd);
            string logDirectory = GetPropertyValue<string>(this.Prop_LogDirectory, "Prop_LogDirectory", sd);
            string baseFileName = GetPropertyValue<string>(this.Prop_BaseFileName, "Prop_BaseFileName", sd);
            string fileExtension = GetPropertyValue<string>(this.Prop_FileExtension, "Prop_FileExtension", sd);
            string customPattern = GetPropertyValue<string>(this.Prop_CustomFileNamePattern, "Prop_CustomFileNamePattern", sd);
            string timestampFormat = GetPropertyValue<string>(this.Prop_TimestampFormat, "Prop_TimestampFormat", sd);
            int maxFileSizeMB = Convert.ToInt32(GetPropertyValue(this.Prop_MaxFileSizeMB, "Prop_MaxFileSizeMB", sd));

            // Валидация
            ValidateStringParameter(message, "Сообщение");

            // Валидация директории только если вывод включает файл
            if (this.OutputMode != LogOutputMode.ConsoleOnly)
            {
                ValidateStringParameter(logDirectory, "Директория логов");
                ValidateStringParameter(baseFileName, "Базовое имя файла");
                ValidateStringParameter(fileExtension, "Расширение файла");

                // Валидация пользовательского паттерна если выбран Custom
                if (this.FileNameTemplate == LogFileNameTemplate.Custom)
                {
                    ValidateStringParameter(customPattern, "Пользовательский паттерн");
                }
            }

            if (maxFileSizeMB <= 0)
            {
                throw new ArgumentException("Максимальный размер файла должен быть больше нуля");
            }

            // Нормализация расширения файла
            if (!fileExtension.StartsWith("."))
            {
                fileExtension = "." + fileExtension;
            }

            // Если формат не указан, использовать значение по умолчанию
            if (string.IsNullOrWhiteSpace(timestampFormat))
            {
                timestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
            }

            return new LogParameters
            {
                Message = message,
                Level = this.Level,
                OutputMode = this.OutputMode,
                LogDirectory = logDirectory,
                FileNameTemplate = this.FileNameTemplate,
                BaseFileName = baseFileName,
                FileExtension = fileExtension,
                CustomFileNamePattern = customPattern,
                RotationStrategy = this.RotationStrategy,
                MaxFileSizeMB = maxFileSizeMB,
                IncludeTimestamp = this.Prop_IncludeTimestamp,
                IncludeLevel = this.Prop_IncludeLevel,
                TimestampFormat = timestampFormat,
                AppendNewLine = this.Prop_AppendNewLine
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
        /// Форматирует сообщение в соответствии с настройками
        /// </summary>
        private string FormatMessage(LogParameters parameters)
        {
            var sb = new StringBuilder();

            // Добавление timestamp
            if (parameters.IncludeTimestamp)
            {
                string timestamp = DateTime.Now.ToString(parameters.TimestampFormat);
                sb.Append($"[{timestamp}]");
            }

            // Добавление уровня
            if (parameters.IncludeLevel)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append($"[{GetLevelString(parameters.Level)}]");
            }

            // Добавление сообщения
            if (sb.Length > 0) sb.Append(" ");
            sb.Append(parameters.Message);

            // Добавление переноса строки
            if (parameters.AppendNewLine)
            {
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Получает строковое представление уровня с выравниванием
        /// </summary>
        private string GetLevelString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return "DEBUG   ";
                case LogLevel.Info:
                    return "INFO    ";
                case LogLevel.Warning:
                    return "WARNING ";
                case LogLevel.Error:
                    return "ERROR   ";
                case LogLevel.Critical:
                    return "CRITICAL";
                default:
                    return level.ToString().ToUpper().PadRight(8);
            }
        }

        /// <summary>
        /// Записывает сообщение в файл с учетом ротации
        /// </summary>
        private string WriteToFile(string message, LogParameters parameters)
        {
            // Определение фактического пути к файлу
            string actualFilePath = BuildLogFilePath(parameters);

            // Обеспечение существования директории
            string directory = Path.GetDirectoryName(actualFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Thread-safe запись в файл
            lock (_fileLock)
            {
                // Проверка на необходимость ротации по размеру
                if (parameters.RotationStrategy == LogRotationStrategy.BySize)
                {
                    actualFilePath = CheckAndRotateBySize(actualFilePath, parameters.MaxFileSizeMB);
                }

                // Запись сообщения
                File.AppendAllText(actualFilePath, message, Encoding.UTF8);
            }

            return actualFilePath;
        }

        /// <summary>
        /// Строит полный путь к файлу лога на основе параметров
        /// </summary>
        private string BuildLogFilePath(LogParameters parameters)
        {
            string fileName = GenerateFileName(parameters);
            return Path.Combine(parameters.LogDirectory, fileName);
        }

        /// <summary>
        /// Генерирует имя файла на основе выбранного шаблона
        /// </summary>
        private string GenerateFileName(LogParameters parameters)
        {
            DateTime now = DateTime.Now;

            switch (parameters.FileNameTemplate)
            {
                case LogFileNameTemplate.Fixed:
                    // Фиксированное имя: app.log
                    return parameters.BaseFileName + parameters.FileExtension;

                case LogFileNameTemplate.WithDate:
                    // С датой: app_2025-02-15.log
                    return $"{parameters.BaseFileName}_{now:yyyy-MM-dd}{parameters.FileExtension}";

                case LogFileNameTemplate.WithDateTime:
                    // С датой и временем: app_2025-02-15_14-30.log
                    return $"{parameters.BaseFileName}_{now:yyyy-MM-dd_HH-mm}{parameters.FileExtension}";

                case LogFileNameTemplate.WithTimestamp:
                    // С timestamp: app_20250215143045.log
                    return $"{parameters.BaseFileName}_{now:yyyyMMddHHmmss}{parameters.FileExtension}";

                case LogFileNameTemplate.Custom:
                    // Пользовательский формат
                    return ApplyCustomPattern(parameters.CustomFileNamePattern, parameters, now);

                default:
                    return parameters.BaseFileName + parameters.FileExtension;
            }
        }

        /// <summary>
        /// Применяет пользовательский паттерн к имени файла
        /// </summary>
        private string ApplyCustomPattern(string pattern, LogParameters parameters, DateTime now)
        {
            // Замена плейсхолдеров
            var result = pattern;

            // Базовые плейсхолдеры
            result = result.Replace("{base}", parameters.BaseFileName);
            result = result.Replace("{ext}", parameters.FileExtension);
            result = result.Replace("{level}", parameters.Level.ToString().ToLower());

            // Дата и время - полные форматы
            result = result.Replace("{date}", now.ToString("yyyy-MM-dd"));
            result = result.Replace("{time}", now.ToString("HH-mm-ss"));
            result = result.Replace("{timestamp}", now.ToString("yyyyMMddHHmmss"));
            result = result.Replace("{milliseconds}", now.ToString("fff"));

            // Дата - отдельные компоненты
            result = result.Replace("{yyyy}", now.ToString("yyyy"));
            result = result.Replace("{yy}", now.ToString("yy"));
            result = result.Replace("{MM}", now.ToString("MM"));
            result = result.Replace("{dd}", now.ToString("dd"));

            // Время - отдельные компоненты
            result = result.Replace("{HH}", now.ToString("HH"));
            result = result.Replace("{mm}", now.ToString("mm"));
            result = result.Replace("{ss}", now.ToString("ss"));
            result = result.Replace("{fff}", now.ToString("fff"));

            // День недели
            result = result.Replace("{weekday}", now.ToString("dddd"));
            result = result.Replace("{weekday_short}", now.ToString("ddd"));

            // Номер недели и день года
            result = result.Replace("{week}", GetWeekNumber(now).ToString("D2"));
            result = result.Replace("{day_of_year}", now.DayOfYear.ToString("D3"));

            // GUID (для уникальности)
            if (result.Contains("{guid}"))
            {
                result = result.Replace("{guid}", Guid.NewGuid().ToString("N"));
            }
            if (result.Contains("{guid_short}"))
            {
                result = result.Replace("{guid_short}", Guid.NewGuid().ToString("N").Substring(0, 8));
            }

            // Очистка недопустимых символов для имени файла
            result = CleanFileName(result);

            return result;
        }

        /// <summary>
        /// Получает номер недели в году
        /// </summary>
        private int GetWeekNumber(DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return culture.Calendar.GetWeekOfYear(date,
                System.Globalization.CalendarWeekRule.FirstDay,
                DayOfWeek.Monday);
        }

        /// <summary>
        /// Очищает имя файла от недопустимых символов
        /// </summary>
        private string CleanFileName(string fileName)
        {
            // Получаем недопустимые символы для имени файла
            var invalidChars = Path.GetInvalidFileNameChars();

            // Заменяем недопустимые символы на подчеркивание
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName;
        }

        /// <summary>
        /// Проверяет размер файла и выполняет ротацию при необходимости
        /// </summary>
        private string CheckAndRotateBySize(string filePath, int maxSizeMB)
        {
            if (!File.Exists(filePath))
            {
                return filePath;
            }

            var fileInfo = new FileInfo(filePath);
            long maxSizeBytes = maxSizeMB * 1024 * 1024;

            // Если файл не превышает лимит, используем его
            if (fileInfo.Length < maxSizeBytes)
            {
                return filePath;
            }

            // Ищем следующий доступный номер файла
            string directory = Path.GetDirectoryName(filePath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            int counter = 1;
            string newFilePath;

            do
            {
                string newFileName = $"{fileNameWithoutExt}_{counter:D4}{extension}";
                newFilePath = string.IsNullOrEmpty(directory)
                    ? newFileName
                    : Path.Combine(directory, newFileName);
                counter++;
            }
            while (File.Exists(newFilePath) && new FileInfo(newFilePath).Length >= maxSizeBytes);

            return newFilePath;
        }

        /// <summary>
        /// Вывод сообщения в лог Primo через штатный механизм
        /// </summary>
        private void WriteToPrimoLog(ScriptingData sd, string message, LogLevel level)
        {
            var messageType = ToLogMessageType(level);
            // Используем перегрузку AddToLog, принимающую ScriptingData (доступна в современных версиях Primo)
            LTools.Workflow.PrimoApp.AddToLog(sd.WorkflowVar, message, messageType);
        }

        /// <summary>
        /// Преобразование внутреннего уровня логирования в тип Primo
        /// </summary>
        private LogMessageType ToLogMessageType(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return LogMessageType.Debug;
                case LogLevel.Info:
                    return LogMessageType.Info;
                case LogLevel.Warning:
                    return LogMessageType.Error;
                case LogLevel.Error:
                    return LogMessageType.Error;
                case LogLevel.Critical:
                    return LogMessageType.Error;
                default:
                    return LogMessageType.Info;
            }
        }

        #endregion

        /// <summary>
        /// Проверка корректности введенных данных
        /// </summary>
        public override ValidationResult Validate()
        {
            ValidationResult ret = new ValidationResult();

            // Проверка сообщения
            ret.ValidateRequired(this.Prop_Message, ActivityStrings.Field_Message, ActivityStrings.Error_MessageRequired);

            // Проверка параметров файла если вывод в файл включен
            if (this.OutputMode != LogOutputMode.ConsoleOnly)
            {
                ret.ValidateRequired(this.Prop_LogDirectory, ActivityStrings.Field_LogDirectory, ActivityStrings.Error_LogDirectoryRequired);
                ret.ValidateRequired(this.Prop_BaseFileName, ActivityStrings.Field_BaseFileName, ActivityStrings.Error_BaseFileNameRequired);
                ret.ValidateRequired(this.Prop_FileExtension, ActivityStrings.Field_FileExtension, ActivityStrings.Error_FileExtensionRequired);

                // Проверка пользовательского паттерна для Custom шаблона
                if (this.FileNameTemplate == LogFileNameTemplate.Custom)
                {
                    ret.ValidateRequired(this.Prop_CustomFileNamePattern, ActivityStrings.Field_CustomFileNamePattern, ActivityStrings.Error_CustomPatternRequired);
                }
            }

            // Проверка максимального размера
            ret.ValidateRequired(this.Prop_MaxFileSizeMB, ActivityStrings.Field_MaxFileSizeMB, ActivityStrings.Error_MaxFileSizeRequired);

            return ret;
        }
    }
}
