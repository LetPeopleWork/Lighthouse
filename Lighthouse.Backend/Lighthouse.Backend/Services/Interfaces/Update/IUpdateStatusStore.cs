using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Services.Interfaces.Update
{
    public interface IUpdateStatusStore
    {
        bool TryAdmit(UpdateKey key, UpdateStatus status);

        UpdateStatus? Advance(UpdateKey key, UpdateProgress to);

        /// <summary>
        /// Marks work cancelled only while it is still waiting to start, and answers null when it had already
        /// started, was never admitted, or was already past waiting.
        ///
        /// Whether work has started is not something a caller can read and then act on: the queue's reader is
        /// starting work at the same time, and between the reading and the acting it can have started. Marking
        /// a running refresh cancelled takes it out of <see cref="HasActiveWork"/> while it is still talking to
        /// a tracker, and everything that waits for this instance to go idle stops waiting - including the gate
        /// that holds database maintenance off. So the reading and the acting are one step, here.
        /// </summary>
        UpdateStatus? CancelIfStillWaiting(UpdateKey key);

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

        /// <summary>
        /// Everything currently admitted, whichever replica admitted it. <see cref="HasActiveWork"/> answers
        /// whether anything is happening; this answers what. Terminal work is not listed, because it is removed
        /// from the store as its run ends - what comes back is work an operator can still do something about.
        /// </summary>
        IReadOnlyList<UpdateStatus> GetAdmittedWork();

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
