using System;
using System.Collections.Generic;
using System.IO;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Статический класс для валидации аргументов методов (Guard Clauses).
    /// Обеспечивает единообразие проверок и сообщений об ошибках.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Guard Clauses — это паттерн, позволяющий вынести проверки аргументов
    /// в начало метода и сделать код более читаемым и поддерживаемым.
    /// </para>
    /// <para>
    /// Преимущества использования Guard:
    ///</para>
    ///<list type="bullet">
    /// <item><description>Единообразные сообщения об ошибках</description></item>
    /// <item><description>Меньше дублирования кода</description></item>
    /// <item><description>Легче поддерживать и изменять логику валидации</description></item>
    /// <item><description>Улучшенная читаемость методов</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public void ProcessData(string path, int timeout, Dictionary&lt;string, string&gt; data)
    /// {
    ///     Guard.NotNullOrWhiteSpace(path, nameof(path));
    ///     Guard.Positive(timeout, nameof(timeout));
    ///     Guard.NotNull(data, nameof(data));
    ///     // ... логика метода
    /// }
    /// </code>
    /// </example>
    public static class Guard
    {
        #region Null Checks

        /// <summary>
        /// Проверяет что значение не равно null.
        /// </summary>
        ///<typeparam name="T">Тип значения (ссылочный тип)</typeparam>
        /// <param name="value">Проверяемое значение</param>
        /// <param name="paramName">Имя параметра</param>
        /// <exception cref="ArgumentNullException">Выбрасывается если value равно null</exception>
        public static void NotNull<T>(T value, string paramName) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Проверяет что значение не равно null (для nullable типов).
        /// </summary>
        ///<typeparam name="T">Тип значения (значимый тип)</typeparam>
        /// <param name="value">Проверяемое значение</param>
        /// <param name="paramName">Имя параметра</param>
        /// <exception cref="ArgumentNullException">Выбрасывается если value равно null</exception>
        public static void NotNull<T>(T? value, string paramName) where T : struct
        {
            if (!value.HasValue)
                throw new ArgumentNullException(paramName);
        }

        #endregion

        #region String Checks

        /// <summary>
        /// Проверяет что строка не равна null или пустой.
        /// </summary>
        /// <param name="value">Проверяемая строка</param>
        ///<param name="paramName">Имя параметра</param>
        ///<exception cref="ArgumentException">Выбрасывается если строка null или пустая</exception>
        public static void NotNullOrEmpty(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"Параметр '{paramName}' не может быть пустым или равным null", paramName);
        }

        /// <summary>
        /// Проверяет что строка не равна null, пустой или состоит только из пробелов.
        /// </summary>
        ///<param name="value">Проверяемая строка</param>
        /// <param name="paramName">Имя параметра</param>
        /// <exception cref="ArgumentException">Выбрасывается если строка некорректна</exception>
        public static void NotNullOrWhiteSpace(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Параметр '{paramName}' не может быть пустым или состоять только из пробелов", paramName);
        }

        #endregion

        #region Numeric Checks

        /// <summary>
        /// Проверяет что значение больше нуля.
        /// </summary>
        ///<param name="value">Проверяемое значение</param>
        ///<param name="paramName">Имя параметра</param>
        ///<exception cref="ArgumentOutOfRangeException">Выбрасывается если значение &lt;= 0</exception>
        public static void Positive(int value, string paramName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(paramName, value, $"Параметр '{paramName}' должен быть больше нуля");
        }

        /// <summary>
        /// Проверяет что значение больше нуля.
        /// </summary>
        /// <param name="value">Проверяемое значение</param>
        /// <param name="paramName">Имя параметра</param>
        /// <exception cref="ArgumentOutOfRangeException">Выбрасывается если значение &lt;= 0</exception>
        public static void Positive(long value, string paramName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(paramName, value, $"Параметр '{paramName}' должен быть больше нуля");
        }

        /// <summary>
        /// Проверяет что значение больше или равно нулю.
        /// </summary>
        ///<param name="value">Проверяемое значение</param>
        ///<param name="paramName">Имя параметра</param>
        ///<exception cref="ArgumentOutOfRangeException">Выбрасывается если значение &lt; 0</exception>
        public static void NotNegative(int value, string paramName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(paramName, value, $"Параметр '{paramName}' не может быть отрицательным");
        }

        /// <summary>
        /// Проверяет что значение находится в указанном диапазоне.
        /// </summary>
        ///<param name="value">Проверяемое значение</param>
        ///<param name="min">Минимальное значение (включительно)</param>
        ///<param name="max">Максимальное значение (включительно)</param>
        ///<param name="paramName">Имя параметра</param>
        ///<exception cref="ArgumentOutOfRangeException">Выбрасывается если значение вне диапазона</exception>
        public static void InRange(int value, int min, int max, string paramName)
        {
            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(paramName, value,
                    $"Параметр '{paramName}' должен быть в диапазоне от {min} до {max}");
        }

        /// <summary>
        /// Проверяет что значение больше указанного минимума.
        /// </summary>
        /// <param name="value">Проверяемое значение</param>
        /// <param name="minValue">Минимальное значение (не включительно)</param>
        /// <param name="paramName">Имя параметра</param>
        /// <exception cref="ArgumentOutOfRangeException">Выбрасывается если значение &lt;= minValue</exception>
        public static void GreaterThan(int value, int minValue, string paramName)
        {
            if (value<= minValue)
                throw new ArgumentOutOfRangeException(paramName, value,
                    $"Параметр '{paramName}' должен быть больше {minValue}");
        }

        #endregion

        #region Collection Checks

        /// <summary>
        /// Проверяет что коллекция не равна null и содержит элементы.
        /// </summary>
        ///<typeparam name="T">Тип элементов коллекции</typeparam>
        ///<param name="collection">Проверяемая коллекция</param>
        /// <param name="paramName">Имя параметра</param>
        /// <exception cref="ArgumentNullException">Выбрасывается если коллекция равна null</exception>
        /// <exception cref="ArgumentException">Выбрасывается если коллекция пустая</exception>
        public static void NotEmpty<T>(ICollection<T> collection, string paramName)
        {
            if (collection == null)
                throw new ArgumentNullException(paramName);
            if (collection.Count == 0)
                throw new ArgumentException($"Коллекция '{paramName}' не может быть пустой", paramName);
        }

        /// <summary>
        /// Проверяет что список не равен null и содержит элементы.
        /// </summary>
        ///<typeparam name="T">Тип элементов списка</typeparam>
        /// <param name="list">Проверяемый список</param>
        /// <param name="paramName">Имя параметра</param>
        /// <exception cref="ArgumentNullException">Выбрасывается если список равен null</exception>
        /// <exception cref="ArgumentException">Выбрасывается если список пустой</exception>
        public static void NotEmpty<T>(IList<T> list, string paramName)
        {
            if (list == null)
                throw new ArgumentNullException(paramName);
            if (list.Count == 0)
                throw new ArgumentException($"Список '{paramName}' не может быть пустым", paramName);
        }

        /// <summary>
        /// Проверяет что словарь не равен null и содержит элементы.
        /// </summary>
        /// <typeparam name="TKey">Тип ключа</typeparam>
        /// <typeparam name="TValue">Тип значения</typeparam>
        /// <param name="dictionary">Проверяемый словарь</param>
        /// <param name="paramName">Имя параметра</param>
        /// <exception cref="ArgumentNullException">Выбрасывается если словарь равен null</exception>
        ///<exception cref="ArgumentException">Выбрасывается если словарь пустой</exception>
        public static void NotEmpty<TKey, TValue>(IDictionary<TKey, TValue> dictionary, string paramName)
        {
            if (dictionary == null)
                throw new ArgumentNullException(paramName);
            if (dictionary.Count == 0)
                throw new ArgumentException($"Словарь '{paramName}' не может быть пустым", paramName);
        }

        #endregion

        #region File/Directory Checks

        /// <summary>
        /// Проверяет что файл существует.
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="paramName">Имя параметра</param>
        /// <exception cref="ArgumentException">Выбрасывается если путь пустой</exception>
        /// <exception cref="FileNotFoundException">Выбрасывается если файл не найден</exception>
        public static void FileExists(string filePath, string paramName)
        {
            NotNullOrWhiteSpace(filePath, paramName);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл не найден: {filePath}", filePath);
        }

        /// <summary>
        /// Проверяет что директория существует.
        /// </summary>
        /// <param name="directoryPath">Путь к директории</param>
        /// <param name="paramName">Имя параметра</param>
        /// <exception cref="ArgumentException">Выбрасывается если путь пустой</exception>
        ///<exception cref="DirectoryNotFoundException">Выбрасывается если директория не найдена</exception>
        public static void DirectoryExists(string directoryPath, string paramName)
        {
            NotNullOrWhiteSpace(directoryPath, paramName);
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Директория не найдена: {directoryPath}");
        }

        #endregion

        #region Type Checks

        /// <summary>
        /// Проверяет что значение является корректным значением перечисления.
        /// </summary>
        /// <typeparam name="T">Тип перечисления</typeparam>
        /// <param name="value">Проверяемое значение</param>
        ///<param name="paramName">Имя параметра</param>
        ///<exception cref="ArgumentOutOfRangeException">Выбрасывается если значение некорректно</exception>
        public static void EnumDefined<T>(T value, string paramName) where T : struct, Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
                throw new ArgumentOutOfRangeException(paramName, value,
                    $"Значение {value} не является допустимым для перечисления {typeof(T).Name}");
        }

        /// <summary>
        /// Проверяет что объект является экземпляром указанного типа.
        /// </summary>
        /// <typeparam name="T">Ожидаемый тип</typeparam>
        /// <param name="value">Проверяемый объект</param>
        /// <param name="paramName">Имя параметра</param>
        /// <exception cref="ArgumentNullException">Выбрасывается если значение null</exception>
        /// <exception cref="InvalidCastException">Выбрасывается если тип не соответствует</exception>
        /// <returns>Значение, приведённое к типу T</returns>
        public static T IsType<T>(object value, string paramName)
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
            if (!(value is T))
                throw new InvalidCastException(
                    $"Параметр '{paramName}' должен быть типа {typeof(T).Name}, но получен {value.GetType().Name}");
            return (T)value;
        }

        #endregion

        #region Business Rules

        /// <summary>
        /// Проверяет что условие истинно.
        /// </summary>
        ///<param name="condition">Проверяемое условие</param>
        /// <param name="message">Сообщение об ошибке</param>
        ///<param name="paramName">Имя параметра (опционально)</param>
        /// <exception cref="ArgumentException">Выбрасывается если условие ложно</exception>
        public static void That(bool condition, string message, string paramName = null)
        {
            if (!condition)
            {
                if (!string.IsNullOrEmpty(paramName))
                    throw new ArgumentException(message, paramName);
                throw new ArgumentException(message);
            }
        }

        /// <summary>
        /// Проверяет что условие истинно (с использованием InvalidOperationException).
        /// Используется для проверки бизнес-правил и состояния объектов.
        /// </summary>
        ///<param name="condition">Проверяемое условие</param>
        /// <param name="message">Сообщение об ошибке</param>
        ///<exception cref="InvalidOperationException">Выбрасывается если условие ложно</exception>
        public static void ValidOperation(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        #endregion
    }
}
