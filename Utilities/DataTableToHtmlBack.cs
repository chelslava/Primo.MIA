// =============================================================================
// DataTableToHtmlBack.cs — конвертация DataTable в HTML таблицу.
//
// Активность преобразует DataTable в красиво оформленную HTML таблицу
// с поддержкой предустановленных тем и пользовательских цветов.
//
// Поддерживает:
// - 4 предустановленные темы (Light, Dark, Blue, Green) + Custom
// - Нумерацию строк
// - Обработку null-значений
// - Ограничение количества строк
// - Два режима вывода: только таблица или полный HTML-документ
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для конвертации DataTable в красиво оформленную HTML таблицу.
    /// Поддерживает предустановленные темы, пользовательские цвета, нумерацию строк,
    /// обработку null-значений и ограничение количества строк.
    /// </summary>
    public class DataTableToHtmlBack : PrimoComponentTO<DataTableToHtml>
    {
        /// <inheritdoc/>
        public override string GroupName
        {
            get => ActivityCategories.Utilities;
            protected set { }
        }

        /// <inheritdoc/>
        protected override int sdkTimeOut
        {
            get => 30000;
            set { }
        }

        // ============== INPUT PROPERTIES ==============

        #region Prop_DataTable

        private DataTable _propDataTable;

        /// <summary>Исходная таблица данных для конвертации.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_DataTable)]
        public DataTable Prop_DataTable
        {
            get => _propDataTable;
            set { _propDataTable = value; InvokePropertyChanged(this, nameof(Prop_DataTable)); }
        }

        #endregion

        #region Prop_Theme

        private HtmlTableTheme _propTheme = HtmlTableTheme.Light;

        /// <summary>Предустановленная тема оформления.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Style), System.ComponentModel.DisplayName(ActivityStrings.Field_HtmlTheme)]
        public HtmlTableTheme Prop_Theme
        {
            get => _propTheme;
            set { _propTheme = value; InvokePropertyChanged(this, nameof(Prop_Theme)); }
        }

        #endregion

        #region Prop_OutputMode

        private HtmlOutputMode _propOutputMode = HtmlOutputMode.TableOnly;

        /// <summary>Режим вывода HTML.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_HtmlOutputMode)]
        public HtmlOutputMode Prop_OutputMode
        {
            get => _propOutputMode;
            set { _propOutputMode = value; InvokePropertyChanged(this, nameof(Prop_OutputMode)); }
        }

        #endregion

        #region Prop_ShowRowNumbers

        private bool _propShowRowNumbers = false;

        /// <summary>Добавить колонку с нумерацией строк.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_ShowRowNumbers)]
        public bool Prop_ShowRowNumbers
        {
            get => _propShowRowNumbers;
            set { _propShowRowNumbers = value; InvokePropertyChanged(this, nameof(Prop_ShowRowNumbers)); }
        }

        #endregion

        #region Prop_NullDisplay

        private string _propNullDisplay = "";

        /// <summary>Текст для отображения null-значений.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_NullDisplay)]
        public string Prop_NullDisplay
        {
            get => _propNullDisplay;
            set { _propNullDisplay = value; InvokePropertyChanged(this, nameof(Prop_NullDisplay)); }
        }

        #endregion

        #region Prop_MaxRows

        private int _propMaxRows = 0;

        /// <summary>Максимальное количество строк (0 = без ограничений).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_MaxRows)]
        public int Prop_MaxRows
        {
            get => _propMaxRows;
            set { _propMaxRows = value; InvokePropertyChanged(this, nameof(Prop_MaxRows)); }
        }

        #endregion

        #region Prop_HeaderColor

        private string _propHeaderColor = "#1565C0";

        /// <summary>Цвет фона заголовка (HEX).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Style), System.ComponentModel.DisplayName(ActivityStrings.Field_HeaderColor)]
        public string Prop_HeaderColor
        {
            get => _propHeaderColor;
            set { _propHeaderColor = value; InvokePropertyChanged(this, nameof(Prop_HeaderColor)); }
        }

        #endregion

        #region Prop_RowColor

        private string _propRowColor = "#FFFFFF";

        /// <summary>Цвет фона строк (HEX).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Style), System.ComponentModel.DisplayName(ActivityStrings.Field_RowColor)]
        public string Prop_RowColor
        {
            get => _propRowColor;
            set { _propRowColor = value; InvokePropertyChanged(this, nameof(Prop_RowColor)); }
        }

        #endregion

        #region Prop_AltRowColor

        private string _propAltRowColor = "#E3F2FD";

        /// <summary>Цвет фона чётных строк (HEX).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Style), System.ComponentModel.DisplayName(ActivityStrings.Field_AltRowColor)]
        public string Prop_AltRowColor
        {
            get => _propAltRowColor;
            set { _propAltRowColor = value; InvokePropertyChanged(this, nameof(Prop_AltRowColor)); }
        }

        #endregion

        #region Prop_BorderColor

        private string _propBorderColor = "#BBDEFB";

        /// <summary>Цвет границ (HEX).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Style), System.ComponentModel.DisplayName(ActivityStrings.Field_BorderColor)]
        public string Prop_BorderColor
        {
            get => _propBorderColor;
            set { _propBorderColor = value; InvokePropertyChanged(this, nameof(Prop_BorderColor)); }
        }

        #endregion

        // ============== OUTPUT PROPERTIES ==============

        #region Prop_HtmlOutput

        private string _propHtmlOutput;

        /// <summary>Сформированный HTML-код.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_HtmlOutput)]
        public string Prop_HtmlOutput
        {
            get => _propHtmlOutput;
            set { _propHtmlOutput = value; InvokePropertyChanged(this, nameof(Prop_HtmlOutput)); }
        }

        #endregion

        #region Prop_RowCount

        private int _propRowCount;

        /// <summary>Количество строк в исходной таблице.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RowCount)]
        public int Prop_RowCount
        {
            get => _propRowCount;
            set { _propRowCount = value; InvokePropertyChanged(this, nameof(Prop_RowCount)); }
        }

        #endregion

        #region Prop_ColumnCount

        private int _propColumnCount;

        /// <summary>Количество столбцов в исходной таблице.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ColumnCount)]
        public int Prop_ColumnCount
        {
            get => _propColumnCount;
            set { _propColumnCount = value; InvokePropertyChanged(this, nameof(Prop_ColumnCount)); }
        }

        #endregion

        /// <summary>
        /// Конструктор компонента.
        /// </summary>
        public DataTableToHtmlBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "DataTable в HTML";
            sdkComponentHelp = @"Компонент ""DataTable в HTML"" (DataTableToHtmlBack)
Активность для конвертации DataTable в красиво оформленную HTML таблицу с гибкими настройками стилизации.

Основные
Таблица данных*: [DataTable] Исходная таблица данных для конвертации.
Режим вывода HTML: [HtmlOutputMode] Формат выходного HTML.
  - TableOnly: только таблица (теги table/thead/tbody/tr/td)
  - FullDocument: полный HTML-документ с html/head/style/body
Нумерация строк: [Boolean] Добавить колонку с порядковыми номерами строк.
Текст для null: [String] Текст для отображения null-значений в ячейках. По умолчанию пустая строка.
Макс. строк: [Int32] Ограничение количества выводимых строк. 0 = без ограничений.

Стиль
Тема оформления: [HtmlTableTheme] Предустановленная тема.
  - Light: светлая тема (белый фон, серые границы)
  - Dark: тёмная тема (тёмный фон, светлый текст)
  - Blue: синяя тема (синий заголовок, чередующиеся синие оттенки)
  - Green: зелёная тема (зелёный заголовок, чередующиеся зелёные оттенки)
  - Custom: пользовательская тема (цвета задаются вручную)

Цвет заголовка: [String] Цвет фона заголовка в формате HEX (например, #1565C0). Используется при теме Custom.
Цвет строк: [String] Цвет фона нечётных строк в формате HEX. Используется при теме Custom.
Цвет чётных строк: [String] Цвет фона чётных строк в формате HEX. Используется при теме Custom.
Цвет границ: [String] Цвет границ таблицы в формате HEX. Используется при теме Custom.

Выходные данные
HTML-код: [String] Сформированный HTML-код таблицы или документа.
Количество строк: [Int32] Количество строк в исходной таблице.
Количество столбцов: [Int32] Количество столбцов в исходной таблице.";

            sdkComponentIcon = ActivityIcons.Table;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<DataTable>("Prop_DataTable", "Исходная таблица данных для конвертации в HTML"),
                PropertyBuilder.Enum<HtmlOutputMode>("Prop_OutputMode", "Режим вывода: только таблица или полный HTML-документ"),
                PropertyBuilder.BooleanObject("Prop_ShowRowNumbers", "Добавить колонку с нумерацией строк"),
                PropertyBuilder.String("Prop_NullDisplay", "Текст для отображения null-значений"),
                PropertyBuilder.Int("Prop_MaxRows", "Максимальное количество строк (0 = без ограничений)"),
                PropertyBuilder.Enum<HtmlTableTheme>("Prop_Theme", "Предустановленная тема оформления"),
                PropertyBuilder.String("Prop_HeaderColor", "Цвет фона заголовка (HEX, например #1565C0)"),
                PropertyBuilder.String("Prop_RowColor", "Цвет фона строк (HEX)"),
                PropertyBuilder.String("Prop_AltRowColor", "Цвет фона чётных строк (HEX)"),
                PropertyBuilder.String("Prop_BorderColor", "Цвет границ (HEX)"),
                PropertyBuilder.Variable<string>("Prop_HtmlOutput", "Сформированный HTML-код"),
                PropertyBuilder.Variable<int>("Prop_RowCount", "Количество строк в исходной таблице"),
                PropertyBuilder.Variable<int>("Prop_ColumnCount", "Количество столбцов в исходной таблице")
            };

            InitClass(container);

            // Значения по умолчанию
            this.Prop_NullDisplay = "\"\"";
            this.Prop_MaxRows = 0;
        }

        /// <summary>
        /// Основное действие компонента.
        /// </summary>
        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                // Валидация
                if (this.Prop_DataTable == null)
                {
                    return new ExecutionResult
                    {
                        IsSuccess = false,
                        ErrorMessage = ActivityStrings.Error_DataTableRequired
                    };
                }

                // Получаем параметры
                var table = this.Prop_DataTable;
                var theme = this.Prop_Theme;
                var outputMode = this.Prop_OutputMode;
                var showRowNumbers = this.Prop_ShowRowNumbers;
                var nullDisplay = this.Prop_NullDisplay ?? "";
                var maxRows = this.Prop_MaxRows;

                // Записываем метаданные
                this.Prop_RowCount = table.Rows.Count;
                this.Prop_ColumnCount = table.Columns.Count;

                // Получаем цвета для темы
                var colors = GetThemeColors(theme);

                // Генерируем HTML
                string html;
                if (outputMode == HtmlOutputMode.FullDocument)
                {
                    html = GenerateFullDocument(table, colors, showRowNumbers, nullDisplay, maxRows);
                }
                else
                {
                    html = GenerateTableOnly(table, colors, showRowNumbers, nullDisplay, maxRows);
                }

                // Записываем результат
                this.Prop_HtmlOutput = html;

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"HTML таблица создана: {this.Prop_RowCount} строк, {this.Prop_ColumnCount} столбцов"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка при конвертации DataTable в HTML: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Валидация входных параметров.
        /// </summary>
        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (this.Prop_DataTable == null)
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_DataTable),
                    Error = ActivityStrings.Error_DataTableRequired
                });
            }

            if (this.Prop_MaxRows < 0)
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = nameof(Prop_MaxRows),
                    Error = ActivityStrings.Error_MaxRowsNegative
                });
            }

            if (this.Prop_Theme == HtmlTableTheme.Custom)
            {
                bool hasAnyColor = !string.IsNullOrWhiteSpace(this.Prop_HeaderColor) ||
                                   !string.IsNullOrWhiteSpace(this.Prop_RowColor) ||
                                   !string.IsNullOrWhiteSpace(this.Prop_AltRowColor) ||
                                   !string.IsNullOrWhiteSpace(this.Prop_BorderColor);

                if (!hasAnyColor)
                {
                    ret.Items.Add(new ValidationResult.ValidationItem
                    {
                        PropertyName = nameof(Prop_Theme),
                        Error = ActivityStrings.Error_CustomThemeRequiresColors
                    });
                }
            }

            return ret;
        }

        /// <summary>
        /// Возвращает цвета для указанной темы.
        /// </summary>
        /// <param name="theme">Тема оформления.</param>
        /// <returns>Кортеж с цветами (header, row, altRow, border).</returns>
        private (string Header, string Row, string AltRow, string Border, string HeaderText) GetThemeColors(HtmlTableTheme theme)
        {
            switch (theme)
            {
                case HtmlTableTheme.Light:
                    return ("#F5F5F5", "#FFFFFF", "#F9F9F9", "#DDDDDD", "#333333");

                case HtmlTableTheme.Dark:
                    return ("#2D2D2D", "#1E1E1E", "#2A2A2A", "#404040", "#FFFFFF");

                case HtmlTableTheme.Blue:
                    return ("#1565C0", "#FFFFFF", "#E3F2FD", "#BBDEFB", "#FFFFFF");

                case HtmlTableTheme.Green:
                    return ("#2E7D32", "#FFFFFF", "#E8F5E9", "#C8E6C9", "#FFFFFF");

                case HtmlTableTheme.Custom:
                    return (
                        this.Prop_HeaderColor ?? "#1565C0",
                        this.Prop_RowColor ?? "#FFFFFF",
                        this.Prop_AltRowColor ?? "#E3F2FD",
                        this.Prop_BorderColor ?? "#BBDEFB",
                        "#FFFFFF"
                    );

                default:
                    return ("#1565C0", "#FFFFFF", "#E3F2FD", "#BBDEFB", "#FFFFFF");
            }
        }

        /// <summary>
        /// Генерирует только HTML-таблицу (без обёртки документа).
        /// </summary>
        private string GenerateTableOnly(DataTable table, (string Header, string Row, string AltRow, string Border, string HeaderText) colors, bool showRowNumbers, string nullDisplay, int maxRows)
        {
            var sb = new StringBuilder();

            // Начало таблицы
            sb.AppendLine($"<table style=\"border-collapse: collapse; width: 100%; font-family: Arial, sans-serif; border: 1px solid {colors.Border};\">");

            // Заголовок
            sb.AppendLine($"<thead>");
            sb.AppendLine($"<tr style=\"background-color: {colors.Header}; color: {colors.HeaderText};\">");

            if (showRowNumbers)
            {
                sb.AppendLine($"<th style=\"padding: 10px; border: 1px solid {colors.Border}; text-align: center; font-weight: bold;\">№</th>");
            }

            foreach (DataColumn column in table.Columns)
            {
                sb.AppendLine($"<th style=\"padding: 10px; border: 1px solid {colors.Border}; text-align: left; font-weight: bold;\">{EscapeHtml(column.ColumnName)}</th>");
            }

            sb.AppendLine($"</tr>");
            sb.AppendLine($"</thead>");

            // Тело таблицы
            sb.AppendLine($"<tbody>");

            int rowCount = maxRows > 0 ? Math.Min(maxRows, table.Rows.Count) : table.Rows.Count;

            for (int i = 0; i < rowCount; i++)
            {
                var row = table.Rows[i];
                var bgColor = i % 2 == 0 ? colors.Row : colors.AltRow;

                sb.AppendLine($"<tr style=\"background-color: {bgColor};\">");

                if (showRowNumbers)
                {
                    sb.AppendLine($"<td style=\"padding: 8px; border: 1px solid {colors.Border}; text-align: center;\">{i + 1}</td>");
                }

                foreach (DataColumn column in table.Columns)
                {
                    var value = row[column] == null || row[column] == DBNull.Value
                        ? nullDisplay
                        : row[column].ToString();

                    sb.AppendLine($"<td style=\"padding: 8px; border: 1px solid {colors.Border};\">{EscapeHtml(value)}</td>");
                }

                sb.AppendLine($"</tr>");
            }

            sb.AppendLine($"</tbody>");
            sb.AppendLine($"</table>");

            return sb.ToString();
        }

        /// <summary>
        /// Генерирует полный HTML-документ с таблицей.
        /// </summary>
        private string GenerateFullDocument(DataTable table, (string Header, string Row, string AltRow, string Border, string HeaderText) colors, bool showRowNumbers, string nullDisplay, int maxRows)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"ru\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("<title>DataTable Export</title>");
            sb.AppendLine("<style>");

            // Стили
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; background-color: #FAFAFA; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; background-color: #FFFFFF; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            sb.AppendLine($"th {{ background-color: {colors.Header}; color: {colors.HeaderText}; padding: 12px; text-align: left; border: 1px solid {colors.Border}; }}");
            sb.AppendLine($"td {{ padding: 10px; border: 1px solid {colors.Border}; }}");
            sb.AppendLine($"tbody tr:nth-child(odd) {{ background-color: {colors.Row}; }}");
            sb.AppendLine($"tbody tr:nth-child(even) {{ background-color: {colors.AltRow}; }}");
            sb.AppendLine("tbody tr:hover { background-color: #F5F5F5; }");

            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Таблица
            sb.AppendLine("<table>");

            // Заголовок
            sb.AppendLine("<thead>");
            sb.AppendLine("<tr>");

            if (showRowNumbers)
            {
                sb.AppendLine($"<th style=\"text-align: center;\">№</th>");
            }

            foreach (DataColumn column in table.Columns)
            {
                sb.AppendLine($"<th>{EscapeHtml(column.ColumnName)}</th>");
            }

            sb.AppendLine("</tr>");
            sb.AppendLine("</thead>");

            // Тело таблицы
            sb.AppendLine("<tbody>");

            int rowCount = maxRows > 0 ? Math.Min(maxRows, table.Rows.Count) : table.Rows.Count;

            for (int i = 0; i < rowCount; i++)
            {
                var row = table.Rows[i];

                sb.AppendLine("<tr>");

                if (showRowNumbers)
                {
                    sb.AppendLine($"<td style=\"text-align: center;\">{i + 1}</td>");
                }

                foreach (DataColumn column in table.Columns)
                {
                    var value = row[column] == null || row[column] == DBNull.Value
                        ? nullDisplay
                        : row[column].ToString();

                    sb.AppendLine($"<td>{EscapeHtml(value)}</td>");
                }

                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        /// <summary>
        /// Экранирует специальные символы HTML.
        /// </summary>
        /// <param name="text">Исходный текст.</param>
        /// <returns>Экранированный текст.</returns>
        private string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // Используем SecurityElement.Escape для экранирования XML/HTML символов
            return System.Security.SecurityElement.Escape(text);
        }
    }
}
