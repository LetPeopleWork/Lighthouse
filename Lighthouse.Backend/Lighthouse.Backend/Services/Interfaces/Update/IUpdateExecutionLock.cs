using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Services.Interfaces.Update
{
    public interface IUpdateExecutionLock
    {
        Task<IAsyncDisposable> AcquireAsync(UpdateKey key, CancellationToken cancellationToken = default);
    }
}
