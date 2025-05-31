using StackExchange.Redis;
using stock_backend.Repository;

namespace stock_backend.Services
{
    public class RedisPubSubRepository : IRedisPubSubRepository
    {
        private readonly ISubscriber _subscriber;
        private readonly string _channelPrefix;

        public RedisPubSubRepository(IConnectionMultiplexer connection, string channelPrefix)
        {
            _subscriber = connection.GetSubscriber();
            _channelPrefix = channelPrefix;
        }

        public async Task SubscribeAsync(string channel, Action<RedisChannel, RedisValue> handler)
        {
            await _subscriber.SubscribeAsync(RedisChannel.Literal($"{_channelPrefix}:{channel}"), handler);
        }

        public async Task PublishAsync(string channel, string message)
        {
            await _subscriber.PublishAsync(RedisChannel.Literal($"{_channelPrefix}:{channel}"), message);
        }
        public async Task UnsubscribeAsync(string channel, Action<RedisChannel, RedisValue> handler)
        {
            await _subscriber.UnsubscribeAsync(RedisChannel.Literal($"{_channelPrefix}:{channel}"), handler);
        }
    }
}
