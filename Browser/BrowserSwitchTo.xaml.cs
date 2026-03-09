using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class BrowserSwitchTo : UserControl
    {
        public BrowserSwitchTo()
        {
            InitializeComponent();
        }

        public BrowserSwitchTo(BrowserSwitchToBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
