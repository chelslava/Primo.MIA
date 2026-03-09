using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementHover : UserControl
    {
        public ElementHover()
        {
            InitializeComponent();
        }

        public ElementHover(ElementHoverBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
