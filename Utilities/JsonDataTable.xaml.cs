// =============================================================================
// JsonDataTable.xaml.cs — code-behind для активности «JSON: DataTable конвертер».
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>Code-behind для активности «JSON: DataTable конвертер».</summary>
    public partial class JsonDataTable : UserControl
    {
        public JsonDataTable() { InitializeComponent(); }

        public JsonDataTable(JsonDataTableBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
