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
        
        // Категории для Generators
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
        
        // Dictionary (дополнительные)
        public const string Activity_DictionaryRemoveKey = "Словарь: Удалить ключ";
        
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
        
        // Tuple fields
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
        
        // List fields
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
        
        // Dictionary fields
        public const string Field_Query = "Строка поиска / паттерн";
        public const string Field_Target = "Где искать";
        public const string Field_SearchMethod = "Метод поиска";
        public const string Field_FilteredOutCount = "Отсеяно фильтром";
        
        // Excel fields
        public const string Field_StartCell = "Начальная ячейка";
        public const string Field_RowOffset = "Смещение по строке (ΔR)";
        public const string Field_ColumnOffset = "Смещение по столбцу (ΔC)";
        public const string Field_TargetCell = "Целевая ячейка";
        
        // Generator fields
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
        
        // TOML Config fields
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
        
                // WaitForFile fields
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
        
        // Browser categories
        public const string Category_Browser = "Браузер";
        public const string Category_BrowserOptions = "2. Опции браузера";
        public const string Category_Element = "Элемент";
        public const string Category_Wait = "Ожидание";
        public const string Category_Locator = "Локатор";
        
        // Browser fields
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
        
        // Element locator fields
        public const string Field_LocatorType = "Тип локатора";
        public const string Field_LocatorValue = "Значение локатора";
        public const string Field_ElementId = "ID элемента";
        public const string Field_ElementIds = "Список ID элементов";
        public const string Field_WaitTimeout = "Таймаут (сек)";
        public const string Field_WaitCondition = "Условие ожидания";
        
                // Element interaction fields
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
        
        // Element state fields
        public const string Field_IsVisible = "Видим";
        public const string Field_IsEnabled = "Включён";
        public const string Field_IsSelected = "Выбран";
        public const string Field_ElementExists = "Элемент существует";
        public const string Field_ElementText = "Текст элемента";
        public const string Field_ElementValue = "Значение элемента";
        public const string Field_ElementCount = "Количество элементов";
        
        // Browser navigation fields
        public const string Field_NavigateUrl = "URL для перехода";
        public const string Field_CurrentUrl = "Текущий URL";
        public const string Field_PageTitle = "Заголовок страницы";
        public const string Field_PageSource = "HTML код страницы";
        
        // Screenshot fields
        public const string Field_ScreenshotPath = "Путь к скриншоту";
        public const string Field_ScreenshotBase64 = "Скриншот (Base64)";
        
        // JavaScript fields
        public const string Field_JavaScriptCode = "JavaScript код";
        public const string Field_ScriptArguments = "Аргументы скрипта";
        public const string Field_ScriptResult = "Результат выполнения";
        
        // Select fields
        public const string Field_SelectMode = "Режим выбора";
        public const string Field_SelectValue = "Значение для выбора";
        public const string Field_SelectedOptions = "Выбранные опции";
        
        // Cookie fields
        public const string Field_CookieOperation = "Операция";
        public const string Field_CookieName = "Имя cookie";
        public const string Field_CookieValue = "Значение cookie";
        public const string Field_CookieDomain = "Домен";
        public const string Field_CookiePath = "Путь";
        public const string Field_CookieExpiry = "Срок действия";
        public const string Field_Cookies = "Cookies";
        
        // Switch context fields
        public const string Field_SwitchToType = "Тип контекста";
        public const string Field_FrameLocator = "Локатор фрейма";
        public const string Field_WindowHandle = "Handle окна";
        public const string Field_WindowHandles = "Список handles";
        
        // Alert fields
        public const string Field_AlertAction = "Действие";
        public const string Field_AlertText = "Текст алерта";
        public const string Field_AlertInput = "Текст для ввода";
        
        // Scroll fields
        public const string Field_ScrollAlignment = "Выравнивание";
        public const string Field_ScrollPixels = "Пиксели";
        public const string Field_ScrollDirection = "Направление";
        
        // Browser errors
        public const string Error_SessionNotFound = "Сессия браузера не найдена";
        public const string Error_LocatorValueRequired = "Значение локатора обязательно";
        public const string Error_ElementIdRequired = "ID элемента обязателен";
        public const string Error_ElementNotFound = "Элемент не найден";
        public const string Error_TextRequired = "Текст обязателен";
        public const string Error_JavaScriptRequired = "JavaScript код обязателен";
        public const string Error_CookieNameRequired = "Имя cookie обязательно";
        public const string Error_InvalidWindowHandle = "Некорректный handle окна";
        
        // Browser activities
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
    }
}
