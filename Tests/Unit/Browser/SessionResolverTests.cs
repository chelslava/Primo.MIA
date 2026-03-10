using NUnit.Framework;
using System;
using Primo.MIA.Browser;

namespace Primo.MIA.Tests.Unit.Browser
{
    /// <summary>
    /// Unit-тесты для SessionResolver
    /// </summary>
    [TestFixture]
    public class SessionResolverTests
    {
        [SetUp]
        public void Setup()
        {
            // Очистка контекста перед каждым тестом
            BrowserSessionContext.ClearStack();
        }

        [TearDown]
        public void TearDown()
        {
            // Очистка контекста после каждого теста
            BrowserSessionContext.ClearStack();
        }

        [Test]
        public void Resolve_WithValidSessionId_ReturnsSessionId()
        {
            // Arrange
            const string sessionId = "test-session-123";
            
            // Act
            var result = SessionResolver.Resolve(sessionId);
            
            // Assert
            Assert.That(result, Is.EqualTo(sessionId));
        }

        [Test]
        public void Resolve_WithEmptyString_ReturnsCurrentSessionId()
        {
            // Arrange
            const string currentSessionId = "current-session";
            BrowserSessionContext.Push(currentSessionId, isContainerEntry: true);
            
            try
            {
                // Act
                var result = SessionResolver.Resolve("");
                
                // Assert
                Assert.That(result, Is.EqualTo(currentSessionId));
            }
            finally
            {
                BrowserSessionContext.Pop();
            }
        }

        [Test]
        public void Resolve_WithEmptyStringAndNoContext_ReturnsEmptyString()
        {
            // Act
            var result = SessionResolver.Resolve("");
            
            // Assert
            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public void Resolve_WithNull_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                SessionResolver.Resolve(null));
        }

        [Test]
        public void Resolve_WithWhitespace_ReturnsCurrentSessionId()
        {
            // Arrange
            const string currentSessionId = "current-session";
            BrowserSessionContext.Push(currentSessionId, isContainerEntry: true);
            
            try
            {
                // Act
                var result = SessionResolver.Resolve("   ");
                
                // Assert
                Assert.That(result, Is.EqualTo(currentSessionId));
            }
            finally
            {
                BrowserSessionContext.Pop();
            }
        }

        [Test]
        public void Resolve_WithNestedContext_ReturnsTopSessionId()
        {
            // Arrange
            BrowserSessionContext.Push("session-1", isContainerEntry: false);
            BrowserSessionContext.Push("session-2", isContainerEntry: false);
            BrowserSessionContext.Push("session-3", isContainerEntry: false);
            
            try
            {
                // Act
                var result = SessionResolver.Resolve("");
                
                // Assert
                Assert.That(result, Is.EqualTo("session-3"));
            }
            finally
            {
                BrowserSessionContext.ClearStack();
            }
        }

        [Test]
        public void Resolve_PreferExplicitSessionIdOverContext()
        {
            // Arrange
            const string explicitSessionId = "explicit-session";
            BrowserSessionContext.Push("context-session", isContainerEntry: true);
            
            try
            {
                // Act
                var result = SessionResolver.Resolve(explicitSessionId);
                
                // Assert
                Assert.That(result, Is.EqualTo(explicitSessionId), 
                    "Явно указанный sessionId должен иметь приоритет над контекстом");
            }
            finally
            {
                BrowserSessionContext.Pop();
            }
        }

        [Test]
        public void Resolve_WithGuidSessionId_ReturnsGuid()
        {
            // Arrange
            var guidSessionId = Guid.NewGuid().ToString();
            
            // Act
            var result = SessionResolver.Resolve(guidSessionId);
            
            // Assert
            Assert.That(result, Is.EqualTo(guidSessionId));
        }
    }
}
