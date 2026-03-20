// =============================================================================
// DataTableToHtmlLogic.cs — логика конвертации DataTable в HTML.
//
// Преобразует DataTable в красиво оформленную HTML таблицу
// с поддержкой предустановленных тем и пользовательских цветов.
//
// Поддерживает:
// - 4 предустановленные темы (Light, Dark, Blue, Green) + Custom
// - Нумерацию строк
// - Обработку null-значений
// - Ограничение количества строк
// - Два режима вывода: только таблица или полный HTML-документ
// =============================================================================

using System;
using System.Data;
using System.Text;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>Тема оформления HTML-таблицы.</summary>
    public enum HtmlTableTheme
    {
        /// <summary>Светлая тема (белый фон, серые границы).</summary>
        Light,
        /// <summary>Тёмная тема (тёмный фон, светлый текст).</summary>
        Dark,
        /// <summary>Синяя тема (синий заголовок).</summary>
        Blue,
        /// <summary>Зелёная тема (зелёный заголовок).</summary>
        Green,
        /// <summary>Пользовательская тема.</summary>
        Custom
    }

    /// <summary>Режим вывода HTML.</summary>
    public enum HtmlOutputMode
    {
        /// <summary>Только таблица (теги table/thead/tbody/tr/td).</summary>
        TableOnly,
        /// <summary>Полный HTML-документ с html/head/style/body.</summary>
        FullDocument
    }

    /// <summary>Цвета темы оформления.</summary>
    public class ThemeColors
    {
        /// <summary>Цвет фона заголовка.</summary>
        public string Header { get; set; }

        /// <summary>Цвет фона нечётных строк.</summary>
        public string Row { get; set; }

        /// <summary>Цвет фона чётных строк.</summary>
        public string AltRow { get; set; }

        /// <summary>Цвет границ.</summary>
        public string Border { get; set; }

        /// <summary>Цвет текста заголовка.</summary>
        public string HeaderText { get; set; }
    }

    /// <summary>Результат конвертации DataTable в HTML.</summary>
    public class DataTableToHtmlResult
    {
        /// <summary>Сформированный HTML-код.</summary>
        public string Html { get; set; }

        /// <summary>Количество строк в исходной таблице.</summary>
        public int RowCount { get; set; }

        /// <summary>Количество столбцов в исходной таблице.</summary>
        public int ColumnCount { get; set; }

        /// <summary>Сообщение об ошибке (null если успешно).</summary>
        public string ErrorMessage { get; set; }

        /// <summary>true если конвертация успешна.</summary>
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }

    /// <summary>
    /// Логика конвертации DataTable в HTML таблицу.
    /// </summary>
    public static class DataTableToHtmlLogic
    {
        // =====================================================================
        // Основной метод
        // =====================================================================

        /// <summary>
        /// Конвертирует DataTable в HTML таблицу.
        /// </summary>
        /// <param name="table">Исходная таблица данных.</param>
        /// <param name="theme">Тема оформления.</param>
        /// <param name="outputMode">Режим вывода HTML.</param>
        /// <param name="showRowNumbers">Добавить колонку с нумерацией строк.</param>
        /// <param name="nullDisplay">Текст для отображения null-значений.</param>
        /// <param name="maxRows">Максимальное количество строк (0 = без ограничений).</param>
        /// <param name="headerColor">Цвет фона заголовка (HEX).</param>
        /// <param name="rowColor">Цвет фона строк (HEX).</param>
        /// <param name="altRowColor">Цвет фона чётных строк (HEX).</param>
        /// <param name="borderColor">Цвет границ (HEX).</param>
        /// <returns>Результат конвертации.</returns>
        public static DataTableToHtmlResult Convert(
            DataTable table,
            HtmlTableTheme theme = HtmlTableTheme.Light,
            HtmlOutputMode outputMode = HtmlOutputMode.TableOnly,
            bool showRowNumbers = false,
            string nullDisplay = "",
            int maxRows = 0,
            string headerColor = "#1565C0",
            string rowColor = "#FFFFFF",
            string altRowColor = "#E3F2FD",
            string borderColor = "#BBDEFB")
        {
            // Валидация
            if (table == null)
                return new DataTableToHtmlResult { ErrorMessage = "Таблица данных не указана" };

            if (maxRows < 0)
                return new DataTableToHtmlResult { ErrorMessage = "Максимальное количество строк не может быть отрицательным" };

            // Получаем цвета для темы
            var colors = GetThemeColors(theme, headerColor, rowColor, altRowColor, borderColor);

            // Генерируем HTML
            string html = outputMode == HtmlOutputMode.FullDocument
                ? GenerateFullDocument(table, colors, showRowNumbers, nullDisplay, maxRows)
                : GenerateTableOnly(table, colors, showRowNumbers, nullDisplay, maxRows);

            return new DataTableToHtmlResult
            {
                Html = html,
                RowCount = table.Rows.Count,
                ColumnCount = table.Columns.Count
            };
        }

        // =====================================================================
        // Получение цветов темы
        // =====================================================================

        /// <summary>
        /// Возвращает цвета для указанной темы.
        /// </summary>
        public static ThemeColors GetThemeColors(
            HtmlTableTheme theme,
            string headerColor = "#1565C0",
            string rowColor = "#FFFFFF",
            string altRowColor = "#E3F2FD",
            string borderColor = "#BBDEFB")
        {
            switch (theme)
            {
                case HtmlTableTheme.Light:
                    return new ThemeColors
                    {
                        Header = "#F5F5F5",
                        Row = "#FFFFFF",
                        AltRow = "#F9F9F9",
                        Border = "#DDDDDD",
                        HeaderText = "#333333"
                    };

                case HtmlTableTheme.Dark:
                    return new ThemeColors
                    {
                        Header = "#2D2D2D",
                        Row = "#1E1E1E",
                        AltRow = "#2A2A2A",
                        Border = "#404040",
                        HeaderText = "#FFFFFF"
                    };

                case HtmlTableTheme.Blue:
                    return new ThemeColors
                    {
                        Header = "#1565C0",
                        Row = "#FFFFFF",
                        AltRow = "#E3F2FD",
                        Border = "#BBDEFB",
                        HeaderText = "#FFFFFF"
                    };

                case HtmlTableTheme.Green:
                    return new ThemeColors
                    {
                        Header = "#2E7D32",
                        Row = "#FFFFFF",
                        AltRow = "#E8F5E9",
                        Border = "#C8E6C9",
                        HeaderText = "#FFFFFF"
                    };

                case HtmlTableTheme.Custom:
                    return new ThemeColors
                    {
                        Header = headerColor ?? "#1565C0",
                        Row = rowColor ?? "#FFFFFF",
                        AltRow = altRowColor ?? "#E3F2FD",
                        Border = borderColor ?? "#BBDEFB",
                        HeaderText = "#FFFFFF"
                    };

                default:
                    return new ThemeColors
                    {
                        Header = "#1565C0",
                        Row = "#FFFFFF",
                        AltRow = "#E3F2FD",
                        Border = "#BBDEFB",
                        HeaderText = "#FFFFFF"
                    };
            }
        }

        // =====================================================================
        // Генерация HTML
        // =====================================================================

        /// <summary>
        /// Генерирует только HTML-таблицу (без обёртки документа).
        /// </summary>
        private static string GenerateTableOnly(
            DataTable table,
            ThemeColors colors,
            bool showRowNumbers,
            string nullDisplay,
            int maxRows)
        {
            var sb = new StringBuilder();

            // Начало таблицы
            sb.AppendLine($"<table style=\"border-collapse: collapse; width: 100%; font-family: Arial, sans-serif; border: 1px solid {colors.Border};\">");

            // Заголовок
            sb.AppendLine("<thead>");
            sb.AppendLine($"<tr style=\"background-color: {colors.Header}; color: {colors.HeaderText};\">");

            if (showRowNumbers)
            {
                sb.AppendLine($"<th style=\"padding: 10px; border: 1px solid {colors.Border}; text-align: center; font-weight: bold;\">№</th>");
            }

            foreach (DataColumn column in table.Columns)
            {
                sb.AppendLine($"<th style=\"padding: 10px; border: 1px solid {colors.Border}; text-align: left; font-weight: bold;\">{EscapeHtml(column.ColumnName)}</th>");
            }

            sb.AppendLine("</tr>");
            sb.AppendLine("</thead>");

            // Тело таблицы
            sb.AppendLine("<tbody>");

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

                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");

            return sb.ToString();
        }

        /// <summary>
        /// Генерирует полный HTML-документ с таблицей.
        /// </summary>
        private static string GenerateFullDocument(
            DataTable table,
            ThemeColors colors,
            bool showRowNumbers,
            string nullDisplay,
            int maxRows)
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
                sb.AppendLine("<th style=\"text-align: center;\">№</th>");
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

        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>
        /// Экранирует специальные символы HTML.
        /// </summary>
        public static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            return System.Security.SecurityElement.Escape(text);
        }
    }
}
