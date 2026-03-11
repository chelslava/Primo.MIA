// =============================================================================
// BrowserServicesFactory.cs — фабрика сервисов Browser модуля
//
// Предоставляет singleton экземпляры сервисов для активностей браузера.
// Использует Lazy<T> для ленивой инициализации и потокобезопасности.
//
// Особенности:
//   - Singleton паттерн для SessionManager, ElementRepository, ActivityLogger
//   - Factory паттерн для ElementLocator (новый экземпляр каждый раз)
//   - Потокобезопасность через Lazy<T>
//   - Метод Reset() для тестирования
// =============================================================================

using System;

namespace Primo.MIA
{
    /// <summary>
    /// Фабрика для создания и получения экземпляров сервисов Browser модуля.
    /// </summary>
    public static class BrowserServicesFactory
    {
        private static readonly Lazy<ISessionManager> _sessionManager = 
            new Lazy<ISessionManager>(() => new SessionManager());

        private static readonly Lazy<IElementRepository> _elementRepository = 
            new Lazy<IElementRepository>(() => new ElementRepository());

        private static readonly Lazy<IActivityLogger> _activityLogger = 
            new Lazy<IActivityLogger>(() => new ActivityLogger());

        /// <summary>
        /// Получает singleton экземпляр SessionManager.
        /// </summary>
        public static ISessionManager SessionManager => _sessionManager.Value;

        /// <summary>
        /// Получает singleton экземпляр ElementRepository.
        /// </summary>
        public static IElementRepository ElementRepository => _elementRepository.Value;

        /// <summary>
        /// Получает singleton экземпляр ActivityLogger.
        /// </summary>
        public static IActivityLogger ActivityLogger => _activityLogger.Value;

        /// <summary>
        /// Создаёт новый экземпляр ElementLocator.
        /// </summary>
        /// <remarks>
        /// ElementLocator не является singleton, так как может использоваться
        /// с различными конфигурациями в разных контекстах.
        /// </remarks>
        public static IElementLocator CreateElementLocator()
        {
            return new ElementLocator();
        }

        /// <summary>
        /// Сбрасывает все singleton экземпляры (для тестирования).
        /// </summary>
        internal static void Reset()
        {
            if (_sessionManager.IsValueCreated)
                _sessionManager.Value.ClearAllSessions();

            if (_elementRepository.IsValueCreated)
                _elementRepository.Value.ClearAllElements();
        }
    }
}