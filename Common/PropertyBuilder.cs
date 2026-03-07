using LTools.Common.Helpers;
using LTools.Common.Model;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Вспомогательный класс для упрощенного создания свойств активностей.
    /// Уменьшает дублирование кода при инициализации sdkProperties.
    /// </summary>
    public static class PropertyBuilder
    {
        /// <summary>
        /// Создает свойство типа SCRIPT (выражение/переменная для чтения).
        /// </summary>
        /// <typeparam name="T">Тип данных свойства</typeparam>
        /// <param name="propName">Имя свойства</param>
        /// <param name="tooltip">Подсказка</param>
        /// <returns>Настроенный PropertiesItem</returns>
        public static WFHelper.PropertiesItem Script<T>(string propName, string tooltip)
        {
            return new WFHelper.PropertiesItem
            {
                PropName = propName,
                PropertyType = PropertyTypes.SCRIPT,
                EditorType = ScriptEditorTypes.NONE,
                DataType = typeof(T),
                ToolTip = tooltip,
                IsReadOnly = false
            };
        }

        /// <summary>
        /// Создает свойство типа VARIABLE (переменная для записи результата).
        /// </summary>
        /// <typeparam name="T">Тип данных свойства</typeparam>
        /// <param name="propName">Имя свойства</param>
        /// <param name="tooltip">Подсказка</param>
        /// <returns>Настроенный PropertiesItem</returns>
        public static WFHelper.PropertiesItem Variable<T>(string propName, string tooltip)
        {
            return new WFHelper.PropertiesItem
            {
                PropName = propName,
                PropertyType = PropertyTypes.VARIABLE,
                EditorType = ScriptEditorTypes.NONE,
                DataType = typeof(T),
                ToolTip = tooltip,
                IsReadOnly = false
            };
        }

        /// <summary>
        /// Создает свойство типа OBJECT для перечислений (enum).
        /// </summary>
        /// <typeparam name="T">Тип enum</typeparam>
        /// <param name="propName">Имя свойства</param>
        /// <param name="tooltip">Подсказка</param>
        /// <returns>Настроенный PropertiesItem</returns>
        public static WFHelper.PropertiesItem Enum<T>(string propName, string tooltip) where T : System.Enum
        {
            return new WFHelper.PropertiesItem
            {
                PropName = propName,
                PropertyType = PropertyTypes.OBJECT,
                EditorType = ScriptEditorTypes.NONE,
                DataType = typeof(T),
                ToolTip = tooltip,
                IsReadOnly = false
            };
        }

        /// <summary>
        /// Создает свойство типа SCRIPT для строки.
        /// Удобный метод для самого частого случая.
        /// </summary>
        /// <param name="propName">Имя свойства</param>
        /// <param name="tooltip">Подсказка</param>
        /// <returns>Настроенный PropertiesItem</returns>
        public static WFHelper.PropertiesItem String(string propName, string tooltip)
        {
            return Script<string>(propName, tooltip);
        }

        /// <summary>
        /// Создает свойство типа SCRIPT для булевого значения.
        /// </summary>
        /// <param name="propName">Имя свойства</param>
        /// <param name="tooltip">Подсказка</param>
        /// <returns>Настроенный PropertiesItem</returns>
        public static WFHelper.PropertiesItem Boolean(string propName, string tooltip)
        {
            return Script<bool>(propName, tooltip);
        }

        /// <summary>
        /// Создает свойство типа SCRIPT для целого числа.
        /// </summary>
        /// <param name="propName">Имя свойства</param>
        /// <param name="tooltip">Подсказка</param>
        /// <returns>Настроенный PropertiesItem</returns>
        public static WFHelper.PropertiesItem Int(string propName, string tooltip)
        {
            return Script<int>(propName, tooltip);
        }
    }
}
