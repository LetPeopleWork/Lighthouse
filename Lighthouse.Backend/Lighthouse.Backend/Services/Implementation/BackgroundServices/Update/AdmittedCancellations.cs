using System.Collections.Concurrent;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// One cancellation source per admitted piece of work, living exactly as long as the key does.
    ///
    /// The lifetime is the whole point. A source kept past the key leaving the store would let a cancel
    /// asked for now stop work admitted later under the same name, which is a different refresh; a source
    /// created after the key is already on the task list leaves a window where an operator can press Cancel
    /// and nothing hears them.
    ///
    /// Every read and write races the run finishing, because the run disposes its own source on the way
    /// out. That is not an edge case - it is the ordinary "it finished while you were reading" the cancel
    /// route is explicitly idempotent about - so the races are answered here rather than left for each
    /// caller to remember.
    /// </summary>
    public sealed class AdmittedCancellations : IDisposable
    {
        private readonly ConcurrentDictionary<UpdateKey, CancellationTokenSource> cancellations = new();

        /// <summary>
        /// Safe to call before the key is admitted, and safe to call twice: the loser of the race disposes
        /// its own source rather than replacing the one already in use.
        /// </summary>
        public void Admit(UpdateKey key)
        {
            var admitted = new CancellationTokenSource();

            if (!cancellations.TryAdd(key, admitted))
            {
                admitted.Dispose();
            }
        }

        public void Forget(UpdateKey key)
        {
            if (cancellations.TryRemove(key, out var finished))
            {
                finished.Dispose();
            }
        }

        /// <summary>
        /// <see cref="CancellationToken.None"/> when the work has already gone. An uncancellable run is a
        /// better outcome than an exception thrown where the caller cannot catch it, which would leave the
        /// key admitted for good and the entity unable to refresh again at all.
        /// </summary>
        public CancellationToken TokenFor(UpdateKey key)
        {
            if (!cancellations.TryGetValue(key, out var cancellation))
            {
                return CancellationToken.None;
            }

            try
            {
                return cancellation.Token;
            }
            catch (ObjectDisposedException)
            {
                return CancellationToken.None;
            }
        }

        /// <summary>
        /// Asks the work for this key to stop, and says nothing if there is none. Cancelling something that
        /// has already finished is accepted by design: the row an operator clicked was drawn before they
        /// clicked it.
        /// </summary>
        public void Stop(UpdateKey key)
        {
            if (!cancellations.TryGetValue(key, out var cancellation))
            {
                return;
            }

            try
            {
                cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The work ended on its own between the lookup and here. Nothing left to stop.
            }
        }

        public void Dispose()
        {
            foreach (var key in cancellations.Keys)
            {
                Forget(key);
            }
        }
    }
}
