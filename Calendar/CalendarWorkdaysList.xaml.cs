// =============================================================================
// CalendarWorkdaysList.xaml.cs — code-behind для активности «Календарь: Рабочие дни списком».
// Связывает XAML-представление с моделью CalendarWorkdaysListBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>Code-behind для активности «Календарь: Рабочие дни списком».</summary>
    public partial class CalendarWorkdaysList : UserControl
    {
        /// <summary>Конструктор по умолчанию — инициализирует компоненты XAML.</summary>
        public CalendarWorkdaysList()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public CalendarWorkdaysList(CalendarWorkdaysListBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
