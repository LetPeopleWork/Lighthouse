using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IProcessBehaviorSnapshotRepository : IRepository<ProcessBehaviorSnapshot>
    {
        /// <summary>
        /// Serves the persisted natural-process-limit series for one owner and metric family, ordered by
        /// RecordedAt. <paramref name="from"/> and <paramref name="to"/> are an optional window on
        /// RecordedAt, inclusive at both ends; either may be omitted independently, and omitting both
        /// returns the full history. The bounds are composed onto the query so the database applies them.
        /// </summary>
        IReadOnlyList<ProcessBehaviorSnapshot> GetSeries(int ownerId, OwnerType ownerType, ProcessBehaviorMetricType metricType, DateOnly? from, DateOnly? to);
    }
}
