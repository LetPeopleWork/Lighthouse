using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Services.Interfaces.Update
{
    /// <summary>
    /// Carries "stop this" to whichever replica is actually running it.
    ///
    /// The token source lives in the process that admitted the work, and the task list deliberately shows
    /// work admitted by any replica - so the pod that answers the operator's click is usually not the pod
    /// that can act on it. Without this, Cancel would do nothing for most rows on a multi-replica instance
    /// and there would be no way to tell which rows those were.
    ///
    /// Deliberately the same shape as <see cref="IUpdateCompletionNotifier"/>, which already solves this
    /// exact problem for completion: one in-process implementation, one over Redis, and the queue
    /// subscribing once at construction.
    /// </summary>
    public interface IUpdateCancellationNotifier
    {
        Task PublishCancellationAsync(UpdateKey key);

        IDisposable Subscribe(Action<UpdateKey> onCancelled);

        /// <summary>
        /// Carries "stop everything" to every replica, because each one holds the token sources of the work it
        /// admitted and no other replica can reach them. Delivered at most once: a replica that misses it keeps
        /// running, and the operator can ask again.
        /// </summary>
        Task PublishCancelAllAsync();

        IDisposable SubscribeToCancelAll(Action onCancelAll);
    }
}
