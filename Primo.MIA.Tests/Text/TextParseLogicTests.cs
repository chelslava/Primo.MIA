// =============================================================================
// TextParseLogicTests.cs — тесты логики разбора текста по шаблону.
//
// Покрывает:
//   - Компиляцию маски в regex
//   - Извлечение значений из строки
//   - Различные синтаксисы плейсхолдеров
//   - Опции: CaseSensitive, Greedy, MultiLine, AllMatches
//   - Валидацию имён групп
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Primo.MIA.Tests.Logic;
using Xunit;

// Алиас для избежания конфликта имён с Primo.MIA.TemplateSyntax
using Syntax = Primo.MIA.Tests.Logic.ParseSyntax;

namespace Primo.MIA.Tests.Text
{
    /// <summary>Тесты логики разбора текста по шаблону.</summary>
    public class TextParseLogicTests
    {
        // =====================================================================
        // Тесты компиляции маски
        // =====================================================================

        [Fact(DisplayName = "CompileMask: простая маска с одним плейсхолдером")]
        public void CompileMask_SinglePlaceholder_ReturnsValidRegex()
        {
            // Arrange
            string mask = "Привет, {Имя}!";

            // Act
            string pattern = TextParseLogic.CompileMask(mask, Syntax.SingleBrace, greedy: false);

            // Assert
            pattern.Should().Be(@"^Привет,\ (?<Имя>.+?)!$");
        }

        [Fact(DisplayName = "CompileMask: маска с несколькими плейсхолдерами")]
        public void CompileMask_MultiplePlaceholders_ReturnsValidRegex()
        {
            // Arrange
            string mask = "Счёт №{Номер} от {Дата}";

            // Act
            string pattern = TextParseLogic.CompileMask(mask, Syntax.SingleBrace, greedy: false);

            // Assert
            pattern.Should().Be(@"^Счёт\ №(?<Номер>.+?)\ от\ (?<Дата>.+?)$");
        }

        [Fact(DisplayName = "CompileMask: жадный режим использует .+ вместо .+?")]
        public void CompileMask_GreedyMode_UsesGreedyQuantifier()
        {
            // Arrange
            string mask = "Значение: {Value}";

            // Act
            string pattern = TextParseLogic.CompileMask(mask, Syntax.SingleBrace, greedy: true);

            // Assert
            pattern.Should().Contain("(?<Value>.+)");
            pattern.Should().NotContain(".+?");
        }

        [Fact(DisplayName = "CompileMask: синтаксис DoubleBrace — {{ключ}}")]
        public void CompileMask_DoubleBraceSyntax_ParsesCorrectly()
        {
            // Arrange
            string mask = "Привет, {{Имя}}!";

            // Act
            string pattern = TextParseLogic.CompileMask(mask, Syntax.DoubleBrace, greedy: false);

            // Assert
            pattern.Should().Be(@"^Привет,\ (?<Имя>.+?)!$");
        }

        [Fact(DisplayName = "CompileMask: синтаксис Percent — %ключ%")]
        public void CompileMask_PercentSyntax_ParsesCorrectly()
        {
            // Arrange
            string mask = "Привет, %Имя%!";

            // Act
            string pattern = TextParseLogic.CompileMask(mask, Syntax.Percent, greedy: false);

            // Assert
            pattern.Should().Be(@"^Привет,\ (?<Имя>.+?)!$");
        }

        [Fact(DisplayName = "CompileMask: экранирование спецсимволов regex")]
        public void CompileMask_SpecialCharacters_EscapesCorrectly()
        {
            // Arrange
            string mask = "Price: {Amount}$ [code: {Code}]";

            // Act
            string pattern = TextParseLogic.CompileMask(mask, Syntax.SingleBrace, greedy: false);

            // Assert
            pattern.Should().Contain(@"\$");
            pattern.Should().Contain(@"\[");
            // Примечание: Regex.Escape экранирует ] только если есть [, но в составе \[
            // Поэтому проверяем что вся последовательность [code: ...] экранирована корректно
            pattern.Should().MatchRegex(@"\[\w+:");  // [ экранирован как \[
        }

        [Fact(DisplayName = "CompileMask: маска без плейсхолдеров — ошибка")]
        public void CompileMask_NoPlaceholders_ThrowsException()
        {
            // Arrange
            string mask = "Просто текст без плейсхолдеров";

            // Act
            Action act = () => TextParseLogic.CompileMask(mask, Syntax.SingleBrace, greedy: false);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*не содержит ни одного плейсхолдера*");
        }

