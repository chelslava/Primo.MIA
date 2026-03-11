using OpenQA.Selenium;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Интерфейс для управления сессиями браузера.
    /// Обеспечивает регистрацию, получение и удаление WebDriver экземпляров.
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// Регистрирует новую сессию браузера.
        /// </summary>
        /// <param name="sessionId">Уникальный идентификатор сессии</param>
        /// <param name="driver">Экземпляр WebDriver</param>
        void RegisterSession(string sessionId, IWebDriver driver);

        /// <summary>
        /// Получает WebDriver по идентификатору сессии.
        /// </summary>
        /// <param name="sessionId">Идентификатор сессии</param>
        /// <returns>Экземпляр WebDriver или null если не найден</returns>
        IWebDriver GetSession(string sessionId);

        /// <summary>
        /// Проверяет существование сессии.
        /// </summary>
        /// <param name="sessionId">Идентификатор сессии</param>
        /// <returns>True если сессия существует</returns>
        bool SessionExists(string sessionId);

        /// <summary>
        /// Удаляет сессию из менеджера.
        /// </summary>
        /// <param name="sessionId">Идентификатор сессии</param>
        /// <returns>True если сессия была удалена</returns>
        bool RemoveSession(string sessionId);

        /// <summary>
        /// Получает список всех активных сессий.
        /// </summary>
        /// <returns>Коллекция идентификаторов активных сессий</returns>
        IEnumerable<string> GetActiveSessions();

        /// <summary>
        /// Получает текущий идентификатор сессии из контекста.
        /// </summary>
        /// <returns>Идентификатор текущей сессии или null</returns>
        string GetCurrentSessionId();

        /// <summary>
        /// Устанавливает текущий идентификатор сессии в контексте.
        /// </summary>
        /// <param name="sessionId">Идентификатор сессии</param>
        void SetCurrentSessionId(string sessionId);

        /// <summary>
        /// Очищает все сессии.
        /// </summary>
        void ClearAllSessions();
    }

    /// <summary>
    /// Интерфейс для поиска элементов на странице.
    /// Предоставляет методы для различных стратегий поиска.
    /// </summary>
    public interface IElementLocator
    {
        /// <summary>
        /// Находит элемент по локатору с ожиданием.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="locatorType">Тип локатора</param>
        /// <param name="locatorValue">Значение локатора</param>
        /// <param name="timeoutSeconds">Таймаут ожидания в секундах</param>
        /// <returns>Найденный элемент</returns>
        IWebElement FindElement(IWebDriver driver, LocatorType locatorType, string locatorValue, int timeoutSeconds);

        /// <summary>
        /// Находит все элементы по локатору.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="locatorType">Тип локатора</param>
        /// <param name="locatorValue">Значение локатора</param>
        /// <returns>Коллекция найденных элементов</returns>
        IReadOnlyCollection<IWebElement> FindElements(IWebDriver driver, LocatorType locatorType, string locatorValue);

        /// <summary>
        /// Пытается найти элемент, возвращая null если не найден.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="locatorType">Тип локатора</param>
        /// <param name="locatorValue">Значение локатора</param>
        /// <param name="timeoutSeconds">Таймаут ожидания в секундах</param>
        /// <returns>Найденный элемент или null</returns>
        IWebElement TryFindElement(IWebDriver driver, LocatorType locatorType, string locatorValue, int timeoutSeconds);

        /// <summary>
        /// Ожидает пока элемент станет кликабельным.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="locatorType">Тип локатора</param>
        /// <param name="locatorValue">Значение локатора</param>
        /// <param name="timeoutSeconds">Таймаут ожидания в секундах</param>
        /// <returns>Кликабельный элемент</returns>
        IWebElement WaitForClickable(IWebDriver driver, LocatorType locatorType, string locatorValue, int timeoutSeconds);

        /// <summary>
        /// Ожидает пока элемент станет видимым.
        /// </summary>
        /// <param name="driver">WebDriver</param>
        /// <param name="locatorType">Тип локатора</param>
        /// <param name="locatorValue">Значение локатора</param>
        /// <param name="timeoutSeconds">Таймаут ожидания в секундах</param>
        /// <returns>Видимый элемент</returns>
        IWebElement WaitForVisible(IWebDriver driver, LocatorType locatorType, string locatorValue, int timeoutSeconds);
    }

    /// <summary>
    /// Интерфейс для хранения элементов.
    /// Позволяет сохранять и получать элементы по идентификатору.
    /// </summary>
    public interface IElementRepository
    {
        /// <summary>
        /// Сохраняет элемент с указанным идентификатором.
        /// </summary>
        /// <param name="elementId">Идентификатор элемента</param>
        /// <param name="element">Экземпляр WebElement</param>
        void StoreElement(string elementId, IWebElement element);

        /// <summary>
        /// Получает элемент по идентификатору.
        /// </summary>
        /// <param name="elementId">Идентификатор элемента</param>
        /// <returns>Экземпляр WebElement или null</returns>
        IWebElement GetElement(string elementId);

        /// <summary>
        /// Проверяет существование элемента.
        /// </summary>
        /// <param name="elementId">Идентификатор элемента</param>
        /// <returns>True если элемент существует</returns>
        bool ElementExists(string elementId);

        /// <summary>
        /// Удаляет элемент из хранилища.
        /// </summary>
        /// <param name="elementId">Идентификатор элемента</param>
        /// <returns>True если элемент был удален</returns>
        bool RemoveElement(string elementId);

        /// <summary>
        /// Очищает все элементы.
        /// </summary>
        void ClearAllElements();
    }

    /// <summary>
    /// Интерфейс для логирования активностей.
    /// </summary>
    public interface IActivityLogger
    {
        /// <summary>
        /// Логирует информационное сообщение.
        /// </summary>
        void LogInfo(string activityName, string message, params object[] args);

        /// <summary>
        /// Логирует предупреждение.
        /// </summary>
        void LogWarning(string activityName, string message, params object[] args);

        /// <summary>
        /// Логирует ошибку.
        /// </summary>
        void LogError(string activityName, Exception ex, string message, params object[] args);

        /// <summary>
        /// Логирует отладочное сообщение.
        /// </summary>
        void LogDebug(string activityName, string message, params object[] args);
    }

    /// <summary>
    /// Типы локаторов для поиска элементов.
    /// </summary>
    public enum LocatorType
    {
        Id,
        Name,
        ClassName,
        TagName,
        LinkText,
        PartialLinkText,
        CssSelector,
        XPath
    }
}
