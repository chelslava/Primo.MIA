// =============================================================================
// ElementClickUnified.xaml.cs — code-behind для объединённой активности клика.
//
// Связывает XAML-представление с моделью ElementClickUnifiedBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Code-behind для объединённой активности «Клик по элементу».
    /// </summary>
    public partial class ElementClick : UserControl
    {
        /// <summary>Конструктор по умолчанию — инициализирует компоненты XAML.</summary>
        public ElementClick()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public ElementClick(ElementClickBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
