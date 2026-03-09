using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementClick : UserControl
    {
        public ElementClick()
        {
            InitializeComponent();
        }

        public ElementClick(ElementClickBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
