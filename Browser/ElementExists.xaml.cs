using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementExists : UserControl
    {
        public ElementExists()
        {
            InitializeComponent();
        }

        public ElementExists(ElementExistsBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
