using NUnit.Framework;
using System;
using Primo.MIA.Browser;

namespace Primo.MIA.Tests.Unit.Browser
{
    /// <summary>
    /// Unit-тесты для BrowserSessionContext
    /// </summary>
    [TestFixture]
    public class BrowserSessionContextTests
    {
        [SetUp]
        public void Setup()
        {
            // Очистка стека перед каждым тестом
            BrowserSessionContext.ClearStack();
        }

        [TearDown]
        public void TearDown()
        {
            // Очистка стека после каждого теста
            BrowserSessionContext.ClearStack();
        }

        [Test]
        public void CurrentSessionId_WhenStackEmpty_ReturnsNull()
        {
            // Act
            var result = BrowserSessionContext.CurrentSessionId;

            // Assert
            Assert.That(result, Is.Null, "CurrentSessionId должен возвращать null когда стек пуст");
        }

        [Test]
        public void Push_WithValidSessionId_SetsCurrentSessionId()
        {
            // Arrange
            const string sessionId = "test-session-123";

            // Act
            BrowserSessionContext.Push(sessionId, isContainerEntry: false);

            // Assert
            Assert.That(BrowserSessionContext.CurrentSessionId, Is.EqualTo(sessionId));
        }

        [Test]
        public void Push_WithContainerEntry_ClearsStackFirst()
        {
            // Arrange
            BrowserSessionContext.Push("old-session", isContainerEntry: false);
            BrowserSessionContext.Push("another-session", isContainerEntry: false);

            // Act
            BrowserSessionContext.Push("new-container-session", isContainerEntry: true);

            // Assert
            Assert.That(BrowserSessionContext.CurrentSessionId, Is.EqualTo("new-container-session"));
            
            // Проверяем что в стеке только одна сессия
            BrowserSessionContext.Pop();
            Assert.That(BrowserSessionContext.CurrentSessionId, Is.Null);
        }

        [Test]
        public void Push_MultipleSessionsWithoutContainer_CreatesStack()
        {
            // Arrange & Act
            BrowserSessionContext.Push("session-1", isContainerEntry: false);
            BrowserSessionContext.Push("session-2", isContainerEntry: false);
            BrowserSessionContext.Push("session-3", isContainerEntry: false);

            // Assert
            Assert.That(BrowserSessionContext.CurrentSessionId, Is.EqualTo("session-3"));

            // Проверяем стек
            BrowserSessionContext.Pop();
            Assert.That(BrowserSessionContext.CurrentSessionId, Is.EqualTo("session-2"));

            BrowserSessionContext.Pop();
            Assert.That(BrowserSessionContext.CurrentSessionId, Is.EqualTo("session-1"));

            BrowserSessionContext.Pop();
            Assert.That(BrowserSessionContext.CurrentSessionId, Is.Null);
        }

        [Test]
        public void Pop_WhenStackEmpty_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => BrowserSessionContext.Pop());
        }

        [Test]
        public void Pop_RemovesTopSessionFromStack()
        {
            // Arrange
            BrowserSessionContext.Push("session-1", isContainerEntry: false);
            BrowserSessionContext.Push("session-2", isContainerEntry: false);

            // Act
            BrowserSessionContext.Pop();

            // Assert
            Assert.That(BrowserSessionContext.CurrentSessionId, Is.EqualTo("session-1"));
        }

        [Test]
        public void ClearStack_RemovesAllSessions()
        {
            // Arrange
            BrowserSessionContext.Push("session-1", isContainerEntry: false);
            BrowserSessionContext.Push("session-2", isContainerEntry: false);
            BrowserSessionContext.Push("session-3", isContainerEntry: false);

            // Act
            BrowserSessionContext.ClearStack();

            // Assert
            Assert.That(BrowserSessionContext.CurrentSessionId, Is.Null);
        }

        [Test]
        public void Push_WithNullSessionId_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                BrowserSessionContext.Push(null, isContainerEntry: false));
        }

        [Test]
        public void Push_WithEmptySessionId_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                BrowserSessionContext.Push("", isContainerEntry: false));
        }

        [Test]
        public void Push_WithWhitespaceSessionId_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                BrowserSessionContext.Push("   ", isContainerEntry: false));
        }
    }
}
