// =============================================================================
// DataTableFilter.xaml.cs — code-behind для активности "DataTable: Фильтрация".
// Связывает XAML-представление с моделью DataTableFilterBack
// и обрабатывает кнопки формы конструктора условий.
// =============================================================================

using System.Windows;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Code-behind для активности "DataTable: Фильтрация".
    /// </summary>
    public partial class DataTableFilter : UserControl
    {
        /// <summary>
        /// Конструктор по умолчанию — инициализирует компоненты XAML.
        /// </summary>
        public DataTableFilter()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public DataTableFilter(DataTableFilterBack item) : this()
        {
            this.DataContext = item;
        }

        /// <summary>Кнопка «+ Добавить условие».</summary>
        private void BtnAddCondition_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as DataTableFilterBack)?.AddConditionRow();
        }

        /// <summary>Кнопка «✕» удаления условия в строке.</summary>
        private void BtnRemoveCondition_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DataTableFilterBack back &&
                (sender as FrameworkElement)?.DataContext is DataTableConditionRow row)
            {
                back.RemoveConditionRow(row);
            }
        }
    }
}
