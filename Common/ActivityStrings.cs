// =============================================================================
// ActivityStrings.cs — централизованные текстовые константы для активностей.
//
// ИЗМЕНЕНИЯ:
//   Добавлены константы в секцию BUSINESS CALENDAR:
//     Error_CalendarIdRequired     — ID календаря обязателен
//     Error_InvalidInputDate       — некорректный формат входной даты
//     Error_DaysCountMustBeInteger — количество дней должно быть целым числом
//     Error_EndDateRequired        — конечная дата обязательна для CountWorkdays
//     Field_EndDate                — конечная дата диапазона
//     Field_WorkdaysBetween        — рабочих дней в диапазоне
//     Field_ForceReload            — принудительная перезагрузка
//     Field_CacheAgeDays           — срок кэша в днях
//     Field_DownloadTimeoutSeconds — таймаут скачивания
// =============================================================================

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

        public const string Category_Main = "1. Основные";
        public const string Category_Output = "Выходные данные";
        public const string Category_Settings = "Настройки";
        public const string Category_Search = "Поиск";
        public const string Category_FromLists = "FromLists";
        public const string Category_Invert = "Invert";
        public const string Category_Separators = "Разделители";
        public const string Category_Elements = "Элементы";
        public const string Category_Lengths = "Длины";
        public const string Category_Statistics = "Статистика";
        public const string Category_File = "Файл";
        public const string Category_Format = "Формат";
        public const string Category_LengthRange = "LengthRange";
        public const string Category_Sertificates_SSL = "2. Сертификаты и SSL";
        public const string Category_Parameters = "Параметры";
        public const string Category_Replace = "Replace";
        public const string Category_PrefixSuffix = "Prefix/Suffix";
        public const string Category_PadTruncate = "Pad/Truncate";
        public const string Category_Join = "Join";
        public const string Category_Random = "Random";
        public const string Category_Comparison = "Сравнение";
        public const string Category_Structure = "Структура";
        public const string Category_Offset = "Смещение";
        public const string Category_Profile = "Профиль";
        public const string Category_FromList = "FromList";
        public const string Category_ToString = "ToString";
        public const string Category_Optional = "Опционально";

        public const string Category_Guid = "2. GUID";
        public const string Category_FileName = "3. Имя файла";
        public const string Category_RandomNumber = "4. Случайное число";
        public const string Category_Timestamp = "5. Временная метка";
        public const string Category_Counter = "6. Счётчик";
        public const string Category_HashId = "7. Хеш ID";
        public const string Category_Username = "8. Имя пользователя";
        public const string Category_Template = "9. Шаблон";

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
        public const string Field_PairSeparator = "Разделитель пар";
        public const string Field_KeyValueSeparator = "Разделитель ключ-значение";
        public const string Field_TrimWhitespace = "Обрезать пробелы";
        public const string Field_SkippedCount = "Пропущено пар";
        public const string Field_ParsedPairs = "Разобрано пар";
        public const string Field_ResultString = "Результирующая строка";
        public const string Field_PairCount = "Количество пар";

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
        public const string Field_Keys = "Список ключей";
        public const string Field_Values = "Список значений";
        public const string Field_IsEmpty = "Словарь пуст";
        public const string Field_FoundKeys = "Ключи с таким значением";
        public const string Field_Removed = "Ключ удалён";
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
        public const string Error_NewValueRequired = "Новое значение обязательно";
        public const string Error_StartCellRequired = "Начальная ячейка обязательна";
        public const string Error_FilePathRequired = "Путь к файлу не может быть пустым";
        public const string Error_ProfileNameRequired = "Имя профиля обязательно в режиме ReadProfile";
        public const string Error_SectionNameRequired = "Имя секции обязательно в режиме SectionToDictionary";
        public const string Error_KeyPathRequired = "Ключ обязателен в режиме SingleValue";
        public const string Error_RegexPatternRequired = "Regex паттерн обязателен для ByRegexGroup";
        public const string Error_TemplateRequired = "Шаблон обязателен";
        public const string Error_FilePatternRequired = "Маска файла не может быть пустой";
        public const string Error_CheckIntervalRequired = "Интервал проверки должен быть указан";
        public const string Error_StabilityTimeoutRequired = "Время стабильности должно быть указано";
        public const string Error_TimeoutRequired = "Таймаут должен быть указан";
        public const string Error_ListBRequired = "Список B обязателен для данной операции";
        public const string Error_ConnectionStringRequired = "Строка подключения обязательна";
        public const string Error_CommandTextRequired = "SQL команда или имя процедуры обязательны";
        public const string Error_DestinationTableRequired = "Таблица-приёмник обязательна";
        public const string Error_TransactionIdRequired = "ID транзакции обязателен";
        public const string Error_TableNameRequired = "Имя таблицы обязательно";

        // ═══════════════════════════════════════════════════════════════════════
        // НАЗВАНИЯ АКТИВНОСТЕЙ
        // ═══════════════════════════════════════════════════════════════════════

        public const string Activity_HttpRequest = "HTTP запрос";
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
        public const string Activity_DictionaryRemoveKey = "Словарь: Удалить ключ";
        public const string Activity_ListFilter = "Список: Фильтрация";
        public const string Activity_ListTransform = "Список: Преобразование";
        public const string Activity_ListSort = "Список: Сортировка";
        public const string Activity_ListAggregate = "Список: Агрегация";
        public const string Activity_ListSlice = "Список: Срез";
        public const string Activity_ListSet = "Список: Теория множеств";
        public const string Activity_ListInspect = "Список: Анализ";
        public const string Activity_ListConvert = "Список: Конвертация";
        public const string Activity_ListGroup = "Список: Группировка";
        public const string Activity_TupleCreate = "Кортеж: Создать";
        public const string Activity_TupleDestructure = "Кортеж: Деструктуризация";
        public const string Activity_TupleGet = "Кортеж: Получить элемент";
        public const string Activity_TupleSet = "Кортеж: Заменить элемент";
        public const string Activity_TupleInspect = "Кортеж: Анализ";
        public const string Activity_TupleConvert = "Кортеж: Конвертация";
        public const string Activity_TupleZip = "Кортеж: Zip списков";
        public const string Activity_TupleUnzip = "Кортеж: Unzip списков";
        public const string Activity_TupleSort = "Кортеж: Сортировка списка";
        public const string Activity_LogMessage = "Логирование";
        public const string Activity_SearchFiles = "Поиск файлов";
        public const string Activity_ReadTomlConfig = "Чтение TOML конфигурации";
        public const string Activity_Generators = "Генераторы";
        public const string Activity_ExcelCellRecalculate = "Excel: Пересчёт ячеек";
        public const string Activity_WaitForFile = "Ожидание файла";
        public const string Activity_DatabaseCheckConnection = "БД: Проверить подключение";
        public const string Activity_DatabaseQuery = "БД: Запрос";
        public const string Activity_DatabaseQueryPaged = "БД: Постраничный запрос";
        public const string Activity_DatabaseScalar = "БД: Скалярное значение";
        public const string Activity_DatabaseNonQuery = "БД: Выполнить команду";
        public const string Activity_DatabaseStoredProcedure = "БД: Stored procedure";
        public const string Activity_DatabaseBulkInsert = "БД: Массовая запись из DataTable";
        public const string Activity_DatabaseUpsert = "БД: Upsert из DataTable";
        public const string Activity_DatabaseTransactionBegin = "БД: Начать транзакцию";
        public const string Activity_DatabaseTransactionCommit = "БД: Подтвердить транзакцию";
        public const string Activity_DatabaseTransactionRollback = "БД: Откатить транзакцию";
        public const string Activity_DatabaseTableExists = "БД: Проверить таблицу";
        public const string Activity_DatabaseListTables = "БД: Список таблиц";
        public const string Activity_DatabaseListColumns = "БД: Список колонок";
        public const string Activity_DatabaseGetTableSchema = "БД: Схема таблицы";

        // ═══════════════════════════════════════════════════════════════════════
        // ДОПОЛНИТЕЛЬНЫЕ ПОЛЯ (сохранены все оригинальные константы)
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
        public const string Field_ConnectionString = "Строка подключения";
        public const string Field_DbProviderInvariantName = "Provider invariant name";
        public const string Field_CommandType = "Тип команды";
        public const string Field_CommandText = "SQL / имя процедуры";
        public const string Field_OrderByExpression = "Order by";
        public const string Field_PageNumber = "Номер страницы";
        public const string Field_CommandTimeoutSeconds = "Таймаут команды (сек)";
        public const string Field_ParametersDictionary = "Параметры";
        public const string Field_OutputParameterNames = "Выходные параметры";
        public const string Field_InputOutputParameters = "InputOutput параметры";
        public const string Field_OutputParameters = "Выходные значения";
        public const string Field_IncludeReturnValue = "Получать return value";
        public const string Field_OutputParameterSize = "Размер output-параметров";
        public const string Field_ReturnValue = "Return value";
        public const string Field_ResultTable = "Таблица-результат";
        public const string Field_HasRows = "Есть строки";
        public const string Field_TotalRows = "Всего строк";
        public const string Field_HasNextPage = "Есть следующая страница";
        public const string Field_HasPreviousPage = "Есть предыдущая страница";
        public const string Field_IsAvailable = "Подключение доступно";
        public const string Field_ServerVersion = "Версия сервера";
        public const string Field_ElapsedMs = "Время выполнения (мс)";
        public const string Field_ErrorText = "Текст ошибки";
        public const string Field_HasValue = "Есть значение";
        public const string Field_AffectedRows = "Затронуто строк";
        public const string Field_HasAffectedRows = "Есть затронутые строки";
        public const string Field_SplitByGoBatches = "Разбивать по GO batch";
        public const string Field_BatchCount = "Количество batch";
        public const string Field_ReturnIdentity = "Получать last inserted id";
        public const string Field_IdentityValue = "Last inserted id";
        public const string Field_DestinationTable = "Таблица-приёмник";
        public const string Field_ColumnMappings = "Маппинг колонок";
        public const string Field_KeyColumns = "Ключевые колонки";
        public const string Field_UpdateColumns = "Колонки обновления";
        public const string Field_BatchSize = "Размер пакета";
        public const string Field_BulkCopyTimeoutSeconds = "Таймаут bulk copy (сек)";
        public const string Field_UseTableLock = "Использовать table lock";
        public const string Field_KeepIdentity = "Сохранять identity";
        public const string Field_PreloadMode = "Очистка перед загрузкой";
        public const string Field_RowsWritten = "Записано строк";
        public const string Field_MappingCount = "Количество маппингов";
        public const string Field_WriteMode = "Режим записи";
        public const string Field_InsertedCount = "Вставлено строк";
        public const string Field_UpdatedCount = "Обновлено строк";
        public const string Field_TransactionId = "ID транзакции";
        public const string Field_IsolationLevel = "Уровень изоляции";
        public const string Field_StartedAtUtc = "Начата (UTC)";
        public const string Field_TableName = "Имя таблицы";
        public const string Field_SchemaName = "Имя схемы";
        public const string Field_IncludeViews = "Учитывать представления";
        public const string Field_Exists = "Существует";
        public const string Field_TableNames = "Список таблиц";
        public const string Field_ColumnNames = "Список колонок";
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
        public const string Field_ItemIndex = "Номер элемента";
        public const string Field_NewValue = "Новое значение";
        public const string Field_NewTuple = "Новый кортеж";
        public const string Field_ItemTypes = "Типы элементов";
        public const string Field_ItemValues = "Значения элементов";
        public const string Field_HasNulls = "Есть null элементы";
        public const string Field_AreEqual = "Равны";
        public const string Field_SameArity = "Одинаковая арность";
        public const string Field_SameTypes = "Совпадают типы";
        public const string Field_TypeName = "Тип кортежа";
        public const string Field_SecondTuple = "Второй кортеж (для сравнения)";
        public const string Field_OutItem1 = "Out: Item1";
        public const string Field_OutItem2 = "Out: Item2";
        public const string Field_OutItem3 = "Out: Item3";
        public const string Field_OutItem4 = "Out: Item4";
        public const string Field_OutItem5 = "Out: Item5";
        public const string Field_OutItem6 = "Out: Item6";
        public const string Field_OutItem7 = "Out: Item7";
        public const string Field_ValueType = "Тип значения";
        public const string Field_IncludeKeys = "Включать ключи";
        public const string Field_ResultTuple = "Кортеж (результат)";
        public const string Field_ResultList = "Список (результат)";
        public const string Field_ResultDict = "Словарь (результат)";
        public const string Field_SourceList = "Список (вход)";
        public const string Field_ConvertMode = "Режим";
        public const string Field_ListA_Item1 = "Список A (Item1)";
        public const string Field_ListB_Item2 = "Список B (Item2)";
        public const string Field_ListC_Item3 = "Список C (Item3, необязателен)";
        public const string Field_TupleList = "Список кортежей";
        public const string Field_TupleArity = "Арность кортежей";
        public const string Field_ListA_FromItem1 = "Список A (из Item1)";
        public const string Field_ListB_FromItem2 = "Список B (из Item2)";
        public const string Field_ListC_FromItem3 = "Список C (из Item3)";
        public const string Field_SortKey = "Ключ сортировки";
        public const string Field_Direction = "Направление";
        public const string Field_SortType = "Тип сортировки";
        public const string Field_GroupingMode = "Способ группировки";
        public const string Field_PrefixLength = "Длина префикса";
        public const string Field_RegexPattern = "Regex паттерн";
        public const string Field_TopN = "Топ N (для TopFrequent)";
        public const string Field_Groups = "Группы";
        public const string Field_GroupCount = "Количество групп";
        public const string Field_FrequencyMap = "Частота элементов";
        public const string Field_SortMode = "Способ сортировки";
        public const string Field_RandomSeed = "Зерно (для Random)";
        public const string Field_ListA = "Список A";
        public const string Field_ListB = "Список B";
        public const string Field_Operation = "Операция";
        public const string Field_SliceMode = "Способ среза";
        public const string Field_N = "N (кол-во / шаг)";
        public const string Field_Page = "Номер страницы";
        public const string Field_PageSize = "Размер страницы";
        public const string Field_FromIndex = "Индекс от (включ.)";
        public const string Field_ToIndex = "Индекс до (включ.)";
        public const string Field_TotalPages = "Всего страниц";
        public const string Field_TransformMode = "Преобразование";
        public const string Field_Find = "Найти";
        public const string Field_Replacement = "Заменить на";
        public const string Field_Prefix = "Префикс";
        public const string Field_Suffix = "Суффикс";
        public const string Field_PadWidth = "Ширина (Pad)";
        public const string Field_PadChar = "Символ заполнения";
        public const string Field_MaxLengthTruncate = "Макс. длина (Truncate)";
        public const string Field_AggregateFunction = "Агрегатная функция";
        public const string Field_Separator = "Разделитель";
        public const string Field_StringResult = "Строковый результат";
        public const string Field_NumericResult = "Числовой результат";
        public const string Field_NumericCount = "Кол-во числовых";
        public const string Field_ChangedCount = "Изменилось элементов";
        public const string Field_Query = "Строка поиска / паттерн";
        public const string Field_Target = "Где искать";
        public const string Field_SearchMethod = "Метод поиска";
        public const string Field_FilteredOutCount = "Отсеяно фильтром";
        public const string Field_StartCell = "Начальная ячейка";
        public const string Field_RowOffset = "Смещение по строке (ΔR)";
        public const string Field_ColumnOffset = "Смещение по столбцу (ΔC)";
        public const string Field_TargetCell = "Целевая ячейка";
        public const string Field_GenerationType = "Тип генерации";
        public const string Field_GuidFormat = "Формат GUID";
        public const string Field_BaseName = "Базовое имя";
        public const string Field_Extension = "Расширение";
        public const string Field_MinValue = "Минимальное значение";
        public const string Field_MaxValue = "Максимальное значение";
        public const string Field_TimestampFormatField = "Формат даты";
        public const string Field_CustomTimestampFormat = "Кастомный формат";
        public const string Field_CounterKey = "Ключ счётчика";
        public const string Field_CounterStart = "Начальное значение";
        public const string Field_CounterStep = "Шаг";
        public const string Field_HashPrefix = "Префикс";
        public const string Field_FirstName = "Имя";
        public const string Field_LastName = "Фамилия";
        public const string Field_Domain = "Домен";
        public const string Field_Template = "Шаблон";
        public const string Field_Variables = "Переменные";
        public const string Field_OutputVariable = "Результат";
        public const string Field_FilePath = "Путь к файлу";
        public const string Field_ReadMode = "Режим чтения";
        public const string Field_KeyPath = "Ключ (section.key)";
        public const string Field_SectionName = "Секция";
        public const string Field_Encoding = "Кодировка";
        public const string Field_ProfileName = "Имя профиля";
        public const string Field_DefaultProfileName = "Имя секции default";
        public const string Field_MergeStrategy = "Стратегия слияния";
        public const string Field_IncludeNestedSections = "Включать вложенные секции";
        public const string Field_StringValue = "Строковое значение";
        public const string Field_DictionaryValues = "Словарь значений";
        public const string Field_KeyProfileFound = "Ключ/профиль найден";
        public const string Field_KeysCount = "Кол-во ключей";
        public const string Field_AvailableProfiles = "Доступные профили";
        public const string Field_ProfileKeysCount = "Ключей из профиля";
        public const string Field_DefaultKeysCount = "Ключей из default";
        public const string Field_WaitMode = "Режим ожидания";
        public const string Field_FilePattern = "Маска файла";
        public const string Field_CheckInterval = "Интервал проверки (мс)";
        public const string Field_WaitForStability = "Ожидать завершения записи";
        public const string Field_StabilityTimeout = "Время стабильности (мс)";
        public const string Field_ThrowOnTimeout = "Ошибка при таймауте";
        public const string Field_FileFound = "Файл найден";
        public const string Field_FileSize = "Размер файла (байт)";
        public const string Field_WaitTime = "Время ожидания (мс)";

        // ═══════════════════════════════════════════════════════════════════════
        // BROWSER / SELENIUM
        // ═══════════════════════════════════════════════════════════════════════

        public const string Category_Browser = "Браузер";
        public const string Category_BrowserOptions = "2. Опции браузера";
        public const string Category_Element = "Элемент";
        public const string Category_Wait = "Ожидание";
        public const string Category_Locator = "Локатор";
        public const string Field_SessionId = "ID сессии";
        public const string Field_BrowserType = "Тип браузера";
        public const string Field_Headless = "Headless режим";
        public const string Field_IncognitoMode = "Режим инкогнито";
        public const string Field_DisableImages = "Отключить изображения";
        public const string Field_UserAgent = "User-Agent";
        public const string Field_DriverPath = "Путь к драйверу";
        public const string Field_WindowSize = "Размер окна";
        public const string Field_PageLoadTimeout = "Таймаут загрузки (сек)";
        public const string Field_ImplicitWait = "Неявное ожидание (сек)";
        public const string Field_LocatorType = "Тип локатора";
        public const string Field_LocatorValue = "Значение локатора";
        public const string Field_ElementId = "ID элемента";
        public const string Field_ElementIds = "Список ID элементов";
        public const string Field_WaitTimeout = "Таймаут (сек)";
        public const string Field_WaitCondition = "Условие ожидания";
        public const string Field_Text = "Текст";
        public const string Field_ClearBeforeType = "Очистить перед вводом";
        public const string Field_SimulateTyping = "Эмуляция печати";
        public const string Field_TypingDelay = "Задержка между символами (мс)";
        public const string Field_WaitAfterClick = "Ожидать после клика";
        public const string Field_ClickBeforeType = "Кликнуть перед вводом";
        public const string Field_PressEnterAfter = "Нажать Enter после ввода";
        public const string Field_SpecialKey = "Специальная клавиша";
        public const string Field_RepeatCount = "Количество повторений";
        public const string Field_PropertyName = "Имя свойства";
        public const string Field_AttributeName = "Имя атрибута";
        public const string Field_CssProperty = "CSS свойство";
        public const string Field_IsVisible = "Видим";
        public const string Field_IsEnabled = "Включён";
        public const string Field_IsSelected = "Выбран";
        public const string Field_ElementExists = "Элемент существует";
        public const string Field_ElementText = "Текст элемента";
        public const string Field_ElementValue = "Значение элемента";
        public const string Field_ElementCount = "Количество элементов";
        public const string Field_NavigateUrl = "URL для перехода";
        public const string Field_CurrentUrl = "Текущий URL";
        public const string Field_PageTitle = "Заголовок страницы";
        public const string Field_PageSource = "HTML код страницы";
        public const string Field_ScreenshotPath = "Путь к скриншоту";
        public const string Field_ScreenshotBase64 = "Скриншот (Base64)";
        public const string Field_JavaScriptCode = "JavaScript код";
        public const string Field_ScriptArguments = "Аргументы скрипта";
        public const string Field_ScriptResult = "Результат выполнения";
        public const string Field_SelectMode = "Режим выбора";
        public const string Field_SelectValue = "Значение для выбора";
        public const string Field_SelectedOptions = "Выбранные опции";
        public const string Field_CookieOperation = "Операция";
        public const string Field_CookieName = "Имя cookie";
        public const string Field_CookieValue = "Значение cookie";
        public const string Field_CookieDomain = "Домен";
        public const string Field_CookiePath = "Путь";
        public const string Field_CookieExpiry = "Срок действия";
        public const string Field_Cookies = "Cookies";
        public const string Field_SwitchToType = "Тип контекста";
        public const string Field_FrameLocator = "Локатор фрейма";
        public const string Field_WindowHandle = "Handle окна";
        public const string Field_WindowHandles = "Список handles";
        public const string Field_AlertAction = "Действие";
        public const string Field_AlertText = "Текст алерта";
        public const string Field_AlertInput = "Текст для ввода";
        public const string Field_ScrollAlignment = "Выравнивание";
        public const string Field_ScrollPixels = "Пиксели";
        public const string Field_ScrollDirection = "Направление";
        public const string Error_SessionNotFound = "Сессия браузера не найдена";
        public const string Error_LocatorValueRequired = "Значение локатора обязательно";
        public const string Error_ElementIdRequired = "ID элемента обязателен";
        public const string Error_ElementNotFound = "Элемент не найден";
        public const string Error_TextRequired = "Текст обязателен";
        public const string Error_JavaScriptRequired = "JavaScript код обязателен";
        public const string Error_CookieNameRequired = "Имя cookie обязательно";
        public const string Error_InvalidWindowHandle = "Некорректный handle окна";
        public const string Activity_BrowserOpen = "Браузер: Открыть";
        public const string Activity_BrowserClose = "Браузер: Закрыть";
        public const string Activity_BrowserNavigate = "Браузер: Навигация";
        public const string Activity_BrowserGetInfo = "Браузер: Получить информацию";
        public const string Activity_BrowserScreenshot = "Браузер: Скриншот";
        public const string Activity_ElementFind = "Элемент: Найти";
        public const string Activity_ElementFindAll = "Элемент: Найти все";
        public const string Activity_ElementClick = "Элемент: Клик";
        public const string Activity_ElementTypeText = "Элемент: Ввод текста";
        public const string Activity_ElementGetProperty = "Элемент: Получить свойство";
        public const string Activity_ElementExists = "Элемент: Проверить существование";
        public const string Activity_ElementIsVisible = "Элемент: Проверить видимость";
        public const string Activity_BrowserWaitFor = "Браузер: Ожидание условия";
        public const string Activity_BrowserExecuteJS = "Браузер: Выполнить JavaScript";
        public const string Activity_ElementSelect = "Элемент: Выбор в списке";
        public const string Activity_BrowserManageCookies = "Браузер: Управление cookies";
        public const string Activity_BrowserSwitchTo = "Браузер: Переключить контекст";
        public const string Activity_AlertHandle = "Браузер: Обработка алерта";
        public const string Activity_ElementScrollTo = "Элемент: Прокрутить к элементу";
        public const string Activity_ElementSendKeys = "Элемент: Нажать клавишу";

        // ═══════════════════════════════════════════════════════════════════════
        // DATATABLE TO HTML
        // ═══════════════════════════════════════════════════════════════════════

        public const string Category_Style = "Стиль";
        public const string Field_DataTable = "Таблица данных";
        public const string Field_HtmlTheme = "Тема оформления";
        public const string Field_HtmlOutputMode = "Режим вывода HTML";
        public const string Field_ShowRowNumbers = "Нумерация строк";
        public const string Field_NullDisplay = "Текст для null";
        public const string Field_MaxRows = "Макс. строк";
        public const string Field_HeaderColor = "Цвет заголовка";
        public const string Field_RowColor = "Цвет строк";
        public const string Field_AltRowColor = "Цвет чётных строк";
        public const string Field_BorderColor = "Цвет границ";
        public const string Field_HtmlOutput = "HTML-код";
        public const string Field_RowCount = "Количество строк";
        public const string Field_ColumnCount = "Количество столбцов";
        public const string Field_UsedDefaultValue = "Использовано значение по умолчанию";
        public const string Field_IntValue = "Значение как Int32";
        public const string Field_DecimalValue = "Значение как Decimal";
        public const string Field_BoolValue = "Значение как Boolean";
        public const string Field_DateTimeValue = "Значение как DateTime";
        public const string Error_DataTableRequired = "Исходная таблица данных не указана";
        public const string Error_MaxRowsNegative = "Максимальное количество строк не может быть отрицательным";
        public const string Error_CustomThemeRequiresColors = "При выборе пользовательской темы необходимо указать хотя бы один цвет";

        // ═══════════════════════════════════════════════════════════════════════
        // BUSINESS CALENDAR (Производственный календарь)
        // ═══════════════════════════════════════════════════════════════════════

        public const string Activity_CalendarLoad      = "Календарь: Загрузить";
        public const string Activity_BusinessCalendar  = "Календарь: Операции";

        // Fields - CalendarLoad
        public const string Field_CalendarPath     = "Путь к папке календаря";
        public const string Field_CalendarYear     = "Год";
        public const string Field_CalendarFormat   = "Формат файла";
        public const string Field_CalendarRegion   = "Регион";
        public const string Field_DownloadIfMissing = "Загрузить из интернета";
        public const string Field_Calendar         = "Календарь";
        public const string Field_CalendarLoaded   = "Календарь загружен";
        public const string Field_CalendarFilePath = "Путь к файлу календаря";
        public const string Field_LoadedFrom       = "Загружен из";
        public const string Field_CalendarSource   = "Источник";
        public const string Field_CalendarID       = "ID календаря";

        /// <summary>[П1] Принудительно скачать файл заново.</summary>
        public const string Field_ForceReload           = "Принудительная перезагрузка";

        /// <summary>[П5] Максимальный возраст кэша в днях.</summary>
        public const string Field_CacheAgeDays          = "Срок кэша (дней)";

        /// <summary>[П6] Таймаут HTTP-скачивания в секундах.</summary>
        public const string Field_DownloadTimeoutSeconds = "Таймаут скачивания (сек)";

        // Fields - BusinessCalendar
        public const string Field_OperationMode    = "Режим операции";
        public const string Field_InputDate        = "Входная дата";
        public const string Field_DaysCount        = "Количество дней";
        public const string Field_ResultDate       = "Результат (дата)";
        public const string Field_DayType          = "Тип дня";
        public const string Field_IsWorkday        = "Рабочий день";
        public const string Field_HolidayName      = "Название праздника";
        public const string Field_WorkdaysInYear   = "Рабочих дней в году";
        public const string Field_HolidaysInYear   = "Праздничных дней в году";
        public const string Field_Hours40          = "Часов (40-час. неделя)";
        public const string Field_Hours36          = "Часов (36-час. неделя)";
        public const string Field_Hours24          = "Часов (24-час. неделя)";

        /// <summary>[П2] Конечная дата диапазона для CountWorkdays.</summary>
        public const string Field_EndDate          = "Конечная дата";

        /// <summary>[П2] Количество рабочих дней в диапазоне.</summary>
        public const string Field_WorkdaysBetween  = "Рабочих дней в диапазоне";

        // Errors - Calendar (оригинальные)
        public const string Error_CalendarPathRequired    = "Путь к папке календаря обязателен";
        public const string Error_CalendarYearRequired    = "Год календаря обязателен";
        public const string Error_CalendarRequired        = "Календарь не загружен";
        public const string Error_InputDateRequired       = "Входная дата обязательна";
        public const string Error_DaysCountRequired       = "Количество дней обязательно";
        public const string Error_CalendarFileNotFound    = "Файл календаря не найден";
        public const string Error_CalendarDownloadFailed  = "Не удалось скачать календарь из интернета";
        public const string Error_CalendarParseFailed     = "Ошибка разбора файла календаря";
        public const string Error_CalendarUnsupportedFormat = "Неподдерживаемый формат календаря";
        public const string Error_DateOutOfRange          = "Дата вне диапазона календаря";
        public const string Error_YearMustBeInteger       = "Год должен быть целым числом";

        // Errors - Calendar (новые)
        /// <summary>ID календаря не указан в поле «ID календаря».</summary>
        public const string Error_CalendarIdRequired      = "ID календаря обязателен";

        /// <summary>Поле «Входная дата» содержит некорректное значение.</summary>
        public const string Error_InvalidInputDate        = "Некорректный формат входной даты. Ожидается значение типа DateTime.";

        /// <summary>Поле «Количество дней» содержит нецелое значение.</summary>
        public const string Error_DaysCountMustBeInteger  = "Количество дней должно быть целым числом";

        /// <summary>[П2] Конечная дата обязательна для режима CountWorkdays.</summary>
        public const string Error_EndDateRequired         = "Конечная дата обязательна для режима «Рабочих дней в диапазоне»";

        // ═══════════════════════════════════════════════════════════════════════
        // CALENDAR WORKDAYS LIST (Рабочие дни списком)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Название активности «Календарь: Рабочие дни списком».</summary>
        public const string Activity_CalendarWorkdaysList = "Календарь: Рабочие дни списком";

        // Fields
        /// <summary>Начальная дата диапазона.</summary>
        public const string Field_StartDate              = "Начальная дата";

        /// <summary>Включать сокращённые рабочие дни (ShortWorkday).</summary>
        public const string Field_IncludeShortDays       = "Включать сокращённые дни";

        /// <summary>Фильтр по дням недели (DayOfWeekFilter).</summary>
        public const string Field_DayOfWeekFilter        = "Фильтр дней недели";

        /// <summary>Пользовательская маска дней недели (строка "1,3,5").</summary>
        public const string Field_DayOfWeekMask          = "Маска дней (Пн=1..Вс=7)";

        /// <summary>Выходной список рабочих дней List&lt;DateTime&gt;.</summary>
        public const string Field_WorkdaysList           = "Список рабочих дней";

        /// <summary>Количество найденных рабочих дней.</summary>
        public const string Field_WorkdaysCount          = "Количество дней";

        /// <summary>Первый рабочий день в диапазоне.</summary>
        public const string Field_FirstWorkday           = "Первый рабочий день";

        /// <summary>Последний рабочий день в диапазоне.</summary>
        public const string Field_LastWorkday            = "Последний рабочий день";

        // Errors
        /// <summary>Начальная дата не указана или некорректна.</summary>
        public const string Error_StartDateRequired      = "Начальная дата обязательна";

        /// <summary>Некорректный формат начальной даты.</summary>
        public const string Error_InvalidStartDate       = "Некорректный формат начальной даты. Ожидается значение типа DateTime.";

        /// <summary>Некорректный формат конечной даты.</summary>
        public const string Error_InvalidEndDate         = "Некорректный формат конечной даты. Ожидается значение типа DateTime.";

        /// <summary>Маска дней недели обязательна при фильтре CustomMask.</summary>
        public const string Error_DayOfWeekMaskRequired  = "Маска дней недели обязательна при выборе фильтра CustomMask. Формат: \"1,3,5\" (Пн=1..Вс=7).";

        /// <summary>Строка маски дней недели содержит некорректные значения.</summary>
        public const string Error_InvalidDayOfWeekMask   = "Некорректная маска дней недели. Используйте номера 1–7 через запятую (Пн=1, Вт=2, Ср=3, Чт=4, Пт=5, Сб=6, Вс=7). Пример: \"1,3,5\".";

        // ═══════════════════════════════════════════════════════════════════════
        // CALENDAR NTH WORKDAY (Nth рабочий день)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Название активности «Календарь: Nth рабочий день».</summary>
        public const string Activity_CalendarNthWorkday  = "Календарь: Nth рабочий день";

        // Fields
        /// <summary>Номер рабочего дня N (1=первый, -1=последний).</summary>
        public const string Field_NthNumber              = "N (номер рабочего дня)";

        /// <summary>Тип периода (Month / Quarter / HalfYear / Year).</summary>
        public const string Field_NthPeriod              = "Период";

        /// <summary>Месяц периода (1–12) для Period=Month.</summary>
        public const string Field_NthMonth               = "Месяц (1–12)";

        /// <summary>Квартал (1–4) или полугодие (1–2).</summary>
        public const string Field_NthQuarter             = "Квартал / Полугодие";

        /// <summary>Общее количество рабочих дней в периоде.</summary>
        public const string Field_TotalWorkdays          = "Рабочих дней в периоде";

        /// <summary>Флаг: найден ли N-й рабочий день.</summary>
        public const string Field_NthIsFound             = "Найден";

        // Errors
        /// <summary>N не указан или равен нулю.</summary>
        public const string Error_NthNRequired           = "N обязателен и не может быть равен нулю (1=первый, -1=последний)";

        /// <summary>Месяц не указан при Period=Month.</summary>
        public const string Error_NthMonthRequired       = "Месяц обязателен при периоде Month";

        /// <summary>Квартал/полугодие не указаны при Period=Quarter или HalfYear.</summary>
        public const string Error_NthQuarterRequired     = "Квартал / номер полугодия обязателен для выбранного периода";

        // ═══════════════════════════════════════════════════════════════════════
        // CALENDAR NEXT DAY OF WEEK (Следующий день недели)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Название активности.</summary>
        public const string Activity_CalendarNextDayOfWeek = "Календарь: Следующий день недели";

        /// <summary>Искомый день недели (TargetDayOfWeek).</summary>
        public const string Field_TargetDayOfWeek        = "День недели";

        /// <summary>Режим поиска (DayOfWeekSearchMode).</summary>
        public const string Field_DayOfWeekSearchMode    = "Режим поиска";

        /// <summary>Поведение при попадании на выходной (DeadlineWeekendBehavior).</summary>
        public const string Field_IfWeekend              = "При выходном";

        /// <summary>Учитывать стартовую дату если она совпадает с нужным днём.</summary>
        public const string Field_IncludeStartDate       = "Включать стартовую дату";

        /// <summary>Максимум дней поиска вперёд.</summary>
        public const string Field_MaxLookAhead           = "Макс. дней поиска";

        /// <summary>Сколько дней от StartDate до найденного результата.</summary>
        public const string Field_DaysAhead              = "Дней вперёд";

        // ═══════════════════════════════════════════════════════════════════════
        // TEXT TEMPLATE (Шаблонизатор)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Название активности «Текст: Шаблонизатор».</summary>
        public const string Activity_TextTemplate        = "Текст: Шаблонизатор";

        // Fields
        /// <summary>Синтаксис плейсхолдеров (TemplateSyntax).</summary>
        public const string Field_TemplateSyntax         = "Синтаксис";

        /// <summary>Поведение при отсутствии ключа в словаре (MissingKeyBehavior).</summary>
        public const string Field_MissingKeyBehavior     = "При отсутствии ключа";

        /// <summary>Количество выполненных замен в шаблоне.</summary>
        public const string Field_TemplateReplacedCount  = "Количество замен";

        /// <summary>Список ключей из шаблона, для которых не нашлось значений.</summary>
        public const string Field_TemplateMissingKeys    = "Незаполненные ключи";

        // Errors

        /// <summary>Словарь переменных не указан.</summary>
        public const string Error_TemplateVariablesRequired = "Словарь переменных обязателен";

        /// <summary>Файл шаблона не найден по указанному пути.</summary>
        public const string Error_TemplateFileNotFound   = "Файл шаблона не найден";

        /// <summary>В шаблоне есть плейсхолдеры без значений (режим ThrowError).</summary>
        public const string Error_TemplateMissingKeys    = "Незаполненные плейсхолдеры";

        // ═══════════════════════════════════════════════════════════════════════
        // TEXT PARSE (Разбор по шаблону)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Название активности «Текст: Разбор по шаблону».</summary>
        public const string Activity_TextParse           = "Текст: Разбор по шаблону";

        // Fields
        /// <summary>Маска с плейсхолдерами для разбора строки.</summary>
        public const string Field_ParseMask              = "Маска";

        /// <summary>Признак: маска успешно совпала со строкой.</summary>
        public const string Field_IsMatched              = "Совпало";

        /// <summary>Количество совпадений или извлечённых ключей.</summary>
        public const string Field_MatchCount           = "Количество совпадений";

        /// <summary>Результат разбора — Dictionary первого совпадения.</summary>
        public const string Field_ParseResult            = "Результат";

        /// <summary>Список всех совпадений (при AllMatches=true).</summary>
        public const string Field_AllResults             = "Все результаты";

        /// <summary>Жадный захват плейсхолдеров.</summary>
        public const string Field_GreedyMatch            = "Жадный захват";

        /// <summary>Многострочный режим (. совпадает с \n).</summary>
        public const string Field_MultiLine              = "Многострочный текст";

        /// <summary>Найти все вхождения маски в тексте.</summary>
        public const string Field_AllMatches             = "Все вхождения";

        /// <summary>Скомпилированный regex-паттерн для диагностики.</summary>
        public const string Field_CompiledPattern        = "Скомпилированный паттерн";

        // Errors
        /// <summary>Маска не указана.</summary>
        public const string Error_ParseMaskRequired      = "Маска обязательна";

        /// <summary>Маска содержит синтаксическую ошибку или недопустимые имена групп.</summary>
        public const string Error_ParseMaskInvalid       = "Некорректная маска";

        // ═══════════════════════════════════════════════════════════════════════
        // TEXT TRANSLIT (Транслитерация)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Название активности «Текст: Транслитерация».</summary>
        public const string Activity_TextTranslit = "Текст: Транслитерация";

        // Fields
        /// <summary>Направление транслитерации (TranslitDirection).</summary>
        public const string Field_TranslitDirection = "Направление";

        /// <summary>Схема транслитерации (TranslitScheme).</summary>
        public const string Field_TranslitScheme = "Схема";

        /// <summary>Сохранять регистр оригинального символа.</summary>
        public const string Field_PreserveCase = "Сохранять регистр";

        /// <summary>Оставлять нетранслитерируемые символы без изменений.</summary>
        public const string Field_PreserveNonAlpha = "Сохранять нетранслитерируемые";

        /// <summary>Привести результат к верхнему регистру.</summary>
        public const string Field_ToUpperCase = "К верхнему регистру";

        /// <summary>Привести результат к нижнему регистру.</summary>
        public const string Field_ToLowerCase = "К нижнему регистру";

        /// <summary>Заменять пробелы в результате указанной строкой.</summary>
        public const string Field_ReplaceSpaces = "Замена пробелов";

        /// <summary>Определённое направление при AutoDetect.</summary>
        public const string Field_DetectedDirection = "Определённое направление";

        /// <summary>Количество изменённых символов.</summary>
        public const string Field_ChangedChars = "Изменено символов";

        // ═══════════════════════════════════════════════════════════════════════
        // TEXT EXTRACT ENTITIES (Извлечь сущности)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Название активности «Текст: Извлечь сущности».</summary>
        public const string Activity_TextExtractEntities = "Текст: Извлечь сущности";

        // Fields
        /// <summary>Типы сущностей для поиска через запятую.</summary>
        public const string Field_EntityTypes = "Типы сущностей";

        /// <summary>Пользовательский Regex-паттерн для типа Custom.</summary>
        public const string Field_CustomPattern = "Пользовательский паттерн (Regex)";

        /// <summary>Пользовательский Regex-паттерн для типа Custom.</summary>
        public const string Field_CustomTypeName = "Имя пользовательского типа";

        /// <summary>Нормализовать найденные значения.</summary>
        public const string Field_NormalizeResults = "Нормализация";

        /// <summary>Проверять контрольные цифры ИНН.</summary>
        public const string Field_ValidateINN = "Валидация ИНН";

        /// <summary>Минимальный уровень уверенности паттерна (0–100).</summary>
        public const string Field_MinConfidence = "Мин. уверенность";

        /// <summary>Словарь найденных сущностей по типу.</summary>
        public const string Field_EntityResults = "Результаты";

        /// <summary>Плоский список всех найденных значений.</summary>
        public const string Field_AllFound = "Все найденные";

        /// <summary>Общее количество найденных сущностей.</summary>
        public const string Field_EntityTotalCount = "Всего найдено";

        /// <summary>Есть хотя бы одно совпадение.</summary>
        public const string Field_HasMatches = "Есть совпадения";

        /// <summary>Список типов с хотя бы одним совпадением.</summary>
        public const string Field_FoundTypes = "Найденные типы";

        // ═══════════════════════════════════════════════════════════════════════
        // FILE CLEANUP (Очистка файлов и папок по времени)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Название активности «Файл: Очистка по времени».</summary>
        public const string Activity_FileCleanup = "Файл: Очистка по времени";

        // Fields
        /// <summary>Путь к папке для очистки.</summary>
        public const string Field_CleanupFolder = "Папка для очистки";

        /// <summary>Что удалять (CleanupTargetType).</summary>
        public const string Field_CleanupTargetType = "Что удалять";

        /// <summary>Временной атрибут для сравнения (FileTimeAttribute).</summary>
        public const string Field_CleanupTimeAttribute = "Атрибут времени";

        /// <summary>Способ задания порога (CleanupThresholdMode).</summary>
        public const string Field_CleanupThresholdMode = "Режим порога";

        /// <summary>Удалять объекты старше N дней.</summary>
        public const string Field_CleanupOlderThanDays = "Старше чем (дней)";

        /// <summary>Удалять объекты с датой раньше указанной.</summary>
        public const string Field_CleanupThresholdDate = "Раньше даты";

        /// <summary>Маска имён файлов (wildcard: *.tmp, *.log).</summary>
        public const string Field_CleanupFilePattern = "Маска файлов";

        /// <summary>Обходить подпапки рекурсивно.</summary>
        public const string Field_CleanupRecursive = "Рекурсивно";

        /// <summary>Не удалять файлы меньше N байт.</summary>
        public const string Field_CleanupMinSize = "Мин. размер (байт)";

        /// <summary>Ограничить количество удалений за один вызов.</summary>
        public const string Field_CleanupMaxItems = "Макс. объектов";

        /// <summary>Режим DryRun — ничего не удалять, только показать список.</summary>
        public const string Field_CleanupDryRun = "Пробный запуск (DryRun)";

        /// <summary>Продолжать при ошибке удаления отдельного файла.</summary>
        public const string Field_CleanupIgnoreErrors = "Игнорировать ошибки";

        /// <summary>Количество удалённых объектов.</summary>
        public const string Field_CleanupDeletedCount = "Удалено объектов";

        /// <summary>Список путей удалённых объектов.</summary>
        public const string Field_CleanupDeletedPaths = "Список удалённых";

        /// <summary>Суммарный размер удалённых файлов в байтах.</summary>
        public const string Field_CleanupFreedBytes = "Освобождено (байт)";

        /// <summary>Список ошибок удаления.</summary>
        public const string Field_CleanupErrors = "Ошибки удаления";

        // Errors
        /// <summary>Путь к папке не указан.</summary>
        public const string Error_CleanupFolderRequired = "Путь к папке обязателен";

        /// <summary>Папка не найдена.</summary>
        public const string Error_CleanupFolderNotFound = "Папка не найдена";

        /// <summary>Количество дней не указано или некорректно.</summary>
        public const string Error_CleanupDaysRequired = "Количество дней обязательно и должно быть >= 0";

        /// <summary>Дата порога не указана или некорректна.</summary>
        public const string Error_CleanupDateRequired = "Дата порога обязательна и должна быть корректной датой";

    }
}
