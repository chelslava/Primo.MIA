using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementSubmit : UserControl
    {
        public ElementSubmit()
        {
            InitializeComponent();
        }

        public ElementSubmit(ElementSubmitBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
