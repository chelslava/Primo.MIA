using Microsoft.Win32;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Логика взаимодействия для DataTableToCsv.xaml (issue #29).
    /// </summary>
    public partial class DataTableToCsv : UserControl
    {
        public DataTableToCsv()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка «...» у пути к CSV-файлу (режим «Файл»).
        /// SaveFileDialog: путь (в кавычках — формат Primo RPA) → Prop_FilePath.
        /// </summary>
        private void BtnBrowseCsv_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title           = "Укажите CSV-файл",
                Filter          = "CSV (*.csv)|*.csv|Все файлы (*.*)|*.*",
                FilterIndex     = 1,
                DefaultExt      = ".csv",
                OverwritePrompt = true
            };

            var currentPath = TxtFilePath.Text?.Trim('"', ' ');
            if (!string.IsNullOrEmpty(currentPath))
            {
                try
                {
                    dialog.InitialDirectory = System.IO.Path.GetDirectoryName(currentPath) ?? string.Empty;
                }
                catch
                {
                    // Некорректный путь — просто не задаём InitialDirectory
                }
            }

            if (dialog.ShowDialog() != true) return;

            if (DataContext is DataTableToCsvBack backEnd)
                backEnd.Prop_FilePath = $"\"{dialog.FileName}\"";
        }
    }
}
