// =============================================================================
// EnumToVisibilityConverter.cs — универсальные конвертеры для UI.
//
// Содержит:
//   - EnumToVisibilityConverter: enum → Visibility через ConverterParameter
//   - BoolToVisibilityConverter: bool → Visibility
//   - InverseBoolConverter: инверсия bool
//   - MultiEnumToVisibilityConverter: проверка нескольких режимов
// =============================================================================

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Primo.MIA
{
    /// <summary>
    /// Универсальный конвертер enum в Visibility.
    /// Сравнивает строковое представление текущего значения с ConverterParameter.
    /// </summary>
    /// <example>
    /// Visibility="{Binding ReadMode, Converter={StaticResource EnumToVisConverter}, ConverterParameter=SingleValue}"
    /// </example>
    public class EnumToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Конвертирует enum-значение в Visibility.
        /// </summary>
        /// <param name="value">Текущее значение enum.</param>
        /// <param name="targetType">Целевой тип (Visibility).</param>
        /// <param name="parameter">Ожидаемое значение enum в виде строки.</param>
        /// <param name="culture">Культура.</param>
        /// <returns>Visible если значения совпадают, иначе Collapsed.</returns>
        public object Convert(object value, Type targetType, 
                              object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string currentMode = value.ToString();
            string targetMode = parameter.ToString();

            return string.Equals(currentMode, targetMode, 
                       StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>
        /// Обратная конвертация не поддерживается.
        /// </summary>
        public object ConvertBack(object value, Type targetType, 
                                  object parameter, CultureInfo culture)
            => throw new NotSupportedException(
                "EnumToVisibilityConverter: обратная конвертация не поддерживается");
    }

    /// <summary>
    /// Конвертер bool в Visibility.
    /// true → Visible, false → Collapsed
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Конвертирует bool в Visibility.
        /// </summary>
        /// <param name="value">Значение bool.</param>
        /// <param name="targetType">Целевой тип (Visibility).</param>
        /// <param name="parameter">Параметр (не используется).</param>
        /// <param name="culture">Культура.</param>
        /// <returns>Visible если true, иначе Collapsed.</returns>
        public object Convert(object value, Type targetType, 
                              object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Обратная конвертация не поддерживается.
        /// </summary>
        public object ConvertBack(object value, Type targetType, 
                                  object parameter, CultureInfo culture)
            => throw new NotSupportedException(
                "BoolToVisibilityConverter: обратная конвертация не поддерживается");
    }

    /// <summary>
    /// Инверсия bool. true → false, false → true
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        /// <summary>
        /// Инвертирует bool-значение.
        /// </summary>
        /// <param name="value">Исходное значение bool.</param>
        /// <param name="targetType">Целевой тип.</param>
        /// <param name="parameter">Параметр (не используется).</param>
        /// <param name="culture">Культура.</param>
        /// <returns>Инвертированное значение.</returns>
        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            // Для null или неожиданного типа возвращаем false (безопасное значение по умолчанию)
            return false;
        }

        /// <summary>
        /// Обратная конвертация не поддерживается.
        /// </summary>
        public object ConvertBack(object value, Type targetType, 
                                  object parameter, CultureInfo culture)
            => throw new NotSupportedException(
                "InverseBoolConverter: обратная конвертация не поддерживается");
    }

    /// <summary>
    /// Конвертер для проверки вхождения в список режимов.
    /// ConverterParameter = "Mode1|Mode2|Mode3" (разделитель | или ;)
    /// </summary>
    public class MultiEnumToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Проверяет вхождение текущего значения в список режимов.
        /// </summary>
        /// <param name="value">Текущее значение enum.</param>
        /// <param name="targetType">Целевой тип (Visibility).</param>
        /// <param name="parameter">Список режимов через | или ;.</param>
        /// <param name="culture">Культура.</param>
        /// <returns>Visible если значение входит в список, иначе Collapsed.</returns>
        public object Convert(object value, Type targetType, 
                              object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string currentMode = value.ToString();
            string paramStr = parameter.ToString();
            
            // Поддерживаем разделители | и ; (запятая не подходит для XAML Binding)
            char[] separators = new[] { '|', ';' };
            string[] targetModes = paramStr.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            foreach (string mode in targetModes)
            {
                if (string.Equals(currentMode, mode.Trim(), 
                        StringComparison.OrdinalIgnoreCase))
                    return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        /// <summary>
        /// Обратная конвертация не поддерживается.
        /// </summary>
        public object ConvertBack(object value, Type targetType, 
                                  object parameter, CultureInfo culture)
            => throw new NotSupportedException(
                "MultiEnumToVisibilityConverter: обратная конвертация не поддерживается");
    }
}
