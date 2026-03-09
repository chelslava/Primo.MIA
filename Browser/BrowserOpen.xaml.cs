using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class BrowserOpen : UserControl
    {
        public BrowserOpen()
        {
            InitializeComponent();
        }

        public BrowserOpen(BrowserOpenBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
