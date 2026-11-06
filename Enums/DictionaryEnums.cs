namespace Primo.MIA
{
    /// <summary>
    /// Стратегия при слиянии двух словарей с конфликтующими ключами
    /// </summary>
    public enum DictionaryMergeStrategy
    {
        /// <summary>
        /// Ключи первого словаря имеют приоритет.
        /// Ключи второго словаря не перезаписывают уже существующие.
        /// </summary>
        [RuName("Приоритет первого")]
        KeepFirst,

        /// <summary>
        /// Ключи второго словаря имеют приоритет.
        /// Перекрывают значения первого при совпадении ключей.
        /// </summary>
        [RuName("Приоритет второго")]
        KeepSecond,

        /// <summary>
        /// При конфликте ключей — выбросить исключение со списком дублей.
        /// Используется когда дублирование ключей является ошибкой процесса.
        /// </summary>
        [RuName("Ошибка при дублях")]
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
        [RuName("Создать пустой")]
        CreateEmpty,

        /// <summary>
        /// Создать словарь из двух List&lt;string&gt; одинаковой длины.
        /// Prop_KeysList[i] → ключ, Prop_ValuesList[i] → значение.
        /// При дублирующихся ключах побеждает последнее значение.
        /// </summary>
        [RuName("Из двух списков")]
        FromLists,

        /// <summary>
        /// Создать полную независимую копию словаря.
        /// Изменения в копии не затронут оригинал.
        /// </summary>
        [RuName("Клонировать")]
        Clone,

        /// <summary>
        /// Инвертировать словарь: значения становятся ключами, ключи — значениями.
        /// При дублирующихся значениях (будущих ключах):
        ///   Prop_ThrowOnDuplicates = true  → исключение
        ///   Prop_ThrowOnDuplicates = false → первое вхождение сохраняется
        /// </summary>
        [RuName("Инвертировать")]
        Invert
    }

    /// <summary>
    /// Где искать: в ключах, значениях или и там и там.
    /// </summary>
    public enum DictionaryFilterTarget
    {
        /// <summary>Применять условие к ключам словаря</summary>
        [RuName("Ключи")]
        Keys,

        /// <summary>Применять условие к значениям словаря</summary>
        [RuName("Значения")]
        Values,

        /// <summary>Оставить пару если условие выполняется хотя бы для ключа ИЛИ значения</summary>
        [RuName("Ключи и значения")]
        KeysAndValues
    }

    /// <summary>
    /// Как искать: подстрока, точное совпадение, regex или wildcard.
    /// </summary>
    public enum DictionaryFilterMethod
    {
        /// <summary>Содержит подстроку (аналог String.Contains)</summary>
        [RuName("Содержит")]
        Contains,

        /// <summary>Точное совпадение всей строки</summary>
        [RuName("Точное совпадение")]
        Exact,

        /// <summary>Регулярное выражение</summary>
        [RuName("Regex")]
        Regex,

        /// <summary>Wildcard-паттерн (* — любое кол-во символов, ? — один символ)</summary>
        [RuName("Wildcard")]
        Wildcard
    }
}
