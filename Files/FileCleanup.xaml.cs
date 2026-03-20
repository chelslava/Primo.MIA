// =============================================================================
// FileCleanup.xaml.cs — code-behind для активности «Файл: Очистка по времени».
// =============================================================================

using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>Code-behind для активности «Файл: Очистка по времени».</summary>
    public partial class FileCleanup : UserControl
    {
        public FileCleanup() { InitializeComponent(); }

        public FileCleanup(FileCleanupBack item) : this()
        {
            this.DataContext = item;
        }
    }
}
