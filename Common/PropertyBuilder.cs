using LTools.Common.Helpers;
using LTools.Common.Model;
using System.Data;
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

        /// <summary>
        /// Создает свойство типа SCRIPT с селектором папки (FOLDER_SELECTOR).
        /// </summary>
        /// <param name="propName">Имя свойства</param>
        /// <param name="tooltip">Подсказка</param>
        /// <returns>Настроенный PropertiesItem</returns>
        public static WFHelper.PropertiesItem FolderSelector(string propName, string tooltip)
        {
            return new WFHelper.PropertiesItem
            {
                PropName = propName,
                PropertyType = PropertyTypes.SCRIPT,
                EditorType = ScriptEditorTypes.FOLDER_SELECTOR,
                DataType = typeof(string),
                ToolTip = tooltip,
                IsReadOnly = false
            };
        }

        /// <summary>
        /// Создает свойство типа SCRIPT с селектором файла (FILE_SELECTOR).
        /// </summary>
        /// <param name="propName">Имя свойства</param>
        /// <param name="tooltip">Подсказка</param>
        /// <returns>Настроенный PropertiesItem</returns>
        public static WFHelper.PropertiesItem FileSelector(string propName, string tooltip)
        {
            return new WFHelper.PropertiesItem
            {
                PropName = propName,
                PropertyType = PropertyTypes.SCRIPT,
                EditorType = ScriptEditorTypes.FILE_SELECTOR,
                DataType = typeof(string),
                ToolTip = tooltip,
                IsReadOnly = false
            };
        }

        /// <summary>
        /// Создает свойство типа OBJECT для булевого значения (checkbox в UI).
        /// Используется для флагов которые не являются выражениями.
        /// </summary>
        /// <param name="propName">Имя свойства</param>
        /// <param name="tooltip">Подсказка</param>
        /// <returns>Настроенный PropertiesItem</returns>
        public static WFHelper.PropertiesItem BooleanObject(string propName, string tooltip)
        {
            return new WFHelper.PropertiesItem
            {
                PropName = propName,
                PropertyType = PropertyTypes.OBJECT,
                EditorType = ScriptEditorTypes.NONE,
                DataType = typeof(bool),
                ToolTip = tooltip,
                IsReadOnly = false
            };
        }

        /// <summary>
        /// Создает свойство типа OBJECT для DatTable.
        /// 
        /// </summary>
        /// <param name="propName">Имя свойства</param>
        /// <param name="tooltip">Подсказка</param>
        /// <returns>Настроенный PropertiesItem</returns>
        public static WFHelper.PropertiesItem TableObject(string propName, string tooltip)
        {
            return new WFHelper.PropertiesItem
            {
                PropName = propName,
                PropertyType = PropertyTypes.SCRIPT,
                EditorType = ScriptEditorTypes.COLLECTION,
                DataType = typeof(System.Data.DataTable),
                ToolTip = tooltip,
                IsReadOnly = false
            };
        }
    }
}
