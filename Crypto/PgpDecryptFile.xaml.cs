using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Логика взаимодействия для PgpDecryptFile.xaml (issue #35).
    /// </summary>
    public partial class PgpDecryptFile : UserControl
    {
        public PgpDecryptFile()
        {
            InitializeComponent();
        }

        private void BtnBrowseInput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "Выберите зашифрованный файл", Filter = "PGP (*.pgp;*.asc)|*.pgp;*.asc|Все файлы (*.*)|*.*" };
            if (dialog.ShowDialog() != true) return;
            if (DataContext is PgpDecryptFileBack backEnd)
                backEnd.Prop_InputPath = "\"" + dialog.FileName + "\"";
        }

        private void BtnBrowseKey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "Выберите приватный ключ", Filter = "Ключи PGP (*.asc;*.gpg)|*.asc;*.gpg|Все файлы (*.*)|*.*" };
            if (dialog.ShowDialog() != true) return;
            if (DataContext is PgpDecryptFileBack backEnd)
                backEnd.Prop_PrivateKeyPath = "\"" + dialog.FileName + "\"";
        }
    }
}