using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementTypeText : UserControl
    {
        public ElementTypeText()
        {
            InitializeComponent();
        }

        public ElementTypeText(ElementTypeTextBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
