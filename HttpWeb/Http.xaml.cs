// =============================================================================
// Http.xaml.cs — code-behind для активности «HTTP Запрос».
// Связывает XAML-представление с моделью HttpBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>Code-behind для активности «HTTP Запрос».</summary>
    public partial class Http : UserControl
    {
        /// <summary>Конструктор по умолчанию — инициализирует компоненты XAML.</summary>
        public Http()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public Http(HttpBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
