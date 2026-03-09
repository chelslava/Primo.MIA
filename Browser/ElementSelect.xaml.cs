using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementSelect : UserControl
    {
        public ElementSelect()
        {
            InitializeComponent();
        }

        public ElementSelect(ElementSelectBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
