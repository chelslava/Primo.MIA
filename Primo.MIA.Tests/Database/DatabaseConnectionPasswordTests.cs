using System;
using System.Security;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.Database
{
    /// <summary>
    /// Тесты SecureString-пароля строки подключения (issue #26):
    /// плейсхолдер {password}, обратная совместимость, ошибки, декодирование.
    /// Коллекция BrowserSessionStack: тесты мутируют глобальный RepoDict.SecDict.
    /// </summary>
    [Trait("Category", "Fast")]
    [Collection("BrowserSessionStack")]
    public class DatabaseConnectionPasswordTests
    {
        private const string Plain = "s3cr3t-пароль";

        private static SecureString SecureOf(string value)
        {
            var secure = new SecureString();
            foreach (var c in value ?? string.Empty)
                secure.AppendChar(c);
            secure.MakeReadOnly();
            return secure;
        }

        #region ApplyPassword

        [Fact(DisplayName = "Плейсхолдер заменяется паролем")]
        public void ApplyPassword_ReplacesPlaceholder()
        {
            var cs = "Server=srv;Database=db;User Id=sa;Password={password};";
            var result = DatabaseConnectionHelper.ApplyPassword(cs, SecureOf(Plain));
            result.Should().Be($"Server=srv;Database=db;User Id=sa;Password={Plain};");
        }

        [Fact(DisplayName = "Плейсхолдер может встречаться несколько раз")]
        public void ApplyPassword_ReplacesAllOccurrences()
        {
            var cs = "Password={password};X={password};";
            var result = DatabaseConnectionHelper.ApplyPassword(cs, SecureOf("p1"));
            result.Should().Be("Password=p1;X=p1;");
        }

        [Fact(DisplayName = "Без плейсхолдера строка не меняется (обратная совместимость)")]
        public void ApplyPassword_NoPlaceholder_Unchanged()
        {
            var cs = "Server=srv;Integrated Security=true;";
            var result = DatabaseConnectionHelper.ApplyPassword(cs, SecureOf(Plain));
            result.Should().BeSameAs(cs);
        }

        [Theory(DisplayName = "Пустая/null строка — возвращается как есть")]
        [InlineData(null)]
        [InlineData("")]
        public void ApplyPassword_EmptyConnectionString_Unchanged(string cs)
        {
            DatabaseConnectionHelper.ApplyPassword(cs, SecureOf(Plain)).Should().Be(cs);
        }

        [Fact(DisplayName = "Плейсхолдер без пароля — DatabaseException")]
        public void ApplyPassword_PlaceholderWithoutPassword_Throws()
        {
            Action act = () => DatabaseConnectionHelper.ApplyPassword(
                "Password={password};", null);
            act.Should().Throw<DatabaseException>()
                .WithMessage("*{password}*");
        }

        [Fact(DisplayName = "Пустой SecureString — подставляется пустая строка")]
        public void ApplyPassword_EmptySecureString()
        {
            var result = DatabaseConnectionHelper.ApplyPassword(
                "Password={password};", new SecureString());
            result.Should().Be("Password=;");
        }

        #endregion

        #region ToPlainString

        [Fact(DisplayName = "ToPlainString: корректное декодирование, включая кириллицу")]
        public void ToPlainString_Decodes()
        {
            DatabaseConnectionHelper.ToPlainString(SecureOf(Plain)).Should().Be(Plain);
            DatabaseConnectionHelper.ToPlainString(SecureOf("")).Should().BeEmpty();
        }

        [Fact(DisplayName = "ToPlainString: null — null")]
        public void ToPlainString_Null()
        {
            DatabaseConnectionHelper.ToPlainString(null).Should().BeNull();
        }

        #endregion

        #region ResolveConnectionString

        [Fact(DisplayName = "Resolve: без плейсхолдера — строка не трогается")]
        public void Resolve_NoPlaceholder_Unchanged()
        {
            var cs = "Server=srv;Integrated Security=true;";
            DatabaseConnectionHelper.ResolveConnectionString(cs).Should().BeSameAs(cs);
        }

        [Fact(DisplayName = "Resolve: пароль берётся из SecDict и снимается пустым")]
        public void Resolve_UsesSecDict()
        {
            var cs = "Password={password};";

            // Пароль не задан — ошибка
            RepoDict.SecDict.TryRemove(DatabaseConnectionHelper.PasswordKey, out _);
            Action act = () => DatabaseConnectionHelper.ResolveConnectionString(cs);
            act.Should().Throw<DatabaseException>();

            // Задан — подставляется
            var secure = SecureOf("pwd-123");
            RepoDict.SecDict[DatabaseConnectionHelper.PasswordKey] = secure;
            try
            {
                DatabaseConnectionHelper.ResolveConnectionString(cs)
                    .Should().Be("Password=pwd-123;");
            }
            finally
            {
                RepoDict.SecDict.TryRemove(DatabaseConnectionHelper.PasswordKey, out _);
            }
        }

        #endregion
    }
}
