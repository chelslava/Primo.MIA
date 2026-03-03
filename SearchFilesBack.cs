using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для поиска файлов и папок в директории с защитой по таймауту:
    /// - Поддержка wildcard и regex фильтрации
    /// - Рекурсивный поиск в подпапках
    /// - Поиск файлов, папок или обоих типов
    /// - Защита от зависания на больших директориях
    /// - Возврат списка найденных элементов
    /// </summary>
    public class SearchFilesBack : PrimoComponentTO<SearchFiles>
    {
        /// <summary>
        /// Имя группы компонента
        /// </summary>
        private const string CGroupName = "MIA" + WFPublishedElementBase.TREE_SEPARATOR + "Утилиты";

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
        /// Путь к директории для поиска
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Путь к директории")]
        public string Prop_DirectoryPath
        {
            get { return this.prop_DirectoryPath; }
            set { this.prop_DirectoryPath = value; this.InvokePropertyChanged(this, "Prop_DirectoryPath"); }
        }

        private SearchFilterType _filterType = SearchFilterType.Wildcard;
        /// <summary>
        /// Тип фильтрации
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Тип фильтрации")]
        public SearchFilterType FilterType
        {
            get => this._filterType;
            set { this._filterType = value; this.InvokePropertyChanged(this, "FilterType"); }
        }

        private string prop_Pattern;
        /// <summary>
        /// Паттерн для поиска (wildcard или regex)
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Основные"), System.ComponentModel.DisplayName("Паттерн поиска")]
        public string Prop_Pattern
        {
            get { return this.prop_Pattern; }
            set { this.prop_Pattern = value; this.InvokePropertyChanged(this, "Prop_Pattern"); }
        }

        private SearchType _searchType = SearchType.FilesOnly;
        /// <summary>
        /// Что искать: файлы, папки или оба типа
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("Настройки"), System.ComponentModel.DisplayName("Что искать")]
        public SearchType SearchTypeValue
        {
            get => this._searchType;
            set { this._searchType = value; this.InvokePropertyChanged(this, "SearchTypeValue"); }
        }

        private bool _searchInSubfolders = false;
        /// <summary>
        /// Искать в подпапках (рекурсивно)
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category("Настройки"), System.ComponentModel.DisplayName("Искать в подпапках")]
        public bool Prop_SearchInSubfolders
        {
            get { return this._searchInSubfolders; }
            set { this._searchInSubfolders = value; this.InvokePropertyChanged(this, "Prop_SearchInSubfolders"); }
        }

        private string prop_SearchTimeout;
        /// <summary>
        /// Таймаут поиска в секундах (для защиты от зависания)
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Настройки"), System.ComponentModel.DisplayName("Таймаут поиска (сек)")]
        public string Prop_SearchTimeout
        {
            get { return this.prop_SearchTimeout; }
            set { this.prop_SearchTimeout = value; this.InvokePropertyChanged(this, "Prop_SearchTimeout"); }
        }

        // ============== OUTPUT PROPERTIES ==============

        private string prop_FoundItems;
        /// <summary>
        /// Список найденных элементов
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category("Output"), System.ComponentModel.DisplayName("Найденные элементы")]
        public string Prop_FoundItems
        {
            get => prop_FoundItems;
            set
            {
                prop_FoundItems = value;
                this.InvokePropertyChanged(this, "Prop_FoundItems");
            }
        }

        private string prop_ItemsCount;
        /// <summary>
        /// Количество найденных элементов
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category("Output"), System.ComponentModel.DisplayName("Количество элементов")]
        public string Prop_ItemsCount
        {
            get => prop_ItemsCount;
            set
            {
                prop_ItemsCount = value;
                this.InvokePropertyChanged(this, "Prop_ItemsCount");
            }
        }

        /// <summary>
        /// Тайм-аут для выполнения поиска (в миллисекундах)
        /// Защищает от зависания при поиске в очень больших директориях
        /// </summary>
        protected override int sdkTimeOut
        {
            get
            {
                try
                {
                    if (!string.IsNullOrEmpty(this.prop_SearchTimeout))
                    {
                        int timeoutSec = Convert.ToInt32(this.prop_SearchTimeout);
                        // Добавляем 5 секунд запаса для обработки результатов
                        return (timeoutSec + 5) * 1000;
                    }
                }
                catch
                {
                    // Игнорируем ошибки парсинга
                }

                // Значение по умолчанию: 65 секунд (60 сек поиск + 5 сек запас)
                return 65000;
            }
            set { }
        }

        /// <summary>
        /// Конструктор компонента
        /// </summary>
        public SearchFilesBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Поиск файлов";
            sdkComponentHelp = @"Компонент ""Поиск файлов""
