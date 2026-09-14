namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    using Lighthouse.Backend.Services.Interfaces;
    using Lighthouse.Backend.Services.Interfaces.Update;
    using System.Collections.Concurrent;

    public class InProcessUpdateStatusStore : IUpdateStatusStore
    {
        private readonly ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses;

        private readonly ILighthouseClock clock;

        public InProcessUpdateStatusStore(ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses, ILighthouseClock clock)
        {
            this.updateStatuses = updateStatuses;
            this.clock = clock;
        }

        public bool TryAdmit(UpdateKey key, UpdateStatus status)
        {
            var admittedAt = clock.Now;

            if (!updateStatuses.TryAdd(key, status))
            {
                return false;
            }

            status.QueuedAt = admittedAt;
            status.StartedAt = null;

            return true;
        }

        public UpdateStatus? Advance(UpdateKey key, UpdateProgress to)
        {
            if (!updateStatuses.TryGetValue(key, out var status))
            {
                return null;
            }

            if ((int)to >= (int)status.Status)
            {
                // Only on the way in, and only once. A refused advance leaves the ordinal where it was, and
                // stamping again would restart the clock of a run that has been going for some time.
                if (to == UpdateProgress.InProgress && status.StartedAt is null)
                {
                    status.StartedAt = clock.Now;
                }

                status.Status = to;
            }

            return status;
        }

        public void Requeue(UpdateKey key)
        {
            if (updateStatuses.TryGetValue(key, out var status))
            {
                // A coalesced follow-up is new work waiting, not the old work still waiting, and the run
                // that just ended is over.
                status.Status = UpdateProgress.Queued;
                status.QueuedAt = clock.Now;
                status.StartedAt = null;
            }
        }

        public bool TryGet(UpdateKey key, out UpdateStatus? status)
        {
            return updateStatuses.TryGetValue(key, out status);
        }

        public void Remove(UpdateKey key)
        {
            updateStatuses.TryRemove(key, out _);
        }

        /// <summary>
        /// Snapshots rather than the live entries. The queue goes on advancing an entry while a reader is
        /// part-way through it, and a reader that saw the new status beside the old moment - or a half-written
        /// one - would put a duration on the screen that belongs to neither. The Redis store rebuilds each row
        /// from what it read, so handing out copies here is also what makes the two agree.
        /// </summary>
        public IReadOnlyList<UpdateStatus> GetAdmittedWork()
        {
            return [.. updateStatuses.Values.Select(AsItStandsNow)];
        }

        private static UpdateStatus AsItStandsNow(UpdateStatus status)
        {
            return new UpdateStatus
            {
                UpdateType = status.UpdateType,
                Id = status.Id,
                Status = status.Status,
                QueuedAt = status.QueuedAt,
                StartedAt = status.StartedAt,
            };
        }

        public bool HasActiveWork()
        {
            return updateStatuses.Values.Any(status =>
                status.Status is UpdateProgress.Queued or UpdateProgress.InProgress);
        }

        public bool HasQueuedWork(IReadOnlyCollection<UpdateKey> keys)
        {
            return keys.Any(key =>
                updateStatuses.TryGetValue(key, out var status) && status.Status == UpdateProgress.Queued);
        }
    }
}
