// =============================================================================
// TextParse.xaml.cs — code-behind для активности «Текст: Разбор по шаблону».
// Связывает XAML-представление с моделью TextParseBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>Code-behind для активности «Текст: Разбор по шаблону».</summary>
    public partial class TextParse : UserControl
    {
        /// <summary>Конструктор по умолчанию — инициализирует компоненты XAML.</summary>
        public TextParse()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public TextParse(TextParseBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
