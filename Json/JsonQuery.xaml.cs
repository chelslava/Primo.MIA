// =============================================================================
// JsonQuery.xaml.cs — code-behind для активности «JSON: Запрос».
// Связывает XAML-представление с моделью JsonQueryBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Code-behind для активности «JSON: Запрос».
    /// </summary>
    public partial class JsonQuery : UserControl
    {
        /// <summary>
        /// Конструктор по умолчанию — инициализирует компоненты XAML.
        /// </summary>
        public JsonQuery()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public JsonQuery(JsonQueryBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
