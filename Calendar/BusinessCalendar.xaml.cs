// =============================================================================
// BusinessCalendar.xaml.cs — code-behind для активности «Календарь: Операции».
// Связывает XAML-представление с моделью BusinessCalendarBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>Code-behind для активности «Календарь: Операции».</summary>
    public partial class BusinessCalendar : UserControl
    {
        /// <summary>Конструктор по умолчанию — инициализирует компоненты XAML.</summary>
        public BusinessCalendar()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public BusinessCalendar(BusinessCalendarBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
