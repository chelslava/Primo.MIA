// =============================================================================
// DataTableToHtmlLogicTests.cs — тесты логики конвертации DataTable в HTML.
//
// Покрывает:
//   - Генерацию HTML-таблицы
//   - Различные темы оформления
//   - Нумерацию строк
//   - Обработку null-значений
//   - Ограничение количества строк
//   - Экранирование HTML
// =============================================================================

using System;
using System.Data;
using FluentAssertions;
using Primo.MIA.Tests.Logic;
using Xunit;

using Theme = Primo.MIA.Tests.Logic.HtmlTableTheme;
using OutputMode = Primo.MIA.Tests.Logic.HtmlOutputMode;

namespace Primo.MIA.Tests.Utilities
{
    /// <summary>Тесты логики конвертации DataTable в HTML.</summary>
    public class DataTableToHtmlLogicTests
    {
        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>Создаёт тестовую DataTable с данными.</summary>
        private static DataTable CreateTestTable(int rows = 3)
        {
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Age", typeof(int));
            table.Columns.Add("Email", typeof(string));

            for (int i = 1; i <= rows; i++)
            {
                table.Rows.Add($"User{i}", 20 + i, $"user{i}@example.com");
            }

            return table;
        }

        /// <summary>Создаёт DataTable с null-значениями.</summary>
        private static DataTable CreateTableWithNulls()
        {
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Value", typeof(string));

            table.Rows.Add("Valid", "value");
            table.Rows.Add("NullValue", null);
            table.Rows.Add(null, "another");

            return table;
        }

        // =====================================================================
        // Тесты базовой конвертации
        // =====================================================================

        [Fact(DisplayName = "Convert: простая таблица → корректный HTML")]
        public void Convert_SimpleTable_GeneratesHtml()
        {
            // Arrange
            var table = CreateTestTable(2);

            // Act
            var result = DataTableToHtmlLogic.Convert(table);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Html.Should().NotBeNullOrEmpty();
            result.RowCount.Should().Be(2);
            result.ColumnCount.Should().Be(3);
        }

        [Fact(DisplayName = "Convert: null таблица → ошибка")]
        public void Convert_NullTable_ReturnsError()
        {
            // Act
            var result = DataTableToHtmlLogic.Convert(null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Таблица данных не указана");
        }

        [Fact(DisplayName = "Convert: пустая таблица → пустая таблица HTML")]
        public void Convert_EmptyTable_GeneratesEmptyTable()
        {
            // Arrange
            var table = new DataTable();
            table.Columns.Add("Column1");

            // Act
            var result = DataTableToHtmlLogic.Convert(table);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Html.Should().Contain("<table");
            result.Html.Should().Contain("<thead>");
            result.RowCount.Should().Be(0);
        }

        [Fact(DisplayName = "Convert: отрицательное maxRows → ошибка")]
        public void Convert_NegativeMaxRows_ReturnsError()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = DataTableToHtmlLogic.Convert(table, maxRows: -5);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("отрицательным");
        }

        // =====================================================================
        // Тесты режимов вывода
        // =====================================================================

        [Fact(DisplayName = "Convert: TableOnly — только таблица")]
        public void Convert_TableOnly_ContainsOnlyTable()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = DataTableToHtmlLogic.Convert(table, outputMode: OutputMode.TableOnly);

            // Assert
            result.Html.Should().Contain("<table");
            result.Html.Should().NotContain("<!DOCTYPE html>");
            result.Html.Should().NotContain("<html");
        }

        [Fact(DisplayName = "Convert: FullDocument — полный HTML-документ")]
        public void Convert_FullDocument_ContainsFullHtml()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = DataTableToHtmlLogic.Convert(table, outputMode: OutputMode.FullDocument);

