// =============================================================================
// Webhook.xaml.cs — code-behind для активности «Webhook: Отправить».
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>Code-behind для активности «Webhook: Отправить».</summary>
    public partial class Webhook : UserControl
    {
        public Webhook() { InitializeComponent(); }

        public Webhook(WebhookBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
