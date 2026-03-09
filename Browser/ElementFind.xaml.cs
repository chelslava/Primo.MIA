using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementFind : UserControl
    {
        public ElementFind()
        {
            InitializeComponent();
        }

        public ElementFind(ElementFindBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
