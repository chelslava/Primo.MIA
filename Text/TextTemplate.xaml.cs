// =============================================================================
// TextTemplate.xaml.cs — code-behind для активности «Текст: Шаблонизатор».
// Связывает XAML-представление с моделью TextTemplateBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>Code-behind для активности «Текст: Шаблонизатор».</summary>
    public partial class TextTemplate : UserControl
    {
        /// <summary>Конструктор по умолчанию — инициализирует компоненты XAML.</summary>
        public TextTemplate()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public TextTemplate(TextTemplateBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
