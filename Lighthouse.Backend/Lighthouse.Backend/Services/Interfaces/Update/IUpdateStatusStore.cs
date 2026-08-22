using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Services.Interfaces.Update
{
    public interface IUpdateStatusStore
    {
        bool TryAdmit(UpdateKey key, UpdateStatus status);

        UpdateStatus? Advance(UpdateKey key, UpdateProgress to);

        /// <summary>
        /// Resets an already-admitted key back to <see cref="UpdateProgress.Queued"/> so the same key can run
        /// again without ever leaving the store. <see cref="Advance"/> cannot do this - it is deliberately
        /// monotonic - but a coalesced follow-up must keep the key continuously active, otherwise callers
        /// polling for "no active work" would observe idle in the handover and read the stale state the
        /// follow-up is about to correct. No-op when the key is not admitted.
        /// </summary>
        void Requeue(UpdateKey key);

        bool TryGet(UpdateKey key, out UpdateStatus? status);

        void Remove(UpdateKey key);

        bool HasActiveWork();

        /// <summary>
        /// Answers whether any of the given keys is admitted but has not started running yet. Work that is
        /// already running deliberately does not count: a caller reacting to its own update would otherwise
        /// find its own key still running and wait for itself forever. Scoped to the keys the caller names,
        /// because <see cref="HasActiveWork"/> is true whenever anything anywhere is busy and would park a
        /// caller behind updates that have nothing to do with it.
        /// </summary>
        bool HasQueuedWork(IReadOnlyCollection<UpdateKey> keys);
    }
}
