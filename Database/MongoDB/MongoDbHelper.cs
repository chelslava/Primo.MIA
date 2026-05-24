using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace Primo.MIA
{
    internal static class MongoDbHelper
    {
        public static IMongoCollection<BsonDocument> GetCollection(string connectionString, string databaseName, string collectionName)
        {
            var client = new MongoClient(connectionString);
            return client.GetDatabase(databaseName).GetCollection<BsonDocument>(collectionName);
        }

        public static FilterDefinition<BsonDocument> ParseFilter(string filterJson)
        {
            if (string.IsNullOrWhiteSpace(filterJson) || filterJson.Trim() == "{}")
                return Builders<BsonDocument>.Filter.Empty;
            return BsonDocument.Parse(filterJson);
        }

        public static string DocumentsToJson(IEnumerable<BsonDocument> documents)
        {
            var list = documents.ToList();
            return "[" + string.Join(",", list.Select(d => d.ToJson())) + "]";
        }
    }
}
