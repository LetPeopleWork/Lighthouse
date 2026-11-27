using Lighthouse.Backend.Models;
using Lighthouse.Backend.Platform.Host;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
#pragma warning disable CS9113 // Skeleton: the writer uses these collaborators once it is filled in.
    public sealed class WithinSleSnapshotWriter(IWithinSleSnapshotRepository snapshots, ILighthouseClock clock) : IWithinSleSnapshotWriter
#pragma warning restore CS9113
    {
        public Task RecordToday(int ownerId, OwnerType ownerType, WithinSleFamilyReader family)
        {
            throw new NotImplementedException("Recording today in the Within SLE record is not built yet.");
        }

        public void FillDayIfAbsent(int ownerId, OwnerType ownerType, DateOnly day, WithinSleFamilyReader family)
        {
            throw new NotImplementedException("Filling in a day of the Within SLE record is not built yet.");
        }

        public Task SaveFilledDay()
        {
            throw new NotImplementedException("Saving a filled-in day of the Within SLE record is not built yet.");
        }
    }
}
