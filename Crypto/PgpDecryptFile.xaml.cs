using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Р›РѕРіРёРєР° РІР·Р°РёРјРѕРґРµР№СЃС‚РІРёСЏ РґР»СЏ PgpDecryptFile.xaml (issue #35).
    /// </summary>
    public partial class PgpDecryptFile : UserControl
    {
        public PgpDecryptFile()
        {
            InitializeComponent();
        }

        private void BtnBrowseInput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "Р’С‹Р±РµСЂРёС‚Рµ Р·Р°С€РёС„СЂРѕРІР°РЅРЅС‹Р№ С„Р°Р№Р»", Filter = "PGP (*.pgp;*.asc)|*.pgp;*.asc|Р’СЃРµ С„Р°Р№Р»С‹ (*.*)|*.*" };
            if (dialog.ShowDialog() != true) return;
            if (DataContext is PgpDecryptFileBack backEnd)
                backEnd.Prop_InputPath = "\"" + dialog.FileName + "\"";
        }

        private void BtnBrowseKey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "Р’С‹Р±РµСЂРёС‚Рµ РїСЂРёРІР°С‚РЅС‹Р№ РєР»СЋС‡", Filter = "РљР»СЋС‡Рё PGP (*.asc;*.gpg)|*.asc;*.gpg|Р’СЃРµ С„Р°Р№Р»С‹ (*.*)|*.*" };
            if (dialog.ShowDialog() != true) return;
            if (DataContext is PgpDecryptFileBack backEnd)
                backEnd.Prop_PrivateKeyPath = "\"" + dialog.FileName + "\"";
        }
    }
}