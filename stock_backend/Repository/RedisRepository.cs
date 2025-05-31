using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stock_backend.Repository
{
    public class RedisRepository : IRedisRepository
    {
        private readonly IConnectionMultiplexer _connection;
        public RedisRepository(IConnectionMultiplexer connection)
        {
            _connection = connection;
        }

        public IDatabase DB => _connection.GetDatabase();

        public async Task StringSetWithSlidingExpirationAsync(string key, string value, TimeSpan slidingExpiration)
        {
            // 設定資料並加入過期時間
            await DB.StringSetAsync(key, value, slidingExpiration);
        }

        // 每次訪問時延長過期時間
        public async Task<string?> StringGetWithSlidingExpirationAsync(string key, TimeSpan slidingExpiration, string? value)
        {
            // 讀取資料
            var cached = await DB.StringGetAsync(key);
            if (cached.IsNullOrEmpty == false)
            {
                // 每次訪問都重新設定過期時間
                await DB.StringSetAsync(key, cached, slidingExpiration);
            }
            return cached;
        }

        //public Task PublishAsync(string channel, string message)
        //{
        //    return _connection.GetSubscriber().PublishAsync(RedisChannel.Literal(channel), message);
        //}
    }
}
