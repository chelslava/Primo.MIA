using NUnit.Framework;
using System;
using Primo.MIA.Browser;

namespace Primo.MIA.Tests.Integration.Browser
{
    /// <summary>
    /// Integration-тесты для работы с sessionID в реальных сценариях
    /// </summary>
    [TestFixture]
    public class SessionIdIntegrationTests
    {
        [SetUp]
        public void Setup()
        {
            BrowserSessionContext.ClearStack();
        }

        [TearDown]
        public void TearDown()
        {
            BrowserSessionContext.ClearStack();
        }

        [Test]
        public void Scenario_BrowserOpenWithinContainer_UsesContainerSession()
        {
            // Arrange
            const string containerSessionId = "container-session-123";
            
            // Act - Симуляция входа в контейнер (например, Sequence)
            BrowserSessionContext.Push(containerSessionId, isContainerEntry: true);
            
            // Симуляция вызова BrowserOpen внутри контейнера
            var resolvedId = SessionResolver.Resolve("");
            
            // Assert
            Assert.That(resolvedId, Is.EqualTo(containerSessionId),
                "BrowserOpen внутри контейнера должен использовать sessionId контейнера");
            
            // Cleanup
            BrowserSessionContext.Pop();
        }

        [Test]
        public void Scenario_NestedBrowserActivities_MaintainSessionStack()
        {
            // Arrange
            const string mainSessionId = "main-session";
            const string nestedSessionId = "nested-session";
            
            // Act - Основная сессия
            BrowserSessionContext.Push(mainSessionId, isContainerEntry: true);
            Assert.That(SessionResolver.Resolve(""), Is.EqualTo(mainSessionId));
            
            // Вложенная активность создает свою сессию
            BrowserSessionContext.Push(nestedSessionId, isContainerEntry: false);
            Assert.That(SessionResolver.Resolve(""), Is.EqualTo(nestedSessionId));
            
            // После завершения вложенной активности возвращаемся к основной
            BrowserSessionContext.Pop();
            Assert.That(SessionResolver.Resolve(""), Is.EqualTo(mainSessionId));
            
            // Cleanup
            BrowserSessionContext.Pop();
        }

        [Test]
        public void Scenario_ExplicitSessionIdOverridesContext()
        {
            // Arrange
            const string contextSessionId = "context-session";
            const string explicitSessionId = "explicit-session";
            
            // Act
            BrowserSessionContext.Push(contextSessionId, isContainerEntry: true);
            
            // Явное указание sessionId должно переопределить контекст
            var resolvedId = SessionResolver.Resolve(explicitSessionId);
            
            // Assert
            Assert.That(resolvedId, Is.EqualTo(explicitSessionId),
                "Явно указанный sessionId должен иметь приоритет");
            
            // Контекст не должен измениться
            Assert.That(BrowserSessionContext.CurrentSessionId, Is.EqualTo(contextSessionId));
            
            // Cleanup
            BrowserSessionContext.Pop();
        }

        [Test]
        public void Scenario_MultipleContainersInSequence_EachClearsStack()
        {
            // Arrange & Act
            // Первый контейнер
            BrowserSessionContext.Push("container-1", isContainerEntry: true);
            BrowserSessionContext.Push("nested-1", isContainerEntry: false);
            Assert.That(BrowserSessionContext.CurrentSessionId, Is.EqualTo("nested-1"));
            
            // Второй контейнер должен очистить стек первого
            BrowserSessionContext.Push("container-2", isContainerEntry: true);
            Assert.That(BrowserSessionContext.CurrentSessionId, Is.EqualTo("container-2"));
            
            // Проверяем что стек первого контейнера был очищен
            BrowserSessionContext.Pop();
            Assert.That(BrowserSessionContext.CurrentSessionId, Is.Null,
                "После Pop второго контейнера стек должен быть пуст");
        }