Компонент для поиска файлов и папок в директории с поддержкой различных типов фильтрации и рекурсивного поиска.

Основные:
Путь к директории*: [String] Путь к директории для поиска файлов и папок (например: C:\Documents)
Тип фильтрации: [SearchFilterType] Способ фильтрации файлов по имени
Wildcard - Использование подстановочных символов * и ? (по умолчанию)
Regex - Использование регулярных выражений для сложной фильтрации
Паттерн поиска*: [String] Маска или паттерн для фильтрации файлов. По умолчанию: ""*""
Для Wildcard: *.txt, report_*.xlsx, data_?.csv
Для Regex: ^report_\d{8}\.xlsx$, ^log_\d{4}-\d{2}-\d{2}\.txt$

Настройки:
Что искать: [SearchType] Определяет тип искомых элементов. По умолчанию: FilesOnly
FilesOnly - Искать только файлы
FoldersOnly - Искать только папки (пути заканчиваются на \)
FilesAndFolders - Искать и файлы, и папки одновременно
Искать в подпапках: [Boolean] Если включено, выполняется рекурсивный поиск во всех вложенных директориях. По умолчанию: false
Таймаут поиска (сек)*: [Int32] Максимальное время выполнения поиска в секундах. Если поиск не завершится за это время, будет выброшено исключение TimeoutException. По умолчанию: 60

