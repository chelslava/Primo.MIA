using Microsoft.Win32;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Логика взаимодействия для FtpsDownload.xaml (issue #31).
    /// </summary>
    public partial class FtpsDownload : UserControl
    {
        public FtpsDownload()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка «...» у локального пути: выбор файла (OpenFileDialog).
        /// Путь (в кавычках — формат Primo RPA) → Prop_LocalPath.
        /// </summary>
        private void BtnBrowseFile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title           = "Выберите файл",
                Filter          = "Все файлы (*.*)|*.*",
                FilterIndex     = 1,
                Multiselect     = false
            };

            var currentPath = TxtLocalPath.Text?.Trim('"', ' ');
            if (!string.IsNullOrEmpty(currentPath))
            {
                try
                {
                    dialog.InitialDirectory = System.IO.Directory.Exists(currentPath)
                        ? currentPath
                        : System.IO.Path.GetDirectoryName(currentPath) ?? string.Empty;
                }
                catch
                {
                    // Некорректный путь — просто не задаём InitialDirectory
                }
            }

            if (dialog.ShowDialog() != true) return;

            if (DataContext is FtpsDownloadBack backEnd)
                backEnd.Prop_LocalPath = '"' + dialog.FileName + '"';
        }
    }
}