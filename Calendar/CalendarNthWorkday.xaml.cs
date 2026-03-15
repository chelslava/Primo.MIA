// =============================================================================
// CalendarNthWorkday.xaml.cs — code-behind для активности «Календарь: Nth рабочий день».
// Связывает XAML-представление с моделью CalendarNthWorkdayBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>Code-behind для активности «Календарь: Nth рабочий день».</summary>
    public partial class CalendarNthWorkday : UserControl
    {
        /// <summary>Конструктор по умолчанию — инициализирует компоненты XAML.</summary>
        public CalendarNthWorkday()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public CalendarNthWorkday(CalendarNthWorkdayBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
