// =============================================================================
// ElementFindUnified.xaml.cs — code-behind для объединённой активности поиска.
//
// Связывает XAML-представление с моделью ElementFindUnifiedBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Code-behind для объединённой активности «Найти элемент(ы)».
    /// </summary>
    public partial class ElementFind : UserControl
    {
        /// <summary>Конструктор по умолчанию — инициализирует компоненты XAML.</summary>
        public ElementFind()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public ElementFind(ElementFindBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
