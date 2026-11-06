using System;

namespace Primo.MIA
{
    /// <summary>
    /// Русское отображаемое имя значения enum для UI активностей (issue #14).
    /// Комбобоксы режимов и заголовки показывают имя из атрибута; при его
    /// отсутствии — сырое имя члена enum (обратно-совместимо).
    /// <para>Пример:</para>
    /// <code>
    /// public enum JsonParseMode
    /// {
    ///     [RuName("Строка")]
    ///     Text,
    ///     [RuName("Файл")]
    ///     File,
    /// }
    /// </code>
    /// Сериализация не зависит от атрибута: в свойствах сохраняются имена
    /// членов enum, атрибут влияет только на отображение.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class RuNameAttribute : Attribute
    {
        /// <summary>Русское отображаемое имя.</summary>
        public string Name { get; }

        /// <summary>Создаёт атрибут с русским отображаемым именем значения.</summary>
        public RuNameAttribute(string name)
        {
            Name = name;
        }
    }
}
