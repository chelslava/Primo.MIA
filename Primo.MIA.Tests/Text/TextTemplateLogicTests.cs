// =============================================================================
// TextTemplateLogicTests.cs — тесты логики шаблонизатора текста.
//
// Покрывает:
//   - Рендеринг шаблонов с различными синтаксисами
//   - Форматирование значений (числа, даты)
//   - Обработка отсутствующих ключей
//   - Регистрозависимый/независимый поиск
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Primo.MIA.Tests.Logic;
using Xunit;

// Алиасы для избежания конфликта имён
using Syntax = Primo.MIA.Tests.Logic.TemplateSyntax;
using MissingBehavior = Primo.MIA.Tests.Logic.MissingKeyBehavior;

namespace Primo.MIA.Tests.Text
{
    /// <summary>Тесты логики шаблонизатора текста.</summary>
    public class TextTemplateLogicTests
    {
        // =====================================================================
        // Тесты базового рендеринга
        // =====================================================================

        [Fact(DisplayName = "Render: простая подстановка одного значения")]
        public void Render_SinglePlaceholder_SubstitutesValue()
        {
            // Arrange
            string template = "Привет, {{Name}}!";
            var variables = new Dictionary<string, string> { { "Name", "Иван" } };

            // Act
            var result = TextTemplateLogic.Render(template, variables);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Result.Should().Be("Привет, Иван!");
            result.ReplacedCount.Should().Be(1);
            result.MissingKeys.Should().BeEmpty();
        }

        [Fact(DisplayName = "Render: подстановка нескольких значений")]
        public void Render_MultiplePlaceholders_SubstitutesAllValues()
        {
            // Arrange
            string template = "{{Greeting}}, {{Name}}! Сегодня {{Day}}.";
            var variables = new Dictionary<string, string>
            {
                { "Greeting", "Привет" },
                { "Name", "Иван" },
                { "Day", "понедельник" }
            };

            // Act
            var result = TextTemplateLogic.Render(template, variables);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Result.Should().Be("Привет, Иван! Сегодня понедельник.");
            result.ReplacedCount.Should().Be(3);
        }

        [Fact(DisplayName = "Render: шаблон без плейсхолдеров")]
        public void Render_NoPlaceholders_ReturnsTemplateAsIs()
        {
            // Arrange
            string template = "Простой текст без плейсхолдеров";
            var variables = new Dictionary<string, string>();

            // Act
            var result = TextTemplateLogic.Render(template, variables);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Result.Should().Be(template);
            result.ReplacedCount.Should().Be(0);
        }

        [Fact(DisplayName = "Render: null шаблон — ошибка")]
        public void Render_NullTemplate_ReturnsError()
        {
            // Arrange
            string template = null;
            var variables = new Dictionary<string, string>();

            // Act
            var result = TextTemplateLogic.Render(template, variables);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Шаблон не указан");
        }

        [Fact(DisplayName = "Render: null словарь — ошибка")]
        public void Render_NullVariables_ReturnsError()
        {
            // Arrange
            string template = "Привет, {{Name}}!";
            Dictionary<string, string> variables = null;

            // Act
            var result = TextTemplateLogic.Render(template, variables);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Словарь переменных не указан");
        }

        // =====================================================================
        // Тесты различных синтаксисов
        // =====================================================================

        [Fact(DisplayName = "Render: синтаксис DoubleBrace — {{ключ}}")]
        public void Render_DoubleBraceSyntax_WorksCorrectly()
        {
            // Arrange
            string template = "Value: {{key}}";
            var variables = new Dictionary<string, string> { { "key", "42" } };

            // Act
            var result = TextTemplateLogic.Render(template, variables, syntax: Syntax.DoubleBrace);

            // Assert
            result.Result.Should().Be("Value: 42");
        }

        [Fact(DisplayName = "Render: синтаксис SingleBrace — {ключ}")]
        public void Render_SingleBraceSyntax_WorksCorrectly()
        {
            // Arrange
            string template = "Value: {key}";
            var variables = new Dictionary<string, string> { { "key", "42" } };

            // Act
            var result = TextTemplateLogic.Render(template, variables, syntax: Syntax.SingleBrace);

            // Assert
            result.Result.Should().Be("Value: 42");
        }

        [Fact(DisplayName = "Render: синтаксис Percent — %ключ%")]
        public void Render_PercentSyntax_WorksCorrectly()
        {
            // Arrange
            string template = "Value: %key%";
            var variables = new Dictionary<string, string> { { "key", "42" } };

            // Act
            var result = TextTemplateLogic.Render(template, variables, syntax: Syntax.Percent);

            // Assert
            result.Result.Should().Be("Value: 42");
        }

        // =====================================================================
        // Тесты форматирования
        // =====================================================================

