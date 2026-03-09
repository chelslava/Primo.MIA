using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class AlertHandle : UserControl
    {
        public AlertHandle()
        {
            InitializeComponent();
        }

        public AlertHandle(AlertHandleBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
