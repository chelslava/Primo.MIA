// =============================================================================
// HttpDownload.xaml.cs — code-behind для активности «HTTP: Скачать файл».
// Связывает XAML-представление с моделью HttpDownloadBack.
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Code-behind для активности «HTTP: Скачать файл».
    /// </summary>
    public partial class HttpDownload : UserControl
    {
        /// <summary>
        /// Конструктор по умолчанию — инициализирует компоненты XAML.
        /// </summary>
        public HttpDownload()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext к модели активности.
        /// </summary>
        /// <param name="item">Экземпляр Back-класса активности.</param>
        public HttpDownload(HttpDownloadBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
