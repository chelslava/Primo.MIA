using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для ожидания появления НОВОГО файла в директории:
    /// - Ожидание появления файла, которого не было в начале работы активности
    /// - Поддержка wildcard и regex паттернов для фильтрации файлов
    /// - Ожидание стабильности размера файла (завершение записи)
    /// - Возврат информации о первом найденном новом файле
    /// </summary>
    public class WaitForFileBack : PrimoComponentSimple<WaitForFile>
    {
        /// <summary>
        /// Имя группы компонента
        /// </summary>
        private const string CGroupName = "MIA";

        /// <summary>
        /// Имя группы компонента
        /// </summary>
        public override string GroupName
        {
            get => CGroupName;
            protected set { }
        }

        // ============== INPUT PROPERTIES ==============

        private string prop_DirectoryPath;
        /// <summary>
        /// Путь к директории для мониторинга
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Input"), System.ComponentModel.DisplayName("Путь к директории")]
        public string Prop_DirectoryPath
        {
            get { return this.prop_DirectoryPath; }
            set { this.prop_DirectoryPath = value; this.InvokePropertyChanged(this, "Prop_DirectoryPath"); }
        }

        private string prop_FilePattern;
        /// <summary>
        /// Маска файла (wildcard: *.txt или regex паттерн)
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Input"), System.ComponentModel.DisplayName("Маска файла")]
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
        [System.ComponentModel.Category("Input"), System.ComponentModel.DisplayName("Таймаут (сек)")]
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
        [System.ComponentModel.Category("Input"), System.ComponentModel.DisplayName("Интервал проверки (мс)")]
        public string Prop_CheckInterval
        {
            get { return this.prop_CheckInterval; }
            set { this.prop_CheckInterval = value; this.InvokePropertyChanged(this, "Prop_CheckInterval"); }
        }

        private string prop_WaitForStability;
        /// <summary>
        /// Ожидать стабильности размера файла (файл полностью записан)
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Input"), System.ComponentModel.DisplayName("Ожидать завершения записи")]
        public string Prop_WaitForStability
        {
            get { return this.prop_WaitForStability; }
            set { this.prop_WaitForStability = value; this.InvokePropertyChanged(this, "Prop_WaitForStability"); }
        }

        private string prop_StabilityTimeout;
        /// <summary>
        /// Время стабильности размера файла в миллисекундах
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Input"), System.ComponentModel.DisplayName("Время стабильности (мс)")]
        public string Prop_StabilityTimeout
        {
            get { return this.prop_StabilityTimeout; }
            set { this.prop_StabilityTimeout = value; this.InvokePropertyChanged(this, "Prop_StabilityTimeout"); }
        }

        private string prop_UseWildcard;
        /// <summary>
        /// Использовать wildcard поиск (*, ?)
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Input"), System.ComponentModel.DisplayName("Использовать wildcard")]
        public string Prop_UseWildcard
        {
            get { return this.prop_UseWildcard; }
            set { this.prop_UseWildcard = value; this.InvokePropertyChanged(this, "Prop_UseWildcard"); }
        }

        private string prop_UseRegex;
        /// <summary>
        /// Использовать regex поиск
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Input"), System.ComponentModel.DisplayName("Использовать regex")]
        public string Prop_UseRegex
        {
            get { return this.prop_UseRegex; }
            set { this.prop_UseRegex = value; this.InvokePropertyChanged(this, "Prop_UseRegex"); }
        }

        private string prop_ThrowOnTimeout;
        /// <summary>
        /// Выбрасывать исключение при таймауте
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Input"), System.ComponentModel.DisplayName("Ошибка при таймауте")]
        public string Prop_ThrowOnTimeout
        {
            get { return this.prop_ThrowOnTimeout; }
            set { this.prop_ThrowOnTimeout = value; this.InvokePropertyChanged(this, "Prop_ThrowOnTimeout"); }
        }

        // ============== OUTPUT PROPERTIES ==============

        private string prop_FileFound;
        /// <summary>
        /// Флаг обнаружения нового файла
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Output"), System.ComponentModel.DisplayName("Файл найден")]
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
        [System.ComponentModel.Category("Output"), System.ComponentModel.DisplayName("Путь к файлу")]
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
        [System.ComponentModel.Category("Output"), System.ComponentModel.DisplayName("Имя файла")]
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
        [System.ComponentModel.Category("Output"), System.ComponentModel.DisplayName("Размер файла (байт)")]
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
        [System.ComponentModel.Category("Output"), System.ComponentModel.DisplayName("Время ожидания (мс)")]
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
            sdkComponentName = "Ожидание нового файла";
            sdkComponentHelp = "Активность ожидает появления нового файла в директории, соответствующего заданной маске";
            sdkComponentIcon = "pack://application:,,/Primo.SDKSample;component/Images/sample.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Input свойства
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_DirectoryPath",
                    PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.FOLDER_SELECTOR,
                    DataType = typeof(string),
                    ToolTip = "Путь к директории для мониторинга появления новых файлов",
                    IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_FilePattern",
                    PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(string),
                    ToolTip = "Маска файла (wildcard: *.txt, report_*.xlsx или regex паттерн)",
                    IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Timeout",
                    PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(int),
                    ToolTip = "Таймаут ожидания в секундах (по умолчанию 30)",
                    IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_CheckInterval",
                    PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(int),
                    ToolTip = "Интервал проверки в миллисекундах (по умолчанию 500)",
                    IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_WaitForStability",
                    PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(bool),
                    ToolTip = "Ожидать стабильности размера файла (файл полностью записан)",
                    IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_StabilityTimeout",
                    PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(int),
                    ToolTip = "Время стабильности размера файла в миллисекундах (по умолчанию 2000)",
                    IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_UseWildcard",
                    PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(bool),
                    ToolTip = "Использовать wildcard поиск (* и ?) - по умолчанию включено",
                    IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_UseRegex",
                    PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(bool),
                    ToolTip = "Использовать regex поиск (имеет приоритет над wildcard)",
                    IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_ThrowOnTimeout",
                    PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(bool),
                    ToolTip = "Выбрасывать исключение при достижении таймаута",
                    IsReadOnly = false
                },
                // Output свойства
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_FileFound",
                    PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(bool),
                    ToolTip = "Флаг обнаружения нового файла",
                    IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_FilePath",
                    PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(string),
                    ToolTip = "Полный путь к найденному файлу",
                    IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_FileName",
                    PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(string),
                    ToolTip = "Имя найденного файла",
                    IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_FileSize",
                    PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(long),
                    ToolTip = "Размер найденного файла в байтах",
                    IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_WaitTime",
                    PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(long),
                    ToolTip = "Фактическое время ожидания в миллисекундах",
                    IsReadOnly = false
                }
            };

            InitClass(container);

            // Установка значений по умолчанию
            this.Prop_FilePattern = "\"*\"";
            this.Prop_Timeout = "30";
            this.Prop_CheckInterval = "500";
            this.Prop_StabilityTimeout = "2000";
            this.Prop_WaitForStability = "false";
            this.Prop_UseWildcard = "true"; // По умолчанию включено
            this.Prop_UseRegex = "false";
            this.Prop_ThrowOnTimeout = "false";
        }

        /// <summary>
        /// Основное действие компонента
        /// </summary>
        public override ExecutionResult SimpleAction(ScriptingData sd)
        {
            try
            {
                // Получение входных параметров
                string directoryPath = GetPropertyValue<string>(this.Prop_DirectoryPath, "Prop_DirectoryPath", sd);
                string filePattern = GetPropertyValue<string>(this.Prop_FilePattern, "Prop_FilePattern", sd);

                // Проверка обязательных полей
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentException("Путь к директории не может быть пустым");
                }

                if (string.IsNullOrWhiteSpace(filePattern))
                {
                    throw new ArgumentException("Маска файла не может быть пустой");
                }

                // Проверка существования директории
                if (!Directory.Exists(directoryPath))
                {
                    throw new DirectoryNotFoundException($"Директория не найдена: {directoryPath}");
                }

                int timeoutSec = Convert.ToInt32(GetPropertyValue(this.Prop_Timeout, "Prop_Timeout", sd));
                int checkInterval = Convert.ToInt32(GetPropertyValue(this.Prop_CheckInterval, "Prop_CheckInterval", sd));
                bool waitForStability = Convert.ToBoolean(GetPropertyValue(this.Prop_WaitForStability, "Prop_WaitForStability", sd));
                int stabilityTimeout = Convert.ToInt32(GetPropertyValue(this.Prop_StabilityTimeout, "Prop_StabilityTimeout", sd));
                bool useWildcard = Convert.ToBoolean(GetPropertyValue(this.Prop_UseWildcard, "Prop_UseWildcard", sd));
                bool useRegex = Convert.ToBoolean(GetPropertyValue(this.Prop_UseRegex, "Prop_UseRegex", sd));
                bool throwOnTimeout = Convert.ToBoolean(GetPropertyValue(this.Prop_ThrowOnTimeout, "Prop_ThrowOnTimeout", sd));

                // Валидация числовых значений
                if (timeoutSec <= 0)
                {
                    throw new ArgumentException("Таймаут должен быть больше нуля");
                }
                if (checkInterval <= 0)
                {
                    throw new ArgumentException("Интервал проверки должен быть больше нуля");
                }
                if (stabilityTimeout <= 0)
                {
                    throw new ArgumentException("Время стабильности должно быть больше нуля");
                }

                // Конвертация таймаута в миллисекунды
                int timeoutMs = timeoutSec * 1000;

                // Начало отсчета времени
                var startTime = DateTime.UtcNow;

                // Получение списка существующих файлов ПЕРЕД началом ожидания
                var dirInfo = new DirectoryInfo(directoryPath);
                var existingFiles = new HashSet<string>(
                    dirInfo.GetFiles().Select(f => f.FullName),
                    StringComparer.OrdinalIgnoreCase
                );

                // Ожидание появления нового файла
                FileInfo newFile = null;

                if (useRegex)
                {
                    // Использование regex для фильтрации
                    newFile = WaitForNewFileWithRegex(directoryPath, filePattern, existingFiles,
                        timeoutMs, checkInterval, waitForStability, stabilityTimeout);
                }
                else if (useWildcard)
                {
                    // Использование wildcard для фильтрации
                    newFile = WaitForNewFileWithWildcard(directoryPath, filePattern, existingFiles,
                        timeoutMs, checkInterval, waitForStability, stabilityTimeout);
                }
                else
                {
                    // Точное совпадение имени файла
                    newFile = WaitForNewFileExact(directoryPath, filePattern, existingFiles,
                        timeoutMs, checkInterval, waitForStability, stabilityTimeout);
                }

                // Расчет фактического времени ожидания
                long actualWaitTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                // Установка выходных параметров
                if (newFile != null)
                {
                    newFile.Refresh();
                    SetVariableValue(this.Prop_FileFound, true, sd);
                    SetVariableValue(this.Prop_FilePath, newFile.FullName, sd);
                    SetVariableValue(this.Prop_FileName, newFile.Name, sd);
                    SetVariableValue(this.Prop_FileSize, newFile.Length, sd);
                    SetVariableValue(this.Prop_WaitTime, actualWaitTime, sd);

                    return new ExecutionResult()
                    {
                        IsSuccess = true,
                        SuccessMessage = $"Новый файл обнаружен: {newFile.Name}"
                    };
                }
                else
                {
                    SetVariableValue(this.Prop_FileFound, false, sd);
                    SetVariableValue(this.Prop_FilePath, string.Empty, sd);
                    SetVariableValue(this.Prop_FileName, string.Empty, sd);
                    SetVariableValue(this.Prop_FileSize, 0L, sd);
                    SetVariableValue(this.Prop_WaitTime, actualWaitTime, sd);

                    if (throwOnTimeout)
                    {
                        throw new TimeoutException($"Таймаут ожидания нового файла истек. Директория: {directoryPath}, Маска: {filePattern}, Таймаут: {timeoutSec} сек.");
                    }

                    return new ExecutionResult()
                    {
                        IsSuccess = true,
                        SuccessMessage = $"Таймаут ожидания нового файла истек после {timeoutSec} секунд"
                    };
                }
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

        /// <summary>
        /// Ожидает появления нового файла с точным именем
        /// </summary>
        /// <param name="directoryPath">Путь к директории</param>
        /// <param name="fileName">Точное имя файла</param>
        /// <param name="existingFiles">Набор существующих файлов</param>
        /// <param name="timeoutMs">Таймаут в миллисекундах</param>
        /// <param name="checkInterval">Интервал проверки в миллисекундах</param>
        /// <param name="waitForStability">Ожидать стабильности размера</param>
        /// <param name="stabilityTimeout">Время стабильности в миллисекундах</param>
        /// <returns>Информация о новом файле или null</returns>
        private FileInfo WaitForNewFileExact(string directoryPath, string fileName,
            HashSet<string> existingFiles, int timeoutMs, int checkInterval,
            bool waitForStability, int stabilityTimeout)
        {
            var startTime = DateTime.UtcNow;
            var filePath = Path.Combine(directoryPath, fileName);
            var fileInfo = new FileInfo(filePath);

            // Ожидание появления нового файла
            while (true)
            {
                // Проверка таймаута
                if ((DateTime.UtcNow - startTime).TotalMilliseconds > timeoutMs)
                {
                    return null;
                }

                fileInfo.Refresh();

                // Проверяем: файл существует И его не было в списке существующих
                if (fileInfo.Exists && !existingFiles.Contains(fileInfo.FullName))
                {
                    // Если требуется ожидание стабильности
                    if (waitForStability)
                    {
                        int remainingTimeout = timeoutMs - (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                        if (WaitForFileStability(fileInfo, stabilityTimeout, remainingTimeout, checkInterval))
                        {
                            return fileInfo;
                        }
                        else
                        {
                            return null; // Таймаут при ожидании стабильности
                        }
                    }

                    return fileInfo;
                }

                Thread.Sleep(checkInterval);
            }
        }

        /// <summary>
        /// Ожидает появления нового файла по wildcard маске
        /// </summary>
        /// <param name="directoryPath">Путь к директории</param>
        /// <param name="pattern">Wildcard паттерн (*.txt, report_*.xlsx)</param>
        /// <param name="existingFiles">Набор существующих файлов</param>
        /// <param name="timeoutMs">Таймаут в миллисекундах</param>
        /// <param name="checkInterval">Интервал проверки в миллисекундах</param>
        /// <param name="waitForStability">Ожидать стабильности размера</param>
        /// <param name="stabilityTimeout">Время стабильности в миллисекундах</param>
        /// <returns>Информация о новом файле или null</returns>
        private FileInfo WaitForNewFileWithWildcard(string directoryPath, string pattern,
            HashSet<string> existingFiles, int timeoutMs, int checkInterval,
            bool waitForStability, int stabilityTimeout)
        {
            var startTime = DateTime.UtcNow;
            var dirInfo = new DirectoryInfo(directoryPath);

            // Ожидание появления нового файла
            while (true)
            {
                // Проверка таймаута
                if ((DateTime.UtcNow - startTime).TotalMilliseconds > timeoutMs)
                {
                    return null;
                }

                // Получение файлов по паттерну
                var matchedFiles = dirInfo.GetFiles(pattern);

                // Фильтрация: оставляем только новые файлы (которых не было в existingFiles)
                var newFiles = matchedFiles
                    .Where(f => !existingFiles.Contains(f.FullName))
                    .OrderBy(f => f.CreationTime) // Сортируем по времени создания (самый старый первый)
                    .ToList();

                // Если найден хотя бы один новый файл
                if (newFiles.Any())
                {
                    var newFile = newFiles.First();

                    // Если требуется ожидание стабильности
                    if (waitForStability)
                    {
                        int remainingTimeout = timeoutMs - (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                        if (WaitForFileStability(newFile, stabilityTimeout, remainingTimeout, checkInterval))
                        {
                            return newFile;
                        }
                        else
                        {
                            return null; // Таймаут при ожидании стабильности
                        }
                    }

                    return newFile;
                }

                Thread.Sleep(checkInterval);
            }
        }

        /// <summary>
        /// Ожидает появления нового файла по regex паттерну
        /// </summary>
        /// <param name="directoryPath">Путь к директории</param>
        /// <param name="regexPattern">Regex паттерн для имени файла</param>
        /// <param name="existingFiles">Набор существующих файлов</param>
        /// <param name="timeoutMs">Таймаут в миллисекундах</param>
        /// <param name="checkInterval">Интервал проверки в миллисекундах</param>
        /// <param name="waitForStability">Ожидать стабильности размера</param>
        /// <param name="stabilityTimeout">Время стабильности в миллисекундах</param>
        /// <returns>Информация о новом файле или null</returns>
        private FileInfo WaitForNewFileWithRegex(string directoryPath, string regexPattern,
            HashSet<string> existingFiles, int timeoutMs, int checkInterval,
            bool waitForStability, int stabilityTimeout)
        {
            var startTime = DateTime.UtcNow;
            var dirInfo = new DirectoryInfo(directoryPath);

            // Компиляция regex паттерна
            Regex regex;
            try
            {
                regex = new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Некорректный regex паттерн: {regexPattern}. Ошибка: {ex.Message}");
            }

            // Ожидание появления нового файла
            while (true)
            {
                // Проверка таймаута
                if ((DateTime.UtcNow - startTime).TotalMilliseconds > timeoutMs)
                {
                    return null;
                }

                // Получение всех файлов в директории
                var allFiles = dirInfo.GetFiles();

                // Фильтрация по regex и проверка на новизну
                var newFiles = allFiles
                    .Where(f => regex.IsMatch(f.Name) && !existingFiles.Contains(f.FullName))
                    .OrderBy(f => f.CreationTime) // Сортируем по времени создания (самый старый первый)
                    .ToList();

                // Если найден хотя бы один новый файл
                if (newFiles.Any())
                {
                    var newFile = newFiles.First();

                    // Если требуется ожидание стабильности
                    if (waitForStability)
                    {
                        int remainingTimeout = timeoutMs - (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                        if (WaitForFileStability(newFile, stabilityTimeout, remainingTimeout, checkInterval))
                        {
                            return newFile;
                        }
                        else
                        {
                            return null; // Таймаут при ожидании стабильности
                        }
                    }

                    return newFile;
                }

                Thread.Sleep(checkInterval);
            }
        }

        /// <summary>
        /// Ожидает стабильности размера файла (завершения записи)
        /// </summary>
        /// <param name="fileInfo">Информация о файле</param>
        /// <param name="stabilityTimeout">Время стабильности в миллисекундах</param>
        /// <param name="remainingTimeout">Оставшееся время таймаута</param>
        /// <param name="checkInterval">Интервал проверки в миллисекундах</param>
        /// <returns>True, если размер стабилен, иначе false</returns>
        private bool WaitForFileStability(FileInfo fileInfo, int stabilityTimeout,
            int remainingTimeout, int checkInterval)
        {
            var stabilityStart = DateTime.UtcNow;
            long previousSize = -1;

            while (true)
            {
                fileInfo.Refresh();

                // Проверка существования файла
                if (!fileInfo.Exists)
                {
                    return false; // Файл исчез
                }

                long currentSize = fileInfo.Length;

                // Проверка на таймаут
                if ((DateTime.UtcNow - stabilityStart).TotalMilliseconds > remainingTimeout)
                {
                    return false;
                }

                // Если размер изменился, сбрасываем счетчик стабильности
                if (currentSize != previousSize)
                {
                    previousSize = currentSize;
                    stabilityStart = DateTime.UtcNow;
                }
                // Если размер стабилен в течение требуемого времени
                else if ((DateTime.UtcNow - stabilityStart).TotalMilliseconds >= stabilityTimeout)
                {
                    return true;
                }

                Thread.Sleep(checkInterval);
            }
        }

        /// <summary>
        /// Проверка корректности введенных данных
        /// </summary>
        public override ValidationResult Validate()
        {
            ValidationResult ret = new ValidationResult();

            // Проверка обязательного поля "Путь к директории"
            if (string.IsNullOrWhiteSpace(this.Prop_DirectoryPath))
            {
                ret.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = "Путь к директории",
                    Error = "Путь к директории не может быть пустым"
                });
            }

            // Проверка обязательного поля "Маска файла"
            if (string.IsNullOrWhiteSpace(this.Prop_FilePattern))
            {
                ret.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = "Маска файла",
                    Error = "Маска файла не может быть пустой"
                });
            }

            // Проверка обязательного поля "Таймаут"
            if (string.IsNullOrWhiteSpace(this.Prop_Timeout))
            {
                ret.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = "Таймаут (сек)",
                    Error = "Таймаут должен быть указан"
                });
            }

            // Проверка обязательного поля "Интервал проверки"
            if (string.IsNullOrWhiteSpace(this.Prop_CheckInterval))
            {
                ret.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = "Интервал проверки (мс)",
                    Error = "Интервал проверки должен быть указан"
                });
            }

            // Проверка обязательного поля "Время стабильности"
            if (string.IsNullOrWhiteSpace(this.Prop_StabilityTimeout))
            {
                ret.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = "Время стабильности (мс)",
                    Error = "Время стабильности должно быть указано"
                });
            }

            // Проверка обязательных boolean полей
            if (string.IsNullOrWhiteSpace(this.Prop_WaitForStability))
            {
                ret.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = "Ожидать завершения записи",
                    Error = "Значение должно быть указано"
                });
            }

            if (string.IsNullOrWhiteSpace(this.Prop_UseWildcard))
            {
                ret.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = "Использовать wildcard",
                    Error = "Значение должно быть указано"
                });
            }

            if (string.IsNullOrWhiteSpace(this.Prop_UseRegex))
            {
                ret.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = "Использовать regex",
                    Error = "Значение должно быть указано"
                });
            }

            if (string.IsNullOrWhiteSpace(this.Prop_ThrowOnTimeout))
            {
                ret.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = "Ошибка при таймауте",
                    Error = "Значение должно быть указано"
                });
            }

            return ret;
        }
    }
}