        [Fact(DisplayName = "Render: форматирование числа N2")]
        public void Render_NumberFormatN2_AppliesCorrectly()
        {
            // Arrange
            string template = "Сумма: {{Amount:N2}} руб.";
            var variables = new Dictionary<string, string> { { "Amount", "14500.5" } };

            // Act
            var result = TextTemplateLogic.Render(template, variables);

            // Assert
            result.IsSuccess.Should().BeTrue();
            // N2 использует CurrentCulture — в русской культуре разделитель запятая
            result.Result.Should().Contain("14");
            result.Result.Should().Contain("50");
        }

        [Fact(DisplayName = "Render: форматирование даты dd.MM.yyyy")]
        public void Render_DateFormat_AppliesCorrectly()
        {
            // Arrange
            string template = "Дата: {{Date:dd.MM.yyyy}}";
            var variables = new Dictionary<string, string> { { "Date", "2026-03-15" } };

            // Act
            var result = TextTemplateLogic.Render(template, variables);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Result.Should().Be("Дата: 15.03.2026");
        }

        [Fact(DisplayName = "Render: форматирование процента P1")]
        public void Render_PercentFormat_AppliesCorrectly()
        {
            // Arrange
            string template = "Процент: {{Rate:P1}}";
            var variables = new Dictionary<string, string> { { "Rate", "0.125" } };

            // Act
            var result = TextTemplateLogic.Render(template, variables);

            // Assert
            result.IsSuccess.Should().BeTrue();
            // P1 форматирует как 12,5 % (в русской культуре)
            result.Result.Should().Contain("12");
        }

        [Fact(DisplayName = "ApplyFormat: число с форматом N2")]
        public void ApplyFormat_NumberN2_ReturnsFormattedValue()
        {
            // Arrange
            string value = "14500.5";
            string format = "N2";

            // Act
            string result = TextTemplateLogic.ApplyFormat(value, format);

            // Assert
            result.Should().Contain("14");
        }

        [Fact(DisplayName = "ApplyFormat: дата с форматом dd.MM.yyyy")]
        public void ApplyFormat_DateFormat_ReturnsFormattedValue()
        {
            // Arrange
            string value = "2026-03-15";
            string format = "dd.MM.yyyy";

            // Act
            string result = TextTemplateLogic.ApplyFormat(value, format);

            // Assert
            result.Should().Be("15.03.2026");
        }

        [Fact(DisplayName = "ApplyFormat: неподдерживаемое значение — возвращается как есть")]
        public void ApplyFormat_UnsupportedValue_ReturnsAsIs()
        {
            // Arrange
            string value = "текст";
            string format = "N2";

            // Act
            string result = TextTemplateLogic.ApplyFormat(value, format);

            // Assert
            result.Should().Be("текст");
        }

        // =====================================================================
        // Тесты обработки отсутствующих ключей
        // =====================================================================

        [Fact(DisplayName = "Render: MissingKeyBehavior.LeaveAsIs — оставить плейсхолдер")]
        public void Render_MissingKeyLeaveAsIs_LeavesPlaceholder()
        {
            // Arrange
            string template = "Привет, {{Name}}!";
            var variables = new Dictionary<string, string>();

            // Act
            var result = TextTemplateLogic.Render(template, variables,
                missingKeyBehavior: MissingBehavior.LeaveAsIs);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Result.Should().Be("Привет, {{Name}}!");
            result.MissingKeys.Should().Contain("Name");
        }

        [Fact(DisplayName = "Render: MissingKeyBehavior.ReplaceWithEmpty — заменить пустой строкой")]
        public void Render_MissingKeyReplaceWithEmpty_ReplacesWithEmpty()
        {
            // Arrange
            string template = "Привет, {{Name}}!";
            var variables = new Dictionary<string, string>();

            // Act
            var result = TextTemplateLogic.Render(template, variables,
                missingKeyBehavior: MissingBehavior.ReplaceWithEmpty);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Result.Should().Be("Привет, !");
            result.MissingKeys.Should().Contain("Name");
        }

        [Fact(DisplayName = "Render: MissingKeyBehavior.ThrowError — ошибка")]
        public void Render_MissingKeyThrowError_ReturnsError()
        {
            // Arrange
            string template = "Привет, {{Name}}!";
            var variables = new Dictionary<string, string>();

            // Act
            var result = TextTemplateLogic.Render(template, variables,
                missingKeyBehavior: MissingBehavior.ThrowError);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Незаполненные ключи");
            result.ErrorMessage.Should().Contain("Name");
        }

        [Fact(DisplayName = "Render: несколько отсутствующих ключей")]
        public void Render_MultipleMissingKeys_TracksAll()
        {
            // Arrange
            string template = "{{A}} {{B}} {{C}}";
            var variables = new Dictionary<string, string> { { "B", "value" } };

            // Act
            var result = TextTemplateLogic.Render(template, variables,
                missingKeyBehavior: MissingBehavior.LeaveAsIs);

            // Assert
            result.MissingKeys.Should().HaveCount(2);
            result.MissingKeys.Should().Contain("A");
            result.MissingKeys.Should().Contain("C");
            result.MissingKeys.Should().NotContain("B");
        }

        // =====================================================================
        // Тесты регистрозависимости
        // =====================================================================

