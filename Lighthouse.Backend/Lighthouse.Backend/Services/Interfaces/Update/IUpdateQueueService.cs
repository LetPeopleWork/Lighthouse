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

        /// <summary>
        /// Whether a request for <paramref name="heldFor"/> is parked by <see cref="HoldUntilQueuedWorkClears"/>
        /// and still waiting to be let go. A caller about to ask for the same work can use this to recognise
        /// that the work is already promised and let the parked request be the one that does it.
        /// </summary>
        bool IsHeld(UpdateKey heldFor);

        Task DrainAsync(CancellationToken cancellationToken = default);
    }
}
