using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Р›РѕРіРёРєР° РІР·Р°РёРјРѕРґРµР№СЃС‚РІРёСЏ РґР»СЏ PgpEncryptFile.xaml (issue #35).
    /// </summary>
    public partial class PgpEncryptFile : UserControl
    {
        public PgpEncryptFile()
        {
            InitializeComponent();
        }

        private void BtnBrowseInput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "Р’С‹Р±РµСЂРёС‚Рµ РёСЃС…РѕРґРЅС‹Р№ С„Р°Р№Р»", Filter = "Р’СЃРµ С„Р°Р№Р»С‹ (*.*)|*.*" };
            if (dialog.ShowDialog() != true) return;
            if (DataContext is PgpEncryptFileBack backEnd)
                backEnd.Prop_InputPath = "\"" + dialog.FileName + "\"";
        }

        private void BtnBrowseKey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "Р’С‹Р±РµСЂРёС‚Рµ РїСѓР±Р»РёС‡РЅС‹Р№ РєР»СЋС‡", Filter = "РљР»СЋС‡Рё PGP (*.asc;*.gpg)|*.asc;*.gpg|Р’СЃРµ С„Р°Р№Р»С‹ (*.*)|*.*" };
            if (dialog.ShowDialog() != true) return;
            if (DataContext is PgpEncryptFileBack backEnd)
                backEnd.Prop_PublicKeyPath = "\"" + dialog.FileName + "\"";
        }
    }
}