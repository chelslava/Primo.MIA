// =============================================================================
// ActivityPanel.cs — канонический каркас активности (issue #41).
//
// До него каждый из ~164 XAML повторял вручную: Grid с фоном
// ScrollBarBrushKey, две колонки (иконка | контент), Image 32×32 и
// StackPanel с Margin="5,0,5,5". Теперь:
//
//     <local:ActivityPanel IconSource="pack://application:,,,/Primo.MIA;component/images/excel.png">
//         <TextBlock .../>  <!-- поля активности -->
//     </local:ActivityPanel>
//
// XAML-содержимое ([ContentProperty]) попадает в правую StackPanel.
// Контрол строит визуальное дерево в конструкторе (без XAML-файла),
// поэтому не требует записи в csproj и работает в дизайнере Studio.
// =============================================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace Primo.MIA
{
    /// <summary>
    /// Каркас активности: иконка 32×32 слева, вертикальная панель полей справа.
    /// </summary>
    [ContentProperty(nameof(Items))]
    public class ActivityPanel : UserControl
    {
        private readonly StackPanel _contentHost;

        /// <summary>XAML-содержимое активности (поля, заголовки, чекбоксы).</summary>
        public UIElementCollection Items => _contentHost.Children;

        /// <summary>Путь к иконке активности (pack-URI, 32×32).</summary>
        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(
                nameof(IconSource), typeof(ImageSource), typeof(ActivityPanel),
                new PropertyMetadata(null));

        public ImageSource IconSource
        {
            get => (ImageSource)GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        public ActivityPanel()
        {
            var layout = new Grid();
            layout.SetResourceReference(
                Panel.BackgroundProperty, SystemColors.ScrollBarBrushKey);

            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var icon = new Image
            {
                Width = 32,
                Height = 32,
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center
            };
            icon.SetBinding(Image.SourceProperty,
                new Binding(nameof(IconSource)) { Source = this });
            Grid.SetColumn(icon, 0);

            _contentHost = new StackPanel { Margin = new Thickness(5, 0, 5, 5) };
            Grid.SetColumn(_contentHost, 1);

            layout.Children.Add(icon);
            layout.Children.Add(_contentHost);
            Content = layout;
        }
    }
}
