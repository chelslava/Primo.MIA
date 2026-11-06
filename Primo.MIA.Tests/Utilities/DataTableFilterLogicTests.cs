// =============================================================================
// DataTableFilterLogicTests.cs — тесты логики фильтрации DataTable.
//
// Покрывает:
//   - Базовую фильтрацию и схему результата
//   - СОХРАНЕНИЕ ПОРЯДКА СТРОК (ключевое требование)
//   - SQL-операторы: =, <>, >, <, >=, <=, LIKE, IN, IS NULL / IS NOT NULL
//   - Логику: AND / OR / NOT, скобки, приоритеты, русские И / ИЛИ / НЕ
//   - Типы колонок: int, double, DateTime, bool, string
//   - Регистрозависимость
//   - Режим конструктора условий + DSL (сериализация/парсинг/экранирование)
//   - Обработку ошибок (синтаксис, неизвестная колонка, пустые входы)
// =============================================================================

using System;
using System.Data;
using System.Linq;
using FluentAssertions;
using Primo.MIA;
using Xunit;

using Op = Primo.MIA.DataTableConditionOperator;
using Join = Primo.MIA.DataTableConditionJoin;

namespace Primo.MIA.Tests.Utilities
{
    /// <summary>Тесты логики фильтрации DataTable.</summary>
    public class DataTableFilterLogicTests
    {
        // =====================================================================
        // Вспомогательные методы
        // =====================================================================

        /// <summary>Создаёт тестовую таблицу: Name, Age, City.</summary>
        private static DataTable CreateTestTable()
        {
            var table = new DataTable("Employees");
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Age", typeof(int));
            table.Columns.Add("City", typeof(string));

            table.Rows.Add("Иван", 20, "Москва");
            table.Rows.Add("Ирина", 25, "Казань");
            table.Rows.Add("Пётр", 30, "Москва");
            table.Rows.Add("Мария", 35, "Berlin");
            table.Rows.Add("Иван", 40, "Сочи");

            return table;
        }

        /// <summary>Создаёт таблицу с разными типами: Date, Active, Salary.</summary>
        private static DataTable CreateMixedTypesTable()
        {
            var table = new DataTable();
            table.Columns.Add("Date", typeof(DateTime));
            table.Columns.Add("Active", typeof(bool));
            table.Columns.Add("Salary", typeof(double));

            table.Rows.Add(new DateTime(2024, 1, 15), true, 50000.5);
            table.Rows.Add(new DateTime(2024, 3, 15), false, 75000.0);
            table.Rows.Add(new DateTime(2024, 6, 1), true, -1200.25);

            return table;
        }

        /// <summary>Создаёт таблицу с пустыми значениями (DBNull).</summary>
        private static DataTable CreateTableWithNulls()
        {
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Score", typeof(int));

            table.Rows.Add("Alice", 10);
            table.Rows.Add("Bob", DBNull.Value);
            table.Rows.Add(null, 20);

            return table;
        }

        private static readonly DataTableFilterLogic Logic = new DataTableFilterLogic();

        // =====================================================================
        // Базовая фильтрация
        // =====================================================================

        [Fact(DisplayName = "Filter: простое условие → корректные счётчики")]
        public void Filter_SimpleExpression_ReturnsCounts()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "Age >= 30");

