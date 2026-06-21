using Lighthouse.Backend.Services.Interfaces.Update;
using StackExchange.Redis;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public sealed class RedisUpdateCompletionNotifier : IUpdateCompletionNotifier
    {
        private static readonly RedisChannel CompletionChannel = RedisChannel.Literal("lighthouse:update-completed");

        private readonly ISubscriber subscriber;

        public RedisUpdateCompletionNotifier(IConnectionMultiplexer multiplexer)
        {
            subscriber = multiplexer.GetSubscriber();
        }

        public bool IsDistributed => true;

        public Task PublishCompletionAsync(UpdateKey key)
        {
            return subscriber.PublishAsync(CompletionChannel, Encode(key));
        }

        public IDisposable Subscribe(Action<UpdateKey> onCompleted)
        {
            void Handler(RedisChannel _, RedisValue message)
            {
                if (TryDecode(message, out var key))
                {
                    onCompleted(key);
                }
            }

            subscriber.Subscribe(CompletionChannel, Handler);
            return new ChannelSubscription(subscriber, Handler);
        }

        private static RedisValue Encode(UpdateKey key) => $"{(int)key.UpdateType}:{key.Id}";

        private static bool TryDecode(RedisValue message, out UpdateKey key)
        {
            var parts = ((string?)message)?.Split(':');
            if (parts is { Length: 2 }
                && int.TryParse(parts[0], out var updateType)
                && int.TryParse(parts[1], out var id))
            {
                key = new UpdateKey((UpdateType)updateType, id);
                return true;
            }

            key = new UpdateKey(UpdateType.Team, 0);
            return false;
        }

        private sealed class ChannelSubscription : IDisposable
        {
            private readonly ISubscriber subscriber;
            private readonly Action<RedisChannel, RedisValue> handler;

            public ChannelSubscription(ISubscriber subscriber, Action<RedisChannel, RedisValue> handler)
            {
                this.subscriber = subscriber;
                this.handler = handler;
            }

            public void Dispose()
            {
                subscriber.Unsubscribe(CompletionChannel, handler);
            }
        }
    }
}
