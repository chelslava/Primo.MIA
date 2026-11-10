using Microsoft.Win32;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Логика взаимодействия для ZipArchiveCreate.xaml (issue #28).
    /// </summary>
    public partial class ZipArchiveCreate : UserControl
    {
        public ZipArchiveCreate()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обработчик кнопки «...» рядом с полем архива.
        /// SaveFileDialog: создаём новый архив, путь (в кавычках — формат
        /// Primo RPA) записывается в Prop_DestinationZip через DataContext.
        /// </summary>
        private void BtnBrowseZip_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title     = "Укажите ZIP-архив",
                Filter    = "ZIP-архив (*.zip)|*.zip|Все файлы (*.*)|*.*",
                FilterIndex = 1,
                DefaultExt = ".zip",
                OverwritePrompt = true
            };

            // Если в поле уже есть путь — стартуем в его папке
            var currentPath = TxtDestinationZip.Text?.Trim('"', ' ');
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

            if (DataContext is ZipArchiveCreateBack backEnd)
                backEnd.Prop_DestinationZip = $"\"{dialog.FileName}\"";
        }
    }
}
