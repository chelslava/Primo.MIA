// =============================================================================
// HttpBatch.xaml.cs — code-behind для активности «HTTP: Пакетные запросы».
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Code-behind для активности «HTTP: Пакетные запросы».
    /// </summary>
    public partial class HttpBatch : UserControl
    {
        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public HttpBatch()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext.
        /// </summary>
        public HttpBatch(HttpBatchBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
