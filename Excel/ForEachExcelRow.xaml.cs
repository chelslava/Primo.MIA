using LTools.Common.Helpers;
using LTools.Common.WFItems;
using Microsoft.Win32;
using System.Windows.Controls;

namespace Primo.MIA
{
    /// <summary>
    /// Представление контейнера «Excel: Для каждой строки».
    /// Паттерн SDK custom-container: рендер в Studio — XAML с WFContainerBase,
    /// робо-режим (DoRender = false) — контейнер создаётся кодом.
    /// </summary>
    public partial class ForEachExcelRow : UserControl
    {
        private WFContainerBase _cntBody;

        /// <summary>
        /// Последнее созданное представление этого типа. В robot-режиме это
        /// экземпляр текущего прогона — Back использует его как fallback
        /// для получения тела цикла.
        /// </summary>
        public static ForEachExcelRow LastInstance;

        /// <summary>Тело цикла: поле из XAML (Studio) либо созданное кодом (робот).</summary>
        public WFContainerBase Body => cntBody ?? _cntBody;

        public ForEachExcelRow()
        {
            LastInstance = this;
            if (WFHelper.DoRender)
                InitializeComponent();
            else
            {
                _cntBody = new WFContainerBase();
                Content = _cntBody;
            }
        }

        /// <summary>Обработчик кнопки «...» — выбор .xlsx файла.</summary>
        private void BtnBrowseXlsx_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите файл Excel",
                Filter = "Файлы Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                FilterIndex = 1,
                CheckFileExists = true,
                Multiselect = false
            };

            var currentPath = TxtFilePath.Text?.Trim('"', ' ');
            if (!string.IsNullOrEmpty(currentPath))
            {
                try
                {
                    var dir = System.IO.Path.GetDirectoryName(currentPath);
                    if (System.IO.Directory.Exists(dir))
                        dialog.InitialDirectory = dir;
                }
                catch
                {
                    // некорректный путь — не задаём InitialDirectory
                }
            }

            if (dialog.ShowDialog() != true)
                return;

            if (DataContext is ForEachExcelRowBack back)
                back.Prop_FilePath = $"\"{dialog.FileName}\"";
        }
    }
}
