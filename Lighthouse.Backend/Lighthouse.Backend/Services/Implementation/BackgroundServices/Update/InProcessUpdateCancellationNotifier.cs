using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// One process, so "publish to every replica" is "hand it to the one subscriber there is".
    /// </summary>
    public sealed class InProcessUpdateCancellationNotifier : IUpdateCancellationNotifier
    {
        private readonly Subscribers<Action<UpdateKey>> cancelSubscribers = new();

        private readonly Subscribers<Action> cancelAllSubscribers = new();

        public Task PublishCancellationAsync(UpdateKey key)
        {
            foreach (var subscriber in cancelSubscribers.Snapshot())
            {
                subscriber(key);
            }

            return Task.CompletedTask;
        }

        public IDisposable Subscribe(Action<UpdateKey> onCancelled) => cancelSubscribers.Add(onCancelled);

        public Task PublishCancelAllAsync()
        {
            foreach (var subscriber in cancelAllSubscribers.Snapshot())
            {
                subscriber();
            }

            return Task.CompletedTask;
        }

        public IDisposable SubscribeToCancelAll(Action onCancelAll) => cancelAllSubscribers.Add(onCancelAll);

        private sealed class Subscribers<THandler>
        {
            private readonly List<THandler> handlers = [];

            public IDisposable Add(THandler handler)
            {
                lock (handlers)
                {
                    handlers.Add(handler);
                }

                return new Subscription(this, handler);
            }

            public THandler[] Snapshot()
            {
                lock (handlers)
                {
                    return [.. handlers];
                }
            }

            private void Remove(THandler handler)
            {
                lock (handlers)
                {
                    handlers.Remove(handler);
                }
            }

            private sealed class Subscription(Subscribers<THandler> subscribers, THandler handler) : IDisposable
            {
                public void Dispose() => subscribers.Remove(handler);
            }
        }
    }
}