            // Assert
            result.Html.Should().Contain("<!DOCTYPE html>");
            result.Html.Should().Contain("<html");
            result.Html.Should().Contain("<head>");
            result.Html.Should().Contain("<body>");
            result.Html.Should().Contain("<table");
        }

        // =====================================================================
        // Тесты тем оформления
        // =====================================================================

        [Fact(DisplayName = "GetThemeColors: Light тема")]
        public void GetThemeColors_Light_ReturnsCorrectColors()
        {
            // Act
            var colors = DataTableToHtmlLogic.GetThemeColors(Theme.Light);

            // Assert
            colors.Header.Should().Be("#F5F5F5");
            colors.Row.Should().Be("#FFFFFF");
            colors.AltRow.Should().Be("#F9F9F9");
            colors.Border.Should().Be("#DDDDDD");
            colors.HeaderText.Should().Be("#333333");
        }

        [Fact(DisplayName = "GetThemeColors: Dark тема")]
        public void GetThemeColors_Dark_ReturnsCorrectColors()
        {
            // Act
            var colors = DataTableToHtmlLogic.GetThemeColors(Theme.Dark);

            // Assert
            colors.Header.Should().Be("#2D2D2D");
            colors.Row.Should().Be("#1E1E1E");
            colors.AltRow.Should().Be("#2A2A2A");
            colors.Border.Should().Be("#404040");
            colors.HeaderText.Should().Be("#FFFFFF");
        }

        [Fact(DisplayName = "GetThemeColors: Blue тема")]
        public void GetThemeColors_Blue_ReturnsCorrectColors()
        {
            // Act
            var colors = DataTableToHtmlLogic.GetThemeColors(Theme.Blue);

            // Assert
            colors.Header.Should().Be("#1565C0");
            colors.Row.Should().Be("#FFFFFF");
            colors.AltRow.Should().Be("#E3F2FD");
            colors.Border.Should().Be("#BBDEFB");
            colors.HeaderText.Should().Be("#FFFFFF");
        }

        [Fact(DisplayName = "GetThemeColors: Green тема")]
        public void GetThemeColors_Green_ReturnsCorrectColors()
        {
            // Act
            var colors = DataTableToHtmlLogic.GetThemeColors(Theme.Green);

            // Assert
            colors.Header.Should().Be("#2E7D32");
            colors.Row.Should().Be("#FFFFFF");
            colors.AltRow.Should().Be("#E8F5E9");
            colors.Border.Should().Be("#C8E6C9");
            colors.HeaderText.Should().Be("#FFFFFF");
        }

        [Fact(DisplayName = "GetThemeColors: Custom тема — использует пользовательские цвета")]
        public void GetThemeColors_Custom_UsesCustomColors()
        {
            // Act
            var colors = DataTableToHtmlLogic.GetThemeColors(
                Theme.Custom,
                headerColor: "#FF0000",
                rowColor: "#00FF00",
                altRowColor: "#0000FF",
                borderColor: "#FFFF00");

            // Assert
            colors.Header.Should().Be("#FF0000");
            colors.Row.Should().Be("#00FF00");
            colors.AltRow.Should().Be("#0000FF");
            colors.Border.Should().Be("#FFFF00");
        }

        [Fact(DisplayName = "Convert: тема применяется к HTML")]
        public void Convert_ThemeApplied_HtmlContainsThemeColors()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = DataTableToHtmlLogic.Convert(table, theme: Theme.Blue);

            // Assert
            result.Html.Should().Contain("#1565C0"); // Blue header
            result.Html.Should().Contain("#E3F2FD"); // Blue alt row
        }

        // =====================================================================
        // Тесты нумерации строк
        // =====================================================================

        [Fact(DisplayName = "Convert: ShowRowNumbers=true — добавляет колонку №")]
        public void Convert_ShowRowNumbers_AddsNumberColumn()
        {
            // Arrange
            var table = CreateTestTable(3);

            // Act
            var result = DataTableToHtmlLogic.Convert(table, showRowNumbers: true);

            // Assert
            result.Html.Should().Contain(">№<");
            result.Html.Should().Contain(">1<");
            result.Html.Should().Contain(">2<");
            result.Html.Should().Contain(">3<");
        }

        [Fact(DisplayName = "Convert: ShowRowNumbers=false — без колонки №")]
        public void Convert_NoRowNumbers_NoNumberColumn()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = DataTableToHtmlLogic.Convert(table, showRowNumbers: false);

            // Assert
            result.Html.Should().NotContain(">№<");
        }

        // =====================================================================
        // Тесты обработки null-значений
        // =====================================================================

        [Fact(DisplayName = "Convert: null-значения заменяются на nullDisplay")]
        public void Convert_NullValues_ReplacesWithNullDisplay()
        {
            // Arrange
            var table = CreateTableWithNulls();

            // Act
            var result = DataTableToHtmlLogic.Convert(table, nullDisplay: "NULL");

            // Assert
            result.Html.Should().Contain("NULL");
        }

        [Fact(DisplayName = "Convert: пустой nullDisplay — пустая строка")]
        public void Convert_EmptyNullDisplay_EmptyString()
        {
            // Arrange
            var table = CreateTableWithNulls();

            // Act
            var result = DataTableToHtmlLogic.Convert(table, nullDisplay: "");

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        // =====================================================================
        // Тесты ограничения строк
        // =====================================================================

        [Fact(DisplayName = "Convert: maxRows ограничивает количество строк")]
        public void Convert_MaxRows_LimitsRowCount()
        {
            // Arrange
            var table = CreateTestTable(10);

            // Act
            var result = DataTableToHtmlLogic.Convert(table, maxRows: 3);

            // Assert
            result.Html.Should().Contain("User1");
            result.Html.Should().Contain("User2");
            result.Html.Should().Contain("User3");
            result.Html.Should().NotContain("User4");
            result.RowCount.Should().Be(10); // Исходное количество строк
        }

        [Fact(DisplayName = "Convert: maxRows=0 — без ограничений")]
        public void Convert_NoMaxRows_AllRowsIncluded()
        {
            // Arrange
            var table = CreateTestTable(5);

            // Act
            var result = DataTableToHtmlLogic.Convert(table, maxRows: 0);

            // Assert
            result.Html.Should().Contain("User1");
            result.Html.Should().Contain("User5");
        }

        // =====================================================================
        // Тесты экранирования HTML
        // =====================================================================

        [Fact(DisplayName = "EscapeHtml: экранирует угловые скобки")]
        public void EscapeHtml_AngleBrackets_EscapesCorrectly()
        {
            // Act
            string result = DataTableToHtmlLogic.EscapeHtml("<script>");

            // Assert
            result.Should().Be("&lt;script&gt;");
        }

        [Fact(DisplayName = "EscapeHtml: экранирует амперсанд")]
        public void EscapeHtml_Ampersand_EscapesCorrectly()
        {
            // Act
            string result = DataTableToHtmlLogic.EscapeHtml("a & b");

            // Assert
            result.Should().Be("a &amp; b");
        }

        [Fact(DisplayName = "EscapeHtml: экранирует одинарные кавычки")]
        public void EscapeHtml_SingleQuote_EscapesCorrectly()
        {
            // Act
            string result = DataTableToHtmlLogic.EscapeHtml("a 'b' c");

            // Assert
            result.Should().Be("a &apos;b&apos; c");
        }

        [Fact(DisplayName = "EscapeHtml: экранирует двойные кавычки")]
        public void EscapeHtml_DoubleQuote_EscapesCorrectly()
        {
            // Act
            string result = DataTableToHtmlLogic.EscapeHtml("a \"b\" c");

            // Assert - проверяем что кавычки экранированы
            result.Should().Contain("&quot;");
            result.Should().NotContain("\"b\"");
        }

        [Fact(DisplayName = "EscapeHtml: null → пустая строка")]
        public void EscapeHtml_Null_ReturnsEmpty()
        {
            // Act
            string result = DataTableToHtmlLogic.EscapeHtml(null);

            // Assert
            result.Should().Be("");
        }

        [Fact(DisplayName = "EscapeHtml: пустая строка → пустая строка")]
        public void EscapeHtml_Empty_ReturnsEmpty()
        {
            // Act
            string result = DataTableToHtmlLogic.EscapeHtml("");

            // Assert
            result.Should().Be("");
        }

        [Fact(DisplayName = "Convert: HTML-символы в данных экранируются")]
        public void Convert_HtmlInData_EscapesCorrectly()
        {
            // Arrange
            var table = new DataTable();
            table.Columns.Add("Content", typeof(string));
            table.Rows.Add("<script>alert('xss')</script>");

            // Act
            var result = DataTableToHtmlLogic.Convert(table);

            // Assert
            result.Html.Should().NotContain("<script>");
            result.Html.Should().Contain("&lt;script&gt;");
        }

        // =====================================================================
        // Интеграционные тесты
        // =====================================================================

        [Fact(DisplayName = "Convert: реальная таблица с разными типами данных")]
        public void Convert_RealWorldTable_GeneratesCorrectHtml()
        {
            // Arrange
            var table = new DataTable();
            table.Columns.Add("ID", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Date", typeof(DateTime));
            table.Columns.Add("Amount", typeof(decimal));

            table.Rows.Add(1, "Product A", new DateTime(2026, 3, 15), 1234.56m);
            table.Rows.Add(2, "Product B", new DateTime(2026, 3, 16), 789.00m);

            // Act
            var result = DataTableToHtmlLogic.Convert(
                table,
                theme: Theme.Blue,
                outputMode: OutputMode.FullDocument,
                showRowNumbers: true);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Html.Should().Contain("<!DOCTYPE html>");
            result.Html.Should().Contain("Product A");
            result.Html.Should().Contain("Product B");
            result.Html.Should().Contain(">№<");
            result.RowCount.Should().Be(2);
            result.ColumnCount.Should().Be(4);
        }

        [Fact(DisplayName = "Convert: чередование цветов строк")]
        public void Convert_AltRowColors_AlternatesCorrectly()
        {
            // Arrange
            var table = CreateTestTable(4);

            // Act
            var result = DataTableToHtmlLogic.Convert(table, theme: Theme.Blue);

            // Assert
            // Строки 0 и 2 — Row (#FFFFFF)
            // Строки 1 и 3 — AltRow (#E3F2FD)
            result.Html.Should().Contain("#FFFFFF");
            result.Html.Should().Contain("#E3F2FD");
        }
    }
}
