// =============================================================================
// SessionManager.cs — реализация ISessionManager
//
// Управляет сессиями браузера, обеспечивая регистрацию, получение и удаление
// WebDriver экземпляров. Использует потокобезопасное хранилище.
//
// Особенности:
//   - Потокобезопасность через lock
//   - Валидация активности сессий
//   - Интеграция с BrowserSessionContext (ambient context)
//   - Автоматическая очистка недействительных сессий
// =============================================================================

using OpenQA.Selenium;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Менеджер сессий браузера с потокобезопасным хранилищем.
    /// </summary>
    public class SessionManager : ISessionManager
    {
        private readonly Dictionary<string, IWebDriver> _sessions;
        private readonly object _lock = new object();

        public SessionManager()
        {
            _sessions = new Dictionary<string, IWebDriver>();
        }

        /// <summary>
        /// Регистрирует новую сессию браузера.
        /// </summary>
        public void RegisterSession(string sessionId, IWebDriver driver)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID не может быть пустым", nameof(sessionId));

            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            lock (_lock)
            {
                if (_sessions.ContainsKey(sessionId))
                    throw new InvalidOperationException($"Сессия с ID '{sessionId}' уже зарегистрирована");

                _sessions[sessionId] = driver;
            }
        }

        /// <summary>
        /// Получает WebDriver по идентификатору сессии.
        /// </summary>
        public IWebDriver GetSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return null;

            lock (_lock)
            {
                if (_sessions.TryGetValue(sessionId, out var driver))
                {
                    // Проверка активности сессии
                    if (IsSessionValid(driver))
                        return driver;

                    // Удаление недействительной сессии
                    _sessions.Remove(sessionId);
                    return null;
                }

                return null;
            }
        }

        /// <summary>
        /// Проверяет существование сессии.
        /// </summary>
        public bool SessionExists(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            lock (_lock)
            {
                return _sessions.ContainsKey(sessionId);
            }
        }

        /// <summary>
        /// Удаляет сессию из менеджера.
        /// </summary>
        public bool RemoveSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            lock (_lock)
            {
                return _sessions.Remove(sessionId);
            }
        }

        /// <summary>
        /// Получает список всех активных сессий.
        /// </summary>
        public IEnumerable<string> GetActiveSessions()
        {
            lock (_lock)
            {
                // Возвращаем копию для потокобезопасности
                return _sessions.Keys.ToList();
            }
        }

        /// <summary>
        /// Получает текущий идентификатор сессии из ambient context.
        /// </summary>
        public string GetCurrentSessionId()
        {
            return BrowserSessionContext.CurrentSessionId;
        }

        /// <summary>
        /// Устанавливает текущий идентификатор сессии в ambient context.
        /// </summary>
        public void SetCurrentSessionId(string sessionId)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                BrowserSessionContext.SetSessionId(sessionId);
            }
        }

        /// <summary>
        /// Очищает все сессии.
        /// </summary>
        public void ClearAllSessions()
        {
            lock (_lock)
            {
                _sessions.Clear();
            }
        }

        /// <summary>
        /// Проверяет валидность сессии WebDriver.
        /// </summary>
        private bool IsSessionValid(IWebDriver driver)
        {
            try
            {
                // Попытка получить текущий URL как проверка активности
                var _ = driver.CurrentUrl;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
