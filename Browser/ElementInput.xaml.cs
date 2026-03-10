using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementInput : UserControl
    {
        public ElementInput()
        {
            InitializeComponent();
        }

        public ElementInput(ElementInputBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
