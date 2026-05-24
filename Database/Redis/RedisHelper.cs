using StackExchange.Redis;
using System.Collections.Concurrent;

namespace Primo.MIA
{
    internal static class RedisHelper
    {
        private static readonly ConcurrentDictionary<string, ConnectionMultiplexer> _pool
            = new ConcurrentDictionary<string, ConnectionMultiplexer>();

        public static IDatabase GetDatabase(string connectionString, int databaseIndex = 0)
        {
            var mux = _pool.GetOrAdd(connectionString, cs => ConnectionMultiplexer.Connect(cs));
            return mux.GetDatabase(databaseIndex);
        }
    }
}
