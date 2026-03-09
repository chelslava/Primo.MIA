using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class BrowserScreenshot : UserControl
    {
        public BrowserScreenshot()
        {
            InitializeComponent();
        }

        public BrowserScreenshot(BrowserScreenshotBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
