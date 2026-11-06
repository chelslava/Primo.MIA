using System;
using System.Globalization;
using System.Windows.Data;

namespace Primo.MIA
{
    /// <summary>
    /// Конвертер значения enum в отображаемое имя: берёт <see cref="RuNameAttribute"/>,
    /// при отсутствии атрибута — имя члена (issue #14). Используется в заголовках
    /// активностей: «Поиск элемента [Одиночный]» вместо «[Single]».
    /// Экземпляр объявлен в Common/Styles.xaml с ключом EnumDisplayNameConverter.
    /// </summary>
    public class EnumDisplayNameConverter : IValueConverter
    {
        /// <summary>Возвращает русское имя значения enum (RuName) или имя члена.</summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            var type = value.GetType();
            if (!type.IsEnum)
                return value.ToString();

            var field = type.GetField(value.ToString());
            var ruName = field?.GetCustomAttributes(typeof(RuNameAttribute), false);
            return ruName != null && ruName.Length > 0
                ? ((RuNameAttribute)ruName[0]).Name
                : value.ToString();
        }

        /// <summary>Не поддерживается (конвертер односторонний).</summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException("EnumDisplayNameConverter — односторонний конвертер");
    }
}
