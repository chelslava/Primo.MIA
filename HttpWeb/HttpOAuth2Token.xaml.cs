// =============================================================================
// HttpOAuth2Token.xaml.cs — code-behind для активности «HTTP: OAuth2 токен».
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Code-behind для активности «HTTP: OAuth2 токен».
    /// </summary>
    public partial class HttpOAuth2Token : UserControl
    {
        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public HttpOAuth2Token()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext.
        /// </summary>
        public HttpOAuth2Token(HttpOAuth2TokenBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
