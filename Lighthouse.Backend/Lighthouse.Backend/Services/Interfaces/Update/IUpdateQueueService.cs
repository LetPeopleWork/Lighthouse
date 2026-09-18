using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Services.Interfaces.Update
{
    public interface IUpdateQueueService
    {
        void EnqueueUpdate(UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask);

        Task EnqueueAndAwaitAsync(UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remembers work that a caller wants done, but not yet, and runs <paramref name="onNamedWorkCleared"/>
        /// once none of <paramref name="waitingOn"/> is still going - neither waiting to start nor running.
        /// The release happens whether that work succeeded or failed - a failure that stranded the held work
        /// would leave the caller waiting forever. Only the newest request per <paramref name="heldFor"/> is
        /// kept, because a single release already acts on the newest state.
        ///
        /// A caller naming its own key is the ordinary case rather than a mistake: both callers here ask
        /// from inside an execution that the held work depends on. A run leaves the store before the sweep
        /// that lets holds go, so such a hold is released the moment the asker's own run ends.
        /// </summary>
        void HoldUntilNamedWorkClears(UpdateKey heldFor, IReadOnlyCollection<UpdateKey> waitingOn, Action onNamedWorkCleared);

        /// <summary>
        /// Whether a request for <paramref name="heldFor"/> is parked by <see cref="HoldUntilNamedWorkClears"/>
        /// and still waiting to be let go. A caller about to ask for the same work can use this to recognise
        /// that the work is already promised and let the parked request be the one that does it.
        /// </summary>
        bool IsHeld(UpdateKey heldFor);

        /// <summary>
        /// Asks the work for <paramref name="key"/> to stop. Queued work leaves without ever reaching the
        /// tracker; running work stops at its next page. Idempotent by design - cancelling something that
        /// has already finished, or was never admitted, is accepted and changes nothing, because the row an
        /// operator clicked was drawn before they clicked it.
        ///
        /// On a multi-replica instance the work is often running on a different pod from the one answering
        /// the click, so this is published to every replica rather than handled locally.
        /// </summary>
        Task CancelAsync(UpdateKey key);

        Task DrainAsync(CancellationToken cancellationToken = default);
    }
}
