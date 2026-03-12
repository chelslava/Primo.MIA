using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementSearch : UserControl
    {
        public ElementSearch()
        {
            InitializeComponent();
        }

        public ElementSearch(ElementSearchBack item) : this()
        {
            this.DataContext = item;
        }
    }
}