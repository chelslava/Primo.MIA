// =============================================================================
// RegexHelper.cs — вспомогательные методы для работы с регулярными выражениями.
//
// Содержит общую логику создания и валидации regex-паттернов.
// =============================================================================

using LTools.Common.Model;
using System;
using System.Text.RegularExpressions;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Вспомогательные методы для работы с регулярными выражениями.
    /// </summary>
    public static class RegexHelper
    {
        /// <summary>
        /// Создаёт скомпилированный Regex с учётом регистра.
        /// </summary>
        public static Regex CreateRegex(string pattern, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException("Regex-паттерн не может быть пустым");

            var options = RegexOptions.Compiled;
            if (!caseSensitive)
                options |= RegexOptions.IgnoreCase;

            try
            {
                return new Regex(pattern, options);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Некорректный regex-паттерн '{pattern}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Валидирует синтаксис regex-паттерна и добавляет ошибку в ValidationResult если некорректен.
        /// </summary>
        public static void ValidateRegexSyntax(ValidationResult result, string pattern, string fieldName = "Паттерн")
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return;

            try
            {
                _ = new Regex(pattern);
            }
            catch (ArgumentException ex)
            {
                result.Items.Add(new ValidationResult.ValidationItem()
                {
                    PropertyName = fieldName,
                    Error = $"Некорректный regex: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Транслирует wildcard-паттерн (* и ?) в regex-паттерн.
        /// </summary>
        public static string WildcardToRegex(string pattern)
        {
            return StringHelper.WildcardToRegex(pattern);
        }
    }
}
