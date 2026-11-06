using System.Globalization;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Правило валидации числовых полей (int/double, InvariantCulture).
    /// Пустая строка считается допустимой — пустота означает «использовать
    /// значение по умолчанию» (issue #15). Применяется в XAML активностей
    /// к числовым полям (таймауты, размер пакета/страницы и т.п.) вместе
    /// со стилем ValidatingFieldBox из Common/Styles.xaml:
    /// <code>
    /// &lt;TextBox Style="{StaticResource ValidatingFieldBox}"&gt;
    ///     &lt;TextBox.Text&gt;
    ///         &lt;Binding Path="Prop_TimeoutSeconds" UpdateSourceTrigger="PropertyChanged"&gt;
    ///             &lt;Binding.ValidationRules&gt;
    ///                 &lt;local:NumericValidationRule/&gt;
    ///             &lt;/Binding.ValidationRules&gt;
    ///         &lt;/Binding&gt;
    ///     &lt;/TextBox.Text&gt;
    /// &lt;/TextBox&gt;
    /// </code>
    /// </summary>
    public class NumericValidationRule : ValidationRule
    {
        /// <summary>
        /// Разрешить отрицательные значения (например, индекс -1 = последний
        /// элемент списка Redis). По умолчанию выключено: таймауты, размеры
        /// страниц и количества отрицательными не бывают.
        /// </summary>
        public bool AllowNegative { get; set; }

        /// <summary>Проверяет, что значение — целое или дробное число (InvariantCulture).</summary>
        public override ValidationResult Validate(object value, CultureInfo culture)
        {
            var text = (value as string)?.Trim();

            // Пустое поле валидно: отсутствие значения означает дефолт активности
            if (string.IsNullOrEmpty(text))
                return ValidationResult.ValidResult;

            if (!double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                return new ValidationResult(false, "Значение должно быть числом");

            if (!AllowNegative && number < 0)
                return new ValidationResult(false, "Значение должно быть неотрицательным числом");

            return ValidationResult.ValidResult;
        }
    }
}