        [Fact(DisplayName = "Render: регистронезависимый поиск (по умолчанию)")]
        public void Render_CaseInsensitive_FindsKey()
        {
            // Arrange
            string template = "Привет, {{NAME}}!";
            var variables = new Dictionary<string, string> { { "name", "Иван" } };

            // Act
            var result = TextTemplateLogic.Render(template, variables, caseSensitive: false);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Result.Should().Be("Привет, Иван!");
        }

        [Fact(DisplayName = "Render: регистрозависимый поиск")]
        public void Render_CaseSensitive_RequiresExactCase()
        {
            // Arrange
            string template = "Привет, {{NAME}}!";
            var variables = new Dictionary<string, string> { { "name", "Иван" } };

            // Act
            var result = TextTemplateLogic.Render(template, variables, caseSensitive: true);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Result.Should().Be("Привет, {{NAME}}!"); // Не заменено
            result.MissingKeys.Should().Contain("NAME");
        }

        // =====================================================================
        // Тесты ParseEncoding
        // =====================================================================

        [Theory(DisplayName = "ParseEncoding: корректные кодировки")]
        [InlineData("UTF-8", "utf-8")]
        [InlineData("UTF-16", "utf-16")]
        [InlineData("Windows-1251", "windows-1251")]
        [InlineData("CP866", "cp866")]
        public void ParseEncoding_ValidEncoding_ReturnsCorrectEncoding(string input, string expectedName)
        {
            // Act
            var encoding = TextTemplateLogic.ParseEncoding(input);

            // Assert
            encoding.WebName.Should().Be(expectedName);
        }

        [Theory(DisplayName = "ParseEncoding: некорректные кодировки — возвращает UTF-8")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("InvalidEncoding")]
        [InlineData(null)]
        public void ParseEncoding_InvalidEncoding_ReturnsUTF8(string input)
        {
            // Act
            var encoding = TextTemplateLogic.ParseEncoding(input);

            // Assert
            encoding.WebName.Should().Be("utf-8");
        }

        // =====================================================================
        // Интеграционные тесты
        // =====================================================================

        [Fact(DisplayName = "Render: реальный пример — email-шаблон")]
        public void Render_RealWorldEmailTemplate_WorksCorrectly()
        {
            // Arrange
            string template = @"
Уважаемый {{Name}}!

Благодарим за заказ №{{OrderNumber}} от {{Date:dd.MM.yyyy}}.
Сумма заказа: {{Amount:N2}} руб.

С уважением,
Команда магазина";
            var variables = new Dictionary<string, string>
            {
                { "Name", "Иван Петров" },
                { "OrderNumber", "АБ-123456" },
                { "Date", "2026-03-15" },
                { "Amount", "14500.50" }
            };

            // Act
            var result = TextTemplateLogic.Render(template, variables);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Result.Should().Contain("Иван Петров");
            result.Result.Should().Contain("АБ-123456");
            result.Result.Should().Contain("15.03.2026");
            result.ReplacedCount.Should().Be(4);
        }

        [Fact(DisplayName = "Render: реальный пример — SQL-запрос")]
        public void Render_RealWorldSQLTemplate_WorksCorrectly()
        {
            // Arrange
            string template = "SELECT * FROM Users WHERE Status = '{{Status}}' AND CreatedAt > '{{Date:yyyy-MM-dd}}'";
            var variables = new Dictionary<string, string>
            {
                { "Status", "active" },
                { "Date", "2026-01-01" }
            };

            // Act
            var result = TextTemplateLogic.Render(template, variables);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Result.Should().Be("SELECT * FROM Users WHERE Status = 'active' AND CreatedAt > '2026-01-01'");
        }

        [Fact(DisplayName = "Render: повторяющиеся плейсхолдеры")]
        public void Render_RepeatedPlaceholders_SubstitutesAll()
        {
            // Arrange
            string template = "{{Name}} {{Name}} {{Name}}";
            var variables = new Dictionary<string, string> { { "Name", "Иван" } };

            // Act
            var result = TextTemplateLogic.Render(template, variables);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Result.Should().Be("Иван Иван Иван");
            result.ReplacedCount.Should().Be(3);
        }

        [Fact(DisplayName = "Render: пустой шаблон")]
        public void Render_EmptyTemplate_ReturnsEmpty()
        {
            // Arrange
            string template = "";
            var variables = new Dictionary<string, string> { { "Key", "Value" } };

            // Act
            var result = TextTemplateLogic.Render(template, variables);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Result.Should().Be("");
            result.ReplacedCount.Should().Be(0);
        }

        [Fact(DisplayName = "Render: пустой словарь — все плейсхолдеры незаполнены")]
        public void Render_EmptyDictionary_AllPlaceholdersMissing()
        {
            // Arrange
            string template = "{{A}} {{B}}";
            var variables = new Dictionary<string, string>();

            // Act
            var result = TextTemplateLogic.Render(template, variables,
                missingKeyBehavior: MissingBehavior.LeaveAsIs);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Result.Should().Be("{{A}} {{B}}");
            result.MissingKeys.Should().HaveCount(2);
        }
    }
}
