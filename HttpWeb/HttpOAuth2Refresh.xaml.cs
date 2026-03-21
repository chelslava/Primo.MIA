// =============================================================================
// HttpOAuth2Refresh.xaml.cs — code-behind для активности «HTTP: OAuth2 обновление токена».
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Code-behind для активности «HTTP: OAuth2 обновление токена».
    /// </summary>
    public partial class HttpOAuth2Refresh : UserControl
    {
        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public HttpOAuth2Refresh()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext.
        /// </summary>
        public HttpOAuth2Refresh(HttpOAuth2RefreshBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
