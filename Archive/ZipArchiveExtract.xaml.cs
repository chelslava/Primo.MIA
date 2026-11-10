using Microsoft.Win32;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Логика взаимодействия для ZipArchiveExtract.xaml (issue #28).
    /// </summary>
    public partial class ZipArchiveExtract : UserControl
    {
        public ZipArchiveExtract()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка «...» у поля архива — OpenFileDialog существующего ZIP.
        /// Путь (в кавычках — формат Primo RPA) → Prop_ZipPath.
        /// </summary>
        private void BtnBrowseZip_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title           = "Выберите ZIP-архив",
                Filter          = "ZIP-архив (*.zip)|*.zip|Все файлы (*.*)|*.*",
                FilterIndex     = 1,
                CheckFileExists = true,
                Multiselect     = false
            };

            var currentPath = TxtZipPath.Text?.Trim('"', ' ');
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

            if (DataContext is ZipArchiveExtractBack backEnd)
                backEnd.Prop_ZipPath = $"\"{dialog.FileName}\"";
        }
    }
}
