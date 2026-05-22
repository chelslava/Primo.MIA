// =============================================================================
// ElementRepository.cs — реализация IElementRepository
//
// Хранилище для WebElement экземпляров с использованием WeakReference
// для предотвращения утечек памяти.
//
// Особенности:
//   - WeakReference для автоматической сборки мусора
//   - Потокобезопасность через lock
//   - Автоматическая проверка stale элементов
//   - Периодическая очистка недействительных ссылок
// =============================================================================

using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using Primo.MIA.Common;

namespace Primo.MIA
{
    /// <summary>
    /// Репозиторий для хранения WebElement с использованием слабых ссылок.
    /// </summary>
    public class ElementRepository : IElementRepository
    {
        private readonly Dictionary<string, WeakReference<IWebElement>> _elements;
        private readonly object _lock = new object();

        public ElementRepository()
        {
            _elements = new Dictionary<string, WeakReference<IWebElement>>();
        }

        /// <summary>
        /// Сохраняет элемент с указанным идентификатором.
        /// </summary>
        public void StoreElement(string elementId, IWebElement element)
        {
            Guard.NotNullOrWhiteSpace(elementId, nameof(elementId));
            Guard.NotNull(element, nameof(element));

            lock (_lock)
            {
                _elements[elementId] = new WeakReference<IWebElement>(element);
            }
        }

        /// <summary>
        /// Получает элемент по идентификатору.
        /// </summary>
        public IWebElement GetElement(string elementId)
        {
            if (string.IsNullOrWhiteSpace(elementId))
                return null;

            lock (_lock)
            {
                if (_elements.TryGetValue(elementId, out var weakRef))
                {
                    if (weakRef.TryGetTarget(out var element))
                    {
                        // Проверка на stale element
                        if (IsElementValid(element))
                            return element;

                        // Удаление stale элемента
                        _elements.Remove(elementId);
                    }
                    else
                    {
                        // Элемент был собран сборщиком мусора
                        _elements.Remove(elementId);
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Проверяет существование элемента.
        /// </summary>
        public bool ElementExists(string elementId)
        {
            if (string.IsNullOrWhiteSpace(elementId))
                return false;

            lock (_lock)
            {
                if (_elements.TryGetValue(elementId, out var weakRef))
                {
                    return weakRef.TryGetTarget(out var element) && IsElementValid(element);
                }

                return false;
            }
        }

        /// <summary>
        /// Удаляет элемент из хранилища.
        /// </summary>
        public bool RemoveElement(string elementId)
        {
            if (string.IsNullOrWhiteSpace(elementId))
                return false;

            lock (_lock)
            {
                return _elements.Remove(elementId);
            }
        }

        /// <summary>
        /// Очищает все элементы.
        /// </summary>
        public void ClearAllElements()
        {
            lock (_lock)
            {
                _elements.Clear();
            }
        }

        /// <summary>
        /// Очищает недействительные элементы (собранные GC или stale).
        /// </summary>
        public void CleanupStaleElements()
        {
            lock (_lock)
            {
                var keysToRemove = new List<string>();

                foreach (var kvp in _elements)
                {
                    if (!kvp.Value.TryGetTarget(out var element) || !IsElementValid(element))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _elements.Remove(key);
                }
            }
        }

        /// <summary>
        /// Проверяет валидность элемента (не stale).
        /// </summary>
        private bool IsElementValid(IWebElement element)
        {
            try
            {
                // Попытка доступа к свойству как проверка stale
                var _ = element.Enabled;
                return true;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
            catch
            {
                // Другие исключения считаем как невалидный элемент
                return false;
            }
        }
    }
}
