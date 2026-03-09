using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementGetProperty : UserControl
    {
        public ElementGetProperty()
        {
            InitializeComponent();
        }

        public ElementGetProperty(ElementGetPropertyBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
