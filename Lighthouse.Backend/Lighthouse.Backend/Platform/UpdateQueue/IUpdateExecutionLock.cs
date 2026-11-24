using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Platform.UpdateQueue
{
    public interface IUpdateExecutionLock
    {
        Task<IAsyncDisposable> AcquireAsync(UpdateKey key, CancellationToken cancellationToken = default);
    }
}
