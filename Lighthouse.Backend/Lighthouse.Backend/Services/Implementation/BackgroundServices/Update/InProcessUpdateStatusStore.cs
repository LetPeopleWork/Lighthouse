namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    using Lighthouse.Backend.Services.Interfaces.Update;
    using System.Collections.Concurrent;

    public class InProcessUpdateStatusStore : IUpdateStatusStore
    {
        private readonly ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses;

        public InProcessUpdateStatusStore(ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses)
        {
            this.updateStatuses = updateStatuses;
        }

        public bool TryAdmit(UpdateKey key, UpdateStatus status)
        {
            return updateStatuses.TryAdd(key, status);
        }

        public UpdateStatus? Advance(UpdateKey key, UpdateProgress to)
        {
            if (!updateStatuses.TryGetValue(key, out var status))
            {
                return null;
            }

            if ((int)to >= (int)status.Status)
            {
                status.Status = to;
            }

            return status;
        }

        public bool TryGet(UpdateKey key, out UpdateStatus? status)
        {
            return updateStatuses.TryGetValue(key, out status);
        }

        public void Remove(UpdateKey key)
        {
            updateStatuses.TryRemove(key, out _);
        }

        public bool HasActiveWork()
        {
            return updateStatuses.Values.Any(status =>
                status.Status is UpdateProgress.Queued or UpdateProgress.InProgress);
        }
    }
}
