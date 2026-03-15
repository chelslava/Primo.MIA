// =============================================================================
// CalendarNextDayOfWeek.xaml.cs — code-behind для активности
// «Календарь: Следующий день недели».
// Связывает XAML-представление с моделью CalendarNextDayOfWeekBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>Code-behind для активности «Календарь: Следующий день недели».</summary>
    public partial class CalendarNextDayOfWeek : UserControl
    {
        /// <summary>Конструктор по умолчанию — инициализирует компоненты XAML.</summary>
        public CalendarNextDayOfWeek()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public CalendarNextDayOfWeek(CalendarNextDayOfWeekBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
