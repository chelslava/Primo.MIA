// =============================================================================
// TextTranslit.xaml.cs — code-behind для активности «Текст: Транслитерация».
// Связывает XAML-представление с моделью TextTranslitBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>Code-behind для активности «Текст: Транслитерация».</summary>
    public partial class TextTranslit : UserControl
    {
        /// <summary>Конструктор по умолчанию — инициализирует компоненты XAML.</summary>
        public TextTranslit()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public TextTranslit(TextTranslitBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
