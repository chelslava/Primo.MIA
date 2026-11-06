// =============================================================================
// DataTableSqlWhereParser.cs — парсер SQL-подобных выражений для фильтрации
// DataTable (аналог секции WHERE).
//
// Грамматика (рекурсивный спуск, ключевые слова регистронезависимы):
//   expr     := orExpr
//   orExpr   := andExpr (OR andExpr)*
//   andExpr  := unary (AND unary)*
//   unary    := NOT unary | primary
//   primary  := '(' expr ')' | predicate
//   predicate:= operand ( (= | == | <> | != | > | < | >= | <= ) operand
//                      | LIKE 'шаблон'
//                      | IN '(' литерал (',' литерал)* ')'
//                      | IS [NOT] NULL )
//   operand  := колонка | литерал
//
// Литералы: 'строка' ("строка"), число, true/false.
// Русские синонимы ключевых слов: И (AND), ИЛИ (OR), НЕ (NOT).
//
// Порядок строк парсер не меняет — возвращает предикат для LINQ Where().
// Ошибки синтаксиса и неизвестные колонки → UtilityException с позицией.
// =============================================================================

using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    /// <summary>
    /// Парсер SQL-подобных выражений фильтрации DataTable.
    /// Компилирует выражение в предикат <see cref="Func{DataRow, Boolean}"/>
    /// для использования с LINQ (порядок строк сохраняется).
    /// </summary>
    public static class DataTableSqlWhereParser
    {
        // ── ПУБЛИЧНЫЙ API ──────────────────────────────────────────────────────

        /// <summary>
        /// Разобрать SQL-подобное выражение и скомпилировать предикат по строке DataTable.
        /// </summary>
        /// <param name="table">Таблица — источник колонок.</param>
        /// <param name="expression">Выражение, например: Age &gt;= 18 AND Name LIKE 'Ив%'</param>
        /// <param name="caseSensitive">Учитывать регистр при сравнении строк.</param>
        /// <returns>Предикат, не изменяющий порядок строк.</returns>
        /// <exception cref="UtilityException">Ошибка синтаксиса или неизвестная колонка.</exception>
        public static Func<DataRow, bool> Compile(DataTable table, string expression, bool caseSensitive = false)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (string.IsNullOrWhiteSpace(expression))
                throw new UtilityException($"{ActivityStrings.Error_SqlQueryRequired}.");

            var tokens = Tokenize(expression);
            var parser = new Parser(tokens, table, caseSensitive, expression);
            var predicate = parser.ParseExpression();

            parser.ExpectEnd();
            return predicate;
        }

        // ── ТОКЕНИЗАТОР ────────────────────────────────────────────────────────

        private enum TokenKind { Word, String, Number, Symbol, LParen, RParen, Comma }

        private sealed class Token
        {
            public TokenKind Kind;
            public string Text;     // слово, символ или литерал без кавычек
            public int Position;    // позиция в исходной строке (0-based)
            public bool FromBrackets; // имя из [скобок] — не проверяется на ключевые слова
        }

        private static List<Token> Tokenize(string expression)
        {
            var tokens = new List<Token>();
            int i = 0;

            while (i < expression.Length)
            {
                char c = expression[i];

                if (char.IsWhiteSpace(c)) { i++; continue; }

                // Строковый литерал в кавычках ('' и "" равнозначны, удвоение — экранирование)
                if (c == '\'' || c == '"')
                {
                    tokens.Add(ReadStringLiteral(expression, ref i, c));
                    continue;
                }

                // [Имя колонки] — скобочная запись для имён с пробелами и ключевыми словами
                if (c == '[')
                {
                    int close = expression.IndexOf(']', i + 1);
                    if (close < 0)
                        throw new UtilityException(
                            $"{ActivityStrings.Error_SqlParse}: не закрыта квадратная скобка для имени колонки (позиция {i + 1}).");
                    tokens.Add(new Token { Kind = TokenKind.Word, Text = expression.Substring(i + 1, close - i - 1), Position = i, FromBrackets = true });
                    i = close + 1;
                    continue;
                }

                // Число (со знаком или без, с десятичной точкой)
                if (char.IsDigit(c) || (c == '-' && i + 1 < expression.Length && char.IsDigit(expression[i + 1])))
                {
                    int start = i;
                    i++; // знак или первая цифра
                    while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                        i++;
                    tokens.Add(new Token { Kind = TokenKind.Number, Text = expression.Substring(start, i - start), Position = start });
                    continue;
                }

                // Идентификатор / ключевое слово (буквы, цифры, подчёркивания; кириллица допустима)
                if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < expression.Length && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_'))
                        i++;
                    tokens.Add(new Token { Kind = TokenKind.Word, Text = expression.Substring(start, i - start), Position = start });
                    continue;
                }

                // Операторы: двухсимвольные раньше односимвольных
                string two = i + 1 < expression.Length ? expression.Substring(i, 2) : null;
                if (two == ">=" || two == "<=" || two == "<>" || two == "!=" || two == "==")
                {
                    tokens.Add(new Token { Kind = TokenKind.Symbol, Text = two, Position = i });
                    i += 2;
                    continue;
                }

                if (c == '=' || c == '>' || c == '<')
                {
                    tokens.Add(new Token { Kind = TokenKind.Symbol, Text = c.ToString(), Position = i });
                    i++;
                    continue;
                }

                if (c == '(') { tokens.Add(new Token { Kind = TokenKind.LParen, Text = "(", Position = i }); i++; continue; }
                if (c == ')') { tokens.Add(new Token { Kind = TokenKind.RParen, Text = ")", Position = i }); i++; continue; }
                if (c == ',') { tokens.Add(new Token { Kind = TokenKind.Comma, Text = ",", Position = i }); i++; continue; }

                throw new UtilityException(
                    $"{ActivityStrings.Error_SqlParse}: неожиданный символ '{c}' (позиция {i + 1}).");
            }

            if (tokens.Count == 0)
                throw new UtilityException($"{ActivityStrings.Error_SqlParse}: выражение пустое.");

            return tokens;
        }

        private static Token ReadStringLiteral(string expression, ref int i, char quote)
        {
            int start = i;
            i++; // открывающая кавычка
            var sb = new StringBuilder();

            while (i < expression.Length)
            {
                char c = expression[i];
                if (c == quote)
                {
                    // Удвоенная кавычка внутри литерала → экранированная кавычка
                    if (i + 1 < expression.Length && expression[i + 1] == quote)
                    {
                        sb.Append(quote);
                        i += 2;
                        continue;
                    }
                    i++; // закрывающая кавычка
                    return new Token { Kind = TokenKind.String, Text = sb.ToString(), Position = start };
                }
                sb.Append(c);
                i++;
            }

            throw new UtilityException(
                $"{ActivityStrings.Error_SqlParse}: не закрыта кавычка строкового литерала (позиция {start + 1}).");
        }

        // ── КЛЮЧЕВЫЕ СЛОВА ─────────────────────────────────────────────────────

        private static bool IsKeyword(Token t, params string[] keywords)
        {
            if (t.Kind != TokenKind.Word) return false;
            return keywords.Any(k => string.Equals(t.Text, k, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsAnd(Token t) => IsKeyword(t, "AND", "И");
        private static bool IsOr(Token t) => IsKeyword(t, "OR", "ИЛИ");
        private static bool IsNot(Token t) => IsKeyword(t, "NOT", "НЕ");
        private static bool IsLike(Token t) => IsKeyword(t, "LIKE");
        private static bool IsIn(Token t) => IsKeyword(t, "IN");
        private static bool IsIs(Token t) => IsKeyword(t, "IS");
        private static bool IsNull(Token t) => IsKeyword(t, "NULL");
        private static bool IsTrue(Token t) => IsKeyword(t, "TRUE");
        private static bool IsFalse(Token t) => IsKeyword(t, "FALSE");

        // ── ПАРСЕР ─────────────────────────────────────────────────────────────

        private sealed class Parser
        {
            private readonly List<Token> _tokens;
            private readonly DataTable _table;
            private readonly bool _caseSensitive;
            private readonly string _source;
            private int _pos;

            public Parser(List<Token> tokens, DataTable table, bool caseSensitive, string source)
            {
                _tokens = tokens;
                _table = table;
                _caseSensitive = caseSensitive;
                _source = source;
            }

            public Func<DataRow, bool> ParseExpression() => ParseOr();

            public void ExpectEnd()
            {
                if (_pos < _tokens.Count)
                {
                    var t = _tokens[_pos];
                    throw Error($"неожиданное '{t.Text}'", t.Position);
                }
            }

            private Func<DataRow, bool> ParseOr()
            {
                var left = ParseAnd();
                while (_pos < _tokens.Count && IsOr(_tokens[_pos]))
                {
                    _pos++;
                    var right = ParseAnd();
                    // Копия left обязательна: захват переменной left самой лямбдой
                    // даёт бесконечную рекурсию (StackOverflow)
                    var prev = left;
                    left = row => prev(row) || right(row);
                }
                return left;
            }

            private Func<DataRow, bool> ParseAnd()
            {
                var left = ParseUnary();
                while (_pos < _tokens.Count && IsAnd(_tokens[_pos]))
                {
                    _pos++;
                    var right = ParseUnary();
                    var prev = left;
                    left = row => prev(row) && right(row);
                }
                return left;
            }

            private Func<DataRow, bool> ParseUnary()
            {
                if (_pos < _tokens.Count && IsNot(_tokens[_pos]))
                {
                    _pos++;
                    var inner = ParseUnary();
                    return row => !inner(row);
                }
                return ParsePrimary();
            }

            private Func<DataRow, bool> ParsePrimary()
            {
                var t = Current();

                if (t.Kind == TokenKind.LParen)
                {
                    _pos++;
                    var inner = ParseOr();
                    Expect(TokenKind.RParen, ")");
                    return inner;
                }

                return ParsePredicate();
            }

            private Func<DataRow, bool> ParsePredicate()
            {
                // Левый операнд — всегда колонка (для «18 < Age» пишем «Age > 18»)
                Operand left = ParseOperand();
                if (!left.IsColumn)
                    throw Error("слева от оператора ожидается имя колонки", left.Position);
                var col = ResolveColumn(left.ColumnName, left.Position);
                var op = Current();

                // IS [NOT] NULL
                if (IsIs(op))
                {
                    _pos++;
                    bool negated = IsNot(Current());
                    if (negated) _pos++;
                    var nullTok = Current();
                    if (!IsNull(nullTok))
                        throw Error("после IS ожидается NULL или NOT NULL", nullTok.Position);
                    _pos++;

                    return negated
                        ? (Func<DataRow, bool>)(row => !DataTableFilterLogic.IsNullOrDbNull(row[col]))
                        : row => DataTableFilterLogic.IsNullOrDbNull(row[col]);
                }

                // LIKE 'шаблон' (% — любое число символов, _ — один символ)
                if (IsLike(op))
                {
                    _pos++;
                    var patternValue = ParseOperand().AsLiteral(this);
                    string pattern = patternValue == null
                        ? string.Empty
                        : Convert.ToString(patternValue, CultureInfo.InvariantCulture);
                    return row =>
                    {
                        string text = DataTableFilterLogic.CellAsString(row, col);
                        return text != null && DataTableFilterLogic.MatchLike(text, pattern, _caseSensitive);
                    };
                }

                // IN (v1, v2, ...)
                if (IsIn(op))
                {
                    _pos++;
                    Expect(TokenKind.LParen, "(");

                    var variants = new List<object>();
                    while (true)
                    {
                        variants.Add(ParseOperand().AsLiteral(this));
                        var next = Current();
                        if (next.Kind == TokenKind.Comma) { _pos++; continue; }
                        Expect(TokenKind.RParen, ")");
                        break;
                    }

                    return row => DataTableFilterLogic.CompareWithAny(row, col, variants, _caseSensitive);
                }

                // Двоичное сравнение
                string cmp;
                if (op.Kind == TokenKind.Symbol && (op.Text == "=" || op.Text == "==" || op.Text == "<>" ||
                    op.Text == "!=" || op.Text == ">" || op.Text == "<" || op.Text == ">=" || op.Text == "<="))
                {
                    cmp = op.Text == "==" ? "=" : op.Text == "!=" ? "<>" : op.Text;
                    _pos++;
                }
                else
                {
                    throw Error("ожидается оператор сравнения, LIKE, IN или IS NULL", op.Position);
                }

                var rightValue = ParseOperand().AsLiteral(this);
                return row => DataTableFilterLogic.Compare(row, col, cmp, rightValue, _caseSensitive);
            }

            private Operand ParseOperand()
            {
                var t = Current();
                switch (t.Kind)
                {
                    case TokenKind.Word:
                        _pos++;
                        if (t.FromBrackets) return Operand.Column(t.Text, t.Position);
                        if (IsTrue(t)) return Operand.Literal(true);
                        if (IsFalse(t)) return Operand.Literal(false);
                        if (IsNull(t) || IsAnd(t) || IsOr(t) || IsNot(t) || IsLike(t) || IsIn(t) || IsIs(t))
                            throw Error($"'{t.Text}' — ключевое слово не может быть именем колонки; используйте [скобки]", t.Position);
                        return Operand.Column(t.Text, t.Position);

                    case TokenKind.String:
                    case TokenKind.Number:
                        _pos++;
                        return Operand.Literal(ParseLiteral(t));

                    default:
                        throw Error("ожидается имя колонки или литерал", t.Position);
                }
            }

            private object ParseLiteral(Token t)
            {
                if (t.Kind == TokenKind.String) return t.Text;

                // Число: целое или с десятичной точкой (инвариантная культура)
                if (decimal.TryParse(t.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal d))
                    return d;

                throw Error($"не удалось разобрать число '{t.Text}'", t.Position);
            }

            private DataColumn ResolveColumn(string name, int position)
            {
                var column = _table.Columns
                    .Cast<DataColumn>()
                    .FirstOrDefault(c => string.Equals(c.ColumnName, name, StringComparison.OrdinalIgnoreCase));

                if (column == null)
                    throw new UtilityException(
                        $"{ActivityStrings.Error_UnknownColumn}: '{name}' (позиция {position + 1}). Таблица содержит: " +
                        string.Join(", ", _table.Columns.Cast<DataColumn>().Select(c => c.ColumnName)) + ".");

                return column;
            }

            private Token Current() =>
                _pos < _tokens.Count
                    ? _tokens[_pos]
                    : throw Error("выражение оборвалось", _source.Length);

            private void Expect(TokenKind kind, string display)
            {
                var t = Current();
                if (t.Kind != kind)
                    throw Error($"ожидается '{display}'", t.Position);
                _pos++;
            }

            private UtilityException Error(string message, int position) =>
                new UtilityException($"{ActivityStrings.Error_SqlParse}: {message} (позиция {position + 1}). Выражение: {_source}");

            /// <summary>Операнд: колонка (с позицией для сообщений об ошибках) или литерал.</summary>
            private readonly struct Operand
            {
                private Operand(string columnName, int position, object literalValue)
                {
                    ColumnName = columnName;
                    Position = position;
                    LiteralValue = literalValue;
                }

                public string ColumnName { get; }
                public int Position { get; }
                public object LiteralValue { get; }
                public bool IsColumn => ColumnName != null;

                public static Operand Column(string name, int position) => new Operand(name, position, null);
                public static Operand Literal(object value) => new Operand(null, -1, value);

                public object AsLiteral(Parser p) =>
                    IsColumn
                        ? throw p.Error("справа от оператора ожидается литерал, а не имя колонки", Position)
                        : LiteralValue;
            }
        }
    }
}
