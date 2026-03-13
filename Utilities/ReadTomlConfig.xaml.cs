// =============================================================================
// ReadTomlConfig.xaml.cs — code-behind для интерфейса активности ReadTomlConfig.
//
// Содержит:
//   1. Partial-класс ReadTomlConfig — инициализация UserControl и обработчик
//      кнопки "..." для выбора TOML-файла через стандартный диалог OpenFileDialog.
//   2. Конвертер ModeToVisibilityConverter — управляет динамической видимостью
//      блоков полей в зависимости от выбранного TomlReadMode.
//
// Принцип работы конвертера:
//   - Каждый экземпляр конвертера в XAML настроен на конкретный TargetMode
//   - Если текущий ReadMode совпадает с TargetMode → Visibility.Visible
//   - Иначе → Visibility.Collapsed (блок не занимает место в layout)
// =============================================================================

using Microsoft.Win32;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Primo.MIA
{
    /// <summary>
    /// Логика взаимодействия для ReadTomlConfig.xaml.
    /// Инициализирует UserControl и обрабатывает выбор файла через диалог.
    /// </summary>
    public partial class ReadTomlConfig : UserControl
    {
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

    /// <summary>
    /// Конвертер для управления динамической видимостью блоков полей в XAML.
    ///
    /// Использование в XAML:
    ///   &lt;local:ModeToVisibilityConverter x:Key="SingleValueVisConverter"
    ///                                    TargetMode="SingleValue"/&gt;
    ///
    ///   Visibility="{Binding ReadMode, Converter={StaticResource SingleValueVisConverter}}"
    ///
    /// Логика:
    ///   value (текущий ReadMode) == TargetMode → Visible
    ///   value != TargetMode                    → Collapsed
    /// </summary>
    public class ModeToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Целевой режим, при котором блок становится видимым.
        /// Устанавливается в XAML через свойство TargetMode.
        /// </summary>
        public TomlReadMode TargetMode { get; set; }

        /// <summary>
        /// Конвертирует текущий TomlReadMode в Visibility.
        /// Если value совпадает с TargetMode — возвращает Visible, иначе Collapsed.
        /// </summary>
        /// <param name="value">Текущее значение ReadMode из привязки</param>
        /// <param name="targetType">Тип возврата (Visibility)</param>
        /// <param name="parameter">Не используется</param>
        /// <param name="culture">Не используется</param>
        /// <returns>Visibility.Visible или Visibility.Collapsed</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Проверяем что value — корректный TomlReadMode
            if (value is TomlReadMode currentMode)
                return currentMode == TargetMode
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // Если привязка вернула null или неожиданный тип — скрываем блок
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Обратная конвертация не поддерживается (одностороннее отображение).
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException(
                "ModeToVisibilityConverter: обратная конвертация не поддерживается");
    }
}
