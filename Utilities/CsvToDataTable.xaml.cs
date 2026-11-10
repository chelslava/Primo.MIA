using Microsoft.Win32;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Логика взаимодействия для CsvToDataTable.xaml (issue #29).
    /// </summary>
    public partial class CsvToDataTable : UserControl
    {
        public CsvToDataTable()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка «...» у пути к CSV-файлу (режим «Файл»).
        /// Путь (в кавычках — формат Primo RPA) → Prop_FilePath.
        /// </summary>
        private void BtnBrowseCsv_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title           = "Выберите CSV-файл",
                Filter          = "CSV (*.csv)|*.csv|Все файлы (*.*)|*.*",
                FilterIndex     = 1,
                CheckFileExists = true,
                Multiselect     = false
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

            if (DataContext is CsvToDataTableBack backEnd)
                backEnd.Prop_FilePath = $"\"{dialog.FileName}\"";
        }
    }
}
