// =============================================================================
// JsonDataTableLogic.cs — чистая логика конвертации JSON ↔ DataTable
// (вывод типов колонок и приведение значений), выделена из
// Utilities/JsonDataTableBack.cs для тестируемости.
// =============================================================================

using System;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace Primo.MIA
{
    /// <summary>
    /// Чистые функции вывода типов и конвертации значений JSON → DataTable.
    /// </summary>
    public static class JsonDataTableLogic
    {
        /// <summary>
        /// Определяет тип C# по значению JsonElement.
        /// Приоритет: int → long → double → bool → DateTime → string.
        /// </summary>
        public static Type InferType(JsonElement elem)
        {
            switch (elem.ValueKind)
            {
                case JsonValueKind.Number:
                    if (elem.TryGetInt32(out _)) return typeof(int);
                    if (elem.TryGetInt64(out _)) return typeof(long);
                    if (elem.TryGetDouble(out _)) return typeof(double);
                    return typeof(decimal);

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return typeof(bool);

                case JsonValueKind.String:
                    string s = elem.GetString() ?? string.Empty;
                    // Только инвариантная культура: типы колонок не должны
                    // зависеть от локали машины, на которой выполняется процесс
                    if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out _))
                        return typeof(DateTime);
                    return typeof(string);

                default:
                    return typeof(string);
            }
        }

        /// <summary>
        /// Конвертирует JsonElement в значение указанного типа C#.
        /// DBNull при JsonValueKind.Null.
        /// </summary>
        public static object ParseJsonValue(
            JsonElement elem, Type targetType, string dateFormat)
        {
            if (elem.ValueKind == JsonValueKind.Null)
                return DBNull.Value;

            try
            {
                if (targetType == typeof(int)) return elem.GetInt32();
                if (targetType == typeof(long)) return elem.GetInt64();
                if (targetType == typeof(double)) return elem.GetDouble();
                if (targetType == typeof(decimal)) return elem.GetDecimal();
                if (targetType == typeof(bool))
                    return elem.ValueKind == JsonValueKind.True ||
                           (elem.ValueKind == JsonValueKind.String &&
                            bool.TryParse(elem.GetString(), out bool b) && b);
                if (targetType == typeof(DateTime))
                {
                    string sv = elem.ValueKind == JsonValueKind.String
                        ? elem.GetString()
                        : elem.ToString();
                    // Инвариантная культура, затем культура машины
                    // (совместимо с CalendarDateHelper)
                    if (DateTime.TryParse(sv, CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out DateTime dt) ||
                        DateTime.TryParse(sv, CultureInfo.CurrentCulture,
                            DateTimeStyles.None, out dt))
                        return dt;
                    return DBNull.Value;
                }
                // string и всё остальное
                return elem.ValueKind == JsonValueKind.String
                    ? elem.GetString()
                    : elem.ToString();
            }
            catch
            {
                // Fallback — возвращаем как строку
                return elem.ValueKind == JsonValueKind.String
                    ? elem.GetString()
                    : elem.ToString();
            }
        }
    }
}
