using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class BrowserExecuteJavaScript : UserControl
    {
        public BrowserExecuteJavaScript()
        {
            InitializeComponent();
        }

        public BrowserExecuteJavaScript(BrowserExecuteJavaScriptBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
