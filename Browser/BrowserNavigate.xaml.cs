using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class BrowserNavigate : UserControl
    {
        public BrowserNavigate()
        {
            InitializeComponent();
        }

        public BrowserNavigate(BrowserNavigateBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
