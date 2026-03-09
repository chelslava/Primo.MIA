using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementScrollTo : UserControl
    {
        public ElementScrollTo()
        {
            InitializeComponent();
        }

        public ElementScrollTo(ElementScrollToBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
