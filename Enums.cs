using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primo.MIA
{
    // =========================================================================
    // ПЕРЕЧИСЛЕНИЯ
    // =========================================================================

    /// <summary>
    /// Режим чтения TOML-конфигурации
    /// </summary>
    public enum TomlReadMode
    {
        /// <summary>
        /// Прочитать одно значение по ключу.
        /// Поддерживает вложенность через точку: "section.subsection.key"
        /// </summary>
        SingleValue,

        /// <summary>
        /// Прочитать все ключи указанной секции в Dictionary&lt;string, string&gt;.
        /// Поддерживает вложенные секции: "app.database"
        /// </summary>
        SectionToDictionary,

        /// <summary>
        /// Прочитать весь файл в плоский словарь.
        /// Ключи вложенных секций объединяются через точку: "server.host"
        /// </summary>
        FullFileToDictionary,

        /// <summary>
        /// Чтение конфига с профилями окружений.
        /// Мёржит секцию [default] с секцией выбранного профиля ([production], [staging] и т.д.)
        /// по выбранной стратегии слияния.
        /// </summary>
        ReadProfile
    }

    /// <summary>
    /// Стратегия слияния профиля с секцией default
    /// </summary>
    public enum ProfileMergeStrategy
    {
        /// <summary>
        /// Стандартная стратегия: default как база, профиль перекрывает его ключи.
        /// Ключи из default, которых нет в профиле, сохраняются.
        /// Пример: default.timeout=30, production.timeout=60 → итог: timeout=60
        ///         default.retry=3 (нет в production) → итог: retry=3
        /// </summary>
        DefaultThenProfile,

        /// <summary>
        /// Только секция профиля, без слияния с default.
        /// Используется когда профили полностью самодостаточны.
        /// </summary>
        ProfileOnly,

        /// <summary>
        /// Обратный порядок: профиль как база, default добавляет только недостающие ключи.
        /// Ключи профиля никогда не перекрываются default.
        /// </summary>
        ProfileThenDefault
    }

    /// <summary>
    /// Стратегия при слиянии двух словарей с конфликтующими ключами
    /// </summary>
    public enum DictionaryMergeStrategy
    {
        /// <summary>
        /// Ключи первого словаря имеют приоритет.
        /// Ключи второго словаря не перезаписывают уже существующие.
        /// </summary>
        KeepFirst,

        /// <summary>
        /// Ключи второго словаря имеют приоритет.
        /// Перекрывают значения первого при совпадении ключей.
        /// </summary>
        KeepSecond,

        /// <summary>
        /// При конфликте ключей — выбросить исключение со списком дублей.
        /// Используется когда дублирование ключей является ошибкой процесса.
        /// </summary>
        ThrowOnDuplicate
    }

    /// <summary>
    /// Режим создания нового словаря
    /// </summary>
    public enum DictionaryCreateMode
    {
        /// <summary>
        /// Создать новый пустой Dictionary&lt;string, string&gt;.
        /// Не требует входного словаря.
        /// </summary>
        CreateEmpty,

        /// <summary>
        /// Создать словарь из двух List&lt;string&gt; одинаковой длины.
        /// Prop_KeysList[i] → ключ, Prop_ValuesList[i] → значение.
        /// При дублирующихся ключах побеждает последнее значение.
        /// </summary>
        FromLists,

        /// <summary>
        /// Создать полную независимую копию словаря.
        /// Изменения в копии не затронут оригинал.
        /// </summary>
        Clone,

        /// <summary>
        /// Инвертировать словарь: значения становятся ключами, ключи — значениями.
        /// При дублирующихся значениях (будущих ключах):
        ///   Prop_ThrowOnDuplicates = true  → исключение
        ///   Prop_ThrowOnDuplicates = false → первое вхождение сохраняется
        /// </summary>
        Invert
    }

    /// <summary>
    /// Режим работы активности
    /// </summary>
    public enum WaitFileMode
    {
        /// <summary>
        /// Ожидать появления нового файла (которого не было при запуске)
        /// </summary>
        WaitForNewFile,

        /// <summary>
        /// Ожидать файл независимо от того, был он или нет
        /// </summary>
        WaitForAnyFile
    }

    /// <summary>
    /// Тип фильтрации файлов
    /// </summary>
    public enum FileFilterType
    {
        /// <summary>
        /// Точное совпадение имени файла
        /// </summary>
        Exact,

        /// <summary>
        /// Wildcard фильтрация (*, ?)
        /// </summary>
        Wildcard,

        /// <summary>
        /// Regex фильтрация
        /// </summary>
        Regex
    }

    /// <summary>
    /// Тип поиска файлов
    /// </summary>
    public enum SearchType
    {
        /// <summary>
        /// Поиск только файлов
        /// </summary>
        FilesOnly,

        /// <summary>
        /// Поиск только папок
        /// </summary>
        FoldersOnly,

        /// <summary>
        /// Поиск файлов и папок
        /// </summary>
        FilesAndFolders
    }

    /// <summary>
    /// Тип фильтрации при поиске
    /// </summary>
    public enum SearchFilterType
    {
        /// <summary>
        /// Wildcard фильтрация (*, ?)
        /// </summary>
        Wildcard,

        /// <summary>
        /// Regex фильтрация
        /// </summary>
        Regex
    }

    /// <summary>
    /// Уровень логирования
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Отладочная информация
        /// </summary>
        Debug,

        /// <summary>
        /// Информационное сообщение
        /// </summary>
        Info,

        /// <summary>
        /// Предупреждение
        /// </summary>
        Warning,

        /// <summary>
        /// Ошибка
        /// </summary>
        Error,

        /// <summary>
        /// Критическая ошибка
        /// </summary>
        Critical
    }
    /// <summary>
    /// Режим вывода логов
    /// </summary>
    public enum LogOutputMode
    {
        /// <summary>
        /// Только в файл
        /// </summary>
        FileOnly,

        /// <summary>
        /// Только в консоль
        /// </summary>
        ConsoleOnly,

        /// <summary>
        /// В файл и консоль одновременно
        /// </summary>
        FileAndConsole
    }

    /// <summary>
    /// Стратегия ротации файлов логов
    /// </summary>
    public enum LogRotationStrategy
    {
        /// <summary>
        /// Без ротации - весь лог в один файл
        /// </summary>
        None,

        /// <summary>
        /// Новый файл каждый день
        /// </summary>
        Daily,

        /// <summary>
        /// Новый файл каждый час
        /// </summary>
        Hourly,

        /// <summary>
        /// Новый файл при превышении размера
        /// </summary>
        BySize
    }

    /// <summary>
    /// Шаблон имени файла лога
    /// </summary>
    public enum LogFileNameTemplate
    {
        /// <summary>
        /// Фиксированное имя (app.log)
        /// </summary>
        Fixed,

        /// <summary>
        /// С датой (app_2025-02-15.log)
        /// </summary>
        WithDate,

        /// <summary>
        /// С датой и временем (app_2025-02-15_14-30.log)
        /// </summary>
        WithDateTime,

        /// <summary>
        /// С timestamp (app_20250215143045.log)
        /// </summary>
        WithTimestamp,

        /// <summary>
        /// Пользовательский формат
        /// </summary>
        Custom
    }

    /// <summary>
    /// Где искать: в ключах, значениях или и там и там.
    /// </summary>
    public enum DictionaryFilterTarget
    {
        /// <summary>Применять условие к ключам словаря</summary>
        Keys,

        /// <summary>Применять условие к значениям словаря</summary>
        Values,

        /// <summary>Оставить пару если условие выполняется хотя бы для ключа ИЛИ значения</summary>
        KeysAndValues
    }

    /// <summary>
    /// Как искать: подстрока, точное совпадение, regex или wildcard.
    /// </summary>
    public enum DictionaryFilterMethod
    {
        /// <summary>Содержит подстроку (аналог String.Contains)</summary>
        Contains,

        /// <summary>Точное совпадение всей строки</summary>
        Exact,

        /// <summary>Регулярное выражение</summary>
        Regex,

        /// <summary>Wildcard-паттерн (* — любое кол-во символов, ? — один символ)</summary>
        Wildcard
    }
}
