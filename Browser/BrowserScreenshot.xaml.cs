// =============================================================================
// BrowserScreenshot.xaml.cs — code-behind для активности «Скриншот страницы».
// Обрабатывает нажатие кнопки обзора и открывает диалог выбора файла.
// =============================================================================

using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace Primo.MIA
{
    public partial class BrowserScreenshot : UserControl
    {
        public BrowserScreenshot()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Открывает диалог выбора файла и записывает путь в Prop_FilePath.
        /// </summary>
        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            // Создаём диалог сохранения файла (скриншот = сохранение)
            var dialog = new SaveFileDialog
            {
                Title = "Выберите путь для сохранения скриншота",
                Filter = "PNG изображение (*.png)|*.png|JPEG изображение (*.jpg)|*.jpg|Все файлы (*.*)|*.*",
                DefaultExt = "png",
                FileName = "screenshot"
            };

            // Если пользователь выбрал файл — записываем путь в привязку
            if (dialog.ShowDialog() == true)
            {
                // Получаем DataContext как активность и обновляем свойство
                if (DataContext is BrowserScreenshotBack activity)
                {
                    activity.Prop_FilePath = $"\"{dialog.FileName.Replace("\\", "\\\\")}\"";
                }
            }
        }
    }
}