        [Test]
        public void Scenario_BrowserOpenCreatesNewSession_ThenActivitiesUseIt()
        {
            // Arrange
            const string newSessionId = "new-browser-session";
            
            // Act - Симуляция BrowserOpen
            BrowserSessionContext.Push(newSessionId, isContainerEntry: true);
            
            // Последующие активности используют эту сессию
            var clickSessionId = SessionResolver.Resolve("");
            var inputSessionId = SessionResolver.Resolve("");
            var navigateSessionId = SessionResolver.Resolve("");
            
            // Assert
            Assert.That(clickSessionId, Is.EqualTo(newSessionId));
            Assert.That(inputSessionId, Is.EqualTo(newSessionId));
            Assert.That(navigateSessionId, Is.EqualTo(newSessionId));
            
            // Cleanup
            BrowserSessionContext.Pop();
        }

        [Test]
        public void Scenario_ParallelContainers_IsolatedSessions()
        {
            // Этот тест демонстрирует что каждый контейнер имеет изолированную сессию
            
            // Контейнер 1
            BrowserSessionContext.Push("parallel-session-1", isContainerEntry: true);
            var session1 = BrowserSessionContext.CurrentSessionId;
            BrowserSessionContext.Pop();
            
            // Контейнер 2 (параллельный)
            BrowserSessionContext.Push("parallel-session-2", isContainerEntry: true);
            var session2 = BrowserSessionContext.CurrentSessionId;
            BrowserSessionContext.Pop();
            
            // Assert
            Assert.That(session1, Is.Not.EqualTo(session2),
                "Параллельные контейнеры должны иметь разные сессии");
        }

        [Test]
        public void Scenario_AttachToExistingSession_WorksCorrectly()
        {
            // Arrange
            const string existingSessionId = "existing-session-456";
            
            // Симуляция: первый BrowserOpen создал сессию
            BrowserSessionContext.Push(existingSessionId, isContainerEntry: true);
            BrowserSessionContext.Pop();
            
            // Act - Второй BrowserOpen присоединяется к существующей сессии
            var attachedSessionId = SessionResolver.Resolve(existingSessionId);
            
            // Assert
            Assert.That(attachedSessionId, Is.EqualTo(existingSessionId),
                "Должна быть возможность присоединиться к существующей сессии");
        }

        [Test]
        public void Scenario_EmptySessionIdInNestedActivity_UsesParentSession()
        {
            // Arrange
            const string parentSessionId = "parent-session";
            
            // Act
            BrowserSessionContext.Push(parentSessionId, isContainerEntry: true);
            
            // Вложенная активность с пустым sessionId
            var nestedResolvedId = SessionResolver.Resolve("");
            
            // Assert
            Assert.That(nestedResolvedId, Is.EqualTo(parentSessionId),
                "Вложенная активность с пустым sessionId должна использовать родительскую сессию");
            
            // Cleanup
            BrowserSessionContext.Pop();
        }

        [Test]
        public void Scenario_ComplexWorkflow_MultipleNestedContainers()
        {
            // Сложный сценарий: несколько уровней вложенности
            
            // Уровень 1: Главный контейнер
            BrowserSessionContext.Push("main-container", isContainerEntry: true);
            Assert.That(SessionResolver.Resolve(""), Is.EqualTo("main-container"));
            
            // Уровень 2: Вложенная активность
            BrowserSessionContext.Push("nested-activity", isContainerEntry: false);
            Assert.That(SessionResolver.Resolve(""), Is.EqualTo("nested-activity"));
            
            // Уровень 3: Еще одна вложенная активность
            BrowserSessionContext.Push("deep-nested", isContainerEntry: false);
            Assert.That(SessionResolver.Resolve(""), Is.EqualTo("deep-nested"));
            
            // Возврат на уровень 2
            BrowserSessionContext.Pop();
            Assert.That(SessionResolver.Resolve(""), Is.EqualTo("nested-activity"));
            
            // Возврат на уровень 1
            BrowserSessionContext.Pop();
            Assert.That(SessionResolver.Resolve(""), Is.EqualTo("main-container"));
            
            // Cleanup
            BrowserSessionContext.Pop();
            Assert.That(BrowserSessionContext.CurrentSessionId, Is.Null);
        }
    }
}
