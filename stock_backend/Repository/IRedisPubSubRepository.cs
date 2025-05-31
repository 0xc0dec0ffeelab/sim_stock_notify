using StackExchange.Redis;

namespace stock_backend.Repository
{
    public interface IRedisPubSubRepository
    {
        Task SubscribeAsync(string channel, Action<RedisChannel, RedisValue> handler);
        Task PublishAsync(string channel, string message);
        Task UnsubscribeAsync(string channel, Action<RedisChannel, RedisValue> handler);
    }
}
