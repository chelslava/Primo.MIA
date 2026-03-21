// =============================================================================
// WebhookListen.xaml.cs — code-behind для активности «Webhook: Получить».
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>Code-behind для активности «Webhook: Получить».</summary>
    public partial class WebhookListen : UserControl
    {
        public WebhookListen() { InitializeComponent(); }

        public WebhookListen(WebhookListenBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
