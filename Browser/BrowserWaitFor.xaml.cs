using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class BrowserWaitFor : UserControl
    {
        public BrowserWaitFor()
        {
            InitializeComponent();
        }

        public BrowserWaitFor(BrowserWaitForBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
