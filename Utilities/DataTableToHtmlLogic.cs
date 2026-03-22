using System;
using System.Data;
using System.Text;

namespace Primo.MIA
{
    public class ThemeColors
    {
        public string Header { get; set; }

        public string Row { get; set; }

        public string AltRow { get; set; }

        public string Border { get; set; }

        public string HeaderText { get; set; }
    }

    public class DataTableToHtmlResult
    {
        public string Html { get; set; }

        public int RowCount { get; set; }

        public int ColumnCount { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }

    public static class DataTableToHtmlLogic
    {
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
            if (table == null)
                return new DataTableToHtmlResult { ErrorMessage = "Таблица данных не указана" };
            if (maxRows < 0)
                return new DataTableToHtmlResult { ErrorMessage = "Максимальное количество строк не может быть отрицательным" };

            ThemeColors colors = GetThemeColors(theme, headerColor, rowColor, altRowColor, borderColor);
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

        public static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return System.Security.SecurityElement.Escape(text);
        }

        private static string GenerateTableOnly(
            DataTable table,
            ThemeColors colors,
            bool showRowNumbers,
            string nullDisplay,
            int maxRows)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<table style=\"border-collapse: collapse; width: 100%; font-family: Arial, sans-serif; border: 1px solid {colors.Border};\">");
            sb.AppendLine("<thead>");
            sb.AppendLine($"<tr style=\"background-color: {colors.Header}; color: {colors.HeaderText};\">");

            if (showRowNumbers)
                sb.AppendLine($"<th style=\"padding: 10px; border: 1px solid {colors.Border}; text-align: center; font-weight: bold;\">№</th>");

            foreach (DataColumn column in table.Columns)
                sb.AppendLine($"<th style=\"padding: 10px; border: 1px solid {colors.Border}; text-align: left; font-weight: bold;\">{EscapeHtml(column.ColumnName)}</th>");

            sb.AppendLine("</tr>");
            sb.AppendLine("</thead>");
            sb.AppendLine("<tbody>");

            int rowCount = maxRows > 0 ? Math.Min(maxRows, table.Rows.Count) : table.Rows.Count;
            for (int i = 0; i < rowCount; i++)
            {
                DataRow row = table.Rows[i];
                string bgColor = i % 2 == 0 ? colors.Row : colors.AltRow;
                sb.AppendLine($"<tr style=\"background-color: {bgColor};\">");

                if (showRowNumbers)
                    sb.AppendLine($"<td style=\"padding: 8px; border: 1px solid {colors.Border}; text-align: center;\">{i + 1}</td>");

                foreach (DataColumn column in table.Columns)
                {
                    string value = row[column] == null || row[column] == DBNull.Value
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
            sb.AppendLine("<table>");
            sb.AppendLine("<thead>");
            sb.AppendLine("<tr>");

            if (showRowNumbers)
                sb.AppendLine("<th style=\"text-align: center;\">№</th>");

            foreach (DataColumn column in table.Columns)
                sb.AppendLine($"<th>{EscapeHtml(column.ColumnName)}</th>");

            sb.AppendLine("</tr>");
            sb.AppendLine("</thead>");
            sb.AppendLine("<tbody>");

            int rowCount = maxRows > 0 ? Math.Min(maxRows, table.Rows.Count) : table.Rows.Count;
            for (int i = 0; i < rowCount; i++)
            {
                DataRow row = table.Rows[i];
                sb.AppendLine("<tr>");

                if (showRowNumbers)
                    sb.AppendLine($"<td style=\"text-align: center;\">{i + 1}</td>");

                foreach (DataColumn column in table.Columns)
                {
                    string value = row[column] == null || row[column] == DBNull.Value
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
    }
}
