// =============================================================================
// TextExtractEntities.xaml.cs — code-behind для активности «Текст: Извлечь сущности».
// Связывает XAML-представление с моделью TextExtractEntitiesBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>Code-behind для активности «Текст: Извлечь сущности».</summary>
    public partial class TextExtractEntities : UserControl
    {
        /// <summary>Конструктор по умолчанию — инициализирует компоненты XAML.</summary>
        public TextExtractEntities()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public TextExtractEntities(TextExtractEntitiesBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
