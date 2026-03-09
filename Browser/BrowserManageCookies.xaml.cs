using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class BrowserManageCookies : UserControl
    {
        public BrowserManageCookies()
        {
            InitializeComponent();
        }

        public BrowserManageCookies(BrowserManageCookiesBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