Выходные данные:
Найденные элементы: [List<String>] Список полных путей к найденным файлам и/или папкам, отсортированный по имени в алфавитном порядке. Папки имеют \ в конце пути.
Количество элементов: [Int32] Общее количество найденных элементов (файлов и/или папок)";

            sdkComponentIcon = "pack://application:,,,/Primo.MIA;component/images/file_search.png";

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                // Путь к директории
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_DirectoryPath",
                    PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.FOLDER_SELECTOR,
                    DataType = typeof(string),
                    ToolTip = "Путь к директории для поиска файлов и папок",
                    IsReadOnly = false
                },
                // Тип фильтрации (enum)
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "FilterType",
                    PropertyType = PropertyTypes.OBJECT,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(SearchFilterType),
                    ToolTip = "Тип фильтрации: Wildcard (*.txt) или Regex паттерн",
                    IsReadOnly = false
                },
                // Паттерн поиска
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_Pattern",
                    PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(string),
                    ToolTip = "Паттерн для поиска (wildcard: *.txt или regex: ^file_\\d{4}\\.txt$)",
                    IsReadOnly = false
                },
                // Что искать (enum)
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "SearchTypeValue",
                    PropertyType = PropertyTypes.OBJECT,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(SearchType),
                    ToolTip = "Что искать: только файлы, только папки или оба типа",
                    IsReadOnly = false
                },
                // Искать в подпапках
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_SearchInSubfolders",
                    PropertyType = PropertyTypes.OBJECT,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(bool),
                    ToolTip = "Если включено, выполняется рекурсивный поиск во всех вложенных директориях.",
                    IsReadOnly = false
                },
                // Таймаут поиска
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_SearchTimeout",
                    PropertyType = PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(int),
                    ToolTip = "Максимальное время поиска в секундах (по умолчанию 60)",
                    IsReadOnly = false
                },
                // Output свойства
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_FoundItems",
                    PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(List<string>),
                    ToolTip = "Список путей к найденным файлам и/или папкам",
                    IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_ItemsCount",
                    PropertyType = PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(int),
                    ToolTip = "Количество найденных элементов",
                    IsReadOnly = false
                }
            };

            InitClass(container);

            // Установка значений по умолчанию
            this.Prop_Pattern = "\"*\"";
            this.Prop_SearchTimeout = "60";
        }

        /// <summary>
        /// Основное действие компонента с тайм-аутом
        /// Заменяет SimpleAction на TimedAction
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Получение и валидация параметров
                var parameters = GetAndValidateParameters(sd);

                // Поиск файлов и папок
                var foundItems = SearchItems(parameters);

                // Установка выходных параметров
                SetVariableValue(this.Prop_FoundItems, foundItems, sd);
                SetVariableValue(this.Prop_ItemsCount, foundItems.Count, sd);

                return new ExecutionResult()
                {
                    IsSuccess = true,
                    SuccessMessage = $"Найдено элементов: {foundItems.Count}"
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
        /// Параметры поиска
        /// </summary>
        private class SearchParameters
        {
            public string DirectoryPath { get; set; }
            public string Pattern { get; set; }
            public SearchFilterType FilterType { get; set; }
            public SearchType SearchType { get; set; }
            public bool SearchInSubfolders { get; set; }
        }

        /// <summary>
        /// Получает и валидирует параметры поиска
        /// </summary>
        private SearchParameters GetAndValidateParameters(ScriptingData sd)
        {
            // Получение параметров
            string directoryPath = GetPropertyValue<string>(this.Prop_DirectoryPath, "Prop_DirectoryPath", sd);
            string pattern = GetPropertyValue<string>(this.Prop_Pattern, "Prop_Pattern", sd);

            // Валидация
            ValidateStringParameter(directoryPath, "Путь к директории");
            ValidateStringParameter(pattern, "Паттерн поиска");
            ValidateDirectoryExists(directoryPath);

            return new SearchParameters
            {
                DirectoryPath = directoryPath,
                Pattern = pattern,
                FilterType = this.FilterType,
                SearchType = this.SearchTypeValue,
                SearchInSubfolders = this.Prop_SearchInSubfolders
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
        /// Выполняет поиск файлов и папок
        /// </summary>
        private List<string> SearchItems(SearchParameters parameters)
        {
            var results = new List<string>();

            // Определение опции поиска (рекурсивно или нет)
            SearchOption searchOption = parameters.SearchInSubfolders
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            // Поиск файлов
            if (parameters.SearchType == SearchType.FilesOnly ||
                parameters.SearchType == SearchType.FilesAndFolders)
            {
                var files = SearchFiles(parameters.DirectoryPath, parameters.Pattern,
                    parameters.FilterType, searchOption);
                results.AddRange(files);
            }

            // Поиск папок
            if (parameters.SearchType == SearchType.FoldersOnly ||
                parameters.SearchType == SearchType.FilesAndFolders)
            {
                var folders = SearchFolders(parameters.DirectoryPath, parameters.Pattern,
                    parameters.FilterType, searchOption);
                results.AddRange(folders);
            }

            // Сортировка результатов по имени
            return results.OrderBy(x => x).ToList();
        }

        /// <summary>
        /// Поиск файлов
        /// </summary>
        private List<string> SearchFiles(string directoryPath, string pattern,
            SearchFilterType filterType, SearchOption searchOption)
        {
            var dirInfo = new DirectoryInfo(directoryPath);

            if (filterType == SearchFilterType.Wildcard)
            {
                // Wildcard поиск - используем встроенный метод
                return dirInfo.GetFiles(pattern, searchOption)
                    .Select(f => f.FullName)
                    .ToList();
            }
            else
            {
                // Regex поиск
                Regex regex;
                try
                {
                    regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"Некорректный regex паттерн: {pattern}. Ошибка: {ex.Message}");
                }

                // Получаем все файлы и фильтруем по regex
                return dirInfo.GetFiles("*", searchOption)
                    .Where(f => regex.IsMatch(f.Name))
                    .Select(f => f.FullName)
                    .ToList();
            }
        }

        /// <summary>
        /// Поиск папок
        /// </summary>
        private List<string> SearchFolders(string directoryPath, string pattern,
            SearchFilterType filterType, SearchOption searchOption)
        {
            var dirInfo = new DirectoryInfo(directoryPath);

            if (filterType == SearchFilterType.Wildcard)
            {
                // Wildcard поиск - используем встроенный метод
                return dirInfo.GetDirectories(pattern, searchOption)
                    .Select(d => d.FullName + "\\")
                    .ToList();
            }
            else
            {
                // Regex поиск
                Regex regex;
                try
                {
                    regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"Некорректный regex паттерн: {pattern}. Ошибка: {ex.Message}");
                }

                // Получаем все папки и фильтруем по regex
                return dirInfo.GetDirectories("*", searchOption)
                    .Where(d => regex.IsMatch(d.Name))
                    .Select(d => d.FullName + "\\")
                    .ToList();
            }
        }

        #endregion

        /// <summary>
        /// Проверка корректности введенных данных
        /// </summary>
        public override ValidationResult Validate()
        {
            ValidationResult ret = new ValidationResult();

            // Проверка обязательных полей
            ValidateField(ret, this.Prop_DirectoryPath, "Путь к директории",
                "Путь к директории не может быть пустым");
            ValidateField(ret, this.Prop_Pattern, "Паттерн поиска",
                "Паттерн поиска не может быть пустым");

            return ret;
        }

        /// <summary>
        /// Вспомогательный метод для валидации поля
        /// </summary>
        private void ValidateField(ValidationResult result, string value,
            string fieldName, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = fieldName,
                    Error = errorMessage
                });
            }
        }
    }
}