using Lighthouse.Backend.Services.Interfaces.Update;
using StackExchange.Redis;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// Carries "stop this" to whichever replica is running it. The token source lives in the process that
    /// admitted the work and the task list shows work from every replica, so the pod answering an
    /// operator's click is usually not the pod that can act on it.
    ///
    /// Deliberately the same shape as <see cref="RedisUpdateCompletionNotifier"/>, down to the encoding:
    /// the two channels carry the same thing and there is no reason for a reader to learn it twice.
    /// </summary>
    public sealed class RedisUpdateCancellationNotifier : IUpdateCancellationNotifier
    {
        private static readonly RedisChannel CancellationChannel = RedisChannel.Literal("lighthouse:update-cancelled");

        private readonly ISubscriber subscriber;

        public RedisUpdateCancellationNotifier(IConnectionMultiplexer multiplexer)
        {
            subscriber = multiplexer.GetSubscriber();
        }

        public Task PublishCancellationAsync(UpdateKey key)
        {
            return subscriber.PublishAsync(CancellationChannel, Encode(key));
        }

        public IDisposable Subscribe(Action<UpdateKey> onCancelled)
        {
            void Handler(RedisChannel _, RedisValue message)
            {
                if (TryDecode(message, out var key))
                {
                    onCancelled(key);
                }
            }

            subscriber.Subscribe(CancellationChannel, Handler);
            return new ChannelSubscription(subscriber, Handler, CancellationChannel);
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

            key = new UpdateKey(default, 0);
            return false;
        }

        private sealed class ChannelSubscription(ISubscriber subscriber, Action<RedisChannel, RedisValue> handler, RedisChannel channel) : IDisposable
        {
            public void Dispose() => subscriber.Unsubscribe(channel, handler);
        }
    }
}
