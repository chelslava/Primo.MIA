using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class ElementWaitAndCheck : UserControl
    {
        public ElementWaitAndCheck()
        {
            InitializeComponent();
        }

        public ElementWaitAndCheck(ElementWaitAndCheckBack item) : this()
        {
            this.DataContext = item;
        }
    }
}