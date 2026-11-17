using Microsoft.Win32;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Логика взаимодействия для ExcelFillColumnFormula.xaml (issue #47).
    /// </summary>
    public partial class ExcelFillColumnFormula : UserControl
    {
        public ExcelFillColumnFormula()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка «...» у пути к файлу: выбор .xlsx (OpenFileDialog).
        /// Путь (в кавычках — формат Primo RPA) → Prop_FilePath.
        /// </summary>
        private void BtnBrowseXlsx_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title           = "Выберите файл Excel",
                Filter          = "Файлы Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                FilterIndex     = 1,
                Multiselect     = false
            };

            var currentPath = TxtFilePath.Text?.Trim('"', ' ');
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

            if (DataContext is ExcelFillColumnFormulaBack backEnd)
                backEnd.Prop_FilePath = '"' + dialog.FileName + '"';
        }
    }
}
