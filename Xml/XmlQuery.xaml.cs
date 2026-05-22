// =============================================================================
// XmlQuery.xaml.cs — code-behind для активности «XML: XPath запрос».
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Code-behind для активности «XML: XPath запрос».
    /// </summary>
    public partial class XmlQuery : UserControl
    {
        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public XmlQuery()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext.
        /// </summary>
        public XmlQuery(XmlQueryBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
