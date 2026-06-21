using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public sealed class InProcessUpdateExecutionLock : IUpdateExecutionLock
    {
        private static readonly IAsyncDisposable NoOpScope = new NoOpDisposable();

        public Task<IAsyncDisposable> AcquireAsync(UpdateKey key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NoOpScope);
        }

        private sealed class NoOpDisposable : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
