// =============================================================================
// CalendarLoad.xaml.cs — code-behind для активности «Календарь: Загрузить».
// Связывает XAML-представление с моделью CalendarLoadBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>Code-behind для активности «Календарь: Загрузить».</summary>
    public partial class CalendarLoad : UserControl
    {
        /// <summary>Конструктор по умолчанию — инициализирует компоненты XAML.</summary>
        public CalendarLoad()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public CalendarLoad(CalendarLoadBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
