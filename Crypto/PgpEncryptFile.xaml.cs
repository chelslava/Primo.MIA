using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Логика взаимодействия для PgpEncryptFile.xaml (issue #35).
    /// </summary>
    public partial class PgpEncryptFile : UserControl
    {
        public PgpEncryptFile()
        {
            InitializeComponent();
        }

        private void BtnBrowseInput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "Выберите исходный файл", Filter = "Все файлы (*.*)|*.*" };
            if (dialog.ShowDialog() != true) return;
            if (DataContext is PgpEncryptFileBack backEnd)
                backEnd.Prop_InputPath = "\"" + dialog.FileName + "\"";
        }

        private void BtnBrowseKey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "Выберите публичный ключ", Filter = "Ключи PGP (*.asc;*.gpg)|*.asc;*.gpg|Все файлы (*.*)|*.*" };
            if (dialog.ShowDialog() != true) return;
            if (DataContext is PgpEncryptFileBack backEnd)
                backEnd.Prop_PublicKeyPath = "\"" + dialog.FileName + "\"";
        }
    }
}