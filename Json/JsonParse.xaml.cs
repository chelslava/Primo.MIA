// =============================================================================
// JsonParse.xaml.cs — code-behind для активности «JSON: Парсинг».
// Связывает XAML-представление с моделью JsonParseBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Code-behind для активности «JSON: Парсинг».
    /// </summary>
    public partial class JsonParse : UserControl
    {
        /// <summary>
        /// Конструктор по умолчанию — инициализирует компоненты XAML.
        /// </summary>
        public JsonParse()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public JsonParse(JsonParseBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
