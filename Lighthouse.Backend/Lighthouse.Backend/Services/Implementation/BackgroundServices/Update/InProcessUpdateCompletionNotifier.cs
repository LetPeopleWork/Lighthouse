using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public sealed class InProcessUpdateCompletionNotifier : IUpdateCompletionNotifier
    {
        public bool IsDistributed => false;

        public Task PublishCompletionAsync(UpdateKey key) => Task.CompletedTask;

        public IDisposable Subscribe(Action<UpdateKey> onCompleted) => NoOpSubscription.Instance;

        private sealed class NoOpSubscription : IDisposable
        {
            public static readonly NoOpSubscription Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