            // Assert
            result.FilteredCount.Should().Be(3);
            result.RejectedCount.Should().Be(2);
            result.Filtered.Rows.Count.Should().Be(3);
            result.Rejected.Rows.Count.Should().Be(2);
        }

        [Fact(DisplayName = "Filter: схема результата — клон исходной таблицы")]
        public void Filter_ResultKeepsSchema()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "Age > 100");

            // Assert — пустой результат сохраняет схему и имя таблицы
            result.Filtered.Rows.Count.Should().Be(0);
            result.Filtered.TableName.Should().Be(table.TableName);
            result.Filtered.Columns.Count.Should().Be(3);
            result.Filtered.Columns["Age"].DataType.Should().Be(typeof(int));
        }

        [Fact(DisplayName = "Filter: исходная таблица не изменяется")]
        public void Filter_DoesNotModifySource()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            Logic.Filter(table, "Age > 25");

            // Assert
            table.Rows.Count.Should().Be(5);
            table.Select().Sum(r => (int)r["Age"]).Should().Be(150);
        }

        // =====================================================================
        // Сохранение порядка строк (ключевое требование)
        // =====================================================================

        [Fact(DisplayName = "Filter: порядок строк сохраняется")]
        public void Filter_PreservesRowOrder()
        {
            // Arrange — фильтр «прореживает» таблицу: проходят строки 2, 3, 5
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "Age >= 25");

            // Assert — прошедшие строки в исходном порядке: Ирина, Пётр, Мария... 
            // (строки с возрастами 25, 30, 35, 40)
            var ages = result.Filtered.Rows.Cast<DataRow>().Select(r => (int)r["Age"]).ToList();
            ages.Should().BeInAscendingOrder().And.Equal(25, 30, 35, 40);
        }

        [Fact(DisplayName = "Filter: большой фильтр не меняет относительный порядок")]
        public void Filter_LargeTableKeepsRelativeOrder()
        {
            // Arrange — 100 строк, проходит каждая третья
            var table = new DataTable();
            table.Columns.Add("Num", typeof(int));
            for (int i = 1; i <= 100; i++)
                table.Rows.Add(i);

            // Act — строим выражение Num = 3 OR Num = 6 OR ... OR Num = 99
            var passes = Enumerable.Range(1, 100).Where(n => n % 3 == 0).ToList();
            string expression = "Num = " + string.Join(" OR Num = ", passes);
            var result = Logic.Filter(table, expression);

            // Assert
            var nums = result.Filtered.Rows.Cast<DataRow>().Select(r => (int)r["Num"]).ToList();
            nums.Should().Equal(passes);
        }

        [Fact(DisplayName = "Filter: отсеянные строки идут в исходном порядке")]
        public void Filter_RejectedKeepsOriginalOrder()
        {
            // Arrange
            var table = new DataTable();
            table.Columns.Add("Num", typeof(int));
            for (int i = 1; i <= 10; i++)
                table.Rows.Add(i);

            // Act — проходят только чётные
            var result = Logic.Filter(table, "Num = 2 OR Num = 4 OR Num = 6 OR Num = 8 OR Num = 10");

            // Assert — rejected: 1, 3, 5, 7, 9 в исходном порядке
            var rejected = result.Rejected.Rows.Cast<DataRow>().Select(r => (int)r["Num"]).ToList();
            rejected.Should().Equal(1, 3, 5, 7, 9);
            rejected.Should().OnlyHaveUniqueItems();
        }

        // =====================================================================
        // SQL-операторы сравнения
        // =====================================================================

        [Theory(DisplayName = "Filter: операторы сравнения чисел")]
        [InlineData("Age = 30", 1)]
        [InlineData("Age == 30", 1)]
        [InlineData("Age <> 30", 4)]
        [InlineData("Age != 30", 4)]
        [InlineData("Age > 30", 2)]
        [InlineData("Age >= 30", 3)]
        [InlineData("Age < 25", 1)]
        [InlineData("Age <= 25", 2)]
        public void Filter_ComparisonOperators(string expression, int expected)
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, expression);

            // Assert
            result.FilteredCount.Should().Be(expected);
        }

        [Fact(DisplayName = "Filter: сравнение строк (регистронезависимо по умолчанию)")]
        public void Filter_StringComparison_CaseInsensitiveByDefault()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "City = 'москва'");

            // Assert
            result.FilteredCount.Should().Be(2);
        }

        [Fact(DisplayName = "Filter: сравнение строк с учётом регистра")]
        public void Filter_StringComparison_CaseSensitive()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "City = 'москва'", caseSensitive: true);

            // Assert
            result.FilteredCount.Should().Be(0);
        }

        [Fact(DisplayName = "Filter: отрицательные числа")]
        public void Filter_NegativeNumbers()
        {
            // Arrange
            var table = CreateMixedTypesTable();

            // Act
            var result = Logic.Filter(table, "Salary < 0");

            // Assert
            result.FilteredCount.Should().Be(1);
            result.Filtered.Rows.Cast<DataRow>().First()["Salary"].Should().Be(-1200.25);
        }

        [Fact(DisplayName = "Filter: сравнение DateTime")]
        public void Filter_DateTimeComparison()
        {
            // Arrange
            var table = CreateMixedTypesTable();

            // Act
            var result = Logic.Filter(table, "Date > '2024-02-01'");

            // Assert
            result.FilteredCount.Should().Be(2);
        }

        [Fact(DisplayName = "Filter: сравнение bool")]
        public void Filter_BoolComparison()
        {
            // Arrange
            var table = CreateMixedTypesTable();

            // Act
            var result = Logic.Filter(table, "Active = true");

            // Assert
            result.FilteredCount.Should().Be(2);
        }

        [Fact(DisplayName = "Filter: дробные числа")]
        public void Filter_DecimalNumbers()
        {
            // Arrange
            var table = CreateMixedTypesTable();

            // Act
            var result = Logic.Filter(table, "Salary >= 50000.5 AND Salary < 70000");

            // Assert
            result.FilteredCount.Should().Be(1);
        }

        // =====================================================================
        // Логические операторы
        // =====================================================================

        [Fact(DisplayName = "Filter: AND объединяет условия")]
        public void Filter_AndOperator()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "Age > 20 AND City = 'Москва'");

            // Assert — только Пётр (30, Москва)
            result.FilteredCount.Should().Be(1);
            result.Filtered.Rows.Cast<DataRow>().First()["Name"].Should().Be("Пётр");
        }

        [Fact(DisplayName = "Filter: OR расширяет выборку")]
        public void Filter_OrOperator()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "City = 'Казань' OR City = 'Сочи'");

            // Assert
            result.FilteredCount.Should().Be(2);
        }

        [Fact(DisplayName = "Filter: NOT инвертирует условие")]
        public void Filter_NotOperator()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "NOT (City = 'Москва')");

            // Assert
            result.FilteredCount.Should().Be(3);
        }

        [Fact(DisplayName = "Filter: AND имеет приоритет над OR")]
        public void Filter_AndBindsTighterThanOr()
        {
            // Arrange
            var table = CreateTestTable();

            // Act — (Age = 20) OR (Age = 25 AND City = 'Berlin') → только Иван-20
            var result = Logic.Filter(table, "Age = 20 OR Age = 25 AND City = 'Berlin'");

            // Assert
            result.FilteredCount.Should().Be(1);
            result.Filtered.Rows.Cast<DataRow>().First()["Age"].Should().Be(20);
        }

        [Fact(DisplayName = "Filter: скобки меняют приоритет")]
        public void Filter_ParenthesesChangePriority()
        {
            // Arrange
            var table = CreateTestTable();

            // Act — (Age = 20 OR Age = 25) AND City = 'Berlin' → пусто
            var result = Logic.Filter(table, "(Age = 20 OR Age = 25) AND City = 'Berlin'");

            // Assert
            result.FilteredCount.Should().Be(0);
        }

        [Fact(DisplayName = "Filter: русские ключевые слова И / ИЛИ / НЕ")]
        public void Filter_RussianKeywords()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "НЕ (City = 'Москва') И Age > 25 ИЛИ City = 'Казань'");

            // Assert — (НЕ Москва И Age > 25) ИЛИ Казань: Мария(35), Иван(40), Ирина(25)
            result.FilteredCount.Should().Be(3);
        }

        [Fact(DisplayName = "Filter: двойное NOT")]
        public void Filter_DoubleNot()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "NOT NOT (Age = 30)");

            // Assert
            result.FilteredCount.Should().Be(1);
        }

        // =====================================================================
        // LIKE, IN, IS NULL
        // =====================================================================

        [Fact(DisplayName = "Filter: LIKE с % — любое число символов")]
        public void Filter_LikePercent()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "Name LIKE 'Ив%'");

            // Assert — Иван и Иван (две строки)
            result.FilteredCount.Should().Be(2);
        }

        [Fact(DisplayName = "Filter: LIKE с _ — ровно один символ")]
        public void Filter_LikeUnderscore()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "Name LIKE 'И___'");

            // Assert — Иван и Ирина (4 буквы на И)
            result.FilteredCount.Should().Be(2);
        }

        [Fact(DisplayName = "Filter: LIKE по несуществующей колонке → ошибка")]
        public void Filter_LikeUnknownColumn_Throws()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            Action act = () => Logic.Filter(table, "Email LIKE '%example%'");

            // Assert
            act.Should().Throw<UtilityException>().WithMessage("*колонка*");
        }

        [Fact(DisplayName = "Filter: IN со строками")]
        public void Filter_InWithStrings()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "City IN ('Москва', 'Сочи')");

            // Assert
            result.FilteredCount.Should().Be(3);
        }

        [Fact(DisplayName = "Filter: IN с числами")]
        public void Filter_InWithNumbers()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "Age IN (20, 30, 40)");

            // Assert
            result.FilteredCount.Should().Be(3);
        }

        [Fact(DisplayName = "Filter: IS NULL находит пустые ячейки")]
        public void Filter_IsNull()
        {
            // Arrange
            var table = CreateTableWithNulls();

            // Act
            var result = Logic.Filter(table, "Name IS NULL");

            // Assert
            result.FilteredCount.Should().Be(1);
        }

        [Fact(DisplayName = "Filter: IS NOT NULL исключает пустые ячейки")]
        public void Filter_IsNotNull()
        {
            // Arrange
            var table = CreateTableWithNulls();

            // Act
            var result = Logic.Filter(table, "Name IS NOT NULL AND Score IS NOT NULL");

            // Assert — только Alice (Bob: Score=null, третья: Name=null)
            result.FilteredCount.Should().Be(1);
            result.Filtered.Rows.Cast<DataRow>().First()["Name"].Should().Be("Alice");
        }

        [Fact(DisplayName = "Filter: сравнение с пустой ячейкой → false")]
        public void Filter_NullCellComparison_ReturnsFalse()
        {
            // Arrange
            var table = CreateTableWithNulls();

            // Act — Bob имеет Score = DBNull; строгие сравнения его не пропускают
            var result = Logic.Filter(table, "Score >= 0");

            // Assert — Alice (10) и третья (20); Bob отсеян
            result.FilteredCount.Should().Be(2);
        }

        [Fact(DisplayName = "Filter: [скобки] для имён с пробелами и ключевых слов")]
        public void Filter_BracketedColumnNames()
        {
            // Arrange
            var table = new DataTable();
            table.Columns.Add("Сумма заказа", typeof(int));
            table.Columns.Add("И", typeof(string));
            table.Rows.Add(100, "x");
            table.Rows.Add(200, "y");

            // Act — «И» — ключевое слово, требуется [скобки]
            var result = Logic.Filter(table, "[Сумма заказа] >= 150 AND [И] = 'y'");

            // Assert
            result.FilteredCount.Should().Be(1);
        }

        [Fact(DisplayName = "Filter: строковые литералы в двойных кавычках и экранирование")]
        public void Filter_QuotedLiterals()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table, "City = \"Berlin\"");

            // Assert
            result.FilteredCount.Should().Be(1);
        }

        // =====================================================================
        // Режим конструктора условий
        // =====================================================================

        [Fact(DisplayName = "Filter(conditions): одиночное условие Equal")]
        public void Conditions_SingleEqual()
        {
            // Arrange
            var table = CreateTestTable();
            var conditions = new[]
            {
                new DataTableFilterCondition { Column = "City", Operator = Op.Equal, Value = "Москва" }
            };

            // Act
            var result = Logic.Filter(table, conditions);

            // Assert
            result.FilteredCount.Should().Be(2);
        }

        [Fact(DisplayName = "Filter(conditions): AND и OR связки")]
        public void Conditions_AndOrJoins()
        {
            // Arrange
            var table = CreateTestTable();
            var conditions = new[]
            {
                new DataTableFilterCondition { Join = Join.And, Column = "City", Operator = Op.Equal, Value = "Москва" },
                new DataTableFilterCondition { Join = Join.Or,  Column = "City", Operator = Op.Equal, Value = "Казань" }
            };

            // Act — (Москва) OR (Казань)
            var result = Logic.Filter(table, conditions);

            // Assert
            result.FilteredCount.Should().Be(3);
        }

        [Fact(DisplayName = "Filter(conditions): все операторы строк")]
        public void Conditions_StringOperators()
        {
            // Arrange
            var table = CreateTestTable();

            // Act + Assert
            Logic.Filter(table, new[] { new DataTableFilterCondition { Column = "Name", Operator = Op.Contains, Value = "ан" } })
                .FilteredCount.Should().Be(2); // Иван, Иван

            Logic.Filter(table, new[] { new DataTableFilterCondition { Column = "Name", Operator = Op.StartsWith, Value = "Ив" } })
                .FilteredCount.Should().Be(2);

            Logic.Filter(table, new[] { new DataTableFilterCondition { Column = "Name", Operator = Op.EndsWith, Value = "ия" } })
                .FilteredCount.Should().Be(1); // Мария

            Logic.Filter(table, new[] { new DataTableFilterCondition { Column = "Name", Operator = Op.Like, Value = "Ир___" } })
                .FilteredCount.Should().Be(1); // Ирина

            Logic.Filter(table, new[] { new DataTableFilterCondition { Column = "City", Operator = Op.In, Value = "Казань, Сочи" } })
                .FilteredCount.Should().Be(2);
        }

        [Fact(DisplayName = "Filter(conditions): IsNull / IsNotNull")]
        public void Conditions_NullOperators()
        {
            // Arrange
            var table = CreateTableWithNulls();

            // Act + Assert
            Logic.Filter(table, new[] { new DataTableFilterCondition { Column = "Name", Operator = Op.IsNull } })
                .FilteredCount.Should().Be(1);

            Logic.Filter(table, new[] { new DataTableFilterCondition { Column = "Name", Operator = Op.IsNotNull } })
                .FilteredCount.Should().Be(2);
        }

        [Fact(DisplayName = "Filter(conditions): число с приведением типа")]
        public void Conditions_NumericCoercion()
        {
            // Arrange — значение приходит строкой из формы
            var table = CreateTestTable();
            var conditions = new[]
            {
                new DataTableFilterCondition { Column = "Age", Operator = Op.GreaterOrEqual, Value = "30" }
            };

            // Act
            var result = Logic.Filter(table, conditions);

            // Assert
            result.FilteredCount.Should().Be(3);
        }

        [Fact(DisplayName = "Filter(conditions): несовместимое значение → строка не проходит")]
        public void Conditions_IncompatibleValue_Excluded()
        {
            // Arrange
            var table = CreateTestTable();
            var conditions = new[]
            {
                new DataTableFilterCondition { Column = "Age", Operator = Op.Equal, Value = "не число" }
            };

            // Act
            var result = Logic.Filter(table, conditions);

            // Assert
            result.FilteredCount.Should().Be(0);
            result.RejectedCount.Should().Be(5);
        }

        [Fact(DisplayName = "Filter(conditions): пустой список → ошибка")]
        public void Conditions_EmptyList_Throws()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            Action act = () => Logic.Filter(table, new DataTableFilterCondition[0]);

            // Assert
            act.Should().Throw<UtilityException>();
        }

        [Fact(DisplayName = "Filter(conditions): неизвестная колонка → ошибка")]
        public void Conditions_UnknownColumn_Throws()
        {
            // Arrange
            var table = CreateTestTable();
            var conditions = new[]
            {
                new DataTableFilterCondition { Column = "Отдел", Operator = Op.Equal, Value = "ИТ" }
            };

            // Act
            Action act = () => Logic.Filter(table, conditions);

            // Assert
            act.Should().Throw<UtilityException>().WithMessage("*Отдел*");
        }

        // =====================================================================
        // DSL конструктора условий
        // =====================================================================

        [Fact(DisplayName = "DSL: сериализация и парсинг — обратная симметрия")]
        public void Dsl_RoundTrip()
        {
            // Arrange
            var conditions = new[]
            {
                new DataTableFilterCondition { Join = Join.And, Column = "Age", Operator = Op.GreaterOrEqual, Value = "18" },
                new DataTableFilterCondition { Join = Join.Or,  Column = "Name", Operator = Op.Contains, Value = "Иван" }
            };

            // Act
            var dsl = DataTableFilterLogic.ToConditionsDsl(conditions);
            var parsed = DataTableFilterLogic.ParseConditionsDsl(dsl);

            // Assert
            parsed.Should().HaveCount(2);
            parsed[0].Column.Should().Be("Age");
            parsed[0].Operator.Should().Be(Op.GreaterOrEqual);
            parsed[0].Value.Should().Be("18");
            parsed[1].Join.Should().Be(Join.Or);
            parsed[1].Column.Should().Be("Name");
            parsed[1].Operator.Should().Be(Op.Contains);
            parsed[1].Value.Should().Be("Иван");
        }

        [Fact(DisplayName = "DSL: экранирование | и \\ в значениях")]
        public void Dsl_EscapesPipeAndBackslash()
        {
            // Arrange
            var conditions = new[]
            {
                new DataTableFilterCondition { Join = Join.And, Column = "A|B", Operator = Op.Equal, Value = "x\\y|z" }
            };

            // Act
            var dsl = DataTableFilterLogic.ToConditionsDsl(conditions);
            var parsed = DataTableFilterLogic.ParseConditionsDsl(dsl);

            // Assert
            parsed.Should().HaveCount(1);
            parsed[0].Column.Should().Be("A|B");
            parsed[0].Value.Should().Be("x\\y|z");
        }

        [Fact(DisplayName = "DSL: пустая строка → пустой список")]
        public void Dsl_EmptyString_ReturnsEmptyList()
        {
            // Act
            var parsed = DataTableFilterLogic.ParseConditionsDsl("   ");

            // Assert
            parsed.Should().BeEmpty();
        }

        [Fact(DisplayName = "DSL: null → пустой список")]
        public void Dsl_Null_ReturnsEmptyList()
        {
            // Act
            var parsed = DataTableFilterLogic.ParseConditionsDsl(null);

            // Assert
            parsed.Should().BeEmpty();
        }

        [Fact(DisplayName = "DSL: некорректный формат строки → ошибка")]
        public void Dsl_MalformedLine_Throws()
        {
            // Act
            Action act = () => DataTableFilterLogic.ParseConditionsDsl("And|Age|GreaterOrEqual");

            // Assert — только 3 части вместо 4
            act.Should().Throw<UtilityException>();
        }

        [Fact(DisplayName = "DSL: неизвестный оператор → ошибка")]
        public void Dsl_UnknownOperator_Throws()
        {
            // Act
            Action act = () => DataTableFilterLogic.ParseConditionsDsl("And|Age|ОператорМагия|5");

            // Assert
            act.Should().Throw<UtilityException>().WithMessage("*оператор*");
        }

        // =====================================================================
        // Обработка ошибок SQL-выражений
        // =====================================================================

        [Fact(DisplayName = "Filter: null-таблица → ArgumentNullException")]
        public void Filter_NullTable_Throws()
        {
            // Act
            Action act = () => Logic.Filter(null, "Age = 1");

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact(DisplayName = "Filter: пустое выражение → ошибка")]
        public void Filter_EmptyExpression_Throws()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            Action act = () => Logic.Filter(table, "   ");

            // Assert
            act.Should().Throw<UtilityException>();
        }

        [Fact(DisplayName = "Filter: неизвестная колонка → ошибка с именем")]
        public void Filter_UnknownColumn_ThrowsWithName()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            Action act = () => Logic.Filter(table, "Отдел = 'ИТ'");

            // Assert
            act.Should().Throw<UtilityException>().WithMessage("*Отдел*");
        }

        [Fact(DisplayName = "Filter: незакрытая кавычка → ошибка")]
        public void Filter_UnclosedQuote_Throws()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            Action act = () => Logic.Filter(table, "City = 'Москва");

            // Assert
            act.Should().Throw<UtilityException>().WithMessage("*кавычка*");
        }

        [Fact(DisplayName = "Filter: неожиданный конец выражения → ошибка")]
        public void Filter_TruncatedExpression_Throws()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            Action act = () => Logic.Filter(table, "Age >");

            // Assert
            act.Should().Throw<UtilityException>();
        }

        [Fact(DisplayName = "Filter: лишняя закрывающая скобка → ошибка")]
        public void Filter_StrayParen_Throws()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            Action act = () => Logic.Filter(table, "Age > 20)");

            // Assert
            act.Should().Throw<UtilityException>();
        }

        [Fact(DisplayName = "Filter: литерал слева от оператора → ошибка")]
        public void Filter_LiteralOnLeft_Throws()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            Action act = () => Logic.Filter(table, "20 < Age");

            // Assert
            act.Should().Throw<UtilityException>().WithMessage("*колонки*");
        }

        [Fact(DisplayName = "Filter: ключевое слово как имя колонки без скобок → ошибка")]
        public void Filter_KeywordAsColumn_Throws()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            Action act = () => Logic.Filter(table, "AND = 1");

            // Assert
            act.Should().Throw<UtilityException>().WithMessage("*скобки*");
        }

        // =====================================================================
        // Интеграционные тесты
        // =====================================================================

        [Fact(DisplayName = "Интеграция: сложное выражение с LIKE, IN и скобками")]
        public void Integration_ComplexExpression()
        {
            // Arrange
            var table = CreateTestTable();

            // Act
            var result = Logic.Filter(table,
                "(Name LIKE 'И%' AND Age >= 30) OR City IN ('Berlin', 'Сочи')");

            // Assert — Мария (Berlin) и Иван-40 (LIKE И% + возраст)
            result.FilteredCount.Should().Be(2);
            result.Filtered.Rows.Cast<DataRow>()
                .Select(r => (int)r["Age"])
                .Should().BeInAscendingOrder()
                .And.Equal(35, 40);
        }

        [Fact(DisplayName = "Интеграция: фильтр по конструктору даёт тот же результат, что SQL")]
        public void Integration_ConditionsMatchSql()
        {
            // Arrange
            var table = CreateTestTable();
            var conditions = new[]
            {
                new DataTableFilterCondition { Join = Join.And, Column = "Age", Operator = Op.GreaterOrEqual, Value = "30" },
                new DataTableFilterCondition { Join = Join.And, Column = "City", Operator = Op.Equal, Value = "Москва" }
            };

            // Act
            var fromConditions = Logic.Filter(table, conditions);
            var fromSql = Logic.Filter(table, "Age >= 30 AND City = 'Москва'");

            // Assert
            fromConditions.FilteredCount.Should().Be(fromSql.FilteredCount);
            fromConditions.Filtered.Rows.Cast<DataRow>().Select(r => (int)r["Age"])
                .Should().Equal(fromSql.Filtered.Rows.Cast<DataRow>().Select(r => (int)r["Age"]));
        }
    }
}
