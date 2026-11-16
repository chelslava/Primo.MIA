using Microsoft.Win32;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Логика взаимодействия для EmlParse.xaml (issue #37, фаза 2).
    /// </summary>
    public partial class EmlParse : UserControl
    {
        public EmlParse()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка «...» у пути к EML: выбор файла (OpenFileDialog).
        /// Путь (в кавычках — формат Primo RPA) → Prop_EmlPath.
        /// </summary>
        private void BtnBrowseEml_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title           = "Выберите EML-файл",
                Filter          = "Электронные письма (*.eml)|*.eml|Все файлы (*.*)|*.*",
                FilterIndex     = 1,
                Multiselect     = false
            };

            var currentPath = TxtEmlPath.Text?.Trim('"', ' ');
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

            if (DataContext is EmlParseBack backEnd)
                backEnd.Prop_EmlPath = '"' + dialog.FileName + '"';
        }
    }
}
