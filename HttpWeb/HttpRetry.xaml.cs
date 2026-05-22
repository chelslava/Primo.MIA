// =============================================================================
// HttpRetry.xaml.cs — code-behind для активности «HTTP: Запрос с повторами».
// Связывает XAML-представление с моделью HttpRetryBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Code-behind для активности «HTTP: Запрос с повторами».
    /// </summary>
    public partial class HttpRetry : UserControl
    {
        /// <summary>
        /// Конструктор по умолчанию — инициализирует компоненты XAML.
        /// </summary>
        public HttpRetry()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public HttpRetry(HttpRetryBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
