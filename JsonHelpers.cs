using System;
using System.Collections.Generic;
using System.Data;
using Newtonsoft.Json.Linq;

namespace Primo.MIA
{
    public static class JsonHelpers
    {
        /// <summary>
        /// Конвертирует JToken в DataTable.
        /// </summary>
        /// <param name="token">JToken для конвертации</param>
        /// <returns>DataTable, заполненная данными из JToken</returns>
        public static DataTable JTokenToDataTable(JToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token), "JToken не может быть null.");

            return JsonTokenToDataTable(token);
        }

        /// <summary>
        /// Конвертирует JToken в словарь string-object.
        /// </summary>
        /// <param name="token">JToken для конвертации</param>
        /// <returns>Словарь с распарсенными значениями</returns>
        public static Dictionary<string, object> JTokenToDictionary(JToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token), "JToken не может быть null.");

            return JsonTokenToDictionary(token);
        }

        /// <summary>
        /// Конвертирует JToken в список словарей.
        /// </summary>
        /// <param name="token">JToken для конвертации</param>
        /// <returns>Список словарей</returns>
        public static List<Dictionary<string, object>> JTokenToList(JToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token), "JToken не может быть null.");

            return JsonTokenToList(token);
        }

        /// <summary>
        /// Конвертирует структурированный JSON с метаданными в DataTable.
        /// </summary>
        /// <param name="token">JToken с структурой {TableName, Columns, RowCount, Data}</param>
        /// <returns>DataTable с правильными типами данных</returns>
        public static DataTable JTokenToStructuredDataTable(JToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token), "JToken не может быть null.");

            return JsonTokenToStructuredDataTable(token);
        }

        //---------------------------------------------------------------------
        //  ВСПОМОГАТЕЛЬНЫЕ ПРИВАТНЫЕ МЕТОДЫ
        //---------------------------------------------------------------------

        private static DataTable JsonTokenToDataTable(JToken token)
        {
            var table = new DataTable();

            if (token is JArray jArray)
            {
                if (jArray.Count == 0)
                    return table;

                var firstObject = jArray[0] as JObject;
                if (firstObject != null)
                {
                    // Создаем колонки на основе первого объекта
                    foreach (var property in firstObject.Properties())
                    {
                        table.Columns.Add(property.Name, typeof(object));
                    }

                    // Заполняем строки
                    foreach (var item in jArray)
                    {
                        if (item is JObject obj)
                        {
                            var row = table.NewRow();
                            foreach (var property in obj.Properties())
                            {
                                row[property.Name] = JTokenToObject(property.Value);
                            }
                            table.Rows.Add(row);
                        }
                    }
                }
            }
            else if (token is JObject singleObject)
            {
                // Если один объект, создаем одну строку
                foreach (var property in singleObject.Properties())
                {
                    table.Columns.Add(property.Name, typeof(object));
                }

                var row = table.NewRow();
                foreach (var property in singleObject.Properties())
                {
                    row[property.Name] = JTokenToObject(property.Value);
                }
                table.Rows.Add(row);
            }

            return table;
        }

        private static DataTable JsonTokenToStructuredDataTable(JToken token)
        {
            if (token is JObject jObject)
            {
                var table = new DataTable();

                // Получаем имя таблицы
                if (jObject["TableName"] != null)
                {
                    table.TableName = jObject["TableName"].Value<string>();
                }

                // Создаем колонки с правильными типами данных
                if (jObject["Columns"] is JArray columnsArray)
                {
                    foreach (var columnToken in columnsArray)
                    {
                        if (columnToken is JObject columnObj)
                        {
                            var name = columnObj["Name"]?.Value<string>();
                            var typeName = columnObj["Type"]?.Value<string>();

                            if (!string.IsNullOrEmpty(name))
                            {
                                Type columnType = GetTypeFromString(typeName);
                                table.Columns.Add(name, columnType);
                            }
                        }
                    }
                }

                // Заполняем данными
                if (jObject["Data"] is JArray dataArray)
                {
                    foreach (var dataToken in dataArray)
                    {
                        if (dataToken is JObject dataObj)
                        {
                            var row = table.NewRow();
                            foreach (DataColumn column in table.Columns)
                            {
                                var valueToken = dataObj[column.ColumnName];
                                if (valueToken != null && valueToken.Type != JTokenType.Null)
                                {
                                    row[column.ColumnName] = ConvertToType(valueToken, column.DataType);
                                }
                                else
                                {
                                    row[column.ColumnName] = DBNull.Value;
                                }
                            }
                            table.Rows.Add(row);
                        }
                    }
                }

                return table;
            }

            throw new ArgumentException("JToken должен быть объектом с структурой {TableName, Columns, Data}");
        }

        private static Type GetTypeFromString(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeof(string);

            switch (typeName.ToLower())
            {
                case "int32":
                case "int":
                    return typeof(int);
                case "int64":
                case "long":
                    return typeof(long);
                case "decimal":
                    return typeof(decimal);
                case "double":
                    return typeof(double);
                case "float":
                case "single":
                    return typeof(float);
                case "string":
                    return typeof(string);
                case "datetime":
                    return typeof(DateTime);
                case "boolean":
                case "bool":
                    return typeof(bool);
                case "byte":
                    return typeof(byte);
                case "guid":
                    return typeof(Guid);
                default:
                    return typeof(string);
            }
        }

        private static object ConvertToType(JToken token, Type targetType)
        {
            if (token == null || token.Type == JTokenType.Null)
                return DBNull.Value;

            try
            {
                if (targetType == typeof(DateTime))
                {
                    return token.Value<DateTime>();
                }
                else if (targetType == typeof(decimal))
                {
                    return token.Value<decimal>();
                }
                else if (targetType == typeof(int))
                {
                    return token.Value<int>();
                }
                else if (targetType == typeof(bool))
                {
                    return token.Value<bool>();
                }
                else if (targetType == typeof(double))
                {
                    return token.Value<double>();
                }
                else if (targetType == typeof(long))
                {
                    return token.Value<long>();
                }
                else if (targetType == typeof(Guid))
                {
                    return Guid.Parse(token.Value<string>());
                }
                else
                {
                    return token.Value<object>();
                }
            }
            catch
            {
                // Если преобразование не удалось, возвращаем значение как строку
                return token.ToString();
            }
        }

        private static Dictionary<string, object> JsonTokenToDictionary(JToken token)
        {
            var result = new Dictionary<string, object>();

            if (token is JObject jObject)
            {
                foreach (var property in jObject.Properties())
                {
                    result[property.Name] = JTokenToObject(property.Value);
                }
            }
            else if (token is JArray jArray && jArray.Count > 0 && jArray[0] is JObject)
            {
                // Если это массив объектов, возьмем первый объект
                foreach (var property in (JObject)jArray[0])
                {
                    result[property.Key] = JTokenToObject(property.Value);
                }
            }

            return result;
        }

        private static List<Dictionary<string, object>> JsonTokenToList(JToken token)
        {
            var result = new List<Dictionary<string, object>>();

            if (token is JArray jArray)
            {
                foreach (var item in jArray)
                {
                    if (item is JObject jObject)
                    {
                        result.Add(JsonTokenToDictionary(jObject));
                    }
                }
            }
            else if (token is JObject jObject)
            {
                result.Add(JsonTokenToDictionary(jObject));
            }

            return result;
        }

        private static object JTokenToObject(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

            switch (token.Type)
            {
                case JTokenType.Array:
                    return ((JArray)token).ToObject<List<object>>();
                case JTokenType.Object:
                    return JsonTokenToDictionary(token);
                case JTokenType.Boolean:
                    return token.Value<bool>();
                case JTokenType.Integer:
                    return token.Value<long>();
                case JTokenType.Float:
                    return token.Value<double>();
                case JTokenType.String:
                    return token.Value<string>();
                case JTokenType.Date:
                    return token.Value<DateTime>();
                default:
                    return token.ToString();
            }
        }
    }
}