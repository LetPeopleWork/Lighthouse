using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Services.Interfaces.Update
{
    public interface IUpdateQueueService
    {
        void EnqueueUpdate(UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask);

        Task EnqueueAndAwaitAsync(UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remembers work that a caller wants done, but not yet, and runs <paramref name="onQueuedWorkCleared"/>
        /// once none of <paramref name="waitingOn"/> is sitting in the queue any more. The release happens
        /// whether that work succeeded or failed - a failure that stranded the held work would leave the caller
        /// waiting forever. Only the newest request per <paramref name="heldFor"/> is kept, because a single
        /// release already acts on the newest state.
        /// </summary>
        void HoldUntilQueuedWorkClears(UpdateKey heldFor, IReadOnlyCollection<UpdateKey> waitingOn, Action onQueuedWorkCleared);

        Task DrainAsync(CancellationToken cancellationToken = default);
    }
}
