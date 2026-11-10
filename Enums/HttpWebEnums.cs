namespace Primo.MIA
{
    /// <summary>
    /// HTTP-методы для выполнения запросов.
    /// </summary>
    public enum HttpMethodType
    {
        /// <summary>GET — получение данных</summary>
        [RuName("GET")]
        GET,
        /// <summary>POST — отправка данных</summary>
        [RuName("POST")]
        POST,
        /// <summary>PUT — обновление ресурса</summary>
        [RuName("PUT")]
        PUT,
        /// <summary>DELETE — удаление ресурса</summary>
        [RuName("DELETE")]
        DELETE,
        /// <summary>PATCH — частичное обновление</summary>
        [RuName("PATCH")]
        PATCH
    }

    /// <summary>
    /// Стратегия задержки между повторами HTTP-запросов.
    /// Используется в активности «HTTP: Запрос с повторами».
    /// </summary>
    public enum HttpRetryDelayStrategy
    {
        /// <summary>
        /// Постоянная задержка между попытками.
        /// Каждая попытка ждёт одинаковое время.
        /// Пример: задержка 1000мс → попытка 1, 1000мс, попытка 2, 1000мс, попытка 3.
        /// </summary>
        [RuName("Фиксированная")]
        Fixed,

        /// <summary>
        /// Линейное увеличение задержки.
        /// Задержка умножается на номер попытки.
        /// Пример: задержка 1000мс → попытка 1, 1000мс, попытка 2, 2000мс, попытка 3, 3000мс.
        /// </summary>
        [RuName("Линейная")]
        Linear,

        /// <summary>
        /// Экспоненциальное увеличение задержки (backoff).
        /// Задержка умножается на 2^(номер попытки - 1).
        /// Пример: задержка 1000мс → попытка 1, 1000мс, попытка 2, 2000мс, попытка 3, 4000мс.
        /// Рекомендуется для распределённых систем и API.
        /// </summary>
        [RuName("Экспоненциальная")]
        Exponential
    }

    /// <summary>
    /// Тип OAuth2 Grant Type для авторизации.
    /// Используется в активности «HTTP: OAuth2 токен».
    /// </summary>
    public enum OAuth2GrantType
    {
        /// <summary>
        /// Client Credentials Flow — авторизация от имени приложения.
        /// Использует client_id и client_secret без участия пользователя.
        /// Подходит для server-to-server интеграций.
        /// </summary>
        [RuName("Client credentials")]
        ClientCredentials,

        /// <summary>
        /// Resource Owner Password Credentials — авторизация по паролю пользователя.
        /// Использует username, password, client_id, client_secret.
        /// Не рекомендуется для новых проектов (deprecated в OAuth 2.1).
        /// </summary>
        [RuName("Password")]
        Password,

        /// <summary>
        /// Authorization Code Flow — авторизация через код авторизации.
        /// Требует предварительного получения code через redirect.
        /// Наиболее безопасный способ для пользовательских приложений.
        /// </summary>
        [RuName("Authorization code")]
        AuthorizationCode
    }

    /// <summary>
    /// Режим парсинга JSON для активности «JSON: Парсинг».
    /// </summary>
    public enum JsonParseMode
    {
        /// <summary>
        /// Преобразовать JSON-объект в Dictionary&lt;string, object&gt;.
        /// Массивы становятся List&lt;object&gt;.
        /// Подходит для большинства сценариев работы с API.
        /// </summary>
        [RuName("В словарь")]
        ToDictionary,

        /// <summary>
        /// Преобразовать JSON-массив в List&lt;object&gt;.
        /// Каждый элемент массива парсится рекурсивно.
        /// </summary>
        [RuName("В список")]
        ToList,

        /// <summary>
        /// Только валидация JSON без разбора.
        /// Возвращает признак валидности и список ошибок.
        /// </summary>
        [RuName("Валидация")]
        Validate,

        /// <summary>
        /// Форматирование JSON (pretty print).
        /// Преобразует в читаемый вид с отступами.
        /// </summary>
        [RuName("Форматирование")]
        Format
    }

    /// <summary>
    /// Режим парсинга XML для активности «XML: Парсинг».
    /// </summary>
    public enum XmlParseMode
    {
        /// <summary>
        /// Преобразовать XML в структуру Dictionary.
        /// Атрибуты записываются с префиксом @.
        /// </summary>
        [RuName("В структуру")]
        ToStructure,

        /// <summary>
        /// Валидация XML — проверка корректности синтаксиса.
        /// </summary>
        [RuName("Валидация")]
        Validate,

        /// <summary>
        /// Форматирование XML с отступами (pretty print).
        /// </summary>
        [RuName("Форматирование")]
        Format
    }

    /// <summary>
    /// Режим запроса XML для активности «XML: XPath запрос».
    /// </summary>
    public enum XmlQueryMode
    {
        /// <summary>
        /// Вернуть одно значение (первое совпадение).
        /// </summary>
        [RuName("Одно значение")]
        SingleValue,

        /// <summary>
        /// Вернуть список всех совпадений.
        /// </summary>
        [RuName("Список")]
        List,

        /// <summary>
        /// Вернуть количество совпадений.
        /// </summary>
        [RuName("Количество")]
        Count
    }

    /// <summary>
    /// Режим запроса JSON для активности «JSON: Запрос».
    /// </summary>
    public enum JsonQueryMode
    {
        /// <summary>
        /// Вернуть одно значение (первое совпадение).
        /// </summary>
        [RuName("Одно значение")]
        SingleValue,

        /// <summary>
        /// Вернуть список всех совпадений.
        /// </summary>
        [RuName("Список")]
        List,

        /// <summary>
        /// Вернуть количество совпадений.
        /// </summary>
        [RuName("Количество")]
        Count
    }

    /// <summary>
    /// Направление конвертации — DataTable в JSON или JSON в DataTable.
    /// </summary>
    public enum JsonConvertDirection
    {
        /// <summary>DataTable → JSON-строка.</summary>
        [RuName("DataTable → JSON")]
        DataTableToJson,

        /// <summary>JSON-строка → DataTable.</summary>
        [RuName("JSON → DataTable")]
        JsonToDataTable
    }

    /// <summary>
    /// Формат JSON при конвертации DataTable → JSON.
    /// </summary>
    public enum JsonTableFormat
    {
        /// <summary>
        /// Массив объектов — каждая строка как объект с именованными полями.
        /// [{"Id":1,"Name":"Иван"},{"Id":2,"Name":"Пётр"}]
        /// Наиболее читаемый формат, совместим с большинством API.
        /// </summary>
        [RuName("Массив объектов")]
        ArrayOfObjects,

        /// <summary>
        /// Массив массивов — каждая строка как массив значений без имён.
        /// [[1,"Иван"],[2,"Пётр"]]
        /// Компактный формат, используется в Chart.js, Google Charts и т.п.
        /// </summary>
        [RuName("Массив массивов")]
        ArrayOfArrays,

        /// <summary>
        /// Объект с ключами "columns" и "rows".
        /// {"columns":["Id","Name"],"rows":[[1,"Иван"],[2,"Пётр"]]}
        /// Удобен для десериализации без потери информации о структуре.
        /// </summary>
        [RuName("Columns + rows")]
        WithHeaders
    }

    /// <summary>
    /// Как представлять NULL-значения в JSON.
    /// </summary>
    public enum JsonNullMode
    {
        /// <summary>NULL → null (JSON null). Стандартное поведение.</summary>
        [RuName("null (JSON null)")]
        JsonNull,

        /// <summary>NULL → "" (пустая строка). Удобно для систем не поддерживающих null.</summary>
        [RuName("Пустая строка")]
        EmptyString,

        /// <summary>NULL → "0" (ноль как строка). Для числовых полей.</summary>
        [RuName("Ноль (\"0\")")]
        Zero,

        /// <summary>Строки с NULL-значением пропускаются при сериализации.</summary>
        [RuName("Пропускать")]
        Skip
    }

    // =========================================================================
    // ПАГИНАЦИЯ (issue #32)
    // =========================================================================

    /// <summary>
    /// Режим пагинации для активности «HTTP: Пагинация».
    /// </summary>
    public enum HttpPaginationMode
    {
        /// <summary>
        /// Номер страницы: в шаблоне URL плейсхолдер {page} заменяется
        /// на номер (старт, шаг 1) до пустой страницы или MaxPages.
        /// </summary>
        [RuName("По номеру страницы ({page})")]
        PageNumber = 0,

        /// <summary>
        /// Курсор: из ответа извлекается следующий курсор по JSONPath
        /// и подставляется в шаблон {cursor}; остановка — курсор не найден.
        /// </summary>
        [RuName("По курсору ({cursor})")]
        Cursor = 1
    }
}
