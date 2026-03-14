// =============================================================================
// DataTableToHtml.xaml.cs — code-behind для активности "DataTable в HTML".
// Связывает XAML-представление с моделью DataTableToHtmlBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Code-behind для активности "DataTable в HTML".
    /// </summary>
    public partial class DataTableToHtml : UserControl
    {
        /// <summary>
        /// Конструктор по умолчанию — инициализирует компоненты XAML.
        /// </summary>
        public DataTableToHtml()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public DataTableToHtml(DataTableToHtmlBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
