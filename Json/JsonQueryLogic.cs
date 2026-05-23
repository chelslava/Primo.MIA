using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Primo.MIA
{
    public class JsonQueryResult
    {
        public object Result { get; set; }
        public int Count { get; set; }
        public bool Found { get; set; }
    }

    /// <summary>
    /// Чистая логика JSONPath-запросов без зависимости от SDK.
    /// Используется как production-кодом, так и unit-тестами.
    /// </summary>
    public class JsonQueryLogic
    {
        private readonly JsonParseLogic _parseLogic;

        public JsonQueryLogic() : this(new JsonParseLogic()) { }

        public JsonQueryLogic(JsonParseLogic parseLogic)
        {
            _parseLogic = parseLogic;
        }

        public JsonQueryResult Query(string json, string jsonPath, bool returnFirst)
        {
            if (!_parseLogic.IsValidJson(json))
                throw new ArgumentException("Невалидный JSON формат");

            var jToken = JToken.Parse(json);
            var matches = jToken.SelectTokens(jsonPath).ToList();

            int count = matches.Count;
            bool found = count > 0;
            object result = null;

            if (found)
            {
                result = returnFirst
                    ? _parseLogic.JTokenToObject(matches[0])
                    : (object)matches.Select(t => _parseLogic.JTokenToObject(t)).ToList();
            }

            return new JsonQueryResult { Result = result, Count = count, Found = found };
        }
    }
}
