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
    }
}
