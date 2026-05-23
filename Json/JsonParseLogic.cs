using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Чистая логика парсинга JSON без зависимости от SDK.
    /// Используется как production-кодом, так и unit-тестами.
    /// </summary>
    public class JsonParseLogic
    {
        public bool IsValidJson(string json) => HttpHelper.IsValidJson(json);

        public string FormatJson(string json) => HttpHelper.FormatJson(json);

        public Dictionary<string, object> ParseToDictionary(string json)
        {
            var jObject = JObject.Parse(json);
            return JObjectToDictionary(jObject);
        }

        public List<object> ParseToList(string json)
        {
            var jArray = JArray.Parse(json);
            return JArrayToList(jArray);
        }

        public Dictionary<string, object> JObjectToDictionary(JObject jObject)
        {
            var result = new Dictionary<string, object>();
            foreach (var property in jObject.Properties())
                result[property.Name] = JTokenToObject(property.Value);
            return result;
        }

        public List<object> JArrayToList(JArray jArray)
        {
            var result = new List<object>();
            foreach (var item in jArray)
                result.Add(JTokenToObject(item));
            return result;
        }

        public object JTokenToObject(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:   return JObjectToDictionary((JObject)token);
                case JTokenType.Array:    return JArrayToList((JArray)token);
                case JTokenType.String:   return token.ToString();
                case JTokenType.Integer:  return token.Value<long>();
                case JTokenType.Float:    return token.Value<double>();
                case JTokenType.Boolean:  return token.Value<bool>();
                case JTokenType.Null:     return null;
                case JTokenType.Date:     return token.Value<DateTime>();
                case JTokenType.Guid:     return token.Value<Guid>();
                case JTokenType.Uri:      return token.Value<Uri>();
                case JTokenType.TimeSpan: return token.Value<TimeSpan>();
                default:                  return token.ToString();
            }
        }
    }
}
