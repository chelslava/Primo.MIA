using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementFindAll : UserControl
    {
        public ElementFindAll()
        {
            InitializeComponent();
        }

        public ElementFindAll(ElementFindAllBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
