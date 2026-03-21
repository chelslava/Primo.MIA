// =============================================================================
// HttpUpload.xaml.cs — code-behind для активности «HTTP: Загрузить файл».
// Связывает XAML-представление с моделью HttpUploadBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Code-behind для активности «HTTP: Загрузить файл».
    /// </summary>
    public partial class HttpUpload : UserControl
    {
        /// <summary>
        /// Конструктор по умолчанию — инициализирует компоненты XAML.
        /// </summary>
        public HttpUpload()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public HttpUpload(HttpUploadBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
