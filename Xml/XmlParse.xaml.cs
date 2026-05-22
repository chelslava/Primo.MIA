// =============================================================================
// XmlParse.xaml.cs — code-behind для активности «XML: Парсинг».
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Code-behind для активности «XML: Парсинг».
    /// </summary>
    public partial class XmlParse : UserControl
    {
        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public XmlParse()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с привязкой DataContext.
        /// </summary>
        public XmlParse(XmlParseBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
