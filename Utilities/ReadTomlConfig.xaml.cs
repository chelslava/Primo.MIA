// =============================================================================
// ReadTomlConfig.xaml.cs — code-behind для интерфейса активности ReadTomlConfig.
//
// Содержит:
//   - Partial-класс ReadTomlConfig — инициализация UserControl и обработчик
//     кнопки "..." для выбора TOML-файла через стандартный диалог OpenFileDialog.
//
// Примечание:
//   Конвертер для динамической видимости блоков вынесен в общий файл
//   Common/Converters/EnumToVisibilityConverter.cs и используется через
//   ConverterParameter вместо отдельных экземпляров ModeToVisibilityConverter.
// =============================================================================

using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Логика взаимодействия для ReadTomlConfig.xaml.
    /// Инициализирует UserControl и обрабатывает выбор файла через диалог.
    /// </summary>
    public partial class ReadTomlConfig : UserControl
    {
        /// <summary>
        /// Конструктор по умолчанию — инициализирует компоненты XAML.
        /// </summary>
        public ReadTomlConfig()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "…" рядом с полем пути к файлу.
        ///
        /// Открывает стандартный диалог OpenFileDialog с фильтром *.toml.
        /// Если пользователь выбрал файл — путь оборачивается в кавычки
        /// (формат Primo RPA для строковых свойств) и записывается в Prop_FilePath
        /// через DataContext.
        ///
        /// Паттерн доступа к DataContext:
        ///   DataContext → ReadTomlConfigBack (устанавливается фреймворком Primo)
        ///   Prop_FilePath — строковое свойство с StoringProperty + PropertyChanged
        /// </summary>
        private void BtnBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            // Создаём диалог выбора файла с фильтром для TOML-файлов
            var dialog = new OpenFileDialog
            {
                Title            = "Выберите TOML-файл конфигурации",
                Filter           = "TOML файлы (*.toml)|*.toml|Все файлы (*.*)|*.*",
                FilterIndex      = 1,
                CheckFileExists  = true,
                CheckPathExists  = true,
                Multiselect      = false
            };

            // Если в поле уже есть путь — открываем диалог в той же папке
            var currentPath = TxtFilePath.Text?.Trim('"', ' ');
            if (!string.IsNullOrEmpty(currentPath))
            {
                try
                {
                    // Используем LINQ-стиль через условное выражение вместо if/else
                    dialog.InitialDirectory = System.IO.Directory.Exists(currentPath)
                        ? currentPath
                        : System.IO.Path.GetDirectoryName(currentPath) ?? string.Empty;
                }
                catch
                {
                    // Некорректный путь — просто не задаём InitialDirectory
                }
            }

            // Показываем диалог и обрабатываем результат
            if (dialog.ShowDialog() != true) return;

            // Записываем выбранный путь в свойство DataContext через приведение типа
            // Путь оборачиваем в кавычки — стандарт для строковых свойств Primo RPA
            if (DataContext is ReadTomlConfigBack backEnd)
                backEnd.Prop_FilePath = $"\"{dialog.FileName}\"";
        }
    }
}
