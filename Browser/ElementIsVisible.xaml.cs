using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementIsVisible : UserControl
    {
        public ElementIsVisible()
        {
            InitializeComponent();
        }

        public ElementIsVisible(ElementIsVisibleBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
