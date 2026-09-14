using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// One process, so "publish to every replica" is "hand it to the one subscriber there is".
    /// </summary>
    public sealed class InProcessUpdateCancellationNotifier : IUpdateCancellationNotifier
    {
        private readonly List<Action<UpdateKey>> subscribers = [];

        public Task PublishCancellationAsync(UpdateKey key)
        {
            foreach (var subscriber in Subscribers())
            {
                subscriber(key);
            }

            return Task.CompletedTask;
        }

        public IDisposable Subscribe(Action<UpdateKey> onCancelled)
        {
            lock (subscribers)
            {
                subscribers.Add(onCancelled);
            }

            return new Subscription(this, onCancelled);
        }

        private Action<UpdateKey>[] Subscribers()
        {
            lock (subscribers)
            {
                return [.. subscribers];
            }
        }

        private sealed class Subscription(InProcessUpdateCancellationNotifier notifier, Action<UpdateKey> handler) : IDisposable
        {
            public void Dispose()
            {
                lock (notifier.subscribers)
                {
                    notifier.subscribers.Remove(handler);
                }
            }
        }
    }
}