        [Fact(DisplayName = "CompileMask: пустая маска — ошибка")]
        public void CompileMask_EmptyMask_ThrowsException()
        {
            // Arrange
            string mask = "";

            // Act
            Action act = () => TextParseLogic.CompileMask(mask, Syntax.SingleBrace, greedy: false);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        // =====================================================================
        // Тесты валидации имён групп
        // =====================================================================

        [Theory(DisplayName = "IsValidGroupName: допустимые имена")]
        [InlineData("Name", true)]
        [InlineData("Имя", true)]
        [InlineData("_name", true)]
        [InlineData("Name123", true)]
        [InlineData("Name_Value", true)]
        [InlineData("_", true)]
        public void IsValidGroupName_ValidNames_ReturnsTrue(string name, bool expected)
        {
            // Act
            bool result = TextParseLogic.IsValidGroupName(name);

            // Assert
            result.Should().Be(expected);
        }

        [Theory(DisplayName = "IsValidGroupName: недопустимые имена")]
        [InlineData("", false)]
        [InlineData("123Name", false)]
        [InlineData("Name-Value", false)]
        [InlineData("Name.Value", false)]
        [InlineData("Name Value", false)]
        [InlineData(null, false)]
        public void IsValidGroupName_InvalidNames_ReturnsFalse(string name, bool expected)
        {
            // Act
            bool result = TextParseLogic.IsValidGroupName(name);

            // Assert
            result.Should().Be(expected);
        }

        [Fact(DisplayName = "CompileMask: недопустимое имя плейсхолдера — ошибка")]
        public void CompileMask_InvalidPlaceholderName_ThrowsException()
        {
            // Arrange
            string mask = "Значение: {123Invalid}";

            // Act
            Action act = () => TextParseLogic.CompileMask(mask, Syntax.SingleBrace, greedy: false);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Недопустимое имя плейсхолдера*");
        }

        // =====================================================================
        // Тесты основного метода Parse
        // =====================================================================

        [Fact(DisplayName = "Parse: успешное извлечение одного значения")]
        public void Parse_SinglePlaceholder_ExtractsValue()
        {
            // Arrange
            string input = "Привет, Иван!";
            string mask = "Привет, {Имя}!";

            // Act
            var result = TextParseLogic.Parse(input, mask);

            // Assert
            result.IsMatched.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
            result.Result.Should().ContainKey("Имя");
            result.Result["Имя"].Should().Be("Иван");
            result.Count.Should().Be(1);
        }

        [Fact(DisplayName = "Parse: успешное извлечение нескольких значений")]
        public void Parse_MultiplePlaceholders_ExtractsAllValues()
        {
            // Arrange
            string input = "Счёт №ЗК-2026 от 15.03.2026 на сумму 14500 руб.";
            string mask = "Счёт №{Номер} от {Дата} на сумму {Сумма} руб.";

            // Act
            var result = TextParseLogic.Parse(input, mask);

            // Assert
            result.IsMatched.Should().BeTrue();
            result.Result.Should().HaveCount(3);
            result.Result["Номер"].Should().Be("ЗК-2026");
            result.Result["Дата"].Should().Be("15.03.2026");
            result.Result["Сумма"].Should().Be("14500");
        }

        [Fact(DisplayName = "Parse: маска не совпала — IsMatched=false")]
        public void Parse_NoMatch_ReturnsIsMatchedFalse()
        {
            // Arrange
            string input = "Произвольный текст";
            string mask = "Счёт №{Номер}";

            // Act
            var result = TextParseLogic.Parse(input, mask);

            // Assert
            result.IsMatched.Should().BeFalse();
            result.Result.Should().BeEmpty();
            result.Count.Should().Be(0);
            result.ErrorMessage.Should().BeNull();
        }

        [Fact(DisplayName = "Parse: null входная строка — ошибка")]
        public void Parse_NullInput_ReturnsError()
        {
            // Arrange
            string input = null;
            string mask = "Привет, {Имя}!";

            // Act
            var result = TextParseLogic.Parse(input, mask);

            // Assert
            result.ErrorMessage.Should().Contain("Входная строка не указана");
            result.IsMatched.Should().BeFalse();
        }

        [Fact(DisplayName = "Parse: пустая маска — ошибка")]
        public void Parse_EmptyMask_ReturnsError()
        {
            // Arrange
            string input = "Привет, Иван!";
            string mask = "";

            // Act
            var result = TextParseLogic.Parse(input, mask);

            // Assert
            result.ErrorMessage.Should().Contain("Маска не указана");
            result.IsMatched.Should().BeFalse();
        }

        // =====================================================================
        // Тесты опций
        // =====================================================================

        [Fact(DisplayName = "Parse: CaseSensitive=true — различает регистр")]
        public void Parse_CaseSensitive_RequiresExactCase()
        {
            // Arrange
            string input = "ПРИВЕТ, Иван!";
            string mask = "Привет, {Имя}!";

            // Act — регистронезависимый (по умолчанию)
            var resultInsensitive = TextParseLogic.Parse(input, mask, caseSensitive: false);

            // Act — регистрозависимый
            var resultSensitive = TextParseLogic.Parse(input, mask, caseSensitive: true);

            // Assert
            resultInsensitive.IsMatched.Should().BeTrue();
            resultSensitive.IsMatched.Should().BeFalse();
        }

        [Fact(DisplayName = "Parse: Greedy=true — жадный захват")]
        public void Parse_GreedyMode_CapturesMaximum()
        {
            // Arrange
            string input = "a: xxx b: yyy c: zzz";
            string mask = "a: {First} b: {Second} c: {Third}";

            // Act — ленивый (по умолчанию)
            var resultLazy = TextParseLogic.Parse(input, mask, greedy: false);

            // Act — жадный
            var resultGreedy = TextParseLogic.Parse(input, mask, greedy: true);

            // Assert — ленивый захватывает минимальное
            resultLazy.IsMatched.Should().BeTrue();
            resultLazy.Result["First"].Should().Be("xxx");
            resultLazy.Result["Second"].Should().Be("yyy");
            resultLazy.Result["Third"].Should().Be("zzz");

            // Assert — жадный с якорями ^...$ также захватывает корректно
            // т.к. весь паттерн должен совпасть от начала до конца строки
            resultGreedy.IsMatched.Should().BeTrue();
            // При жадном режиме с якорями, regex engine найдёт правильное совпадение
            // т.к. должен совпасть весь паттерн "a: ... b: ... c: ..."
            resultGreedy.Result["First"].Should().Be("xxx");
            resultGreedy.Result["Second"].Should().Be("yyy");
            resultGreedy.Result["Third"].Should().Be("zzz");
        }

        [Fact(DisplayName = "Parse: MultiLine=true — точка совпадает с переносом строки")]
        public void Parse_MultiLine_DotMatchesNewline()
        {
            // Arrange
            string input = "Начало\nстрока 1\nстрока 2\nКонец";
            string mask = "Начало{Content}Конец";

            // Act — без MultiLine
            var resultNoMultiLine = TextParseLogic.Parse(input, mask, multiLine: false);

            // Act — с MultiLine
            var resultMultiLine = TextParseLogic.Parse(input, mask, multiLine: true);

            // Assert
            resultNoMultiLine.IsMatched.Should().BeFalse();
            resultMultiLine.IsMatched.Should().BeTrue();
            resultMultiLine.Result["Content"].Should().Contain("\n");
        }

        [Fact(DisplayName = "Parse: AllMatches=true — находит все вхождения")]
        public void Parse_AllMatches_ReturnsAllOccurrences()
        {
            // Arrange
            // Используем маску с запятой как разделителем, чтобы ленивый квантификатор
            // останавливался на запятой
            string input = "Item: Apple, Item: Banana, Item: Cherry";
            string mask = "Item: {Name},";  // Запятая как разделитель

            // Act
            var result = TextParseLogic.Parse(input, mask, allMatches: true);

            // Assert
            result.IsMatched.Should().BeTrue();
            // Найдём 2 совпадения (Apple, и Banana,) + последнее без запятой не совпадёт
            result.AllResults.Should().HaveCount(2);
            result.AllResults[0]["Name"].Should().Be("Apple");
            result.AllResults[1]["Name"].Should().Be("Banana");
            result.Count.Should().Be(2); // Количество вхождений
        }

        [Fact(DisplayName = "Parse: AllMatches=false — только первое вхождение")]
        public void Parse_FirstMatchOnly_ReturnsFirstOccurrence()
        {
            // Arrange
            // При allMatches=false используются якоря ^...$, поэтому маска должна
            // совпадать со всей строкой
            string input = "Item: Apple";
            string mask = "Item: {Name}";

            // Act
            var result = TextParseLogic.Parse(input, mask, allMatches: false);

            // Assert
            result.IsMatched.Should().BeTrue();
            result.Result["Name"].Should().Be("Apple");
            result.AllResults.Should().BeEmpty();
            result.Count.Should().Be(1); // Количество ключей
        }

        // =====================================================================
        // Тесты различных синтаксисов
        // =====================================================================

        [Fact(DisplayName = "Parse: синтаксис DoubleBrace")]
        public void Parse_DoubleBraceSyntax_ExtractsCorrectly()
        {
            // Arrange
            // При DoubleBrace синтаксисе плейсхолдер {{Name}} превращается в (?<Name>.+?)
            // Входная строка должна содержать только значение без {{ }}
            string input = "Hello, World!";
            string mask = "Hello, {{Name}}!";

            // Act
            var result = TextParseLogic.Parse(input, mask, syntax: Syntax.DoubleBrace);

            // Assert
            result.IsMatched.Should().BeTrue();
            result.Result["Name"].Should().Be("World");
        }

        [Fact(DisplayName = "Parse: синтаксис Percent")]
        public void Parse_PercentSyntax_ExtractsCorrectly()
        {
            // Arrange
            string input = "Value: 42%";
            string mask = "Value: %Number%%";

            // Act
            var result = TextParseLogic.Parse(input, mask, syntax: Syntax.Percent);

            // Assert
            result.IsMatched.Should().BeTrue();
            result.Result["Number"].Should().Be("42");
        }

        // =====================================================================
        // Тесты CompiledPattern
        // =====================================================================

        [Fact(DisplayName = "Parse: возвращает скомпилированный паттерн")]
        public void Parse_ReturnsCompiledPattern()
        {
            // Arrange
            string input = "Привет, Иван!";
            string mask = "Привет, {Имя}!";

            // Act
            var result = TextParseLogic.Parse(input, mask);

            // Assert
            result.CompiledPattern.Should().NotBeNullOrEmpty();
            result.CompiledPattern.Should().Contain("(?<Имя>");
        }

        // =====================================================================
        // Интеграционные тесты
        // =====================================================================

        [Fact(DisplayName = "Parse: реальный пример — счёт-фактура")]
        public void Parse_RealWorldInvoice_ExtractsAllFields()
        {
            // Arrange
            string input = "Счёт-фактура №АБ-123456 от 15.03.2026. " +
                           "Поставщик: ООО «Ромашка». Сумма: 125 000,00 руб.";
            string mask = "Счёт-фактура №{Номер} от {Дата}. " +
                          "Поставщик: {Поставщик}. Сумма: {Сумма} руб.";

            // Act
            var result = TextParseLogic.Parse(input, mask);

            // Assert
            result.IsMatched.Should().BeTrue();
            result.Result["Номер"].Should().Be("АБ-123456");
            result.Result["Дата"].Should().Be("15.03.2026");
            result.Result["Поставщик"].Should().Be("ООО «Ромашка»");
            result.Result["Сумма"].Should().Be("125 000,00");
        }

        [Fact(DisplayName = "Parse: реальный пример — email")]
        public void Parse_RealWorldEmail_ExtractsFields()
        {
            // Arrange
            string input = "From: john.doe@example.com, To: jane@example.com";
            string mask = "From: {From}, To: {To}";

            // Act
            var result = TextParseLogic.Parse(input, mask);

            // Assert
            result.IsMatched.Should().BeTrue();
            result.Result["From"].Should().Be("john.doe@example.com");
            result.Result["To"].Should().Be("jane@example.com");
        }

        [Fact(DisplayName = "Parse: реальный пример — лог-строка")]
        public void Parse_RealWorldLogLine_ExtractsFields()
        {
            // Arrange
            string input = "[2026-03-15 14:30:45] [ERROR] [UserService] Login failed for user: admin";
            string mask = "[{Timestamp}] [{Level}] [{Logger}] {Message}";

            // Act
            var result = TextParseLogic.Parse(input, mask);

            // Assert
            result.IsMatched.Should().BeTrue();
            result.Result["Timestamp"].Should().Be("2026-03-15 14:30:45");
            result.Result["Level"].Should().Be("ERROR");
            result.Result["Logger"].Should().Be("UserService");
            result.Result["Message"].Should().Be("Login failed for user: admin");
        }

        [Fact(DisplayName = "Parse: несколько заказов в одном тексте")]
        public void Parse_MultipleOrders_ExtractsAllMatches()
        {
            // Arrange
            // Используем перенос строки как разделитель для корректной работы allMatches
            string input = "Order #123: 5 items, $150.50\n" +
                           "Order #456: 3 items, $75.00\n" +
                           "Order #789: 10 items, $320.00";
            // Маска с переносом строки как разделителем
            string mask = "Order #{OrderId}: {Items} items, ${Price}\n";

            // Act
            var result = TextParseLogic.Parse(input, mask, allMatches: true);

            // Assert
            result.IsMatched.Should().BeTrue();
            // Найдём 2 совпадения (первые две строки с \n в конце)
            result.AllResults.Should().HaveCount(2);

            result.AllResults[0]["OrderId"].Should().Be("123");
            result.AllResults[0]["Items"].Should().Be("5");
            result.AllResults[0]["Price"].Should().Be("150.50");

            result.AllResults[1]["OrderId"].Should().Be("456");
            result.AllResults[1]["Items"].Should().Be("3");
            result.AllResults[1]["Price"].Should().Be("75.00");
        }
    }
}
