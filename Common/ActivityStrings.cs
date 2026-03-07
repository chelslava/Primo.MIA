namespace Primo.MIA.Common
{
    /// <summary>
    /// Централизованные текстовые константы для активностей.
    /// Используются в валидации, сообщениях об ошибках, названиях полей и категориях.
    /// </summary>
    public static class ActivityStrings
    {
        // ═══════════════════════════════════════════════════════════════════════
        // КАТЕГОРИИ
        // ═══════════════════════════════════════════════════════════════════════
        
        public const string Category_Main = "Основные";
        public const string Category_Output = "Выходные данные";
        public const string Category_Settings = "Настройки";
        public const string Category_Search = "Поиск";
        public const string Category_FromLists = "FromLists";
        public const string Category_Invert = "Invert";
        public const string Category_Elements = "Элементы";
        public const string Category_Lengths = "Длины";
        public const string Category_Statistics = "Статистика";
        public const string Category_File = "Файл";
        public const string Category_Format = "Формат";
        public const string Category_LengthRange = "LengthRange";

        // ═══════════════════════════════════════════════════════════════════════
        // НАЗВАНИЯ ПОЛЕЙ (общие)
        // ═══════════════════════════════════════════════════════════════════════
        
        public const string Field_Dictionary = "Словарь";
        public const string Field_List = "Список";
        public const string Field_Tuple = "Кортеж";
        public const string Field_Key = "Ключ";
        public const string Field_Value = "Значение";
        public const string Field_DefaultValue = "Значение по умолчанию";
        public const string Field_Url = "URL";
        public const string Field_Method = "Метод";
        public const string Field_Pattern = "Шаблон";
        public const string Field_Path = "Путь";
        public const string Field_Directory = "Директория";
        public const string Field_FileName = "Имя файла";
        public const string Field_SearchPattern = "Шаблон поиска";
        public const string Field_SearchValue = "Искомое значение";
        public const string Field_InputString = "Входная строка";
        public const string Field_FirstDictionary = "Первый словарь";
        public const string Field_SecondDictionary = "Второй словарь";
        public const string Field_KeysList = "Список ключей";
        public const string Field_ValuesList = "Список значений";
        public const string Field_Mode = "Режим создания";
        public const string Field_Strategy = "Стратегия при конфликте";
        public const string Field_ThrowIfNotFound = "Ошибка если не найдено";
        public const string Field_ThrowOnDuplicates = "Ошибка при дублях";
        public const string Field_CaseSensitive = "Учитывать регистр";
        
        // ═══════════════════════════════════════════════════════════════════════
        // НАЗВАНИЯ ПОЛЕЙ (выходные)
        // ═══════════════════════════════════════════════════════════════════════
        
        public const string Field_ResultDictionary = "Результирующий словарь";
        public const string Field_Count = "Количество элементов";
        public const string Field_DuplicatesCount = "Пропущено дублей (Invert)";
        public const string Field_Found = "Ключ найден";
        public const string Field_KeyExists = "Ключ существует";
        public const string Field_IsUpdate = "Обновление (не добавление)";
        public const string Field_HadConflicts = "Были конфликты ключей";
        public const string Field_ConflictKeys = "Конфликтующие ключи";
        public const string Field_TotalCount = "Всего элементов";
        public const string Field_UniqueCount = "Уникальных";
        public const string Field_EmptyCount = "Пустых";
        public const string Field_HasDuplicates = "Есть дубликаты";
        public const string Field_Duplicates = "Дублирующиеся значения";
        public const string Field_AvgLength = "Средняя длина";
        public const string Field_Contains = "Найдено";
        public const string Field_FirstIndex = "Первый индекс";
        public const string Field_AllIndexes = "Все индексы";
        public const string Field_OccurrenceCount = "Кол-во вхождений";
        
        // ═══════════════════════════════════════════════════════════════════════
        // СООБЩЕНИЯ ОБ ОШИБКАХ
        // ═══════════════════════════════════════════════════════════════════════
        
        public const string Error_DictionaryRequired = "Словарь обязателен";
        public const string Error_ListRequired = "Список обязателен";
        public const string Error_TupleRequired = "Кортеж обязателен";
        public const string Error_KeyRequired = "Ключ обязателен";
        public const string Error_ValueRequired = "Значение обязательно";
        public const string Error_UrlRequired = "URL не может быть пустым";
        public const string Error_MethodRequired = "Метод обязателен";
        public const string Error_PatternRequired = "Шаблон обязателен";
        public const string Error_PathRequired = "Путь обязателен";
        public const string Error_DirectoryRequired = "Директория обязательна";
        public const string Error_FileNameRequired = "Имя файла обязательно";
        public const string Error_SearchPatternRequired = "Шаблон поиска обязателен";
        public const string Error_InvalidRegex = "Некорректное регулярное выражение";
        public const string Error_SearchValueRequired = "Искомое значение обязательно";
        public const string Error_InputStringRequired = "Входная строка обязательна";
        public const string Error_FirstDictionaryRequired = "Первый словарь обязателен";
        public const string Error_SecondDictionaryRequired = "Второй словарь обязателен";
        public const string Error_CustomContentTypeRequired = "Укажите Content-Type при выборе Custom";
        public const string Error_KeysListRequired = "Список ключей обязателен для режима FromLists";
        public const string Error_ValuesListRequired = "Список значений обязателен для режима FromLists";
        public const string Error_DictionaryRequiredForMode = "Словарь обязателен для выбранного режима";
        public const string Error_MessageRequired = "Сообщение не может быть пустым";
        public const string Error_LogDirectoryRequired = "Директория логов не может быть пустой";
        public const string Error_BaseFileNameRequired = "Базовое имя файла не может быть пустым";
        public const string Error_FileExtensionRequired = "Расширение файла не может быть пустым";
        public const string Error_CustomPatternRequired = "Пользовательский паттерн не может быть пустым";
        public const string Error_MaxFileSizeRequired = "Максимальный размер файла должен быть указан";
        public const string Error_SearchTimeoutRequired = "Таймаут поиска должен быть указан";
        
        // ═══════════════════════════════════════════════════════════════════════
        // НАЗВАНИЯ АКТИВНОСТЕЙ
        // ═══════════════════════════════════════════════════════════════════════
        
        // HTTP
        public const string Activity_HttpRequest = "HTTP запрос";
        
        // Dictionary
        public const string Activity_DictionaryCreate = "Словарь: Создать";
        public const string Activity_DictionaryGetValue = "Словарь: Получить значение";
        public const string Activity_DictionarySetValue = "Словарь: Установить значение";
        public const string Activity_DictionaryContainsKey = "Словарь: Проверить ключ";
        public const string Activity_DictionaryMerge = "Словарь: Объединить";
        public const string Activity_DictionaryFilter = "Словарь: Фильтрация";
        public const string Activity_DictionaryContainsValue = "Словарь: Проверить значение";
        public const string Activity_DictionaryGetInfo = "Словарь: Информация";
        public const string Activity_DictionaryFromString = "Словарь: Из строки";
        public const string Activity_DictionaryToString = "Словарь: В строку";
        public const string Activity_DictionaryOperations = "Словарь: Операции";
        
        // List
        public const string Activity_ListFilter = "Список: Фильтрация";
        public const string Activity_ListTransform = "Список: Преобразование";
        public const string Activity_ListSort = "Список: Сортировка";
        public const string Activity_ListAggregate = "Список: Агрегация";
        public const string Activity_ListSlice = "Список: Срез";
        public const string Activity_ListSet = "Список: Теория множеств";
        public const string Activity_ListInspect = "Список: Анализ";
        public const string Activity_ListConvert = "Список: Конвертация";
        public const string Activity_ListGroup = "Список: Группировка";
        
        // Tuple
        public const string Activity_TupleCreate = "Кортеж: Создать";
        public const string Activity_TupleDestructure = "Кортеж: Деструктуризация";
        public const string Activity_TupleGet = "Кортеж: Получить элемент";
        public const string Activity_TupleSet = "Кортеж: Заменить элемент";
        public const string Activity_TupleInspect = "Кортеж: Анализ";
        public const string Activity_TupleConvert = "Кортеж: Конвертация";
        public const string Activity_TupleZip = "Кортеж: Zip списков";
        public const string Activity_TupleUnzip = "Кортеж: Unzip списков";
        public const string Activity_TupleSort = "Кортеж: Сортировка списка";
        
        // Utilities
        public const string Activity_LogMessage = "Логирование";
        public const string Activity_SearchFiles = "Поиск файлов";
        public const string Activity_ReadTomlConfig = "Чтение TOML конфигурации";
        public const string Activity_Generators = "Генераторы";
        public const string Activity_ExcelCellRecalculate = "Excel: Пересчёт ячеек";
        public const string Activity_WaitForFile = "Ожидание файла";
        
        // ═══════════════════════════════════════════════════════════════════════
        // ДОПОЛНИТЕЛЬНЫЕ ПОЛЯ
        // ═══════════════════════════════════════════════════════════════════════
        
        public const string Field_Message = "Сообщение";
        public const string Field_Level = "Уровень";
        public const string Field_OutputMode = "Режим вывода";
        public const string Field_LogDirectory = "Директория логов";
        public const string Field_FileNameTemplate = "Шаблон имени файла";
        public const string Field_BaseFileName = "Базовое имя файла";
        public const string Field_FileExtension = "Расширение";
        public const string Field_CustomFileNamePattern = "Пользовательский паттерн";
        public const string Field_RotationStrategy = "Ротация файлов";
        public const string Field_MaxFileSizeMB = "Макс. размер (МБ)";
        public const string Field_IncludeTimestamp = "Добавлять время";
        public const string Field_IncludeLevel = "Добавлять уровень";
        public const string Field_TimestampFormat = "Формат времени";
        public const string Field_AppendNewLine = "Перенос строки";
        public const string Field_ActualLogFile = "Путь к файлу лога";
        public const string Field_FormattedMessage = "Форматированное сообщение";
        public const string Field_DirectoryPath = "Путь к директории";
        public const string Field_FilterType = "Тип фильтрации";
        public const string Field_SearchType = "Что искать";
        public const string Field_SearchInSubfolders = "Искать в подпапках";
        public const string Field_SearchTimeout = "Таймаут поиска (сек)";
        public const string Field_FoundItems = "Найденные элементы";
        public const string Field_ItemsCount = "Количество элементов";
        public const string Field_Headers = "Заголовки";
        public const string Field_Body = "Тело запроса";
        public const string Field_ContentType = "Тип контента";
        public const string Field_CustomContentType = "Пользовательский тип";
        public const string Field_Timeout = "Таймаут (сек)";
        public const string Field_ResponseBody = "Тело ответа";
        public const string Field_StatusCode = "Код статуса";
        public const string Field_ResponseHeaders = "Заголовки ответа";
        public const string Field_IsSuccess = "Успешно";
        public const string Field_ResponseTime = "Время ответа (мс)";
        public const string Field_TupleKind = "Тип кортежа";
        public const string Field_Item1 = "Item1 (обязателен)";
        public const string Field_Item2 = "Item2";
        public const string Field_Item3 = "Item3";
        public const string Field_Item4 = "Item4";
        public const string Field_Item5 = "Item5";
        public const string Field_Item6 = "Item6";
        public const string Field_Item7 = "Item7";
        public const string Field_Result = "Кортеж";
        public const string Field_Arity = "Арность";
        public const string Field_KindName = "Тип (имя)";
        public const string Field_Condition = "Условие фильтрации";
        public const string Field_PatternSubstring = "Паттерн / Подстрока";
        public const string Field_MinLength = "Мин. длина";
        public const string Field_MaxLength = "Макс. длина";
        public const string Field_Matched = "Прошли фильтр";
        public const string Field_Rejected = "Не прошли фильтр";
        public const string Field_MatchedCount = "Кол-во прошедших";
        public const string Field_RejectedCount = "Кол-во отсеянных";
    }
}
