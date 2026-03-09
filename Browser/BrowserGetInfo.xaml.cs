using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class BrowserGetInfo : UserControl
    {
        public BrowserGetInfo()
        {
            InitializeComponent();
        }

        public BrowserGetInfo(BrowserGetInfoBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
