using Microsoft.Win32;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Логика взаимодействия для SftpDownload.xaml
    /// </summary>
    public partial class SftpDownload : UserControl
    {
        public SftpDownload()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обработчик кнопки «...» рядом с полем локального пути.
        /// Открывает OpenFileDialog выбора файла для сохранения и записывает
        /// путь (в кавычках — формат Primo RPA) в Prop_LocalPath через DataContext.
        /// </summary>
        private void BtnBrowseFile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title           = "Выберите файл для сохранения",
                Filter          = "Все файлы (*.*)|*.*",
                FilterIndex     = 1,
                CheckPathExists = true,
                Multiselect     = false
            };

            // Если в поле уже есть путь — открываем диалог в той же папке
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

            if (DataContext is SftpDownloadBack backEnd)
                backEnd.Prop_LocalPath = $"\"{dialog.FileName}\"";
        }
    }
}
