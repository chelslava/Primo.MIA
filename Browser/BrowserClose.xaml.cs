using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class BrowserClose : UserControl
    {
        public BrowserClose()
        {
            InitializeComponent();
        }

        public BrowserClose(BrowserCloseBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
