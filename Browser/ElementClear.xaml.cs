using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementClear : UserControl
    {
        public ElementClear()
        {
            InitializeComponent();
        }

        public ElementClear(ElementClearBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
