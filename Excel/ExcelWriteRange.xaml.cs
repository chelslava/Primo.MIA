using Microsoft.Win32;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Логика взаимодействия для ExcelWriteRange.xaml (issue #47).
    /// </summary>
    public partial class ExcelWriteRange : UserControl
    {
        public ExcelWriteRange()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка «...» у пути к файлу: выбор/создание .xlsx (SaveFileDialog).
        /// Путь (в кавычках — формат Primo RPA) → Prop_FilePath.
        /// </summary>
        private void BtnBrowseXlsx_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title           = "Файл Excel для записи",
                Filter          = "Файлы Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                FilterIndex     = 1,
                DefaultExt      = "xlsx",
                AddExtension    = true
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

            if (DataContext is ExcelWriteRangeBack backEnd)
                backEnd.Prop_FilePath = '"' + dialog.FileName + '"';
        }
    }
}
