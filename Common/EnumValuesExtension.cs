using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Markup;

namespace Primo.MIA
{
    /// <summary>
    /// Элемент списка «значение enum + русское имя» для комбобоксов режимов (issue #14).
    /// Key — значение enum (привязывается через SelectedValuePath="Key"),
    /// Display — имя из <see cref="RuNameAttribute"/> (DisplayMemberPath="Display").
    /// </summary>
    public class EnumDisplayItem
    {
        /// <summary>Значение enum (боксированное).</summary>
        public object Key { get; }

        /// <summary>Русское отображаемое имя.</summary>
        public string Display { get; }

        /// <summary>Создаёт элемент списка.</summary>
        public EnumDisplayItem(object key, string display)
        {
            Key = key;
            Display = display;
        }
    }

    /// <summary>
    /// Markup-расширение: список значений enum с русскими именами для ComboBox (issue #14).
    /// Заменяет связку ObjectDataProvider + Enum.GetValues, добавляя отображаемые имена.
    /// <para>Использование:</para>
    /// <code>
    /// &lt;ComboBox ItemsSource="{local:EnumValues local:JsonParseMode}"
    ///           SelectedValue="{Binding Prop_Mode}"
    ///           SelectedValuePath="Key" DisplayMemberPath="Display"/&gt;
    /// </code>
    /// Порядок элементов — порядок объявления членов enum. Значения без
    /// [RuName] отображаются сырым именем члена (обратно-совместимо).
    /// </summary>
    [MarkupExtensionReturnType(typeof(List<EnumDisplayItem>))]
    public class EnumValuesExtension : MarkupExtension
    {
        private readonly Type _enumType;

        /// <summary>Создаёт расширение для перечислимого типа.</summary>
        public EnumValuesExtension(Type enumType)
        {
            _enumType = enumType ?? throw new ArgumentNullException(nameof(enumType));
        }

        /// <summary>Возвращает список значений enum с русскими именами.</summary>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (!_enumType.IsEnum)
                throw new ArgumentException(
                    $"Тип {_enumType.Name} не является перечислением (enum)", nameof(_enumType));

            var items = new List<EnumDisplayItem>();
            foreach (var field in _enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var ruName = field.GetCustomAttributes(typeof(RuNameAttribute), false);
                var display = ruName != null && ruName.Length > 0
                    ? ((RuNameAttribute)ruName[0]).Name
                    : field.Name;
                items.Add(new EnumDisplayItem(field.GetValue(null), display));
            }

            return items;
        }
    }
}
