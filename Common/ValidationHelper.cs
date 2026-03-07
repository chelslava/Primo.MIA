// =============================================================================
// ValidationHelper.cs — вспомогательные методы для валидации активностей.
//
// Уменьшает дублирование кода валидации во всех активностях.
// =============================================================================

using LTools.Common.Model;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Вспомогательные методы для валидации параметров активностей.
    /// Используется для уменьшения дублирования кода.
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Добавляет ошибку валидации если поле пустое или null.
        /// </summary>
        /// <param name="result">Результат валидации</param>
        /// <param name="value">Проверяемое значение</param>
        /// <param name="fieldName">Имя поля для отображения</param>
        /// <param name="errorMessage">Сообщение об ошибке</param>
        public static void ValidateRequired(this ValidationResult result, string value, string fieldName, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = fieldName,
                    Error = errorMessage
                });
            }
        }

        /// <summary>
        /// Добавляет ошибку валидации если условие истинно.
        /// </summary>
        /// <param name="result">Результат валидации</param>
        /// <param name="condition">Условие ошибки</param>
        /// <param name="fieldName">Имя поля</param>
        /// <param name="errorMessage">Сообщение об ошибке</param>
        public static void ValidateCondition(this ValidationResult result, bool condition, string fieldName, string errorMessage)
        {
            if (condition)
            {
                result.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = fieldName,
                    Error = errorMessage
                });
            }
        }
    }
}
