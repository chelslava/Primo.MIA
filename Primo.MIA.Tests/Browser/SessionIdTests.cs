using System;
using FluentAssertions;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Browser
{
    /// <summary>
    /// Тесты для проверки работы механизма sessionID:
    /// - BrowserSessionContext (ambient context с стеком сессий в RepoDict)
    /// - SessionResolver (разрешение ID сессии с приоритетами)
    /// </summary>
    public class SessionIdTests : IDisposable
    {
        public SessionIdTests()
        {
            // Очищаем стек перед каждым тестом
            CleanupSessionStack();
        }

        public void Dispose()
        {
            // Очищаем стек после каждого теста
            CleanupSessionStack();
        }

        private void CleanupSessionStack()
        {
            // Удаляем стек из RepoDict
            RepoDict.Remove("__BrowserSessionStack__");
        }

        #region BrowserSessionContext Tests

        [Fact]
        public void BrowserSessionContext_Current_ReturnsNull_WhenStackIsEmpty()
        {
            // Arrange & Act
            var current = BrowserSessionContext.Current;

            // Assert
            current.Should().BeNull("стек сессий пуст");
        }

        [Fact]
        public void BrowserSessionContext_Push_StoresSessionId()
        {
            // Arrange
            const string sessionId = "test-session-123";

            try
            {
                // Act
                BrowserSessionContext.Push(sessionId);

                // Assert
                BrowserSessionContext.Current.Should().Be(sessionId);
            }
            finally
            {
                // Cleanup
                BrowserSessionContext.Pop();
            }
        }

        [Fact]
        public void BrowserSessionContext_Push_ThrowsException_WhenSessionIdIsNull()
        {
            // Act
            Action act = () => BrowserSessionContext.Push(null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*ID сессии не может быть пустым*");
        }

        [Fact]
        public void BrowserSessionContext_Push_ThrowsException_WhenSessionIdIsEmpty()
        {
            // Act
            Action act = () => BrowserSessionContext.Push("");

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*ID сессии не может быть пустым*");
        }

        [Fact]
        public void BrowserSessionContext_Push_ThrowsException_WhenSessionIdIsWhitespace()
        {
            // Act
            Action act = () => BrowserSessionContext.Push("   ");

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*ID сессии не может быть пустым*");
        }

        [Fact]
        public void BrowserSessionContext_Pop_RemovesSessionFromStack()
        {
            // Arrange
            const string sessionId = "test-session-456";
            BrowserSessionContext.Push(sessionId);

            // Act
            BrowserSessionContext.Pop();

            // Assert
            BrowserSessionContext.Current.Should().BeNull("сессия была удалена из стека");
        }

        [Fact]
        public void BrowserSessionContext_SupportsNestedSessions()
        {
            // Arrange
            const string session1 = "outer-session";
            const string session2 = "inner-session";

            try
            {
                // Act - Push первой сессии
                BrowserSessionContext.Push(session1);
                BrowserSessionContext.Current.Should().Be(session1);

                // Act - Push второй сессии (вложенной)
                BrowserSessionContext.Push(session2);
                BrowserSessionContext.Current.Should().Be(session2, "вложенная сессия должна быть активной");

                // Act - Pop вложенной сессии
                BrowserSessionContext.Pop();
                BrowserSessionContext.Current.Should().Be(session1, "после Pop должна вернуться внешняя сессия");

                // Act - Pop внешней сессии
                BrowserSessionContext.Pop();
                BrowserSessionContext.Current.Should().BeNull("после Pop всех сессий стек должен быть пуст");
            }
            finally
            {
                // Cleanup - на случай если тест упал
                while (BrowserSessionContext.Current != null)
                {
                    BrowserSessionContext.Pop();
                }
            }
        }

        [Fact]
        public void BrowserSessionContext_Pop_DoesNotThrow_WhenStackIsEmpty()
        {
            // Act
            Action act = () => BrowserSessionContext.Pop();

            // Assert
            act.Should().NotThrow("Pop на пустом стеке должен быть безопасным");
        }

        #endregion

        #region SessionResolver Tests

        [Fact]
        public void SessionResolver_Resolve_ReturnsExplicitSessionId_WhenProvided()
        {
            // Arrange
            const string explicitSessionId = "explicit-session-789";

            try
            {
                // Создаём контекст с другой сессией
                BrowserSessionContext.Push("context-session");

                // Act - явно указанная сессия имеет приоритет
                var resolved = SessionResolver.Resolve(explicitSessionId);

                // Assert
                resolved.Should().Be(explicitSessionId, "явная сессия имеет приоритет над контекстом");
            }
            finally
            {
                BrowserSessionContext.Pop();
            }
        }

        [Fact]
        public void SessionResolver_Resolve_ReturnsContextSession_WhenExplicitIsEmpty()
        {
            // Arrange
            const string contextSessionId = "context-session-abc";

            try
            {
                BrowserSessionContext.Push(contextSessionId);

                // Act - передаём пустую строку, должна взяться сессия из контекста
                var resolved = SessionResolver.Resolve("");

                // Assert
                resolved.Should().Be(contextSessionId, "если явная сессия пуста, берётся из контекста");
            }
            finally
            {
                BrowserSessionContext.Pop();
            }
        }

        [Fact]
        public void SessionResolver_Resolve_ReturnsContextSession_WhenExplicitIsNull()
        {
            // Arrange
            const string contextSessionId = "context-session-def";

            try
            {
                BrowserSessionContext.Push(contextSessionId);

                // Act - передаём null, должна взяться сессия из контекста
                var resolved = SessionResolver.Resolve(null);

                // Assert
                resolved.Should().Be(contextSessionId, "если явная сессия null, берётся из контекста");
            }
            finally
            {
                BrowserSessionContext.Pop();
            }
        }

        [Fact]
        public void SessionResolver_Resolve_ReturnsContextSession_WhenExplicitIsWhitespace()
        {
            // Arrange
            const string contextSessionId = "context-session-ghi";

            try
            {
                BrowserSessionContext.Push(contextSessionId);

                // Act - передаём пробелы, должна взяться сессия из контекста
                var resolved = SessionResolver.Resolve("   ");

                // Assert
                resolved.Should().Be(contextSessionId, "если явная сессия содержит только пробелы, берётся из контекста");
            }
            finally
            {
                BrowserSessionContext.Pop();
            }
        }

        [Fact]
        public void SessionResolver_Resolve_ThrowsException_WhenBothAreEmpty()
        {
            // Arrange - убеждаемся что контекст пуст
            while (BrowserSessionContext.Current != null)
            {
                BrowserSessionContext.Pop();
            }

            // Act
            Action act = () => SessionResolver.Resolve("");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*ID сессии не задан*активность запущена вне контейнера*");
        }

        [Fact]
        public void SessionResolver_Resolve_ThrowsException_WhenBothAreNull()
        {
            // Arrange - убеждаемся что контекст пуст
            while (BrowserSessionContext.Current != null)
            {
                BrowserSessionContext.Pop();
            }

            // Act
            Action act = () => SessionResolver.Resolve(null);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*ID сессии не задан*активность запущена вне контейнера*");
        }

        [Fact]
        public void SessionResolver_Resolve_PrioritizesExplicitOverContext()
        {
            // Arrange
            const string explicitSession = "explicit-priority";
            const string contextSession = "context-fallback";

            try
            {
                BrowserSessionContext.Push(contextSession);

                // Act
                var resolved = SessionResolver.Resolve(explicitSession);

                // Assert
                resolved.Should().Be(explicitSession, "явная сессия всегда имеет приоритет");
            }
            finally
            {
                BrowserSessionContext.Pop();
            }
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Integration_SessionWorkflow_SimulatesContainerUsage()
        {
            // Arrange
            const string outerSession = "outer-browser-session";
            const string innerSession = "inner-browser-session";

            try
            {
                // Simulate: Открыт внешний контейнер браузера
                BrowserSessionContext.Push(outerSession);

                // Активность внутри контейнера без явного sessionId
                var resolved1 = SessionResolver.Resolve(null);
                resolved1.Should().Be(outerSession);

                // Simulate: Открыт вложенный контейнер браузера
                BrowserSessionContext.Push(innerSession);

                // Активность внутри вложенного контейнера
                var resolved2 = SessionResolver.Resolve(null);
                resolved2.Should().Be(innerSession);

                // Активность с явным sessionId игнорирует контекст
                var resolved3 = SessionResolver.Resolve("manual-session");
                resolved3.Should().Be("manual-session");

                // Simulate: Закрыт вложенный контейнер
                BrowserSessionContext.Pop();

                // Активность снова видит внешний контейнер
                var resolved4 = SessionResolver.Resolve(null);
                resolved4.Should().Be(outerSession);

                // Simulate: Закрыт внешний контейнер
                BrowserSessionContext.Pop();

                // Активность вне контейнера должна упасть
                Action act = () => SessionResolver.Resolve(null);
                act.Should().Throw<ArgumentException>();
            }
            finally
            {
                // Cleanup
                while (BrowserSessionContext.Current != null)
                {
                    BrowserSessionContext.Pop();
                }
            }
        }

        #endregion
    }
}
