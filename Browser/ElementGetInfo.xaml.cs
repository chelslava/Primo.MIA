using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementGetInfo : UserControl
    {
        public ElementGetInfo()
        {
            InitializeComponent();
        }

        public ElementGetInfo(ElementGetInfoBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
